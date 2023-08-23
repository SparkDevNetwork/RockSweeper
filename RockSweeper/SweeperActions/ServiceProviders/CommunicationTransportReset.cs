using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all transport configuration to system default values.
    /// </summary>
    [ActionId( "c1770744-9498-4b35-a172-f64ad00f74c0" )]
    [Title( "Communication Transport (Reset)" )]
    [Description( "Resets all transport configuration to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( CommunicationTransportDisable ) )]
    public class CommunicationTransportReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Communication.TransportComponent" );
        }
    }
}
