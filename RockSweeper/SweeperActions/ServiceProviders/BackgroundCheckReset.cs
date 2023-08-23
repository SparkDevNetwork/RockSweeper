using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all background check providers to system default values.
    /// </summary>
    [ActionId( "4d9f9857-429b-4e1b-8833-8e702bbc7952" )]
    [Title( "Background Check (Reset)" )]
    [Description( "Resets all background check providers to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( BackgroundCheckDisable ) )]
    public class BackgroundCheckReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Security.BackgroundCheckComponent" );
        }
    }
}
