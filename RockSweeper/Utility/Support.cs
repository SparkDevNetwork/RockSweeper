using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Provides various helper methods used by the RockDevBooster application.
    /// </summary>
    static class Support
    {
        #region Data Path Methods

        /// <summary>
        /// Get the filesystem path to the RockDevBooster data folder.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
        static public string GetDataPath()
        {
            string appDataPath = Environment.GetEnvironmentVariable( "LocalAppData" );
            string dataPath = Path.Combine( appDataPath, "RockSweeper" );

            if ( !Directory.Exists( dataPath ) )
            {
                Directory.CreateDirectory( dataPath );
            }

            return dataPath;
        }

        /// <summary>
        /// Get the path to the temporary build directory.
        /// </summary>
        /// <returns>A string representing a location on the filesystem.</returns>
        static public string GetGeocodeCachePath()
        {
            return Path.Combine( GetDataPath(), "geocache.json" );
        }

        #endregion

        #region Geocode Cache Methods

        /// <summary>
        /// Loads the geocode cache.
        /// </summary>
        /// <returns></returns>
        static public Dictionary<string, Address> LoadGeocodeCache()
        {
            if ( File.Exists( GetGeocodeCachePath() ) )
            {
                return JsonConvert.DeserializeObject<Dictionary<string, Address>>( File.ReadAllText( GetGeocodeCachePath() ) );
            }

            return new Dictionary<string, Address>();
        }

        /// <summary>
        /// Saves the geocode cache.
        /// </summary>
        /// <param name="cache">The cache.</param>
        static public void SaveGeocodeCache( Dictionary<string, Address> cache )
        {
            File.WriteAllText( GetGeocodeCachePath(), JsonConvert.SerializeObject( cache ) );
        }

        #endregion
    }
}
