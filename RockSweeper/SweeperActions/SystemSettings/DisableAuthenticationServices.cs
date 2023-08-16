using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the authentication services.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "a68b25fb-6ec2-4d3d-b1de-16aae76c47d6" )]
    [Title( "Disable Authentication Services" )]
    [Description( "Updates the Rock configuration to ensure that authentication services other than database and PIN are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisableAuthenticationServices : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DisableComponentsOfType( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PINAuthentication" } );

            return Task.CompletedTask;
        }
    }
}
