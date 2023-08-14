using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper
{
    public partial class SweeperController
    {
        /// <summary>
        /// Moves all the binary files into database.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "abbe9ca5-6915-4e40-95b4-9c485c09f382" )]
        [Title( "Move Binary Files Into Database" )]
        [Description( "Moves any binary file data stored externally into the database, this includes any filesystem storage." )]
        [Category( "Storage" )]
        public void MoveBinaryFilesIntoDatabase()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );

            var files = SqlQuery<int, Guid, string>( $"SELECT [Id],[Guid],[FileName] FROM [BinaryFile] WHERE [StorageEntityTypeId] != { databaseEntityTypeId }" );
            double fileCount = files.Count;

            for ( int i = 0; i < files.Count; i++ )
            {
                int fileId = files[i].Item1;
                Guid fileGuid = files[i].Item2;
                string fileName = files[i].Item3;

                CancellationToken.ThrowIfCancellationRequested();

                Progress( i / fileCount );

                using ( var ms = GetFileDataFromRock( fileGuid ) )
                {
                    string path = IsFileNameImage( fileName ) ? $"~/GetImage.ashx?Guid={ fileGuid }" : $"~/GetFile.ashx?Guid={ fileGuid }";

                    SqlCommand( $"UPDATE [BinaryFile] SET [StorageEntityTypeId] = @EntityTypeId, [StorageEntitySettings] = NULL, [Path] = @Path WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "EntityTypeId", databaseEntityTypeId },
                        { "Path", path }
                    } );

                    SqlCommand( $"DELETE FROM [BinaryFileData] WHERE [Id] = { fileId }" );
                    SqlCommand( $"INSERT INTO [BinaryFileData] ([Id], [Content], [Guid]) VALUES ({ fileId }, @Content, NEWID())", new Dictionary<string, object>
                    {
                        { "Content", ms }
                    } );
                }
            }
        }

        [ActionId( "6be04438-0725-4ec6-b5be-adc2a03ab028" )]
        [Title( "Convert Binary Files to Database Placeholders" )]
        [Description( "Converts binary files stored in the database to placeholder data and then converts any non-database storage files to be placeholders stored in the database." )]
        [Category( "Storage" )]
        [ConflictsWithAction( nameof( MoveBinaryFilesIntoDatabase ) )]
        public void ConvertBinaryFilesToDatabasePlaceholders()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );

            var files = SqlQuery( "SELECT [Id],[Guid],[FileName],[MimeType],[FileSize],[Width],[Height] FROM [BinaryFile]" )
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
                    CancellationToken.ThrowIfCancellationRequested();

                    Progress( fileNumber / fileCount );

                    Stream contentStream;

                    if ( file.IsImage() )
                    {
                        if ( imageStream == null )
                        {
                            // Force to PNG format.
                            imageStream = new MemoryStream( CreatePlaceholderImage( "test.png", file.Width ?? 256, file.Height ?? 256 ) );
                        }

                        contentStream = imageStream;
                    }
                    else
                    {
                        contentStream = new MemoryStream( new byte[0] );
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

                    SqlCommand( $"UPDATE [BinaryFile] SET {string.Join( ", ", sets )} WHERE [Id] = {file.Id}", parameters );

                    // Update the image content.
                    SqlCommand( $"DELETE FROM [BinaryFileData] WHERE [Id] = {file.Id}" );
                    SqlCommand( $"INSERT INTO [BinaryFileData] ([Id], [Content], [Guid]) VALUES ({file.Id}, @Content, NEWID())", new Dictionary<string, object>
                    {
                        { "Content", contentStream }
                    } );

                    fileNumber++;
                }

                imageStream?.Dispose();
            }
        }

        /// <summary>
        /// Replaces the database images with correctly sized placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "b18bf1ae-b391-4b71-8c4a-f88afe457f1d" )]
        [Title( "Replace Database Images With Sized Placeholders" )]
        [Description( "Replaces any database-stored PNG or JPG files with correctly sized placeholders." )]
        [Category( "Storage" )]
        [AfterAction( nameof( MoveBinaryFilesIntoDatabase ) )]
        public void ReplaceDatabaseImagesWithSizedPlaceholders()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;
            List<Tuple<int, string, long?, int?, int?>> files;

            try
            {
                files = SqlQuery<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],[Width],[Height] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => IsFileNameImage( f.Item2 ) )
                .ToList();
            }
            catch
            {
                files = SqlQuery<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],NULL,NULL FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                    .Where( f => IsFileNameImage( f.Item2 ) )
                    .ToList();
            }

            void processFile( Tuple<int, string, long?, int?, int?> file )
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
                    using ( var ms = GetFileDataFromBinaryFileData( fileId ) )
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

                using ( var imageStream = new MemoryStream( CreatePlaceholderImage( filename, width, height ) ) )
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
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    // Update the image content.
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", imageStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database images with empty placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "99fb22cf-94cd-4f44-95e3-142d54a176e1" )]
        [Title( "Replace Database Images With Empty Placeholders" )]
        [Description( "Replaces any database-stored PNG or JPG files with 1x1 pixel placeholders." )]
        [Category( "Storage" )]
        [AfterAction( nameof( MoveBinaryFilesIntoDatabase ) )]
        public void ReplaceDatabaseImagesWithEmptyPlaceholders()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?, int?, int?>( $"SELECT [Id],[FileName],[FileSize],[Width],[Height] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?, int?, int?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                int width = 1;
                int height = 1;

                using ( var imageStream = new MemoryStream( CreatePlaceholderImage( filename, width, height ) ) )
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
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", imageStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database documents with sized placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "56e602d7-a9a4-4a4d-99bb-2df5e11a4404" )]
        [Title( "Replace Database Documents With Sized Placeholders" )]
        [Description( "Replaces any database-stored non-PNG and non-JPG files with placeholder text of the original file size." )]
        [Category( "Storage" )]
        [AfterAction( nameof( MoveBinaryFilesIntoDatabase ) )]
        public void ReplaceDatabaseDocumentsWithSizedPlaceholders()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?>( $"SELECT [Id],[FileName],[FileSize] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => !IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;
                long fileSize;

                //
                // Determine if we already have the file size or if we need to calculate it.
                //
                if ( file.Item3.HasValue && file.Item3.Value > 0 )
                {
                    fileSize = file.Item3.Value;
                }
                else
                {
                    using ( var ms = GetFileDataFromBinaryFileData( fileId ) )
                    {
                        fileSize = ms.Length;
                    }
                }

                using ( var fileStream = new MemoryStream() )
                {
                    byte[] data = new byte[4096];

                    for ( int i = 0; i < data.Length; i++ )
                    {
                        data[i] = ( byte ) 'X';
                    }

                    while ( fileStream.Length < fileSize )
                    {
                        var len = Math.Min( data.Length, fileSize - fileStream.Length );

                        fileStream.Write( data, 0, ( int ) len );
                    }

                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", fileStream.Length );
                    }

                    if ( sets.Any() )
                    {
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", fileStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        /// <summary>
        /// Replaces the database documents with empty placeholders.
        /// </summary>
        /// <param name="actionData">The action data.</param>
        [ActionId( "f3e7806e-8142-4825-b3f4-70e71462dda5" )]
        [Title( "Replace Database Documents With Empty Placeholders" )]
        [Description( "Replaces any database-stored non-PNG and non-JPG files with empty file content." )]
        [Category( "Storage" )]
        [AfterAction( nameof( MoveBinaryFilesIntoDatabase ) )]
        public void ReplaceDatabaseDocumentsWithEmptyPlaceholders()
        {
            var databaseEntityTypeId = GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = SqlQuery<int, string, long?>( $"SELECT [Id],[FileName],[FileSize] FROM [BinaryFile] WHERE [StorageEntityTypeId] = { databaseEntityTypeId }" )
                .Where( f => !IsFileNameImage( f.Item2 ) )
                .ToList();

            void processFile( Tuple<int, string, long?> file )
            {
                int fileId = file.Item1;
                string filename = file.Item2;

                using ( var fileStream = new MemoryStream() )
                {
                    //
                    // Update the existing record with the size and size if we already had those.
                    //
                    var parameters = new Dictionary<string, object>();
                    var sets = new List<string>();

                    if ( file.Item3.HasValue )
                    {
                        sets.Add( "[FileSize] = @Size" );
                        parameters.Add( "Size", fileStream.Length );
                    }

                    if ( sets.Any() )
                    {
                        SqlCommand( $"UPDATE [BinaryFile] SET { string.Join( ", ", sets ) } WHERE [Id] = { fileId }", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = { fileId }", new Dictionary<string, object>
                    {
                        { "Content", fileStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                CancellationToken.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }
        }

        #region Private Methods

        /// <summary>
        /// Creates a new placeholder image of the given dimensions.
        /// </summary>
        /// <param name="originalFilename">The original filename, used to determine the output format.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <returns>A byte array that represents the encoded format.</returns>
        private byte[] CreatePlaceholderImage( string originalFilename, int width, int height )
        {
            using ( var imageStream = new MemoryStream() )
            {
                // Generate the new image.
                try
                {
                    var image = new Bitmap( width, height );
                    var g = Graphics.FromImage( image );

                    g.FillRectangle( Brushes.White, new Rectangle( 0, 0, width, height ) );

                    if ( width > 128 && height > 64 )
                    {
                        var font = new Font( "Tahoma", height / 10 );
                        var sizeText = $"{width}x{height}";
                        var size = g.MeasureString( sizeText, font );
                        g.DrawString( sizeText, font, Brushes.Black, new PointF( ( width - size.Width ) / 2, ( height - size.Height ) / 2 ) );
                    }

                    g.Flush();

                    image.SetResolution( 72, 72 );

                    if ( originalFilename.EndsWith( ".png", StringComparison.CurrentCultureIgnoreCase ) )
                    {
                        image.Save( imageStream, System.Drawing.Imaging.ImageFormat.Png );
                    }
                    else
                    {
                        var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                            .Where( c => c.MimeType == "image/jpeg" )
                            .First();

                        var encoderParameters = new System.Drawing.Imaging.EncoderParameters( 1 );
                        encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter( System.Drawing.Imaging.Encoder.Quality, 50L );

                        image.Save( imageStream, encoder, encoderParameters );
                    }

                    imageStream.Position = 0;
                }
                catch
                {
                    imageStream.Position = 0;
                    imageStream.SetLength( 0 );
                }

                return imageStream.ToArray();
            }
        }

        #endregion
    }
}
