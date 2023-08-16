﻿using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the external storage providers.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "405bb345-d1a4-4bc3-861c-9fab90c0c2da" )]
    [Title( "Reset External Storage Providers" )]
    [Description( "Resets storage providers other than database and filesystem to system default values." )]
    [Category( "System Settings" )]
    [RequiresRockWeb]
    public class ResetExternalStorageProviders : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DeleteAttributeValuesForComponentsOfType( "Rock.Storage.ProviderComponent", new[]
            {
                "Rock.Storage.Provider.Database",
                "Rock.Storage.Provider.FileSystem"
            } );

            return Task.CompletedTask;
        }
    }
}