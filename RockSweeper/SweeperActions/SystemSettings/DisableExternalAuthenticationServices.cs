using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the external authentication services.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "47f1e5dd-01cb-46bd-8f2a-a097b171c070" )]
    [Title( "Disable External Authentication Services" )]
    [Description( "Updates the Rock configuration to ensure that authentication services other than database, AD and PIN are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisableExternalAuthenticationServices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PINAuthentication" } );
        }
    }
}
