using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    /// <summary>
    /// A simple AutoResetEvent that can be used with tasks.
    /// </summary>
    public class AsyncAutoResetEvent
    {
        #region Fields

        /// <summary>
        /// The tasks that are currently waiting for signals.
        /// </summary>
        private readonly LinkedList<TaskCompletionSource<bool>> _waiters = new LinkedList<TaskCompletionSource<bool>>();

        /// <summary>
        /// Determines if we are currently in a signaled state.
        /// </summary>
        private bool _isSignaled;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AsyncAutoResetEvent"/>.
        /// </summary>
        /// <param name="signaled">The initial signaled state.</param>
        public AsyncAutoResetEvent( bool signaled )
        {
            _isSignaled = signaled;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Waits until a signal is received.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait for a signal.</param>
        /// <param name="cancellationToken">A token that can abort the wait early.</param>
        /// <returns><c>true</c> if a signal was received; otherwise <c>false</c>.</returns>
        public async Task<bool> WaitAsync( TimeSpan timeout, CancellationToken cancellationToken )
        {
            TaskCompletionSource<bool> tcs;

            lock ( _waiters )
            {
                if ( _isSignaled )
                {
                    _isSignaled = false;
                    return true;
                }
                else if ( timeout == TimeSpan.Zero )
                {
                    return _isSignaled;
                }
                else
                {
                    tcs = new TaskCompletionSource<bool>();
                    _waiters.AddLast( tcs );
                }
            }

            Task winner = await Task.WhenAny( tcs.Task, Task.Delay( timeout, cancellationToken ) );
            if ( winner == tcs.Task )
            {
                // The task was signaled.
                return true;
            }
            else
            {
                // We timed-out; remove our reference to the task.
                // This is an O(n) operation since waiters is a LinkedList<T>.
                lock ( _waiters )
                {
                    bool removed = _waiters.Remove( tcs );
                    Debug.Assert( removed );
                    return false;
                }
            }
        }

        /// <summary>
        /// Set the signal.
        /// </summary>
        public void Set()
        {
            lock ( _waiters )
            {
                if ( _waiters.Count > 0 )
                {
                    // Signal the first task in the waiters list. This must be done on a new
                    // thread to avoid stack-dives and situations where we try to complete the
                    // same result multiple times.
                    TaskCompletionSource<bool> tcs = _waiters.First.Value;
                    Task.Run( () => tcs.SetResult( true ) );
                    _waiters.RemoveFirst();
                }
                else if ( !_isSignaled )
                {
                    // No tasks are pending
                    _isSignaled = true;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Signaled: {_isSignaled}, Waiters: {_waiters.Count}";
        }

        #endregion
    }
}
