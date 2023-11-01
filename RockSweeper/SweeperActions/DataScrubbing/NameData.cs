using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Replaces any person names with randomized names.
    /// </summary>
    [ActionId( "d000b0a2-c318-4d72-93df-ed60dd3a015c" )]
    [Title( "Names" )]
    [Description( "Replaces any person names with randomized names." )]
    [Category( "Data Scrubbing" )]
    public class NameData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var processedPersonIds = new List<int>();
            var processedFamilyIds = new List<int>();
            var businessGuid = new Guid( "BF64ADD3-E70A-44CE-9C4B-E76BBED37550" );
            int stepCount = 6 + Sweeper.ScrubNameTables.Count - 1;
            CountProgressReporter reporter;

            //
            // Stage 1: Update Person table
            //
            var familyData = ( await Sweeper.SqlQueryAsync( @"SELECT
G.[Id] AS [FamilyId], P.[Id], P.[FirstName], P.[NickName], P.[MiddleName], P.[LastName], RT.[Guid] AS [RecordType], P.[Gender], G.[Name] AS [FamilyName]
FROM [Person] AS P
INNER JOIN [GroupMember] AS GM ON GM.[PersonId] = P.[Id]
INNER JOIN [Group] AS G ON G.[Id] = GM.[GroupId]
INNER JOIN [GroupType] AS GT ON GT.[Id] = G.[GroupTypeId]
INNER JOIN [DefinedValue] AS RT ON RT.[Id] = P.[RecordTypeValueId]
WHERE GT.[Guid] = '790E3215-3B10-442B-AF69-616C0DCB998E'
" ) ).GroupBy( p => ( int ) p["FamilyId"] ).ToList();
            reporter = new CountProgressReporter( familyData.Count, p => Progress( p, 1, stepCount ) );

            foreach ( var familyChunk in familyData.Chunk( 500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var family in familyChunk )
                {
                    var familyId = family.Key;
                    var lastNameLookup = new Dictionary<string, string>();

                    foreach ( var person in family )
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
                            if ( !string.IsNullOrWhiteSpace( lastName ) )
                            {
                                if ( !lastNameLookup.ContainsKey( lastName ) )
                                {
                                    lastNameLookup.Add( lastName, Sweeper.DataFaker.Name.LastName() + " " + Sweeper.DataFaker.Name.LastName() + " LLC" );
                                }

                                changes.Add( "LastName", lastNameLookup[lastName] );
                            }
                        }
                        else
                        {
                            if ( !string.IsNullOrWhiteSpace( lastName ) )
                            {
                                if ( !lastNameLookup.ContainsKey( lastName ) )
                                {
                                    lastNameLookup.Add( lastName, Sweeper.DataFaker.Name.LastName() );
                                }

                                changes.Add( "LastName", lastNameLookup[lastName] );
                            }

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

                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( ( int ) person["Id"], changes ) );

                        //
                        // Update family name.
                        //
                        if ( !processedFamilyIds.Contains( familyId ) && !string.IsNullOrWhiteSpace( lastName ) && familyName.StartsWith( lastName ) )
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

                            await Sweeper.UpdateDatabaseRecordAsync( "Group", familyId, familyChanges );
                        }
                    }
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "Person", bulkChanges );

                reporter.Add( familyChunk.Count );
            }

            //
            // Stage 2: Update BenevolenceRequest
            //
            var queryData = await Sweeper.SqlQueryAsync( @"SELECT
BR.[Id], P.[FirstName] AS [PersonFirstName], P.[LastName] AS [PersonLastName]
FROM [BenevolenceRequest] AS BR
LEFT OUTER JOIN [PersonAlias] AS PA ON PA.[Id] = BR.[RequestedByPersonAliasId]
LEFT JOIN [Person] AS P ON P.[Id] = PA.[PersonId]" );
            reporter = new CountProgressReporter( queryData.Count, p => Progress( p, 2, stepCount ) );

            foreach ( var chunk in queryData.Chunk( 2_500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in chunk )
                {
                    var changes = new Dictionary<string, object>();

                    if ( item["PersonFirstName"] != null )
                    {
                        changes.Add( "FirstName", item["PersonFirstName"] );
                    }
                    else
                    {
                        changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
                    }

                    if ( item["PersonLastName"] != null )
                    {
                        changes.Add( "LastName", item["PersonLastName"] );
                    }
                    else
                    {
                        changes.Add( "LastName", Sweeper.DataFaker.Name.LastName() );
                    }

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( ( int ) item["Id"], changes ) );
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "BenevolenceRequest", bulkChanges );

                reporter.Add( chunk.Count );
            }

            //
            // Stage 3: Update PrayerRequest
            //
            queryData = await Sweeper.SqlQueryAsync( @"SELECT
PR.[Id], P.[FirstName] AS [PersonFirstName], P.[LastName] AS [PersonLastName]
FROM [PrayerRequest] AS PR
LEFT OUTER JOIN [PersonAlias] AS PA ON PA.[Id] = PR.[RequestedByPersonAliasId]
LEFT JOIN [Person] AS P ON P.[Id] = PA.[PersonId]" );
            reporter = new CountProgressReporter( queryData.Count, p => Progress( p, 3, stepCount ) );

            foreach ( var chunk in queryData.Chunk( 2_500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in chunk )
                {
                    var changes = new Dictionary<string, object>();

                    if ( item["PersonFirstName"] != null )
                    {
                        changes.Add( "FirstName", item["PersonFirstName"] );
                    }
                    else
                    {
                        changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
                    }

                    if ( item["PersonLastName"] != null )
                    {
                        changes.Add( "LastName", item["PersonLastName"] );
                    }
                    else
                    {
                        changes.Add( "LastName", Sweeper.DataFaker.Name.LastName() );
                    }

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( ( int ) item["Id"], changes ) );
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "PrayerRequest", bulkChanges );

                reporter.Add( chunk.Count );
            }

            //
            // Stage 4: Update Registration
            //
            queryData = await Sweeper.SqlQueryAsync( @"SELECT
R.[Id], P.[FirstName] AS [PersonFirstName], P.[LastName] AS [PersonLastName]
FROM [Registration] AS R
LEFT OUTER JOIN [PersonAlias] AS PA ON PA.[Id] = R.[PersonAliasId]
LEFT JOIN [Person] AS P ON P.[Id] = PA.[PersonId]" );
            reporter = new CountProgressReporter( queryData.Count, p => Progress( p, 4, stepCount ) );

            foreach ( var chunk in queryData.Chunk( 2_500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in chunk )
                {
                    var changes = new Dictionary<string, object>();

                    if ( item["PersonFirstName"] != null )
                    {
                        changes.Add( "FirstName", item["PersonFirstName"] );
                    }
                    else
                    {
                        changes.Add( "FirstName", Sweeper.DataFaker.Name.FirstName() );
                    }

                    if ( item["PersonLastName"] != null )
                    {
                        changes.Add( "LastName", item["PersonLastName"] );
                    }
                    else
                    {
                        changes.Add( "LastName", Sweeper.DataFaker.Name.LastName() );
                    }

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( ( int ) item["Id"], changes ) );
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "Registration", bulkChanges );

                reporter.Add( chunk.Count );
            }

            //
            // Stage 5: Update PersonPreviousName
            //
            var previousNames = ( await Sweeper.SqlQueryAsync<int, int, string>( @"SELECT
PPN.[Id], PA.[PersonId], PPN.[LastName]
FROM PersonPreviousName AS PPN
INNER JOIN [PersonAlias] AS PA ON PA.[Id] = PPN.[PersonAliasId]
" ) ).GroupBy( p => p.Item2 ).ToList();
            reporter = new CountProgressReporter( previousNames.Count, p => Progress( p, 5, stepCount ) );

            foreach ( var chunk in previousNames.Chunk( 2_500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in chunk )
                {
                    var previousNameLookup = new Dictionary<string, string>();
                    var personId = item.Key;

                    foreach ( var previousName in item )
                    {
                        var changes = new Dictionary<string, object>();

                        if ( !previousNameLookup.ContainsKey( previousName.Item3 ) )
                        {
                            previousNameLookup.Add( previousName.Item3, Sweeper.DataFaker.Name.LastName() );
                        }

                        changes.Add( "LastName", previousNameLookup[previousName.Item3] );

                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( previousName.Item1, changes ) );
                    }
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "PersonPreviousName", bulkChanges );

                reporter.Add( chunk.Count );
            }

            //
            // Stage 6: Update other tables
            //
            var fromNameLookup = new ConcurrentDictionary<string, string>();
            string scrubFromName( string oldValue )
            {
                if ( oldValue.StartsWith( "{" ) )
                {
                    return oldValue;
                }

                return fromNameLookup.GetOrAdd( oldValue, v => Sweeper.DataFaker.Name.FullName() );
            }

            int tableStep = 0;
            foreach ( var tc in Sweeper.ScrubNameTables )
            {
                await Sweeper.ScrubTableTextColumnsAsync( tc.Key, tc.Value, scrubFromName, p =>
                {
                    Progress( p, 6 + tableStep, stepCount );
                } );

                tableStep++;
            }
        }
    }
}
