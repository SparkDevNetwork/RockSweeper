using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using RestSharp;

using RockSweeper.SweeperActions;
using RockSweeper.Utility;

using HereRestApi = RockSweeper.External.HereRestApi;

namespace RockSweeper
{
    public partial class SweeperController : IDisposable
    {
        #region Regular Expressions

        /// <summary>
        /// The regular expression to use when scanning for e-mail addresses in text.
        /// </summary>
        private readonly Regex _scrubEmailRegex = new Regex( @"^\w+@([a-zA-Z_]+?\.)+?[a-zA-Z]{2,}$" );

        /// <summary>
        /// The regular expression to use when scanning for phone numbers. This is
        /// complex, but it should catch various forms of phone numbers, such as:
        /// 1 (555) 555-5555
        /// 555.555.5555
        /// 15555555555
        /// </summary>
        private readonly Regex _scrubPhoneRegex = new Regex( @"(^|\D)((1?[2-9][0-9]{2}[2-9][0-9]{2}[0-9]{4}|(1 ?)?\([2-9][0-9]{2}\) ?[2-9][0-9]{2}\-[0-9]{4}|(1[\-\.])?([2-9][0-9]{2}[\-\.])?[2-9][0-9]{2}[\-\.][0-9]{4}|(1 )?[2-9][0-9]{2} [2-9][0-9]{2} [0-9]{4}))($|\D)", RegexOptions.Multiline );

        #endregion

        /// <summary>
        /// Content placeholder for .doc files.
        /// </summary>
        private static readonly Lazy<byte[]> _placeholderDoc = new Lazy<byte[]>( () => Bogus.ResourceHelper.ReadResource( typeof( SweeperController ).Assembly, "RockSweeper.Resources.placeholder.doc" ) );

        /// <summary>
        /// Content placeholder for .docx files.
        /// </summary>
        private static readonly Lazy<byte[]> _placeholderDocx = new Lazy<byte[]>( () => Bogus.ResourceHelper.ReadResource( typeof( SweeperController ).Assembly, "RockSweeper.Resources.placeholder.docx" ) );

        /// <summary>
        /// Content placeholder for .xls files.
        /// </summary>
        private static readonly Lazy<byte[]> _placeholderXls = new Lazy<byte[]>( () => Bogus.ResourceHelper.ReadResource( typeof( SweeperController ).Assembly, "RockSweeper.Resources.placeholder.xls" ) );

        /// <summary>
        /// Content placeholder for .xlsx files.
        /// </summary>
        private static readonly Lazy<byte[]> _placeholderXlsx = new Lazy<byte[]>( () => Bogus.ResourceHelper.ReadResource( typeof( SweeperController ).Assembly, "RockSweeper.Resources.placeholder.xlsx" ) );

        /// <summary>
        /// Content placeholder for .pdf files.
        /// </summary>
        private static readonly Lazy<byte[]> _placeholderPdf = new Lazy<byte[]>( () => Bogus.ResourceHelper.ReadResource( typeof( SweeperController ).Assembly, "RockSweeper.Resources.placeholder.pdf" ) );

        #region Events

        public event EventHandler<ProgressEventArgs> OperationStarted;
        
        public event EventHandler<ProgressEventArgs> ProgressChanged;
        
        public event EventHandler<ProgressEventArgs> OperationCompleted;

        #endregion

        #region Properties

        public CancellationToken CancellationToken { get; set; }

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
        public RockDomain Domain
        {
            get
            {
                if ( _domain == null )
                {
                    _domain = new RockDomain( RockWeb );
                }

                return _domain;
            }
        }
        private RockDomain _domain;

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
        private ConcurrentDictionary<string, string> EmailMap { get; set; }

        /// <summary>
        /// Gets the phone map.
        /// </summary>
        /// <value>
        /// The phone map.
        /// </value>
        private ConcurrentDictionary<string, string> PhoneMap { get; set; }


        /// <summary>
        /// Gets the faker object that will help generate fake data.
        /// </summary>
        /// <value>
        /// The faker object that will help generate fake data.
        /// </value>
        public Bogus.Faker DataFaker { get; private set; }

        /// <summary>
        /// Gets the geo lookup cache.
        /// </summary>
        /// <value>
        /// The geo lookup cache.
        /// </value>
        protected ConcurrentDictionary<string, Address> GeoLookupCache { get; private set; }

