﻿using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Sanitizes the application roots.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "dcb7cde1-7764-4ce4-bbb8-13001c4cc9dd" )]
    [Title( "Sanitize Application Roots" )]
    [Description( "Modifies the PublicApplicationRoot and InternalApplicationRoot to safe values." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    public class SanitizeApplicationRoots : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.SetGlobalAttributeValue( "InternalApplicationRoot", "http://rock.example.org" );
            Sweeper.SetGlobalAttributeValue( "PublicApplicationRoot", "http://www.example.org" );

            return Task.CompletedTask;
        }
    }
}