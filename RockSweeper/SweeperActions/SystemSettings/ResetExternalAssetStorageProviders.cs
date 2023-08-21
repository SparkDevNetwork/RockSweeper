using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the external asset storage providers.
    /// </summary>
    [ActionId( "e357c385-00bb-499b-b19e-f9438dba5f86" )]
    [Title( "Reset External Asset Storage Providers" )]
    [Description( "Resets asset storage providers other than filesystem to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableExternalAssetStorageProviders ) )]
    public class ResetExternalAssetStorageProviders : SweeperAction
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
