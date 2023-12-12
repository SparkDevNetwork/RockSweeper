using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes group descriptions by replacing description text with lorem ipsum.
    /// </summary>
    [ActionId( "f6a44b98-2eb0-4e0e-a821-ee5997b9a56d" )]
    [Title( "Group Descriptions" )]
    [Description( "Sanitizes group descriptions by replacing description text with lorem ipsum." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( GroupNameData ) )]
    public class GroupDescriptionData : SweeperAction
    {
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
            var groups = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Description] FROM [Group] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", groupIds )}) ORDER BY [Id]" ) ).ToObjects<Group>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var group in groups )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( group.Description ) )
                {
                    var groupDescription = Sweeper.DataFaker.Lorem.ReplaceWords( group.Description );

                    if ( groupDescription != group.Description )
                    {
                        changes["Description"] = groupDescription;
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

            public string Description { get; set; }
        }
    }
}
