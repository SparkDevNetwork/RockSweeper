using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Windows;

namespace RockSweeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        protected SweeperController Sweeper { get; set; }

        protected RockDomain Domain { get; set; }

        protected string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=RockObs;Integrated Security=True";

        public string SqlDatabaseName
        {
            get => _sqlDatabaseName;
            private set
            {
                _sqlDatabaseName = value;
                NotifyPropertyChanged( "SqlDatabaseName" );
                NotifyPropertyChanged( "CanStart" );
            }
        }
        private string _sqlDatabaseName = "localhost\\RockObs";

        public string RockWebFolder
        {
            get => _rockWebFolder;
            private set
            {
                _rockWebFolder = value;
                NotifyPropertyChanged( "RockWebFolder" );
                NotifyPropertyChanged( "CanStart" );
            }
        }
        private string _rockWebFolder = "C:\\Users\\Daniel Hazelbaker\\Desktop\\Rockit\\RockWeb";

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                _isRunning = value;
                NotifyPropertyChanged( "IsRunning" );
                NotifyPropertyChanged( "CanStart" );
                NotifyPropertyChanged( "CanConfigure" );
            }
        }
        private bool _isRunning;

        public bool CanConfigure
        {
            get
            {
                return !IsRunning;
            }
        }

        public bool CanStart
        {
            get
            {
                if (!string.IsNullOrWhiteSpace( SqlDatabaseName ) && !string.IsNullOrWhiteSpace( RockWebFolder ) && !IsRunning )
                {
                    return true;
                }

                return false;
            }
        }

        public string StatusBarText
        {
            get => _statusBarText;
            set
            {
                _statusBarText = value;
                NotifyPropertyChanged( "StatusBarText" );
            }
        }
        private string _statusBarText;

        /// <summary>
        /// Gets or sets the progress lines.
        /// </summary>
        /// <value>
        /// The progress lines.
        /// </value>
        public ObservableCollection<ProgressLine> ProgressLines { get; } = new ObservableCollection<ProgressLine>();

        /// <summary>
        /// Gets or sets the configuration options.
        /// </summary>
        /// <value>
        /// The configuration options.
        /// </value>
        public ObservableCollection<SweeperOption> ConfigOptions { get; } = new ObservableCollection<SweeperOption>();

        public List<SweeperOption> EnabledOptions { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //
            // Initialize all the possible options.
            //
            Enum.GetValues( typeof( SweeperAction ) )
                .Cast<SweeperAction>()
                .Select( a => new SweeperOption( a ) )
                .OrderBy( o => o.Category )
                .ThenBy( o => (int)o.Action )
                .ToList()
                .ForEach( o => ConfigOptions.Add( o ) );

            DataContext = this;
        }

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

        /// <summary>
        /// Handles sweeping the database in a background thread.
        /// </summary>
        /// <exception cref="Exception">Unknown sweep type</exception>
        protected void ThreadSweep()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            SweeperOption option = null;
            ProgressLine progressLine = null;

            Sweeper.ProgressCallback = delegate ( string text )
            {
                StatusBarText = $"{ option.Title } { text }";
            };
            Sweeper.CancellationToken = cancellationTokenSource.Token;

            for ( int i = 0; i < EnabledOptions.Count; i++ )
            {
                option = EnabledOptions[i];
                progressLine = ProgressLines[i];

                try
                {
                    Dispatcher.Invoke( () =>
                    {
                        progressLine.State = ProgressLineState.Processing;
                        dgProgress.ScrollIntoView( progressLine );
                        StatusBarText = option.Title;
                    } );

                    var methodInfo = Sweeper.GetType().GetMethod( option.MethodName );

                    if ( methodInfo == null )
                    {
                        throw new Exception( $"Unknown sweeper method named '{ option.MethodName }'" );
                    }

                    methodInfo.Invoke( Sweeper, new object[] { } );

                    Dispatcher.Invoke( () =>
                    {
                        progressLine.Progress = null;
                        progressLine.State = ProgressLineState.Completed;
                    } );
                }
                catch ( Exception e )
                {
                    Dispatcher.Invoke( () =>
                    {
                        progressLine.State = ProgressLineState.Failed;
                        MessageBox.Show( this, e.Message, "Error while processing" );
                    } );

                    return;
                }
            }

            StatusBarText = string.Empty;

            Dispatcher.Invoke( () =>
            {
                MessageBox.Show( this, "Finished processing database.", "Completed" );
            } );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the OpenDatabase control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void OpenDatabase_Click( object sender, RoutedEventArgs e )
        {
            var dialog = new Dialogs.SqlConnectionDialog();
            if ( !dialog.ShowDialog().GetValueOrDefault() )
            {
                return;
            }

            ConnectionString = dialog.ConnectionString;

            var builder = new SqlConnectionStringBuilder( ConnectionString );
            SqlDatabaseName = $"{ builder.DataSource }\\{ builder.InitialCatalog }";
        }

        /// <summary>
        /// Handles the Click event of the SelectRockFolder control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void SelectRockFolder_Click( object sender, RoutedEventArgs e )
        {
            var browser = new WPFFolderBrowser.WPFFolderBrowserDialog( "RockWeb Folder" );
            if ( !browser.ShowDialog().GetValueOrDefault() )
            {
                return;
            }

            RockWebFolder = browser.FileName;
        }

        /// <summary>
        /// Handles the Click event of the Start control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void Start_Click( object sender, RoutedEventArgs e )
        {
            Sweeper = new SweeperController( ConnectionString, RockWebFolder );

            var options = ConfigOptions.Where( o => o.Enabled ).ToList();
            EnabledOptions = options.OrderBy( o => o.RunAfterActions.Count )
                .TopologicalSort( ( o ) =>
                {
                    return options.Where( oo => o.RunAfterActions.Contains( oo.Action ) );
                } )
                .ToList();

            ProgressLines.Clear();
            EnabledOptions
                .Select( o => new ProgressLine
                {
                    Title = o.Title,
                    State = ProgressLineState.Pending
                } )
                .ToList()
                .ForEach( p => ProgressLines.Add( p ) );

            dgOptions.Visibility = Visibility.Hidden;
            dgProgress.Visibility = Visibility.Visible;

            IsRunning = true;
            new Thread( ThreadSweep ).Start();
        }

        /// <summary>
        /// Handles the Click event of the SelectAll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void SelectAll_Click( object sender, RoutedEventArgs e )
        {
            foreach ( var option in ConfigOptions )
            {
                option.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the SelectNone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void SelectNone_Click( object sender, RoutedEventArgs e )
        {
            foreach ( var option in ConfigOptions )
            {
                option.Enabled = false;
            }
        }

        #endregion
    }
}
