using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets authentication services other than database and PIN to system default values.
    /// </summary>
    [ActionId( "29386c4e-38da-4e83-8d8d-058f735a087c" )]
    [Title( "Authentication (Reset)" )]
    [Description( "Resets authentication services other than database and PIN to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( AuthenticationDisable ) )]
    [ConflictsWithAction( typeof( AuthenticationDisableExternal ) )]
    [ConflictsWithAction( typeof( AuthenticationResetExternal ) )]
    public class AuthenticationReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.AuthenticationComponent", new[] {
                "Rock.Security.Authentication.Database",
                "Rock.Security.Authentication.PasswordlessAuthentication",
                "Rock.Security.Authentication.PINAuthentication" } );
        }
    }
}
