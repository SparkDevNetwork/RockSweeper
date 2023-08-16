using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the google API keys.
    /// </summary>
    [ActionId( "d5e352c6-b1bf-405a-9934-6f875725a5c1" )]
    [Title( "Reset Google API Keys" )]
    [Description( "Clears the Google API keys stored in global attributes." )]
    [Category( "System Settings" )]
    public class ResetGoogleApiKeys : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SetGlobalAttributeValue( "GoogleAPIKey", string.Empty );
            await Sweeper.SetGlobalAttributeValue( "core_GoogleReCaptchaSiteKey", string.Empty );
        }
    }
}
