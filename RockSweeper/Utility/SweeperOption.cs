using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the unique identifier of this option instance.
        /// </summary>
        /// <value>
        /// The unique identifier of this option instance.
        /// </value>
        public Guid Id { get; } = Guid.NewGuid();

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
        /// Gets the title.
        /// </summary>
        /// <value>
        /// The title.
        /// </value>
        public string Title => GetActionAttribute<TitleAttribute>( Action )?.Title ?? Action.ToString();

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>
        /// The category.
        /// </value>
        public string Category => GetActionAttribute<CategoryAttribute>( Action )?.Category ?? "General";

        /// <summary>
        /// Gets the tooltip description.
        /// </summary>
        /// <value>
        /// The tooltip description.
        /// </value>
        public string Description => GetActionAttribute<DescriptionAttribute>( Action )?.Description ?? string.Empty;

        /// <summary>
        /// Gets the name of the method to be called.
        /// </summary>
        /// <value>
        /// The name of the method to be called.
        /// </value>
        public string MethodName => Action.ToString();

        /// <summary>
        /// Gets the full display name.
        /// </summary>
        /// <value>
        /// The full display name.
        /// </value>
        public string FullName => $"{ Category } >> { Title }";

        /// <summary>
        /// Gets a value indicating whether the RockWeb folder is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the RockWeb folder is required; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresRockWeb => GetActionAttribute<RequiresRockWebAttribute>( Action ) != null;

        /// <summary>
        /// Gets a value indicating whether location services are required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if location services are required; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresLocationServices => GetActionAttribute<RequiresLocationServiceAttribute>( Action ) != null;

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public SweeperAction Action { get; private set; }

        /// <summary>
        /// Gets the actions that this option must run after.
        /// </summary>
        /// <value>
        /// The actions that this option must run after.
        /// </value>
        public ICollection<SweeperAction> RunAfterActions
        {
            get
            {
                return GetActionAttributes<AfterActionAttribute>( Action )
                    .Select( a => a.Action )
                    .ToList();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SweeperOption"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        public SweeperOption( SweeperAction action )
        {
            Action = action;

            var defaultValue = GetActionAttribute<DefaultValueAttribute>( action );
            if ( defaultValue != null )
            {
                Selected = (bool)defaultValue.Value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the action attribute.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected T GetActionAttribute<T>( SweeperAction action )
        {
            var memberInfo = action.GetType().GetMember( action.ToString() ).FirstOrDefault();

            if ( memberInfo != null )
            {
                return (T)memberInfo.GetCustomAttributes( typeof( T ), false ).SingleOrDefault();
            }

            return default( T );
        }

        /// <summary>
        /// Gets the action attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        protected ICollection<T> GetActionAttributes<T>( SweeperAction action )
        {
            var memberInfo = action.GetType().GetMember( action.ToString() ).FirstOrDefault();

            if ( memberInfo != null )
            {
                return memberInfo.GetCustomAttributes( typeof( T ), false ).Cast<T>().ToList();
            }

            return new List<T>();
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
