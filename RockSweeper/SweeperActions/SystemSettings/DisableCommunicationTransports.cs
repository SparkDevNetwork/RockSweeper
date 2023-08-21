using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the communication transports.
    /// </summary>
    [ActionId( "b55c9a45-763d-45d7-8a77-dc0a93fc542b" )]
    [Title( "Disable Communication Transports" )]
    [Description( "Updates the Rock configuration to ensure that all communication transports are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetCommunicationTransports ) )]
    public class DisableCommunicationTransports : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Communication.TransportComponent" );
        }
    }
}
