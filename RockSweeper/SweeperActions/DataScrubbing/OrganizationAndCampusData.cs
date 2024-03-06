using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Generates the organization and campuses.
    /// </summary>
    [ActionId( "7a047807-1a42-413e-899a-b2a1fc90a889" )]
    [Title( "Organization and Campuses" )]
    [Description( "Scrubs the organization name and URL as well as campus names and URLs." )]
    [Category( "Data Scrubbing" )]
    public class OrganizationAndCampusData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var organizationCity = Sweeper.DataFaker.PickRandom( Sweeper.LocationCityPostalCodes.Keys.ToList() );
            var usedCampusNames = new List<string>();

            await Sweeper.SetGlobalAttributeValue( "OrganizationName", $"{organizationCity} Community Church" );
            await Sweeper.SetGlobalAttributeValue( "OrganizationAbbreviation", $"{organizationCity} Community Church" );
            await Sweeper.SetGlobalAttributeValue( "OrganizationWebsite", $"http://www.{organizationCity.Replace( " ", "" ).ToLower()}communitychurch.org/" );

            var campuses = await Sweeper.SqlQueryAsync<int, string, string, string>( "SELECT [Id], [Url], [Description], [ShortCode] FROM [Campus]" );
            foreach ( var campus in campuses )
            {
                var campusCityName = Sweeper.DataFaker.PickRandom( Sweeper.LocationCityPostalCodes.Keys.ToList() );
                int nameAttempt = 0;

                while ( usedCampusNames.Contains( campusCityName ) && nameAttempt < 100 )
                {
                    campusCityName = Sweeper.DataFaker.PickRandom( Sweeper.LocationCityPostalCodes.Keys.ToList() );
                    nameAttempt++;
                }

                usedCampusNames.Add( campusCityName );

                var changes = new Dictionary<string, object>
                {
                    { "Name", campusCityName }
                };

                if ( !string.IsNullOrWhiteSpace( campus.Item2 ) )
                {
                    changes.Add( "Url", $"http://{changes["Name"].ToString().Replace( " ", "" ).ToLower()}.{organizationCity.Replace( " ", "" ).ToLower()}communitychurch.org/" );
                }

                if ( !string.IsNullOrWhiteSpace( campus.Item3 ) )
                {
                    changes.Add( "Description", Sweeper.DataFaker.Lorem.Sentence() );
                }

                if ( !string.IsNullOrWhiteSpace( campus.Item4 ) )
                {
                    changes.Add( "ShortCode", campusCityName.Substring( 0, 3 ).ToUpper() );
                }

                await Sweeper.UpdateDatabaseRecordAsync( "Campus", campus.Item1, changes );
            }
        }
    }
}
