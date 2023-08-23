using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all signed document providers are disabled.
    /// </summary>
    [ActionId( "0d1246a9-8a19-4658-a1c5-c8804e8bfeab" )]
    [Title( "Signature Document (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all signed document providers are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( SignatureDocumentReset ) )]
    public class SignatureDocumentDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
        }
    }
}
