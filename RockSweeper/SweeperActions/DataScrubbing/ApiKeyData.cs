using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Replaces any API keys with generated values.
    /// </summary>
    [ActionId( "705c42d1-5600-418c-b0bd-7c3a3ac9c982" )]
    [Title( "API Keys" )]
    [Description( "Replaces any API keys with generated values." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( LoginData ) )]
    public class ApiKeyData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var loginIds = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [UserLogin] WHERE [ApiKey] IS NOT NULL AND [ApiKey] != '' ORDER BY [Id]" );

            await Sweeper.ProcessItemsInParallelAsync( loginIds, 1000, async ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var loginId in items )
                {
                    var changes = new Dictionary<string, object>
                    {
                        { "ApiKey", Sweeper.DataFaker.Internet.Password( 20 ) }
                    };

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( loginId, changes ) );
                }

                if ( bulkChanges.Count() > 0 )
                {
                    await Sweeper.UpdateDatabaseRecordsAsync( "UserLogin", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p );
            } );
        }
    }
}
