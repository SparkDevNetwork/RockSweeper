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
        /// Internal event to know when new items can be read.
        /// </summary>
        private readonly AsyncAutoResetEvent _dataReady = new AsyncAutoResetEvent( false );

        /// <summary>
        /// The function that will produce items.
        /// </summary>
        private readonly Func<AsyncProducer<T>, CancellationToken, Task> _factory;

        /// <summary>
        /// The maximum number of background tasks to use when producing items.
        /// </summary>
        private readonly int? _maxConcurrency;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AsyncProducer{T}"/> that will
        /// have it's items provided via the <see cref="Enqueue(T)"/> method.
        /// </summary>
        public AsyncProducer()
        {
            _collection = new BlockingCollection<T>();
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
        public void Enqueue( T item )
        {
            _collection.Add( item );
            _dataReady.Set();
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
            T item;

            while ( !_collection.TryTake( out item ) )
            {
                if ( _collection.IsCompleted )
                {
                    throw new TaskCanceledException();
                }

                // It's possible two tasks each this point at the same time
                // with only 1 item left in the queue so we need to use a
                // timeout otherwise one task will be stuck waiting forever.
                await _dataReady.WaitAsync( TimeSpan.FromMilliseconds( 50 ), cancellationToken );
            }

            return item;
        }

        #endregion
    }
}
