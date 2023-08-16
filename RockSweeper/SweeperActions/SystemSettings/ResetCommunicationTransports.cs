using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the existing communication transport configuration attribute values.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "c1770744-9498-4b35-a172-f64ad00f74c0" )]
    [Title( "Reset Communication Transports" )]
    [Description( "Resets all transport configuration to system default values." )]
    [Category( "System Settings" )]
    [RequiresRockWeb]
    public class ResetCommunicationTransports : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Communication.TransportComponent" );
        }
    }
}
