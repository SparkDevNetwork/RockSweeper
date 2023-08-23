using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Clears out the contents of the Rock Exception Log.
    /// </summary>
    [ActionId( "89b28ddd-a138-4b59-91e4-ebf8019c1cad" )]
    [Title( "Exception Log (Remove Data)" )]
    [Description( "Clears out the contents of the Rock Exception Log." )]
    [Category( "Data Scrubbing" )]
    public class ExceptionLogRemoveData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( "TRUNCATE TABLE [ExceptionLog]" );
        }
    }
}
