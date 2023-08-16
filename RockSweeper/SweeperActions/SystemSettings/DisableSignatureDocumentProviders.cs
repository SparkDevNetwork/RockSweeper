using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the signature document providers.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "0d1246a9-8a19-4658-a1c5-c8804e8bfeab" )]
    [Title( "Disable Signature Document Providers" )]
    [Description( "Updates the Rock configuration to ensure that all signed document providers are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisableSignatureDocumentProviders : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DisableComponentsOfType( "Rock.Security.DigitalSignatureComponent" );

            return Task.CompletedTask;
        }
    }
}
