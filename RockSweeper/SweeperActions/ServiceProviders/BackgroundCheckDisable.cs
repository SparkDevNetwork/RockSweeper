using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Disables the background check providers.
    /// </summary>
    [ActionId( "903e3037-e614-41ad-a663-babd069d7927" )]
    [Title( "Background Check (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all background check providers are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( BackgroundCheckReset ) )]
    public class BackgroundCheckDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.BackgroundCheckComponent" );
        }
    }
}