        /// <summary>
        /// Gets the geo lookup count.
        /// </summary>
        /// <value>
        /// The geo lookup count.
        /// </value>
        protected int GeoLookupCount { get; private set; }

        /// <summary>
        /// Gets the primary state for the database.
        /// </summary>
        /// <value>
        /// The primary state for the database.
        /// </value>
        protected string LocationPrimaryState
        {
            get
            {
                if ( _locationPrimaryState == null )
                {
                    _locationPrimaryState = SqlScalar<string>( "SELECT TOP 1 [State] FROM [Location] GROUP BY [State] ORDER BY COUNT(*) DESC" );
                }

                return _locationPrimaryState;
            }
        }
        private string _locationPrimaryState;

        /// <summary>
        /// Gets the location city postal codes.
        /// </summary>
        /// <value>
        /// The location city postal codes.
        /// </value>
        public Dictionary<string, List<string>> LocationCityPostalCodes
        {
            get
            {
                if ( _locationCityPostalCodes == null )
                {
                    var cityPostalCodes = new Dictionary<string, List<string>>();

                    //
                    // Setup the list of cities and postal codes to use when we don't have a geo-address.
                    //
                    var res = Bogus.ResourceHelper.ReadResource( GetType().Assembly, "RockSweeper.Resources.city_postal.csv" );
                    var csv = System.Text.Encoding.UTF8.GetString( res ).Split( new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries );
                    foreach ( var cityPair in csv )
                    {
                        var pair = cityPair.Split( ',' );

                        if ( pair.Length == 2 )
                        {
                            if ( !cityPostalCodes.ContainsKey( pair[0] ) )
                            {
                                cityPostalCodes.Add( pair[0], new List<string>() );
                            }

                            if ( !cityPostalCodes[pair[0]].Contains( pair[1] ) )
                            {
                                cityPostalCodes[pair[0]].Add( pair[1] );
                            }
                        }
                    }

                    _locationCityPostalCodes = cityPostalCodes;
                }

                return _locationCityPostalCodes;
            }
        }
        private Dictionary<string, List<string>> _locationCityPostalCodes;

        #endregion

        #region Scrubbed Tables

        /// <summary>
        /// The common tables that are scrubbed by various means.
        /// </summary>
        public Dictionary<string, string[]> ScrubCommonTables { get; } = new Dictionary<string, string[]>
        {
            { "BenevolenceRequest", new[] { "RequestText", "ResultSummary" } },
            { "Communication", new[] { "Message" } },
            { "Note", new[] { "Text" } },
            { "DefinedValue", new[] { "Value", "Description" } },
            { "HtmlContent", new[] { "Content" } },
            { "Group", new[] { "Description" } }
        };

        /// <summary>
        /// The tables that are scrubbed for e-mail addresses in addition to the common tables.
        /// </summary>
        public Dictionary<string, string[]> ScrubEmailTables { get; } = new Dictionary<string, string[]>
        {
            { "BenevolenceRequest", new[] { "Email" } },
            { "Communication", new[] { "FromEmail", "ReplyToEmail", "CCEmails", "BCCEmails" } },
            { "CommunicationTemplate", new[] { "FromEmail", "ReplyToEmail", "CCEmails", "BCCEmails" } },
            { "EventItemOccurrence", new[] { "ContactEmail" } },
            { "PrayerRequest", new[] { "Email" } },
            { "Registration", new[] { "ConfirmationEmail" } },
            { "RegistrationTemplate", new[] { "ConfirmationFromEmail", "ReminderFromEmail", "PaymentReminderFromEmail", "WaitListTransitionFromEmail" } },
            { "ServiceJob", new[] { "NotificationEmails" } },
            { "SystemEmail", new[] { "From", "To" } }
        };

        /// <summary>
        /// The tables that are scrubbed for phone numbers in addition to the common tables.
        /// </summary>
        public Dictionary<string, string[]> ScrubPhoneTables { get; } = new Dictionary<string, string[]>
        {
            { "RegistrationInstance", new[] { "ContactPhone" } },
            { "EventItemOccurrence", new[] { "ContactPhone" } },
            { "BenevolenceRequest", new[] { "HomePhoneNumber", "CellPhoneNumber", "WorkPhoneNumber" } },
            { "Campus", new[] { "PhoneNumber" } }
        };

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
            RockWeb = rockWeb;

