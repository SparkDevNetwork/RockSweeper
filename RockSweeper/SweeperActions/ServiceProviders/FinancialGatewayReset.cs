using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all financial gateways except the test gateway to system default values.
    /// </summary>
    [ActionId( "57da3ba1-4166-446a-a998-be7229a32b52" )]
    [Title( "Financial Gateway (Reset)" )]
    [Description( "Resets all financial gateways except the test gateway to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( FinancialGatewayDisable ) )]
    public class FinancialGatewayReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            int? entityTypeId = await Sweeper.GetEntityTypeIdAsync( "Rock.Model.FinancialGateway" );

            await Sweeper.SqlCommandAsync( $@"DELETE AV
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
INNER JOIN [FinancialGateway] AS FG ON FG.[Id] = AV.[EntityId]
INNER JOIN [EntityType] AS ET ON ET.[Id] = FG.[EntityTypeId]
WHERE A.[EntityTypeId] = {entityTypeId.Value} AND ET.[Name] != 'Rock.Financial.TestGateway'" );
        }
    }
}
