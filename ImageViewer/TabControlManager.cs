using System.Collections.Generic;
using System.Windows;
using Dragablz;
using Dragablz.Dockablz;

namespace Frame
{
  public class TabControlManager
  {
    TabablzControl currentTabControl;

    public List<TabablzControl> TabControls
    {
      get
      {
        var tabablzControls = new List<TabablzControl>();
        var currentMainWindow = CurrentMainWindow();
        if (currentMainWindow == null)
        {
          tabablzControls.Add(currentTabControl);
          return tabablzControls;
        }

        switch (currentMainWindow.DockLayout.Content)
        {
          case Branch children:
          {
            tabablzControls.AddRange(GetTabablzControls(children));
            break;
          }
          case TabablzControl tabablzControl:
          {
            tabablzControls.Add(tabablzControl);
            break;
          }
        }

        return tabablzControls;
      }
    }

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

                if (itemTabItemControl.ImageAreaScrollViewwer.IsFocused)
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
      private set => currentTabControl = value;
    }

    public TabControlManager(TabablzControl tabControl)
    {
      CurrentTabControl = tabControl;
    }

    public int CurrentTabIndex => CurrentTabControl.SelectedIndex;

    public TabItemControl CurrentTab
    {
      get
      {
        var currentMainWindow = CurrentMainWindow();
        var tabControl        = CurrentTabControl;
        if (currentMainWindow == null || tabControl.Items.IsEmpty)
        {
          return null;
        }

        var tabItemControl = tabControl.SelectedItem as TabItemControl;
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
      var tabControl = CurrentTabControl;
      if (tabControl.SelectedIndex < 0
          || tabControl.Items.IsEmpty)
      {
        return false;
      }

      return ((TabItemControl) tabControl.SelectedItem).IsValid;
    }

    public TabItemControl AddTab(string filepath)
    {
      var item = new TabItemControl()
      {
        InitialImagePath = filepath
      };
      var tabControl = CurrentTabControl;
      tabControl.AddToSource(item);

      if (tabControl.SelectedIndex == -1)
      {
        tabControl.SelectedIndex = 0;
      }
      else
      {
        tabControl.SelectedIndex = tabControl.Items.Count - 1;
      }

      return item;
    }

    public static TabItemControl GetTab(string filepath)
    {
      return new TabItemControl()
      {
        InitialImagePath = filepath
      };
    }

    public void CloseSelectedTab()
    {
      if (!CanExcectute())
      {
        return;
      }

      CurrentTab.Dispose();
      TabablzControl.CloseItem(CurrentTab);
    }
  }
}