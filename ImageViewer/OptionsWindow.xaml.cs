using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Frame.Properties;

namespace Frame
{
    public partial class OptionsWindow
    {
        readonly Options options = new Options();

        class Options
        {
            [DisplayName("100% Zoom")]
            public bool FullZoom
            {
                get => Settings.Default.ImageFullZoom;
                set => Settings.Default.ImageFullZoom = value;
            }

            [DisplayName("Channels Montage Borders")]
            public bool SplitChannelsBorder
            {
                get => Settings.Default.SplitChannelsBorder;
                set => Settings.Default.SplitChannelsBorder = value;
            }

            [DisplayName("Replace Tab On Drop")]
            public bool ReplaceTabOnDrop
            {
                get => Settings.Default.ReplaceImageOnDrop;
                set => Settings.Default.ReplaceImageOnDrop = value;
            }

            [DisplayName("Background Color")]
            public Color BackgroundColor
            {
                get
                {
                    var color = Settings.Default.BackgroundColor;
                    return Color.FromArgb(color.A, color.R, color.G, color.B);
                }
                set => Settings.Default.BackgroundColor =
                    System.Drawing.Color.FromArgb(value.A, value.R, value.G, value.B);
            }
        }

        public OptionsWindow()
        {
            InitializeComponent();
            IsVisibleChanged += OnIsVisibleChanged;
            OptionsProperyGrid.SelectedObject = options;
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OptionsProperyGrid.Update();
            if ((bool)e.NewValue)
            {
                Settings.Default.Reload();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Settings.Default.Reload();
            Hide();
            e.Cancel = true;
        }

        void Save_OnClick(object sender, RoutedEventArgs e)
        {
            Settings.Default.Save();
            Hide();
        }

        void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("This will reset all your options.", "Reset Options!", MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel);

            if (result == MessageBoxResult.OK)
            {
                Settings.Default.Reset();
                OptionsProperyGrid.Update();
            }
        }
    }
}