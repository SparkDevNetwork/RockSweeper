using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace RockSweeper.Utility
{
    /// <summary>
    /// Defines a configurable action to perform when sweeping through Rock.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class SweeperOption : INotifyPropertyChanged
    {
        #region Fields

        /// <summary>
        /// The backing field for the action.
        /// </summary>
        private readonly SweeperAction _action;

        #endregion

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
        public Guid Id => _action.Id;

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title => _action.Title;

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public string Category => _action.Category;

        /// <summary>
        /// Gets the tooltip description.
        /// </summary>
        /// <value>
        /// The tooltip description.
        /// </value>
        public string Description => _action.Description;

        /// <summary>
        /// Gets the name of the method to be called.
        /// </summary>
        /// <value>
        /// The name of the method to be called.
        /// </value>
        public MethodInfo Method => _action.Method;

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
        public bool RequiresRockWeb => _action.RequiresRockWeb;

        /// <summary>
        /// Gets a value indicating whether location services are required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if location services are required; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresLocationServices => _action.RequiresLocationServices;

        /// <summary>
        /// Gets the actions that this option must run after.
        /// </summary>
        /// <value>
        /// The actions that this option must run after.
        /// </value>
        public ICollection<Guid> RunAfterActions => _action.RunAfterActions;

        /// <summary>
        /// Gets the actions that this option conflicts with.
        /// </summary>
        /// <value>
        /// The actions that this option conflicts with.
        /// </value>
        public ICollection<Guid> ConflictingActions => _action.ConflictingActions;

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
        /// <param name="action">The action.</param>
        public SweeperOption( SweeperAction action )
        {
            _action = action;
            Selected = _action.SelectedByDefault;
        }

        #endregion

        #region Methods

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
