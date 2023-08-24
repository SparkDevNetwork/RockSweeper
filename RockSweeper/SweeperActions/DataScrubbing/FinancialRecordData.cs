using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RockSweeper.Attributes;
using RockSweeper.Utility;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Removes all sensitive and identifying information from financial related records.
    /// </summary>
    [ActionId( "73a6169f-6552-4dfd-b759-774573280cad" )]
    [Title( "Financial Records" )]
    [Description( "Removes all sensitive and identifying information from financial related records." )]
    [Category( "Data Scrubbing" )]
    public class FinancialRecordData : SweeperAction
    {
        #region Constants

        private const string StatementReportTemplate = @"{% assign publicApplicationRoot = 'Global' | Attribute:'PublicApplicationRoot' %}
{% assign organizationName = 'Global' | Attribute:'OrganizationName' %}
{% assign organizationAddress = 'Global' | Attribute:'OrganizationAddress' %}
{% assign organizationWebsite = 'Global' | Attribute:'OrganizationWebsite' %}
{% assign organizationEmail = 'Global' | Attribute:'OrganizationEmail' %}
{% assign organizationPhone = 'Global' | Attribute:'OrganizationPhone' %}
{% assign currencySymbol = 'Global' | Attribute:'CurrencySymbol' %}
{% assign bodyPadding = '0' %}
{% assign fontSizeBase = '9px' %}
{% if RenderMedium == 'Html' %}
  {% assign bodyPadding = '0' %}
  {% assign fontSizeBase = '12px' %}
{% endif %}
<!DOCTYPE html>
<html>
<head>
    <title>
        {{ organizationName }} | Contribution Statement
    </title>

    <!-- Included CSS Files -->
    <link rel=""stylesheet"" href=""https://stackpath.bootstrapcdn.com/bootstrap/3.4.1/css/bootstrap.min.css"" integrity=""sha384-HSMxcRTRxnN+Bdg0JdbxYKrThecOKuH5zCYotlSAcp1+c8xmyTe9GYg1l9a69psu"" crossorigin=""anonymous"">

   <style>
        html, body {
            height: auto;
            width: 100%;
            min-width: 100%;
            font-size: {{ fontSizeBase }};
            margin: 0 0 0 0;
            padding: {{ bodyPadding }};
            vertical-align: top;
            background-color: #FFFFFF;
        }
        
         /* helper classes not included in stock bs3 */
        
        .margin-t-md {
            margin-top: 15px; !important
        }
        .margin-r-md {
            margin-right: 15px; !important
        }
        .margin-b-md {
            margin-bottom: 15px; !important
        }
        .margin-l-md {
            margin-left: 15px; !important
        }
        .padding-t-md {
            padding-top: 15px; !important
        }
        .padding-r-md {
            padding-right: 15px; !important
        }
        .padding-b-md {
            padding-bottom: 15px; !important
        }
        .padding-l-md {
            padding-left: 15px; !important
        }
    </style>

<body>

    <!-- set top padding to help align logo and return address with envelope window -->
    <div style='padding-top:44px'>

    <!-- set fixed height to help align recipient address with envelope window -->
    <div class=""row"" style='{% if RenderMedium == 'Html' %}height:155px{% else %}height:175px{% endif %}'>
        <div class=""col-md-6 pull-left"">
            <div>
                <img src=""{{ publicApplicationRoot }}GetImage.ashx?Id={{ FinancialStatementTemplate.LogoBinaryFileId }}"" width=""170px"" height=""56px"" />
            </div>
            
            <div>
                {{ organizationAddress }}<br />
                {{ organizationWebsite }}
            </div>
        </div>
        <div class=""col-md-6 text-right"">
            <h5>Contribution Summary for {{ Salutation }}</h5>
            <p>{{ StatementStartDate | Date:'M/d/yyyy' }} - {{ StatementEndDate | Date:'M/d/yyyy' }}<p>
        </div>
    </div>

    <h5>
        {{ Salutation }} <br />
        {{ StreetAddress1 }} <br />
        {% if StreetAddress2 != '' %}
            {{ StreetAddress2 }} <br />
        {% endif %}
        {{ City }}, {{ State }} {{ PostalCode }}
    </h5>
</div>

<hr style=""opacity: .5;"" />

<div class='well' style='padding: 8px'>
    <div class=""row"">
        <div class=""col-xs-6 pull-left"">
            <strong style='margin-left: 5px'>Total Cash Gifts This Period</strong>
        </div>
        <div class=""col-xs-6 text-right"">
            <strong>{{ currencySymbol }}{{ TotalContributionAmount }}</strong>
        </div>
    </div>
</div>

<table class=""table table-bordered table-striped table-condensed"">
    <thead>
        <tr>
            <th>Date</th>
            <th>Type</th>
            <th>Account</th>
            <th style=""text-align:right"">Amount</th>
        </tr>
    </thead>    

    <tbody>
    {% for transactionDetail in TransactionDetails %}
        <tr>
            <td>{{ transactionDetail.Transaction.TransactionDateTime | Date:'M/d/yyyy' }}</td>
            <td>{{ transactionDetail.Transaction.FinancialPaymentDetail.CurrencyTypeValue.Value }}</td>
            <td>{{ transactionDetail.Account.Name }}</td>
            <td style=""text-align:right"">{{ currencySymbol }}{{ transactionDetail.Amount }}</td>
        </tr>
    {% endfor %}
    </tbody>
    <tfoot>
    </tfoot>
</table>

{% assign nonCashCount = TransactionDetailsNonCash | Size %}

{% if nonCashCount > 0 %}
    <hr style=""opacity: .5;"" />

    <h3>Non-Cash Gifts</h3>

    <table class=""table table-condensed"">
        <thead>
            <tr>
                <th>Date</th>
                <th>Fund</th>
                <th>Description</th>
                <th style=""text-align:right"">Amount</th>
            </tr>
        </thead>    

        <tbody>
        {% for transactionDetailNonCash in TransactionDetailsNonCash %}
            <tr>
                <td>{{ transactionDetailNonCash.Transaction.TransactionDateTime | Date:'M/d/yyyy' }}</td>
                <td>{{ transactionDetailNonCash.Account.Name }}</td>
                <td>{{ transactionDetailNonCash.Transaction.Summary }} {{ transactionDetailNonCash.Summary }}</td>
                <td style=""text-align:right"">{{ currencySymbol }}{{ transactionDetailNonCash.Amount }}</td>
            </tr>
        {% endfor %}
        </tbody>
        <tfoot>
        </tfoot>
    </table>
{% endif %}

{% assign accountSummaryCount = AccountSummary | Size %}

{% if accountSummaryCount > 0 %}
<hr style=""opacity: .5;"" />

{% if RenderMedium == 'Html' %}
<div class=""row"">
    <div class=""col-xs-6 col-xs-offset-6"">
        <h4 class=""margin-t-md margin-b-md"">Fund Summary</h4>
        <div class=""row"">
            <div class=""col-xs-6"">
                <strong>Fund Name</strong>
            </div>
            <div class=""col-xs-6 text-right"">
                <strong>Total Amount</strong>
            </div>
        </div>
        
        {% for accountsummary in AccountSummary %}
            <div class=""row"">
                <div class=""col-xs-6"">{{ accountsummary.AccountName }}</div>
                <div class=""col-xs-6 text-right"">{{ accountsummary.Total | FormatAsCurrency }}</div>
            </div>
         {% endfor %}
    </div>
</div>
{% else %}
    <h3>Account Totals</h3>
    {% for accountsummary in AccountSummary %}
        <div class=""row"">
            <div class=""col-xs-3 pull-left"">{{ accountsummary.AccountName }}</div>
            <div class=""col-xs-3 text-right"">{{ currencySymbol }}{{ accountsummary.Total }}</div>
            <div class=""col-xs-6""></div>
        </div>
    {% endfor %}
{% endif %}

{% endif %}
 
{% assign pledgeCount = Pledges | Size %}

{% if pledgeCount > 0 %}
    <hr style=""opacity: .5;"" />

    <h3>Pledges</h3>
 
    {% for pledge in Pledges %}
        <div class=""row"">
            <div class=""col-xs-3"">
                <strong>{{ pledge.AccountName }}</strong>
                
                <p>
                    Amt Pledged: {{ currencySymbol }}{{ pledge.AmountPledged }} <br />
                    Amt Given: {{ currencySymbol }}{{ pledge.AmountGiven }} <br />
                    Amt Remaining: {{ currencySymbol }}{{ pledge.AmountRemaining }}
                </p>
            </div>
            <div class=""col-xs-3"">
                <br />
                <p>
                    Percent Complete <br />
                    {{ pledge.PercentComplete }}%
                    <br />
                </p>
            </div>
        </div>
    {% endfor %}
{% endif %}

<hr style=""opacity: .5;"" />
<p class=""text-center"">
    Thank you for your continued support of the {{ organizationName }}. If you have any questions about your statement,
    email {{ organizationEmail }} or call {{ organizationPhone }}.
</p>

<p class=""text-center"">
    <em>Unless otherwise noted, the only goods and services provided are intangible religious benefits.</em>
</p>

</body>
</html>";

        private const string StatementReportSettingsJson = @"{
  ""TransactionSettings"": {
    ""AccountSelectionOption"": 0,
    ""SelectedAccountIds"": [ 1 ],
    ""CurrencyTypesForCashGiftGuids"": [ ""8b086a19-405a-451f-8d44-174e92d6b402"", ""f3adc889-1ee8-4eb6-b3fd-8c10f3c8af93"", ""928a2e04-c77b-4282-888f-ec549cee026a"", ""dabee8fd-aedf-43e1-8547-4c97fa14d9b6"", ""d42c4df7-1ae9-4dde-ada2-774b866b798c"", ""6151f6e0-3223-46ba-a59e-e091be4af75c"", ""56c9ae9c-b5eb-46d5-9650-2ef86b14f856"", ""0fdf0bb3-b483-4c0a-9dff-a35abe3b688d"" ],
    ""CurrencyTypesForNonCashGuids"": [ ""7950ff66-80ee-e8ab-4a77-4a13edeb7513"" ],
    ""TransactionTypeGuids"": [ ""2d607262-52d6-4724-910d-5c6e8fb89acc"" ],
    ""HideRefundedTransactions"": true,
    ""HideCorrectedTransactionOnSameData"": true
  },
  ""PledgeSettings"": {
    ""AccountIds"": [],
    ""IncludeGiftsToChildAccounts"": false,
    ""IncludeNonCashGifts"": false
  },
  ""PDFSettings"": {
    ""PaperSize"": 0,
    ""MarginRightMillimeters"": 10,
    ""MarginLeftMillimeters"": 10,
    ""MarginBottomMillimeters"": 15,
    ""MarginTopMillimeters"": 10
  }
}";

        private const string StatementFooterSettingsJson = @"{
  ""TransactionSettings"": {
    ""AccountSelectionOption"": 0,
    ""SelectedAccountIds"": [ 1 ],
    ""CurrencyTypesForCashGiftGuids"": [ ""8b086a19-405a-451f-8d44-174e92d6b402"", ""f3adc889-1ee8-4eb6-b3fd-8c10f3c8af93"", ""928a2e04-c77b-4282-888f-ec549cee026a"", ""dabee8fd-aedf-43e1-8547-4c97fa14d9b6"", ""d42c4df7-1ae9-4dde-ada2-774b866b798c"", ""6151f6e0-3223-46ba-a59e-e091be4af75c"", ""56c9ae9c-b5eb-46d5-9650-2ef86b14f856"", ""0fdf0bb3-b483-4c0a-9dff-a35abe3b688d"" ],
    ""CurrencyTypesForNonCashGuids"": [ ""7950ff66-80ee-e8ab-4a77-4a13edeb7513"" ],
    ""TransactionTypeGuids"": [ ""2d607262-52d6-4724-910d-5c6e8fb89acc"" ],
    ""HideRefundedTransactions"": true,
    ""HideCorrectedTransactionOnSameData"": true
  },
  ""PledgeSettings"": {
    ""AccountIds"": [],
    ""IncludeGiftsToChildAccounts"": false,
    ""IncludeNonCashGifts"": false
  },
  ""PDFSettings"": {
    ""PaperSize"": 0,
    ""MarginRightMillimeters"": 10,
    ""MarginLeftMillimeters"": 10,
    ""MarginBottomMillimeters"": 15,
    ""MarginTopMillimeters"": 10
  }
}";

        #endregion

        private readonly ConcurrentDictionary<string, string> _gatewayPersonIdentifierMap = new ConcurrentDictionary<string, string>();

        private readonly ConcurrentDictionary<string, string> _scheduleIdentifierMap = new ConcurrentDictionary<string, string>();

        private int _stepCount;

        /// <inheritdoc/>
        public override async Task ExecuteAsync()
        {
            _stepCount = 11;
            var step = 1;

            await ProcessFinancialAccountsAsync( step++ );
            await ProcessFinancialBatchesAsync( step++ );
            await ProcessFinancialPaymentDetailsAsync( step++ );
            await ProcessFinancialPersonBankAccountsAsync( step++ );
            await ProcessFinancialPersonSavedAccountsAsync( step++ );
            await ProcessFinancialScheduledTransactionsAsync( step++ );
            await ProcessFinancialScheduledTransactionDetailsAsync( step++ );
            await ProcessFinancialStatementTemplatesAsync( step++ );
            await ProcessFinancialTransactionsAsync( step++ );
            await ProcessFinancialTransactionDetailsAsync( step++ );
            await ProcessFinancialTransactionRefundsAsync( step++ );
        }

        /// <summary>
        /// Sanitize the FinancialAccount table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialAccountsAsync( int step )
        {
            var accounts = await Sweeper.SqlQueryAsync( "SELECT [Id], [Description], [Url], [PublicDescription] FROM [FinancialAccount]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in accounts )
            {
                var id = ( int ) account["Id"];
                var description = ( string ) account["Description"];
                var url = ( string ) account["Url"];
                var publicDescription = ( string ) account["PublicDescription"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( description ) )
                {
                    changes["Description"] = Sweeper.DataFaker.Lorem.Sentence( description.Split( ' ' ).Length );
                }

                if ( !string.IsNullOrWhiteSpace( url ) )
                {
                    changes["Url"] = Sweeper.DataFaker.Internet.Url();
                }

                if ( !string.IsNullOrWhiteSpace( publicDescription ) )
                {
                    changes["PublicDescription"] = Sweeper.DataFaker.Lorem.Sentence( publicDescription.Split( ' ' ).Length );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            await Sweeper.UpdateDatabaseRecordsAsync( "FinancialAccount", bulkChanges, p => Progress( p, step, _stepCount ) );

            Progress( 1, step, _stepCount );
        }

        /// <summary>
        /// Sanitize the FinancialBatch table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialBatchesAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialBatch] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialBatchesAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialBatch", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialBatchesAsync( List<int> ids )
        {
            var batches = await Sweeper.SqlQueryAsync( $"SELECT [Id], [Note] FROM [FinancialBatch] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var batch in batches )
            {
                var id = ( int ) batch["Id"];
                var note = ( string ) batch["Note"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( note ) )
                {
                    changes["Note"] = Sweeper.DataFaker.Lorem.Sentence( note.Split( ' ' ).Length );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialPaymentDetail table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialPaymentDetailsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialPaymentDetail] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialPaymentDetailsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialPaymentDetail", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialPaymentDetailsAsync( List<int> ids )
        {
            var details = await Sweeper.SqlQueryAsync( $"SELECT [Id], [AccountNumberMasked], [NameOnCard] FROM [FinancialPaymentDetail] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var detail in details )
            {
                var id = ( int ) detail["Id"];
                var accountNumberMasked = ( string ) detail["AccountNumberMasked"];
                var nameOnCard = ( string ) detail["NameOnCard"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( accountNumberMasked ) )
                {
                    changes["AccountNumberMasked"] = accountNumberMasked.RandomizeLettersAndNumbers( '*' );
                }

                if ( !string.IsNullOrWhiteSpace( nameOnCard ) )
                {
                    changes["NameOnCard"] = Sweeper.DataFaker.Name.FullName();
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialPersonBankAccount table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialPersonBankAccountsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialPersonBankAccount] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialPersonBankAccountsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialPersonBankAccount", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialPersonBankAccountsAsync( List<int> ids )
        {
            var accounts = await Sweeper.SqlQueryAsync( $"SELECT [Id], [AccountNumberMasked] FROM [FinancialPersonBankAccount] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in accounts )
            {
                var id = ( int ) account["Id"];
                var accountNumberMasked = ( string ) account["AccountNumberMasked"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( accountNumberMasked ) )
                {
                    changes["AccountNumberMasked"] = accountNumberMasked.RandomizeLettersAndNumbers( ' ' );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialPersonSavedAccount table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialPersonSavedAccountsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialPersonSavedAccount] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialPersonSavedAccountsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialPersonSavedAccount", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialPersonSavedAccountsAsync( List<int> ids )
        {
            var accounts = await Sweeper.SqlQueryAsync( $"SELECT [Id], [ReferenceNumber], [Name], [TransactionCode], [GatewayPersonIdentifier] FROM [FinancialPersonSavedAccount] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in accounts )
            {
                var id = ( int ) account["Id"];
                var referenceNumber = ( string ) account["ReferenceNumber"];
                var name = ( string ) account["Name"];
                var transactionCode = ( string ) account["TransactionCode"];
                var gatewayPersonIdentifier = ( string ) account["GatewayPersonIdentifier"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( referenceNumber ) )
                {
                    changes["ReferenceNumber"] = referenceNumber.RandomizeLettersAndNumbers();
                }

                if ( !string.IsNullOrWhiteSpace( name ) )
                {
                    changes["Name"] = Sweeper.DataFaker.Lorem.Sentence( name.Split( ' ' ).Length ).Left( 50 );
                }

                if ( !string.IsNullOrWhiteSpace( transactionCode ) )
                {
                    changes["TransactionCode"] = transactionCode.RandomizeLettersAndNumbers();
                }

                if ( !string.IsNullOrWhiteSpace( gatewayPersonIdentifier ) )
                {
                    changes["GatewayPersonIdentifier"] = _gatewayPersonIdentifierMap.GetOrAdd( gatewayPersonIdentifier, gpid => gpid.RandomizeLettersAndNumbers() );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialScheduledTransactions table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialScheduledTransactionsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialScheduledTransaction] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialScheduledTransactionsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialScheduledTransaction", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialScheduledTransactionsAsync( List<int> ids )
        {
            var scheduledTransactions = await Sweeper.SqlQueryAsync( $"SELECT [Id], [TransactionCode], [GatewayScheduleId], [Summary], [PreviousGatewayScheduleIdsJson] FROM [FinancialScheduledTransaction] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var scheduledTransaction in scheduledTransactions )
            {
                var id = ( int ) scheduledTransaction["Id"];
                var transactionCode = ( string ) scheduledTransaction["TransactionCode"];
                var gatewayScheduleId = ( string ) scheduledTransaction["GatewayScheduleId"];
                var summary = ( string ) scheduledTransaction["Summary"];
                var previousGatewayScheduleIdsJson = ( string ) scheduledTransaction["PreviousGatewayScheduleIdsJson"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( transactionCode ) )
                {
                    changes["TransactionCode"] = transactionCode.RandomizeLettersAndNumbers();
                }

                if ( !string.IsNullOrWhiteSpace( gatewayScheduleId ) )
                {
                    changes["GatewayScheduleId"] = _scheduleIdentifierMap.GetOrAdd( gatewayScheduleId, sid => sid.RandomizeLettersAndNumbers() );
                }

                if ( !string.IsNullOrWhiteSpace( summary ) )
                {
                    changes["Summary"] = Sweeper.DataFaker.Lorem.Sentence( summary.Split( ' ' ).Length );
                }

                if ( !string.IsNullOrWhiteSpace( previousGatewayScheduleIdsJson ) )
                {
                    try
                    {
                        var previousScheduleIds = JsonConvert.DeserializeObject<List<string>>( previousGatewayScheduleIdsJson )
                            .Select( s => _scheduleIdentifierMap.GetOrAdd( s, sid => sid.RandomizeLettersAndNumbers() ) )
                            .ToList();

                        changes["PreviousGatewayScheduleIdsJson"] = JsonConvert.SerializeObject( previousScheduleIds );
                    }
                    catch
                    {
                        // Intentionally ignored.
                    }
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialScheduledTransactionDetail table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialScheduledTransactionDetailsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialScheduledTransactionDetail] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialScheduledTransactionDetailsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialScheduledTransactionDetail", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialScheduledTransactionDetailsAsync( List<int> ids )
        {
            var details = await Sweeper.SqlQueryAsync( $"SELECT [Id], [Summary] FROM [FinancialScheduledTransactionDetail] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var detail in details )
            {
                var id = ( int ) detail["Id"];
                var summary = ( string ) detail["Summary"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( summary ) )
                {
                    changes["Summary"] = Sweeper.DataFaker.Lorem.Sentence( summary.Split( ' ' ).Length ).Left( 500 );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialStatementTemplate table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialStatementTemplatesAsync( int step )
        {
            var parameters = new Dictionary<string, object>
            {
                { "ReportTemplate", StatementReportTemplate },
                { "ReportSettingsJson", StatementReportSettingsJson },
                { "FooterSettingsJson", StatementFooterSettingsJson }
            };

            await Sweeper.SqlCommandAsync( @"UPDATE [FinancialStatementTemplate]
SET [ReportTemplate] = @ReportTemplate
, [ReportSettingsJson] = @ReportSettingsJson
, [FooterSettingsJson] = @FooterSettingsJson", parameters );

            Progress( 1, step, _stepCount );
        }

        /// <summary>
        /// Sanitize the FinancialTransaction table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialTransactionsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialTransaction] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialTransactionsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialTransaction", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialTransactionsAsync( List<int> ids )
        {
            var accounts = await Sweeper.SqlQueryAsync( $"SELECT [Id], [TransactionCode], [Summary] FROM [FinancialTransaction] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in accounts )
            {
                var id = ( int ) account["Id"];
                var transactionCode = ( string ) account["TransactionCode"];
                var summary = ( string ) account["Summary"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( transactionCode ) )
                {
                    changes["TransactionCode"] = transactionCode.RandomizeLettersAndNumbers();
                }

                if ( !string.IsNullOrWhiteSpace( summary ) )
                {
                    changes["Summary"] = Sweeper.DataFaker.Lorem.Sentence( summary.Split( ' ' ).Length );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialTransactionDetail table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialTransactionDetailsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialTransactionDetail] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialTransactionDetailsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialTransactionDetail", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialTransactionDetailsAsync( List<int> ids )
        {
            var details = await Sweeper.SqlQueryAsync( $"SELECT [Id], [Summary] FROM [FinancialTransactionDetail] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var detail in details )
            {
                var id = ( int ) detail["Id"];
                var summary = ( string ) detail["Summary"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( summary ) )
                {
                    changes["Summary"] = Sweeper.DataFaker.Lorem.Sentence( summary.Split( ' ' ).Length ).Left( 500 );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }

        /// <summary>
        /// Sanitize the FinancialTransactionRefund table.
        /// </summary>
        /// <returns>A task that represents the operation.</returns>
        private async Task ProcessFinancialTransactionRefundsAsync( int step )
        {
            var ids = await Sweeper.SqlQueryAsync<int>( "SELECT [Id] FROM [FinancialTransactionRefund] ORDER BY [Id]" );
            var reporter = new CountProgressReporter( ids.Count, p => Progress( p, step, _stepCount ) );

            await AsyncProducer.FromItems( ids.Chunk( 2_500 ).Select( c => c.ToList() ) )
                .Pipe( async items =>
                {
                    var result = await ScrubFinancialTransactionRefundsAsync( items );

                    reporter.Add( items.Count - result.Count );

                    return result;
                } )
                .Consume( async changes =>
                {
                    if ( changes.Any() )
                    {
                        await Sweeper.UpdateDatabaseRecordsAsync( "FinancialTransactionRefund", changes );
                        reporter.Add( changes.Count );
                    }
                } )
                .RunAsync( Sweeper.CancellationToken );
        }

        private async Task<List<Tuple<int, Dictionary<string, object>>>> ScrubFinancialTransactionRefundsAsync( List<int> ids )
        {
            var details = await Sweeper.SqlQueryAsync( $"SELECT [Id], [RefundReasonSummary] FROM [FinancialTransactionRefund] WITH (NOLOCK) WHERE [Id] IN ({string.Join( ",", ids )}) ORDER BY [Id]" );
            var bulkChanges = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var detail in details )
            {
                var id = ( int ) detail["Id"];
                var refundReasonSummary = ( string ) detail["RefundReasonSummary"];
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( refundReasonSummary ) )
                {
                    changes["RefundReasonSummary"] = Sweeper.DataFaker.Lorem.Sentence( refundReasonSummary.Split( ' ' ).Length );
                }

                if ( changes.Any() )
                {
                    bulkChanges.Add( new Tuple<int, Dictionary<string, object>>( id, changes ) );
                }
            }

            return bulkChanges;
        }
    }
}
