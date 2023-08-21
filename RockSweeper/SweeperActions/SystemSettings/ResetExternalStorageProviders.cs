using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the external storage providers.
    /// </summary>
    [ActionId( "405bb345-d1a4-4bc3-861c-9fab90c0c2da" )]
    [Title( "Reset External Storage Providers" )]
    [Description( "Resets storage providers other than database and filesystem to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisableExternalStorageProviders ) )]
    public class ResetExternalStorageProviders : SweeperAction
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
