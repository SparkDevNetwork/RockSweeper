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
    /// Sanitizes data related to connection requests by removing identifying information.
    /// </summary>
    [ActionId( "f489a377-d4a5-4737-9657-53a7b3b7374f" )]
    [Title( "Connection Requests" )]
    [Description( "Sanitizes data related to connection requests by removing identifying information." )]
    [Category( "Data Scrubbing" )]
    public class ConnectionRequestData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            await ExecuteConnectionOpportunitiesAsync();

            await ExecuteConnectionRequestsAsync();

            await ExecuteConnectionRequestActivitiesAsync();
        }

        #region Opportunities

        private async Task ExecuteConnectionOpportunitiesAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [ConnectionOpportunity] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 1, 3 ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubConnectionOpportunitiesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "ConnectionOpportunity", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubConnectionOpportunitiesAsync( List<int> ids )
        {
            var opportunities = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Name], [PublicName], [Description] FROM [ConnectionOpportunity] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<ConnectionOpportunity>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var opportunity in opportunities )
            {
                var changes = new Dictionary<string, object>();

                changes["Name"] = Sweeper.DataFaker.Lorem.ReplaceWords( opportunity.Name ).Left( 50 );

                if ( !string.IsNullOrWhiteSpace( opportunity.PublicName ) )
                {
                    if ( opportunity.PublicName == opportunity.Name )
                    {
                        changes["PublicName"] = changes["Name"];
                    }
                    else
                    {
                        changes["PublicName"] = Sweeper.DataFaker.Lorem.ReplaceWords( opportunity.PublicName ).Left( 50 );
                    }
                }

                if ( !string.IsNullOrWhiteSpace( opportunity.Description ) )
                {
                    changes["Description"] = Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( opportunity.Description );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( opportunity.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class ConnectionOpportunity
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string PublicName { get; set; }

            public string Description { get; set; }
        }

        #endregion

        #region Requests

        private async Task ExecuteConnectionRequestsAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [ConnectionRequest] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 2, 3 ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubConnectionRequestsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "ConnectionRequest", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubConnectionRequestsAsync( List<int> ids )
        {
            var requests = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Comments] FROM [ConnectionRequest] WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<ConnectionRequest>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var request in requests )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( request.Comments ) )
                {
                    changes["Comments"] = Sweeper.DataFaker.Lorem.ReplaceWords( request.Comments );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( request.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        public class ConnectionRequest
        {
            public int Id { get; set; }

            public string Comments { get; set; }
        }

        #endregion

        #region Activities

        private async Task ExecuteConnectionRequestActivitiesAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [ConnectionRequestActivity] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 3, 3 ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubConnectionRequestActivitiesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "ConnectionRequestActivity", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubConnectionRequestActivitiesAsync( List<int> ids )
        {
            var requests = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Note] FROM [ConnectionRequestActivity] WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<ConnectionRequestActivity>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var request in requests )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( request.Note ) )
                {
                    changes["Note"] = Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( request.Note );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( request.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        public class ConnectionRequestActivity
        {
            public int Id { get; set; }

            public string Note { get; set; }
        }

        #endregion
    }
}
