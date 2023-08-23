using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all phone system settings to system default values.
    /// </summary>
    [ActionId( "afee211e-2a2d-4d85-a428-81ac95637ed6" )]
    [Title( "Phone System (Reset)" )]
    [Description( "Resets all phone system settings to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( PhoneSystemDisable ) )]
    public class PhoneSystemReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Pbx.PbxComponent" );
        }
    }
}
