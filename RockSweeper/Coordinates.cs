using System;

namespace RockSweeper
{
    /// <summary>
    /// https://stackoverflow.com/questions/6366408/calculating-distance-between-two-latitude-and-longitude-geocoordinates
    /// </summary>
    public class Coordinates
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public Coordinates( double latitude, double longitude )
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public Coordinates( Tuple<double, double> latlong )
            : this( latlong.Item1, latlong.Item2 )
        {
        }

        public Coordinates( string latlong )
            : this( double.Parse( latlong.Split( ',' )[0] ), double.Parse( latlong.Split( ',' )[1] ) )
        {
        }

        public Coordinates CoordinatesByAdjusting( double latitude, double longitude )
        {
            return new Coordinates( Latitude + latitude, Longitude + longitude );
        }

        public double DistanceTo( Coordinates targetCoordinates )
        {
            return DistanceTo( targetCoordinates, UnitOfLength.Kilometers );
        }

        public double DistanceTo( Coordinates targetCoordinates, UnitOfLength unitOfLength )
        {
            var baseRad = Math.PI * this.Latitude / 180;
            var targetRad = Math.PI * targetCoordinates.Latitude / 180;
            var theta = this.Longitude - targetCoordinates.Longitude;
            var thetaRad = Math.PI * theta / 180;

            double dist =
                Math.Sin( baseRad ) * Math.Sin( targetRad ) + Math.Cos( baseRad ) *
                Math.Cos( targetRad ) * Math.Cos( thetaRad );
            dist = Math.Acos( dist );

            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;

            return unitOfLength.ConvertFromMiles( dist );
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{ Latitude },{ Longitude }";
        }
    }

    public class UnitOfLength
    {
        public static UnitOfLength Kilometers = new UnitOfLength( 1.609344 );
        public static UnitOfLength NauticalMiles = new UnitOfLength( 0.8684 );
        public static UnitOfLength Miles = new UnitOfLength( 1 );

        private readonly double _fromMilesFactor;

        private UnitOfLength( double fromMilesFactor )
        {
            _fromMilesFactor = fromMilesFactor;
        }

        public double ConvertFromMiles( double input )
        {
            return input * _fromMilesFactor;
        }
    }
}
