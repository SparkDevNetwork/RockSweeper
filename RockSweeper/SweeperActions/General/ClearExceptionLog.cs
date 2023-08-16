using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.General
{
    /// <summary>
    /// Clears the exception log table.
    /// </summary>
    [ActionId( "89b28ddd-a138-4b59-91e4-ebf8019c1cad" )]
    [Title( "Clear Exception Log" )]
    [Description( "Clears out the contents of the Rock Exception Log." )]
    [Category( "General" )]
    public class ClearExceptionLog : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.SqlCommand( "TRUNCATE TABLE [ExceptionLog]" );

            return Task.CompletedTask;
        }
    }
}
