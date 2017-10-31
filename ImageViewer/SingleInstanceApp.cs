using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.VisualBasic.ApplicationServices;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace Frame
{
    public class SingleInstanceApp : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        public void Poke()
        {
            MainWindow?.Activate();
        }
    }

    public class SingleAppMangager : WindowsFormsApplicationBase
    {
        SingleInstanceApp app;
        ReadOnlyCollection<string> args;

        public SingleAppMangager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
        {
            args = eventArgs.CommandLine;
            app = new SingleInstanceApp();
            app.Run();
            var mainWindow = (MainWindow)app.MainWindow;

            foreach (var arg in args)
            {
                mainWindow?.AddNewTab(arg);
            }
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
            app.Poke();
        }
    }
}