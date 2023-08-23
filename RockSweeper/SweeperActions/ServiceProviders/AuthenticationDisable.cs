using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Disables the authentication services.
    /// </summary>
    [ActionId( "a68b25fb-6ec2-4d3d-b1de-16aae76c47d6" )]
    [Title( "Authentication (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that authentication services other than database and PIN are disabled." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( AuthenticationDisableExternal ) )]
    [ConflictsWithAction( typeof( AuthenticationReset ) )]
    [ConflictsWithAction( typeof( AuthenticationResetExternal ) )]
    public class AuthenticationDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PasswordlessAuthentication",
                "Rock.Security.Authentication.PINAuthentication" } );
        }
    }
}
