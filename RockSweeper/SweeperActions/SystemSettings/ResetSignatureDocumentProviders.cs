using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the signature document providers.
    /// </summary>
    [ActionId( "59fd05b1-0263-4442-9d63-7491e254bcd1" )]
    [Title( "Reset Signature Document Providers" )]
    [Description( "Resets all signed document providers to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableSignatureDocumentProviders ) )]
    public class ResetSignatureDocumentProviders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
            await Sweeper.SetGlobalAttributeValue( "SignNowAccessToken", string.Empty );
        }
    }
}
