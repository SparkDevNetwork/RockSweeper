using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.Storage
{
    /// <summary>
    /// Replaces the database images with empty placeholders.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "99fb22cf-94cd-4f44-95e3-142d54a176e1" )]
    [Title( "Replace Database Images With Empty Placeholders" )]
    [Description( "Replaces any database-stored PNG or JPG files with 1x1 pixel placeholders." )]
    [Category( "Storage" )]
    [AfterAction( typeof( MoveBinaryFilesIntoDatabase ) )]
    [AfterAction( typeof( DataScrubbing.RemoveBackgroundCheckData ) )]
    public class ReplaceDatabaseImagesWithEmptyPlaceholders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var databaseEntityTypeId = await Sweeper.GetEntityTypeIdAsync( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = ( await Sweeper.SqlQueryAsync<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],[Width],[Height] FROM [BinaryFile] WHERE [StorageEntityTypeId] = {databaseEntityTypeId}" ) )
                .Where( f => Sweeper.IsFileNameImage( f.Item2 ) )
                .ToList();

            async Task processFile( Tuple<int, string, long?, int?, int?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                int width = 1;
                int height = 1;

                using ( var imageStream = new MemoryStream( Sweeper.CreatePlaceholderImage( filename, width, height ) ) )
                {
                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", imageStream.Length );
                    }

                    if ( file.Item4.HasValue )
                    {
                        sets.Add( "[Width] = @Width" );
                        parameters.Add( "Width", width );
                    }

                    if ( file.Item5.HasValue )
                    {
                        sets.Add( "[Height] = @Height" );
                        parameters.Add( "Height", height );
                    }

                    if ( sets.Any() )
                    {
                        await Sweeper.SqlCommandAsync( $"UPDATE [BinaryFile] SET {string.Join( ", ", sets )} WHERE [Id] = {fileId}", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    await Sweeper.SqlCommandAsync( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = {fileId}", new Dictionary<string, object>
                    {
                        { "Content", imageStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                Sweeper.CancellationToken.ThrowIfCancellationRequested();

                await processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }
    }
}
