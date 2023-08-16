using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions
{
    /// <summary>
    /// Base class for all sweeper actions to inherit from.
    /// </summary>
    public abstract class SweeperAction : IDisposable
    {
        /// <summary>
        /// <c>true</c> if we have been disposed.
        /// </summary>
        private bool _disposedValue;

        /// <summary>
        /// The stopwatch that is used to calculate timing for progress updates.
        /// </summary>
        private Stopwatch _stopwatch;

        /// <summary>
        /// The sweeper that handles all the low level logic.
        /// </summary>
        public SweeperController Sweeper { get; set; }

        /// <summary>
        /// Progresses the specified percentage.
        /// </summary>
        /// <param name="actionId">The identifier of the action that is updating it's progress.</param>
        /// <param name="percentage">The percentage value, from 0.0 to 1.0.</param>
        /// <param name="step">The step.</param>
        /// <param name="stepCount">The step count.</param>
        protected void Progress( double percentage, int? step = null, int? stepCount = null )
        {
            if ( _stopwatch == null )
            {
                _stopwatch = Stopwatch.StartNew();
            }
            else if ( _stopwatch.Elapsed.TotalMilliseconds < 1000 / 60.0 )
            {
                // Only update progress at roughly 60fps.
                return;
            }

            Sweeper.Progress( GetActionId(), percentage, step, stepCount );
            _stopwatch.Restart();
        }

        /// <summary>
        /// Gets the identifier of this action.
        /// </summary>
        /// <returns></returns>
        protected Guid GetActionId()
        {
            return GetType().GetCustomAttribute<ActionIdAttribute>().Id;
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        public abstract Task ExecuteAsync();

        /// <summary>
        /// Performs application-defined tasks associated with freeing,
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this is called in response to a <c>Dispose()</c> call.</param>
        protected virtual void Dispose( bool disposing )
        {
            if ( !_disposedValue )
            {
                if ( disposing )
                {
                    Sweeper = null;
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose( disposing: true );
            GC.SuppressFinalize( this );
        }
    }
}
