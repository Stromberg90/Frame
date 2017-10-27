using Cyotek.Windows.Forms;
using System;
using System.IO;
using System.Windows.Controls;

namespace Frame
{
    class TabControlManager
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
            var item = new TabData(Path.GetDirectoryName(filepath)) {CloseTabAction = CloseTab};
            imageViewerWm.Tabs.Add(item);

            tabControl.Items.Add(item.tabItem);

            if (imageViewerWm.CurrentTabIndex == -1)
            {
                imageViewerWm.CurrentTabIndex = 0;
                tabControl.SelectedIndex = imageViewerWm.CurrentTabIndex;
            }
            else
            {
                tabControl.SelectedIndex = imageViewerWm.CurrentTabIndex + 1;
            }
        }

        public void CloseTab(TabData data)
        {
            var currentlySelectedItem = tabControl.SelectedItem;
            var currentlySelectedIndex = tabControl.SelectedIndex;
            var newIndex = tabControl.Items.IndexOf(data.tabItem);
            if (newIndex < 0)
            {
                CloseSelectedTab();
            }
            else
            {
                imageViewerWm.CurrentTabIndex = newIndex;
                tabControl.SelectedIndex = newIndex;
                CloseSelectedTab();
                if (currentlySelectedIndex != newIndex)
                {
                    tabControl.SelectedItem = currentlySelectedItem;
                }
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
                imageBox.Image = null;
                GC.Collect();
            }

            imageViewerWm.Tabs.RemoveAt(tabControl.SelectedIndex);
            tabControl.Items.RemoveAt(tabControl.SelectedIndex);
        }
    }
}