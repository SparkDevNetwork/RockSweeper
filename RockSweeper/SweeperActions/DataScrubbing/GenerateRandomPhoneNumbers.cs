using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Generates the random phone numbers.
    /// </summary>
    [ActionId( "46f34fe1-4577-425d-9cdf-cf54da349365" )]
    [Title( "Generate Random Phone Numbers" )]
    [Description( "Replaces any phone numbers found in the system with generated values." )]
    [Category( "Data Scrubbing" )]
    public class GenerateRandomPhoneNumbers : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var scrubTables = Sweeper.MergeScrubTableDictionaries( Sweeper.ScrubCommonTables, Sweeper.ScrubPhoneTables );
            int stepCount = 5 + scrubTables.Count - 1;

            // Stage 1: Replace all System phone numbers.
            List<Tuple<int, string>> systemPhoneNumbers;
            try
            {
                systemPhoneNumbers = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id], [Number] FROM [SystemPhoneNumber] ORDER BY [Id]" );
            }
            catch
            {
                // Older version of Rock.
                systemPhoneNumbers = new List<Tuple<int, string>>();
            }

            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in systemPhoneNumbers )
                {
                    var changes = new Dictionary<string, object>();
                    var phoneNumber = Sweeper.GenerateFakePhoneNumberForPhone( item.Item2 );
                    string numberFormatted;

                    if ( phoneNumber.Length == 10 )
                    {
                        numberFormatted = $"+1{phoneNumber}";
                    }
                    else
                    {
                        numberFormatted = phoneNumber;
                    }

                    changes.Add( "Number", phoneNumber );

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( item.Item1, changes ) );
                }

                if ( bulkChanges.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordsAsync( "SystemPhoneNumber", bulkChanges );
                }
            }
            Progress( 1, 1, stepCount );


            //
            // Stage 2: Replace all Person phone numbers.
            //
            var phoneNumbers = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id], [Number] FROM [PhoneNumber] WHERE [Number] != '' ORDER BY [Id]" );
            await Sweeper.ProcessItemsInParallelAsync( phoneNumbers, 1000, async ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in items )
                {
                    var changes = new Dictionary<string, object>();
                    var phoneNumber = Sweeper.GenerateFakePhoneNumberForPhone( item.Item2 );
                    string numberFormatted;

                    if ( phoneNumber.Length == 10 )
                    {
                        numberFormatted = $"({phoneNumber.Substring( 0, 3 )}) {phoneNumber.Substring( 3, 4 )}-{phoneNumber.Substring( 7 )}";
                    }
                    else if ( phoneNumber.Length == 7 )
                    {
                        numberFormatted = phoneNumber.Substring( 0, 3 ) + "-" + phoneNumber.Substring( 3 );
                    }
                    else
                    {
                        numberFormatted = phoneNumber;
                    }

                    changes.Add( "Number", phoneNumber );
                    changes.Add( "NumberFormatted", numberFormatted );

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( item.Item1, changes ) );
                }

                if ( bulkChanges.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordsAsync( "PhoneNumber", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 2, stepCount );
            } );

            //
            // Stage 3: Replace all AttributeValue phone numbers.
            //
            var fieldTypeIds = new List<int>
            {
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.TextFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.PhoneNumberFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.CodeEditorFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.HtmlFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.MarkdownFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.MemoFieldType" ) ).Value
            };

            var attributeValues = await Sweeper.SqlQueryAsync<int, string>( $"SELECT AV.[Id], AV.[Value] FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] IN ({string.Join( ",", fieldTypeIds.Select( i => i.ToString() ) )}) AND AV.[Value] != '' ORDER BY [AV].[Id]" );
            await Sweeper.ProcessItemsInParallelAsync( attributeValues, 1000, async ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var item in items )
                {
                    var newValue = Sweeper.ScrubContentForPhoneNumbers( item.Item2 );

                    if ( newValue != item.Item2 )
                    {
                        var changes = new Dictionary<string, object>
                            {
                            { "Value", newValue }
                            };

                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( item.Item1, changes ) );
                    }
                }

                if ( bulkChanges.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordsAsync( "AttributeValue", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 3, stepCount );
            } );

            //
            // Stage 4: Scrub the global attributes.
            //
            var attributeValue = await Sweeper.GetGlobalAttributeValueAsync( "OrganizationPhone" );
            await Sweeper.SetGlobalAttributeValue( "OrganizationPhone", Sweeper.ScrubContentForPhoneNumbers( attributeValue ) );
            Progress( 1.0, 4, stepCount );

            //
            // Stage 5: Scan and replace phone numbers in misc data.
            //
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                await Sweeper.ScrubTableTextColumnsAsync( tc.Key, tc.Value, Sweeper.ScrubContentForPhoneNumbers, p =>
                {
                    Progress( p, 5 + tableStep, stepCount );
                } );

                tableStep++;
            }
        }
    }
}
