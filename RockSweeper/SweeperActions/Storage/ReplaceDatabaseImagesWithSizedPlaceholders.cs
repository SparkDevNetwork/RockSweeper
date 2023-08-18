using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.Storage
{
    /// <summary>
    /// Replaces the database images with correctly sized placeholders.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "b18bf1ae-b391-4b71-8c4a-f88afe457f1d" )]
    [Title( "Replace Database Images With Sized Placeholders" )]
    [Description( "Replaces any database-stored PNG or JPG files with correctly sized placeholders." )]
    [Category( "Storage" )]
    [AfterAction( typeof( MoveBinaryFilesIntoDatabase ) )]
    [AfterAction( typeof( DataScrubbing.RemoveBackgroundCheckData ) )]
    public class ReplaceDatabaseImagesWithSizedPlaceholders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var databaseEntityTypeId = await Sweeper.GetEntityTypeIdAsync( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;
            List<Tuple<int, string, long?, int?, int?>> files;

            try
            {
                files = ( await Sweeper.SqlQueryAsync<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],[Width],[Height] FROM [BinaryFile] WHERE [StorageEntityTypeId] = {databaseEntityTypeId}" ) )
                .Where( f => Sweeper.IsFileNameImage( f.Item2 ) )
                .ToList();
            }
            catch
            {
                files = ( await Sweeper.SqlQueryAsync<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],NULL,NULL FROM [BinaryFile] WHERE [StorageEntityTypeId] = {databaseEntityTypeId}" ) )
                    .Where( f => Sweeper.IsFileNameImage( f.Item2 ) )
                    .ToList();
            }

            async Task processFile( Tuple<int, string, long?, int?, int?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                int width;
                int height;

                //
                // Determine if we already have the image size or if we need to calculate it.
                //
                if ( file.Item4.HasValue && file.Item4.Value > 0 && file.Item5.HasValue && file.Item5.Value > 0 )
                {
                    width = file.Item4.Value;
                    height = file.Item5.Value;
                }
                else
                {
                    using ( var ms = await Sweeper.GetFileDataFromBinaryFileDataAsync( fileId ) )
                    {
                        try
                        {
                            var image = new Bitmap( ms );

                            width = image.Width;
                            height = image.Height;
                        }
                        catch
                        {
                            width = 100;
                            height = 100;
                        }
                    }
                }

                using ( var imageStream = new MemoryStream( Sweeper.CreatePlaceholderImage( filename, width, height ) ) )
                {
                    // Update the existing record with the size and size if we already had those.
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

                    // Update the image content.
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
