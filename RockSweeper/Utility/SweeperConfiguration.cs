using System.Collections.Generic;

namespace RockSweeper.Utility
{
    public class SweeperConfiguration
    {
        public IEnumerable<SweeperOption> Options { get; set; }

        public string ConnectionString { get; set; }
    }
}
