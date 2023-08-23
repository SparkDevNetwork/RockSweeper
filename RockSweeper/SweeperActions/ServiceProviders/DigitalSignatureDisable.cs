using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all digital signature services are disabled.
    /// </summary>
    [ActionId( "218ec162-27a9-4182-986e-868c35c9d63d" )]
    [Title( "Digital Signature (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all digital signature services are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( DigitalSignatureReset ) )]
    public class DigitalSignatureDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
        }
    }
}
