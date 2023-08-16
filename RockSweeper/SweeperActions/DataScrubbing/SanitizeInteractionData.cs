using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes the interaction data.
    /// </summary>
    [ActionId( "31200ef0-cad7-4775-8be8-c1a06a591e3f" )]
    [Title( "Sanitize Interaction Data" )]
    [Description( "Removes all custom data from Interactions, InteractionComponents and InteractionChannels." )]
    [Category( "Data Scrubbing" )]
    public class SanitizeInteractionData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( "UPDATE [InteractionChannel] SET [ChannelData] = NULL" );
            await Sweeper.SqlCommandAsync( "UPDATE [InteractionComponent] SET [ComponentData] = NULL" );
            await Sweeper.SqlCommandAsync( "UPDATE [Interaction] SET [InteractionData] = NULL" );
        }
    }
}
