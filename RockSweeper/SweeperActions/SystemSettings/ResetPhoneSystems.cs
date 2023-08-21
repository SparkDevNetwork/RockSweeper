﻿using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.SystemSettings
{
    /// <summary>
    /// Resets the phone systems.
    /// </summary>
    [ActionId( "afee211e-2a2d-4d85-a428-81ac95637ed6" )]
    [Title( "Reset Phone Systems" )]
    [Description( "Resets all phone system settings to system default values." )]
    [Category( "System Settings" )]
    [ConflictsWithAction( typeof( DisablePhoneSystems ) )]
    public class ResetPhoneSystems : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            await Sweeper.DeleteAttributeValuesForComponentsOfTypeAsync( "Rock.Pbx.PbxComponent" );
        }
    }
}
