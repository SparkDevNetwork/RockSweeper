using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RockSweeper.Dialogs
{
    /// <summary>
    /// Dialog to ask the user to configure a SQL connection string.
    /// </summary>
    public partial class SqlConnectionDialog : Window, INotifyPropertyChanged
    {
        #region Fields

        protected const string WindowsAuthentication = "Windows";

        protected const string SqlServerAuthentication = "SQL Server Authentication";

        private bool useConnectionString = true;

        private bool useParameters;

        private string serverName;

        private string databaseName;

        private string authentication;

        private string userName;

        private string connectionString;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value that determines if we are using a typed
        /// in connection string.
        /// </summary>
        /// <value>
        /// <c>true</c> if the dialog will construct the connection string from parameters.
        /// </value>
        public bool UseConnectionString
        {
            get => useConnectionString;
            set
            {
                useConnectionString = value;
                OnPropertyChanged();
                NotifyIsValid();
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if we are using individual
        /// SQL parameters when constructing the connection string.
        /// </summary>
        /// <value>
        /// <c>true</c> if the dialog will construct the connection string from parameters.
        /// </value>
        public bool UseParameters
        {
            get => useParameters;
            set
            {
                useParameters = value;
                OnPropertyChanged();
                NotifyIsValid();
            }
        }

        /// <summary>
        /// Gets or sets the name of the server.
        /// </summary>
        /// <value>
        /// The name of the server.
        /// </value>
        public string ServerName
        {
            get
            {
                return serverName;
            }
            set
            {
                serverName = value;
                NotifyIsValid();
            }
        }

        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public string DatabaseName
        {
            get
            {
                return databaseName;
            }
            set
            {
                databaseName = value;
                NotifyIsValid();
            }
        }

        /// <summary>
        /// Gets or sets the authentication.
        /// </summary>
        /// <value>
        /// The authentication.
        /// </value>
        public string Authentication
        {
            get
            {
                return authentication;
            }
            set
            {
                authentication = value;
                NotifyIsValid();
            }
        }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName
        {
            get
            {
                return userName;
            }
            set
            {
                userName = value;
                NotifyIsValid();
            }
        }

        /// <summary>
        /// Gets or sets the server names found by the SQL browser.
        /// </summary>
        /// <value>
        /// The server names found by the SQL browser.
        /// </value>
        public string[] ServerNames { set; get; }

        /// <summary>
        /// Gets the allowed authentication modes.
        /// </summary>
        /// <value>
        /// The allowed authentication modes.
        /// </value>
        public string[] AuthenticationModes { get; } = { WindowsAuthentication, SqlServerAuthentication };

        /// <summary>
        /// Returns true if the user input is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => Validate();

        /// <summary>
        /// Gets a value indicating whether this instance has credential input enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has credential input enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCredentialInputEnabled => Authentication == SqlServerAuthentication;

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString
        {
            get => connectionString;
            set
            {
                connectionString = value;
                OnPropertyChanged();
                NotifyIsValid();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionDialog"/> class.
        /// </summary>
        public SqlConnectionDialog()
        {
            InitializeComponent();
            InitDataContext();
            SetupControls();

            Authentication = WindowsAuthentication;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the data context.
        /// </summary>
        protected virtual void InitDataContext()
        {
            DataContext = this;
        }

        /// <summary>
        /// Setups the controls.
        /// </summary>
        protected virtual void SetupControls()
        {
            ServerNameComboBox.Focus();
            PasswordBox.PasswordChanged += ( sender, args ) =>
            {
                OnPropertyChanged( nameof( IsValid ) );
            };
        }

        /// <summary>
        /// Notifies the the various UI controls that various properties have changed.
        /// </summary>
        protected virtual void NotifyIsValid()
        {
            OnPropertyChanged( nameof( IsValid ) );
            OnPropertyChanged( nameof( IsCredentialInputEnabled ) );
        }

        /// <summary>
        /// Validates the user provided values.
        /// </summary>
        /// <returns>true if the values are correct.</returns>
        protected virtual bool Validate()
        {
            if ( UseConnectionString )
            {
                if ( string.IsNullOrWhiteSpace( ConnectionString ) )
                {
                    return false;
                }

                return true;
            }

            if ( string.IsNullOrWhiteSpace( ServerName ) )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( DatabaseName ) )
            {
                return false;
            }

            if ( Authentication == SqlServerAuthentication )
            {
                if ( string.IsNullOrWhiteSpace( UserName ) || string.IsNullOrWhiteSpace( PasswordBox.Password ) )
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called when the named property changes.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        /// <summary>
        /// Gets the connection string that represents the current state of
        /// the dialog.
        /// </summary>
        /// <returns>A SQL connection string or an emptys tring.</returns>
        public string GetConnectionString()
        {
            if ( !IsValid )
            {
                return string.Empty;
            }

            if ( UseConnectionString )
            {
                return ConnectionString;
            }

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = ServerName,
                InitialCatalog = DatabaseName
            };

            if ( Authentication == WindowsAuthentication )
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = UserName;
                builder.Password = PasswordBox.Password;
            }

            return builder.ConnectionString;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the DropDownOpened event of the ServerName control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ServerName_DropDownOpened( object sender, EventArgs e )
        {
            //
            // If this is the first time the user has opened the drop down, try to enumerate
            // the servers found in the browser.
            //
            if ( ServerNames == null )
            {
                Cursor = Cursors.Wait;

                try
                {
                    var enumerator = SqlDataSourceEnumerator.Instance;
                    var results = enumerator.GetDataSources();
                    var servers = new List<string>();

                    foreach ( DataRow r in results.Rows )
                    {
                        servers.Add( r["ServerName"].ToString() );
                    }

                    ServerNames = servers.ToArray();
                    OnPropertyChanged( "ServerNames" );
                }
                finally
                {
                    Cursor = null;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Test control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Test_Click( object sender, RoutedEventArgs e )
        {
            try
            {
                Cursor = Cursors.Wait;

                using ( var connection = new SqlConnection( GetConnectionString() ) )
                {
                    connection.Open();
                    connection.Close();
                }

                MessageBox.Show( this, "Connection succeeded" );
            }
            catch ( Exception ex )
            {
                MessageBox.Show( this, ex.Message, "Connection failed" );
            }
            finally
            {
                Cursor = null;
            }
        }

        /// <summary>
        /// Handles the Click event of the Ok control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Ok_Click( object sender, RoutedEventArgs e )
        {
            DialogResult = true;
        }

        #endregion
    }
}
