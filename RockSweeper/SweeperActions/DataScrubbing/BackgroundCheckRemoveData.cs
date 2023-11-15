using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Removes all background check data that can be found.
    /// </summary>
    [ActionId( "9083a514-b9bc-418c-b48f-86355399c37a" )]
    [Title( "Background Checks (Remove Data)" )]
    [Description( "Removes all background check data that can be found." )]
    [Category( "Data Scrubbing" )]
    [ConflictsWithAction( typeof( BackgroundCheckData ) )]
    public class BackgroundCheckRemoveData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            int stepCount = 5;
            int step = 1;

            // Step 1: Remove all background check records.
            await Sweeper.SqlCommandAsync( "DELETE FROM [BackgroundCheck]" );
            Progress( 1, step++, stepCount );

            // Step 2: Clear any background check field types.
            int? backgroundCheckFieldTypeId = await Sweeper.GetFieldTypeIdAsync( "Rock.Field.Types.BackgroundCheckFieldType" );
            if ( backgroundCheckFieldTypeId.HasValue )
            {
                await Sweeper.SqlCommandAsync( $@"
UPDATE AV SET
    AV.[Value] = ''
    , AV.[PersistedTextValue] = ''
    , AV.[PersistedHtmlValue] = ''
    , AV.[PersistedCondensedTextValue] = ''
    , AV.[PersistedCondensedHtmlValue] = ''
FROM [AttributeValue] AS AV
INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId]
WHERE A.[FieldTypeId] = {backgroundCheckFieldTypeId.Value}" );
            }
            Progress( 1, step++, stepCount );

            // Step 3: Delete any background check binary files.
            var backgroundCheckFileTypeId = await Sweeper.SqlScalarAsync<int?>( "SELECT TOP 1 [Id] FROM [BinaryFileType] WHERE [Name] = 'Background Check'" );
            if ( backgroundCheckFileTypeId.HasValue )
            {
                await Sweeper.SqlCommandAsync( $@"
DELETE BFD
FROM [BinaryFileData] AS [BFD]
INNER JOIN [BinaryFile] AS [BF] ON [BF].[Id] = [BFD].[Id]
WHERE [BF].[BinaryFileTypeId] = {backgroundCheckFileTypeId.Value}
" );

                await Sweeper.SqlCommandAsync( $@"
DELETE FROM [BinaryFile]
WHERE [BinaryFileTypeId] = {backgroundCheckFileTypeId.Value}
" );
            }
            Progress( 1, step++, stepCount );

            // Step 4: Delete any background check workflows.
            var backgroundCheckWorkflowTypeIds = await Sweeper.SqlQueryAsync<int>( @"
SELECT
    WT.[Id]
FROM [WorkflowType] AS WT
INNER JOIN [Attribute] AS APackageType ON APackageType.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND APackageType.[EntityTypeQualifierValue] = WT.[Id] AND APackageType.[Key] = 'PackageType'
INNER JOIN [Attribute] AS APerson ON APerson.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND APerson.[EntityTypeQualifierValue] = WT.[Id] AND APerson.[Key] = 'Person'
LEFT JOIN [Attribute] AS AReportRecommendation ON AReportRecommendation.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND AReportRecommendation.[EntityTypeQualifierValue] = WT.[Id] AND AReportRecommendation.[Key] = 'ReportRecommendation'
LEFT JOIN [Attribute] AS ASSN ON ASSN.[EntityTypeQualifierColumn] = 'WorkflowTypeId' AND ASSN.[EntityTypeQualifierValue] = WT.[Id] AND ASSN.[Key] = 'SSN'
WHERE AReportRecommendation.[Id] IS NOT NULL OR [ASSN].[Id] IS NOT NULL" );
            foreach ( var workflowTypeId in backgroundCheckWorkflowTypeIds )
            {
                await Sweeper.SqlCommandAsync( $@"
DELETE FROM [Workflow]
WHERE [WorkflowTypeId] IN ({string.Join( ",", backgroundCheckWorkflowTypeIds.Select( id => id.ToString() ) )})
" );
            }
            Progress( 1, step++, stepCount );

            // Step 5: Delete any attribute values for those workflows.
            var workflowAttributeIds = await Sweeper.SqlQueryAsync<int>( $@"
SELECT
    [Id]
FROM [Attribute]
WHERE [EntityTypeQualifierColumn] = 'WorkflowTypeId'
  AND [EntityTypeQualifierValue] IN ({string.Join( ",", backgroundCheckWorkflowTypeIds.Select( id => id.ToString() ) )})
" );

            await Sweeper.SqlCommandAsync( $@"
DELETE FROM [AttributeValue]
WHERE [AttributeId] IN ({string.Join( ",", workflowAttributeIds.Select( id => id.ToString() ) )})
" );
            Progress( 1, step++, stepCount );
        }
    }
}
