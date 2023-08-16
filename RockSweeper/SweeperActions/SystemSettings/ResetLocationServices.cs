using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the location services.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "ff2e4638-87ba-40eb-bda9-039d8277be8b" )]
    [Title( "Reset Location Services" )]
    [Description( "Resets all location services to system default values." )]
    [Category( "System Settings" )]
    [RequiresRockWeb]
    public class ResetLocationServices : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DeleteAttributeValuesForComponentsOfType( "Rock.Address.VerificationComponent" );

            return Task.CompletedTask;
        }
    }
}
