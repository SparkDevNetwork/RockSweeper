using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RockSweeper.Utility
{
    public class SweeperConfiguration
    {
        public IEnumerable<SweeperOption> Options { get; set; }

        public string ConnectionString { get; set; }

        public string RockWebFolder { get; set; }
    }
}
