using System;

namespace RockSweeper.Utility
{
    public class Coordinates
    {
        #region Properties

        /// <summary>
        /// Gets the latitude.
        /// </summary>
        /// <value>
        /// The latitude.
        /// </value>
        public double Latitude { get; private set; }

        /// <summary>
        /// Gets the longitude.
        /// </summary>
        /// <value>
        /// The longitude.
        /// </value>
        public double Longitude { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Coordinates"/> class.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        public Coordinates( double latitude, double longitude )
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Coordinates"/> class.
        /// </summary>
        /// <param name="latlong">The latlong.</param>
        public Coordinates( Tuple<double, double> latlong )
            : this( latlong.Item1, latlong.Item2 )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Coordinates"/> class.
        /// </summary>
        /// <param name="latlong">The latlong.</param>
        public Coordinates( string latlong )
            : this( double.Parse( latlong.Split( ',' )[0] ), double.Parse( latlong.Split( ',' )[1] ) )
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a new coordinates object by adjusting the latitude and longitude by the given amount.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude.</param>
        /// <returns></returns>
        public Coordinates CoordinatesByAdjusting( double latitude, double longitude )
        {
            return new Coordinates( Latitude + latitude, Longitude + longitude );
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

        #endregion
    }
}
