using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all digital signature services to system default values.
    /// </summary>
    [ActionId( "4e60e8cf-38aa-4cbd-9fd2-013ae2aab95a" )]
    [Title( "Digital Signature (Reset)" )]
    [Description( "Resets all digital signature services to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( DigitalSignatureDisable ) )]
    public class DigitalSignatureReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
        }
    }
}
