using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all phone systems are disabled.
    /// </summary>
    [ActionId( "9c4bf948-5dd7-4bc0-ac2e-0cfb2493d02f" )]
    [Title( "Phone System (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all phone systems are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( PhoneSystemReset ) )]
    public class PhoneSystemDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Pbx.PbxComponent" );
        }
    }
}
