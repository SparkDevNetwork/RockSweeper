using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes the interaction data.
    /// </summary>
    [ActionId( "31200ef0-cad7-4775-8be8-c1a06a591e3f" )]
    [Title( "Sanitize Interaction Data" )]
    [Description( "Removes all custom data from Interactions, InteractionComponents and InteractionChannels." )]
    [Category( "Data Scrubbing" )]
    public class SanitizeInteractionData : SweeperAction
    {
        private readonly ConcurrentDictionary<string, string> _ipAddressMap = new ConcurrentDictionary<string, string>( 4, 100_000 );

        public override async Task ExecuteAsync()
        {
            await ProcessChannelsAsync();
            await ProcessComponentsAsync();
            await ProcessInteractionsAsync();
            await ProcessInteractionSessionsAsync();
            await ProcessInteractionSessionLocationsAsync();
        }

        private async Task ProcessChannelsAsync()
        {
            await Sweeper.SqlCommandAsync( "UPDATE [InteractionChannel] SET [ChannelData] = NULL" );
            Progress( 1, 1, 5 );
        }

        private async Task ProcessComponentsAsync()
        {
            var minId = await Sweeper.SqlScalarAsync<int>( "SELECT MIN([Id]) FROM [InteractionComponent]" );
            var maxId = await Sweeper.SqlScalarAsync<int>( "SELECT MAX([Id]) FROM [InteractionComponent]" );
            var idChunks = new List<IdChunk>();

            for ( int id = minId; id <= maxId; id += 25_000 )
            {
                idChunks.Add( new IdChunk
                {
                    First = id,
                    Last = Math.Min( id + 25_000 - 1, maxId )
                } );
            }

            var reporter = new CountProgressReporter( idChunks.Count, p => Progress( p, 2, 5 ) );

            await AsyncProducer.FromItems( idChunks )
                .Consume( async chunk =>
                {
                    await Sweeper.SqlCommandAsync( $"UPDATE [InteractionComponent] SET [ComponentData] = NULL WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" );
                    reporter.Add( 1 );
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task ProcessInteractionsAsync()
        {
            var minId = await Sweeper.SqlScalarAsync<int>( "SELECT MIN([Id]) FROM [Interaction]" );
            var maxId = await Sweeper.SqlScalarAsync<int>( "SELECT MAX([Id]) FROM [Interaction]" );
            var idChunks = new List<IdChunk>();

            for ( int id = minId; id <= maxId; id += 25_000 )
            {
                idChunks.Add( new IdChunk
                {
                    First = id,
                    Last = Math.Min( id + 25_000 - 1, maxId )
                } );
            }

            var reporter = new CountProgressReporter( idChunks.Count, p => Progress( p, 3, 5 ) );

            await AsyncProducer.FromItems( idChunks )
                .Consume( async chunk =>
                {
                    await Sweeper.SqlCommandAsync( $"UPDATE [Interaction] SET [InteractionData] = NULL WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" );
                    reporter.Add( 1 );
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task ProcessInteractionSessionsAsync()
        {
            var minId = await Sweeper.SqlScalarAsync<int>( "SELECT MIN([Id]) FROM [InteractionSession]" );
            var maxId = await Sweeper.SqlScalarAsync<int>( "SELECT MAX([Id]) FROM [InteractionSession]" );
            var idChunks = new List<IdChunk>();

            for ( int id = minId; id <= maxId; id += 25_000 )
            {
                idChunks.Add( new IdChunk
                {
                    First = id,
                    Last = Math.Min( id + 25_000 - 1, maxId )
                } );
            }

            var reporter = new CountProgressReporter( idChunks.Count, p => Progress( p, 4, 5 ) );

            var x = AsyncProducer.FromItems( idChunks )
                .Pipe( async chunk =>
                {
                    var sessions = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [IpAddress] FROM [InteractionSession] WITH (NOLOCK) WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" ) ).ToObjects<InteractionSession>();
                    var setNullTask = Sweeper.SqlCommandAsync( $"UPDATE [InteractionSession] SET [SessionData] = NULL, [InteractionSessionLocationId] = NULL WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" );

                    var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                    foreach ( var session in sessions )
                    {
                        var changes = new Dictionary<string, object>();

                        if ( !string.IsNullOrWhiteSpace( session.IpAddress ) )
                        {
                            changes["IpAddress"] = _ipAddressMap.GetOrAdd( session.IpAddress, oldIp =>
                            {
                                if ( oldIp.Contains( ":" ) )
                                {
                                    return Sweeper.DataFaker.Internet.Ipv6();
                                }
                                else
                                {
                                    return Sweeper.DataFaker.Internet.Ip();
                                }
                            } );
                        }

                        if ( changes.Any() )
                        {
                            bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( session.Id, changes ) );
                        }
                    }

                    await setNullTask;

                    return bulkChanges;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "InteractionSession", changes );
                    }

                    reporter.Add( 1 );
                } );
            await x.RunAsync( Sweeper.CancellationToken );
        }

        private async Task ProcessInteractionSessionLocationsAsync()
        {
            var minId = await Sweeper.SqlScalarAsync<int>( "SELECT MIN([Id]) FROM [InteractionSessionLocation]" );
            var maxId = await Sweeper.SqlScalarAsync<int>( "SELECT MAX([Id]) FROM [InteractionSessionLocation]" );
            var idChunks = new List<IdChunk>();

            for ( int id = minId; id <= maxId; id += 25_000 )
            {
                idChunks.Add( new IdChunk
                {
                    First = id,
                    Last = Math.Min( id + 25_000 - 1, maxId )
                } );
            }

            var reporter = new CountProgressReporter( idChunks.Count, p => Progress( p, 5, 5 ) );

            await AsyncProducer.FromItems( idChunks )
                .Consume( async chunk =>
                {
                    await Sweeper.SqlCommandAsync( $"DELETE FROM [InteractionSessionLocation] WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" );

                    reporter.Add( 1 );
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private class IdChunk
        {
            public int First { get; set; }

            public int Last { get; set; }
        }

        private class InteractionSession
        {
            public int Id { get; set; }

            public string IpAddress { get; set; }
        }
    }
}
