using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Clears the background check response data.
    /// </summary>
    [ActionId( "38c0aa94-f914-470a-9be1-ea3b6da14d41" )]
    [Title( "Sanitize Background Check Data" )]
    [Description( "Clears as much sensitive information from background checks as possible." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( GenerateRandomNames ) )]
    public class SanitizeBackgroundCheckData : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            int stepCount = 5;

            //
            // Step 1: Clear background check response data, which can contain sensitive information.
            //
            Sweeper.SqlCommand( "UPDATE [BackgroundCheck] SET [ResponseData] = ''" );
            Progress( 1, 1, stepCount );

            //
            // Step 2: Clear any links to PDFs from Protect My Ministry
            //
            Sweeper.SqlCommand( "UPDATE [AttributeValue] SET [Value] = 'HIDDEN' WHERE [Value] LIKE '%://services.priorityresearch.com%'" );
            Progress( 1, 2, stepCount );

            //
            // Step 3: Clear any background check field types.
            //
            int? backgroundCheckFieldTypeId = Sweeper.GetFieldTypeId( "Rock.Field.Types.BackgroundCheckFieldType" );
            if ( backgroundCheckFieldTypeId.HasValue )
            {
                Sweeper.SqlCommand( $"UPDATE AV SET AV.[Value] = '' FROM [AttributeValue] AS AV INNER JOIN [Attribute] AS A ON A.[Id] = AV.[AttributeId] WHERE A.[FieldTypeId] = {backgroundCheckFieldTypeId.Value}" );
                Progress( 1, 3, stepCount );
            }

            //
            // Step 4: Update name of any background check workflows.
            // This action is run after the action to randomize person names runs, so just update
            // the names to the new person name.
            //
            var backgroundCheckWorkflowTypeIds = Sweeper.SqlQuery<int>( @"
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
                Sweeper.SqlCommand( $@"
UPDATE W
	SET W.[Name] = P.[NickName] + ' ' + P.[LastName]
FROM [Workflow] AS W
INNER JOIN [AttributeValue] AS AVPerson ON AVPerson.[EntityId] = W.[Id]
INNER JOIN [Attribute] AS APerson ON APerson.[Id] = AVPerson.[AttributeId] AND APerson.[Key] = 'Person'
INNER JOIN [PersonAlias] AS PA ON PA.[Guid] = TRY_CAST(AVPerson.[Value] AS uniqueidentifier)
INNER JOIN [Person] AS P ON P.[Id] = PA.[PersonId]
WHERE W.[WorkflowTypeId] = {workflowTypeId}
  AND APerson.[EntityTypeQualifierColumn] = 'WorkflowTypeId'
  AND APerson.[EntityTypeQualifierValue] = W.[WorkflowTypeId]" );
            }
            Progress( 1, 4, stepCount );

            return Task.CompletedTask;
        }
    }
}
