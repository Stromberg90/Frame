using System.Windows.Controls;
using System.Windows.Media;

// TODO Save pan/scale settings per image.
namespace ImageViewer
{
    class TabData
    {
        public TranslateTransform Pan { get; set; }
        public ScaleTransform Scale { get; set; }
        public TabItem tabItem;
        public ImageSet images;
        public ImageSet last_images;
        public string InitialImagePath { get; set; }
        public int Index { get; set; }
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
        public ImageSettings imageSettings = new ImageSettings();
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
    }
}
