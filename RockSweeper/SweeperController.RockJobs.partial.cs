namespace RockSweeper
{
    public partial class SweeperController
    {
        /// <summary>
        /// Disables the rock jobs except the Job Pulse job.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        public void DisableRockJobs()
        {
            SqlCommand( $"UPDATE [ServiceJob] SET [IsActive] = 0 WHERE [Guid] != 'CB24FF2A-5AD3-4976-883F-DAF4EFC1D7C7'" );
        }
    }
}
