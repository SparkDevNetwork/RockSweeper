using System;

namespace RockSweeper.Attributes
{
    /// <summary>
    /// Defines the user friendly name of the object.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    public class TitleAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleAttribute"/> class.
        /// </summary>
        /// <param name="title">The title.</param>
        public TitleAttribute( string title )
        {
            Title = title;
        }
    }
}
