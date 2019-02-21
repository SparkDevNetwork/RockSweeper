using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Windows;
using RockSweeper.Utility;

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

        /// <summary>
        /// Gets or sets the sweeper.
        /// </summary>
        /// <value>
        /// The sweeper.
        /// </value>
        protected SweeperController Sweeper { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        protected RockDomain Domain { get; set; }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        protected string ConnectionString { get; set; }

        /// <summary>
        /// Gets the name of the SQL database.
        /// </summary>
        /// <value>
        /// The name of the SQL database.
        /// </value>
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
        private string _sqlDatabaseName;

        /// <summary>
        /// Gets the rock web folder.
        /// </summary>
        /// <value>
        /// The rock web folder.
        /// </value>
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
        private string _rockWebFolder;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
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
                NotifyPropertyChanged( "CanStop" );
            }
        }
        private bool _isRunning;

        /// <summary>
        /// Gets a value indicating whether this instance can configure.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can configure; otherwise, <c>false</c>.
        /// </value>
        public bool CanConfigure
        {
            get
            {
                return !IsRunning;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can start.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can start; otherwise, <c>false</c>.
        /// </value>
        public bool CanStart
        {
            get
            {
                if ( !string.IsNullOrWhiteSpace( SqlDatabaseName ) && !IsRunning )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can stop.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can stop; otherwise, <c>false</c>.
        /// </value>
        public bool CanStop
        {
            get
            {
                return CancellationTokenSource != null;
            }
        }

        /// <summary>
        /// Gets or sets the status bar text.
        /// </summary>
        /// <value>
        /// The status bar text.
        /// </value>
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

        /// <summary>
        /// Gets or sets the enabled options.
        /// </summary>
        /// <value>
        /// The enabled options.
        /// </value>
        public List<SweeperOption> EnabledOptions { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        /// <value>
        /// The cancellation token source.
        /// </value>
        protected CancellationTokenSource CancellationTokenSource { get; set; }

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
                .ThenBy( o => ( int ) o.Action )
                .ToList()
                .ForEach( o => ConfigOptions.Add( o ) );
            UpdateOptionStates();

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
        /// Updates the option states.
        /// </summary>
        protected void UpdateOptionStates()
        {

            foreach ( var option in ConfigOptions )
            {
                bool hasDatabase = !string.IsNullOrWhiteSpace( SqlDatabaseName );
                bool hasRockWeb = !string.IsNullOrWhiteSpace( RockWebFolder );
                bool hasLocationServices = !string.IsNullOrWhiteSpace( Properties.Settings.Default.HereAppCode ) && !string.IsNullOrWhiteSpace( Properties.Settings.Default.HereAppId );

                option.Enabled = hasDatabase;
                option.Enabled = option.Enabled && ( !option.RequiresRockWeb || hasRockWeb );
                option.Enabled = option.Enabled && ( !option.RequiresLocationServices || hasLocationServices );
            }
        }

        /// <summary>
        /// Handles sweeping the database in a background thread.
        /// </summary>
        /// <exception cref="Exception">Unknown sweep type</exception>
        protected void ThreadSweep()
        {
            SweeperOption option = null;
            ProgressLine progressLine = null;

            Sweeper.ProgressCallback = delegate ( string text )
            {
                StatusBarText = $"{ option.Title } { text }";
            };
            Sweeper.CancellationToken = CancellationTokenSource.Token;

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

                    CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    Dispatcher.Invoke( () =>
                    {
                        progressLine.Progress = null;
                        progressLine.State = ProgressLineState.Completed;
                    } );
                }
                catch ( Exception e )
                {
                    Sweeper.Dispose();
                    Sweeper = null;
                    CancellationTokenSource = null;
                    IsRunning = false;

                    Dispatcher.Invoke( () =>
                    {
                        progressLine.State = ProgressLineState.Failed;
                        Exception ex = e;
                        while ( ex.InnerException != null )
                        {
                            ex = ex.InnerException;
                        }
                        MessageBox.Show( this, ex.Message, "Error while processing" );
                    } );

                    StatusBarText = string.Empty;

                    return;
                }
            }

            StatusBarText = string.Empty;

            Sweeper.Dispose();
            Sweeper = null;
            CancellationTokenSource = null;
            IsRunning = false;

            Dispatcher.Invoke( () =>
            {
                MessageBox.Show( this, "Finished processing database.", "Completed" );
            } );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        protected void Window_Closing( object sender, CancelEventArgs e )
        {
            if ( IsRunning )
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the Preferences control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void Preferences_Click( object sender, RoutedEventArgs e )
        {
            new Dialogs.PreferencesDialog().ShowDialog();

            UpdateOptionStates();
        }

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

            UpdateOptionStates();
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

            UpdateOptionStates();
        }

        /// <summary>
        /// Handles the Click event of the Start control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void Start_Click( object sender, RoutedEventArgs e )
        {
            Sweeper = new SweeperController( ConnectionString, RockWebFolder );

            var options = ConfigOptions.Where( o => o.Enabled && o.Selected ).ToList();
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

            CancellationTokenSource = new CancellationTokenSource();
            IsRunning = true;

            new Thread( ThreadSweep ).Start();
        }

        /// <summary>
        /// Handles the Click event of the Stop control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        protected void Stop_Click( object sender, RoutedEventArgs e )
        {
            CancellationTokenSource.Cancel();
            IsRunning = false;
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
                option.Selected = true;
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
                option.Selected = false;
            }
        }

        #endregion
    }
}
