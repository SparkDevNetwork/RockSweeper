using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the digital signature services.
    /// </summary>
    [ActionId( "218ec162-27a9-4182-986e-868c35c9d63d" )]
    [Title( "Disable Digital Signature Services" )]
    [Description( "Updates the Rock configuration to ensure that all digital signature services are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetDigitalSignatureServices ) )]
    public class DisableDigitalSignatureServices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
        }
    }
}
