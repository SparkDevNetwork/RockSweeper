using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Empties the saved account data.
    /// </summary>
    [ActionId( "702834d4-ca31-4ddb-bbac-ce629edbb82d" )]
    [Title( "Scrub Saved Account Tables" )]
    [Description( "Removes sensitive information the saved bank account and saved CC account tables." )]
    [Category( "Data Scrubbing" )]
    public class ScrubSavedAccountTables : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            // Process all the saved account records.
            var accounts = await Sweeper.SqlQueryAsync<int, string, string, string, string>( "SELECT [Id], [ReferenceNumber], [Name], [TransactionCode], [GatewayPersonIdentifier] FROM [FinancialPersonSavedAccount]" );
            var gatewayPersonIdentifierLookup = accounts
                .Select( a => a.Item5 )
                .Distinct()
                .ToDictionary( id => id, id => id.RandomizeLettersAndNumbers() );
            var updates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in accounts )
            {
                var newData = new Dictionary<string, object>
                {
                    { "ReferenceNumber", account.Item2.RandomizeLettersAndNumbers() },
                    { "Name", Sweeper.DataFaker.Lorem.Words( account.Item3.Split( ' ' ).Length ) },
                    { "TransactionCode", account.Item4.RandomizeLettersAndNumbers() },
                    { "GatewayPersonIdentifier", gatewayPersonIdentifierLookup[account.Item5] }
                };

                updates.Add( new Tuple<int, Dictionary<string, object>>( account.Item1, newData ) );
            }

            await Sweeper.UpdateDatabaseRecordsAsync( "FinancialPersonSavedAccount", updates );

            Progress( 0.5 );

            // Process all the bank account records.
            var bankAccounts = await Sweeper.SqlQueryAsync<int, string, string>( "SELECT [Id], [AccountNumberSecured], [AccountNumberMasked] FROM [FinancialPersonBankAccount]" );
            gatewayPersonIdentifierLookup = accounts
                .Select( a => a.Item5 )
                .Distinct()
                .ToDictionary( id => id, id => id.RandomizeLettersAndNumbers() );
            updates = new List<Tuple<int, Dictionary<string, object>>>();

            foreach ( var account in bankAccounts )
            {
                var newData = new Dictionary<string, object>
                {
                    { "AccountNumberSecured", string.Empty },
                    { "AccountNumberMasked", account.Item3.RandomizeLettersAndNumbers( '*' ) }
                };

                updates.Add( new Tuple<int, Dictionary<string, object>>( account.Item1, newData ) );
            }

            await Sweeper.UpdateDatabaseRecordsAsync( "FinancialPersonBankAccount", updates );
        }
    }
}
