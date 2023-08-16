using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Generates the random logins.
    /// </summary>
    [ActionId( "304cf8ff-a70d-4de1-95d0-e4262cf1bfeb" )]
    [Title( "Generate Random Logins" )]
    [Description( "Replaces any login names found in the system with generated values." )]
    [Category( "Data Scrubbing" )]
    public class GenerateRandomLogins : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var logins = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id], [UserName] FROM [UserLogin]" );

            await Sweeper.ProcessItemsInParallelAsync( logins, 1000, async ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var login in items )
                {
                    var changes = new Dictionary<string, object>
                    {
                        { "UserName", $"fakeuser{ login.Item1 }" }
                    };

                    if ( login.Item2 != changes.First().Value.ToString() )
                    {
                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( login.Item1, changes ) );
                    }
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
