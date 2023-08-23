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
    /// Sanitizes any data regarding the web farm environment.
    /// </summary>
    [ActionId( "a46a95e5-d2c4-46fc-bffc-225ccc1a3987" )]
    [Title( "Web Farm Data" )]
    [Description( "Sanitizes any data regarding the web farm environment." )]
    [Category( "Data Scrubbing" )]
    public class WebFarmData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            await ProcessWebFarmNodes();
            await ProcessWebFarmNodeLogs();
        }

        #region Web Farm Nodes

        private async Task ProcessWebFarmNodes()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [WebFarmNode] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 1, 2 ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubWebFarmNodesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "WebFarmNode", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubWebFarmNodesAsync( List<int> ids )
        {
            var nodes = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [NodeName] FROM [WebFarmNode] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<WebFarmNode>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var node in nodes )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( node.NodeName ) )
                {
                    changes["NodeName"] = Sweeper.DataFaker.Lorem.ReplaceWords( node.NodeName );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( node.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class WebFarmNode
        {
            public int Id { get; set; }

            public string NodeName { get; set; }
        }

        #endregion

        #region Web Farm Node Logs

        private async Task ProcessWebFarmNodeLogs()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [WebFarmNodeLog] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 2, 2 ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubWebFarmNodeLogsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "WebFarmNodeLog", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubWebFarmNodeLogsAsync( List<int> ids )
        {
            var logs = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Message] FROM [WebFarmNodeLog] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<WebFarmNodeLog>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var log in logs )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( log.Message ) )
                {
                    changes["Message"] = Sweeper.DataFaker.Lorem.ReplaceWords( log.Message );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( log.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class WebFarmNodeLog
        {
            public int Id { get; set; }

            public string Message { get; set; }
        }

        #endregion
    }
}
