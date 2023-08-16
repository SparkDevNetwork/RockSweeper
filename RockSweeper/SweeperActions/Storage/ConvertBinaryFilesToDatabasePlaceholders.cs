using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.Storage
{
    /// <summary>
    /// Converts binary files stored in the database to placeholder data and
    /// then converts any non-database storage files to be placeholders
    /// stored in the database.
    /// </summary>
    [ActionId( "6be04438-0725-4ec6-b5be-adc2a03ab028" )]
    [Title( "Convert Binary Files to Database Placeholders" )]
    [Description( "Converts binary files stored in the database to placeholder data and then converts any non-database storage files to be placeholders stored in the database." )]
    [Category( "Storage" )]
    [ConflictsWithAction( typeof( MoveBinaryFilesIntoDatabase ) )]
    [ConflictsWithAction( typeof( ReplaceDatabaseImagesWithSizedPlaceholders ) )]
    [ConflictsWithAction( typeof( ReplaceDatabaseImagesWithEmptyPlaceholders ) )]
    [ConflictsWithAction( typeof( ReplaceDatabaseDocumentsWithSizedPlaceholders ) )]
    [ConflictsWithAction( typeof( ReplaceDatabaseDocumentsWithEmptyPlaceholders ) )]
    public class ConvertBinaryFilesToDatabasePlaceholders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var databaseEntityTypeId = await Sweeper.GetEntityTypeIdAsync( "Rock.Storage.Provider.Database" );

            var files = ( await Sweeper.SqlQueryAsync( "SELECT [Id],[Guid],[FileName],[MimeType],[FileSize],[Width],[Height] FROM [BinaryFile]" ) )
                .ToObjects<BinaryFile>();
            double fileCount = files.Count;
            var fileNumber = 0;

            var groupedFiles = files
                .GroupBy( f => new
                {
                    f.Width,
                    f.Height
                } );

            foreach ( var fileGroup in groupedFiles )
            {
                Stream imageStream = null;

                foreach ( var file in fileGroup )
                {
                    Sweeper.CancellationToken.ThrowIfCancellationRequested();

                    Progress( fileNumber / fileCount );

                    Stream contentStream;

                    if ( file.IsImage() )
                    {
                        if ( imageStream == null )
                        {
                            // Force to PNG format.
                            imageStream = new MemoryStream( Sweeper.CreatePlaceholderImage( "test.png", file.Width ?? 256, file.Height ?? 256 ) );
                        }

                        contentStream = imageStream;
                    }
                    else
                    {
                        contentStream = new MemoryStream( Sweeper.GetPlaceholderForBinaryFilename( file ) );
                    }

                    // Update the existing record with the size and size if we already had those.
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();
                    var path = file.IsImage() ? $"~/GetImage.ashx?Guid={file.Guid}" : $"~/GetFile.ashx?Guid={file.Guid}";

                    if ( file.FileSize.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", contentStream.Length );
                    }

                    sets.Add( "[StorageEntityTypeId] = @EntityTypeId" );
                    parameters.Add( "EntityTypeId", databaseEntityTypeId );

                    sets.Add( "[StorageEntitySettings] = NULL" );

                    sets.Add( "[Path] = @Path" );
                    parameters.Add( "Path", path );

                    if ( file.IsImage() )
                    {
                        sets.Add( "[MimeType] = 'image/png'" );
                    }

                    await Sweeper.SqlCommandAsync( $"UPDATE [BinaryFile] SET {string.Join( ", ", sets )} WHERE [Id] = {file.Id}", parameters );

                    // Update the image content.
                    await Sweeper.SqlCommandAsync( $"DELETE FROM [BinaryFileData] WHERE [Id] = {file.Id}" );
                    await Sweeper.SqlCommandAsync( $"INSERT INTO [BinaryFileData] ([Id], [Content], [Guid]) VALUES ({file.Id}, @Content, NEWID())", new Dictionary<string, object>
                    {
                        { "Content", contentStream }
                    } );

                    fileNumber++;
                }
            }
        }
    }
}
