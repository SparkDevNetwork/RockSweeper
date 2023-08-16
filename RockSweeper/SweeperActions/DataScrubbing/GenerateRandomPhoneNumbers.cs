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
        public override Task ExecuteAsync()
        {
            var scrubTables = Sweeper.MergeScrubTableDictionaries( Sweeper.ScrubCommonTables, Sweeper.ScrubPhoneTables );
            int stepCount = 4 + scrubTables.Count - 1;

            //
            // Stage 1: Replace all Person phone numbers.
            //
            var phoneNumbers = Sweeper.SqlQuery<int, string>( "SELECT [Id], [Number] FROM [PhoneNumber] WHERE [Number] != ''" );
            Sweeper.ProcessItemsInParallel( phoneNumbers, 1000, ( items ) =>
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
                    Sweeper.UpdateDatabaseRecords( "PhoneNumber", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 1, stepCount );
            } );

            //
            // Stage 2: Replace all AttributeValue phone numbers.
            //
            var fieldTypeIds = new List<int>
            {
                Sweeper.GetFieldTypeId( "Rock.Field.Types.TextFieldType" ).Value,
                Sweeper.GetFieldTypeId( "Rock.Field.Types.PhoneNumberFieldType" ).Value,
                Sweeper.GetFieldTypeId( "Rock.Field.Types.CodeEditorFieldType" ).Value,
                Sweeper.GetFieldTypeId( "Rock.Field.Types.HtmlFieldType" ).Value,
                Sweeper.GetFieldTypeId( "Rock.Field.Types.MarkdownFieldType" ).Value,
                Sweeper.GetFieldTypeId( "Rock.Field.Types.MemoFieldType" ).Value
            };

            var attributeValues = Sweeper.SqlQuery<int, string>( $"SELECT AV.[Id], AV.[Value] FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] IN ({string.Join( ",", fieldTypeIds.Select( i => i.ToString() ) )}) AND AV.[Value] != ''" );
            Sweeper.ProcessItemsInParallel( attributeValues, 1000, ( items ) =>
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
                    Sweeper.UpdateDatabaseRecords( "AttributeValue", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 2, stepCount );
            } );

            //
            // Stage 3: Scrub the global attributes.
            //
            var attributeValue = Sweeper.GetGlobalAttributeValue( "OrganizationPhone" );
            Sweeper.SetGlobalAttributeValue( "OrganizationPhone", Sweeper.ScrubContentForPhoneNumbers( attributeValue ) );
            Progress( 1.0, 3, stepCount );

            //
            // Stage 4: Scan and replace phone numbers in misc data.
            //
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                Sweeper.ScrubTableTextColumns( tc.Key, tc.Value, Sweeper.ScrubContentForPhoneNumbers, p =>
                {
                    Progress( p, 4 + tableStep, stepCount );
                } );

                tableStep++;
            }

            return Task.CompletedTask;
        }
    }
}
