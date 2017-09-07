using System.Windows.Controls;

// TODO Maybe have a list of paths per tab?
// TODO Save pan/scale settings per image.
namespace ImageViewer
{
    class TabData
    {
        public TabItem tabItem;
        public ImageSet images;
        public ImageSet last_images;
        public string initialImagePath;
        public int currentIndex;
        public ImageSettings imageSettings = new ImageSettings();
        public TabData(TabItem tabItem, string tabPath)
        {
            this.tabItem = tabItem;
            initialImagePath = tabPath;
        }
        public TabData(TabItem tabItem, string tabPath, int currentIndex)
        {
            this.tabItem = tabItem;
            initialImagePath = tabPath;
            this.currentIndex = currentIndex;
        }

        public TabData DeepCopy()
        {
            TabData other = (TabData)MemberwiseClone();
            other.currentIndex = currentIndex;
            other.images.paths = images.paths;
            other.imageSettings.Current_sort_mode = imageSettings.Current_sort_mode;
            other.imageSettings.displayChannel= imageSettings.displayChannel;
            return other;

        }
    }
}
