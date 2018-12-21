using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace RockSweeper
{
    public class SweeperController
    {
        #region Properties

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
        protected List<T> SqlScalarList<T>( string sql )
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
            return SqlScalar<int?>( $"SELECT [Id] FROM [EntityType] WHERE [Name] = '{ entityType.Replace( "'", "''" ) }'" );
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

        #endregion

        #region System Settings

        /// <summary>
        /// Sanitizes the application roots.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void SanitizeApplicationRoots( SweeperActionData actionData )
        {
            SetGlobalAttributeValue( "InternalApplicationRoot", "rock.example.org" );
            SetGlobalAttributeValue( "PublicApplicationRoot", "www.example.org" );
        }

        /// <summary>
        /// Disables the communication transports.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableCommunicationTransports( SweeperActionData actionData )
        {
            DisableComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Resets the existing communication transport configuration attribute values.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetCommunicationTransports( SweeperActionData actionData )
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Communication.TransportComponent" );
        }

        /// <summary>
        /// Configures Rock to use localhost SMTP email delivery.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ConfigureForLocalhostSmtp( SweeperActionData actionData )
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
        public void DisableFinancialGateways( SweeperActionData actionData )
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
        public void ResetFinancialGateways( SweeperActionData actionData )
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
        public void DisableExternalAuthenticationServices( SweeperActionData actionData )
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
        public void ResetExternalAuthenticationServices( SweeperActionData actionData )
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
        public void DisableAuthenticationServices( SweeperActionData actionData )
        {
            DisableComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Resets the authentication services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetAuthenticationServices( SweeperActionData actionData )
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );
        }

        /// <summary>
        /// Disables the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableLocationServices( SweeperActionData actionData )
        {
            DisableComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Resets the location services.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetLocationServices( SweeperActionData actionData )
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Address.VerificationComponent" );
        }

        /// <summary>
        /// Disables the external storage providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableExternalStorageProviders( SweeperActionData actionData )
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
        public void ResetExternalStorageProviders( SweeperActionData actionData )
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
        public void DisableBackgroundCheckProviders( SweeperActionData actionData )
        {
            DisableComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Resets the background check providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetBackgroundCheckProviders( SweeperActionData actionData )
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.BackgroundCheckComponent" );
        }

        /// <summary>
        /// Disables the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableSignatureDocumentProviders( SweeperActionData actionData )
        {
            DisableComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
        }

        /// <summary>
        /// Resets the signature document providers.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetSignatureDocumentProviders( SweeperActionData actionData )
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
        }

        /// <summary>
        /// Disables the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisablePhoneSystems( SweeperActionData actionData )
        {
            DisableComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        /// <summary>
        /// Resets the phone systems.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ResetPhoneSystems( SweeperActionData actionData )
        {
            DeleteAttributeValuesForComponentsOfType( "Rock.Pbx.PbxComponent" );
        }

        #endregion

        #region Rock Jobs

        /// <summary>
        /// Disables the rock jobs except the Job Pulse job.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableRockJobs( SweeperActionData actionData )
        {
            SqlCommand( $"UPDATE [ServiceJob] SET [IsActive] = 0 WHERE [Guid] != 'CB24FF2A-5AD3-4976-883F-DAF4EFC1D7C7'" );
        }

        #endregion

        #region Storage

        /// <summary>
        /// Moves all the binary files into database.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void MoveBinaryFilesIntoDatabase( SweeperActionData actionData )
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );

            var files = SqlQuery<int, Guid, string>( $"SELECT [Id],[Guid],[FileName] FROM [BinaryFile] WHERE [StorageEntityTypeId] != { databaseEntityTypeId }" );
            double fileCount = files.Count;

            for ( int i = 0; i < files.Count; i++ )
            {
                int fileId = files[i].Item1;
                Guid fileGuid = files[i].Item2;
                string fileName = files[i].Item3;

                actionData.CancellationToken?.ThrowIfCancellationRequested();

                actionData.ProgressCallback?.Invoke( i / fileCount );

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
        public void ReplaceDatabaseImagesWithSizedPlaceholders( SweeperActionData actionData )
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
                actionData.CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    actionData.ProgressCallback?.Invoke( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database images with empty placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseImagesWithEmptyPlaceholders( SweeperActionData actionData )
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
                actionData.CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    actionData.ProgressCallback?.Invoke( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database documents with sized placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseDocumentsWithSizedPlaceholders( SweeperActionData actionData )
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
                actionData.CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    actionData.ProgressCallback?.Invoke( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database documents with empty placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void ReplaceDatabaseDocumentsWithEmptyPlaceholders( SweeperActionData actionData )
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
                actionData.CancellationToken?.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    actionData.ProgressCallback?.Invoke( completedCount / fileCount );
                }
            }
        }

        #endregion
    }

    public class SweeperActionData
    {
        public Action<double> ProgressCallback { get; set; }

        public CancellationToken? CancellationToken { get; set; }
    }
}
