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
  }
}