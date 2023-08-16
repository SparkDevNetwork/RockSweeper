using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the location services.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "8a6cb4e7-cc47-4d0d-8358-2373424bace1" )]
    [Title( "Disable Location Services" )]
    [Description( "Updates the Rock configuration to ensure that all location services are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisableLocationServices : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DisableComponentsOfType( "Rock.Address.VerificationComponent" );

            return Task.CompletedTask;
        }
    }
}
