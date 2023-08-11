using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

using RockSweeper.Dialogs;
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
                NotifyPropertyChanged();
                NotifyPropertyChanged( nameof( CanStart ) );
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
            }
        }
        private string _rockWebFolder;

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
                if ( !string.IsNullOrWhiteSpace( SqlDatabaseName ) )
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets or sets the configuration options.
        /// </summary>
        /// <value>
        /// The configuration options.
        /// </value>
        public ObservableCollection<SweeperOption> ConfigOptions { get; } = new ObservableCollection<SweeperOption>();

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
                .OrderBy( o => o.Category == "Data Scrubbing" )
                .ThenBy( o => o.Category )
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
        protected void NotifyPropertyChanged( [CallerMemberName] string propertyName = null )
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
                bool hasLocationServices = !string.IsNullOrWhiteSpace( Properties.Settings.Default.HereAppCode ) && !string.IsNullOrWhiteSpace( Properties.Settings.Default.HereAppId ) && !string.IsNullOrWhiteSpace( Properties.Settings.Default.TargetGeoCenter );

                option.Enabled = hasDatabase;
                option.Enabled = option.Enabled && ( !option.RequiresRockWeb || hasRockWeb );
                option.Enabled = option.Enabled && ( !option.RequiresLocationServices || hasLocationServices );
            }
        }

        #endregion

        #region Event Handlers

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

            ConnectionString = dialog.GetConnectionString();

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
            var operation = new OperationProgressDialog( new SweeperConfiguration
            {
                Options = ConfigOptions.Where( o => o.Enabled && o.Selected ).ToList(),
                ConnectionString = ConnectionString,
                RockWebFolder = RockWebFolder
            } );

            operation.ShowDialog();
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
