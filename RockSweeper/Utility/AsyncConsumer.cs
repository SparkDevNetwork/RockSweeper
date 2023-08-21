using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Consumes a series of items from a consumable on one or more background
    /// tasks.
    /// </summary>
    /// <typeparam name="T">The type of item to be consumed.</typeparam>
    public class AsyncConsumer<T> : IAsyncRunnable
    {
        #region Fields

        /// <summary>
        /// The consumable that will provide the items to be processed.
        /// </summary>
        private readonly IAsyncConsumable<T> _consumable;

        /// <summary>
        /// The function that will process the items.
        /// </summary>
        private readonly Func<T, Task> _processor;

        /// <summary>
        /// The maximum number of items to process in parallel.
        /// </summary>
        private readonly int? _maxConcurrency;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AsyncConsumer{T}"/> and configures
        /// it to process the specified items.
        /// </summary>
        /// <param name="consumable">The consumable that will provide the items.</param>
        /// <param name="processor">The function that will process the items.</param>
        /// <param name="maxConcurrency">The maximum number of items to process in parallel.</param>
        public AsyncConsumer( IAsyncConsumable<T> consumable, Func<T, Task> processor, int? maxConcurrency = null )
        {
            _consumable = consumable;
            _processor = processor;
            _maxConcurrency = maxConcurrency;
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public async Task RunAsync( CancellationToken cancellationToken = default )
        {
            var concurrency = _maxConcurrency ?? 4;
            var tasks = new List<Task>();

            for ( int i = 0; i < concurrency; i++ )
            {
                tasks.Add( Task.Run( async () =>
                {
                    while ( true )
                    {
                        T item;

                        try
                        {
                            item = await _consumable.DequeueAsync( cancellationToken );
                        }
                        catch ( TaskCanceledException )
                        {
                            return;
                        }

                        await _processor( item );
                    }
                } ) );
            }

            if ( _consumable is IAsyncRunnable runnable )
            {
                await runnable.RunAsync( cancellationToken );
            }

            await Task.WhenAll( tasks );
        }

        #endregion
    }
}
