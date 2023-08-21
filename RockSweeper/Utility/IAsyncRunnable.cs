using System.Threading;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Describes an object that can run some operation on a background task
    /// and then wait until it has completed.
    /// </summary>
    public interface IAsyncRunnable
    {
        /// <summary>
        /// Runs the consumable and waits until it has finished processing all
        /// the items in the queue.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RunAsync( CancellationToken cancellationToken = default );
    }
}
