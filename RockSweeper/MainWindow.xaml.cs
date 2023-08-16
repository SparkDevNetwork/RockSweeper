using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

using RockSweeper.Attributes;
using RockSweeper.Dialogs;
using RockSweeper.SweeperActions;
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
        public ObservableCollection<SweeperOption> ConfigOptions { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Initialize all the possible operations.
            var operations = GetType()
                .Assembly
                .GetTypes()
                .Where( t => typeof( SweeperAction ).IsAssignableFrom( t ) && !t.IsAbstract )
                .Select( t => new SweeperOption( t ) )
                .OrderBy( t => t.Category )
                .ThenBy( t => t.Title )
                .ToList();

            // Make sure all conflicts go both ways.
            foreach ( var operation in operations )
            {
                var conflictingWithMe = operations
                    .Where( a => a.ConflictingActions.Contains( operation.Id ) )
                    .Select( a => a.Id )
                    .ToList();

                if ( conflictingWithMe.Any() )
                {
                    operation.AddConflicts( conflictingWithMe );
                }

                operation.PropertyChanged += Option_PropertyChanged;
            }

            // Initialize all UI options.
            ConfigOptions = new ObservableCollection<SweeperOption>( operations );

            UpdateConflictingStates();
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

                option.Enabled = !option.Conflicted && hasDatabase;
                option.Enabled = option.Enabled && ( !option.RequiresRockWeb || hasRockWeb );
                option.Enabled = option.Enabled && ( !option.RequiresLocationServices || hasLocationServices );
            }
        }

        protected void UpdateConflictingStates()
        {
            var conflictingIds = ConfigOptions.Where( o => o.Selected )
                .SelectMany( o => o.ConflictingActions )
                .ToList();

            foreach ( var option in ConfigOptions )
            {
                option.Conflicted = conflictingIds.Contains( option.Id );
            }

            // If anything is in a selected and conflicted state, just reset it all.
            if ( ConfigOptions.Any( o => o.Selected && o.Conflicted ) )
            {
                foreach ( var option in ConfigOptions )
                {
                    option.Conflicted = false;
                    option.Selected = false;
                }
            }

            UpdateOptionStates();
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
                if ( option.Enabled && !option.Conflicted )
                {
                    option.Selected = true;
                }
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

        /// <summary>
        /// Handles the PropertyChanged event of the option.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void Option_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            if ( e.PropertyName == nameof( SweeperOption.Selected ) )
            {
                UpdateConflictingStates();
            }
        }

        #endregion
    }
}
