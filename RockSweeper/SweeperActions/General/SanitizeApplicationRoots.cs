using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.General
{
    /// <summary>
    /// Modifies the PublicApplicationRoot and InternalApplicationRoot to safe values.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "dcb7cde1-7764-4ce4-bbb8-13001c4cc9dd" )]
    [Title( "Sanitize Application Roots" )]
    [Description( "Modifies the PublicApplicationRoot and InternalApplicationRoot to safe values." )]
    [Category( "General" )]
    [DefaultValue( true )]
    public class SanitizeApplicationRoots : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.SetGlobalAttributeValue( "InternalApplicationRoot", "http://rock.example.org/" );
            await Sweeper.SetGlobalAttributeValue( "PublicApplicationRoot", "http://www.example.org/" );
        }
    }
}
