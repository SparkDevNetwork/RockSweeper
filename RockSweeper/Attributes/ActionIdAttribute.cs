using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Method )]
    public class ActionIdAttribute : Attribute
    {
        /// <summary>
        /// Gets the identifier of the action.
        /// </summary>
        /// <value>
        /// The identifier of the action.
        /// </value>
        public Guid Id { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionIdAttribute"/> class.
        /// </summary>
        /// <param name="id">The GUID value.</param>
        public ActionIdAttribute( string id )
        {
            Id = Guid.Parse( id );
        }
    }
}
