using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets AI providers to system default values.
    /// </summary>
    [ActionId( "da03b960-1bec-4c55-b966-537970cd6dcd" )]
    [Title( "AI (Reset)" )]
    [Description( "Resets AI providers to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( AIDisable ) )]
    public class AIReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.AI.Provider.AIProviderComponent" );
        }
    }
}
