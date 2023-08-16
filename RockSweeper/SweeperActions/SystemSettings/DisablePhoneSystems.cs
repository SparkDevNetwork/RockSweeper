using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the phone systems.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "9c4bf948-5dd7-4bc0-ac2e-0cfb2493d02f" )]
    [Title( "Disable Phone Systems" )]
    [Description( "Updates the Rock configuration to ensure that all phone systems are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisablePhoneSystems : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Pbx.PbxComponent" );
        }
    }
}
