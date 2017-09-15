using System.Windows.Controls;
using System.Windows.Media;

namespace Frame
{
    class TabData
    {
        public System.Windows.Point Pan { get; set; } = new System.Windows.Point(0.0, 0.0);
        public double Scale { get; set; } = 1.0;
        public TabItem tabItem;
        public ImageSet images;
        public ImageSet last_images;
        public ImageSettings imageSettings = new ImageSettings();
        // INotifyPropertyChanged so I can update the header without having to call UpdateTitle() explicitly.
        public bool InSlideshowMode { get; set; }
        public bool InToggleMode { get; set; }
        public int CurrentSlideshowTime { get; set; }
        public string InitialImagePath { get; set; }
        public int Index { get; set; }
        public string Path { get { return images.Paths[Index]; } }
        public string Title {
            set
            {
                tabItem.Header = value;
            }
            get
            {
                return tabItem.Header.ToString();
            }
        }

        public string Filename
        {
            get
            {
                return new System.IO.FileInfo(images.Paths[Index]).Name;
            }
        }

        public TabData(TabItem tabItem, string tabPath)
        {
            this.tabItem = tabItem;
            InitialImagePath = tabPath;
        }

        public TabData(TabItem tabItem, string tabPath, int currentIndex)
        {
            this.tabItem = tabItem;
            InitialImagePath = tabPath;
            Index = currentIndex;
        }

        public void UpdateTitle()
        {
            if (InToggleMode)
            {
                Title = $" [Toggle] {Filename}";
            }
            else
            {
                Title = Filename;
            }

            if (InSlideshowMode)
            {
                Title += $" [Slideshow] {CurrentSlideshowTime}";
            }
        }
    }
}
