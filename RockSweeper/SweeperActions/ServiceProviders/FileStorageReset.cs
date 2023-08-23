using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.ServiceProviders
{
    /// <summary>
    /// Resets file storage providers other than database and filesystem to system default values.
    /// </summary>
    [ActionId( "405bb345-d1a4-4bc3-861c-9fab90c0c2da" )]
    [Title( "File Storage (Reset)" )]
    [Description( "Resets file storage providers other than database and filesystem to system default values." )]
    [Category( "Service Providers" )]
    [ConflictsWithAction( typeof( FileStorageDisable ) )]
    public class FileStorageReset : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );
        }
    }
}
