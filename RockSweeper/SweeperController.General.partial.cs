using System.ComponentModel;

using RockSweeper.Attributes;

namespace RockSweeper
{
    public partial class SweeperController
    {
        /// <summary>
        /// Disables the SSL requirement for sites and pages.
        /// </summary>
        [ActionId( "8c1155d0-6319-4750-a7ca-0d713740bfde" )]
        [Title( "Disable SSL for Sites and Pages" )]
        [Description( "Modifies all Sites and Pages and removes the requirement for an SSL connection." )]
        [Category( "General" )]
        [DefaultValue( true )]
        public void DisableSslForSitesAndPages()
        {
            SqlCommand( "UPDATE [Site] SET [RequiresEncryption] = 0" );
            SqlCommand( "UPDATE [Page] SET [RequiresEncryption] = 0" );
        }

        /// <summary>
        /// Clears the exception log table.
        /// </summary>
        [ActionId( "89b28ddd-a138-4b59-91e4-ebf8019c1cad" )]
        [Title( "Clear Exception Log" )]
        [Description( "Clears out the contents of the Rock Exception Log." )]
        [Category( "General" )]
        public void ClearExceptionLog()
        {
            SqlCommand( "TRUNCATE TABLE [ExceptionLog]" );
        }

        /// <summary>
        /// Clears the person token table.
        /// </summary>
        [ActionId( "c81ff0e1-f605-402a-9881-251dbf84c853" )]
        [Title( "Clear Person Tokens" )]
        [Description( "Clears out the contents of the PersonToken table." )]
        [Category( "General" )]
        public void ClearPersonTokens()
        {
            SqlCommand( "TRUNCATE TABLE [PersonToken]" );
        }
    }
}
