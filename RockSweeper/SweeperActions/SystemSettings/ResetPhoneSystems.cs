using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the phone systems.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "afee211e-2a2d-4d85-a428-81ac95637ed6" )]
    [Title( "Reset Phone Systems" )]
    [Description( "Resets all phone system settings to system default values." )]
    [Category( "System Settings" )]
    [RequiresRockWeb]
    public class ResetPhoneSystems : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DeleteAttributeValuesForComponentsOfType( "Rock.Pbx.PbxComponent" );

            return Task.CompletedTask;
        }
    }
}
