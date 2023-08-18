using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Inserts the history placeholders.
    /// </summary>
    [ActionId( "5cdcf31e-382f-4663-ad26-122814990187" )]
    [Title( "Insert History Placeholders" )]
    [Description( "Modifies all History records to remove any identifying information." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( GenerateRandomLogins ) )]
    public class InsertHistoryPlaceholders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var fieldValueRegex = new Regex( "(<span class=['\"]field-value['\"]>)([^<]*)(<\\/span>)" );
            var loginFieldValueRegex = new Regex( "(.*logged in.*<span class=['\"]field-name['\"]>)([^<]*)(<\\/span>)" );

            var historyIds = await Sweeper.SqlQueryAsync<int>( $"SELECT [Id] FROM [History] ORDER BY [Id]" );

            async Task ProcessChunk( List<int> items )
            {
                var historyItems = await Sweeper.SqlQueryAsync( $"SELECT * FROM [History] WHERE [Id] IN ({string.Join( ",", items )})" );
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var history in historyItems )
                {
                    var changes = new Dictionary<string, object>();

                    //
                    // Scrub the Caption.
                    //
                    var caption = ( string ) history["Caption"];
                    if ( !string.IsNullOrWhiteSpace( caption ) )
                    {
                        var value = Sweeper.DataFaker.Lorem.Sentence( caption.Split( ' ' ).Length ).Left( 200 );

                        changes.Add( "Caption", value );
                    }

                    //
                    // Scrub the RelatedData to remove any mentions of the original values.
                    //
                    var relatedData = ( string ) history["RelatedData"];
                    if ( !string.IsNullOrWhiteSpace( relatedData ) )
                    {
                        var newValue = fieldValueRegex.Replace( relatedData, ( m ) =>
                        {
                            return $"{m.Groups[1].Value}HIDDEN{m.Groups[3].Value}";
                        } );

                        if ( newValue != relatedData )
                        {
                            changes.Add( "RelatedData", newValue );
                        }
                    }

                    //
                    // Scrub the OldValue.
                    //
                    if ( history.ContainsKey( "OldValue" ) && !string.IsNullOrWhiteSpace( ( string ) history["OldValue"] ) )
                    {
                        changes.Add( "OldValue", "HIDDEN" );
                    }

                    //
                    // Scrub the NewValue.
                    //
                    if ( history.ContainsKey( "NewValue" ) && !string.IsNullOrWhiteSpace( ( string ) history["NewValue"] ) )
                    {
                        changes.Add( "NewValue", "HIDDEN" );
                    }

                    // Scrub the OldValueRaw
                    if ( history.ContainsKey( "OldValueRaw" ) && !string.IsNullOrWhiteSpace( ( string ) history["OldValueRaw"] ) )
                    {
                        changes.Add( "OldValueRaw", "HIDDEN" );
                    }

                    // Scrub the NewValueRaw
                    if ( history.ContainsKey( "NewValueRaw" ) && !string.IsNullOrWhiteSpace( ( string ) history["NewValueRaw"] ) )
                    {
                        changes.Add( "NewValueRaw", "HIDDEN" );
                    }

                    //
                    // Scrub the ValueName.
                    //
                    var verb = ( string ) history["Verb"];
                    if ( verb == "ADDEDTOGROUP" || verb == "REMOVEDROMGROUP" || verb == "REGISTERED" || verb == "MERGE" )
                    {
                        changes.Add( "ValueName", "HIDDEN" );
                    }
                    else if ( verb == "LOGIN" && history.ContainsKey( "ValueName" ) )
                    {
                        var valueName = ( string ) history["ValueName"];
                        if ( !string.IsNullOrWhiteSpace( valueName ) && !valueName.StartsWith( "fakeuser" ) )
                        {
                            changes.Add( "ValueName", "HIDDEN" );
                        }
                    }

                    if ( changes.Any() )
                    {
                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( ( int ) history["Id"], changes ) );
                    }
                }

                if ( bulkChanges.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordsAsync( "History", bulkChanges );
                }
            }

            await Sweeper.ProcessItemsInParallelAsync( historyIds, 1000, ProcessChunk, ( p ) =>
            {
                Progress( p );
            } );
        }
    }
}
