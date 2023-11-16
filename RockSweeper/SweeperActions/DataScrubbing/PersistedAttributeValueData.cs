using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Clears all persisted attribute value data and marks them as dirty.
    /// </summary>
    [ActionId( "4f7df3ed-3797-49a5-995c-acd1463458c4" )]
    [Title( "Persisted Attribute Values" )]
    [Description( "Clears all persisted attribute value data and marks them as dirty." )]
    [Category( "Data Scrubbing" )]
    [AfterAction( typeof( BackgroundCheckData ) )]
    [AfterAction( typeof( BackgroundCheckRemoveData ) )]
    [AfterAction( typeof( EmailAddressData ) )]
    [AfterAction( typeof( OrganizationAndCampusData ) )]
    [AfterAction( typeof( PhoneNumberData ) )]
    [AfterAction( typeof( General.ConfigureForLocalhostSmtp ) )]
    [AfterAction( typeof( General.ResetGoogleApiKeys ) )]
    [AfterAction( typeof( General.SanitizeApplicationRoots ) )]
    [AfterAction( typeof( ServiceProviders.AddressVerificationDisable ) )]
    [AfterAction( typeof( ServiceProviders.AIDisable ) )]
    [AfterAction( typeof( ServiceProviders.AssetStorageDisable ) )]
    [AfterAction( typeof( ServiceProviders.AuthenticationDisable ) )]
    [AfterAction( typeof( ServiceProviders.AuthenticationDisableExternal ) )]
    [AfterAction( typeof( ServiceProviders.BackgroundCheckDisable ) )]
    [AfterAction( typeof( ServiceProviders.CommunicationTransportDisable ) )]
    [AfterAction( typeof( ServiceProviders.DigitalSignatureDisable ) )]
    [AfterAction( typeof( ServiceProviders.FileStorageDisable ) )]
    [AfterAction( typeof( ServiceProviders.FinancialGatewayDisable ) )]
    [AfterAction( typeof( ServiceProviders.PhoneSystemDisable ) )]
    [AfterAction( typeof( ServiceProviders.SignatureDocumentDisable ) )]
    public class PersistedAttributeValueData : SweeperAction
    {
        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            await ProcessAttributesAsync( 1, 2 );
            await ProcessAttributeValuesAsync( 2, 2 );
        }

        /// <summary>
        /// Processes the attributes default values.
        /// </summary>
        /// <param name="step">The current step.</param>
        /// <param name="stepCount">The total number of steps.</param>
        private async Task ProcessAttributesAsync( int step, int stepCount )
        {
            var attributeValueIds = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [Attribute] ORDER BY [Id]" );

            if ( Sweeper.RockVersion < new Version( 1, 15, 0 ) )
            {
                // The trigger in Rock v14 causes deadlocks when processing
                // in parallel. So single-thread it on a v14 database.
                var chunks = attributeValueIds.Chunk( 1000 ).ToList();

                for ( int i = 0; i < chunks.Count; i++ )
                {
                    await ProcessAttributesChunkAsync( chunks[i] );

                    Progress( ( i + 1 ) / ( double ) chunks.Count, step, stepCount );
                }

                Progress( 1, step, stepCount );
            }
            else
            {
                await Sweeper.ProcessItemsInParallelAsync( attributeValueIds, 1000, ProcessAttributesChunkAsync, ( p ) =>
                {
                    Progress( p, step, stepCount );
                } );
            }
        }

        /// <summary>
        /// Process a single chunk of Attributes.
        /// </summary>
        /// <param name="chunk">The chunk identifiers.</param>
        private async Task ProcessAttributesChunkAsync( IEnumerable<int> chunk )
        {
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var id in chunk )
            {
                var changes = new Dictionary<string, object>
                    {
                        { "DefaultPersistedTextValue", string.Empty },
                        { "DefaultPersistedHtmlValue", string.Empty },
                        { "DefaultPersistedCondensedTextValue", string.Empty },
                        { "DefaultPersistedCondensedHtmlValue", string.Empty },
                        { "IsDefaultPersistedValueDirty", true }
                    };

                bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
            }

            if ( bulkChanges.Count() > 0 )
            {
                await Sweeper.UpdateDatabaseRecordsAsync( "Attribute", bulkChanges );
            }
        }

        /// <summary>
        /// Processes the attribute values.
        /// </summary>
        /// <param name="step">The current step.</param>
        /// <param name="stepCount">The total number of steps.</param>
        private async Task ProcessAttributeValuesAsync( int step, int stepCount )
        {
            var attributeValueIds = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [AttributeValue] ORDER BY [Id]" );

            if ( Sweeper.RockVersion < new Version( 1, 15, 0 ) )
            {
                // The trigger in Rock v14 causes deadlocks when processing
                // in parallel. So single-thread it on a v14 database.
                var chunks = attributeValueIds.Chunk( 1000 ).ToList();

                for ( int i = 0; i < chunks.Count; i++ )
                {
                    await ProcessAttributeValuesChunkAsync( chunks[i] );

                    Progress( ( i + 1 ) / ( double ) chunks.Count, step, stepCount );
                }

                Progress( 1, step, stepCount );
            }
            else
            {
                await Sweeper.ProcessItemsInParallelAsync( attributeValueIds, 1000, ProcessAttributeValuesChunkAsync, ( p ) =>
                {
                    Progress( p, step, stepCount );
                } );
            }
        }

        /// <summary>
        /// Process a single chunk of AttributeValues.
        /// </summary>
        /// <param name="chunk">The chunk identifiers.</param>
        private async Task ProcessAttributeValuesChunkAsync( IEnumerable<int> chunk )
        {
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var id in chunk )
            {
                var changes = new Dictionary<string, object>
                    {
                        { "PersistedTextValue", string.Empty },
                        { "PersistedHtmlValue", string.Empty },
                        { "PersistedCondensedTextValue", string.Empty },
                        { "PersistedCondensedHtmlValue", string.Empty },
                        { "IsPersistedValueDirty", true }
                    };

                bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
            }

            if ( bulkChanges.Count() > 0 )
            {
                await Sweeper.UpdateDatabaseRecordsAsync( "AttributeValue", bulkChanges );
            }
        }
    }
}
