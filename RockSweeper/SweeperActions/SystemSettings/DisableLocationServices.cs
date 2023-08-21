using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the location services.
    /// </summary>
    [ActionId( "8a6cb4e7-cc47-4d0d-8358-2373424bace1" )]
    [Title( "Disable Location Services" )]
    [Description( "Updates the Rock configuration to ensure that all location services are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetLocationServices ) )]
    public class DisableLocationServices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Address.VerificationComponent" );
        }
    }
}
