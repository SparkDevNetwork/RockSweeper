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
    /// Sanitizes note content by changing all notes to lorem ipsum text.
    /// </summary>
    [ActionId( "664802e9-acc4-4257-9585-4a463b706b6b" )]
    [Title( "Notification Messages" )]
    [Description( "Sanitizes notification messages content by changing all text to lorem ipsum text." )]
    [Category( "Data Scrubbing" )]
    public class NotificationMessageData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            List<int> ids;

            try
            {
                // Added in Rock v15.
                ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [NotificationMessage] ORDER BY [Id]" );
            }
            catch
            {
                ids = new List<int>();
            }

            var reporter = new CountProgressReporter( ids.Count, p => Progress( p ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ProcessNotificationMessagesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "NotificationMessage", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ProcessNotificationMessagesAsync( List<int> ids )
        {
            var messages = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Title], [Description] FROM [NotificationMessage] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<Note>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var message in messages )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( message.Title ) )
                {
                    changes["Title"] = Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( message.Title );
                }

                if ( !string.IsNullOrWhiteSpace( message.Description ) )
                {
                    changes["Description"] = Sweeper.DataFaker.Lorem.ReplaceWords( message.Description );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( message.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class Note
        {
            public int Id { get; set; }

            public string Title { get; set; }

            public string Description { get; set; }
        }
    }
}
