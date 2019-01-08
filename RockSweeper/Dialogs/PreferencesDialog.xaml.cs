using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        #endregion

        public PreferencesDialog()
        {
            HereAppId = Properties.Settings.Default.HereAppId;
            HereAppCode = Properties.Settings.Default.HereAppCode;

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
            Properties.Settings.Default.Save();

            DialogResult = true;
        }

        #endregion
    }
}
