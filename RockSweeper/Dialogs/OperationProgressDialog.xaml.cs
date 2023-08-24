using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using RockSweeper.Utility;

namespace RockSweeper.Dialogs
{
    /// <summary>
    /// Interaction logic for OperationProgressDialog.xaml
    /// </summary>
    public partial class OperationProgressDialog : Window
    {
        private readonly SweeperConfiguration _configuration;
        private readonly CancellationTokenSource _operationCancellationTokenSource = new CancellationTokenSource();
        private readonly ViewModel _viewModel = new ViewModel();
        private readonly Stopwatch _lineStopwatch = new Stopwatch();
        private int _lineQueryStartCount = 0;
        private Guid? _currentOperationId = null;

        public OperationProgressDialog( SweeperConfiguration configuration )
        {
            _configuration = configuration;

            DataContext = _viewModel;

            InitializeComponent();

            Task.Run( ExecuteOnTask );
        }

        private async Task ExecuteOnTask()
        {
            var sweeper = new SweeperController( _configuration.ConnectionString );

            sweeper.ProgressChanged += Sweeper_ProgressChanged;
            sweeper.OperationStarted += Sweeper_OperationStarted;
            sweeper.OperationCompleted += Sweeper_OperationCompleted;

            sweeper.CancellationToken = _operationCancellationTokenSource.Token;

            var orderedOptions = _configuration.Options.OrderBy( o => o.RunAfterActions.Count )
                .TopologicalSort( ( o ) =>
                {
                    return _configuration.Options.Where( oo => o.RunAfterActions.Contains( oo.Id ) );
                } )
                .ToList();

            Dispatcher.Invoke( () =>
            {
                orderedOptions
                    .Select( o => new ProgressLine( o.Id, o.Title ) )
                    .ToList()
                    .ForEach( p => _viewModel.ProgressLines.Add( p ) );

                _viewModel.CanCancel = true;
            } );

            var timeUpdateTimer = new Timer( _ =>
            {
                Dispatcher.Invoke( () =>
                {
                    var operationId = _currentOperationId;

                    if ( operationId == null )
                    {
                        return;
                    }

                    var progressLine = _viewModel.ProgressLines.Single( p => p.OptionId == operationId );

                    progressLine.Duration = _lineStopwatch.Elapsed.ToString( "hh\\:mm\\:ss" );
                } );
            }, null, 100, 100 );

            try
            {
                await sweeper.ExecuteAsync( orderedOptions );
            }
            catch ( Exception ex )
            {
                timeUpdateTimer.Dispose();
                sweeper.Dispose();

                Dispatcher.Invoke( () =>
                {
                    _viewModel.CanCancel = false;

                    Exception e = ex;
                    while ( e.InnerException != null )
                    {
                        e = e.InnerException;
                    }

                    var progressLine = _viewModel.ProgressLines
                        .FirstOrDefault( p => p.State == ProgressLineState.Processing );

                    if ( progressLine != null )
                    {
                        progressLine.State = ProgressLineState.Failed;
                        progressLine.Tooltip = $"Error: {ex.Message}";
                    }

                    var stackTrace = new EnhancedStackTrace( e ).ToString();

                    MessageBox.Show( this, $"{e.Message}\n\n{stackTrace}", "Error while processing" );
                } );

                return;
            }

            timeUpdateTimer.Dispose();

            Dispatcher.Invoke( () =>
            {
                _viewModel.CanCancel = false;
                MessageBox.Show( this, $"Finished processing database.\nExecuted {sweeper.SqlQueryCount:N0} queries.", "Completed" );
            } );

            sweeper.Dispose();
        }

        private void Sweeper_OperationStarted( object sender, ProgressEventArgs e )
        {
            _lineStopwatch.Restart();
            _lineQueryStartCount = ( ( SweeperController ) sender ).SqlQueryCount;

            Dispatcher.InvokeAsync( () =>
            {
                _currentOperationId = e.OperationId;

                var progressLine = _viewModel.ProgressLines.Single( p => p.OptionId == e.OperationId );

                progressLine.State = ProgressLineState.Processing;
                progressLine.Tooltip = "Running";
                progressLine.Progress = e.Progress * 100;
                progressLine.Message = e.Message;

                dgProgress.ScrollIntoView( progressLine );
            } );
        }

        private void Sweeper_ProgressChanged( object sender, ProgressEventArgs e )
        {
            Dispatcher.InvokeAsync( () =>
            {
                var progressLine = _viewModel.ProgressLines.Single( p => p.OptionId == e.OperationId );

                progressLine.Message = e.Message;

                if ( e.Progress.HasValue )
                {
                    progressLine.Progress = e.Progress.Value * 100;

                    var index = _viewModel.ProgressLines.IndexOf( progressLine );
                    _viewModel.Progress = ( index + e.Progress ?? 0 ) / _viewModel.ProgressLines.Count;
                }
            } );
        }

        private void Sweeper_OperationCompleted( object sender, ProgressEventArgs e )
        {
            var duration = _lineStopwatch.Elapsed.ToString( "hh\\:mm\\:ss" );
            var tooltip = $"Completed\nQuery Count: {( ( ( SweeperController ) sender ).SqlQueryCount - _lineQueryStartCount ):N0}";

            Dispatcher.InvokeAsync( () =>
            {
                var progressLine = _viewModel.ProgressLines.Single( p => p.OptionId == e.OperationId );

                progressLine.Progress = null;
                progressLine.State = ProgressLineState.Completed;
                progressLine.Message = string.Empty;
                progressLine.Duration = duration;
                progressLine.Tooltip = tooltip;

                var index = _viewModel.ProgressLines.IndexOf( progressLine );

                _viewModel.Progress = ( index + 1 ) / ( double ) _viewModel.ProgressLines.Count;
                _currentOperationId = null;
            } );
        }

        /// <summary>
        /// Handles the Click event of the Stop control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void Stop_Click( object sender, RoutedEventArgs e )
        {
            _operationCancellationTokenSource.Cancel();
            _viewModel.CanCancel = false;
        }

        private class ViewModel : INotifyPropertyChanged
        {
            private double _progress;

            private string _statusMessage;

            private bool _canCancel;

            public event PropertyChangedEventHandler PropertyChanged;

            public double Progress
            {
                get => _progress;
                set
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }

            public string StatusMessage
            {
                get => _statusMessage;
                set
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }

            public bool CanCancel
            {
                get => _canCancel;
                set
                {
                    _canCancel = value;
                    OnPropertyChanged();
                }
            }

            /// <summary>
            /// Gets or sets the progress lines.
            /// </summary>
            /// <value>
            /// The progress lines.
            /// </value>
            public ObservableCollection<ProgressLine> ProgressLines { get; } = new ObservableCollection<ProgressLine>();

            private void OnPropertyChanged( [CallerMemberName] string propertyName = null )
            {
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }
    }
}
