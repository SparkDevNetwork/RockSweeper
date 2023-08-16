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
        public override Task ExecuteAsync()
        {
            Sweeper.SqlCommand( "UPDATE [InteractionChannel] SET [ChannelData] = NULL" );
            Sweeper.SqlCommand( "UPDATE [InteractionComponent] SET [ComponentData] = NULL" );
            Sweeper.SqlCommand( "UPDATE [Interaction] SET [InteractionData] = NULL" );

            return Task.CompletedTask;
        }
    }
}
