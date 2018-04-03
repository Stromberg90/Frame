using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.VisualBasic.ApplicationServices;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace Frame
{
  public class SingleInstanceApp : Application
  {
    public readonly List<string> Args = new List<string>();
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      MainWindow = new MainWindow();
      foreach (var arg in Args)
      {
        ((MainWindow)MainWindow).AddNewTab(arg);
      }
      MainWindow.Show();
    }

    public void Poke()
    {
      MainWindow?.Activate();
    }
  }

  public class SingleAppMangager : WindowsFormsApplicationBase
  {
    SingleInstanceApp          app;
    ReadOnlyCollection<string> args;

    public SingleAppMangager()
    {
      IsSingleInstance = true;
    }

    protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
    {
      args = eventArgs.CommandLine;
      app  = new SingleInstanceApp();
      foreach (var arg in args)
      {
        app.Args.Add(arg);
      }

      app.Run();
      return false;
    }

    protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
    {
      base.OnStartupNextInstance(eventArgs);
      args = eventArgs.CommandLine;
      var mainWindow = (MainWindow) app.MainWindow;

      foreach (var arg in args)
      {
        mainWindow?.AddNewTab(arg);
      }

      if (app.MainWindow != null && app.MainWindow.WindowState == WindowState.Minimized)
      {
        app.MainWindow.WindowState = WindowState.Normal;
      }
      app.Poke();
    }
  }
}