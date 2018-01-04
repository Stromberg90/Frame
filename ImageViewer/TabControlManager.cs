using Cyotek.Windows.Forms;
using System;
using System.IO;
using System.Windows.Controls;

namespace Frame
{
    public class TabControlManager
    {
        readonly TabControl tabControl;
        readonly ImageViewerWm imageViewerWm;
        readonly ImageBox imageBox;

        public TabControlManager(TabControl tabControl, ImageViewerWm imageViewerWm, ImageBox imageBox)
        {
            this.imageBox = imageBox;
            this.imageViewerWm = imageViewerWm;
            this.tabControl = tabControl;
        }

        public void AddTab(string filepath)
        {
            var item = TabData.CreateTabData(Path.GetDirectoryName(filepath));
            imageViewerWm.Tabs.Add(item);

            tabControl.Items.Add(item.tabItem);

            if (imageViewerWm.CurrentTabIndex == -1)
            {
                imageViewerWm.CurrentTabIndex = 0;
                tabControl.SelectedIndex = imageViewerWm.CurrentTabIndex;
            }
            else
            {
                tabControl.SelectedIndex = tabControl.Items.Count - 1;
            }
        }

        public void CloseSelectedTab()
        {
            if (!imageViewerWm.CanExcectute())
            {
                return;
            }

            if (tabControl.SelectedIndex == 0)
            {
                imageBox.Image.Dispose();
                imageBox.Image = null;
            }

            imageViewerWm.Tabs[tabControl.SelectedIndex].Dispose();
            imageViewerWm.Tabs.RemoveAt(tabControl.SelectedIndex);
            tabControl.Items.RemoveAt(tabControl.SelectedIndex);
            GC.Collect();
        }
    }
}