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
    /// Sanitizes communications by replacing content with lorem ipsum text.
    /// </summary>
    [ActionId( "1e7dac9c-3002-4896-9dd3-fa2e4964e241" )]
    [Title( "Communications" )]
    [Description( "Sanitizes communications by replacing content with lorem ipsum text." )]
    [Category( "Data Scrubbing" )]
    public class CommunicationData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            await ProcessCommunications();
            await ProcessCommunicationResponses();
        }

        #region Communications

        private async Task ProcessCommunications()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [Communication] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 1, 2 ) );

            // Communications are usually pretty large in bytes, so use a
            // smaller chunk size than normal.
            await AsyncProducer.FromItems( ids.Chunk( 500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubCommunicationsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "Communication", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubCommunicationsAsync( List<int> ids )
        {
            var communications = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Subject], [Message], [SMSMessage], [PushTitle], [PushMessage] FROM [Communication] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<Communication>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var communication in communications )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( communication.Subject ) )
                {
                    changes["Subject"] = Sweeper.DataFaker.Lorem.ReplaceWords( communication.Subject );
                }

                if ( !string.IsNullOrWhiteSpace( communication.Message ) )
                {
                    changes["Message"] = Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( communication.Message );
                }

                if ( !string.IsNullOrWhiteSpace( communication.SMSMessage ) )
                {
                    changes["SMSMessage"] = Sweeper.DataFaker.Lorem.ReplaceWords( communication.SMSMessage );
                }

                if ( !string.IsNullOrWhiteSpace( communication.PushTitle ) )
                {
                    changes["PushTitle"] = Sweeper.DataFaker.Lorem.ReplaceWords( communication.PushTitle );
                }

                if ( !string.IsNullOrWhiteSpace( communication.PushMessage ) )
                {
                    changes["PushMessage"] = Sweeper.DataFaker.Lorem.ReplaceWords( communication.PushMessage );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( communication.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class Communication
        {
            public int Id { get; set; }

            public string Subject { get; set; }

            public string Message { get; set; }

            public string SMSMessage { get; set; }

            public string PushTitle { get; set; }

            public string PushMessage { get; set; }
        }

        #endregion

        #region Communication Responses

        private async Task ProcessCommunicationResponses()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [CommunicationResponse] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 1, 2 ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubCommunicationResponsesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "CommunicationResponse", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubCommunicationResponsesAsync( List<int> ids )
        {
            var responses = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Response] FROM [CommunicationResponse] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<CommunicationResponse>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var response in responses )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( response.Response ) )
                {
                    changes["Response"] = Sweeper.DataFaker.Lorem.ReplaceWords( response.Response );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( response.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class CommunicationResponse
        {
            public int Id { get; set; }

            public string Response { get; set; }
        }

        #endregion
    }
}
