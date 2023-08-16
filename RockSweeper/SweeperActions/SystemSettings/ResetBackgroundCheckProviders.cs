using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the background check providers.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "4d9f9857-429b-4e1b-8833-8e702bbc7952" )]
    [Title( "Reset Background Check Providers" )]
    [Description( "Resets all background check providers to system default values." )]
    [Category( "System Settings" )]
    [RequiresRockWeb]
    public class ResetBackgroundCheckProviders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.BackgroundCheckComponent" );
        }
    }
}
