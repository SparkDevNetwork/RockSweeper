using System.Threading;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Describes a consumable queue that operates asyncronously.
    /// </summary>
    /// <typeparam name="T">The type of data in the consumable.</typeparam>
    public interface IAsyncConsumable<T>
    {
        /// <summary>
        /// Attempts to dequeue the next item from the queue. If one is not
        /// available it will wait until one becomes available. If the consumable
        /// becomes empty and will not get any new items then an exception of
        /// type <see cref="TaskCanceledException"/> will be thrown.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that aborts the operation early.</param>
        /// <returns>A task that represents the operation.</returns>
        Task<T> DequeueAsync( CancellationToken cancellationToken = default );
    }
}
