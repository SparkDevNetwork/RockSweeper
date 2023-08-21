using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the background check providers.
    /// </summary>
    [ActionId( "903e3037-e614-41ad-a663-babd069d7927" )]
    [Title( "Disable Background Check Providers" )]
    [Description( "Updates the Rock configuration to ensure that all background check providers are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetBackgroundCheckProviders ) )]
    public class DisableBackgroundCheckProviders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.BackgroundCheckComponent" );
        }
    }
}
