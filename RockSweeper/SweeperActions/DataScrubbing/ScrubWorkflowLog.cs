using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Scrubs the workflow log.
    /// </summary>
    [ActionId( "d6a36aad-f7ba-4ada-b018-f6aa4065f6bf" )]
    [Title( "Scrub Workflow Log" )]
    [Description( "Modifies the log text to only include the activity and action and not the specific action text." )]
    [Category( "Data Scrubbing" )]
    public class ScrubWorkflowLog : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.ScrubTableTextColumnAsync( "WorkflowLog", "LogText", ( s ) =>
            {
                if ( s.Contains( ">" ) )
                {
                    var sections = s.Split( new[] { ':' }, 2 );

                    if ( sections.Length == 2 )
                    {
                        if ( sections[1] != " Activated" && sections[1] != " Processing..." && sections[1] != " Completed" )
                        {
                            return $"{sections[0]}: HIDDEN";
                        }
                    }
                }

                return s;
            }, p => Progress( p ) );
        }
    }
}
