using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to that the observability feature is disabled.
    /// </summary>
    [ActionId( "5e27c631-3681-4d1b-b50c-0e5f3898e541" )]
    [Title( "Observability (Disable)" )]
    [Description( "Updates the Rock configuration to that the observability feature is disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ObservabilityReset ) )]
    public class ObservabilityDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( "DELETE FROM [Attribute] WHERE [EntityTypeQualifierColumn] = 'SystemSetting' AND [Key] = 'core_ObservabilityEnabled'" );
        }
    }
}
