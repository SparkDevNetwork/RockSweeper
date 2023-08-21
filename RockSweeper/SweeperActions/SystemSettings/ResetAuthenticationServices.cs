using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the authentication services.
    /// </summary>
    [ActionId( "29386c4e-38da-4e83-8d8d-058f735a087c" )]
    [Title( "Reset Authentication Services" )]
    [Description( "Resets authentication services other than database and PIN to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableAuthenticationServices ) )]
    [ConflictsWithAction( typeof( DisableExternalAuthenticationServices ) )]
    [ConflictsWithAction( typeof( ResetExternalAuthenticationServices ) )]
    public class ResetAuthenticationServices : SweeperAction
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
