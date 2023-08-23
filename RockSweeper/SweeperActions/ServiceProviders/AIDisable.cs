using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all AI providers are disabled.
    /// </summary>
    [ActionId( "9deb7575-f55c-4501-8635-30c011c16665" )]
    [Title( "AI (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all AI providers are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( AIReset ) )]
    public class AIDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.AI.Provider.AIProviderComponent" );
        }
    }
}
