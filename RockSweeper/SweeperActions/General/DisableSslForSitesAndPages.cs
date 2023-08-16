using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.General
{
    /// <summary>
    /// Disables the SSL requirement for sites and pages.
    /// </summary>
    [ActionId( "8c1155d0-6319-4750-a7ca-0d713740bfde" )]
    [Title( "Disable SSL for Sites and Pages" )]
    [Description( "Modifies all Sites and Pages and removes the requirement for an SSL connection." )]
    [Category( "General" )]
    [DefaultValue( true )]
    public class DisableSslForSitesAndPages : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.SqlCommand( "UPDATE [Site] SET [RequiresEncryption] = 0" );
            Sweeper.SqlCommand( "UPDATE [Page] SET [RequiresEncryption] = 0" );

            return Task.CompletedTask;
        }
    }
}
