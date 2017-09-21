using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Frame
{
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            VersionText.Text = VersionText.Text.Replace("0.0.0", ImageViewerWM.VERSION);
            CopyrightText.Text = CopyrightText.Text.Replace("{}", System.DateTime.Now.Year.ToString());
        }

        void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
