using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using RockSweeper.Attributes;

namespace RockSweeper.SweeperActions.DataScrubbing
{
    /// <summary>
    /// Sanitizes the devices.
    /// </summary>
    [ActionId( "d23bfc10-7c58-4f9c-8497-553a6a5c7c1c" )]
    [Title( "Sanitize Devices" )]
    [Description( "Replacing IPAddress with fake address information." )]
    [Category( "Data Scrubbing" )]
    public class SanitizeDevices : SweeperAction
    {
        public override async Task ExecuteAsync()
        {
            var devices = await Sweeper.SqlQueryAsync<int, string>( "SELECT [Id], [IPAddress] FROM [Device]" );

            foreach ( var device in devices )
            {
                var changes = new Dictionary<string, object>();

                if ( device.Item2 == "::1" || device.Item2 == "127.0.0.1" )
                {
                    continue;
                }

                if ( System.Net.IPAddress.TryParse( device.Item2, out var _ ) )
                {
                    ushort subAddress = ( ushort ) device.Item1;
                    var bytes = BitConverter.GetBytes( subAddress );

                    changes.Add( "IPAddress", $"172.16.{bytes[1]}.{bytes[0]}" );
                }
                else
                {
                    changes.Add( "IPAddress", $"device-{device.Item1}.rocksolidchurchdemo.com" );
                }

                await Sweeper.UpdateDatabaseRecordAsync( "Device", device.Item1, changes );
            }
        }
    }
}
