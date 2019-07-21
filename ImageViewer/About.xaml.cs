using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;
using AutoUpdaterDotNET;

namespace Frame
{
    public partial class About
    {
        public About()
        {
            InitializeComponent();
            VersionText.Text = VersionText.Text.Replace("0.0.0", Assembly.GetEntryAssembly().GetName().Version.ToString());
            CopyrightText.Text = CopyrightText.Text.Replace("{}", System.DateTime.Now.Year.ToString());
            AutoUpdater.Start("http://www.dropbox.com/s/2b0gna7rz889b5u/Update.xml?dl=1");
        }

        void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}