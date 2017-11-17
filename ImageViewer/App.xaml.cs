using System.Windows;

namespace Frame
{
    public partial class App
    {
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
