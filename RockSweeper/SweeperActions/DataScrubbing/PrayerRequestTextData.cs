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
    /// Sanitizes prayer request text by replacing with lorem ipsum.
    /// </summary>
    [ActionId( "f6a44b98-2eb0-4e0e-a821-ee5997b9a56d" )]
    [Title( "Prayer Request Text" )]
    [Description( "Sanitizes prayer request text by replacing the request text and answer with lorem ipsum." )]
    [Category( "Data Scrubbing" )]
    public class PrayerRequestTextData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [PrayerRequest] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubPrayerRequestsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "PrayerRequest", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubPrayerRequestsAsync( List<int> requestIds )
        {
            var requests = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Text], [Answer] FROM [PrayerRequest] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", requestIds )}) ORDER BY [Id]" ) ).ToObjects<PrayerRequest>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var request in requests )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( request.Text ) )
                {
                    var requestText = Sweeper.DataFaker.Lorem.ReplaceWords( request.Text );

                    if ( requestText != request.Text )
                    {
                        changes["Text"] = requestText;
                    }
                }

                if ( !string.IsNullOrWhiteSpace( request.Answer ) )
                {
                    var requestAnswer = Sweeper.DataFaker.Lorem.ReplaceWords( request.Answer );

                    if ( requestAnswer != request.Answer )
                    {
                        changes["Answer"] = requestAnswer;
                    }
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( request.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class PrayerRequest
        {
            public int Id { get; set; }

            public string Text { get; set; }

            public string Answer { get; set; }
        }
    }
}
