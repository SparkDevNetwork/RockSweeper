using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    /// <summary>
    /// An asyncronous pipe that consumes a series of items and translates
    /// them into a different item.
    /// </summary>
    /// <typeparam name="TIn">The type of item to be used as the input.</typeparam>
    /// <typeparam name="TOut">The type of item to be used as the output.</typeparam>
    public class AsyncPipe<TIn, TOut> : IAsyncConsumable<TOut>, IAsyncRunnable
    {
        #region Fields

        /// <summary>
        /// The consumable that contains all the items to be processed.
        /// </summary>
        private readonly IAsyncConsumable<TIn> _consumable;

        /// <summary>
        /// The producer we will use to store the processed items.
        /// </summary>
        private readonly AsyncProducer<TOut> _producer = new AsyncProducer<TOut>();

        /// <summary>
        /// The function that will convert the items.
        /// </summary>
        private readonly Func<TIn, Task<TOut>> _converter;

        /// <summary>
        /// The maximum number of conversion operations to run in parallel.
        /// </summary>
        private readonly int? _maxConcurrency;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AsyncPipe{TIn, TOut}"/> and
        /// configures it to process the specifies items.
        /// </summary>
        /// <param name="consumable">The consumable that will provide the items to be processed.</param>
        /// <param name="converter">The function to be called for each item.</param>
        /// <param name="maxConcurrency">The maximum number of items to process in parallel.</param>
        public AsyncPipe( IAsyncConsumable<TIn> consumable, Func<TIn, Task<TOut>> converter, int? maxConcurrency = null )
        {
            _consumable = consumable;
            _converter = converter;
            _maxConcurrency = maxConcurrency;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public async Task RunAsync( CancellationToken cancellationToken = default )
        {
            var tasks = new List<Task>();

            var consumer = new AsyncConsumer<TIn>( _consumable, async item =>
            {
                await _producer.EnqueueAsync( await _converter( item ), cancellationToken );
            }, _maxConcurrency );

            if ( _consumable is IAsyncRunnable runnable )
            {
                tasks.Add( runnable.RunAsync( cancellationToken ) );
            }

            tasks.Add( consumer.RunAsync( cancellationToken ) );

            await Task.WhenAll( tasks );

            _producer.Complete();
        }

        /// <inheritdoc/>
        public Task<TOut> DequeueAsync( CancellationToken cancellationToken = default )
        {
            return _producer.DequeueAsync( cancellationToken );
        }

        #endregion
    }
}
