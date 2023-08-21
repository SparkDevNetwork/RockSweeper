using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Generates the random email addresses.
    /// </summary>
    [ActionId( "7d14924a-b367-4495-b480-96b86bcf712b" )]
    [Title( "Generate Random Email Addresses" )]
    [Description( "Replaces any e-mail addresses found in the system with generated values." )]
    [Category( "Data Scrubbing" )]
    public class GenerateRandomEmailAddresses : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var scrubTables = Sweeper.MergeScrubTableDictionaries( Sweeper.ScrubCommonTables, Sweeper.ScrubEmailTables );
            int stepCount = 4 + scrubTables.Count - 1;

            //
            // Stage 1: Replace all Person e-mail addresses.
            //
            var peopleAddresses = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id], [Email] FROM [Person] WHERE [Email] IS NOT NULL AND [Email] != '' ORDER BY [Id]" );
            await Sweeper.ProcessItemsInParallelAsync( peopleAddresses, 1000, async ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var p in items )
                {
                    var changes = new Dictionary<string, object>
                    {
                        { "Email", Sweeper.GenerateFakeEmailAddressForAddress( p.Item2 ) }
                    };

                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( p.Item1, changes ) );
                }

                if ( bulkChanges.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordsAsync( "Person", bulkChanges );
                }
            }, ( p ) =>
            {
                Progress( p, 1, stepCount );
            } );

            //
            // Stage 2: Replace all AttributeValue e-mail addresses.
            //
            var fieldTypeIds = new List<int>
            {
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.TextFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.EmailFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.CodeEditorFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.HtmlFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.MarkdownFieldType" ) ).Value,
                ( await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.MemoFieldType" ) ).Value
            };

            var attributeValues = await Sweeper.SqlQueryAsync<int, string>( $"SELECT AV.[Id], AV.[Value] FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] IN ({string.Join( ",", fieldTypeIds.Select( i => i.ToString() ) )}) AND AV.[Value] LIKE '%@%' ORDER BY [AV].[Id]" );
            await Sweeper.ProcessItemsInParallelAsync( attributeValues, 1000, async ( items ) =>
            {
                var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

                foreach ( var av in items )
                {
                    var newValue = Sweeper.ScrubContentForEmailAddresses( av.Item2 );

                    if ( newValue != av.Item2 )
                    {
                        var changes = new Dictionary<string, object>
                            {
                            { "Value", newValue }
                            };

                        bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( av.Item1, changes ) );
                    }
                }

                await Sweeper.UpdateDatabaseRecordsAsync( "AttributeValue", bulkChanges );
            }, ( p ) =>
            {
                Progress( p, 2, stepCount );
            } );

            //
            // Stage 3: Scrub the global attributes.
            //
            var attributeValue = await Sweeper.GetGlobalAttributeValueAsync( "EmailExceptionsList" );
            await Sweeper.SetGlobalAttributeValue( "EmailExceptionsList", Sweeper.ScrubContentForEmailAddresses( attributeValue ) );
            attributeValue = await Sweeper.GetGlobalAttributeValueAsync( "OrganizationEmail" );
            await Sweeper.SetGlobalAttributeValue( "OrganizationEmail", Sweeper.ScrubContentForEmailAddresses( attributeValue ) );
            Progress( 1.0, 3, stepCount );

            //
            // Stage 4: Scan and replace e-mail addresses in misc data.
            //
            int tableStep = 0;
            foreach ( var tc in scrubTables )
            {
                await Sweeper.ScrubTableTextColumnsAsync( tc.Key, tc.Value, Sweeper.ScrubContentForEmailAddresses, p =>
                {
                    Progress( p, 4 + tableStep, stepCount );
                } );

                tableStep++;
            }
        }
    }
}
