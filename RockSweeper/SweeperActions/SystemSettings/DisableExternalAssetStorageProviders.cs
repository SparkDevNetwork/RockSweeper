using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the external asset storage providers.
    /// </summary>
    [ActionId( "cc02ebd8-ad71-439f-85d0-46c7f56ae4e2" )]
    [Title( "Disable External Asset Storage Providers" )]
    [Description( "Updates the Rock configuration to ensure that asset storage providers other than filesystem are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetExternalAssetStorageProviders ) )]
    public class DisableExternalAssetStorageProviders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Storage.AssetStorage.AssetStorageComponent", new[]
            {
                "Rock.Storage.AssetStorage.FileSystemComponent"
            } );
        }
    }
}
