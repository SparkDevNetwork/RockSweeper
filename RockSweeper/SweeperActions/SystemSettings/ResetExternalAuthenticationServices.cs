using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the external authentication services.
    /// </summary>
    [ActionId( "98e65e7a-32cb-4783-af07-d0605c6ca9a4" )]
    [Title( "Reset External Authentication Services" )]
    [Description( "Resets authentication services other than database, AD and PIN to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableAuthenticationServices ) )]
    [ConflictsWithAction( typeof( DisableExternalAuthenticationServices ) )]
    [ConflictsWithAction( typeof( ResetAuthenticationServices ) )]
    public class ResetExternalAuthenticationServices : SweeperAction
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
