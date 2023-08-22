using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Provides helpful methods to construct producers.
    /// </summary>
    public static class AsyncProducer
    {
        /// <summary>
        /// Creates a new producer from a set of items.
        /// </summary>
        /// <typeparam name="TIn">The type of item to produce.</typeparam>
        /// <param name="items">The items to be produced.</param>
        /// <returns>An instance of <see cref="AsyncProducer{T}"/>.</returns>
        public static AsyncProducer<TIn> FromItems<TIn>( IEnumerable<TIn> items )
        {
            return new AsyncProducer<TIn>( items );
        }

        /// <summary>
        /// Creates a new producer from a task that will generate the items
        /// on a background task.
        /// </summary>
        /// <typeparam name="TIn">The type of item to produce.</typeparam>
        /// <param name="factory">The function that will produce the items.</param>
        /// <param name="maxConcurrency">The maximum number of factory instances to run in parallel.</param>
        /// <returns>An instance of <see cref="AsyncProducer{T}"/>.</returns>
        public static AsyncProducer<TIn> FromFactory<TIn>( Func<AsyncProducer<TIn>, CancellationToken, Task> factory, int? maxConcurrency = null )
        {
            return new AsyncProducer<TIn>( factory, maxConcurrency );
        }
    }

    /// <summary>
    /// A producer that makes items available asyncronously.
    /// </summary>
    /// <typeparam name="T">The type of item to be produced.</typeparam>
    public class AsyncProducer<T> : IAsyncConsumable<T>, IAsyncRunnable
    {
        #region Fields

        /// <summary>
        /// The thread-safe backing collection that will hold the queued items.
        /// </summary>
        private readonly BlockingCollection<T> _collection = new BlockingCollection<T>();

        /// <summary>
        /// The function that will produce items.
        /// </summary>
        private readonly Func<AsyncProducer<T>, CancellationToken, Task> _factory;

        /// <summary>
        /// The maximum number of background tasks to use when producing items.
        /// </summary>
        private readonly int? _maxConcurrency;

        /// <summary>
        /// The semaphore the tracks how many items we can store.
        /// </summary>
        private readonly SemaphoreSlim _addToBufferLock;

        /// <summary>
        /// Indicates that items are in the buffer ready to be read.
        /// </summary>
        private readonly SemaphoreSlim _readFromBufferLock = new SemaphoreSlim( 0 );

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AsyncProducer{T}"/> that will
        /// have it's items provided via the <see cref="Enqueue(T)"/> method.
        /// </summary>
        public AsyncProducer( int maximumBufferSize = 100 )
        {
            _collection = new BlockingCollection<T>();
            _addToBufferLock = new SemaphoreSlim( maximumBufferSize );
        }

        /// <summary>
        /// Creates a new instance of <see cref="AsyncProducer{T}"/> that will
        /// have it's items filled in by the set of items. Additional items
        /// can be added via the <see cref="Enqueue(T)"/> method.
        /// </summary>
        /// <param name="initialItems">The initial set of items.</param>
        public AsyncProducer( IEnumerable<T> initialItems )
            : this()
        {
            foreach ( var item in initialItems )
            {
                Enqueue( item );
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="AsyncProducer{T}"/> that will
        /// use the factory function to populate it's items. Calling the
        /// <see cref="Enqueue(T)"/> method should only happen inside the factory.
        /// </summary>
        /// <param name="factory">The function to execute to generate the items.</param>
        /// <param name="maxConcurrency">The maximum number of factories to run in parallel.</param>
        public AsyncProducer( Func<AsyncProducer<T>, CancellationToken, Task> factory, int? maxConcurrency = null )
            : this()
        {
            _factory = factory;
            _maxConcurrency = maxConcurrency;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public async Task RunAsync( CancellationToken cancellationToken = default )
        {
            if ( _factory == null )
            {
                Complete();

                return;
            }

            var concurrency = _maxConcurrency ?? 4;
            var tasks = new List<Task>();

            for ( int i = 0; i < concurrency; i++ )
            {
                tasks.Add( Task.Run( async () => await _factory( this, cancellationToken ) ) );
            }

            await Task.WhenAll( tasks );

            Complete();
        }

        /// <summary>
        /// Adds a new item to the queue.
        /// </summary>
        /// <param name="item">The item to be processed.</param>
        public async Task EnqueueAsync( T item, CancellationToken cancellationToken )
        {
            if ( _addToBufferLock != null )
            {
                await _addToBufferLock.WaitAsync( cancellationToken );
            }

            Enqueue( item );
        }

        /// <summary>
        /// Adds a new item to the queue.
        /// </summary>
        /// <param name="item">The item to be processed.</param>
        private void Enqueue( T item )
        {
            _collection.Add( item );
            _readFromBufferLock.Release();
        }

        /// <summary>
        /// Informs the producer that no new items will be added.
        /// </summary>
        public void Complete()
        {
            _collection.CompleteAdding();
        }

        /// <inheritdoc/>
        public async Task<T> DequeueAsync( CancellationToken cancellationToken = default )
        {
            var timeout = TimeSpan.FromMilliseconds( 50 );

            while ( !await _readFromBufferLock.WaitAsync( timeout, cancellationToken ) )
            {
                if ( _collection.IsCompleted )
                {
                    throw new TaskCanceledException();
                }
            }

            var item = _collection.Take( cancellationToken );

            if ( _addToBufferLock != null )
            {
                _addToBufferLock.Release();
            }

            return item;
        }

        #endregion
    }
}
