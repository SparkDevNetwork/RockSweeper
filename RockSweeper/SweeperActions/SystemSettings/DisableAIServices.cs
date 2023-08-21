using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the AI services.
    /// </summary>
    [ActionId( "9deb7575-f55c-4501-8635-30c011c16665" )]
    [Title( "Disable AI Services" )]
    [Description( "Updates the Rock configuration to ensure that all AI services are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetAIServices ) )]
    public class DisableAIServices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.AI.Provider.AIProviderComponent" );
        }
    }
}
