using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Cyotek.Windows.Forms;
using Dragablz;
using Dragablz.Dockablz;

namespace Frame
{
  public class TabControlManager
  {
    TabablzControl tabControl;

    public TabablzControl TabControl
    {
      get
      {
        var currentMainWindow = CurrentMainWindow();
        if (currentMainWindow == null)
        {
          return tabControl;
        }

        var tabItemControl = currentMainWindow.ImageTabControl;

        if (currentMainWindow.DockLayout.Content is Branch children)
        {
          var controls = getTabablzControls(children);
          foreach (var control in controls)
          {
            foreach (var controlItem in control.Items)
            {
              if (controlItem is TabItemControl itemTabItemControl)
              {
                if (itemTabItemControl.WinFormsHost.IsFocused)
                {
                  tabItemControl = control;
                }
              }
            }
          }
        }

        return tabItemControl;
      }
      set
      {
        if (tabControl == null)
        {
          tabControl = value;
        }
      }
    }

    readonly MainWindow    mainWindow;
    readonly ImageViewerWm imageViewerWm;

    public TabControlManager(TabablzControl tabControl, ImageViewerWm imageViewerWm, MainWindow mainWindow)
    {
      this.mainWindow    = mainWindow;
      this.imageViewerWm = imageViewerWm;
      this.tabControl    = tabControl;
    }

    public int CurrentTabIndex => TabControl.SelectedIndex;

    public int TabCount => TabControl.Items.Count;

    public TabItemControl CurrentTab
    {
      get
      {
        if (CurrentMainWindow() == null)
        {
          return null;
        }

        if (TabControl.Items.IsEmpty)
        {
          return null;
        }

        var tabItemControl = TabControl.SelectedItem as TabItemControl;

        if (CurrentMainWindow().DockLayout.Content is Branch children)
        {
          var controls = getTabablzControls(children);
          foreach (var control in controls)
          {
            foreach (var controlItem in control.Items)
            {
              if (controlItem is TabItemControl itemTabItemControl)
              {
                if (itemTabItemControl.WinFormsHost.IsFocused)
                {
                  tabItemControl = itemTabItemControl;
                }
              }
            }
          }
        }

        return tabItemControl;
      }
    }

    public MainWindow CurrentMainWindow()
    {
      var result = Application.Current.MainWindow as MainWindow;
      foreach (var window in Application.Current.Windows)
      {
        if (window.GetType() == typeof(MainWindow))
        {
          if (((MainWindow) window).IsActive)
          {
            result = window as MainWindow;
          }
        }
      }

      return result;
    }

    List<TabablzControl> getTabablzControls(Branch branch)
    {
      var controls = new List<TabablzControl>();

      if (branch.FirstItem is TabablzControl firstItem)
      {
        controls.Add(firstItem);
      }
      else if (branch.FirstItem is Branch children)
      {
        controls.AddRange(getTabablzControls(children));
      }

      if (branch.SecondItem is TabablzControl secondItem)
      {
        controls.Add(secondItem);
      }
      else if (branch.SecondItem is Branch children)
      {
        controls.AddRange(getTabablzControls(children));
      }

      return controls;
    }

    public bool CanExcectute()
    {
      if (TabControl.SelectedIndex < 0)
      {
        return false;
      }

      if (TabControl.Items.IsEmpty)
      {
        return false;
      }

      return TabControl.SelectedIndex != -1 && ((TabItemControl) TabControl.SelectedItem).IsValid;
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
      TabControl.Items.Add(item);

      if (TabControl.SelectedIndex == -1)
      {
        TabControl.SelectedIndex = 0;
      }
      else
      {
        TabControl.SelectedIndex = TabControl.Items.Count - 1;
      }

    }

    public TabItemControl GetTab(string filepath)
    {
      return CreateTabData(Path.GetDirectoryName(filepath));
    }

    public void CloseSelectedTab()
    {
      //BUG This be messed up.
      if (!CanExcectute())
      {
        return;
      }

      var preDeleteCount = TabControl.Items.Count;
      ((TabItemControl) TabControl.SelectedItem).Dispose();
      TabControl.Items.RemoveAt(TabControl.SelectedIndex);
      if (preDeleteCount - 1 == 0)
      {
        if (CurrentMainWindow().DockLayout.Content is Branch branch)
        {
          var foundControl    = CurrentMainWindow().DockLayout.Content;
          var tabablzControls = getTabablzControls(branch);
          foreach (var control in tabablzControls)
          {
            if (!control.IsEmpty)
            {
              foundControl = control;
            }
          }

          if (foundControl is TabablzControl)
          {
            CurrentMainWindow().DockLayout.Content = (TabablzControl) foundControl;
          }
          else if (foundControl is Branch)
          {
            CurrentMainWindow().DockLayout.Content = (Branch) foundControl;
          }
        }

        //TabControl.InterTabController.
      }

      GC.Collect();
    }
  }
}