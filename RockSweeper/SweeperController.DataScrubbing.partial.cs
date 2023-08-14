using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper
{
    public partial class SweeperController
    {
        #region Regular Expressions

        /// <summary>
        /// The regular expression to use when scanning for e-mail addresses in text.
        /// </summary>
        private Regex _scrubEmailRegex = new Regex( @"^\w+@([a-zA-Z_]+?\.)+?[a-zA-Z]{2,}$" );

        /// <summary>
        /// The regular expression to use when scanning for phone numbers. This is
        /// complex, but it should catch various forms of phone numbers, such as:
        /// 1 (555) 555-5555
        /// 555.555.5555
        /// 15555555555
        /// </summary>
        private Regex _scrubPhoneRegex = new Regex( @"(^|\D)((1?[2-9][0-9]{2}[2-9][0-9]{2}[0-9]{4}|(1 ?)?\([2-9][0-9]{2}\) ?[2-9][0-9]{2}\-[0-9]{4}|(1[\-\.])?([2-9][0-9]{2}[\-\.])?[2-9][0-9]{2}[\-\.][0-9]{4}|(1 )?[2-9][0-9]{2} [2-9][0-9]{2} [0-9]{4}))($|\D)", RegexOptions.Multiline );

        #endregion

        #region Scrubbed Tables

        /// <summary>
        /// The common tables that are scrubbed by various means.
        /// </summary>
        private Dictionary<string, string[]> _scrubCommonTables = new Dictionary<string, string[]>
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
        private Dictionary<string, string[]> _scrubEmailTables = new Dictionary<string, string[]>
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
        private Dictionary<string, string[]> _scrubPhoneTables = new Dictionary<string, string[]>
        {
            { "RegistrationInstance", new[] { "ContactPhone" } },
            { "EventItemOccurrence", new[] { "ContactPhone" } },
            { "BenevolenceRequest", new[] { "HomePhoneNumber", "CellPhoneNumber", "WorkPhoneNumber" } },
            { "Campus", new[] { "PhoneNumber" } }
        };

        #endregion

        #region Properties

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
        protected Dictionary<string, List<string>> LocationCityPostalCodes
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

        #region Helper Methods

        /// <summary>
        /// Scrubs the content of any e-mail addresses.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private string ScrubEmailAddressForContent( string value )
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
        private string ScrubPhoneNumberForContent( string value )
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
        private Dictionary<string, string[]> ScrubMergeTableDictionaries( params Dictionary<string, string[]>[] dictionaries )
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
        void UpdateLocationWithFakeData( int locationId, string street1, string street2, string county, string postalCode, string state, string country )
        {
            var changes = new Dictionary<string, object>();

            if ( country != "US" )
            {
                changes.Add( "Street1", DataFaker.Address.StreetAddress( false ) );
                changes.Add( "City", $"{ DataFaker.Address.City() } { DataFaker.Address.CitySuffix() }" );
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
                    changes.Add( "PostalCode", $"{ newPostal }-{ postalCode.Split( '-' )[1] }" );
                }
                else
                {
                    changes.Add( "PostalCode", newPostal );
                }
            }

            UpdateDatabaseRecord( "Location", locationId, changes );
        }

        #endregion

        /// <summary>
        /// Generates the random email addresses.
        /// </summary>
        [ActionId( "7d14924a-b367-4495-b480-96b86bcf712b" )]
        [Title( "Generate Random Email Addresses" )]
        [Description( "Replaces any e-mail addresses found in the system with generated values." )]
        [Category( "Data Scrubbing" )]
        public void GenerateRandomEmailAddresses()
        {
            var scrubTables = ScrubMergeTableDictionaries( _scrubCommonTables, _scrubEmailTables );
            int stepCount = 4 + scrubTables.Count - 1;

            //
            // Stage 1: Replace all Person e-mail addresses.
            // 51.26 single thread.
            // 10.43 multi-threaded.
            // 4.26 multi-threaded bulk update.
            //
            var peopleAddresses = SqlQuery<int, string>( "SELECT [Id], [Email] FROM [Person] WHERE [Email] IS NOT NULL AND [Email] != ''" );
            ProcessItemsInParallel( peopleAddresses, 1000, ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach (var p in items)
                {
                    var changes = new Dictionary<string, object>
                    {
                        { "Email", GenerateFakeEmailAddressForAddress( p.Item2 ) }
                    };

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( p.Item1, changes ) );
                }

                if ( bulkChanges.Any() )
                {
                    UpdateDatabaseRecords( "Person", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 1, stepCount );
            } );

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
            ProcessItemsInParallel( attributeValues, 1000, ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var av in items )
                {
                    var newValue = ScrubEmailAddressForContent( av.Item2 );

                    if ( newValue != av.Item2 )
                    {
                        var changes = new Dictionary<string, object>
                        {
                            { "Value", newValue }
                        };

                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( av.Item1, changes ) );
                    }
                }

                UpdateDatabaseRecords( "AttributeValue", bulkChanges );
            }, ( p ) =>
            {
                Progress( p, 2, stepCount );
            } );

            //
            // Stage 3: Scrub the global attributes.
            //
            var attributeValue = GetGlobalAttributeValue( "EmailExceptionsList" );
            SetGlobalAttributeValue( "EmailExceptionsList", ScrubEmailAddressForContent( attributeValue ) );
            attributeValue = GetGlobalAttributeValue( "OrganizationEmail" );
            SetGlobalAttributeValue( "OrganizationEmail", ScrubEmailAddressForContent( attributeValue ) );
            Progress( 1.0, 3, stepCount );

            //
            // Stage 4: Scan and replace e-mail addresses in misc data.
            //
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                ScrubTableTextColumns( tc.Key, tc.Value, ScrubEmailAddressForContent, 4 + tableStep, stepCount );
                tableStep++;
            }
        }

        /// <summary>
        /// Empties the analytics source tables.
        /// </summary>
        [ActionId( "8b6a47eb-006a-4734-a876-94f20b43b994" )]
        [Title( "Empty Analytics Source Tables" )]
        [Description( "Truncates the AnalyticsSource* tables so they contain no data." )]
        [Category( "Data Scrubbing" )]
        public void EmptyAnalyticsSourceTables()
        {
            var tables = SqlQuery<string>( "SELECT [name] FROM sys.all_objects WHERE [type_desc] = 'USER_TABLE' AND [name] LIKE 'AnalyticsSource%'" );

            foreach ( var table in tables )
            {
                SqlCommand( $"TRUNCATE TABLE [{ table }]" );
            }
        }

        /// <summary>
        /// Inserts the history placeholders.
        /// </summary>
        [ActionId( "5cdcf31e-382f-4663-ad26-122814990187" )]
        [Title( "Insert History Placeholders" )]
        [Description( "Modifies all History records to remove any identifying information." )]
        [Category( "Data Scrubbing" )]
        [AfterAction( nameof( GenerateRandomLogins ) )]
        public void InsertHistoryPlaceholders()
        {
            var fieldValueRegex = new Regex( "(<span class=['\"]field-value['\"]>)([^<]*)(<\\/span>)" );
            var loginFieldValueRegex = new Regex( "(.*logged in.*<span class=['\"]field-name['\"]>)([^<]*)(<\\/span>)" );

            var historyIds = SqlQuery<int>( $"SELECT [Id] FROM [History]" );

            void ProcessChunk(List<int> items)
            {
                var historyItems = SqlQuery( $"SELECT * FROM [History] WHERE [Id] IN ({ string.Join( ",", items ) })" );
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var history in historyItems )
                {
                    var changes = new Dictionary<string, object>();

                    //
                    // Scrub the Caption.
                    //
                    var caption = ( string ) history["Caption"];
                    if ( !string.IsNullOrWhiteSpace( caption ) )
                    {
                        var value = string.Join( " ", DataFaker.Lorem.Words( caption.Split( ' ' ).Length ) );

                        if ( value.Length > 200 )
                        {
                            value = value.Substring( 0, 200 );
                        }

                        changes.Add( "Caption", value );
                    }

                    //
                    // Scrub the Summary.
                    //
                    var summary = ( string ) history["Summary"];
                    if ( !string.IsNullOrWhiteSpace( summary ) )
                    {
                        var newValue = fieldValueRegex.Replace( summary, ( m ) =>
                        {
                            return $"{ m.Groups[1].Value }HIDDEN{ m.Groups[3].Value }";
                        } );

                        newValue = loginFieldValueRegex.Replace( newValue, ( m ) =>
                        {
                            return $"{ m.Groups[1].Value }HIDDEN{ m.Groups[3].Value }";
                        } );

                        if ( newValue != summary )
                        {
                            changes.Add( "Summary", newValue );
                        }
                    }

                    //
                    // Scrub the RelatedData to remove any mentions of the original values.
                    //
                    var relatedData = ( string ) history["RelatedData"];
                    if ( !string.IsNullOrWhiteSpace( relatedData ) )
                    {
                        var newValue = fieldValueRegex.Replace( relatedData, ( m ) =>
                        {
                            return $"{ m.Groups[1].Value }HIDDEN{ m.Groups[3].Value }";
                        } );

                        if ( newValue != relatedData )
                        {
                            changes.Add( "RelatedData", newValue );
                        }
                    }

                    //
                    // Scrub the OldValue.
                    //
                    if ( history.ContainsKey( "OldValue" ) && !string.IsNullOrWhiteSpace( ( string ) history["OldValue"] ) )
                    {
                        changes.Add( "OldValue", "HIDDEN" );
                    }

                    //
                    // Scrub the NewValue.
                    //
                    if ( history.ContainsKey( "NewValue" ) && !string.IsNullOrWhiteSpace( ( string ) history["NewValue"] ) )
                    {
                        changes.Add( "NewValue", "HIDDEN" );
                    }

                    // Scrub the OldValueRaw
                    if ( history.ContainsKey( "OldValueRaw" ) && !string.IsNullOrWhiteSpace( ( string ) history["OldValueRaw"] ) )
                    {
                        changes.Add( "OldValueRaw", "HIDDEN" );
                    }

                    // Scrub the NewValueRaw
                    if ( history.ContainsKey( "NewValueRaw" ) && !string.IsNullOrWhiteSpace( ( string ) history["NewValueRaw"] ) )
                    {
                        changes.Add( "NewValueRaw", "HIDDEN" );
                    }

                    //
                    // Scrub the ValueName.
                    //
                    var verb = ( string ) history["Verb"];
                    if ( verb == "ADDEDTOGROUP" || verb == "REMOVEDROMGROUP" || verb == "REGISTERED" || verb == "MERGE" )
                    {
                        changes.Add( "ValueName", "HIDDEN" );
                    }
                    else if ( verb == "LOGIN" && history.ContainsKey( "ValueName" ) )
                    {
                        var valueName = ( string ) history["ValueName"];
                        if ( !string.IsNullOrWhiteSpace( valueName ) && !valueName.StartsWith( "fakeuser" ) )
                        {
                            changes.Add( "ValueName", "HIDDEN" );
                        }
                    }

                    if ( changes.Any() )
                    {
                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( ( int ) history["Id"], changes ) );
                    }
                }

                if ( bulkChanges.Any() )
                {
                    UpdateDatabaseRecords( "History", bulkChanges );
                }
            }

            ProcessItemsInParallel( historyIds, 1000, ProcessChunk, ( p ) =>
            {
                Progress( p );
            } );
        }

        /// <summary>
        /// Generates the random logins.
        /// </summary>
        [ActionId( "304cf8ff-a70d-4de1-95d0-e4262cf1bfeb" )]
        [Title( "Generate Random Logins" )]
        [Description( "Replaces any login names found in the system with generated values." )]
        [Category( "Data Scrubbing" )]
        public void GenerateRandomLogins()
        {
            var logins = SqlQuery<int, string>( "SELECT [Id], [UserName] FROM [UserLogin]" );

            ProcessItemsInParallel( logins, 1000, ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var login in items )
                {
                    var changes = new Dictionary<string, object>
                    {
                        { "UserName", $"fakeuser{ login.Item1 }" }
                    };

                    if (login.Item2 != changes.First().Value.ToString())
                    {
                        bulkChanges.Add(new Tuple<int, Dictionary<string, object>>(login.Item1, changes));
                    }   
                }

                if (bulkChanges.Count() > 0)
                {
                    UpdateDatabaseRecords("UserLogin", bulkChanges);
                }
            }, ( p ) =>
            {
                Progress( p );
            } );
        }

        /// <summary>
        /// Clears the background check response data.
        /// </summary>
        [ActionId( "38c0aa94-f914-470a-9be1-ea3b6da14d41" )]
        [Title( "Sanitize Background Check Data" )]
        [Description( "Clears as much sensitive information from background checks as possible." )]
        [Category( "Data Scrubbing" )]
        [AfterAction( nameof( GenerateRandomNames ) )]
        public void SanitizeBackgroundCheckData()
        {
            int stepCount = 5;

            //
            // Step 1: Clear background check response data, which can contain sensitive information.
            //
            SqlCommand( "UPDATE [BackgroundCheck] SET [ResponseData] = ''" );
            Progress( 1, 1, stepCount );

            //
            // Step 2: Clear any links to PDFs from Protect My Ministry
            //
            SqlCommand( "UPDATE [AttributeValue] SET [Value] = 'HIDDEN' WHERE [Value] LIKE '%://services.priorityresearch.com%'" );
            Progress( 1, 2, stepCount );

            //
            // Step 3: Clear any background check field types.
            //
            int? backgroundCheckFieldTypeId = GetFieldTypeId( "Rock.Field.Types.BackgroundCheckFieldType" );
            if ( backgroundCheckFieldTypeId.HasValue )
            {
                SqlCommand( $"UPDATE AV SET AV.[Value] = '' FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] = { backgroundCheckFieldTypeId.Value }" );
                Progress( 1, 3, stepCount );
            }

            //
            // Step 4: Update name of any background check workflows.
            // This action is run after the action to randomize person names runs, so just update
            // the names to the new person name.
            //
            var backgroundCheckWorkflowTypeIds = SqlQuery<int>( @"
SELECT
	WT.[Id]
FROM [WorkflowType] AS WT
INNER JOIN [Attribute] AS APackageType ON APackageType.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND APackageType.[EntityTypeQualifierValue] = WT.[Id] AND APackageType.[Key] = 'PackageType'
INNER JOIN [Attribute] AS APerson ON APerson.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND APerson.[EntityTypeQualifierValue] = WT.[Id] AND APerson.[Key] = 'Person'
LEFT JOIN [Attribute] AS AReportRecommendation ON AReportRecommendation.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND AReportRecommendation.[EntityTypeQualifierValue] = WT.[Id] AND AReportRecommendation.[Key] = 'ReportRecommendation'
LEFT JOIN [Attribute] AS ASSN ON ASSN.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND ASSN.[EntityTypeQualifierValue] = WT.[Id] AND ASSN.[Key] = 'SSN'
WHERE AReportRecommendation.[Id] IS NOT NULL OR [ASSN].[Id] IS NOT NULL" );
            foreach ( var workflowTypeId in backgroundCheckWorkflowTypeIds )
            {
                SqlCommand( $@"
UPDATE W
	SET W.[Name] = P.[NickName] + ' ' + P.[LastName]
FROM [Workflow] AS W
INNER JOIN [AttributeValue] AS AVPerson ON AVPerson.[EntityId] = W.[Id]
INNER JOIN [Attribute] AS APerson ON APerson.[Id] = AVPerson.[AttributeId] AND APerson.[Key] = 'Person'
INNER JOIN [PersonAlias] AS PA ON PA.[Guid] = TRY_CAST(AVPerson.[Value] AS uniqueidentifier)
INNER JOIN [Person] AS P ON P.[Id] = PA.[PersonId]
WHERE W.[WorkflowTypeId] = { workflowTypeId }
  AND APerson.[EntityTypeQualifierColumn] = 'WorkflowTypeId'
  AND APerson.[EntityTypeQualifierValue] = W.[WorkflowTypeId]" );
            }
            Progress( 1, 4, stepCount );
        }

        /// <summary>
        /// Generates the random phone numbers.
        /// </summary>
        [ActionId( "46f34fe1-4577-425d-9cdf-cf54da349365" )]
        [Title( "Generate Random Phone Numbers" )]
        [Description( "Replaces any phone numbers found in the system with generated values." )]
        [Category( "Data Scrubbing" )]
        public void GenerateRandomPhoneNumbers()
        {
            var scrubTables = ScrubMergeTableDictionaries( _scrubCommonTables, _scrubPhoneTables );
            int stepCount = 4 + scrubTables.Count - 1;

            //
            // Stage 1: Replace all Person phone numbers.
            //
            var phoneNumbers = SqlQuery<int, string>( "SELECT [Id], [Number] FROM [PhoneNumber] WHERE [Number] != ''" );
            ProcessItemsInParallel( phoneNumbers, 1000, ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in items )
                {
                    var changes = new Dictionary<string, object>();
                    var phoneNumber = GenerateFakePhoneNumberForPhone( item.Item2 );
                    string numberFormatted;

                    if ( phoneNumber.Length == 10 )
                    {
                        numberFormatted = $"({ phoneNumber.Substring( 0, 3 ) }) { phoneNumber.Substring( 3, 4 ) }-{ phoneNumber.Substring( 7 ) }";
                    }
                    else if ( phoneNumber.Length == 7 )
                    {
                        numberFormatted = phoneNumber.Substring( 0, 3 ) + "-" + phoneNumber.Substring( 3 );
                    }
                    else
                    {
                        numberFormatted = phoneNumber;
                    }

                    changes.Add( "Number", phoneNumber );
                    changes.Add( "NumberFormatted", numberFormatted );

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( item.Item1, changes ) );
                }

                if ( bulkChanges.Any() )
                {
                    UpdateDatabaseRecords( "PhoneNumber", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 1, stepCount );
            } );

            //
            // Stage 2: Replace all AttributeValue phone numbers.
            //
            var fieldTypeIds = new List<int>
            {
                GetFieldTypeId( "Rock.Field.Types.TextFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.PhoneNumberFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.CodeEditorFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.HtmlFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.MarkdownFieldType" ).Value,
                GetFieldTypeId( "Rock.Field.Types.MemoFieldType" ).Value
            };

            var attributeValues = SqlQuery<int, string>( $"SELECT AV.[Id], AV.[Value] FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] IN ({ string.Join( ",", fieldTypeIds.Select( i => i.ToString() ) ) }) AND AV.[Value] != ''" );
            ProcessItemsInParallel( attributeValues, 1000, ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in items )
                {
                    var newValue = ScrubPhoneNumberForContent( item.Item2 );

                    if ( newValue != item.Item2 )
                    {
                        var changes = new Dictionary<string, object>
                        {
                            { "Value", newValue }
                        };

                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( item.Item1, changes ) );
                    }
                }

                if ( bulkChanges.Any() )
                {
                    UpdateDatabaseRecords( "AttributeValue", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 2, stepCount );
            } );

            //
            // Stage 3: Scrub the global attributes.
            //
            var attributeValue = GetGlobalAttributeValue( "OrganizationPhone" );
            SetGlobalAttributeValue( "OrganizationPhone", ScrubPhoneNumberForContent( attributeValue ) );
            Progress( 1.0, 3, stepCount );

            //
            // Stage 4: Scan and replace phone numbers in misc data.
            //
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                ScrubTableTextColumns( tc.Key, tc.Value, ScrubPhoneNumberForContent, 4 + tableStep, stepCount );
                tableStep++;
            }
        }

        /// <summary>
        /// Sanitizes the benevolence request data.
        /// </summary>
        [ActionId( "944fea3a-7826-4e9b-9539-eae3f26fa2ac" )]
        [Title( "Sanitize Benevolence Request Data" )]
        [Description( "Scrubs out any government IDs as well as request and result text." )]
        [Category( "Data Scrubbing" )]
        public void SanitizeBenevolenceRequestData()
        {
            var queryData = SqlQuery<int, string, string, string>( "SELECT [Id],[GovernmentId],[RequestText],[ResultSummary] FROM [BenevolenceRequest]" );
            var wordRegex = new Regex( "([a-zA-Z]+)" );

            for ( int i = 0; i < queryData.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item2 ) )
                {
                    changes.Add( "GovernmentId", DataFaker.Random.ReplaceNumbers( "GEN######" ) );
                }

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item3 ) )
                {
                    var value = wordRegex.Replace( queryData[i].Item3, ( m ) =>
                    {
                        return DataFaker.Lorem.Word();
                    } );

                    changes.Add( "RequestText", value );
                }

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item4 ) )
                {
                    var value = wordRegex.Replace( queryData[i].Item4, ( m ) =>
                    {
                        return DataFaker.Lorem.Word();
                    } );

                    changes.Add( "ResultSummary", value );
                }

                if ( changes.Any() )
                {
                    UpdateDatabaseRecord( "BenevolenceRequest", queryData[i].Item1, changes );
                }

                Progress( i / ( double ) queryData.Count );
            }
        }

        /// <summary>
        /// Generates the random names.
        /// </summary>
        [ActionId( "d000b0a2-c318-4d72-93df-ed60dd3a015c" )]
        [Title( "Generate Random Names" )]
        [Description( "Replaces any person names with randomized names." )]
        [Category( "Data Scrubbing" )]
        public void GenerateRandomNames()
        {
            var processedPersonIds = new List<int>();
            var processedFamilyIds = new List<int>();
            var businessGuid = new Guid( "BF64ADD3-E70A-44CE-9C4B-E76BBED37550" );
            var scrubTables = new Dictionary<string, string[]>
            {
                { "Communication", new[] { "FromName" } },
                { "CommunicationTemplate", new[] { "FromName" } },
                { "RegistrationTemplate", new[] { "ConfirmationFromName", "PaymentReminderFromName", "ReminderFromName", "RequestEntryName", "WaitListTransitionFromName" } },
                { "SystemEmail", new[] { "FromName" } }
            };
            int stepCount = 6 + scrubTables.Count - 1;

            //
            // Stage 1: Update Person table
            //
            var familyData = SqlQuery( @"SELECT
G.[Id] AS [FamilyId], P.[Id], P.[FirstName], P.[NickName], P.[MiddleName], P.[LastName], RT.[Guid] AS [RecordType], P.[Gender], G.[Name] AS [FamilyName]
FROM [Person] AS P
INNER JOIN [GroupMember] AS GM ON GM.[PersonId] = P.[Id]
INNER JOIN [Group] AS G ON G.[Id] = GM.[GroupId]
INNER JOIN [GroupType] AS GT ON GT.[Id] = G.[GroupTypeId]
INNER JOIN [DefinedValue] AS RT ON RT.[Id] = P.[RecordTypeValueId]
WHERE GT.[Guid] = '790E3215-3B10-442B-AF69-616C0DCB998E'
" ).GroupBy( p => ( int ) p["FamilyId"] ).ToList();

            for ( int i = 0; i < familyData.Count; i++ )
            {
                var familyId = familyData[i].Key;
                var lastNameLookup = new Dictionary<string, string>();

                foreach ( var person in familyData[i] )
                {
                    var changes = new Dictionary<string, object>();
                    var firstName = ( string ) person["FirstName"];
                    var nickName = ( string ) person["NickName"];
                    var middleName = ( string ) person["MiddleName"];
                    var lastName = ( string ) person["LastName"];
                    var recordType = ( Guid ) person["RecordType"];
                    int gender = ( int ) person["Gender"];
                    var familyName = ( string ) person["FamilyName"];

                    if ( processedPersonIds.Contains( ( int ) person["Id"] ) )
                    {
                        continue;
                    }
                    processedPersonIds.Add( ( int ) person["Id"] );

                    //
                    // Skip special names.
                    //
                    if ( lastName == "Administrator" || lastName == "Anonymous" || firstName == "Anonymous" )
                    {
                        continue;
                    }

                    if ( recordType == businessGuid )
                    {
                        if ( !lastNameLookup.ContainsKey( lastName ) )
                        {
                            lastNameLookup.Add( lastName, DataFaker.Name.LastName() + " " + DataFaker.Name.LastName() + " LLC" );
                        }

                        changes.Add( "LastName", lastNameLookup[lastName] );
                    }
                    else
                    {
                        if ( !lastNameLookup.ContainsKey( lastName ) )
                        {
                            lastNameLookup.Add( lastName, DataFaker.Name.LastName() );
                        }

                        changes.Add( "LastName", lastNameLookup[lastName] );

                        if ( !string.IsNullOrWhiteSpace( firstName ) )
                        {
                            if ( gender == 1 )
                            {
                                changes.Add( "FirstName", DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Male ) );
                            }
                            else if ( gender == 2 )
                            {
                                changes.Add( "FirstName", DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Female ) );
                            }
                            else
                            {
                                changes.Add( "FirstName", DataFaker.Name.FirstName() );
                            }
                        }

                        if ( !string.IsNullOrWhiteSpace( nickName ) )
                        {
                            if ( nickName == firstName )
                            {
                                changes.Add( "NickName", changes["FirstName"] );
                            }
                            else
                            {
                                if ( gender == 1 )
                                {
                                    changes.Add( "NickName", DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Male ) );
                                }
                                else if ( gender == 2 )
                                {
                                    changes.Add( "NickName", DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Female ) );
                                }
                                else
                                {
                                    changes.Add( "NickName", DataFaker.Name.FirstName() );
                                }
                            }
                        }

                        //
                        // Leave middle name as-is.
                        //
                    }

                    UpdateDatabaseRecord( "Person", ( int ) person["Id"], changes );

                    //
                    // Update family name.
                    //
                    if ( !processedFamilyIds.Contains( familyId ) && familyName.StartsWith( lastName ) )
                    {
                        processedFamilyIds.Add( familyId );

                        var familyChanges = new Dictionary<string, object>();

                        if ( familyName.EndsWith( " Family" ) )
                        {
                            familyChanges.Add( "Name", $"{ ( string ) changes["LastName"] } Family" );
                        }
                        else
                        {
                            familyChanges.Add( "Name", changes["LastName"] );
                        }

                        UpdateDatabaseRecord( "Group", familyId, familyChanges );
                    }
                }

                Progress( i / ( double ) familyData.Count, 1, stepCount );
            }

            //
            // Stage 2: Update BenevolenceRequest
            //
            var queryData = SqlQuery( @"SELECT
BR.[Id], P.[FirstName] AS [PersonFirstName], P.[LastName] AS [PersonLastName]
FROM [BenevolenceRequest] AS BR
LEFT JOIN [PersonAlias] AS PA ON PA.[Id] = BR.[RequestedByPersonAliasId]
LEFT JOIN [Person] AS P ON P.[Id] = PA.[PersonId]" );

            for ( int i = 0; i < queryData.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( queryData[i]["PersonFirstName"] != null )
                {
                    changes.Add( "FirstName", queryData[i]["PersonFirstName"] );
                }
                else
                {
                    changes.Add( "FirstName", DataFaker.Name.FirstName() );
                }

                if ( queryData[i]["PersonLastName"] != null )
                {
                    changes.Add( "LastName", queryData[i]["PersonLastName"] );
                }
                else
                {
                    changes.Add( "LastName", DataFaker.Name.LastName() );
                }

                UpdateDatabaseRecord( "BenevolenceRequest", ( int ) queryData[i]["Id"], changes );

                Progress( i / ( double ) queryData.Count, 2, stepCount );
            }

            //
            // Stage 3: Update PrayerRequest
            //
            queryData = SqlQuery( @"SELECT
PR.[Id], P.[FirstName] AS [PersonFirstName], P.[LastName] AS [PersonLastName]
FROM [PrayerRequest] AS PR
LEFT JOIN [PersonAlias] AS PA ON PA.[Id] = PR.[RequestedByPersonAliasId]
LEFT JOIN [Person] AS P ON P.[Id] = PA.[PersonId]" );

            for ( int i = 0; i < queryData.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( queryData[i]["PersonFirstName"] != null )
                {
                    changes.Add( "FirstName", queryData[i]["PersonFirstName"] );
                }
                else
                {
                    changes.Add( "FirstName", DataFaker.Name.FirstName() );
                }

                if ( queryData[i]["PersonLastName"] != null )
                {
                    changes.Add( "LastName", queryData[i]["PersonLastName"] );
                }
                else
                {
                    changes.Add( "LastName", DataFaker.Name.LastName() );
                }

                UpdateDatabaseRecord( "PrayerRequest", ( int ) queryData[i]["Id"], changes );

                Progress( i / ( double ) queryData.Count, 3, stepCount );
            }

            //
            // Stage 4: Update Registration
            //
            queryData = SqlQuery( @"SELECT
R.[Id], P.[FirstName] AS [PersonFirstName], P.[LastName] AS [PersonLastName]
FROM [Registration] AS R
LEFT JOIN [PersonAlias] AS PA ON PA.[Id] = R.[PersonAliasId]
LEFT JOIN [Person] AS P ON P.[Id] = PA.[PersonId]" );

            for ( int i = 0; i < queryData.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( queryData[i]["PersonFirstName"] != null )
                {
                    changes.Add( "FirstName", queryData[i]["PersonFirstName"] );
                }
                else
                {
                    changes.Add( "FirstName", DataFaker.Name.FirstName() );
                }

                if ( queryData[i]["PersonLastName"] != null )
                {
                    changes.Add( "LastName", queryData[i]["PersonLastName"] );
                }
                else
                {
                    changes.Add( "LastName", DataFaker.Name.LastName() );
                }

                UpdateDatabaseRecord( "PrayerRequest", ( int ) queryData[i]["Id"], changes );

                Progress( i / ( double ) queryData.Count, 4, stepCount );
            }

            //
            // Stage 5: Update PersonPreviousName
            //
            var previousNames = SqlQuery<int, int, string>( @"SELECT
PPN.[Id], PA.[PersonId], PPN.[LastName]
FROM PersonPreviousName AS PPN
INNER JOIN [PersonAlias] AS PA ON PA.[Id] = PPN.[PersonAliasId]
" ).GroupBy( p => p.Item2 ).ToList();

            for ( int i = 0; i < previousNames.Count; i++ )
            {
                var previousNameLookup = new Dictionary<string, string>();
                var personId = previousNames[i].Key;

                foreach ( var previousName in previousNames[i] )
                {
                    var changes = new Dictionary<string, object>();

                    if ( !previousNameLookup.ContainsKey( previousName.Item3 ) )
                    {
                        previousNameLookup.Add( previousName.Item3, DataFaker.Name.LastName() );
                    }

                    changes.Add( "LastName", previousNameLookup[previousName.Item3] );

                    UpdateDatabaseRecord( "PersonPreviousName", previousName.Item1, changes );
                }

                Progress( i / ( double ) previousNames.Count, 5, stepCount );
            }

            //
            // Stage 6: Update other tables
            //
            var fromNameLookup = new Dictionary<string, string>();
            string scrubFromName( string oldValue )
            {
                if ( oldValue.StartsWith( "{" ) )
                {
                    return oldValue;
                }

                if ( !fromNameLookup.ContainsKey( oldValue ) )
                {
                    fromNameLookup.Add( oldValue, DataFaker.Name.FullName() );
                }

                return fromNameLookup[oldValue];
            }
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                ScrubTableTextColumns( tc.Key, tc.Value, scrubFromName, 6 + tableStep, stepCount );
                tableStep++;
            }
        }

        /// <summary>
        /// Sanitizes the devices.
        /// </summary>
        [ActionId( "d23bfc10-7c58-4f9c-8497-553a6a5c7c1c" )]
        [Title( "Sanitize Devices" )]
        [Description( "Replacing IPAddress with fake address information." )]
        [Category( "Data Scrubbing" )]
        public void SanitizeDevices()
        {
            var devices = SqlQuery<int, string>( "SELECT [Id], [IPAddress] FROM [Device]" );

            foreach ( var device in devices )
            {
                var changes = new Dictionary<string, object>();

                if ( device.Item2 == "::1" || device.Item2 == "127.0.0.1" )
                {
                    continue;
                }

                if ( System.Net.IPAddress.TryParse( device.Item2, out System.Net.IPAddress ipAddress ) )
                {
                    ushort subAddress = ( ushort ) device.Item1;
                    var bytes = BitConverter.GetBytes( subAddress );

                    changes.Add( "IPAddress", $"172.16.{ bytes[1] }.{ bytes[0] }" );
                }
                else
                {
                    changes.Add( "IPAddress", $"device-{ device.Item1 }.rocksolidchurchdemo.com" );
                }

                UpdateDatabaseRecord( "Device", device.Item1, changes );
            }
        }

        /// <summary>
        /// Sanitizes the content channel items.
        /// </summary>
        [ActionId( "6ecb7b00-e1ed-44e9-b24e-e4beffbac9f3" )]
        [Title( "Sanitize Content Channel Items" )]
        [Description( "Replaces content channel item content with ipsum text." )]
        [Category( "Data Scrubbing" )]
        public void SanitizeContentChannelItems()
        {
            var contentChannelItems = SqlQuery<int, string>( "SELECT [Id], [Content] FROM [ContentChannelItem]" );
            var regex = new PCRE.PcreRegex( @"(<[^>]*>(*SKIP)(*F)|[^\W]\w+)" );

            for ( int i = 0; i < contentChannelItems.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( contentChannelItems[i].Item2 ) )
                {
                    var newValue = regex.Replace( contentChannelItems[i].Item2, ( m ) =>
                    {
                        return DataFaker.Lorem.Word();
                    } );

                    if ( newValue != contentChannelItems[i].Item2 )
                    {
                        changes.Add( "Content", newValue );
                    }
                }

                if ( changes.Any() )
                {
                    UpdateDatabaseRecord( "ContentChannelItem", contentChannelItems[i].Item1, changes );
                }

                Progress( i / ( double ) contentChannelItems.Count );
            }
        }

        /// <summary>
        /// Scrubs the workflow log.
        /// </summary>
        [ActionId( "d6a36aad-f7ba-4ada-b018-f6aa4065f6bf" )]
        [Title( "Scrub Workflow Log" )]
        [Description( "Modifies the log text to only include the activity and action and not the specific action text." )]
        [Category( "Data Scrubbing" )]
        public void ScrubWorkflowLog()
        {
            ScrubTableTextColumn( "WorkflowLog", "LogText", ( s ) =>
            {
                if ( s.Contains( ">" ) )
                {
                    var sections = s.Split( new[] { ':' }, 2 );

                    if ( sections.Length == 2 )
                    {
                        if ( sections[1] != " Activated" && sections[1] != " Processing..." && sections[1] != " Completed" )
                        {
                            return $"{ sections[0] }: HIDDEN";
                        }
                    }
                }

                return s;
            }, null, null );
        }

        /// <summary>
        /// Generates the organization and campuses.
        /// </summary>
        [ActionId( "7a047807-1a42-413e-899a-b2a1fc90a889" )]
        [Title( "Generate Organization and Campuses" )]
        [Description( "Scrubs the organization name and URL as well as campus names and URLs." )]
        [Category( "Data Scrubbing" )]
        public void GenerateOrganizationAndCampuses()
        {
            string organizationCity = DataFaker.PickRandom( LocationCityPostalCodes.Keys.ToList() );

            SetGlobalAttributeValue( "OrganizationName", $"{ organizationCity } Community Church" );
            SetGlobalAttributeValue( "OrganizationAbbreviation", $"{ organizationCity } Community Church" );
            SetGlobalAttributeValue( "OrganizationWebsite", $"http://www.{ organizationCity.Replace( " ", "" ).ToLower() }communitychurch.org/" );

            var campuses = SqlQuery<int, string, string, string>( "SELECT [Id], [Url], [Description], [ShortCode] FROM [Campus]" );
            foreach ( var campus in campuses )
            {
                var campusCityName = DataFaker.PickRandom( LocationCityPostalCodes.Keys.ToList() );

                var changes = new Dictionary<string, object>
                {
                    { "Name", campusCityName }
                };

                if ( !string.IsNullOrWhiteSpace( campus.Item2 ) )
                {
                    changes.Add( "Url", $"http://{ changes["Name"].ToString().Replace( " ", "" ).ToLower() }.{ organizationCity.Replace( " ", "" ).ToLower() }communitychurch.org/" );
                }

                if ( !string.IsNullOrWhiteSpace( campus.Item3 ) )
                {
                    changes.Add( "Description", DataFaker.Lorem.Sentence() );
                }

                if ( !string.IsNullOrWhiteSpace( campus.Item4 ) )
                {
                    changes.Add( "ShortCode", campusCityName.Substring( 0, 3 ).ToUpper() );
                }

                UpdateDatabaseRecord( "Campus", campus.Item1, changes );
            }
        }

        /// <summary>
        /// Sanitizes the interaction data.
        /// </summary>
        [ActionId( "31200ef0-cad7-4775-8be8-c1a06a591e3f" )]
        [Title( "Sanitize Interaction Data" )]
        [Description( "Removes all custom data from Interactions, InteractionComponents and InteractionChannels." )]
        [Category( "Data Scrubbing" )]
        public void SanitizeInteractionData()
        {
            SqlCommand( "UPDATE [InteractionChannel] SET [ChannelData] = NULL" );
            SqlCommand( "UPDATE [InteractionComponent] SET [ComponentData] = NULL" );
            SqlCommand( "UPDATE [Interaction] SET [InteractionData] = NULL" );
        }

        /// <summary>
        /// Generates random location addresses.
        /// </summary>
        [ActionId( "5b110e1e-dde1-49e2-922e-c604083e8679" )]
        [Title( "Generate Random Location Addresses" )]
        [Description( "Generates random addresses in the database, centered around Phoenix, AZ." )]
        [Category( "Data Scrubbing" )]
        [RequiresLocationService]
        public void GenerateRandomLocationAddresses()
        {
            int stepCount = 3;

            //
            // Step 1: Process all locations that are not geo-coded.
            //
            var locations = SqlQuery( "SELECT [Id], [Street1], [Street2], [County], [PostalCode], [State], [Country] FROM [Location] WHERE ISNULL([Street1], '') != '' AND ISNULL([City], '') != '' AND [GeoPoint] IS NULL" );
            for ( int i = 0; i < locations.Count; i++ )
            {
                var locationId = ( int ) locations[i]["Id"];
                var street1 = ( string ) locations[i]["Street1"];
                var street2 = ( string ) locations[i]["Street2"];
                var county = ( string ) locations[i]["County"];
                var postalCode = ( string ) locations[i]["PostalCode"];
                var state = ( string ) locations[i]["State"];
                var country = ( string ) locations[i]["Country"];

                UpdateLocationWithFakeData( locationId, street1, street2, county, postalCode, state, country );

                Progress( i / ( double ) locations.Count, 1, stepCount );
            }

            double radiusDistance = 35 * 1609.344;
            var centerLocationGuid = GetGlobalAttributeValue( "OrganizationAddress" );
            var centerLocation = new Coordinates( SqlQuery<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{ centerLocationGuid }'" ).First() );
            var targetCenterLocation = new Coordinates( Properties.Settings.Default.TargetGeoCenter );
            var adjustCoordinates = new Coordinates( targetCenterLocation.Latitude - centerLocation.Latitude, targetCenterLocation.Longitude - centerLocation.Longitude );

            //
            // Step 2: Move all locations with a valid GeoPoint inside our radius.
            //
            var geoLocations = SqlQuery( $"SELECT [Id], [GeoPoint].Lat AS [Latitude], [GeoPoint].Long AS [Longitude], [Street1], [Street2], [City], [County], [PostalCode], [State], [Country] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({ centerLocation.Latitude }, { centerLocation.Longitude }, 4326).STDistance([GeoPoint]) < { radiusDistance }" );
            var step2Changes = new Dictionary<string, object>();
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = ( int ) geoLocations[i]["Id"];
                var latitude = ( double ) geoLocations[i]["Latitude"];
                var longitude = ( double ) geoLocations[i]["Longitude"];
                var street1 = ( string ) geoLocations[i]["Street1"];
                var street2 = ( string ) geoLocations[i]["Street2"];
                var city = ( string ) geoLocations[i]["City"];
                var county = ( string ) geoLocations[i]["County"];
                var postalCode = ( string ) geoLocations[i]["PostalCode"];
                var state = ( string ) geoLocations[i]["State"];
                var country = ( string ) geoLocations[i]["Country"];

                CancellationToken.ThrowIfCancellationRequested();
                step2Changes.Clear();

                var coordinates = new Coordinates( latitude, longitude ).CoordinatesByAdjusting( adjustCoordinates.Latitude, adjustCoordinates.Longitude );

                if ( Properties.Settings.Default.JitterAddresses )
                {
                    //
                    // Jitter the coordinates by +/- one mile.
                    //
                    coordinates = coordinates.CoordinatesByAdjusting( DataFaker.Random.Double( -0.0144927, 0.0144927 ), DataFaker.Random.Double( -0.0144927, 0.0144927 ) );
                }

                var address = GetBestAddressForCoordinates( coordinates );

                step2Changes.Add( "GeoPoint", coordinates );

                if ( !string.IsNullOrWhiteSpace( street1 ) )
                {
                    step2Changes.Add( "Street1", address.Street1 );
                }

                if ( !string.IsNullOrWhiteSpace( city ) )
                {
                    step2Changes.Add( "City", address.City );
                }

                if ( !string.IsNullOrWhiteSpace( county ) )
                {
                    step2Changes.Add( "County", address.Country );
                }

                if ( !string.IsNullOrWhiteSpace( postalCode ) )
                {
                    step2Changes.Add( "PostalCode", address.PostalCode );
                }

                if ( !string.IsNullOrWhiteSpace( state ) )
                {
                    step2Changes.Add( "State", address.State );
                }

                if ( !string.IsNullOrWhiteSpace( country ) )
                {
                    step2Changes.Add( "Country", address.Country );
                }

                UpdateDatabaseRecord( "Location", locationId, step2Changes );

                Progress( i / ( double ) geoLocations.Count, 2, stepCount );
            }

            //
            // Step 3: Add a 1-mile jitter to any address outside our radius.
            //
            geoLocations = SqlQuery( $"SELECT [Id], [GeoPoint].Lat AS [Latitude], [GeoPoint].Long AS [Longitude], [Street1], [Street2], [City], [County], [PostalCode], [State], [Country] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({ centerLocation.Latitude }, { centerLocation.Longitude }, 4326).STDistance([GeoPoint]) >= { radiusDistance }" );
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = ( int ) geoLocations[i]["Id"];
                var latitude = ( double ) geoLocations[i]["Latitude"];
                var longitude = ( double ) geoLocations[i]["Longitude"];
                var street1 = ( string ) geoLocations[i]["Street1"];
                var street2 = ( string ) geoLocations[i]["Street2"];
                var city = ( string ) geoLocations[i]["City"];
                var county = ( string ) geoLocations[i]["County"];
                var postalCode = ( string ) geoLocations[i]["PostalCode"];
                var state = ( string ) geoLocations[i]["State"];
                var country = ( string ) geoLocations[i]["Country"];

                if ( Properties.Settings.Default.JitterAddresses )
                {
                    var changes = new Dictionary<string, object>();

                    var coordinates = new Coordinates( latitude, longitude );
                    coordinates = coordinates.CoordinatesByAdjusting( DataFaker.Random.Double( -0.0144927, 0.0144927 ), DataFaker.Random.Double( -0.0144927, 0.0144927 ) );

                    var address = GetBestAddressForCoordinates( coordinates );

                    changes.Add( "GeoPoint", coordinates );

                    if ( !string.IsNullOrWhiteSpace( street1 ) )
                    {
                        changes.Add( "Street1", address.Street1 );
                    }

                    if ( !string.IsNullOrWhiteSpace( city ) )
                    {
                        changes.Add( "City", address.City );
                    }

                    if ( !string.IsNullOrWhiteSpace( county ) )
                    {
                        changes.Add( "County", address.Country );
                    }

                    if ( !string.IsNullOrWhiteSpace( postalCode ) )
                    {
                        changes.Add( "PostalCode", address.PostalCode );
                    }

                    if ( !string.IsNullOrWhiteSpace( state ) )
                    {
                        changes.Add( "State", address.State );
                    }

                    if ( !string.IsNullOrWhiteSpace( country ) )
                    {
                        changes.Add( "Country", address.Country );
                    }

                    UpdateDatabaseRecord( "Location", locationId, changes );
                }
                else
                {
                    UpdateLocationWithFakeData( locationId, street1, street2, county, postalCode, state, country );
                }

                Progress( i / ( double ) geoLocations.Count, 3, stepCount );
            }
        }

        /// <summary>
        /// Shuffles the location addresses.
        /// </summary>
        [ActionId( "8b164228-50a7-4abe-8769-096ea5157a88" )]
        [Title( "Shuffle Location Addresses" )]
        [Description( "Takes the location addresses in the database and shuffles them all around." )]
        [Category( "Data Scrubbing" )]
        public void ShuffleLocationAddresses()
        {
            List<int> idNumbers;
            int stepCount = 3;

            //
            // Step 1: Shuffle all locations that are not geo-coded.
            //
            var locations = SqlQuery( "SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode] FROM [Location] WHERE ISNULL([Street1], '') != '' AND ISNULL([City], '') != '' AND [GeoPoint] IS NULL" );
            idNumbers = locations.Select( l => ( int ) l["Id"] ).ToList();
            for ( int i = 0; i < locations.Count; i++ )
            {
                var locationId = DataFaker.PickRandom( idNumbers );
                idNumbers.Remove( locationId );

                locations[i].Remove( "Id" );

                UpdateDatabaseRecord( "Location", locationId, locations[i] );

                Progress( i / ( double ) locations.Count, 1, stepCount );
            }

            //
            // Step 2: Shuffle all locations with a valid GeoPoint inside our radius.
            //
            double radiusDistance = 35 * 1609.344;
            var centerLocationGuid = GetGlobalAttributeValue( "OrganizationAddress" );
            var centerLocationValues = SqlQuery<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" );
            var centerLocation = centerLocationValues.Any()
                ? new Coordinates( SqlQuery<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" ).First() )
                : null;

            var geoLocations = centerLocation != null
                ? SqlQuery( $"SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode], [GeoPoint].Lat AS [Lat], [GeoPoint].Long AS [Long] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) < {radiusDistance}" )
                : new List<Dictionary<string, object>>();

            idNumbers = geoLocations.Select( l => ( int ) l["Id"] ).ToList();
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = DataFaker.PickRandom( idNumbers );
                idNumbers.Remove( locationId );

                geoLocations[i].Remove( "Id" );
                geoLocations[i].Add( "GeoPoint", new Coordinates( ( double ) geoLocations[i]["Lat"], ( double ) geoLocations[i]["Long"] ) );
                geoLocations[i].Remove( "Lat" );
                geoLocations[i].Remove( "Long" );

                UpdateDatabaseRecord( "Location", locationId, geoLocations[i] );

                Progress( i / ( double ) geoLocations.Count, 2, stepCount );
            }

            //
            // Step 3: Shuffle all locations with a valid GeoPoint outside our radius.
            //
            geoLocations = centerLocation != null
                ? SqlQuery( $"SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode], [GeoPoint].Lat AS [Lat], [GeoPoint].Long AS [Long] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) >= {radiusDistance}" )
                : new List<Dictionary<string, object>>();

            idNumbers = geoLocations.Select( l => ( int ) l["Id"] ).ToList();
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = DataFaker.PickRandom( idNumbers );
                idNumbers.Remove( locationId );

                geoLocations[i].Remove( "Id" );
                geoLocations[i].Add( "GeoPoint", new Coordinates( ( double ) geoLocations[i]["Lat"], ( double ) geoLocations[i]["Long"] ) );
                geoLocations[i].Remove( "Lat" );
                geoLocations[i].Remove( "Long" );

                UpdateDatabaseRecord( "Location", locationId, geoLocations[i] );

                Progress( i / ( double ) geoLocations.Count, 3, stepCount );
            }
        }

        /// <summary>
        /// Empties the saved account data.
        /// </summary>
        [ActionId( "702834d4-ca31-4ddb-bbac-ce629edbb82d" )]
        [Title( "Empty Saved Account Tables" )]
        [Description( "Clears the saved bank account and saved CC account tables." )]
        [Category( "Data Scrubbing" )]
        public void EmptySavedAccountTables()
        {
            SqlCommand( "TRUNCATE TABLE [FinancialPersonBankAccount]" );
            SqlCommand( "TRUNCATE TABLE [FinancialPersonSavedAccount]" );
        }
    }
}
