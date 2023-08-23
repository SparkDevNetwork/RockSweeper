using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Removes all data from the AnalyticsSource tables.
    /// </summary>
    [ActionId( "8b6a47eb-006a-4734-a876-94f20b43b994" )]
    [Title( "Analytics Source Tables (Remove Data)" )]
    [Description( "Removes all data from the AnalyticsSource tables." )]
    [Category( "Data Scrubbing" )]
    public class AnalyticsSourceRemoveData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var tables = await Sweeper.SqlQueryAsync<string>( "SELECT [name] FROM sys.all_objects WHERE [type_desc] = 'USER_TABLE' AND [name] LIKE 'AnalyticsSource%'" );

            foreach ( var table in tables )
            {
                await Sweeper.SqlCommandAsync( $"TRUNCATE TABLE [{table}]" );
            }
        }
    }
}
