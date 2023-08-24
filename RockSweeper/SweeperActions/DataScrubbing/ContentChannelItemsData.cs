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
    /// Sanitizes the content channel items.
    /// </summary>
    [ActionId( "6ecb7b00-e1ed-44e9-b24e-e4beffbac9f3" )]
    [Title( "Content Channel Items" )]
    [Description( "Replaces content channel item content with ipsum text." )]
    [Category( "Data Scrubbing" )]
    public class ContentChannelItemsData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [ContentChannelItem] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p ) );

            await AsyncProducer.FromItems( ids.Chunk( 500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubContentChannelItemsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "ContentChannelItem", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubContentChannelItemsAsync( List<int> ids )
        {
            var items = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Content] FROM [ContentChannelItem] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<ContentChannelItem>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var item in items )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( item.Content ) )
                {
                    changes["Content"] = Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( item.Content );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( item.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class ContentChannelItem
        {
            public int Id { get; set; }

            public string Content { get; set; }
        }
    }
}
