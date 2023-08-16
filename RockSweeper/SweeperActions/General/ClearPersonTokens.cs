using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.General
{
    /// <summary>
    /// Clears the person token table.
    /// </summary>
    [ActionId( "c81ff0e1-f605-402a-9881-251dbf84c853" )]
    [Title( "Clear Person Tokens" )]
    [Description( "Clears out the contents of the PersonToken table." )]
    [Category( "General" )]
    public class ClearPersonTokens : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.SqlCommand( "TRUNCATE TABLE [PersonToken]" );

            return Task.CompletedTask;
        }
    }
}
