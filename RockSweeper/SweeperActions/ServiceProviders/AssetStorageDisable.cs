using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that asset storage providers other than filesystem are disabled.
    /// </summary>
    [ActionId( "cc02ebd8-ad71-439f-85d0-46c7f56ae4e2" )]
    [Title( "Asset Storage (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that asset storage providers other than filesystem are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( AssetStorageReset ) )]
    public class AssetStorageDisable : SweeperAction
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
