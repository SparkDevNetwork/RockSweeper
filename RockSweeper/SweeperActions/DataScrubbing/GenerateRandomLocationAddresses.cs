﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Generates random location addresses.
    /// </summary>
    [ActionId( "5b110e1e-dde1-49e2-922e-c604083e8679" )]
    [Title( "Generate Random Location Addresses" )]
    [Description( "Generates random addresses in the database, centered around Phoenix, AZ." )]
    [Category( "Data Scrubbing" )]
    [RequiresLocationService]
    public class GenerateRandomLocationAddresses : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            int stepCount = 3;

            //
            // Step 1: Process all locations that are not geo-coded.
            //
            var locations = Sweeper.SqlQuery( "SELECT [Id], [Street1], [Street2], [County], [PostalCode], [State], [Country] FROM [Location] WHERE ISNULL([Street1], '') != '' AND ISNULL([City], '') != '' AND [GeoPoint] IS NULL" );
            for ( int i = 0; i < locations.Count; i++ )
            {
                var locationId = ( int ) locations[i]["Id"];
                var street1 = ( string ) locations[i]["Street1"];
                var street2 = ( string ) locations[i]["Street2"];
                var county = ( string ) locations[i]["County"];
                var postalCode = ( string ) locations[i]["PostalCode"];
                var state = ( string ) locations[i]["State"];
                var country = ( string ) locations[i]["Country"];

                Sweeper.UpdateLocationWithFakeData( locationId, street1, street2, county, postalCode, state, country );

                Progress( i / ( double ) locations.Count, 1, stepCount );
            }

            double radiusDistance = 35 * 1609.344;
            var centerLocationGuid = Sweeper.GetGlobalAttributeValue( "OrganizationAddress" );
            var centerLocation = new Coordinates( Sweeper.SqlQuery<double, double>( $"SELECT [GeoPoint].Lat, [GeoPoint].Long FROM [Location] WHERE [Guid] = '{centerLocationGuid}'" ).First() );
            var targetCenterLocation = new Coordinates( Properties.Settings.Default.TargetGeoCenter );
            var adjustCoordinates = new Coordinates( targetCenterLocation.Latitude - centerLocation.Latitude, targetCenterLocation.Longitude - centerLocation.Longitude );

            //
            // Step 2: Move all locations with a valid GeoPoint inside our radius.
            //
            var geoLocations = Sweeper.SqlQuery( $"SELECT [Id], [GeoPoint].Lat AS [Latitude], [GeoPoint].Long AS [Longitude], [Street1], [Street2], [City], [County], [PostalCode], [State], [Country] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) < {radiusDistance}" );
            var step2Changes = new Dictionary<string, object>();
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = ( int ) geoLocations[i]["Id"];
                var latitude = ( double ) geoLocations[i]["Latitude"];
                var longitude = ( double ) geoLocations[i]["Longitude"];
                var street1 = ( string ) geoLocations[i]["Street1"];
                var street2 = ( string ) geoLocations[i]["Street2"];
                var city = ( string ) geoLocations[i]["City"];
                var county = ( string ) geoLocations[i]["County"];
                var postalCode = ( string ) geoLocations[i]["PostalCode"];
                var state = ( string ) geoLocations[i]["State"];
                var country = ( string ) geoLocations[i]["Country"];

                Sweeper.CancellationToken.ThrowIfCancellationRequested();
                step2Changes.Clear();

                var coordinates = new Coordinates( latitude, longitude ).CoordinatesByAdjusting( adjustCoordinates.Latitude, adjustCoordinates.Longitude );

                if ( Properties.Settings.Default.JitterAddresses )
                {
                    //
                    // Jitter the coordinates by +/- one mile.
                    //
                    coordinates = coordinates.CoordinatesByAdjusting( Sweeper.DataFaker.Random.Double( -0.0144927, 0.0144927 ), Sweeper.DataFaker.Random.Double( -0.0144927, 0.0144927 ) );
                }

                var address = Sweeper.GetBestAddressForCoordinates( coordinates );

                step2Changes.Add( "GeoPoint", coordinates );

                if ( !string.IsNullOrWhiteSpace( street1 ) )
                {
                    step2Changes.Add( "Street1", address.Street1 );
                }

                if ( !string.IsNullOrWhiteSpace( city ) )
                {
                    step2Changes.Add( "City", address.City );
                }

                if ( !string.IsNullOrWhiteSpace( county ) )
                {
                    step2Changes.Add( "County", address.Country );
                }

                if ( !string.IsNullOrWhiteSpace( postalCode ) )
                {
                    step2Changes.Add( "PostalCode", address.PostalCode );
                }

                if ( !string.IsNullOrWhiteSpace( state ) )
                {
                    step2Changes.Add( "State", address.State );
                }

                if ( !string.IsNullOrWhiteSpace( country ) )
                {
                    step2Changes.Add( "Country", address.Country );
                }

                Sweeper.UpdateDatabaseRecord( "Location", locationId, step2Changes );

                Progress( i / ( double ) geoLocations.Count, 2, stepCount );
            }

            //
            // Step 3: Add a 1-mile jitter to any address outside our radius.
            //
            geoLocations = Sweeper.SqlQuery( $"SELECT [Id], [GeoPoint].Lat AS [Latitude], [GeoPoint].Long AS [Longitude], [Street1], [Street2], [City], [County], [PostalCode], [State], [Country] FROM [Location] WHERE [GeoPoint] IS NOT NULL AND geography::Point({centerLocation.Latitude}, {centerLocation.Longitude}, 4326).STDistance([GeoPoint]) >= {radiusDistance}" );
            for ( int i = 0; i < geoLocations.Count; i++ )
            {
                var locationId = ( int ) geoLocations[i]["Id"];
                var latitude = ( double ) geoLocations[i]["Latitude"];
                var longitude = ( double ) geoLocations[i]["Longitude"];
                var street1 = ( string ) geoLocations[i]["Street1"];
                var street2 = ( string ) geoLocations[i]["Street2"];
                var city = ( string ) geoLocations[i]["City"];
                var county = ( string ) geoLocations[i]["County"];
                var postalCode = ( string ) geoLocations[i]["PostalCode"];
                var state = ( string ) geoLocations[i]["State"];
                var country = ( string ) geoLocations[i]["Country"];

                if ( Properties.Settings.Default.JitterAddresses )
                {
                    var changes = new Dictionary<string, object>();

                    var coordinates = new Coordinates( latitude, longitude );
                    coordinates = coordinates.CoordinatesByAdjusting( Sweeper.DataFaker.Random.Double( -0.0144927, 0.0144927 ), Sweeper.DataFaker.Random.Double( -0.0144927, 0.0144927 ) );

                    var address = Sweeper.GetBestAddressForCoordinates( coordinates );

                    changes.Add( "GeoPoint", coordinates );

                    if ( !string.IsNullOrWhiteSpace( street1 ) )
                    {
                        changes.Add( "Street1", address.Street1 );
                    }

                    if ( !string.IsNullOrWhiteSpace( city ) )
                    {
                        changes.Add( "City", address.City );
                    }

                    if ( !string.IsNullOrWhiteSpace( county ) )
                    {
                        changes.Add( "County", address.Country );
                    }

                    if ( !string.IsNullOrWhiteSpace( postalCode ) )
                    {
                        changes.Add( "PostalCode", address.PostalCode );
                    }

                    if ( !string.IsNullOrWhiteSpace( state ) )
                    {
                        changes.Add( "State", address.State );
                    }

                    if ( !string.IsNullOrWhiteSpace( country ) )
                    {
                        changes.Add( "Country", address.Country );
                    }

                    Sweeper.UpdateDatabaseRecord( "Location", locationId, changes );
                }
                else
                {
                    Sweeper.UpdateLocationWithFakeData( locationId, street1, street2, county, postalCode, state, country );
                }

                Progress( i / ( double ) geoLocations.Count, 3, stepCount );
            }

            return Task.CompletedTask;
        }
    }
}