            var internalApplicationRoot = GetGlobalAttributeValue( "InternalApplicationRoot" );
            GetFileUrl = $"{ internalApplicationRoot }GetFile.ashx";

            EmailMap = new ConcurrentDictionary<string, string>();
            PhoneMap = new ConcurrentDictionary<string, string>();

            GeoLookupCache = new ConcurrentDictionary<string, Address>( Support.LoadGeocodeCache() );

            SetupDataFaker();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Support.SaveGeocodeCache( GeoLookupCache.ToDictionary( kvp => kvp.Key, kvp => kvp.Value ) );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles sweeping the database in a background thread.
        /// </summary>
        public async Task ExecuteAsync( IList<SweeperOption> options )
        {
            for ( int i = 0; i < options.Count; i++ )
            {
                var option = options[i];
                
                OperationStarted?.Invoke( this, new ProgressEventArgs( option.Id, null, "Running" ) );

                using ( var action = ( SweeperAction ) Activator.CreateInstance( option.ActionType ) )
                {
                    action.Sweeper = this;

                    await action.ExecuteAsync();
                }

                CancellationToken.ThrowIfCancellationRequested();

                OperationCompleted?.Invoke( this, new ProgressEventArgs( option.Id, null, null ) );
            }
        }

        /// <summary>
        /// Setups the data faker.
        /// </summary>
        private void SetupDataFaker()
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
        /// <param name="actionId">The identifier of the action that is updating it's progress.</param>
        /// <param name="percentage">The percentage value, from 0.0 to 1.0.</param>
        /// <param name="step">The step.</param>
        /// <param name="stepCount">The step count.</param>
        public void Progress( Guid actionId, double percentage, int? step = null, int? stepCount = null )
        {
            ProgressEventArgs args;

            if ( step.HasValue && stepCount.HasValue )
            {
                args = new ProgressEventArgs( actionId, percentage, $"Step {step} of {stepCount}" );
            }
            else if ( step.HasValue )
            {
                args = new ProgressEventArgs( actionId, percentage, $"Step {step}" );
            }
            else
            {
                args = new ProgressEventArgs( actionId, percentage, null );
            }

            ProgressChanged?.Invoke( this, args );
        }

        /// <summary>
        /// Processes the items in parallel.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items to be processed.</param>
        /// <param name="chunkSize">Size of the chunk to process at one time.</param>
        /// <param name="processor">The processor function to call for each chunk.</param>
        /// <param name="progress">The progress to call to indicate how far along we are (1 = 100%).</param>
        public void ProcessItemsInParallel<T>( List<T> items, int chunkSize, Action<List<T>> processor, Action<double> progress )
        {
            int totalItems = items.Count;
            int processedItems = 0;
            var lockObject = new object();
            var cancelProcessTokenSource = new CancellationTokenSource();
            var cancelProcessToken = cancelProcessTokenSource.Token;

            void ProcessChunk()
            {
                List<T> chunkItems;

                lock ( lockObject )
                {
                    chunkItems = items.Take( chunkSize ).ToList();
                    items = items.Skip( chunkSize ).ToList();
                }

                while ( chunkItems.Any() )
                {
                    processor( chunkItems );

                    cancelProcessToken.ThrowIfCancellationRequested();

                    lock ( lockObject )
                    {
                        processedItems += chunkItems.Count;
                        progress( ( processedItems + 1 ) / ( double ) totalItems );

                        chunkItems = items.Take( chunkSize ).ToList();
                        items = items.Skip( chunkSize ).ToList();
                    }
                }
            }

            //
            // Create all the tasks we need.
            //
            var tasks = new List<System.Threading.Tasks.Task>();
            for ( int i = 0; i < Environment.ProcessorCount * 2; i++ )
            {
                var task = new System.Threading.Tasks.Task( ProcessChunk, cancelProcessToken );
                tasks.Add( task );
                task.Start();
            }

            //
            // Wait for the tasks to complete. Also cancels tasks if we need to.
            //
            while ( tasks.Any( t => !t.IsCompleted ) )
            {
                Thread.Sleep( 100 );

                if ( CancellationToken.IsCancellationRequested || tasks.Any( t => t.IsFaulted ) )
                {
                    cancelProcessTokenSource.Cancel();
                }
            }

            //
            // If any task threw an exception, re-throw it.
            //
            if ( tasks.Any( t => t.IsFaulted ) )
            {
                throw tasks.First( t => t.IsFaulted ).Exception.InnerException;
            }
        }

