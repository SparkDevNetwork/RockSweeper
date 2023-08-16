using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the external storage providers.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "604d8e13-9f32-4542-8916-13e204695838" )]
    [Title( "Disable External Storage Providers" )]
    [Description( "Updates the Rock configuration to ensure that storage providers other than database and filesystem are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [RequiresRockWeb]
    public class DisableExternalStorageProviders : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DisableComponentsOfType( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );

            return Task.CompletedTask;
        }
    }
}
