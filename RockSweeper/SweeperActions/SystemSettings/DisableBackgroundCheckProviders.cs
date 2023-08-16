using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the background check providers.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "903e3037-e614-41ad-a663-babd069d7927" )]
    [Title( "Disable Background Check Providers" )]
    [Description( "Updates the Rock configuration to ensure that all background check providers are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisableBackgroundCheckProviders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.BackgroundCheckComponent" );
        }
    }
}
