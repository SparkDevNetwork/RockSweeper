using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
    public class ConflictsWithActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the action method name that this action conflicts with.
        /// </summary>
        /// <value>
        /// The action to be run after.
        /// </value>
        public string MethodName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AfterActionAttribute"/> class.
        /// </summary>
        /// <param name="actionMethodName">The name of the action method.</param>
        public ConflictsWithActionAttribute( string methodName )
        {
            MethodName = methodName;
        }
    }
}
