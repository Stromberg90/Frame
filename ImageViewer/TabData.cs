using System;
using System.Windows.Controls;
using System.Windows.Media;

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
        public System.Windows.Point Pan { get; set; } = new System.Windows.Point(0.0, 0.0);
        public double Scale { get; set; } = 1.0;
        public TabItem tabItem;
        public ImageSet images;
        public ImageSettings imageSettings = new ImageSettings();
        // INotifyPropertyChanged so I can update the header without having to call UpdateTitle() explicitly.
        public ApplicationMode Mode { get; set; } = ApplicationMode.Normal;
        public int CurrentSlideshowTime { get; set; }
        public string InitialImagePath { get; set; }
        public int Index { get; set; }
        public string Path { get { return images.Paths[Index]; } }
        public string Title {
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
                return new System.IO.FileInfo(images.Paths[Index]).Name;
            }
        }

        public object Width { get; internal set; }
        public object Height { get; internal set; }
        public long Size { get; internal set; }

        public TabData(TabItem tabItem, string tabPath)
        {
            this.tabItem = tabItem;
            InitialImagePath = tabPath;
            ((Button)((StackPanel)tabItem.Header).Children[1]).Click += TabData_Click;
            (((StackPanel)tabItem.Header)).MouseDown += TabData_MouseDown;
        }

        void TabData_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                CloseTabAction?.Invoke(this);
            }
        }

        public TabData(TabItem tabItem, string tabPath, int currentIndex) : this(tabItem, tabPath)
        {
            Index = currentIndex;
        }

        void TabData_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CloseTabAction?.Invoke(this);
        }

        public void UpdateTitle()
        {
            Title = Filename;
        }
    }
}
