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
    /// Sanitizes any person alternative identifiers and search keys of identifying information.
    /// </summary>
    [ActionId( "50b87e79-f539-4fa5-b848-1b7a9fd00cc9" )]
    [Title( "Person Alternate Identifiers" )]
    [Description( "Sanitizes any person alternative identifiers and search keys of identifying information." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( EmailAddressData ) )]
    public class PersonAlternateIdentifierData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            var searchKeys = ( await Sweeper.SqlQueryAsync( "SELECT [Id], [SearchValue] FROM [PersonSearchKey] ORDER BY [Id]" ) ).ToObjects<SearchKey>();
            var reporter = new CountProgressReporter( searchKeys.Count, p => Progress( p ) );

            await new AsyncProducer<List<SearchKey>>( searchKeys.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var results = await ScrubSearchKeys( items );

                    reporter.Add( items.Count - results.Count );
                    
                    return results;
                } )
                .Consume( async items =>
                {
                    await SaveUpdates( items );

                    reporter.Add( items.Count );
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private Task<List<Tuple<int, Dictionary<string, object>>>> ScrubSearchKeys( IEnumerable<SearchKey> searchKeys )
        {
            var emailHasBeenScrubbed = Sweeper.HasActionExecuted<EmailAddressData>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var searchKey in searchKeys )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( searchKey.SearchValue ) )
                {
                    if ( searchKey.SearchValue.IsEmailAddress() )
                    {
                        if ( !emailHasBeenScrubbed )
                        {
                            changes["SearchValue"] = Sweeper.GenerateFakeEmailAddressForAddress( searchKey.SearchValue );
                        }
                    }
                    else
                    {
                        changes["SearchValue"] = searchKey.SearchValue.RandomizeLettersAndNumbers();
                    }
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( searchKey.Id, changes ) );
                }
            }

            return Task.FromResult( bulkUpdates );
        }

        private async Task SaveUpdates( List<Tuple<int, Dictionary<string, object>>> updates )
        {
            await Sweeper.UpdateDatabaseRecordsAsync( "PersonSearchKey", updates );
        }

        private class SearchKey
        {
            public int Id { get; set; }

            public string SearchValue { get; set; }
        }
    }
}
