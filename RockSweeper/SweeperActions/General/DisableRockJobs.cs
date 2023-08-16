using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.General
{
    /// <summary>
    /// Disables the rock jobs except the Job Pulse job.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "48f41eef-d394-49b8-a214-27c9f2a2abd0" )]
    [Title( "Disable Rock Jobs" )]
    [Description( "Disables all Rock jobs except the Job Pulse." )]
    [Category( "General" )]
    [DefaultValue( true )]
    public class DisableRockJobs : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( $"UPDATE [ServiceJob] SET [IsActive] = 0 WHERE [Guid] != 'CB24FF2A-5AD3-4976-883F-DAF4EFC1D7C7'" );
        }
    }
}
