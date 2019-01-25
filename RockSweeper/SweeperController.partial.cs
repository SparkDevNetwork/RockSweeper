using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using RockSweeper.Utility;
using HereRestApi = RockSweeper.External.HereRestApi;

namespace RockSweeper
{
    public partial class SweeperController : IDisposable
    {
        #region Properties

        public Action<string> ProgressCallback { get; set; }

        public CancellationToken? CancellationToken { get; set; }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <value>
        /// The database connection string.
        /// </value>
        protected string ConnectionString { get; private set; }

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

        /// <summary>
        /// Gets the map of original e-mail addresses to new scrubbed e-mail addresses.
        /// </summary>
        /// <value>
        /// The map of original e-mail addresses to new scrubbed e-mail addresses.
        /// </value>
        protected Dictionary<string, string> EmailMap { get; private set; }

        /// <summary>
        /// Gets the phone map.
        /// </summary>
        /// <value>
        /// The phone map.
        /// </value>
        protected Dictionary<string, string> PhoneMap { get; private set; }

        /// <summary>
        /// Gets the map of original login names to new scrubbed login names.
        /// </summary>
        /// <value>
        /// The map of original login names to new scrubbed login names.
        /// </value>
        protected Dictionary<string, string> LoginMap { get; private set; }

        /// <summary>
        /// Gets the faker object that will help generate fake data.
        /// </summary>
        /// <value>
        /// The faker object that will help generate fake data.
        /// </value>
        protected Bogus.Faker DataFaker { get; private set; }

        /// <summary>
        /// Gets the geo lookup cache.
        /// </summary>
        /// <value>
        /// The geo lookup cache.
        /// </value>
        protected Dictionary<string, Address> GeoLookupCache { get; private set; }

