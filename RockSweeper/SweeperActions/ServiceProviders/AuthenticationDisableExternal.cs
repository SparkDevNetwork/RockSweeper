using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that authentication services other than database, AD and PIN are disabled.
    /// </summary>
    [ActionId( "47f1e5dd-01cb-46bd-8f2a-a097b171c070" )]
    [Title( "Authentication (Disable External)" )]
    [Description( "Updates the Rock configuration to ensure that authentication services other than database, AD and PIN are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( AuthenticationDisable ) )]
    [ConflictsWithAction( typeof( AuthenticationReset ) )]
    [ConflictsWithAction( typeof( AuthenticationResetExternal ) )]
    public class AuthenticationDisableExternal : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PasswordlessAuthentication",
                "Rock.Security.Authentication.PINAuthentication" } );
        }
    }
}
