using System;
using System.IO;
using System.Windows.Controls;
using Cyotek.Windows.Forms;

namespace Frame
{
  public class TabControlManager
  {
    readonly TabControl    tabControl;
    readonly MainWindow    mainWindow;
    readonly ImageViewerWm imageViewerWm;

    public TabControlManager(TabControl tabControl, ImageViewerWm imageViewerWm, MainWindow mainWindow)
    {
      this.mainWindow = mainWindow;
      this.imageViewerWm = imageViewerWm;
      this.tabControl    = tabControl;
    }

    public int CurrentTabIndex => tabControl.SelectedIndex;

    public int TabCount => tabControl.Items.Count;

    public TabItemControl CurrentTab
    {
      get
      {
        if (tabControl.Items.IsEmpty)
        {
          return null;
        }

        return tabControl.SelectedItem as TabItemControl;
      }
    }

    public bool CanExcectute()
    {
      if (tabControl.SelectedIndex < 0)
      {
        return false;
      }

      if (tabControl.Items.IsEmpty)
      {
        return false;
      }

      return tabControl.SelectedIndex != -1 && ((TabItemControl) tabControl.SelectedItem).IsValid;
    }

    TabItemControl CreateTabData(string path)
    {
      return new TabItemControl(mainWindow)
      {
        InitialImagePath = path
      };
    }

    public void AddTab(string filepath)
    {
      var item = CreateTabData(Path.GetDirectoryName(filepath));

      tabControl.Items.Add(item);

      if (tabControl.SelectedIndex == -1)
      {
        tabControl.SelectedIndex = 0;
      }
      else
      {
        tabControl.SelectedIndex = tabControl.Items.Count - 1;
      }
    }

    public void CloseSelectedTab()
    {
      if (!CanExcectute())
      {
        return;
      }
      
      ((TabItemControl)tabControl.SelectedItem).Dispose();
      tabControl.Items.RemoveAt(tabControl.SelectedIndex);
      GC.Collect();
    }
  }
}