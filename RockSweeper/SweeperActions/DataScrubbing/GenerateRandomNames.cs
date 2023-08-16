using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Generates the random names.
    /// </summary>
    [ActionId( "d000b0a2-c318-4d72-93df-ed60dd3a015c" )]
    [Title( "Generate Random Names" )]
    [Description( "Replaces any person names with randomized names." )]
    [Category( "Data Scrubbing" )]
    public class GenerateRandomNames : SweeperAction
    {
        public override Task ExecuteAsync()
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
            var familyData = Sweeper.SqlQuery( @"SELECT
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
                            lastNameLookup.Add( lastName, Sweeper.DataFaker.Name.LastName() + " " + Sweeper.DataFaker.Name.LastName() + " LLC" );
                        }

                        changes.Add( "LastName", lastNameLookup[lastName] );
                    }
                    else
                    {
                        if ( !lastNameLookup.ContainsKey( lastName ) )
                        {
                            lastNameLookup.Add( lastName, Sweeper.DataFaker.Name.LastName() );
                        }

                        changes.Add( "LastName", lastNameLookup[lastName] );

                        if ( !string.IsNullOrWhiteSpace( firstName ) )
                        {
                            if ( gender == 1 )
                            {
                                changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Male ) );
                            }
                            else if ( gender == 2 )
                            {
                                changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Female ) );
                            }
                            else
                            {
                                changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
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
                                    changes.Add( "NickName", Sweeper.DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Male ) );
                                }
                                else if ( gender == 2 )
                                {
                                    changes.Add( "NickName", Sweeper.DataFaker.Name.FirstName( Bogus.DataSets.Name.Gender.Female ) );
                                }
                                else
                                {
                                    changes.Add( "NickName", Sweeper.DataFaker.Name.FirstName() );
                                }
                            }
                        }

                        //
                        // Leave middle name as-is.
                        //
                    }

                    Sweeper.UpdateDatabaseRecord( "Person", ( int ) person["Id"], changes );

                    //
                    // Update family name.
                    //
                    if ( !processedFamilyIds.Contains( familyId ) && familyName.StartsWith( lastName ) )
                    {
                        processedFamilyIds.Add( familyId );

                        var familyChanges = new Dictionary<string, object>();

                        if ( familyName.EndsWith( " Family" ) )
                        {
                            familyChanges.Add( "Name", $"{( string ) changes["LastName"]} Family" );
                        }
                        else
                        {
                            familyChanges.Add( "Name", changes["LastName"] );
                        }

                        Sweeper.UpdateDatabaseRecord( "Group", familyId, familyChanges );
                    }
                }

                Progress( i / ( double ) familyData.Count, 1, stepCount );
            }

            //
            // Stage 2: Update BenevolenceRequest
            //
            var queryData = Sweeper.SqlQuery( @"SELECT
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
                    changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
                }

                if ( queryData[i]["PersonLastName"] != null )
                {
                    changes.Add( "LastName", queryData[i]["PersonLastName"] );
                }
                else
                {
                    changes.Add( "LastName", Sweeper.DataFaker.Name.LastName() );
                }

                Sweeper.UpdateDatabaseRecord( "BenevolenceRequest", ( int ) queryData[i]["Id"], changes );

                Progress( i / ( double ) queryData.Count, 2, stepCount );
            }

            //
            // Stage 3: Update PrayerRequest
            //
            queryData = Sweeper.SqlQuery( @"SELECT
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
                    changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
                }

                if ( queryData[i]["PersonLastName"] != null )
                {
                    changes.Add( "LastName", queryData[i]["PersonLastName"] );
                }
                else
                {
                    changes.Add( "LastName", Sweeper.DataFaker.Name.LastName() );
                }

                Sweeper.UpdateDatabaseRecord( "PrayerRequest", ( int ) queryData[i]["Id"], changes );

                Progress( i / ( double ) queryData.Count, 3, stepCount );
            }

            //
            // Stage 4: Update Registration
            //
            queryData = Sweeper.SqlQuery( @"SELECT
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
                    changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
                }

                if ( queryData[i]["PersonLastName"] != null )
                {
                    changes.Add( "LastName", queryData[i]["PersonLastName"] );
                }
                else
                {
                    changes.Add( "LastName", Sweeper.DataFaker.Name.LastName() );
                }

                Sweeper.UpdateDatabaseRecord( "PrayerRequest", ( int ) queryData[i]["Id"], changes );

                Progress( i / ( double ) queryData.Count, 4, stepCount );
            }

            //
            // Stage 5: Update PersonPreviousName
            //
            var previousNames = Sweeper.SqlQuery<int, int, string>( @"SELECT
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
                        previousNameLookup.Add( previousName.Item3, Sweeper.DataFaker.Name.LastName() );
                    }

                    changes.Add( "LastName", previousNameLookup[previousName.Item3] );

                    Sweeper.UpdateDatabaseRecord( "PersonPreviousName", previousName.Item1, changes );
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
                    fromNameLookup.Add( oldValue, Sweeper.DataFaker.Name.FullName() );
                }

                return fromNameLookup[oldValue];
            }
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                Sweeper.ScrubTableTextColumns( tc.Key, tc.Value, scrubFromName, p =>
                {
                    Progress( p, 6 + tableStep, stepCount );
                } );

                tableStep++;
            }

            return Task.CompletedTask;
        }
    }
}
