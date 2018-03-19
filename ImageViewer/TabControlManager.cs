using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Dragablz;
using Dragablz.Dockablz;

namespace Frame
{
  public class TabControlManager
  {
    readonly TabablzControl currentTabControl;

    public TabablzControl CurrentTabControl
    {
      get
      {
        var currentMainWindow = CurrentMainWindow();
        if (currentMainWindow == null)
        {
          return currentTabControl;
        }

        var tabItemControl = currentMainWindow.ImageTabControl;

        switch (currentMainWindow.DockLayout.Content)
        {
          case Branch children:
          {
            var controls = GetTabablzControls(children);
            foreach (var control in controls)
            {
              foreach (var controlItem in control.Items)
              {
                if (!(controlItem is TabItemControl itemTabItemControl))
                {
                  continue;
                }

                if (itemTabItemControl.WinFormsHost.IsFocused)
                {
                  tabItemControl = control;
                }
              }
            }

            break;
          }
          case TabablzControl tabablzControl:
          {
            tabItemControl = tabablzControl;
            break;
          }
        }

        currentMainWindow.ImageTabControl = tabItemControl;
        return tabItemControl;
      }
    }

    public TabControlManager(TabablzControl tabControl)
    {
      currentTabControl = tabControl;
    }

    public int CurrentTabIndex => CurrentTabControl.SelectedIndex;

    public int TabCount => CurrentTabControl.Items.Count;

    public TabItemControl CurrentTab
    {
      get
      {
        if (CurrentMainWindow() == null || CurrentTabControl.Items.IsEmpty)
        {
          return null;
        }

        var tabItemControl = CurrentTabControl.SelectedItem as TabItemControl;

        if (!(CurrentMainWindow().DockLayout.Content is Branch children))
        {
          return tabItemControl;
        }

        var controls = GetTabablzControls(children);
        foreach (var control in controls)
        {
          foreach (var controlItem in control.Items)
          {
            if (!(controlItem is TabItemControl itemTabItemControl))
            {
              continue;
            }

            if (itemTabItemControl.WinFormsHost.IsFocused)
            {
              tabItemControl = itemTabItemControl;
            }
          }
        }

        return tabItemControl;
      }
    }

    static MainWindow CurrentMainWindow()
    {
      var result = Application.Current.MainWindow as MainWindow;
      foreach (var window in Application.Current.Windows)
      {
        if (window.GetType() != typeof(MainWindow))
        {
          continue;
        }

        if (((MainWindow) window).IsActive)
        {
          result = window as MainWindow;
        }
      }

      return result;
    }

    static List<TabablzControl> GetTabablzControls(Branch branch)
    {
      var controls = new List<TabablzControl>();

      switch (branch.FirstItem)
      {
        case TabablzControl _:
        {
          controls.Add((TabablzControl) branch.FirstItem);
          break;
        }
        case Branch _:
        {
          controls.AddRange(GetTabablzControls((Branch) branch.FirstItem));
          break;
        }
      }

      switch (branch.SecondItem)
      {
        case TabablzControl secondItem:
        {
          controls.Add(secondItem);
          break;
        }
        case Branch _:
        {
          controls.AddRange(GetTabablzControls((Branch) branch.SecondItem));
          break;
        }
      }

      return controls;
    }

    public bool CanExcectute()
    {
      if (CurrentTabControl.SelectedIndex < 0
          || CurrentTabControl.Items.IsEmpty)
      {
        return false;
      }

      return CurrentTabControl.SelectedIndex != -1 && ((TabItemControl) CurrentTabControl.SelectedItem).IsValid;
    }

    static TabItemControl CreateTabData(string path)
    {
      return new TabItemControl(CurrentMainWindow())
      {
        InitialImagePath = path
      };
    }

    public void AddTab(string filepath)
    {
      var item = CreateTabData(Path.GetDirectoryName(filepath));
      CurrentTabControl.AddToSource(item);

      if (CurrentTabControl.SelectedIndex == -1)
      {
        CurrentTabControl.SelectedIndex = 0;
      }
      else
      {
        CurrentTabControl.SelectedIndex = TabCount - 1;
      }
    }

    public static TabItemControl GetTab(string filepath)
    {
      return CreateTabData(Path.GetDirectoryName(filepath));
    }

    public void CloseSelectedTab()
    {
      if (!CanExcectute())
      {
        return;
      }

      TabablzControl.CloseItem(CurrentTab);

      GC.Collect();
    }
  }
}