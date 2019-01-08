namespace RockSweeper
{
    public partial class SweeperController
    {
        /// <summary>
        /// Disables the SSL requirement for sites and pages.
        /// </summary>
        public void DisableSslForSitesAndPages()
        {
            SqlCommand( "UPDATE [Site] SET [RequiresEncryption] = 0" );
            SqlCommand( "UPDATE [Page] SET [RequiresEncryption] = 0" );
        }

        /// <summary>
        /// Clears the exception log table.
        /// </summary>
        public void ClearExceptionLog()
        {
            SqlCommand( "TRUNCATE TABLE [ExceptionLog]" );
        }

        /// <summary>
        /// Clears the person token table.
        /// </summary>
        public void ClearPersonTokens()
        {
            SqlCommand( "TRUNCATE TABLE [PersonToken]" );
        }
    }
}
