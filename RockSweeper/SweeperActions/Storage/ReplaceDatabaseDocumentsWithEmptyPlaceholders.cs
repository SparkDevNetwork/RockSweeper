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
    /// Replaces the database documents with empty placeholders.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "f3e7806e-8142-4825-b3f4-70e71462dda5" )]
    [Title( "Replace Database Documents With Empty Placeholders" )]
    [Description( "Replaces any database-stored non-PNG and non-JPG files with empty file content." )]
    [Category( "Storage" )]
    [AfterAction( typeof( MoveBinaryFilesIntoDatabase ) )]
    public class ReplaceDatabaseDocumentsWithEmptyPlaceholders : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            var databaseEntityTypeId = Sweeper.GetEntityTypeId( "Rock.Storage.Provider.Database" );
            int completedCount = 0;
            double fileCount = 0;

            var files = Sweeper.SqlQuery<int, string, long?>( $"SELECT [Id],[FileName],[FileSize] FROM [BinaryFile] WHERE [StorageEntityTypeId] = {databaseEntityTypeId}" )
                .Where( f => !Sweeper.IsFileNameImage( f.Item2 ) )
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
                        Sweeper.SqlCommand( $"UPDATE [BinaryFile] SET {string.Join( ", ", sets )} WHERE [Id] = {fileId}", parameters );
                    }

                    //
                    // Update the image content.
                    //
                    Sweeper.SqlCommand( $"UPDATE [BinaryFileData] SET [Content] = @Content WHERE [Id] = {fileId}", new Dictionary<string, object>
                    {
                        { "Content", fileStream }
                    } );
                }
            };

            fileCount = files.Count;

            foreach ( var file in files )
            {
                Sweeper.CancellationToken.ThrowIfCancellationRequested();

                processFile( file );

                completedCount += 1;

                if ( completedCount % 10 == 0 )
                {
                    Progress( completedCount / fileCount );
                }
            }

            return Task.CompletedTask;
        }
    }
}
