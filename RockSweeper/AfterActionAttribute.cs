using System;

namespace RockSweeper
{
    [AttributeUsage( AttributeTargets.Field, AllowMultiple = true )]
    public class AfterActionAttribute : System.Attribute
    {
        public SweeperAction Action { get; set; }

        public AfterActionAttribute( SweeperAction action )
        {
            Action = action;
        }
    }
}
