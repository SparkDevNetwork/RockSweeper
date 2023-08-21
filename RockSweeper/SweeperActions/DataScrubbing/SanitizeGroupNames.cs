using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes group names by attempting to remove proper names.
    /// </summary>
    [ActionId( "02c2d628-ac02-4a36-9f0a-1f4b34c72036" )]
    [Title( "Sanitize Group Names" )]
    [Description( "Sanitizes group names by attempting to remove proper names." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( GenerateRandomNames ) )]
    public class SanitizeGroupNames : SweeperAction
    {
        /// <summary>
        /// The regular expression to match individual words.
        /// </summary>
        private static readonly Regex WordRegex = new Regex( @"\b\w{3,}\b" );

        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            var ignoreGroupTypeIds = await Sweeper.SqlQueryAsync<int>( @"
SELECT [Id]
FROM [GroupType]
WHERE [Guid] IN (
    'AECE949F-704C-483E-A4FB-93D5E4720C4C', -- Security Role
    '790E3215-3B10-442B-AF69-616C0DCB998E', -- Family
    'E0C5A0E2-B7B3-4EF4-820D-BBF7F9A374EF', -- Known Relationships
    '8C0E5852-F08F-4327-9AA5-87800A6AB53E' -- Peer Network
)" );
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [Group] WHERE [GroupTypeId] NOT IN ({string.Join( ",", ignoreGroupTypeIds.Select( id => id.ToString() ) )}) ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubGroupsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "Group", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubGroupsAsync( List<int> groupIds )
        {
            var groups = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Name] FROM [Group] WHERE [Id] IN ({string.Join( ",", groupIds )}) ORDER BY [Id]" ) ).ToObjects<Group>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var group in groups )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( group.Name ) )
                {
                    var groupName = WordRegex.Replace( group.Name, m =>
                    {
                        if ( Sweeper.OriginalFirstNames.Contains( m.Value ) || Sweeper.OriginalNickNames.Contains( m.Value ) )
                        {
                            return Sweeper.DataFaker.Name.FirstName();
                        }
                        else if ( Sweeper.OriginalLastNames.Contains( m.Value ) )
                        {
                            return Sweeper.DataFaker.Name.LastName();
                        }
                        else
                        {
                            return m.Value;
                        }
                    } );

                    if ( groupName != group.Name )
                    {
                        changes["Name"] = groupName;
                    }
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( group.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class Group
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}
