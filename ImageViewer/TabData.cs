using System;
using System.Windows.Controls;
using System.Windows.Media;
using Optional.Unsafe;
using System.Windows;

namespace Frame
{
    public enum ApplicationMode
    {
        Normal,
        Slideshow
    }

    class TabData
    {
        public Action<TabData> CloseTabAction;
        public Point Pan { get; set; } = new Point(0.0, 0.0);
        public double Scale { get; set; } = 1.0;
        public TabItem tabItem = TabItem();
        public ImageSet Images { get; set; } = new ImageSet();
        public ImageSettings ImageSettings { get; set; } = new ImageSettings();
        // INotifyPropertyChanged so I can update the header without having to call UpdateTitle() explicitly.
        public ApplicationMode Mode { get; set; } = ApplicationMode.Normal;
        public int CurrentSlideshowTime { get; set; }
        public string InitialImagePath { get; set; }
        public int Index { get; set; }
        public string Path { get { return Images.Paths.ValueOrFailure()[Index]; } }
        public string Title
        {
            set
            {
                ((TextBlock)((StackPanel)tabItem.Header).Children[0]).Text = value;
            }
            get
            {
                return ((TextBlock)((StackPanel)tabItem.Header).Children[0]).Text;
            }
        }

        public string Filename
        {
            get
            {
                return new System.IO.FileInfo(Images.Paths.ValueOrFailure()[Index]).Name;
            }
        }

        static TabItem TabItem()
        {
            var closeTabButton = new Button { Content = "-", IsTabStop = false, FocusVisualStyle = null, Background = new SolidColorBrush(Color.FromArgb(0, 240, 240, 240)), Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, BorderThickness = new Thickness(0) };
            var tabInternalControl = new StackPanel { Orientation = Orientation.Horizontal };
            tabInternalControl.Children.Add(new TextBlock());
            tabInternalControl.Children.Add(closeTabButton);

            return new TabItem { Header = tabInternalControl, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
        }

        public object Width { get; internal set; }
        public object Height { get; internal set; }
        public long Size { get; internal set; }

        public TabData(string tabPath)
        {
            InitialImagePath = tabPath;
            ((Button)((StackPanel)tabItem.Header).Children[1]).Click += TabData_Click;
            (((StackPanel)tabItem.Header)).MouseDown += TabData_MouseDown;
        }

        void TabData_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                CloseTabAction?.Invoke(this);
            }
        }

        public TabData(string tabPath, int currentIndex) : this(tabPath)
        {
            Index = currentIndex;
        }

        void TabData_Click(object sender, RoutedEventArgs e)
        {
            CloseTabAction?.Invoke(this);
        }

        public void UpdateTitle()
        {
            Title = Filename;
        }
    }
}
