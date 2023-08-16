using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
    public class AfterActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the class that this action is to be run after.
        /// </summary>
        /// <value>
        /// The action to be run after.
        /// </value>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AfterActionAttribute"/> class.
        /// </summary>
        /// <param name="type">The action class.</param>
        public AfterActionAttribute( Type type )
        {
            Type = type;
        }
    }
}
