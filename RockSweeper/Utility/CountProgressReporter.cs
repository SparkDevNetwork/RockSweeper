using System;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Simple progress reporter that handles progress over a counted set.
    /// </summary>
    public class CountProgressReporter
    {
        #region Fields

        /// <summary>
        /// The function to call to report progress.
        /// </summary>
        private readonly Action<double> _callback;

        /// <summary>
        /// The total count of things to be processed.
        /// </summary>
        private readonly int _totalCount;

        /// <summary>
        /// The current count of things that have been processed.
        /// </summary>
        private int _currentCount;

        /// <summary>
        /// A lock object to ensure consistency across threads.
        /// </summary>
        private readonly object _lockObject = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="CountProgressReporter"/>.
        /// </summary>
        /// <param name="totalCount">The total number of items to be processed.</param>
        /// <param name="callback">The function to call with updated progress as a value between 0 and 1.</param>
        public CountProgressReporter( int totalCount, Action<double> callback )
        {
            _totalCount = totalCount;
            _callback = callback;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds to the number of items that have been processed.
        /// </summary>
        /// <param name="count">The additional number of items.</param>
        public void Add( int count )
        {
            lock( _lockObject )
            {
                _currentCount += count;

                _callback( _currentCount / ( double ) _totalCount );
            }
        }

        #endregion
    }
}
