using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Cleans all media elements, folders and accounts information.
    /// </summary>
    [ActionId( "811a270f-f655-4332-8737-367acf88b496" )]
    [Title( "Media Element Data" )]
    [Description( "Cleans all media elements, folders and account information." )]
    [Category( "Data Scrubbing" )]
    public class MediaElementData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            await ProcessMediaAccounts();
            await ProcessMediaFolders();
            await ProcessMediaElements();
        }

        #region Media Accounts

        private async Task ProcessMediaAccounts()
        {
            var accounts = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Name] FROM [MediaAccount] ORDER BY [Id]" ) ).ToObjects<MediaAccount>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in accounts )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( account.Name ) )
                {
                    changes["Name"] = Sweeper.DataFaker.Lorem.ReplaceWords( account.Name );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( account.Id, changes ) );
                }
            }

            await Sweeper.UpdateDatabaseRecordsAsync( "MediaAccount", bulkUpdates );

            Progress( 1, 1, 3 );
        }

        private class MediaAccount
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        #endregion

        #region Media Folders

        private async Task ProcessMediaFolders()
        {
            var folders = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Name], [Description] FROM [MediaFolder] ORDER BY [Id]" ) ).ToObjects<MediaFolder>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var folder in folders )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( folder.Name ) )
                {
                    changes["Name"] = Sweeper.DataFaker.Lorem.ReplaceWords( folder.Name );
                }

                if ( !string.IsNullOrWhiteSpace( folder.Description ) )
                {
                    changes["Description"] = Sweeper.DataFaker.Lorem.ReplaceWords( folder.Description );
                }

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( folder.Id, changes ) );
                }
            }

            await Sweeper.UpdateDatabaseRecordsAsync( "MediaFolder", bulkUpdates );

            Progress( 1, 2, 3 );
        }

        private class MediaFolder
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }
        }

        #endregion

        #region Media Elements

        private async Task ProcessMediaElements()
        {
            var ids = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [MediaElement] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, 2, 2 ) );

            // Use a smaller chunk of 500 since the JSON data might be quite large.
            await AsyncProducer.FromItems( ids.Chunk( 500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubMediaElementsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "MediaElement", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubMediaElementsAsync( List<int> ids )
        {
            var elements = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [Name], [Description], [ThumbnailDataJson], [FileDataJson] FROM [MediaElement] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" ) ).ToObjects<MediaElement>();
            var bulkUpdates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var element in elements )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( element.Name ) )
                {
                    changes["Name"] = Sweeper.DataFaker.Lorem.ReplaceWords( element.Name );
                }

                if ( !string.IsNullOrWhiteSpace( element.Description ) )
                {
                    changes["Description"] = Sweeper.DataFaker.Lorem.ReplaceWords( element.Description );
                }

                changes["ThumbnailDataJson"] = "[]";
                changes["FileDataJson"] = "[]";

                if ( changes.Any() )
                {
                    bulkUpdates.Add( new Tuple<int, Dictionary<string, object>>( element.Id, changes ) );
                }
            }

            return bulkUpdates;
        }

        private class MediaElement
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public string ThumbnailDataJson { get; set; }

            public string FileDataJson { get; set; }
        }

        #endregion
    }
}
