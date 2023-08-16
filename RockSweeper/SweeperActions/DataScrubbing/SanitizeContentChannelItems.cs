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
    [Title( "Sanitize Content Channel Items" )]
    [Description( "Replaces content channel item content with ipsum text." )]
    [Category( "Data Scrubbing" )]
    public class SanitizeContentChannelItems : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            var contentChannelItems = Sweeper.SqlQuery<int, string>( "SELECT [Id], [Content] FROM [ContentChannelItem]" );
            var regex = new PCRE.PcreRegex( @"(<[^>]*>(*SKIP)(*F)|[^\W]\w+)" );

            for ( int i = 0; i < contentChannelItems.Count; i++ )
            {
                var changes = new Dictionary<string, object>();

                if ( !string.IsNullOrWhiteSpace( contentChannelItems[i].Item2 ) )
                {
                    var newValue = regex.Replace( contentChannelItems[i].Item2, ( m ) =>
                    {
                        return Sweeper.DataFaker.Lorem.Word();
                    } );

                    if ( newValue != contentChannelItems[i].Item2 )
                    {
                        changes.Add( "Content", newValue );
                    }
                }

                if ( changes.Any() )
                {
                    Sweeper.UpdateDatabaseRecord( "ContentChannelItem", contentChannelItems[i].Item1, changes );
                }

                Progress( i / ( double ) contentChannelItems.Count );
            }

            return Task.CompletedTask;
        }
    }
}
