using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the digital signature services.
    /// </summary>
    [ActionId( "4e60e8cf-38aa-4cbd-9fd2-013ae2aab95a" )]
    [Title( "Reset Digital Signature Services" )]
    [Description( "Resets all digital signature services to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableDigitalSignatureServices ) )]
    public class ResetDigitalSignatureServices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
        }
    }
}
