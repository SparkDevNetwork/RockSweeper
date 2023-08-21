using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the external storage providers.
    /// </summary>
    [ActionId( "604d8e13-9f32-4542-8916-13e204695838" )]
    [Title( "Disable External Storage Providers" )]
    [Description( "Updates the Rock configuration to ensure that storage providers other than database and filesystem are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetExternalStorageProviders ) )]
    public class DisableExternalStorageProviders : SweeperAction
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
