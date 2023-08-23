using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets asset storage providers other than filesystem to system default values.
    /// </summary>
    [ActionId( "e357c385-00bb-499b-b19e-f9438dba5f86" )]
    [Title( "Asset Storage (Reset)" )]
    [Description( "Resets asset storage providers other than filesystem to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( AssetStorageDisable ) )]
    public class AssetStorageReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Storage.AssetStorage.AssetStorageComponent", new[]
            {
                "Rock.Storage.AssetStorage.FileSystemComponent"
            } );
        }
    }
}
