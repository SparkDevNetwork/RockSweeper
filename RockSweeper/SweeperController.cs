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
        public static readonly Regex EmailRegex = new Regex( @"^\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase );

        /// <summary>
        /// The regular expression to use when scanning for phone numbers. This is
        /// complex, but it should catch various forms of phone numbers, such as:
        /// 1 (555) 555-5555
        /// 555.555.5555
        /// 15555555555
        /// </summary>
        private readonly Regex _scrubPhoneRegex = new Regex( @"(^|\D)((1?[2-9][0-9]{2}[2-9][0-9]{2}[0-9]{4}|(1 ?)?\([2-9][0-9]{2}\) ?[2-9][0-9]{2}\-[0-9]{4}|(1[\-\.])?([2-9][0-9]{2}[\-\.])?[2-9][0-9]{2}[\-\.][0-9]{4}|(1 )?[2-9][0-9]{2} [2-9][0-9]{2} [0-9]{4}))($|\D)", RegexOptions.Multiline );

        #endregion

        #region Fields

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

        /// <summary>
        /// Known component class names.
        /// </summary>
        private static readonly Dictionary<string, string[]> _knownComponentTypes = new Dictionary<string, string[]>();

        /// <summary>
        /// The primary state the organization belongs to.
        /// </summary>
        private string _primaryState;

        /// <summary>
        /// The URL to use to get a file from the server.
        /// </summary>
        private string _getFileUrl;

        /// <summary>
        /// The list of action types that have been executed.
        /// </summary>
        private ConcurrentBag<Type> _executeActionTypes = new ConcurrentBag<Type>();

        /// <summary>
        /// The number of SQL queries that have been executed.
        /// </summary>
        private int _sqlQueryCount = 0;

        #endregion

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
        /// Gets the rock version that was found in the database.
        /// </summary>
        /// <value>The rock version, such as 1.17.0.4.</value>
        public Version RockVersion { get; private set; }

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

        /// <summary>
        /// The original first name values from the Person table.
        /// </summary>
        public IReadOnlyCollection<string> OriginalFirstNames { get; private set; }

        /// <summary>
        /// The original nick name values from the Person table.
        /// </summary>
        public IReadOnlyCollection<string> OriginalNickNames { get; private set; }

        /// <summary>
        /// The original last name values from the Person table.
        /// </summary>
        public IReadOnlyCollection<string> OriginalLastNames { get; private set; }

        /// <summary>
        /// The number of SQL queries that have been executed.
        /// </summary>
        public int SqlQueryCount => _sqlQueryCount;

        #endregion

        #region Scrubbed Tables

        /// <summary>
        /// The common tables that are scrubbed by various means.
        /// </summary>
        public Dictionary<string, string[]> ScrubCommonTables { get; } = new Dictionary<string, string[]>
        {
            { "BenevolenceRequest", new[] { "RequestText", "ResultSummary" } },
            { "DefinedValue", new[] { "Value", "Description" } },
            { "HtmlContent", new[] { "Content" } },
            { "Group", new[] { "Description" } }
        };

        /// <summary>
        /// The tables that are scrubbed for person names.
        /// </summary>
        public Dictionary<string, string[]> ScrubNameTables { get; } = new Dictionary<string, string[]>
        {
            { "Communication", new[] { "FromName" } },
            { "CommunicationTemplate", new[] { "FromName" } },
            { "RegistrationTemplate", new[] { "ConfirmationFromName", "PaymentReminderFromName", "ReminderFromName", "RequestEntryName", "WaitListTransitionFromName" } },
            { "SystemEmail", new[] { "FromName" } }
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
            { "PersonSearchKey", new[] { "SearchValue" } },
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
            { "CommunicationResponse", new[] { "MessageKey" } },
            { "RegistrationInstance", new[] { "ContactPhone" } },
            { "EventItemOccurrence", new[] { "ContactPhone" } },
            { "BenevolenceRequest", new[] { "HomePhoneNumber", "CellPhoneNumber", "WorkPhoneNumber" } },
            { "Campus", new[] { "PhoneNumber" } }
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the <see cref="SweeperController"/> static data.
        /// </summary>
        static SweeperController()
        {
            _knownComponentTypes.Add( "Rock.AI.Provider.AIProviderComponent", new string[]
            {
                "Rock.AI.OpenAI.Provider.OpenAIProvider"
            } );

            _knownComponentTypes.Add( "Rock.Address.VerificationComponent", new string[]
            {
                "Rock.Address.Bing",
                "Rock.Address.SmartyStreets",
                "com.bricksandmortarstudio.IdealPostcodes.Address.IdealPostcodes",
                "sg.carmel.Address.OneMap.OneMap"
            } );

            _knownComponentTypes.Add( "Rock.Communication.TransportComponent", new string[]
            {
                "Rock.Communication.Transport.Firebase",
                "Rock.Communication.Transport.MailgunHttp",
                "Rock.Communication.Transport.MandrillSmtp",
                "Rock.Communication.Transport.OneSignal", // Plugin
                "Rock.Communication.Transport.RockMobilePush",
                "Rock.Communication.Transport.SendGridHttp",
                "Rock.Communication.Transport.SmsTest",
                "Rock.Communication.Transport.Twilio",
                "com.bricksandmortarstudio.Communication.Transport.TwilioAlphanumeric",
                "com.clearstream.Clearstream.Communication.Transport.Clearstream",
                "com.intulse.PbxComponent.IntulseSmsTransport",
                "com.mbt.mbtSMS.Communication.Transport.MBT",
                "com.subsplash.Communcation.Transport.SubsplashTransport",
                "fortresstechnology.za.co.Plugins.Communication.Transport.BulkSMS",
                "fortresstechnology.za.co.Plugins.Communication.Transport.ConnectMobile"
            } );

            _knownComponentTypes.Add( "Rock.Financial.GatewayComponent", new string[]
            {
                "Rock.Financial.TestGateway",
                "Rock.Financial.TestRedirectionGateway",
                "Rock.MyWell.MyWellGateway",
                "Rock.NMI.Gateway",
                "Rock.PayFlowPro.Gateway",
                "com.GyveGateway.GyveGateway",
                "com.PaymentData.Gateway.Gateway",
                "com.SimpleDonation.Gateway.SimpleDonation",
                "com.pushpay.RockRMS.Gateway",
                "fortresstechnology.za.co.PayFast.Gateway",
                "io.lanio.stripe.Gateway",
                "io.scanpay.RockRMS.Gateway",
                "org.mywell.MyWellGateway.MyWellGateway"
            } );

            _knownComponentTypes.Add( "Rock.Pbx.PbxComponent", new string[]
            {
                "com.blueboxmoon.FreePBX.Provider.FreePBX",
                "com.intulse.PbxComponent.IntulsePbxComponent",
                "com.minecartstudio.PbxSwitchvox.Pbx.Provider.Switchvox"
            } );

            _knownComponentTypes.Add( "Rock.Security.AuthenticationComponent", new string[]
            {
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.Facebook",
                "Rock.Security.Authentication.Google",
                "Rock.Security.Authentication.OidcClient",
                "Rock.Security.Authentication.PasswordlessAuthentication",
                "Rock.Security.Authentication.PINAuthentication",
                "Rock.Security.Authentication.Twitter",
                "com.bemaservices.Security.SSO.Authenticators.Okta",
                "com.bemaservices.Security.SSO.Authenticators.Office365",
                "com.bemaservices.Security.SSO.Authenticators.Vision2",
                "tech.triumph.AzureAD.Security.Authentication.AzureAD"
            } );

            _knownComponentTypes.Add( "Rock.Security.BackgroundCheckComponent", new string[]
            {
                "Rock.Security.BackgroundCheck.ProtectMyMinistry",
                "Rock.Checkr.Checkr",
                "CIAResearch.CIAResearch",
                "com.activescreeningfaith.BackgroundCheck.ActiveScreeningFaith",
                "com.bemaservices.MinistrySafe.MinistrySafe",
                "com.protectmyministry.BackgroundCheck.ProtectMyMinistryV2",
                "com.safehiringsolutions.BackgroundCheck.SafeMinistrySolutions",
                "com.securesearchfaith.BackgroundCheck.SecureSearchFaith"
            } );

            _knownComponentTypes.Add( "Rock.Security.DigitalSignatureComponent", new string[]
            {
                "Rock.SignNow.SignNow"
            } );

            _knownComponentTypes.Add( "Rock.Storage.AssetStorage.AssetStorageComponent", new string[]
            {
                "Rock.Storage.AssetStorage.AmazonS3Component",
                "Rock.Storage.AssetStorage.AzureCloudStorageComponent",
                "Rock.Storage.AssetStorage.FileSystemComponent",
                "Rock.Storage.AssetStorage.GoogleCloudStorageComponent",
                "com.minecartstudio.CloudinaryStorageProvider.Storage.CloudinaryComponent",
                "tech.triumph.CloudinaryStorageProvider.CloudinaryComponent"
            } );

            _knownComponentTypes.Add( "Rock.Storage.ProviderComponent", new string[]
            {
                "Rock.Storage.Provider.AzureBlobStorage",
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem",
                "Rock.Storage.Provider.GoogleCloudStorageProvider",
                "com.blueboxmoon.B2CloudStorage.B2StorageProvider",
                "com.minecartstudio.CloudinaryStorageProvider.CloudinaryBlobStorage",
                "rocks.pillars.AmazonStorageProvider.S3BlobStorage",
                "rocks.pillars.AzureStorageProvider.AzureBlobStorage",
                "tech.triumph.CloudinaryStorageProvider.CloundinaryBlobStorage",
            } );
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SweeperController"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SweeperController( string connectionString )
        {
            ConnectionString = connectionString;

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
        /// Gets the base URL to use to retrieve a file from the server.
        /// </summary>
        /// <returns>A string representing the URL.</returns>
        private async Task<string> GetFileUrlAsync()
        {
            if ( _getFileUrl == null )
            {
                var internalApplicationRoot = await GetGlobalAttributeValueAsync( "InternalApplicationRoot" );
                _getFileUrl = $"{internalApplicationRoot}GetFile.ashx";
            }

            return _getFileUrl;
        }

        /// <summary>
        /// Handles sweeping the database in a background thread.
        /// </summary>
        public async Task ExecuteAsync( IList<SweeperOption> options )
        {
            await GetRockVersionAsync();

            if ( RockVersion < new Version( 1, 14, 0 ) )
            {
                throw new Exception( $"Database version {RockVersion} is not supported, 1.14.0 is the minimum supported version." );
            }

            SleepHelper.PreventSleep();

            try
            {
                await StashOriginalPersonNamesAsync();

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

                    _executeActionTypes.Add( option.ActionType );
                }
            }
            finally
            {
                SleepHelper.AllowSleep();
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
        /// Get rock version from the database.
        /// </summary>
        private async Task GetRockVersionAsync()
        {
            var globalDefaultAssemblyName = await SqlScalarAsync<string>( "SELECT [AssemblyName] FROM [EntityType] WHERE [Name] = 'Rock.Security.GlobalDefault'" );
            var match = Regex.Match( globalDefaultAssemblyName, "Version=([\\d.]+)," );

            if ( !match.Success )
            {
                throw new Exception( "Unable to determine Rock version from database." );
            }

            RockVersion = new Version( match.Groups[1].Value );
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

            var progressStepCount = Math.Max( 1, stepCount ?? 1 );
            var progressStep = Math.Max( 1, step ?? 1 );

            percentage = ( ( progressStep - 1 ) / ( double ) progressStepCount ) + ( percentage / progressStepCount );

            if ( step.HasValue && stepCount.HasValue )
            {
                args = new ProgressEventArgs( actionId, percentage, $"Step {step}/{stepCount}" );
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
        public async Task ProcessItemsInParallelAsync<T>( List<T> items, int chunkSize, Func<List<T>, Task> processor, Action<double> progress )
        {
            int totalItems = items.Count;
            int processedItems = 0;
            var lockObject = new object();
            var queue = new ConcurrentQueue<List<T>>();

            // Convert the full set to chunks and then enqueue them all.
            items.Chunk( chunkSize )
                .ToList()
                .ForEach( c => queue.Enqueue( c.ToList() ) );

            async Task ProcessChunk()
            {
                while ( queue.TryDequeue( out var chunk ) )
                {
                    await processor( chunk );

                    CancellationToken.ThrowIfCancellationRequested();

                    lock ( lockObject )
                    {
                        processedItems += chunk.Count;
                        progress( processedItems / ( double ) totalItems );
                    }
                }
            }

            //
            // Create all the tasks we need.
            //
            var tasks = new List<Task>();
            for ( int i = 0; i < Environment.ProcessorCount * 2; i++ )
            {
                tasks.Add( Task.Run( ProcessChunk, CancellationToken ) );
            }

            //
            // Wait for the tasks to complete. Also cancels tasks if we need to.
            //
            await Task.WhenAll( tasks );
        }

        /// <summary>
        /// Determines if the given action has already been executed.
        /// </summary>
        /// <typeparam name="T">The action type.</typeparam>
        /// <returns><c>true</c> if the action has been executed; otherwise <c>false</c>.</returns>
        public bool HasActionExecuted<T>()
        {
            return _executeActionTypes.Contains( typeof( T ) );
        }

        /// <summary>
        /// Gets the unique person names and store them in memory for use by
        /// various actions.
        /// </summary>
        /// <returns>A task that represents this operation.</returns>
        public async Task StashOriginalPersonNamesAsync()
        {
            OriginalFirstNames = new HashSet<string>( await SqlQueryAsync<string>( "SELECT DISTINCT [FirstName] FROM [Person]" ) );
            OriginalNickNames = new HashSet<string>( await SqlQueryAsync<string>( "SELECT DISTINCT [NickName] FROM [Person]" ) );
            OriginalLastNames = new HashSet<string>( await SqlQueryAsync<string>( "SELECT DISTINCT [LastName] FROM [Person]" ) );
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
                        return $"user{EmailMap.Count + 1}@fakeinbox.com";
                    }
                    else
                    {
                        return $"user{EmailMap.Count + 1}";
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

                        number += new string( areaCode );
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
                    number += new string( exchangeCode );

                    number += DataFaker.Random.Replace( "####" );

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
                if ( char.IsDigit( newPhone[i] ) )
                {
                    newPhone[i] = newPhoneDigits[digits++];
                }
            }

            return new string( newPhone );
        }

        #endregion

        #region Location Methods

        /// <summary>
        /// Gets the primary state for the database.
        /// </summary>
        /// <value>
        /// The primary state for the database.
        /// </value>
        private async Task<string> GetPrimaryStateAsync()
        {
            if ( _primaryState == null )
            {
                _primaryState = await SqlScalarAsync<string>( "SELECT TOP 1 [State] FROM [Location] GROUP BY [State] ORDER BY COUNT(*) DESC" );
            }

            return _primaryState;
        }

        /// <summary>
        /// Gets the best address for coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns></returns>
        public async Task<Address> GetBestAddressForCoordinatesAsync( Coordinates coordinates )
        {
            if ( !GeoLookupCache.TryGetValue( coordinates.ToString(), out var address ) )
            {
                var client = new RestClient( "https://reverse.geocoder.api.here.com/6.2" );
                var req = new RestRequest( "reversegeocode.json" );
                req.AddParameter( "prox", coordinates.ToString() );
                req.AddParameter( "mode", "retrieveAddresses" );
                req.AddParameter( "maxresults", 1 );
                req.AddParameter( "app_id", Properties.Settings.Default.HereAppId );
                req.AddParameter( "app_code", Properties.Settings.Default.HereAppCode );

                var resp = await client.ExecuteAsync<HereRestApi.ApiResponse<HereRestApi.LocationResult>>( req );

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
                        Street1 = $"{location.Address.HouseNumber} {location.Address.Street}",
                        City = location.Address.City,
                        State = location.Address.State,
                        County = location.Address.County,
                        PostalCode = location.Address.PostalCode,
                        Country = location.Address.Country.Substring( 0, 2 )
                    };
                }

                if ( GeoLookupCache.TryAdd( coordinates.ToString(), address ) )
                {
                    lock ( GeoLookupCache )
                    {
                        GeoLookupCount += 1;
                    }
                }
            }

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
        public async Task UpdateLocationWithFakeDataAsync( int locationId, string street1, string street2, string county, string postalCode, string state, string country )
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
            else if ( state != await GetPrimaryStateAsync() )
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

            await UpdateDatabaseRecordAsync( "Location", locationId, changes );
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
        public async Task<T> SqlScalarAsync<T>( string sql )
        {
            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    var r = await command.ExecuteScalarAsync();

                    if ( r == DBNull.Value  )
                    {
                        return ( T ) ( object ) null;
                    }

                    return ( T ) r;
                }
            }
        }

        /// <summary>
        /// Execute a SQL query that returns multiple rows of a single column data.
        /// </summary>
        /// <typeparam name="T">The type of the return values.</typeparam>
        /// <param name="sql">The SQL statement.</param>
        /// <returns></returns>
        public async Task<List<T>> SqlQueryAsync<T>( string sql )
        {
            var list = new List<T>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    using ( var reader = await command.ExecuteReaderAsync() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default : ( T ) reader[0];

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
        public async Task<List<Tuple<T1, T2>>> SqlQueryAsync<T1, T2>( string sql )
        {
            var list = new List<Tuple<T1, T2>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    using ( var reader = await command.ExecuteReaderAsync() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default : ( T2 ) reader[1];

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
        public async Task<List<Tuple<T1, T2, T3>>> SqlQueryAsync<T1, T2, T3>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    using ( var reader = await command.ExecuteReaderAsync() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default : ( T2 ) reader[1];
                            var c3 = reader.IsDBNull( 2 ) ? default : ( T3 ) reader[2];

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
        public async Task<List<Tuple<T1, T2, T3, T4>>> SqlQueryAsync<T1, T2, T3, T4>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3, T4>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    using ( var reader = await command.ExecuteReaderAsync() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default : ( T2 ) reader[1];
                            var c3 = reader.IsDBNull( 2 ) ? default : ( T3 ) reader[2];
                            var c4 = reader.IsDBNull( 3 ) ? default : ( T4 ) reader[3];

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
        public async Task<List<Tuple<T1, T2, T3, T4, T5>>> SqlQueryAsync<T1, T2, T3, T4, T5>( string sql )
        {
            var list = new List<Tuple<T1, T2, T3, T4, T5>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    using ( var reader = await command.ExecuteReaderAsync() )
                    {
                        while ( reader.Read() )
                        {
                            var c1 = reader.IsDBNull( 0 ) ? default : ( T1 ) reader[0];
                            var c2 = reader.IsDBNull( 1 ) ? default : ( T2 ) reader[1];
                            var c3 = reader.IsDBNull( 2 ) ? default : ( T3 ) reader[2];
                            var c4 = reader.IsDBNull( 3 ) ? default : ( T4 ) reader[3];
                            var c5 = reader.IsDBNull( 4 ) ? default : ( T5 ) reader[4];

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
        public async Task<List<Dictionary<string, object>>> SqlQueryAsync( string sql )
        {
            var list = new List<Dictionary<string, object>>();

            using ( var connection = GetDatabaseConnection() )
            {
                using ( var command = connection.CreateCommand() )
                {
                    command.CommandText = sql;
                    command.CommandTimeout = 300;

                    Interlocked.Increment( ref _sqlQueryCount );

                    using ( var reader = await command.ExecuteReaderAsync() )
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
        public Task<int> SqlCommandAsync( string sql )
        {
            return SqlCommandAsync( sql, null );
        }

        /// <summary>
        /// Executes a non-query SQL command.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <returns>The number of rows affected.</returns>
        public async Task<int> SqlCommandAsync( string sql, Dictionary<string, object> parameters )
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

                    Interlocked.Increment( ref _sqlQueryCount );

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Updates the database record.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="updatedValues">The updated values.</param>
        public async Task UpdateDatabaseRecordAsync( string tableName, int recordId, Dictionary<string, object> updatedValues )
        {
            if ( updatedValues.Any() )
            {
                var updateStrings = new List<string>();

                foreach ( var k in updatedValues.Keys.ToList() )
                {
                    if ( updatedValues[k] is Coordinates coordinates )
                    {
                        updatedValues.Remove( k );
                        updatedValues.Add( $"{k}Latitude", coordinates.Latitude );
                        updatedValues.Add( $"{k}Longitude", coordinates.Longitude );

                        updateStrings.Add( $"[{k}] = geography::Point(@{k}Latitude, @{k}Longitude, 4326)" );
                    }
                    else
                    {
                        updateStrings.Add( $"[{k}] = @{k}" );
                    }
                }

                try
                {
                    await SqlCommandAsync( $"UPDATE [{tableName}] SET {string.Join( ", ", updateStrings )} WHERE [Id] = {recordId}", updatedValues );
                }
                catch ( Exception e )
                {
                    System.Diagnostics.Debug.WriteLine( $"{e.Message}:" );
                    System.Diagnostics.Debug.WriteLine( $"UPDATE [{tableName}] SET {string.Join( ", ", updateStrings )} WHERE [Id] = {recordId}" );
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
        /// <param name="progressCallback">The function to call to report progress, may be <c>null</c>.</param>
        /// <exception cref="Exception">Unknown column type '' in bulk update.</exception>
        public async Task UpdateDatabaseRecordsAsync( string tableName, List<Tuple<int, Dictionary<string, object>>> records, Action<double> progressCallback = null )
        {
            if ( !records.Any() )
            {
                return;
            }

            // If we got more than 2,500 records to update then do it in chunks.
            if ( records.Count > 2_500 )
            {
                var chunks = records.Chunk( 2_500 ).ToList();

                for (int i = 0; i < chunks.Count; i++)
                {
                    await UpdateDatabaseRecordsAsync( tableName, chunks[i].ToList(), null );

                    progressCallback?.Invoke( ( i + 1 ) / ( double ) chunks.Count );
                }

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
                        if ( r.Item2[k] is Coordinates )
                        {
                            if ( !dt.Columns.Contains( $"{k}Latitude" ) )
                            {
                                dt.Columns.Add( $"{k}Latitude", typeof( double ) );
                                dt.Columns.Add( $"{k}Longitude", typeof( double ) );
                            }
                        }
                        else
                        {
                            dt.Columns.Add( k, r.Item2[k].GetType() );
                        }
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
                        if ( r.Item2[k] is Coordinates coordinates )
                        {
                            dr[$"{k}Latitude"] = coordinates.Latitude;
                            dr[$"{k}Longitude"] = coordinates.Longitude;
                        }
                        else
                        {
                            dr[k] = r.Item2[k];
                        }
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
                            columns.Add( $"[{c.ColumnName}] [varchar](max) NULL" );
                        }
                        else if ( c.DataType == typeof( int ) )
                        {
                            columns.Add( $"[{c.ColumnName}] [int] NULL" );
                        }
                        else if ( c.DataType == typeof( double ) && ( c.ColumnName.EndsWith( "Latitude" ) || c.ColumnName.EndsWith( "Longitude" ) ) )
                        {
                            columns.Add( $"[{c.ColumnName}] [decimal](18, 2) NULL" );
                        }
                        else if ( c.DataType == typeof( bool ) )
                        {
                            columns.Add( $"[{c.ColumnName}] [bit] NULL" );
                        }
                        else
                        {
                            throw new Exception( $"Unknown column type '{c.DataType.FullName}' in bulk update." );
                        }

                        if ( c.ColumnName != "Id" )
                        {
                            if ( c.DataType == typeof( double ) )
                            {
                                if ( c.ColumnName.EndsWith( "Latitude" ) )
                                {
                                    var columnName = c.ColumnName.Replace( "Latitude", string.Empty );

                                    setColumns.Add( $"T.[{columnName}] = CASE WHEN U.[{columnName}Latitude] IS NOT NULL AND U.[{columnName}Longitude] IS NOT NULL THEN geography::Point(U.[{columnName}Latitude], U.[{columnName}Longitude], 4326) ELSE T.[{columnName}] END" );
                                }
                                else if ( c.ColumnName.EndsWith( "Longitude" ) )
                                {
                                    // Do nothing, handled above.
                                }
                                else
                                {
                                    throw new Exception( "Don't know how to handle generic double column." );
                                }
                            }
                            else
                            {
                                setColumns.Add( $"T.[{c.ColumnName}] = ISNULL(U.[{c.ColumnName}], T.[{c.ColumnName}])" );
                            }
                        }
                    }

                    //
                    // Create a temporary table to bulk insert our changes into.
                    //
                    command.CommandText = $"CREATE TABLE #BulkUpdate({string.Join( ",", columns )})";
                    Interlocked.Increment( ref _sqlQueryCount );
                    await command.ExecuteNonQueryAsync();

                    //
                    // Use SqlBulkCopy to insert all the changes in bulk.
                    //
                    Interlocked.Increment( ref _sqlQueryCount );
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
                    command.CommandText = $"UPDATE T SET {string.Join( ",", setColumns )} FROM [{tableName}] AS T INNER JOIN #BulkUpdate AS U ON U.[Id] = T.[Id]";
                    Interlocked.Increment( ref _sqlQueryCount );
                    await command.ExecuteNonQueryAsync();
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
        public Task<int?> GetEntityTypeIdAsync( string entityType )
        {
            return SqlScalarAsync<int?>( $"SELECT [Id] FROM [EntityType] WHERE [Name] = '{entityType}'" );
        }

        /// <summary>
        /// Gets the field type identifier.
        /// </summary>
        /// <param name="fieldType">Type of the field.</param>
        /// <returns></returns>
        public Task<int?> GetFieldTypeIdAsync( string fieldType )
        {
            return SqlScalarAsync<int?>( $"SELECT [Id] FROM [FieldType] WHERE [Class] = '{fieldType}'" );
        }

        /// <summary>
        /// Disables a single component with the given class name.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        public async Task DisableComponentTypeAsync( string componentType )
        {
            var entityTypeId = await GetEntityTypeIdAsync( componentType );

            if ( entityTypeId.HasValue )
            {
                await SqlCommandAsync( $@"UPDATE AV SET
    AV.[Value] = 'False'
    , AV.[PersistedTextValue] = 'False'
    , AV.[PersistedHtmlValue] = 'False'
    , AV.[PersistedCondensedTextValue] = 'False'
    , AV.[PersistedCondensedHtmlValue] = 'False'
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE AV.EntityId = 0
  AND A.[EntityTypeId] = {entityTypeId.Value}
  AND A.[Key] = 'Active'" );
            }
        }

        /// <summary>
        /// Deletes the attribute values for component.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        public async Task DeleteAttributeValuesForComponentTypeAsync( string componentType )
        {
            var entityTypeId = await GetEntityTypeIdAsync( componentType );

            if ( entityTypeId.HasValue )
            {
                await SqlCommandAsync( $@"DELETE AV
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE AV.EntityId = 0
  AND A.[EntityTypeId] = {entityTypeId.Value}" );
            }
        }

        /// <summary>
        /// Disables all the individual components of the given parent type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <param name="excludedTypes">The types to be excluded.</param>
        public async Task DisableComponentsOfTypeAsync( string componentType, string[] excludedTypes = null )
        {
            if ( !_knownComponentTypes.TryGetValue( componentType, out var types ) )
            {
                throw new Exception( $"Unknown component type '{componentType}'." );
            }

            foreach ( var type in types )
            {
                if ( excludedTypes == null || !excludedTypes.Contains( type ) )
                {
                    await DisableComponentTypeAsync( type );
                }
            }
        }

        /// <summary>
        /// Deletes the attribute values for the child components of the given component type.
        /// </summary>
        /// <param name="componentType">Type of the component.</param>
        /// <param name="excludedTypes">The types to be excluded.</param>
        public async Task DeleteAttributeValuesForComponentsOfTypeAsync( string componentType, string[] excludedTypes = null )
        {
            if ( !_knownComponentTypes.TryGetValue( componentType, out var types ) )
            {
                throw new Exception( $"Unknown component type '{componentType}'." );
            }

            foreach ( var type in types )
            {
                if ( excludedTypes == null || !excludedTypes.Contains( type ) )
                {
                    await DeleteAttributeValuesForComponentTypeAsync( type );
                }
            }
        }

        /// <summary>
        /// Gets the global attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task<string> GetGlobalAttributeValueAsync( string key )
        {
            var defaultValue = ( await SqlQueryAsync<int, string>( $"SELECT [Id], [DefaultValue] FROM [Attribute] WHERE [Key] = '{key}' AND [EntityTypeId] IS NULL" ) ).First();
            var value = await SqlScalarAsync<string>( $"SELECT [Value] FROM [AttributeValue] WHERE [AttributeId] = {defaultValue.Item1}" );

            return !string.IsNullOrEmpty( value ) ? value : defaultValue.Item2;
        }

        /// <summary>
        /// Sets the global attribute value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public async Task SetGlobalAttributeValue( string key, string value )
        {
            var attributeId = await SqlScalarAsync<int?>( $"SELECT [Id] FROM [Attribute] WHERE [Key] = '{key}' AND [EntityTypeId] IS NULL" );

            if ( !attributeId.HasValue )
            {
                return;
            }

            var attributeValueId = await SqlScalarAsync<int?>( $"SELECT [Id] FROM [AttributeValue] WHERE [AttributeId] = {attributeId.Value}" );
            var parameters = new Dictionary<string, object>
            {
                { "Value", value }
            };

            if ( attributeValueId.HasValue )
            {
                await SqlCommandAsync( $@"
UPDATE [AttributeValue] SET
    [Value] = @Value
    , [PersistedTextValue] = @Value
    , [PersistedHtmlValue] = @Value
    , [PersistedCondensedTextValue] = @Value
    , [PersistedCondensedHtmlValue] = @Value
WHERE [Id] = {attributeValueId.Value}", parameters );
            }
            else
            {
                await SqlCommandAsync( $@"
INSERT INTO [AttributeValue]
    ([IsSystem], [AttributeId], [Value], [PersistedTextValue], [PersistedHtmlValue], [PersistedCondensedTextValue], [PersistedCondensedHtmlValue], [Guid])
    VALUES
    (0, {attributeId.Value}, @Value, @Value, @Value, @Value, @Value, NEWID())", parameters );
            }
        }

        /// <summary>
        /// Sets the component attribute value by either updating the existing value or creating a new one.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="value">The value.</param>
        public Task SetComponentAttributeValue( string entityType, string attributeKey, string value )
        {
            return SqlCommandAsync( $@"DECLARE @AttributeId int = (SELECT A.[Id] FROM [Attribute] AS A INNER JOIN [EntityType] AS ET ON ET.[Id] = A.[EntityTypeId] WHERE ET.[Name] = '{entityType}' AND A.[Key] = '{attributeKey}')
IF EXISTS (SELECT * FROM [AttributeValue] WHERE [AttributeId] = @AttributeId)
BEGIN
    UPDATE [AttributeValue] SET
        [Value] = '{value}'
        , [PersistedTextValue] = '{value}'
        , [PersistedHtmlValue] = '{value}'
        , [PersistedCondensedTextValue] = '{value}'
        , [PersistedCondensedHtmlValue] = '{value}'
    WHERE [AttributeId] = @AttributeId AND [EntityId] = 0
END
ELSE
BEGIN
    INSERT INTO [AttributeValue]
        ([IsSystem], [AttributeId], [EntityId], [Value], [PersistedTextValue], [PersistedHtmlValue], [PersistedCondensedTextValue], [PersistedCondensedHtmlValue], [Guid])
        VALUES
        (0, @AttributeId, 0, '{value}', '{value}', '{value}', '{value}', '{value}', NEWID())
END" );
        }

        /// <summary>
        /// Gets the file data from rock.
        /// </summary>
        /// <param name="binaryFileId">The binary file identifier.</param>
        /// <returns></returns>
        public async Task<MemoryStream> GetFileDataFromRockAsync( int binaryFileId )
        {
            var url = $"{await GetFileUrlAsync()}?Id={binaryFileId}";
            var client = new WebClient();

            try
            {
                var ms = new MemoryStream();

                using ( var stream = await client.OpenReadTaskAsync( url ) )
                {
                    await stream.CopyToAsync( ms );
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
        public async Task<MemoryStream> GetFileDataFromRockAsync( Guid binaryFileGuid )
        {
            var url = $"{await GetFileUrlAsync()}?Guid={binaryFileGuid}";
            var client = new WebClient();

            try
            {
                var ms = new MemoryStream();

                using ( var stream = await client.OpenReadTaskAsync( url ) )
                {
                    await stream.CopyToAsync( ms );
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
        public async Task<MemoryStream> GetFileDataFromBinaryFileDataAsync( int binaryFileId )
        {
            var data = await SqlScalarAsync<byte[]>( $"SELECT [Content] FROM [BinaryFileData] WHERE [Id] = {binaryFileId}" );

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
        public async Task ScrubTableTextColumnAsync( string tableName, string columnName, Func<string, string> replacement, Action<double> progress = null )
        {
            await ScrubTableTextColumnsAsync( tableName, new[] { columnName }, replacement, progress );
        }

        /// <summary>
        /// Scrubs the specified table columns with the given replacement data.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="replacement">The replacement function to provide the new value.</param>
        /// <param name="step">The step number.</param>
        /// <param name="stepCount">The step count.</param>
        public async Task ScrubTableTextColumnsAsync( string tableName, IEnumerable<string> columnNames, Func<string, string> replacement, Action<double> progress = null )
        {
            string columns = string.Join( "], [", columnNames );
            var rowIds = await SqlQueryAsync<int>( $"SELECT [Id] FROM [{tableName}] ORDER BY [Id]" );

            CancellationToken.ThrowIfCancellationRequested();

            await ProcessItemsInParallelAsync( rowIds, 1_000, async ( itemIds ) =>
            {
                var rows = await SqlQueryAsync( $"SELECT [Id], [{columns}] FROM [{tableName}] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", itemIds )})" );
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                for ( int i = 0; i < rows.Count; i++ )
                {
                    int valueId = ( int ) rows[i]["Id"];
                    var changes = new Dictionary<string, object>();

                    foreach ( var c in columnNames )
                    {
                        var value = ( string ) rows[i][c];

                        if ( !string.IsNullOrWhiteSpace( value ) )
                        {
                            var newValue = replacement( value );

                            if ( value != newValue )
                            {
                                changes.Add( c, newValue );
                            }
                        }
                    }

                    if ( changes.Any() )
                    {
                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( valueId, changes ) );
                    }
                }

                await UpdateDatabaseRecordsAsync( tableName, bulkChanges );
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
            return EmailRegex.Replace( value, ( match ) =>
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
