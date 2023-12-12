using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Scrubs out any government IDs as well as request and result text.
    /// </summary>
    [ActionId( "944fea3a-7826-4e9b-9539-eae3f26fa2ac" )]
    [Title( "Benevolence Requests" )]
    [Description( "Scrubs out any government IDs as well as request and result text." )]
    [Category( "Data Scrubbing" )]
    public class BenevolenceRequestData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await CleanRequestDataAsync( 1, 2 );
            await CleanResultDataAsync( 2, 2 );
        }

        private async Task CleanRequestDataAsync(int step, int stepCount)
        {
            var queryData = await Sweeper.SqlQueryAsync<int, string, string, string>( "SELECT [Id],[GovernmentId],[RequestText],[ResultSummary] FROM [BenevolenceRequest]" );
            var wordRegex = new Regex( "([a-zA-Z]+)" );

            for ( int i = 0; i < queryData.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item2 ) )
                {
                    changes.Add( "GovernmentId", Sweeper.DataFaker.Random.ReplaceNumbers( "GEN######" ) );
                }

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item3 ) )
                {
                    var value = wordRegex.Replace( queryData[i].Item3, ( m ) =>
                    {
                        return Sweeper.DataFaker.Lorem.Word();
                    } );

                    changes.Add( "RequestText", value );
                }

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item4 ) )
                {
                    var value = wordRegex.Replace( queryData[i].Item4, ( m ) =>
                    {
                        return Sweeper.DataFaker.Lorem.Word();
                    } );

                    changes.Add( "ResultSummary", value );
                }

                if ( changes.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordAsync( "BenevolenceRequest", queryData[i].Item1, changes );
                }

                Progress( i / ( double ) queryData.Count, step, stepCount );
            }
        }

        private async Task CleanResultDataAsync( int step, int stepCount )
        {
            var queryData = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id],[ResultSummary] FROM [BenevolenceResult]" );

            for ( int i = 0; i < queryData.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( queryData[i].Item2 ) )
                {
                    changes.Add( "ResultSummary", Sweeper.DataFaker.Lorem.ReplaceWords( queryData[i].Item2 ) );
                }

                if ( changes.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordAsync( "BenevolenceResult", queryData[i].Item1, changes );
                }

                Progress( i / ( double ) queryData.Count, step, stepCount );
            }
        }
    }
}
