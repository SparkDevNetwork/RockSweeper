using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes the interaction data.
    /// </summary>
    [ActionId( "50b87e79-f539-4fa5-b848-1b7a9fd00cc9" )]
    [Title( "Sanitize Person Alternate Identifiers" )]
    [Description( "Sanitizes any person alternative identifiers and search keys of identifying information." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( GenerateRandomEmailAddresses ) )]
    public class SanitizePersonAlternateIdentifiers : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [PersonSearchKey] ORDER BY [Id]" );

            await Sweeper.ProcessItemsInParallelAsync( ids, 2_500, ProcessSearchKeysAsync, p => Progress( p ) );
        }

        private async Task ProcessSearchKeysAsync( List<int> ids )
        {
            var searchKeys = ( await Sweeper.SqlQueryAsync( "SELECT [Id], [SearchValue] FROM [PersonSearchKey] ORDER BY [Id]" ) ).ToObjects<SearchKey>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();
            var emailHasBeenScrubbed = Sweeper.HasActionExecuted<GenerateRandomEmailAddresses>();

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

            await Sweeper.UpdateDatabaseRecordsAsync( "PersonSearchKey", bulkUpdates );
        }

        private class SearchKey
        {
            public int Id { get; set; }

            public string SearchValue { get; set; }
        }
    }
}
