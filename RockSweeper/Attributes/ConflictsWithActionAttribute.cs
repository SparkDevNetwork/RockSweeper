using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
    public class ConflictsWithActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the action method name that this action conflicts with.
        /// </summary>
        /// <value>
        /// The action to be run after.
        /// </value>
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictsWithActionAttribute"/> class.
        /// </summary>
        /// <param name="type">The action class.</param>
        public ConflictsWithActionAttribute( Type type )
        {
            Type = type;
        }
    }
}
