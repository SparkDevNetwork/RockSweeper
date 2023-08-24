using System;
using System.Runtime.InteropServices;

namespace RockSweeper.Utility
{
    public static class SleepHelper
    {
        /// <summary>
        /// Prevents the computer from going to sleep via automatic timer.
        /// </summary>
        public static void PreventSleep()
        {
            SetThreadExecutionState( ExecutionState.EsContinuous | ExecutionState.EsSystemRequired );
        }

        /// <summary>
        /// Allows the computer to go to sleep.
        /// </summary>
        public static void AllowSleep()
        {
            SetThreadExecutionState( ExecutionState.EsContinuous );
        }

        [DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
        private static extern ExecutionState SetThreadExecutionState( ExecutionState esFlags );

        [FlagsAttribute]
        private enum ExecutionState : uint
        {
            EsAwaymodeRequired = 0x00000040,
            EsContinuous = 0x80000000,
            EsDisplayRequired = 0x00000002,
            EsSystemRequired = 0x00000001
        }
    }
}
