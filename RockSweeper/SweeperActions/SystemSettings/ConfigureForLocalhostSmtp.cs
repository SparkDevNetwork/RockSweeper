using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Configures Rock to use localhost SMTP email delivery.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "24ea3e6e-04ab-4896-9174-bc275f67f766" )]
    [Title( "Configure For Localhost SMTP" )]
    [Description( "Updates the communication settings to use a localhost SMTP server." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [AfterAction( typeof( DisableCommunicationTransports ) )]
    [AfterAction( typeof( ResetCommunicationTransports ) )]
    public class ConfigureForLocalhostSmtp : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            //
            // Setup the Email medium.
            //
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Medium.Email", "Active", "True" );
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Medium.Email", "TransportContainer", "1fef44b2-8685-4001-be5b-8a059bc65430" );

            //
            // Set SMTP Transport to Active.
            //
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Active", "True" );
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Server", "localhost" );
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Port", "25" );
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "UserName", "" );
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "Password", "" );
            await Sweeper.SetComponentAttributeValue( "Rock.Communication.Transport.SMTP", "UseSSL", "False" );
        }
    }
}
