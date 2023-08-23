using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets authentication services other than database, AD and PIN to system default values.
    /// </summary>
    [ActionId( "98e65e7a-32cb-4783-af07-d0605c6ca9a4" )]
    [Title( "Authentication (Reset External)" )]
    [Description( "Resets authentication services other than database, AD and PIN to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( AuthenticationDisable ) )]
    [ConflictsWithAction( typeof( AuthenticationDisableExternal ) )]
    [ConflictsWithAction( typeof( AuthenticationReset ) )]
    public class AuthenticationResetExternal : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.ActiveDirectory",
                "Rock.Security.Authentication.PasswordlessAuthentication",
                "Rock.Security.Authentication.PINAuthentication" } );
        }
    }
}
