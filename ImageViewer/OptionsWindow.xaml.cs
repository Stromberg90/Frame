using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

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
                get => Properties.Settings.Default.ImageFullZoom;
                set => Properties.Settings.Default.ImageFullZoom = value;
            }

            [DisplayName("Channels Montage Colored Borders")]
            public bool SplitChannelsBorder
            {
                get => Properties.Settings.Default.SplitChannelsBorder;
                set => Properties.Settings.Default.SplitChannelsBorder = value;
            }

            [DisplayName("Replace Tab On Drop")]
            public bool ReplaceTabOnDrop
            {
                get => Properties.Settings.Default.ReplaceImageOnDrop;
                set => Properties.Settings.Default.ReplaceImageOnDrop = value;
            }

            [DisplayName("Background Color")]
            public Color BackgroundColor
            {
                get
                {
                    var color = Properties.Settings.Default.BackgroundColor;
                    return Color.FromArgb(color.A, color.R, color.G, color.B);
                }
                set => Properties.Settings.Default.BackgroundColor =
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
                Properties.Settings.Default.Reload();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            CancelChanges();
            Hide();
            e.Cancel = true;
        }

        void Save_OnClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            Hide();
        }

        void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            CancelChanges();
            Hide();
        }

        void CancelChanges()
        {
            Properties.Settings.Default.Reload();
        }
    }
}