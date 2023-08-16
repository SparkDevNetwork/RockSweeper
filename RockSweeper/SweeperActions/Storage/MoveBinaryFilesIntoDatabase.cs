using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.Storage
{
    /// <summary>
    /// Moves all the binary files into database.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "abbe9ca5-6915-4e40-95b4-9c485c09f382" )]
    [Title( "Move Binary Files Into Database" )]
    [Description( "Moves any binary file data stored externally into the database, this includes any filesystem storage." )]
    [Category( "Storage" )]
    public class MoveBinaryFilesIntoDatabase : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            var databaseEntityTypeId = Sweeper.GetEntityTypeId( "Rock.Storage.Provider.Database" );

            var files = Sweeper.SqlQuery<int, Guid, string>( $"SELECT [Id],[Guid],[FileName] FROM [BinaryFile] WHERE [StorageEntityTypeId] != {databaseEntityTypeId}" );
            double fileCount = files.Count;

            for ( int i = 0; i < files.Count; i++ )
            {
                int fileId = files[i].Item1;
                Guid fileGuid = files[i].Item2;
                string fileName = files[i].Item3;

                Sweeper.CancellationToken.ThrowIfCancellationRequested();

                Progress( i / fileCount );

                using ( var ms = Sweeper.GetFileDataFromRock( fileGuid ) )
                {
                    string path = Sweeper.IsFileNameImage( fileName ) ? $"~/GetImage.ashx?Guid={fileGuid}" : $"~/GetFile.ashx?Guid={fileGuid}";

                    Sweeper.SqlCommand( $"UPDATE [BinaryFile] SET [StorageEntityTypeId] = @EntityTypeId, [StorageEntitySettings] = NULL, [Path] = @Path WHERE [Id] = {fileId}", new Dictionary<string, object>
                    {
                        { "EntityTypeId", databaseEntityTypeId },
                        { "Path", path }
                    } );

                    Sweeper.SqlCommand( $"DELETE FROM [BinaryFileData] WHERE [Id] = {fileId}" );
                    Sweeper.SqlCommand( $"INSERT INTO [BinaryFileData] ([Id], [Content], [Guid]) VALUES ({fileId}, @Content, NEWID())", new Dictionary<string, object>
                    {
                        { "Content", ms }
                    } );
                }
            }

            return Task.CompletedTask;
        }
    }
}
