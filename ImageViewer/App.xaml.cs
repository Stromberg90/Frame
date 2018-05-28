using System.Collections.Generic;
using System.Windows;

namespace Frame
{
  public partial class App
  {
    public static readonly About         AboutDialog   = new About();
    public static readonly OptionsWindow OptionsDialog = new OptionsWindow();

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);
      var unused = new MainWindow();
    }

    public void Poke()
    {
      MainWindow?.Activate();
    }

    public static List<Window> GetMainWindows()
    {
      var mainWindows = new List<Window>(Current.Windows.Count);
      foreach (Window currentWindow in Current.Windows)
      {
        if (currentWindow.GetType() == typeof(MainWindow))
        {
          mainWindows.Add(currentWindow);
        }
      }

      return mainWindows;
    }
  }
}