        #endregion

        #region Fake Data Methods

        /// <summary>
        /// Generates the fake email address for the real address.
        /// </summary>
        /// <param name="originalEmail">The original email.</param>
        /// <returns></returns>
        public string GenerateFakeEmailAddressForAddress( string originalEmail )
        {
            string email = EmailMap.GetOrAdd( originalEmail.ToLower(), ( key ) =>
            {
                lock ( EmailMap )
                {
                    if ( originalEmail.Contains( "@" ) )
                    {
                        return $"user{ EmailMap.Count + 1 }@fakeinbox.com";
                    }
                    else
                    {
                        return $"user{ EmailMap.Count + 1 }";
                    }
                }
            } );


            return email;
        }

        /// <summary>
        /// Generates the fake phone for the real phone number.
        /// </summary>
        /// <param name="originalPhone">The original phone number.</param>
        /// <returns></returns>
        public string GenerateFakePhoneNumberForPhone( string originalPhone )
        {
            var originalPhoneDigits = new string( originalPhone.Where( c => char.IsDigit( c ) ).ToArray() );

            var newPhoneDigits = PhoneMap.GetOrAdd( originalPhoneDigits, ( key ) =>
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

                    return number;
                }
                else
                {
                    string format = string.Join( "", Enumerable.Repeat( "#", originalPhoneDigits.Length ) );
                    return DataFaker.Random.Replace( format );
                }
            } );

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

        #endregion

        #region Location Methods

        /// <summary>
        /// Gets the best address for coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns></returns>
        public Address GetBestAddressForCoordinates( Coordinates coordinates )
        {
            Address address = GeoLookupCache.GetOrAdd( coordinates.ToString(), ( key ) =>
            {
                var client = new RestClient( "https://reverse.geocoder.api.here.com/6.2" );
                var req = new RestRequest( "reversegeocode.json" );
                req.AddParameter( "prox", coordinates.ToString() );
                req.AddParameter( "mode", "retrieveAddresses" );
                req.AddParameter( "maxresults", 1 );
                req.AddParameter( "app_id", Properties.Settings.Default.HereAppId );
                req.AddParameter( "app_code", Properties.Settings.Default.HereAppCode );

                var resp = client.Execute<HereRestApi.ApiResponse<HereRestApi.LocationResult>>( req );

                lock ( GeoLookupCache )
                {
                    GeoLookupCount += 1;
                }

                if ( !resp.Data.Response.View.Any() || !resp.Data.Response.View.First().Result.Any() )
                {
                    return new Address
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

                    return new Address
                    {
                        Street1 = $"{ location.Address.HouseNumber } { location.Address.Street }",
                        City = location.Address.City,
                        State = location.Address.State,
                        County = location.Address.County,
                        PostalCode = location.Address.PostalCode,
                        Country = location.Address.Country.Substring( 0, 2 )
                    };
                }
            } );

            //
            // Save the cache every 100 lookups. That way, if there is a crash, we don't lose everything.
            //
            lock ( GeoLookupCache )
            {
                if ( GeoLookupCount > 100 )
                {
                    Support.SaveGeocodeCache( GeoLookupCache.ToDictionary( kvp => kvp.Key, kvp => kvp.Value ) );
                    GeoLookupCount = 0;
                }
            }

            return address;
        }

