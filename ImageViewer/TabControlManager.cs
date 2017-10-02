using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Optional;
using Optional.Unsafe;
using Cyotek.Windows.Forms;

namespace Frame
{
    class TabControlManager
    {
        readonly TabControl tabControl;
        readonly ImageViewerWM ImageViewerWM;
        readonly ImageBox imageBox;

        public TabControlManager(TabControl tabControl, ImageViewerWM imageViewerWM, ImageBox imageBox)
        {
            this.imageBox = imageBox;
            ImageViewerWM = imageViewerWM;
            this.tabControl = tabControl;
        }

        public void AddTab(string filepath)
        {
            var folderPath = Path.GetDirectoryName(filepath);
            TabData item = new TabData(folderPath) { CloseTabAction = CloseTab };
            ImageViewerWM.Tabs.Add(item);

            tabControl.Items.Add(item.tabItem);

            if (ImageViewerWM.CurrentTabIndex == -1)
            {
                ImageViewerWM.CurrentTabIndex = 0;
                tabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex;
            }
            else
            {
                tabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;
            }
        }

        public void CloseTab(TabData data)
        {
            var currently_selected_item = tabControl.SelectedItem;
            var currently_selected_index = tabControl.SelectedIndex;
            int newIndex = tabControl.Items.IndexOf(data.tabItem);
            if (newIndex < 0)
            {
                CloseSelectedTab();
            }
            else
            {
                ImageViewerWM.CurrentTabIndex = newIndex;
                tabControl.SelectedIndex = newIndex;
                CloseSelectedTab();
                if (currently_selected_index != newIndex)
                {
                    tabControl.SelectedItem = currently_selected_item;
                }
            }
        }

        public void CloseSelectedTab()
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (tabControl.SelectedIndex == 0)
            {
                imageBox.Image = null;
                GC.Collect();
            }

            ImageViewerWM.Tabs.RemoveAt(tabControl.SelectedIndex);
            tabControl.Items.RemoveAt(tabControl.SelectedIndex);
        }
    }
}
