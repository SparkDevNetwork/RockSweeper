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
    [ActionId( "756343a6-3473-4c3b-8ad9-f16ff604ae07" )]
    [Title( "Sanitize Notes" )]
    [Description( "Sanitizes note content by changing all notes to lorem ipsum text." )]
    [Category( "Data Scrubbing" )]
    public class SanitizeNotes : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [Note] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubNotesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "Note", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubNotesAsync( List<int> ids )
        {
            var notes = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Text] FROM [Note] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<Note>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var note in notes )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( note.Text ) )
                {
                    changes["Text"] = Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( note.Text );
                }

                if ( !string.IsNullOrWhiteSpace( note.Caption ) )
                {
                    changes["Caption"] = Sweeper.DataFaker.Lorem.ReplaceWords( note.Caption );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( note.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class Note
        {
            public int Id { get; set; }

            public string Caption { get; set; }

            public string Text { get; set; }
        }
    }
}