        /// <summary>
        /// Updates the location with fake data.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="street1">The street1.</param>
        /// <param name="street2">The street2.</param>
        /// <param name="county">The county.</param>
        /// <param name="postalCode">The postal code.</param>
        /// <param name="state">The state.</param>
        /// <param name="country">The country.</param>
        public void UpdateLocationWithFakeData( int locationId, string street1, string street2, string county, string postalCode, string state, string country )
        {
            var changes = new Dictionary<string, object>();

            if ( country != "US" )
            {
                changes.Add( "Street1", DataFaker.Address.StreetAddress( false ) );
                changes.Add( "City", $"{DataFaker.Address.City()} {DataFaker.Address.CitySuffix()}" );
                changes.Add( "Country", DataFaker.Address.CountryCode() );

                if ( !string.IsNullOrWhiteSpace( street2 ) )
                {
                    changes.Add( "Street2", DataFaker.Address.SecondaryAddress() );
                }

                if ( !string.IsNullOrWhiteSpace( county ) )
                {
                    changes.Add( "County", DataFaker.Address.County() );
                }

                if ( !string.IsNullOrWhiteSpace( postalCode ) )
                {
                    changes.Add( "PostalCode", postalCode.RandomizeLettersAndNumbers() );
                }

                if ( !string.IsNullOrWhiteSpace( state ) )
                {
                    changes.Add( "State", DataFaker.Address.StateAbbr() );
                }
            }
            else if ( state != LocationPrimaryState )
            {
                changes.Add( "Street1", DataFaker.Address.StreetAddress( street1.Contains( " Apt" ) ) );
                changes.Add( "City", DataFaker.Address.City() );

                if ( !string.IsNullOrWhiteSpace( street2 ) )
                {
                    changes.Add( "Street2", DataFaker.Address.SecondaryAddress() );
                }

                if ( !string.IsNullOrWhiteSpace( county ) )
                {
                    changes.Add( "County", DataFaker.Address.County() );
                }

                if ( !string.IsNullOrWhiteSpace( postalCode ) )
                {
                    changes.Add( "PostalCode", postalCode.RandomizeLettersAndNumbers() );
                }

                if ( !string.IsNullOrWhiteSpace( state ) )
                {
                    changes.Add( "State", DataFaker.Address.StateAbbr() );
                }
            }
            else
            {
                var newCity = DataFaker.PickRandom( LocationCityPostalCodes.Keys.ToList() );
                var newPostal = DataFaker.PickRandom( LocationCityPostalCodes[newCity] );

                changes.Add( "Street1", DataFaker.Address.StreetAddress( street1.Contains( " Apt" ) ) );
                changes.Add( "City", newCity );
                changes.Add( "State", "AZ" );

                if ( !string.IsNullOrWhiteSpace( street2 ) )
                {
                    changes.Add( "Street2", DataFaker.Address.SecondaryAddress() );
                }

                if ( !string.IsNullOrWhiteSpace( county ) )
                {
                    changes.Add( "County", "Maricopa" );
                }

                if ( postalCode.Contains( "-" ) )
                {
                    changes.Add( "PostalCode", $"{newPostal}-{postalCode.Split( '-' )[1]}" );
                }
                else
                {
                    changes.Add( "PostalCode", newPostal );
                }
            }

            UpdateDatabaseRecord( "Location", locationId, changes );
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
        public T SqlScalar<T>( string sql )
        {
            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public List<T> SqlQuery<T>( string sql )
        {
            var list = new List<T>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public List<Tuple<T1, T2>> SqlQuery<T1, T2>( string sql )
        {
            var list = new List<Tuple<T1, T2>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public List<Tuple<T1, T2, T3>> SqlQuery<T1, T2, T3>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public List<Tuple<T1, T2, T3, T4>> SqlQuery<T1, T2, T3, T4>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3, T4>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public List<Tuple<T1, T2, T3, T4, T5>> SqlQuery<T1, T2, T3, T4, T5>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3, T4, T5>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public List<Dictionary<string, object>> SqlQuery( string sql )
        {
            var list = new List<Dictionary<string, object>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

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
        public int SqlCommand( string sql )
        {
            return SqlCommand( sql, null );
        }

        /// <summary>
        /// Executes a non-query SQL command.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The number of rows affected.</returns>
        public int SqlCommand( string sql, Dictionary<string, object> parameters )
        {
            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    if ( parameters != null )
                    {
                        foreach ( var p in parameters )
                        {
                            command.Parameters.AddWithValue( p.Key, p.Value ?? DBNull.Value );
                        }
                    }

                    return command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Updates the database record.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="updatedValues">The updated values.</param>
        public void UpdateDatabaseRecord( string tableName, int recordId, Dictionary<string, object> updatedValues )
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

                try
                {
                    SqlCommand( $"UPDATE [{ tableName }] SET { string.Join( ", ", updateStrings ) } WHERE [Id] = { recordId }", updatedValues );
                }
                catch ( Exception e )
                {
                    System.Diagnostics.Debug.WriteLine( $"{ e.Message }:" );
                    System.Diagnostics.Debug.WriteLine( $"UPDATE [{ tableName }] SET { string.Join( ", ", updateStrings ) } WHERE [Id] = { recordId }" );
                    System.Diagnostics.Debug.WriteLine( Newtonsoft.Json.JsonConvert.SerializeObject( updatedValues, Newtonsoft.Json.Formatting.Indented ) );

                    throw e;
                }
            }
        }

        /// <summary>
        /// Updates the database records in bulk. Null values are skipped, they are not set to NULL in the database.
        /// </summary>
        /// <param name="tableName">Name of the table to update.</param>
        /// <param name="records">The records to be updated.</param>
        /// <exception cref="Exception">Unknown column type '' in bulk update.</exception>
        public void UpdateDatabaseRecords( string tableName, List<Tuple<int, Dictionary<string, object>>> records )
        {
            if ( !records.Any() )
            {
                return;
            }

            var dt = new DataTable( "BulkUpdate" );

            //
            // Generate all the data table columns found in the source of records.
            //
            dt.Columns.Add( "Id", typeof( int ) );
            foreach ( var r in records )
            {
                foreach ( var k in r.Item2.Keys )
                {
                    if ( dt.Columns.Contains( k ) )
                    {
                        continue;
                    }

                    if ( r.Item2[k] != null )
                    {
                        dt.Columns.Add( k, r.Item2[k].GetType() );
                    }
                }
            }

            //
            // Load the data into our in-memory data table.
            //
            foreach ( var r in records )
            {
                var dr = dt.NewRow();
                dr["Id"] = r.Item1;
                foreach ( var k in r.Item2.Keys )
                {
                    if ( dt.Columns.Contains( k ) )
                    {
                        dr[k] = r.Item2[k];
                    }
                }
                dt.Rows.Add( dr );
            }

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    var columns = new List<string>();
                    var setColumns = new List<string>();

                    //
                    // Generate the SQL column list as well as the SET statements.
                    //
                    foreach ( DataColumn c in dt.Columns )
                    {
                        if ( c.DataType == typeof( string ) )
                        {
                            columns.Add( $"[{ c.ColumnName }] [varchar](max) NULL" );
                        }
                        else if ( c.DataType == typeof(int))
                        {
                            columns.Add( $"[{ c.ColumnName }] [int] NULL" );
                        }
                        else
                        {
                            throw new Exception( $"Unknown column type '{ c.DataType.FullName }' in bulk update." );
                        }

                        if ( c.ColumnName != "Id" )
                        {
                            setColumns.Add( $"T.[{ c.ColumnName }] = ISNULL(U.[{ c.ColumnName }], T.[{ c.ColumnName }])" );
                        }
                    }

                    //
                    // Create a temporary table to bulk insert our changes into.
                    //
                    command.CommandText = $"CREATE TABLE #BulkUpdate({ string.Join( ",", columns ) })";
                    command.ExecuteNonQuery();

                    //
                    // Use SqlBulkCopy to insert all the changes in bulk.
                    //
                    using ( SqlBulkCopy bulkCopy = new SqlBulkCopy( connection ) )
                    {
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = "#BulkUpdate";
                        bulkCopy.WriteToServer( dt );
                    }

                    //
                    // Now run a SQL statement that updates any non-NULL columns into the real table.
                    //
                    command.CommandTimeout = 300;
                    command.CommandText = $"UPDATE T SET { string.Join( ",", setColumns ) } FROM [{ tableName }] AS T INNER JOIN #BulkUpdate AS U ON U.[Id] = T.[Id]";
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Rock Helper Methods

        /// <summary>
        /// Gets the entity type identifier.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public int? GetEntityTypeId( string entityType )
        {
            return SqlScalar<int?>( $"SELECT [Id] FROM [EntityType] WHERE [Name] = '{ entityType }'" );
        }

        /// <summary>
        /// Gets the field type identifier.
        /// </summary>
        /// <param name="fieldType">Type of the field.</param>
        /// <returns></returns>
        public int? GetFieldTypeId( string fieldType )
        {
            return SqlScalar<int?>( $"SELECT [Id] FROM [FieldType] WHERE [Class] = '{ fieldType }'" );
        }

        /// <summary>
        /// Disables a single component with the given class name.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        public void DisableComponentType( string componentType )
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
        public void DeleteAttributeValuesForComponentType( string componentType )
        {
            var entityTypeId = GetEntityTypeId( componentType );

            if ( entityTypeId.HasValue )
            {
                SqlCommand( $@"DELETE AV
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE AV.EntityId = 0
  AND A.[EntityTypeId] = { entityTypeId.Value }" );
            }
        }

        /// <summary>
        /// Disables all the individual components of the given parent type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <param name="excludedTypes">The types to be excluded.</param>
        public void DisableComponentsOfType( string componentType, string[] excludedTypes = null )
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
        public void DeleteAttributeValuesForComponentsOfType( string componentType, string[] excludedTypes = null )
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
        public string GetGlobalAttributeValue( string key )
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
        public void SetGlobalAttributeValue( string key, string value )
        {
            var attributeId = SqlScalar<int?>( $"SELECT [Id] FROM [Attribute] WHERE [Key] = '{ key }' AND [EntityTypeId] IS NULL" );

            if ( !attributeId.HasValue )
            {
                return;
            }

            var attributeValueId = SqlScalar<int?>( $"SELECT [Id] FROM [AttributeValue] WHERE [AttributeId] = { attributeId.Value }" );
            var parameters = new Dictionary<string, object>
            {
                { "Value", value }
            };

            if ( attributeValueId.HasValue )
            {
                SqlCommand( $"UPDATE [AttributeValue] SET [Value] = @Value WHERE [Id] = { attributeValueId.Value }", parameters );
            }
            else
            {
                SqlCommand( $"INSERT INTO [AttributeValue] ([Issystem], [AttributeId], [Value], [Guid]) VALUES (0, { attributeId.Value }, @Value, NEWID())", parameters );
            }
        }

        /// <summary>
        /// Sets the component attribute value by either updating the existing value or creating a new one.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="value">The value.</param>
        public void SetComponentAttributeValue( string entityType, string attributeKey, string value )
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
        public MemoryStream GetFileDataFromRock( int binaryFileId )
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
        public MemoryStream GetFileDataFromRock( Guid binaryFileGuid )
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
        public MemoryStream GetFileDataFromBinaryFileData( int binaryFileId )
        {
            var data = SqlScalar<byte[]>( $"SELECT [Content] FROM [BinaryFileData] WHERE [Id] = { binaryFileId }" );

            if ( data == null )
            {
                return null;
            }

            return new MemoryStream( data );
        }

        #endregion

        #region File Methods

        /// <summary>
        /// Determines whether filename is an image.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>
        ///   <c>true</c> if filename is an image; otherwise, <c>false</c>.
        /// </returns>
        public bool IsFileNameImage( string filename )
        {
            return filename.EndsWith( ".jpg", StringComparison.CurrentCultureIgnoreCase ) ||
                filename.EndsWith( ".jpeg", StringComparison.CurrentCultureIgnoreCase ) ||
                filename.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase );
        }

        /// <summary>
        /// Creates a new placeholder image of the given dimensions.
        /// </summary>
        /// <param name="originalFilename">The original filename, used to determine the output format.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <returns>A byte array that represents the encoded format.</returns>
        public byte[] CreatePlaceholderImage( string originalFilename, int width, int height )
        {
            using ( var imageStream = new MemoryStream() )
            {
                // Generate the new image.
                try
                {
                    var image = new Bitmap( width, height );
                    var g = Graphics.FromImage( image );

                    g.FillRectangle( Brushes.White, new Rectangle( 0, 0, width, height ) );

                    if ( width > 128 && height > 64 )
                    {
                        var font = new Font( "Tahoma", height / 10 );
                        var sizeText = $"{width}x{height}";
                        var size = g.MeasureString( sizeText, font );
                        g.DrawString( sizeText, font, Brushes.Black, new PointF( ( width - size.Width ) / 2, ( height - size.Height ) / 2 ) );
                    }

                    g.Flush();

                    image.SetResolution( 72, 72 );

                    if ( originalFilename.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase ) )
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

                return imageStream.ToArray();
            }
        }

        /// <summary>
        /// Gets the placeholder file data for a binary file.
        /// </summary>
        /// <param name="file">The file information.</param>
        /// <returns>An array of bytes to use as the new content.</returns>
        public byte[] GetPlaceholderForBinaryFilename( BinaryFile file )
        {
            if ( file.FileName.EndsWith( ".docx", StringComparison.OrdinalIgnoreCase ) || file.MimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" )
            {
                return _placeholderDocx.Value;
            }
            else if ( file.FileName.EndsWith( ".doc", StringComparison.OrdinalIgnoreCase ) || file.MimeType == "application/msword" )
            {
                return _placeholderDoc.Value;
            }
            else if ( file.FileName.EndsWith( ".xlsx", StringComparison.OrdinalIgnoreCase ) || file.MimeType == "" )
            {
                return _placeholderXlsx.Value;
            }
            else if ( file.FileName.EndsWith( ".xls", StringComparison.OrdinalIgnoreCase ) || file.MimeType == "application/vnd.ms-excel" )
            {
                return _placeholderXls.Value;
            }
            else if ( file.FileName.EndsWith( ".pdf", StringComparison.OrdinalIgnoreCase ) || file.MimeType == "application/pdf" )
            {
                return _placeholderPdf.Value;
            }
            else
            {
                return new byte[0];
            }
        }

        #endregion

        #region Data Scrubbing Methods

        /// <summary>
        /// Scrubs the specified table column with the given replacement data.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="replacement">The replacement function to provide the new value.</param>
        /// <param name="step">The step number.</param>
        /// <param name="stepCount">The step count.</param>
        public void ScrubTableTextColumn( string tableName, string columnName, Func<string, string> replacement, Action<double> progress = null )
        {
            ScrubTableTextColumns( tableName, new[] { columnName }, replacement, progress );
        }

        /// <summary>
        /// Scrubs the specified table columns with the given replacement data.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="replacement">The replacement function to provide the new value.</param>
        /// <param name="step">The step number.</param>
        /// <param name="stepCount">The step count.</param>
        public void ScrubTableTextColumns( string tableName, IEnumerable<string> columnNames, Func<string, string> replacement, Action<double> progress = null )
        {
            string columns = string.Join( "], [", columnNames );
            var rowIds = SqlQuery<int>( $"SELECT [Id] FROM [{tableName}] ORDER BY [Id]" );

            CancellationToken.ThrowIfCancellationRequested();

            ProcessItemsInParallel( rowIds, 1000, ( itemIds ) =>
            {
                var rows = SqlQuery( $"SELECT [Id], [{columns}] FROM [{tableName}] WHERE [Id] IN ({string.Join( ",", itemIds )})" );

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
                }
            }, ( p ) =>
            {
                progress?.Invoke( p );
            } );
        }

        /// <summary>
        /// Scrubs the content of any e-mail addresses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string ScrubContentForEmailAddresses( string value )
        {
            return _scrubEmailRegex.Replace( value, ( match ) =>
            {
                return GenerateFakeEmailAddressForAddress( match.Value );
            } );
        }

        /// <summary>
        /// Scrubs the content of any phone numbers.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string ScrubContentForPhoneNumbers( string value )
        {
            return _scrubPhoneRegex.Replace( value, ( match ) =>
            {
                return match.Groups[1].Value + GenerateFakePhoneNumberForPhone( match.Groups[2].Value ) + match.Groups[8].Value;
            } );
        }

        /// <summary>
        /// Merge the various scrub table dictionaries together into a single master dictionary.
        /// </summary>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <returns></returns>
        public Dictionary<string, string[]> MergeScrubTableDictionaries( params Dictionary<string, string[]>[] dictionaries )
        {
            var master = new Dictionary<string, string[]>();

            foreach ( var dictionary in dictionaries )
            {
                foreach ( var kvp in dictionary )
                {
                    if ( !master.ContainsKey( kvp.Key ) )
                    {
                        master.Add( kvp.Key, new string[0] );
                    }

                    var values = master[kvp.Key].ToList();

                    foreach ( var value in kvp.Value )
                    {
                        if ( !values.Contains( value ) )
                        {
                            values.Add( value );
                        }
                    }

                    master[kvp.Key] = values.ToArray();
                }
            }

            return master;
        }

        #endregion
    }
}
