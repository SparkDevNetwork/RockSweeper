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
    /// Takes the location addresses in the database and shuffles them all around to make it difficult to link a person to an address
    /// </summary>
    [ActionId( "8b164228-50a7-4abe-8769-096ea5157a88" )]
    [Title( "Location Addresses (Shuffle)" )]
    [Description( "Takes the location addresses in the database and shuffles them all around to make it difficult to link a person to an address." )]
    [Category( "Data Scrubbing" )]
    [ConflictsWithAction( typeof( LocationAddressGenerateData ) )]
    public class LocationAddressShuffleData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            List<int> idNumbers;
            CountProgressReporter reporter;
            int stepCount = 3;

            // Find the center location.
            double radiusDistance = 35 * 1609.344;
            var centerLocationGuid = await Sweeper.GetGlobalAttributeValueAsync( "OrganizationAddress" );
            var centerLocationValues = await Sweeper.SqlQueryAsync<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" );
            var centerLocation = centerLocationValues.Any()
                ? new Coordinates( ( await Sweeper.SqlQueryAsync<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" ) ).First() )
                : null;

            //
            // Step 1: Shuffle all locations that are not geo-coded.
            //
            var locations = await Sweeper.SqlQueryAsync( "SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode] FROM [Location] WHERE ISNULL([Street1], '') != '' AND ISNULL([City], '') != '' AND [GeoPoint] IS NULL" );
            idNumbers = locations.Select( l => ( int ) l["Id"] ).ToList();
            reporter = new CountProgressReporter( locations.Count, p => Progress( p, 1, stepCount ) );

            foreach ( var chunk in locations.Chunk( 500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var location in chunk )
                {
                    var locationId = Sweeper.DataFaker.PickRandom( idNumbers );
                    idNumbers.Remove( locationId );

                    location.Remove( "Id" );

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( locationId, location ) );
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "Location", bulkChanges );

                reporter.Add( chunk.Count );
            }

            //
            // Step 2: Shuffle all locations with a valid GeoPoint inside our radius.
            //
            var geoLocations = centerLocation != null
                ? await Sweeper.SqlQueryAsync( $"SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode], [GeoPoint].Lat AS [Lat], [GeoPoint].Long AS [Long] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) < {radiusDistance}" )
                : new List<Dictionary<string, object>>();
            idNumbers = geoLocations.Select( l => ( int ) l["Id"] ).ToList();
            reporter = new CountProgressReporter( geoLocations.Count, p => Progress( p, 2, stepCount ) );

            foreach ( var chunk in geoLocations.Chunk( 500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var location in chunk )
                {
                    var locationId = Sweeper.DataFaker.PickRandom( idNumbers );
                    idNumbers.Remove( locationId );

                    location.Remove( "Id" );
                    location.Add( "GeoPoint", new Coordinates( ( double ) location["Lat"], ( double ) location["Long"] ) );
                    location.Remove( "Lat" );
                    location.Remove( "Long" );

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( locationId, location ) );
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "Location", bulkChanges );

                reporter.Add( chunk.Count );
            }

            //
            // Step 3: Shuffle all locations with a valid GeoPoint outside our radius.
            //
            geoLocations = centerLocation != null
                ? await Sweeper.SqlQueryAsync( $"SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode], [GeoPoint].Lat AS [Lat], [GeoPoint].Long AS [Long] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) >= {radiusDistance}" )
                : new List<Dictionary<string, object>>();
            idNumbers = geoLocations.Select( l => ( int ) l["Id"] ).ToList();
            reporter = new CountProgressReporter( geoLocations.Count, p => Progress( p, 3, stepCount ) );

            foreach ( var chunk in geoLocations.Chunk( 500 ).Select( c => c.ToList() ) )
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var location in chunk )
                {
                    var locationId = Sweeper.DataFaker.PickRandom( idNumbers );
                    idNumbers.Remove( locationId );

                    location.Remove( "Id" );
                    location.Add( "GeoPoint", new Coordinates( ( double ) location["Lat"], ( double ) location["Long"] ) );
                    location.Remove( "Lat" );
                    location.Remove( "Long" );

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( locationId, location ) );
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "Location", bulkChanges );

                reporter.Add( chunk.Count );
            }
        }
    }
}
