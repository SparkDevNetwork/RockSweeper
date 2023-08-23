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
    /// Replaces the database documents with sized placeholders.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "56e602d7-a9a4-4a4d-99bb-2df5e11a4404" )]
    [Title( "Replace Database Documents With Sized Placeholders" )]
    [Description( "Replaces any database-stored non-PNG and non-JPG files with placeholder text of the original file size." )]
    [Category( "Storage" )]
    [AfterAction( typeof( MoveBinaryFilesIntoDatabase ) )]
    [AfterAction( typeof( DataScrubbing.BackgroundCheckRemoveData ) )]
    public class ReplaceDatabaseDocumentsWithSizedPlaceholders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var databaseEntityTypeId = await Sweeper.GetEntityTypeIdAsync( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = ( await Sweeper.SqlQueryAsync<int, string, long?>( $"SELECT [Id],[FileName],[FileSize] FROM [BinaryFile] WHERE [StorageEntityTypeId] = {databaseEntityTypeId}" ) )
                .Where( f => !Sweeper.IsFileNameImage( f.Item2 ) )
                .ToList();

            async Task processFile( Tuple<int, string, long?> file )
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
                    using ( var ms = await Sweeper.GetFileDataFromBinaryFileDataAsync( fileId ) )
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
                        await Sweeper.SqlCommandAsync( $"UPDATE [BinaryFile] SET {string.Join( ", ", sets )} WHERE [Id] = {fileId}", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    await Sweeper.SqlCommandAsync( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = {fileId}", new Dictionary<string, object>
                    {
                        { "Content", fileStream }
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
