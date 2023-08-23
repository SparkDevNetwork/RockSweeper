using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that all financial gateways except the test gateway are disabled.
    /// </summary>
    [ActionId( "47e49503-5a38-4781-8695-4f69d3296e7e" )]
    [Title( "Financial Gateway (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that all financial gateways except the test gateway are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( FinancialGatewayReset ) )]
    public class FinancialGatewayDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( $@"UPDATE FG
SET FG.[IsActive] = 0
FROM [FinancialGateway] AS FG
INNER JOIN[EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE ET.[Name] != 'Rock.Financial.TestGateway'
  AND ET.[Name] != 'Rock.Financial.TestRedirectionGateway'" );
        }
    }
}
