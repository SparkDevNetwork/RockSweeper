using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the AI services.
    /// </summary>
    [ActionId( "da03b960-1bec-4c55-b966-537970cd6dcd" )]
    [Title( "Reset AI Services" )]
    [Description( "Resets AI services to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableAIServices ) )]
    public class ResetAIServices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.AI.Provider.AIProviderComponent" );
        }
    }
}
