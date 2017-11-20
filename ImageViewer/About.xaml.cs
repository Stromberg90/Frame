using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Frame
{
    public partial class About
    {
        public About()
        {
            InitializeComponent();
            VersionText.Text = VersionText.Text.Replace("0.0.0", ImageViewerWm.VERSION);
            CopyrightText.Text = CopyrightText.Text.Replace("{}", System.DateTime.Now.Year.ToString());
            UpdateManager.CheckForUpdate();
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