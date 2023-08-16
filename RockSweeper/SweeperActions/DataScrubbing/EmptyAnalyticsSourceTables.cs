using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Empties the analytics source tables.
    /// </summary>
    [ActionId( "8b6a47eb-006a-4734-a876-94f20b43b994" )]
    [Title( "Empty Analytics Source Tables" )]
    [Description( "Truncates the AnalyticsSource* tables so they contain no data." )]
    [Category( "Data Scrubbing" )]
    public class EmptyAnalyticsSourceTables : SweeperAction
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
