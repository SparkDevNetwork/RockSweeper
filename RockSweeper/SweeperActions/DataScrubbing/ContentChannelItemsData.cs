using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes the content channel items.
    /// </summary>
    [ActionId( "6ecb7b00-e1ed-44e9-b24e-e4beffbac9f3" )]
    [Title( "Content Channel Items" )]
    [Description( "Replaces content channel item content with ipsum text." )]
    [Category( "Data Scrubbing" )]
    public class ContentChannelItemsData : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var contentChannelItems = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id], [Content] FROM [ContentChannelItem]" );

            for ( int i = 0; i < contentChannelItems.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( contentChannelItems[i].Item2 ) )
                {
                    changes.Add( "Content", Sweeper.DataFaker.Lorem.ReplaceNonHtmlWords( contentChannelItems[i].Item2 ) );
                }

                if ( changes.Any() )
                {
                    await Sweeper.UpdateDatabaseRecordAsync( "ContentChannelItem", contentChannelItems[i].Item1, changes );
                }

                Progress( i / ( double ) contentChannelItems.Count );
            }
        }
    }
}
