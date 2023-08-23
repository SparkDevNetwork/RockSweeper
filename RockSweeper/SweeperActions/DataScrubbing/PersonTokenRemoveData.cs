using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Clears out the contents of the PersonToken table.
    /// </summary>
    [ActionId( "c81ff0e1-f605-402a-9881-251dbf84c853" )]
    [Title( "Person Tokens (Remove Data)" )]
    [Description( "Clears out the contents of the PersonToken table." )]
    [Category( "Data Scrubbing" )]
    public class PersonTokenRemoveData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( "TRUNCATE TABLE [PersonToken]" );
        }
    }
}
