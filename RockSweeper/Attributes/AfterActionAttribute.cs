using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Field, AllowMultiple = true )]
    public class AfterActionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the action to be run after.
        /// </summary>
        /// <value>
        /// The action to be run after.
        /// </value>
        public SweeperAction Action { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AfterActionAttribute"/> class.
        /// </summary>
        /// <param name="action">The action that should be run before the current action.</param>
        public AfterActionAttribute( SweeperAction action )
        {
            Action = action;
        }
    }
}
