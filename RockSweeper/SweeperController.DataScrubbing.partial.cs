using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RockSweeper
{
    public partial class SweeperController
    {
        private Regex _scrubEmailRegex = new Regex( @"^\w+@([a-zA-Z_]+?\.)+?[a-zA-Z]{2,}$" );
        private Regex _scrubPhoneRegex = new Regex( @"(^|\D)((1?[2-9][0-9]{2}[2-9][0-9]{2}[0-9]{4}|(1 ?)?\([2-9][0-9]{2}\) ?[2-9][0-9]{2}\-[0-9]{4}|(1[\-\.])?([2-9][0-9]{2}[\-\.])?[2-9][0-9]{2}[\-\.][0-9]{4}|(1 )?[2-9][0-9]{2} [2-9][0-9]{2} [0-9]{4}))($|\D)", RegexOptions.Multiline );

        private Dictionary<string, string[]> _scrubCommonTables = new Dictionary<string, string[]>
        {
            { "BenevolenceRequest", new[] { "RequestText", "ResultSummary" } },
            { "Communication", new[] { "Message" } },
            { "Note", new[] { "Text" } },
            { "HtmlContent", new[] { "Content" } },
            { "Group", new[] { "Description" } }
        };
        private Dictionary<string, string[]> _scrubEmailTables = new Dictionary<string, string[]>
        {
            { "BenevolenceRequest", new[] { "Email" } },
            { "Communication", new[] { "FromEmail", "ReplyToEmail", "CCEmails", "BCCEmails" } },
            { "CommunicationTemplate", new[] { "FromEmail", "ReplyToEmail", "CCEmails", "BCCEmails" } },
            { "EventItemOccurrence", new[] { "ContactEmail" } },
            { "PrayerRequest", new[] { "Email" } },
            { "Registration", new[] { "ConfirmationEmail" } },
            { "RegistrationTemplate", new[] { "ConfirmationFromEmail", "ReminderFromEmail", "PaymentReminderFromEmail", "WaitListTransitionFromEmail" } },
            { "ServiceJob", new[] { "NotificationEmails" } }
        };
        private Dictionary<string, string[]> _scrubPhoneTables = new Dictionary<string, string[]>
        {
            { "RegistrationInstance", new[] { "ContactPhone" } },
            { "EventItemOccurrence", new[] { "ContactPhone" } },
            { "BenevolenceRequest", new[] { "HomePhoneNumber", "CellPhoneNumber", "WorkPhoneNumber" } },
            { "Campus", new[] { "PhoneNumber" } }
        };

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

        #endregion

        /// <summary>
        /// Generates the random email addresses.
        /// </summary>
        public void GenerateRandomEmailAddresses()
        {
            var scrubTables = ScrubMergeTableDictionaries( _scrubCommonTables, _scrubEmailTables );
            int stepCount = 4 + scrubTables.Count - 1;

            //
            // Stage 1: Replace all Person e-mail addresses.
            //
            var peopleAddresses = SqlQuery<int, string>( "SELECT [Id], [Email] FROM [Person] WHERE [Email] IS NOT NULL AND [Email] != ''" );
            for ( int i = 0; i < peopleAddresses.Count; i++ )
            {
                int personId = peopleAddresses[i].Item1;
                string email = GenerateFakeEmailAddressForAddress( peopleAddresses[i].Item2 );

                SqlCommand( $"UPDATE [Person] SET [Email] = '{ email }' WHERE [Id] = { personId }" );

                Progress( i / ( double ) peopleAddresses.Count, 1, stepCount );
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

                var newValue = ScrubEmailAddressForContent( value );

                if ( value != newValue )
                {
                    SqlCommand( $"UPDATE [AttributeValue] SET [Value] = @Value WHERE [Id] = { valueId }", new Dictionary<string, object>
                    {
                        { "Value", newValue }
                    } );
                }

                Progress( i / ( double ) attributeValues.Count, 2, stepCount );
            }

            //
            // Stage 3: Scrub the global attributes.
            //
            var attributeValue = SqlScalar<string>( "SELECT [DefaultValue] FROM [Attribute] WHERE [Key] = 'EmailExceptionsList' AND [EntityTypeId] IS NULL" );
            SetGlobalAttributeValue( "EmailExceptionsList", ScrubEmailAddressForContent( attributeValue ) );
            attributeValue = SqlScalar<string>( "SELECT [DefaultValue] FROM [Attribute] WHERE [Key] = 'OrganizationEmail' AND [EntityTypeId] IS NULL" );
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
        public void EmptyAnalyticsSourceTables()
        {
            var tables = SqlQuery<string>( "SELECT [name] FROM sys.all_objects WHERE [type_desc] = 'USER_TABLE' AND [name] LIKE 'AnalyticsSource%'" );

            foreach ( var table in tables )
            {
                SqlCommand( $"TRUNCATE TABLE [{ table }]");
            }
        }

        /// <summary>
        /// Inserts the history placeholders.
        /// </summary>
        public void InsertHistoryPlaceholders()
        {
            var histories = SqlQuery( "SELECT * FROM [History]" );
            var fieldValueRegex = new Regex( "(<span class=['\"]field-value['\"]>)([^<]*)(<\\/span>)" );
            var loginFieldValueRegex = new Regex( "(.*logged in.*<span class=['\"]field-name['\"]>)([^<]*)(<\\/span>)" );

            for ( int i = 0; i < histories.Count; i++ )
            {
                var changes = new Dictionary<string, object>();
                var history = histories[i];

                if ( i % 100 == 0 )
                {
                    Progress( i / ( double ) histories.Count );
                }

                //
                // Scrub the Caption.
                //
                var caption = ( string ) history["Caption"];
                if ( !string.IsNullOrWhiteSpace( caption ) )
                {
                    changes.Add( "Caption", string.Join( " ", DataFaker.Lorem.Words( caption.Split( ',' ).Length ) ) );
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
                if ( !string.IsNullOrWhiteSpace( ( string ) history["OldValue"] ) )
                {
                    changes.Add( "OldValue", "HIDDEN" );
                }

                //
                // Scrub the NewValue.
                //
                if ( !string.IsNullOrWhiteSpace( ( string ) history["NewValue"] ) )
                {
                    changes.Add( "NewValue", "HIDDEN" );
                }

                //
                // Scrub the ValueName.
                //
                var verb = ( string ) history["Verb"];
                var valueName = ( string ) history["ValueName"];
                if ( verb == "ADDEDTOGROUP" || verb == "REMOVEDROMGROUP" || verb == "REGISTERED" || verb == "MERGE" )
                {
                    changes.Add( "ValueName", "HIDDEN" );
                }
                else if ( verb == "LOGIN" )
                {
                    if ( !string.IsNullOrWhiteSpace( valueName ) && valueName.StartsWith( "fakeuser" ) )
                    {
                        changes.Add( "ValueName", "HIDDEN" );
                    }
                }

                if ( changes.Any() )
                {
                    UpdateDatabaseRecord( "History", ( int ) history["Id"], changes );
                }
            }
        }

        /// <summary>
        /// Generates the random logins.
        /// </summary>
        public void GenerateRandomLogins()
        {
            var logins = SqlQuery<int, string>( "SELECT [Id], [UserName] FROM [UserLogin]" );

            for ( int i = 0; i < logins.Count; i++ )
            {
                var login = logins[i];

                if ( i % 100 == 0 )
                {
                    Progress( i / ( double ) logins.Count );
                }

                SqlCommand( $"UPDATE [UserLogin] SET [UserName] = '{ GenerateFakeLoginForLogin( login.Item2 ) }' WHERE [Id] = { login.Item1 }" );
            }
        }

        /// <summary>
        /// Clears the background check response data.
        /// </summary>
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
            int backgroundCheckFieldTypeId = GetFieldTypeId( "Rock.Field.Types.BackgroundCheckFieldType" ).Value;
            SqlCommand( $"UPDATE AV SET AV.[Value] = '' FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] = { backgroundCheckFieldTypeId }" );
            Progress( 1, 3, stepCount );

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
        public void GenerateRandomPhoneNumbers()
        {
            var scrubTables = ScrubMergeTableDictionaries( _scrubCommonTables, _scrubPhoneTables );
            int stepCount = 4 + scrubTables.Count - 1;

            //
            // Stage 1: Replace all Person phone numbers.
            //
            var phoneNumbers = SqlQuery<int, string>( "SELECT [Id], [Number] FROM [PhoneNumber] WHERE [Number] != ''" );
            for ( int i = 0; i < phoneNumbers.Count; i++ )
            {
                int phoneNumberId = phoneNumbers[i].Item1;
                string phoneNumber = GenerateFakePhoneNumberForPhone( phoneNumbers[i].Item2 );
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

                SqlCommand( $"UPDATE [PhoneNumber] SET [Number] = '{ phoneNumber }', [NumberFormatted] = '{ numberFormatted }' WHERE [Id] = { phoneNumberId }" );

                Progress( i / ( double ) phoneNumbers.Count, 1, stepCount );
            }

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
            for ( int i = 0; i < attributeValues.Count; i++ )
            {
                int valueId = attributeValues[i].Item1;
                string value = attributeValues[i].Item2;

                var newValue = ScrubPhoneNumberForContent( value );

                if ( value != newValue )
                {
                    SqlCommand( $"UPDATE [AttributeValue] SET [Value] = @Value WHERE [Id] = { valueId }", new Dictionary<string, object>
                    {
                        { "Value", newValue }
                    } );
                }

                Progress( i / ( double ) attributeValues.Count, 2, stepCount );
            }

            //
            // Stage 3: Scrub the global attributes.
            //
            var attributeValue = SqlScalar<string>( "SELECT [DefaultValue] FROM [Attribute] WHERE [Key] = 'OrganizationPhone' AND [EntityTypeId] IS NULL" );
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

        // TODO: Scrub attribute values.
        // TODO: Organization Name, Organization Address, Organization Website
    }
}
