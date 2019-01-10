using System.Windows;

namespace RockSweeper.Dialogs
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesDialog : Window
    {
        #region Properties

        public string HereAppId { get; set; }

        public string HereAppCode { get; set; }

        public string TargetGeoCenter { get; set; }

        public bool JitterAddresses { get; set; }

        #endregion

        public PreferencesDialog()
        {
            HereAppId = Properties.Settings.Default.HereAppId;
            HereAppCode = Properties.Settings.Default.HereAppCode;
            TargetGeoCenter = Properties.Settings.Default.TargetGeoCenter;
            JitterAddresses = Properties.Settings.Default.JitterAddresses;

            InitializeComponent();
            DataContext = this;
        }

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the Ok control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Ok_Click( object sender, RoutedEventArgs e )
        {
            Properties.Settings.Default.HereAppId = HereAppId;
            Properties.Settings.Default.HereAppCode = HereAppCode;
            Properties.Settings.Default.TargetGeoCenter = TargetGeoCenter;
            Properties.Settings.Default.JitterAddresses = JitterAddresses;
            Properties.Settings.Default.Save();

            DialogResult = true;
        }

        #endregion
    }
}