        /// <summary>
        /// Gets the geo lookup count.
        /// </summary>
        /// <value>
        /// The geo lookup count.
        /// </value>
        protected int GeoLookupCount { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SweeperController"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="rockWeb">The rock web.</param>
        public SweeperController( string connectionString, string rockWeb )
        {
            ConnectionString = connectionString;
            Domain = new RockDomain( rockWeb );
            RockWeb = rockWeb;

            var internalApplicationRoot = GetGlobalAttributeValue( "InternalApplicationRoot" );
            GetFileUrl = $"{ internalApplicationRoot }GetFile.ashx";

            EmailMap = new Dictionary<string, string>();
            PhoneMap = new Dictionary<string, string>();
            LoginMap = new Dictionary<string, string>();

            GeoLookupCache = Support.LoadGeocodeCache();

            SetupDataFaker();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Support.SaveGeocodeCache( GeoLookupCache );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Setups the data faker.
        /// </summary>
        protected virtual void SetupDataFaker()
        {
            var res = Bogus.ResourceHelper.ReadResource( GetType().Assembly, "RockSweeper.Resources.en_rock.locale.json" );
            var json = System.Text.Encoding.UTF8.GetString( res );
            var jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject( json );

            using ( var ms = new MemoryStream() )
            {
                using ( var writer = new Newtonsoft.Json.Bson.BsonDataWriter( ms ) )
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    serializer.Serialize( writer, jsonObj );
                }

                var bson = Bogus.Bson.Bson.Load( ms.ToArray() );

                //
                // Use the BORK language as a hack since we can't add a language that doesn't exist.
                //
                Bogus.Database.Data.Value.TryAdd( "en_BORK", bson );
            }

            DataFaker = new Bogus.Faker( "en_BORK" );
        }

        /// <summary>
        /// Progresses the specified percentage.
        /// </summary>
        /// <param name="percentage">The percentage value, from 0.0 to 1.0.</param>
        /// <param name="step">The step.</param>
        /// <param name="stepCount">The step count.</param>
        protected void Progress( double percentage, int? step = null, int? stepCount = null )
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

        /// <summary>
        /// Generates the fake email address for the real address.
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
        /// Generates the fake login for the real login.
        /// </summary>
        /// <param name="originalLogin">The original login.</param>
        /// <returns></returns>
        protected string GenerateFakeLoginForLogin( string originalLogin )
        {
            if ( LoginMap.ContainsKey( originalLogin.ToLower() ) )
            {
                return LoginMap[originalLogin.ToLower()];
            }

            string login = $"fakeuser{ LoginMap.Count + 1 }";

            LoginMap.Add( originalLogin.ToLower(), login );

            return login;
        }

        /// <summary>
        /// Generates the fake phone for the real phone number.
        /// </summary>
        /// <param name="originalPhone">The original phone number.</param>
        /// <returns></returns>
        protected string GenerateFakePhoneNumberForPhone( string originalPhone )
        {
            var originalPhoneDigits = new string( originalPhone.Where( c => char.IsDigit( c ) ).ToArray() );

            if ( !PhoneMap.ContainsKey( originalPhoneDigits ) )
            {
                if ( originalPhoneDigits.Length == 7 || originalPhoneDigits.Length == 10 || originalPhoneDigits.Length == 11 )
                {
                    string lineNumber = DataFaker.Random.Replace( "####" );
                    string number = string.Empty;

                    if ( originalPhoneDigits.Length == 11 )
                    {
                        number = "1";
                    }

                    //
                    // Generate area code.
                    //
                    if ( originalPhoneDigits.Length >= 10 )
                    {
                        var areaCode = new[]
                        {
                            Convert.ToChar( '0' + DataFaker.Random.Number( 2, 9 ) ),
                            Convert.ToChar( '0' + DataFaker.Random.Number( 0, 9 ) ),
                            Convert.ToChar( '0' + DataFaker.Random.Number( 0, 9 ) )
                        };

                        number = number + new string( areaCode );
                    }

                    //
                    // Generate exchange code.
                    //
                    var exchangeCode = new[]
                    {
                        Convert.ToChar( '0' + DataFaker.Random.Number( 2, 9 ) ),
                        Convert.ToChar( '0' + DataFaker.Random.Number( 0, 9 ) ),
                        Convert.ToChar( '0' + DataFaker.Random.Number( 0, 9 ) )
                    };
                    number = number + new string( exchangeCode );

                    number = number + DataFaker.Random.Replace( "####" );

                    PhoneMap.Add( originalPhoneDigits, number );
                }
                else
                {
                    string format = string.Join( "", Enumerable.Repeat( "#", originalPhoneDigits.Length ) );
                    PhoneMap.Add( originalPhoneDigits, DataFaker.Random.Replace( format ) );
                }

            }

            var newPhoneDigits = PhoneMap[originalPhoneDigits];
            var newPhone = originalPhone.Select( c => c ).ToArray();
            int digits = 0;

            for ( int i = 0; i < newPhone.Length; i++ )
            {
                if (char.IsDigit(newPhone[i]))
                {
                    newPhone[i] = newPhoneDigits[digits++];
                }
            }

            return new string( newPhone );
        }

        /// <summary>
        /// Gets the best address for coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns></returns>
        protected Address GetBestAddressForCoordinates( Coordinates coordinates )
        {
            Address address;

            if ( GeoLookupCache.ContainsKey( coordinates.ToString() ) )
            {
                return GeoLookupCache[coordinates.ToString()];
            }

            var client = new RestSharp.RestClient( "https://reverse.geocoder.api.here.com/6.2" );
            var req = new RestSharp.RestRequest( "reversegeocode.json" );
            req.AddParameter( "prox", coordinates.ToString() );
            req.AddParameter( "mode", "retrieveAddresses" );
            req.AddParameter( "maxresults", 1 );
            req.AddParameter( "app_id", Properties.Settings.Default.HereAppId );
            req.AddParameter( "app_code", Properties.Settings.Default.HereAppCode );

            var resp = client.Execute<HereRestApi.ApiResponse<HereRestApi.LocationResult>>( req );

            if ( !resp.Data.Response.View.Any() || !resp.Data.Response.View.First().Result.Any() )
            {
                address = new Address
                {
                    Street1 = DataFaker.Address.StreetAddress(),
                    City = DataFaker.Address.City(),
                    State = DataFaker.Address.State(),
                    County = DataFaker.Address.County(),
                    PostalCode = DataFaker.Address.ZipCode(),
                    Country = "US"
                };
            }
            else
            {
                var location = resp.Data.Response.View.First().Result.First().Location;
                address = new Address
                {
                    Street1 = $"{ location.Address.HouseNumber } { location.Address.Street }",
                    City = location.Address.City,
                    State = location.Address.State,
                    County = location.Address.County,
                    PostalCode = location.Address.PostalCode,
                    Country = location.Address.Country.Substring( 0, 2 )
                };
            }

            GeoLookupCache.Add( coordinates.ToString(), address );

            //
            // Save the cache every 100 lookups. That way, if there is a crash, we don't lose everything.
            //
            GeoLookupCount += 1;
            if ( GeoLookupCount > 100 )
            {
                Support.SaveGeocodeCache( GeoLookupCache );
                GeoLookupCount = 0;
            }

            return address;
        }

        #endregion

        #region SQL Methods

        /// <summary>
        /// Gets the database connection.
        /// </summary>
        /// <returns></returns>
        protected SqlConnection GetDatabaseConnection()
        {
            var connection = new SqlConnection( ConnectionString );

            connection.Open();

            return connection;
        }

        /// <summary>
        /// Executes a SQL scalar statement and returns the value.
        /// </summary>
        /// <typeparam name="T">The expected value type to be returned.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The value that resulted from the statement.</returns>
        protected T SqlScalar<T>( string sql )
        {
            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    return ( T ) command.ExecuteScalar();
                }
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

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    using ( var reader = command.ExecuteReader() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default( T ) : ( T ) reader[0];

                            list.Add( c1 );
                        }
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

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    using ( var reader = command.ExecuteReader() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : ( T2 ) reader[1];

                            list.Add( new Tuple<T1, T2>( c1, c2 ) );
                        }
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

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    using ( var reader = command.ExecuteReader() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : ( T2 ) reader[1];
                            var c3 = reader.IsDBNull( 2 ) ? default( T3 ) : ( T3 ) reader[2];

                            list.Add( new Tuple<T1, T2, T3>( c1, c2, c3 ) );
                        }
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

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    using ( var reader = command.ExecuteReader() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : ( T2 ) reader[1];
                            var c3 = reader.IsDBNull( 2 ) ? default( T3 ) : ( T3 ) reader[2];
                            var c4 = reader.IsDBNull( 3 ) ? default( T4 ) : ( T4 ) reader[3];

                            list.Add( new Tuple<T1, T2, T3, T4>( c1, c2, c3, c4 ) );
                        }
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

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    using ( var reader = command.ExecuteReader() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default( T1 ) : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default( T2 ) : ( T2 ) reader[1];
                            var c3 = reader.IsDBNull( 2 ) ? default( T3 ) : ( T3 ) reader[2];
                            var c4 = reader.IsDBNull( 3 ) ? default( T4 ) : ( T4 ) reader[3];
                            var c5 = reader.IsDBNull( 4 ) ? default( T5 ) : ( T5 ) reader[4];

                            list.Add( new Tuple<T1, T2, T3, T4, T5>( c1, c2, c3, c4, c5 ) );
                        }
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

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    using ( var reader = command.ExecuteReader() )
                    {
                        while ( reader.Read() )
                        {
                            var dictionary = new Dictionary<string, object>();

                            for ( int i = 0; i < reader.FieldCount; i++ )
                            {
                                dictionary.Add( reader.GetName( i ), reader.IsDBNull( i ) ? null : reader[i] );
                            }

                            list.Add( dictionary );
                        }
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
            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;

                    if ( parameters != null )
                    {
                        foreach ( var p in parameters )
                        {
                            command.Parameters.AddWithValue( p.Key, p.Value ?? DBNull.Value );
                        }
                    }

                    try
                    {
                        return command.ExecuteNonQuery();
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the database record.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="updatedValues">The updated values.</param>
        protected void UpdateDatabaseRecord( string tableName, int recordId, Dictionary<string, object> updatedValues )
        {
            if ( updatedValues.Any() )
            {
                var updateStrings = new List<string>();

                foreach ( var k in updatedValues.Keys.ToList() )
                {
                    if ( updatedValues[k] is Coordinates coordinates )
                    {
                        updatedValues.Remove( k );
                        updatedValues.Add( $"{ k }Latitude", coordinates.Latitude );
                        updatedValues.Add( $"{ k }Longitude", coordinates.Longitude );

                        updateStrings.Add( $"[{ k }] = geography::Point(@{ k }Latitude, @{ k }Longitude, 4326)" );
                    }
                    else
                    {
                        updateStrings.Add( $"[{ k }] = @{ k }" );
                    }
                }

                SqlCommand( $"UPDATE [{ tableName }] SET { string.Join( ", ", updateStrings ) } WHERE [Id] = { recordId }", updatedValues );
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

            if ( entityTypeId.HasValue )
            {
                SqlCommand( $@"UPDATE AV
SET AV.[Value] = 'False'
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE AV.EntityId = 0
  AND A.[EntityTypeId] = { entityTypeId.Value }
  AND A.[Key] = 'Active'" );
            }
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
        /// Gets the global attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        protected string GetGlobalAttributeValue( string key )
        {
            var defaultValue = SqlQuery<int, string>( $"SELECT [Id], [DefaultValue] FROM [Attribute] WHERE [Key] = '{ key }' AND [EntityTypeId] IS NULL" ).First();
            var value = SqlScalar<string>( $"SELECT [Value] FROM [AttributeValue] WHERE [AttributeId] = { defaultValue.Item1 }" );

            return !string.IsNullOrEmpty( value ) ? value : defaultValue.Item2;
        }

        /// <summary>
        /// Sets the global attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        protected void SetGlobalAttributeValue( string key, string value )
        {
            var attributeId = SqlScalar<int?>( $"SELECT [Id] FROM [Attribute] WHERE [Key] = '{ key }' AND [EntityTypeId] IS NULL" );

            if ( !attributeId.HasValue )
            {
                return;
            }

            var attributeValueId = SqlScalar<int?>( $"SELECT [Id] FROM [AttributeValue] WHERE [AttributeId] = { attributeId }" );
            var parameters = new Dictionary<string, object>
            {
                { "Value", value }
            };

            if ( attributeValueId.HasValue )
            {
                SqlCommand( $"UPDATE [AttributeValue] SET [Value] = @Value WHERE [Id] = { attributeValueId }", parameters );
            }
            else
            {
                SqlCommand( $"INSERT INTO [AttributeValue] ([Issystem], [AttributeId], [Value], [Guid]) VALUES (0, { attributeValueId.Value }, @Value, NEWID())", parameters );
            }
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
        /// Gets the file data from rock the rock database directly.
        /// </summary>
        /// <param name="binaryFileId">The binary file identifier.</param>
        /// <returns></returns>
        protected MemoryStream GetFileDataFromBinaryFileData( int binaryFileId )
        {
            var data = SqlScalar<byte[]>( $"SELECT [Content] FROM [BinaryFileData] WHERE [Id] = { binaryFileId }" );

            if ( data == null )
            {
                return null;
            }

            return new MemoryStream( data );
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
            int processedRows = 0;
            int nextOffset = 0;
            int chunkSize = 1000;
            int totalCount = SqlScalar<int>( $"SELECT COUNT(*) FROM [{ tableName }]" );

            void ProcessChunk()
            {
                int offset = 0;

                lock ( this )
                {
                    offset = nextOffset;
                    nextOffset += chunkSize;
                }
                var rows = SqlQuery( $"SELECT [Id], [{ columns }] FROM [{ tableName }] ORDER BY [Id] OFFSET { offset } ROWS FETCH NEXT { chunkSize } ROWS ONLY" );

                while ( rows.Count > 0 )
                {
                    for ( int i = 0; i < rows.Count; i++ )
                    {
                        int valueId = ( int ) rows[i]["Id"];
                        var updatedValues = new Dictionary<string, object>();

                        foreach ( var c in columnNames )
                        {
                            var value = ( string ) rows[i][c];

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
                            UpdateDatabaseRecord( tableName, valueId, updatedValues );
                        }

                        CancellationToken?.ThrowIfCancellationRequested();
                    }

                    lock ( this )
                    {
                        processedRows += rows.Count;
                        Progress( processedRows / ( double ) totalCount, step, stepCount );

                        offset = nextOffset;
                        nextOffset += chunkSize;
                    }
                    rows = SqlQuery( $"SELECT [Id], [{ columns }] FROM [{ tableName }] ORDER BY [Id] OFFSET { offset } ROWS FETCH NEXT { chunkSize } ROWS ONLY" );
                }
            }

            var tasks = new List<System.Threading.Tasks.Task>();
            for ( int i = 0; i < Environment.ProcessorCount * 2; i++ )
            {
                var task = new System.Threading.Tasks.Task( ProcessChunk );
                tasks.Add( task );
                task.Start();
            }

            while ( !tasks.Any( t => t.IsCompleted ) )
            {
                Thread.Sleep( 100 );
            }
        }

        #endregion
    }
}
