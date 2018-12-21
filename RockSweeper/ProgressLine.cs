using System.ComponentModel;

namespace RockSweeper
{
    /// <summary>
    /// Defines a single progress line.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class ProgressLine : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the foreground.
        /// </summary>
        /// <value>
        /// The foreground.
        /// </value>
        public System.Windows.Media.Brush Foreground
        {
            get
            {
                switch ( State )
                {
                    case ProgressLineState.Pending:
                        return System.Windows.Media.Brushes.LightGray;

                    case ProgressLineState.Processing:
                        return System.Windows.Media.Brushes.Black;

                    case ProgressLineState.Completed:
                        return System.Windows.Media.Brushes.Green;

                    case ProgressLineState.Failed:
                        return System.Windows.Media.Brushes.Red;

                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title
        {
            get
            {
                var title = _title;

                if ( _progress.HasValue )
                {
                    title += string.Format( " {0:0.00}%", _progress );
                }

                return title;
            }
            set
            {
                _title = value;
                NotifyPropertyChanged( "Title" );
            }
        }
        private string _title;

        /// <summary>
        /// Gets the icon.
        /// </summary>
        /// <value>
        /// The icon.
        /// </value>
        public FontAwesome.WPF.FontAwesomeIcon Icon
        {
            get
            {
                switch ( State )
                {
                    case ProgressLineState.Pending:
                    case ProgressLineState.Processing:
                        return FontAwesome.WPF.FontAwesomeIcon.Cog;

                    case ProgressLineState.Completed:
                        return FontAwesome.WPF.FontAwesomeIcon.Check;

                    case ProgressLineState.Failed:
                        return FontAwesome.WPF.FontAwesomeIcon.Times;

                    default:
                        return FontAwesome.WPF.FontAwesomeIcon.None;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether icon is spinning.
        /// </summary>
        /// <value>
        ///   <c>true</c> if icon is spinning; otherwise, <c>false</c>.
        /// </value>
        public bool IsSpinning
        {
            get
            {
                switch ( State )
                {
                    case ProgressLineState.Processing:
                        return true;

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public ProgressLineState State
        {
            get => _state;
            set
            {
                _state = value;
                NotifyPropertyChanged( "State" );
                NotifyPropertyChanged( "Foreground" );
                NotifyPropertyChanged( "Icon" );
                NotifyPropertyChanged( "IsSpinning" );
            }
        }
        ProgressLineState _state;

        /// <summary>
        /// Gets or sets the progress.
        /// </summary>
        /// <value>
        /// The progress.
        /// </value>
        public double? Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                NotifyPropertyChanged( "Title" );
            }
        }
        private double? _progress;

        #endregion

        #region Methods

        /// <summary>
        /// Notifies the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void NotifyPropertyChanged( string propertyName )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        #endregion
    }

    public enum ProgressLineState
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}
