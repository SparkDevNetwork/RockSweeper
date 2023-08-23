using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all address verification services are disabled.
    /// </summary>
    [ActionId( "8a6cb4e7-cc47-4d0d-8358-2373424bace1" )]
    [Title( "Address Verification (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all address verification services are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( AddressVerificationReset ) )]
    public class AddressVerificationDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Address.VerificationComponent" );
        }
    }
}
