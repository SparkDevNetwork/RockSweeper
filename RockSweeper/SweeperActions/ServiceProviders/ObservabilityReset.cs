using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets all observability settings to system default values.
    /// </summary>
    [ActionId( "12956c26-2965-4c40-9cfd-28c542357e55" )]
    [Title( "Observability (Reset)" )]
    [Description( "Resets all observability settings to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( ObservabilityDisable ) )]
    public class ObservabilityReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SqlCommandAsync( "DELETE FROM [Attribute] WHERE [EntityTypeQualifierColumn] = 'SystemSetting' AND [Key] LIKE 'core_Observability%'" );
        }
    }
}
