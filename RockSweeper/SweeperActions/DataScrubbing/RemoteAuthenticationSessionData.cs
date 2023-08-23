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
    /// Cleans remote authentcation session table to removing identifying information.
    /// </summary>
    [ActionId( "f81ab6fa-a219-40d1-9455-ea66c0d7aef4" )]
    [Title( "Remote Authentication Sessions" )]
    [Description( "Cleans remote authentcation session table to removing identifying information." )]
    [Category( "Data Scrubbing" )]
    public class RemoteAuthenticationSessionData: SweeperAction
    {
        private readonly ConcurrentDictionary<string, string> _ipAddressMap = new ConcurrentDictionary<string, string>( 4, 10_000 );

        public override async Task ExecuteAsync()
        {
            var minId = ( await Sweeper.SqlScalarAsync<int?>( "SELECT MIN([Id]) FROM [RemoteAuthenticationSession]" ) ?? 0 );
            var maxId = ( await Sweeper.SqlScalarAsync<int?>( "SELECT MAX([Id]) FROM [RemoteAuthenticationSession]" ) ?? 0 );
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

            await AsyncProducer.FromItems( idChunks )
                .Pipe( async chunk =>
                {
                    var sessions = ( await Sweeper.SqlQueryAsync( $"SELECT [Id], [ClientIpAddress], [AuthenticationIpAddress] FROM [RemoteAuthenticationSession] WITH (NOLOCK) WHERE [Id] >= {chunk.First} AND [Id] <= {chunk.Last}" ) ).ToObjects<RemoteAuthenticationsession>();
                    var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                    foreach ( var session in sessions )
                    {
                        var changes = new Dictionary<string, object>();

                        if ( !string.IsNullOrWhiteSpace( session.ClientIpAddress ) )
                        {
                            changes["ClientIpAddress"] = GetNewIpAddress( session.ClientIpAddress );
                        }

                        if ( !string.IsNullOrWhiteSpace( session.AuthenticationIpAddress ) )
                        {
                            changes["AuthenticationIpAddress"] = GetNewIpAddress( session.AuthenticationIpAddress );
                        }

                        if ( changes.Any() )
                        {
                            bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( session.Id, changes ) );
                        }
                    }

                    return bulkChanges;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "RemoteAuthenticationSession", changes );
                    }

                    reporter.Add( 1 );
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private string GetNewIpAddress( string ip )
        {
            return _ipAddressMap.GetOrAdd( ip, oldIp =>
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

        private class IdChunk
        {
            public int First { get; set; }

            public int Last { get; set; }
        }

        private class RemoteAuthenticationsession
        {
            public int Id { get; set; }

            public string ClientIpAddress { get; set; }

            public string AuthenticationIpAddress { get; set; }
        }
    }
}
