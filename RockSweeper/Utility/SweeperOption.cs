using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using RockSweeper.Attributes;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Defines a configurable action to perform when sweeping through Rock.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class SweeperOption : INotifyPropertyChanged
    {
        #region Events

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

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
        /// Gets the type of the action class.
        /// </summary>
        /// <value>
        /// The type of the action class.
        /// </value>
        public Type ActionType { get; }

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
        /// Gets the full display name.
        /// </summary>
        /// <value>
        /// The full display name.
        /// </value>
        public string FullName => $"{Category} >> {Title}";

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SweeperOption"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                NotifyPropertyChanged( "Enabled" );
            }
        }
        private bool _enabled;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SweeperOption"/> is selected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                NotifyPropertyChanged( "Selected" );
            }
        }
        private bool _selected;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SweeperOption"/> is
        /// in conflict with another option.
        /// </summary>
        /// <value>
        /// <c>true</c> if conflicted; otherwise <c>false</c>.
        /// </value>
        public bool Conflicted { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SweeperOption"/> class.
        /// </summary>
        /// <param name="actionType">The class type that will handle the processing.</param>
        public SweeperOption( Type actionType )
        {
            ActionType = actionType;
            Id = actionType.GetCustomAttribute<ActionIdAttribute>().Id;
            Title = actionType.GetCustomAttribute<TitleAttribute>().Title;
            Description = actionType.GetCustomAttribute<DescriptionAttribute>().Description;
            Category = actionType.GetCustomAttribute<CategoryAttribute>().Category;
            RequiresRockWeb = actionType.GetCustomAttribute<RequiresRockWebAttribute>() != null;
            RequiresLocationServices = actionType.GetCustomAttribute<RequiresLocationServiceAttribute>() != null;

            var defaultValue = actionType.GetCustomAttribute<DefaultValueAttribute>()?.Value;

            if ( defaultValue != null && defaultValue is bool boolDefaultValue )
            {
                Selected = boolDefaultValue;
            }

            RunAfterActions = actionType.GetCustomAttributes<AfterActionAttribute>()
                .Select( a => a.Type )
                .Select( t => t.GetCustomAttribute<ActionIdAttribute>().Id )
                .ToList();

            ConflictingActions = actionType.GetCustomAttributes<ConflictsWithActionAttribute>()
                .Select( a => a.Type )
                .Select( t => t.GetCustomAttribute<ActionIdAttribute>().Id )
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

        /// <summary>
        /// Notifies the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void NotifyPropertyChanged( string propertyName )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }

        #endregion
    }
}
