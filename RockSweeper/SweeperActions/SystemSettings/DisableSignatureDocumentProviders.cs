﻿using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Disables the signature document providers.
    /// </summary>
    [ActionId( "0d1246a9-8a19-4658-a1c5-c8804e8bfeab" )]
    [Title( "Disable Signature Document Providers" )]
    [Description( "Updates the Rock configuration to ensure that all signed document providers are disabled." )]
    [Category( "System Settings" )]
    [DefaultValue( true )]
    [ConflictsWithAction( typeof( ResetSignatureDocumentProviders ) )]
    public class DisableSignatureDocumentProviders : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DisableComponentsOfTypeAsync( "Rock.Security.DigitalSignatureComponent" );
        }
    }
}
