using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all signed document providers to system default values.
    /// </summary>
    [ActionId( "59fd05b1-0263-4442-9d63-7491e254bcd1" )]
    [Title( "Signature Document (Reset)" )]
    [Description( "Resets all signed document providers to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( SignatureDocumentDisable ) )]
    public class SignatureDocumentReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
            await Sweeper.SetGlobalAttributeValue( "SignNowAccessToken", string.Empty );
        }
    }
}
