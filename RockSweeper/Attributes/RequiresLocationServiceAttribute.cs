using System;

namespace RockSweeper.Attributes
{
    [AttributeUsage( AttributeTargets.Class )]
    public class RequiresLocationServiceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresLocationServiceAttribute"/> class.
        /// </summary>
        public RequiresLocationServiceAttribute()
        {
        }
    }
}
