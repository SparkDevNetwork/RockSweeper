using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using RockSweeper.Attributes;

namespace RockSweeper
{
    public class SweeperAction
    {
        #region Properties

        /// <summary>
        /// Gets the unique identifier of this option instance.
        /// </summary>
        /// <value>
        /// The unique identifier of this option instance.
        /// </value>
        public Guid Id { get; }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title { get; }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public string Category { get; }

        /// <summary>
        /// Gets the tooltip description.
        /// </summary>
        /// <value>
        /// The tooltip description.
        /// </value>
        public string Description { get; }

        /// <summary>
        /// Gets the name of the method to be called.
        /// </summary>
        /// <value>
        /// The name of the method to be called.
        /// </value>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the full display name.
        /// </summary>
        /// <value>
        /// The full display name.
        /// </value>
        public string FullName => $"{Category} >> {Title}";

        /// <summary>
        /// Gets a value indicating whether the RockWeb folder is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the RockWeb folder is required; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresRockWeb { get; }

        /// <summary>
        /// Gets a value indicating whether location services are required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if location services are required; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresLocationServices { get; }

        /// <summary>
        /// Gets the actions that this option must run after.
        /// </summary>
        /// <value>
        /// The actions that this option must run after.
        /// </value>
        public ICollection<Guid> RunAfterActions { get; }

        /// <summary>
        /// Gets the actions that this action conflicts with.
        /// </summary>
        /// <value>
        /// The actions that this action conflicts with.
        /// </value>
        public ICollection<Guid> ConflictingActions { get; }

        /// <summary>
        /// Gets a value that indicates if this action should be selected by default.
        /// </summary>
        /// <value>
        /// <c>true</c> if this action should be selected by default.
        /// </value>
        public bool SelectedByDefault { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SweeperAction"/> class.
        /// </summary>
        /// <param name="method">The method that performs this action.</param>
        public SweeperAction( MethodInfo method )
        {
            Method = method;
            Id = method.GetCustomAttribute<ActionIdAttribute>().Id;
            Title = method.GetCustomAttribute<TitleAttribute>().Title;
            Description = method.GetCustomAttribute<DescriptionAttribute>().Description;
            Category = method.GetCustomAttribute<CategoryAttribute>().Category;
            RequiresRockWeb = method.GetCustomAttribute<RequiresRockWebAttribute>() != null;
            RequiresLocationServices = method.GetCustomAttribute<RequiresLocationServiceAttribute>() != null;

            var defaultValue = method.GetCustomAttribute<DefaultValueAttribute>()?.Value;

            if ( defaultValue != null && defaultValue is bool boolDefaultValue )
            {
                SelectedByDefault = boolDefaultValue;
            }

            RunAfterActions = method.GetCustomAttributes<AfterActionAttribute>()
                .Select( a => typeof( SweeperController ).GetMethod( a.MethodName ) )
                .Select( a => a.GetCustomAttribute<ActionIdAttribute>().Id )
                .ToList();

            ConflictingActions = method.GetCustomAttributes<ConflictsWithActionAttribute>()
                .Select( a => typeof( SweeperController ).GetMethod( a.MethodName ) )
                .Select( a => a.GetCustomAttribute<ActionIdAttribute>().Id )
                .ToList();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds additional conflicts to this action.
        /// </summary>
        /// <param name="conflictingIds">The identifiers that conflict with this action.</param>
        public void AddConflicts( IEnumerable<Guid> conflictingIds )
        {
            ( ( List<Guid> ) ConflictingActions ).AddRange( conflictingIds );
        }

        #endregion
    }
}
