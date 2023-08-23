using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all communication transports are disabled.
    /// </summary>
    [ActionId( "b55c9a45-763d-45d7-8a77-dc0a93fc542b" )]
    [Title( "Communication Transport (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all communication transports are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( CommunicationTransportReset ) )]
    public class CommunicationTransportDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Communication.TransportComponent" );
        }
    }
}
