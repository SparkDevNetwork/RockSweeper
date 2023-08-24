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
            int stepCount = 3;

            //
            // Step 1: Shuffle all locations that are not geo-coded.
            //
            //var locations = await Sweeper.SqlQueryAsync( "SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode] FROM [Location] WHERE ISNULL([Street1], '') != '' AND ISNULL([City], '') != '' AND [GeoPoint] IS NULL" );
            //idNumbers = locations.Select( l => ( int ) l["Id"] ).ToList();
            //for ( int i = 0; i < locations.Count; i++ )
            //{
            //    var locationId = Sweeper.DataFaker.PickRandom( idNumbers );
            //    idNumbers.Remove( locationId );

            //    locations[i].Remove( "Id" );

            //    await Sweeper.UpdateDatabaseRecordAsync( "Location", locationId, locations[i] );

            //    Progress( i / ( double ) locations.Count, 1, stepCount );
            //}

            //
            // Step 2: Shuffle all locations with a valid GeoPoint inside our radius.
            //
            double radiusDistance = 35 * 1609.344;
            var centerLocationGuid = await Sweeper.GetGlobalAttributeValueAsync( "OrganizationAddress" );
            var centerLocationValues = await Sweeper.SqlQueryAsync<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" );
            var centerLocation = centerLocationValues.Any()
                ? new Coordinates( ( await Sweeper.SqlQueryAsync<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" ) ).First() )
                : null;

            var geoLocations = centerLocation != null
                ? await Sweeper.SqlQueryAsync( $"SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode], [GeoPoint].Lat AS [Lat], [GeoPoint].Long AS [Long] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) < {radiusDistance}" )
                : new List<Dictionary<string, object>>();

            idNumbers = geoLocations.Select( l => ( int ) l["Id"] ).ToList();
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = Sweeper.DataFaker.PickRandom( idNumbers );
                idNumbers.Remove( locationId );

                geoLocations[i].Remove( "Id" );
                geoLocations[i].Add( "GeoPoint", new Coordinates( ( double ) geoLocations[i]["Lat"], ( double ) geoLocations[i]["Long"] ) );
                geoLocations[i].Remove( "Lat" );
                geoLocations[i].Remove( "Long" );

                await Sweeper.UpdateDatabaseRecordAsync( "Location", locationId, geoLocations[i] );

                Progress( i / ( double ) geoLocations.Count, 2, stepCount );
            }

            //
            // Step 3: Shuffle all locations with a valid GeoPoint outside our radius.
            //
            geoLocations = centerLocation != null
                ? await Sweeper.SqlQueryAsync( $"SELECT [Id], [Street1], [Street2], [City], [State], [Country], [PostalCode], [GeoPoint].Lat AS [Lat], [GeoPoint].Long AS [Long] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) >= {radiusDistance}" )
                : new List<Dictionary<string, object>>();

            idNumbers = geoLocations.Select( l => ( int ) l["Id"] ).ToList();
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = Sweeper.DataFaker.PickRandom( idNumbers );
                idNumbers.Remove( locationId );

                geoLocations[i].Remove( "Id" );
                geoLocations[i].Add( "GeoPoint", new Coordinates( ( double ) geoLocations[i]["Lat"], ( double ) geoLocations[i]["Long"] ) );
                geoLocations[i].Remove( "Lat" );
                geoLocations[i].Remove( "Long" );

                await Sweeper.UpdateDatabaseRecordAsync( "Location", locationId, geoLocations[i] );

                Progress( i / ( double ) geoLocations.Count, 3, stepCount );
            }
        }
    }
}
