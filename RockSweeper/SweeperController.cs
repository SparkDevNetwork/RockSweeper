using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace RockSweeper
{
    public class SweeperController
    {
        #region Properties

        public Action<string> ProgressCallback { get; set; }

        public CancellationToken? CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets the database connection.
        /// </summary>
        /// <value>
        /// The database connection.
        /// </value>
        protected SqlConnection Connection { get; private set; }

        /// <summary>
        /// Gets or sets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        public SqlTransaction Transaction { get; private set; }

        /// <summary>
        /// Gets the rock web folder path.
        /// </summary>
        /// <value>
        /// The rock web folder path.
        /// </value>
        protected string RockWeb { get; private set; }

        /// <summary>
        /// Gets the Rock domain.
        /// </summary>
        /// <value>
        /// The Rock domain.
        /// </value>
        public RockDomain Domain { get; private set; }

        /// <summary>
        /// Gets the URL to be used when requesting files from Rock.
        /// </summary>
        /// <value>
        /// The URL to be used when requesting files from Rock.
        /// </value>
        protected string GetFileUrl { get; private set; }

        protected Dictionary<string, string> EmailMap { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SweeperController"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="rockWeb">The rock web.</param>
        public SweeperController( string connectionString, string rockWeb )
        {
            Connection = new SqlConnection( connectionString );
            Domain = new RockDomain( rockWeb );
            RockWeb = rockWeb;

            Connection.Open();
            //Transaction = Connection.BeginTransaction();

            var internalApplicationRoot = SqlScalar<string>( "SELECT [DefaultValue] FROM [Attribute] WHERE [Key] = 'InternalApplicationRoot' AND [EntityTypeId] IS NULL" );
            GetFileUrl = $"{ internalApplicationRoot }GetFile.ashx";
            GetFileUrl = "http://localhost:64706/GetFile.ashx";

            EmailMap = new Dictionary<string, string>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Progresses the specified percentage.
        /// </summary>
        /// <param name="percentage">The percentage value, from 0.0 to 1.0.</param>
        /// <param name="step">The step.</param>
        /// <param name="stepCount">The step count.</param>
        public void Progress( double percentage, int? step = null, int? stepCount = null )
        {
            if ( step.HasValue && stepCount.HasValue )
            {
                ProgressCallback?.Invoke( string.Format( "{0:0.00}% (Step {1} of {2})", percentage * 100, step, stepCount ) );
            }
            else if ( step.HasValue )
            {
                ProgressCallback?.Invoke( string.Format( "{0:0.00}% (Step {1})", percentage * 100, step ) );
            }
            else
            {
                ProgressCallback?.Invoke( string.Format( "{0:0.00}%", percentage * 100 ) );
            }
        }

        #endregion

        #region SQL Methods

        /// <summary>
        /// Executes a SQL scalar statement and returns the value.
        /// </summary>
        /// <typeparam name="T">The expected value type to be returned.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The value that resulted from the statement.</returns>
        protected T SqlScalar<T>( string sql )
        {
            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                return (T)command.ExecuteScalar();
            }
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows of a single column data.
        /// </summary>
        /// <typeparam name="T">The type of the return values.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        protected List<T> SqlQuery<T>( string sql )
        {
            var list = new List<T>();

            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var c1 = reader.IsDBNull( 0 ) ? default( T ) : (T)reader[0];

                        list.Add( c1 );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows of data.
        /// </summary>
        /// <typeparam name="T1">The type of the return values in the first column.</typeparam>
        /// <typeparam name="T2">The type of the return values in the second column.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        protected List<Tuple<T1, T2>> SqlQuery<T1, T2>( string sql )
        {
            var list = new List<Tuple<T1, T2>>();

            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : (T1)reader[0];
                        var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : (T2)reader[1];

                        list.Add( new Tuple<T1, T2>( c1, c2 ) );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows of data.
        /// </summary>
        /// <typeparam name="T1">The type of the return values in the first column.</typeparam>
        /// <typeparam name="T2">The type of the return values in the second column.</typeparam>
        /// <typeparam name="T3">The type of the return values in the third column.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        protected List<Tuple<T1, T2, T3>> SqlQuery<T1, T2, T3>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3>>();

            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : (T1)reader[0];
                        var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : (T2)reader[1];
                        var c3 = reader.IsDBNull( 2 ) ? default( T3 ) : (T3)reader[2];

                        list.Add( new Tuple<T1, T2, T3>( c1, c2, c3 ) );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows of data.
        /// </summary>
        /// <typeparam name="T1">The type of the return values in the first column.</typeparam>
        /// <typeparam name="T2">The type of the return values in the second column.</typeparam>
        /// <typeparam name="T3">The type of the return values in the third column.</typeparam>
        /// <typeparam name="T4">The type of the return values in the fourth column.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        protected List<Tuple<T1, T2, T3, T4>> SqlQuery<T1, T2, T3, T4>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3, T4>>();

            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : (T1)reader[0];
                        var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : (T2)reader[1];
                        var c3 = reader.IsDBNull( 2 ) ? default( T3 ) : (T3)reader[2];
                        var c4 = reader.IsDBNull( 3 ) ? default( T4 ) : (T4)reader[3];

                        list.Add( new Tuple<T1, T2, T3, T4>( c1, c2, c3, c4 ) );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows of data.
        /// </summary>
        /// <typeparam name="T1">The type of the return values in the first column.</typeparam>
        /// <typeparam name="T2">The type of the return values in the second column.</typeparam>
        /// <typeparam name="T3">The type of the return values in the third column.</typeparam>
        /// <typeparam name="T4">The type of the return values in the fourth column.</typeparam>
        /// <typeparam name="T5">The type of the return values in the fifth column.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        protected List<Tuple<T1, T2, T3, T4, T5>> SqlQuery<T1, T2, T3, T4, T5>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3, T4, T5>>();

            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : (T1)reader[0];
                        var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : (T2)reader[1];
                        var c3 = reader.IsDBNull( 2 ) ? default( T3 ) : (T3)reader[2];
                        var c4 = reader.IsDBNull( 3 ) ? default( T4 ) : (T4)reader[3];
                        var c5 = reader.IsDBNull( 4 ) ? default( T5 ) : (T5)reader[4];

                        list.Add( new Tuple<T1, T2, T3, T4, T5>( c1, c2, c3, c4, c5 ) );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        protected List<Dictionary<string, object>> SqlQuery( string sql )
        {
            var list = new List<Dictionary<string, object>>();

            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        var dictionary = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++ )
                        {
                            dictionary.Add( reader.GetName( i ), reader.IsDBNull( i ) ? null : reader[i] );
                        }

                        list.Add( dictionary );
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Executes a non-query SQL command.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The number of rows affected.</returns>
        protected int SqlCommand( string sql )
        {
            return SqlCommand( sql, null );
        }

        /// <summary>
        /// Executes a non-query SQL command.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The number of rows affected.</returns>
        protected int SqlCommand( string sql, Dictionary<string, object> parameters )
        {
            using ( var command = Connection.CreateCommand() )
            {
                command.Transaction = Transaction;
                command.CommandText = sql;

                if ( parameters != null )
                {
                    foreach ( var p in parameters )
                    {
                        command.Parameters.AddWithValue( p.Key, p.Value );
                    }
                }

                return command.ExecuteNonQuery();
            }
        }

        #endregion

        #region Rock Helper Methods

        /// <summary>
        /// Gets the entity type identifier.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        protected int? GetEntityTypeId( string entityType )
        {
            return SqlScalar<int?>( $"SELECT [Id] FROM [EntityType] WHERE [Name] = '{ entityType }'" );
        }

        /// <summary>
        /// Gets the field type identifier.
        /// </summary>
        /// <param name="fieldType">Type of the field.</param>
        /// <returns></returns>
        protected int? GetFieldTypeId( string fieldType )
        {
            return SqlScalar<int?>( $"SELECT [Id] FROM [FieldType] WHERE [Class] = '{ fieldType }'" );
        }

        /// <summary>
        /// Disables a single component with the given class name.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        protected void DisableComponentType( string componentType )
        {
            var entityTypeId = GetEntityTypeId( componentType );

            SqlCommand( $@"UPDATE AV
SET AV.[Value] = 'False'
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE AV.EntityId = 0
  AND A.[EntityTypeId] = { entityTypeId.Value }
  AND A.[Key] = 'Active'" );
        }

        /// <summary>
        /// Deletes the attribute values for component.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        protected void DeleteAttributeValuesForComponentType( string componentType )
        {
            var entityTypeId = GetEntityTypeId( componentType );

            SqlCommand( $@"DELETE AV
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE AV.EntityId = 0
  AND A.[EntityTypeId] = { entityTypeId }" );
        }

        /// <summary>
        /// Disables all the individual components of the given parent type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <param name="excludedTypes">The types to be excluded.</param>
        protected void DisableComponentsOfType( string componentType, string[] excludedTypes = null )
        {
            var types = Domain.FindTypes( componentType ).Where( t => excludedTypes == null || !excludedTypes.Contains( t ) );

            foreach ( var type in types )
            {
                DisableComponentType( type );
            }
        }

        /// <summary>
        /// Deletes the attribute values for the child components of the given component type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <param name="excludedTypes">The types to be excluded.</param>
        protected void DeleteAttributeValuesForComponentsOfType( string componentType, string[] excludedTypes = null )
        {
            var types = Domain.FindTypes( componentType ).Where( t => excludedTypes == null || !excludedTypes.Contains( t ) );

            foreach ( var type in types )
            {
                DeleteAttributeValuesForComponentType( type );
            }
        }

        /// <summary>
        /// Sets the global attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        protected void SetGlobalAttributeValue( string key, string value )
        {
            SqlCommand( $"UPDATE [Attribute] SET [DefaultValue] = '{ value.Replace( "'", "''" ) }' WHERE [Key] = '{ key.Replace( "'", "''" ) }' AND [EntityTypeId] IS NULL" );
        }

        /// <summary>
        /// Sets the component attribute value by either updating the existing value or creating a new one.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="value">The value.</param>
        protected void SetComponentAttributeValue( string entityType, string attributeKey, string value )
        {
            SqlCommand( $@"DECLARE @AttributeId int = (SELECT A.[Id] FROM [Attribute] AS A INNER JOIN [EntityType] AS ET ON ET.[Id] = A.[EntityTypeId] WHERE ET.[Name] = '{ entityType }' AND A.[Key] = '{ attributeKey }')
IF EXISTS (SELECT * FROM [AttributeValue] WHERE [AttributeId] = @AttributeId)
	UPDATE [AttributeValue] SET [Value] = '{ value }' WHERE [AttributeId] = @AttributeId AND [EntityId] = 0
ELSE
	INSERT INTO [AttributeValue] ([IsSystem], [AttributeId], [EntityId], [Value], [Guid]) VALUES (0, @AttributeId, 0, '{ value }', NEWID())" );
        }

        /// <summary>
        /// Gets the file data from rock.
        /// </summary>
        /// <param name="binaryFileId">The binary file identifier.</param>
        /// <returns></returns>
        protected MemoryStream GetFileDataFromRock( int binaryFileId )
        {
            var url = $"{ GetFileUrl }?Id={ binaryFileId }";
            var client = new WebClient();

            try
            {
                var ms = new MemoryStream();

                using ( var stream = client.OpenRead( url ) )
                {
                    stream.CopyTo( ms );
                }

                ms.Seek( 0, SeekOrigin.Begin );

                return ms;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the file data from rock.
        /// </summary>
        /// <param name="binaryFileGuid">The binary file identifier.</param>
        /// <returns></returns>
        protected MemoryStream GetFileDataFromRock( Guid binaryFileGuid )
        {
            var url = $"{ GetFileUrl }?Guid={ binaryFileGuid }";
            var client = new WebClient();

            try
            {
                var ms = new MemoryStream();

                using ( var stream = client.OpenRead( url ) )
                {
                    stream.CopyTo( ms );
                }

                ms.Seek( 0, SeekOrigin.Begin );

                return ms;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether filename is an image.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>
        ///   <c>true</c> if filename is an image; otherwise, <c>false</c>.
        /// </returns>
        protected bool IsFileNameImage( string filename )
        {
            return filename.EndsWith( ".jpg", StringComparison.CurrentCultureIgnoreCase ) ||
                filename.EndsWith( ".jpeg", StringComparison.CurrentCultureIgnoreCase ) ||
                filename.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase );
        }

        /// <summary>
        /// Generates the fake email address for address.
        /// </summary>
        /// <param name="originalEmail">The original email.</param>
        /// <returns></returns>
        protected string GenerateFakeEmailAddressForAddress( string originalEmail )
        {
            if ( EmailMap.ContainsKey( originalEmail.ToLower() ) )
            {
                return EmailMap[originalEmail.ToLower()];
            }

            string email;

            if ( originalEmail.Contains( "@" ) )
            {
                email = $"user{ EmailMap.Count + 1 }@fakeinbox.com";
            }
            else
            {
                email = $"user{ EmailMap.Count + 1 }";
            }

            EmailMap.Add( originalEmail.ToLower(), email );

            return email;
        }

        /// <summary>
        /// Scrubs the specified table column with the given replacement data.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="replacement">The replacement function to provide the new value.</param>
        /// <param name="step">The step number.</param>
        /// <param name="stepCount">The step count.</param>
        protected void ScrubTableTextColumn( string tableName, string columnName, Func<string, string> replacement, int? step, int? stepCount )
        {
            ScrubTableTextColumns( tableName, new[] { columnName }, replacement, step, stepCount );
        }

        /// <summary>
        /// Scrubs the specified table columns with the given replacement data.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="replacement">The replacement function to provide the new value.</param>
        /// <param name="step">The step number.</param>
        /// <param name="stepCount">The step count.</param>
        protected void ScrubTableTextColumns( string tableName, IEnumerable<string> columnNames, Func<string, string> replacement, int? step, int? stepCount )
        {
            string columns = string.Join( "], [", columnNames );
            var rows = SqlQuery( $"SELECT [Id], [{ string.Join( "], [", columnNames ) }] FROM [{ tableName }]" );

            for ( int i = 0; i < rows.Count; i++ )
            {
                int valueId = (int)rows[i]["Id"];
                var updatedValues = new Dictionary<string, object>();

                foreach ( var c in columnNames )
                {
                    var value = (string)rows[i][c];

                    if ( !string.IsNullOrWhiteSpace( value ) )
                    {
                        var newValue = replacement( value );

                        if ( value != newValue )
                        {
                            updatedValues.Add( c, newValue );
                        }
                    }
                }

                if ( updatedValues.Any() )
                {
                    var updateStrings = updatedValues.Keys.Select( k => $"[{ k }] = @{ k }" );
                    SqlCommand( $"UPDATE [{ tableName }] SET { string.Join( ", ", updateStrings ) } WHERE [Id] = { valueId }", updatedValues );
                }

                Progress( i / (double)rows.Count, step, stepCount );
            }
        }

        #endregion

        #region System Settings

        /// <summary>
        /// Sanitizes the application roots.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void SanitizeApplicationRoots()
        {
            SetGlobalAttributeValue( "InternalApplicationRoot", "rock.example.org" );
            SetGlobalAttributeValue( "PublicApplicationRoot", "www.example.org" );
        }

        /// <summary>
        /// Disables the communication transports.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableCommunicationTransports()
        {
            DisableComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Resets the existing communication transport configuration attribute values.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetCommunicationTransports()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Configures Rock to use localhost SMTP email delivery.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ConfigureForLocalhostSmtp()
        {
            //
            // Setup the Email medium.
            //
            SetComponentAttributeValue( "Rock.Communication.Medium.Email", "Active", "True" );
            SetComponentAttributeValue( "Rock.Communication.Medium.Email", "TransportContainer", "1fef44b2-8685-4001-be5b-8a059bc65430" );

            //
            // Set SMTP Transport to Active.
            //
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Active", "True" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Server", "localhost" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Port", "25" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "UserName", "" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Password", "" );
            SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "UseSSL", "False" );
        }

        /// <summary>
        /// Disables the financial gateways.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableFinancialGateways()
        {
            SqlCommand( $@"UPDATE FG
SET FG.[IsActive] = 0
FROM[FinancialGateway] AS FG
INNER JOIN[EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE ET.[Name] != 'Rock.Financial.TestGateway'" );
        }

        /// <summary>
        /// Resets the financial gateway configuration attributes.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetFinancialGateways()
        {
            int? entityTypeId = GetEntityTypeId( "Rock.Model.FinancialGateway" );

            SqlCommand( $@"DELETE AV
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
INNER JOIN [FinancialGateway] AS FG ON FG.[Id] = AV.[EntityId]
INNER JOIN [EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE A.[EntityTypeId] = { entityTypeId.Value } AND ET.[Name] != 'Rock.Financial.TestGateway'" );
        }

        /// <summary>
        /// Disables the external authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableExternalAuthenticationServices()
        {
            DisableComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Resets the external authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetExternalAuthenticationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Disables the authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableAuthenticationServices()
        {
            DisableComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Resets the authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetAuthenticationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Disables the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableLocationServices()
        {
            DisableComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Resets the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetLocationServices()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Disables the external storage providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableExternalStorageProviders()
        {
            DisableComponentsOfType( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );
        }

        /// <summary>
        /// Resets the external storage providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetExternalStorageProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );
        }

        /// <summary>
        /// Disables the background check providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableBackgroundCheckProviders()
        {
            DisableComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Resets the background check providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetBackgroundCheckProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Disables the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableSignatureDocumentProviders()
        {
            DisableComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
        }

        /// <summary>
        /// Resets the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetSignatureDocumentProviders()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
            SetGlobalAttributeValue( "SignNowAccessToken", string.Empty );
        }

        /// <summary>
        /// Disables the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisablePhoneSystems()
        {
            DisableComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetPhoneSystems()
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the google API keys.
        /// </summary>
        public void ResetGoogleApiKeys()
        {
            SetGlobalAttributeValue( "GoogleAPIKey", string.Empty );
            SetGlobalAttributeValue( "core_GoogleReCaptchaSiteKey", string.Empty );
        }

        #endregion

        #region Rock Jobs

        /// <summary>
        /// Disables the rock jobs except the Job Pulse job.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableRockJobs()
        {
            SqlCommand( $"UPDATE [ServiceJob] SET [IsActive] = 0 WHERE [Guid] != 'CB24FF2A-5AD3-4976-883F-DAF4EFC1D7C7'" );
        }

        #endregion

        #region Storage

        /// <summary>
        /// Moves all the binary files into database.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void MoveBinaryFilesIntoDatabase()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );

            var files = SqlQuery<int, Guid, string>( $"SELECT [Id],[Guid],[FileName] FROM [BinaryFile] WHERE [StorageEntityTypeId] != { databaseEntityTypeId }" );
            double fileCount = files.Count;

            for ( int i = 0; i < files.Count; i++ )
            {
                int fileId = files[i].Item1;
                Guid fileGuid = files[i].Item2;
                string fileName = files[i].Item3;

                CancellationToken?.ThrowIfCancellationRequested();

                Progress( i / fileCount );

                using ( var ms = GetFileDataFromRock( fileGuid ) )
                {
                    string path = IsFileNameImage( fileName ) ? $"~/GetImage.ashx?Guid={ fileGuid }" : $"~/GetFile.ashx?Guid={ fileGuid }";

                    SqlCommand( $"UPDATE [BinaryFile] SET [StorageEntityTypeId] = @EntityTypeId, [StorageEntitySettings] = NULL, [Path] = @Path WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "EntityTypeId", databaseEntityTypeId },
                        { "Path", path }
                    } );

                    SqlCommand( $"DELETE FROM [BinaryFileData] WHERE [Id] = { fileId }" );
                    SqlCommand( $"INSERT INTO [BinaryFileData] ([Id], [Content], [Guid]) VALUES ({ fileId }, @Content, NEWID())", new Dictionary<string, object>
                    {
                        { "Content", ms }
                    } );
                }
            }
        }

        /// <summary>
        /// Replaces the database images with correctly sized placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseImagesWithSizedPlaceholders()
        {
            var tasks = new List<Thread>();
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],[Width],[Height] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?, int?, int?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                int width;
                int height;

                //
                // Determine if we already have the image size or if we need to calculate it.
                //
                if ( file.Item4.HasValue && file.Item4.Value > 0 && file.Item5.HasValue && file.Item5.Value > 0 )
                {
                    width = file.Item4.Value;
                    height = file.Item5.Value;
                }
                else
                {
                    using ( var ms = GetFileDataFromRock( fileId ) )
                    {
                        try
                        {
                            var image = new Bitmap( ms );

                            width = image.Width;
                            height = image.Height;
                        }
                        catch
                        {
                            width = 100;
                            height = 100;
                        }
                    }
                }

                using ( var imageStream = new MemoryStream() )
                {
                    //
                    // Generate the new image.
                    //
                    try
                    {
                        var image = new Bitmap( width, height );
                        var g = Graphics.FromImage( image );
                        var font = new Font( "Tahoma", height / 10 );
                        var sizeText = $"{ width }x{ height }";

                        g.FillRectangle( Brushes.White, new Rectangle( 0, 0, width, height ) );
                        var size = g.MeasureString( sizeText, font );
                        g.DrawString( sizeText, font, Brushes.Black, new PointF( ( width - size.Width ) / 2, ( height - size.Height ) / 2 ) );
                        g.Flush();

                        image.SetResolution( 72, 72 );

                        if ( filename.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            image.Save( imageStream, System.Drawing.Imaging.ImageFormat.Png );
                        }
                        else
                        {
                            var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                                .Where( c => c.MimeType == "image/jpeg" )
                                .First();

                            var encoderParameters = new System.Drawing.Imaging.EncoderParameters( 1 );
                            encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter( System.Drawing.Imaging.Encoder.Quality, 50L );

                            image.Save( imageStream, encoder, encoderParameters );
                        }

                        imageStream.Position = 0;
                    }
                    catch
                    {
                        imageStream.Position = 0;
                        imageStream.SetLength( 0 );
                    }

                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", imageStream.Length );
                    }

                    if ( file.Item4.HasValue )
                    {
                        sets.Add( "[Width] = @Width" );
                        parameters.Add( "Width", width );
                    }

                    if ( file.Item5.HasValue )
                    {
                        sets.Add( "[Height] = @Height" );
                        parameters.Add( "Height", height );
                    }

                    if ( sets.Any() )
                    {
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", imageStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database images with empty placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseImagesWithEmptyPlaceholders()
        {
            var tasks = new List<Thread>();
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],[Width],[Height] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?, int?, int?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                int width = 1;
                int height = 1;

                using ( var imageStream = new MemoryStream() )
                {
                    //
                    // Generate the new image.
                    //
                    try
                    {
                        var image = new Bitmap( width, height );

                        image.SetResolution( 72, 72 );

                        if ( filename.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase ) )
                        {
                            image.Save( imageStream, System.Drawing.Imaging.ImageFormat.Png );
                        }
                        else
                        {
                            var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                                .Where( c => c.MimeType == "image/jpeg" )
                                .First();

                            var encoderParameters = new System.Drawing.Imaging.EncoderParameters( 1 );
                            encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter( System.Drawing.Imaging.Encoder.Quality, 50L );

                            image.Save( imageStream, encoder, encoderParameters );
                        }

                        imageStream.Position = 0;
                    }
                    catch
                    {
                        imageStream.Position = 0;
                        imageStream.SetLength( 0 );
                    }

                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", imageStream.Length );
                    }

                    if ( file.Item4.HasValue )
                    {
                        sets.Add( "[Width] = @Width" );
                        parameters.Add( "Width", width );
                    }

                    if ( file.Item5.HasValue )
                    {
                        sets.Add( "[Height] = @Height" );
                        parameters.Add( "Height", height );
                    }

                    if ( sets.Any() )
                    {
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", imageStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database documents with sized placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseDocumentsWithSizedPlaceholders()
        {
            var tasks = new List<Thread>();
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?>( $"SELECT [Id],[FileName],[FileSize] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => !IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                long fileSize;

                //
                // Determine if we already have the file size or if we need to calculate it.
                //
                if ( file.Item3.HasValue && file.Item3.Value > 0 )
                {
                    fileSize = file.Item3.Value;
                }
                else
                {
                    using ( var ms = GetFileDataFromRock( fileId ) )
                    {
                        fileSize = ms.Length;
                    }
                }

                using ( var fileStream = new MemoryStream() )
                {
                    byte[] data = new byte[4096];

                    for (int i = 0; i < data.Length; i++ )
                    {
                        data[i] = (byte)'X';
                    }

                    while ( fileStream.Length < fileSize )
                    {
                        var len = Math.Min( data.Length, fileSize - fileStream.Length );

                        fileStream.Write( data, 0, (int)len );
                    }

                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", fileStream.Length );
                    }

                    if ( sets.Any() )
                    {
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", fileStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database documents with empty placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseDocumentsWithEmptyPlaceholders()
        {
            var tasks = new List<Thread>();
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?>( $"SELECT [Id],[FileName],[FileSize] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => !IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;

                using ( var fileStream = new MemoryStream() )
                {
                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", fileStream.Length );
                    }

                    if ( sets.Any() )
                    {
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", fileStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        #endregion

        #region General

        /// <summary>
        /// Disables the SSL requirement for sites and pages.
        /// </summary>
        public void DisableSslForSitesAndPages()
        {
            SqlCommand( "UPDATE [Site] SET [RequiresEncryption] = 0" );
            SqlCommand( "UPDATE [Page] SET [RequiresEncryption] = 0" );
        }

        #endregion

        #region Data Scrubbing

        /// <summary>
        /// Generates the random email addresses.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void GenerateRandomEmailAddresses()
        {
            var emailRegex = new Regex( "^\\w+@([a-zA-Z_]+?\\.)+?[a-zA-Z]{2,}$" );
            var tableContent = new Dictionary<string, string[]>
            {
                { "BenevolenceRequest", new[] { "Email", "RequestText", "ResultSummary" } },
                { "Communication", new[] { "FromEmail", "ReplyToEmail", "CCEmails", "BCCEmails", "Message" } },
                { "CommunicationTemplate", new[] { "FromEmail", "ReplyToEmail", "CCEmails", "BCCEmails" } },
                { "EventItemOccurrence", new[] { "ContactEmail" } },
                { "PrayerRequest", new[] { "Email" } },
                { "Registration", new[] { "ConfirmationEmail" } },
                { "RegistrationTemplate", new[] { "ConfirmationFromEmail", "ReminderFromEmail", "PaymentReminderFromEmail", "WaitListTransitionFromEmail" } },
                { "ServiceJob", new[] { "NotificationEmails" } },
                { "Note", new[] { "Text" } },
                { "HtmlContent", new[] { "Content" } }
            };
            int stepCount = 3 + tableContent.Count - 1;

            string replaceEmail( string value )
            {
                return emailRegex.Replace( value, ( match ) =>
                {
                    return GenerateFakeEmailAddressForAddress( match.Value );
                } );
            }

            //
            // Stage 1: Replace all Person e-mail addresses.
            //
            var peopleAddresses = SqlQuery<int, string>( "SELECT [Id], [Email] FROM [Person] WHERE [Email] IS NOT NULL AND [Email] != ''" );
            for ( int i = 0; i < peopleAddresses.Count; i++ )
            {
                int personId = peopleAddresses[i].Item1;
                string email = GenerateFakeEmailAddressForAddress( peopleAddresses[i].Item2 );

                SqlCommand( $"UPDATE [Person] SET [Email] = '{ email }' WHERE [Id] = { personId }" );

                Progress( i / (double)peopleAddresses.Count, 1, stepCount );
            }

            //
            // Stage 2: Replace all AttributeValue e-mail addresses.
            //
            var fieldTypeIds = new List<int>
            {
                GetFieldTypeId( "Rock.Field.Types.TextFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.EmailFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.CodeEditorFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.HtmlFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.MarkdownFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.MemoFieldType" ).Value
            };

            var attributeValues = SqlQuery<int, string>( $"SELECT AV.[Id], AV.[Value] FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] IN ({ string.Join( ",", fieldTypeIds.Select( i => i.ToString() ) ) }) AND AV.[Value] LIKE '%@%'" );
            for ( int i = 0; i < attributeValues.Count; i++ )
            {
                int valueId = attributeValues[i].Item1;
                string value = attributeValues[i].Item2;

                var newValue = replaceEmail( value );

                if ( value != newValue )
                {
                    SqlCommand( $"UPDATE [AttributeValue] SET [Value] = @Value WHERE [Id] = { valueId }", new Dictionary<string, object>
                    {
                        { "Value", newValue }
                    } );
                }

                Progress( i / (double)attributeValues.Count, 2, stepCount );
            }

            //
            // Stage 3: Scrub the Email Exception List global attribute.
            //
            var emailExceptionListValue = SqlScalar<string>( "SELECT [DefaultValue] FROM [Attribute] WHERE [Key] = 'EmailExceptionsList' AND [EntityTypeId] IS NULL" );
            SetGlobalAttributeValue( "EmailExceptionsList", replaceEmail( emailExceptionListValue ) );

            //
            // Stage 3: Scan and replace e-mail addresses in misc data.
            //
            int tableStep = 0;
            foreach ( var tc in tableContent )
            {
                ScrubTableTextColumns( tc.Key, tc.Value, replaceEmail, 3 + tableStep, stepCount );
                tableStep++;
            }
        }

        // TODO: Scrub attribute values.
        // TODO: Prune Analytics Tables

        #endregion

        // TODO: Organization Name, Organization Address, Organization Email, Organization Website, Organization Phone

    }
}
