using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the financial gateways.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "47e49503-5a38-4781-8695-4f69d3296e7e" )]
    [Title( "Disable Financial Gateways" )]
    [Description( "Updates the Rock configuration to ensure that all financial gateways except the test gateway are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    public class DisableFinancialGateways : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( $@"UPDATE FG
SET FG.[IsActive] = 0
FROM [FinancialGateway] AS FG
INNER JOIN[EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE ET.[Name] != 'Rock.Financial.TestGateway'" );
        }
    }
}
