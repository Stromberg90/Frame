using System.ComponentModel;
using System.Windows;
using Frame.Annotations;
using Frame.Properties;

namespace Frame
{
    public partial class OptionsWindow
    {
        readonly Options options = new Options();

        class Options
        {
            [DisplayName("100% Zoom")]
            [UsedImplicitly]
            public bool FullZoom
            {
                get => Settings.Default.ImageFullZoom;
                set => Settings.Default.ImageFullZoom = value;
            }

            [DisplayName("Channels Montage Borders")]
            [UsedImplicitly]
            public bool SplitChannelsBorder
            {
                get => Settings.Default.SplitChannelsBorder;
                set => Settings.Default.SplitChannelsBorder = value;
            }

            [DisplayName("Replace Tab On Drop")]
            [UsedImplicitly]
            public bool ReplaceTabOnDrop
            {
                get => Settings.Default.ReplaceImageOnDrop;
                set => Settings.Default.ReplaceImageOnDrop = value;
            }
        }

        public OptionsWindow()
        {
            InitializeComponent();
            IsVisibleChanged += OnIsVisibleChanged;
//            OptionsProperyGrid.SelectedObject = options;
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
//            OptionsProperyGrid.Update();
            if ((bool) e.NewValue)
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
            var result = MessageBox.Show("This will reset all your options.", "Reset Options!",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning, MessageBoxResult.Cancel);

            if (result == MessageBoxResult.OK)
            {
                Settings.Default.Reset();
//                OptionsProperyGrid.Update();
            }
        }
    }
}