using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Updates the Rock configuration to ensure that file storage providers other than database and filesystem are disabled.
    /// </summary>
    [ActionId( "604d8e13-9f32-4542-8916-13e204695838" )]
    [Title( "File Storage (Disable)" )]
    [Description( "Updates the Rock configuration to ensure that file storage providers other than database and filesystem are disabled." )]
    [Category( "Service Providers" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( FileStorageReset ) )]
    public class FileStorageDisable : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );
        }
    }
}
