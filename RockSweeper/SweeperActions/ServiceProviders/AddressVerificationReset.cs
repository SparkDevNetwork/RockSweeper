using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all address verification providers to system default values.
    /// </summary>
    [ActionId( "ff2e4638-87ba-40eb-bda9-039d8277be8b" )]
    [Title( "Address Verification (Reset)" )]
    [Description( "Resets all address verification providers to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( AddressVerificationDisable ) )]
    public class AddressVerificationReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Address.VerificationComponent" );
        }
    }
}
