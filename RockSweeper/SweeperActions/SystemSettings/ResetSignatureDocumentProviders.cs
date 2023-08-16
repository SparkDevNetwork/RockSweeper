﻿using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the signature document providers.
    /// </summary>
    /// <param name="actionData">The action data.</param>
    [ActionId( "59fd05b1-0263-4442-9d63-7491e254bcd1" )]
    [Title( "Reset Signature Document Providers" )]
    [Description( "Resets all signed document providers to system default values." )]
    [Category( "System Settings" )]
    [RequiresRockWeb]
    public class ResetSignatureDocumentProviders : SweeperAction
    {
        public override Task ExecuteAsync()
        {
            Sweeper.DeleteAttributeValuesForComponentsOfType( "Rock.Security.DigitalSignatureComponent" );
            Sweeper.SetGlobalAttributeValue( "SignNowAccessToken", string.Empty );

            return Task.CompletedTask;
        }
    }
}