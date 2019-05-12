using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.VisualBasic.ApplicationServices;
using StartupEventArgs = System.Windows.StartupEventArgs;

namespace Frame
{
    public class SingleInstanceApp : Application
    {
        internal readonly List<string> Args = new List<string>();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = new MainWindow();
            MainWindow.Show();
            //Add tabs after showiung the window
        }

        public void poke()
        {
            MainWindow?.Activate();
        }
    }

    public class SingleAppMangager : WindowsFormsApplicationBase
    {
        SingleInstanceApp app;

        public SingleAppMangager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs eventArgs)
        {
            app = new SingleInstanceApp();
            foreach (var arg in eventArgs.CommandLine)
            {
                app.Args.Add(arg);
            }

            app.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            base.OnStartupNextInstance(eventArgs);

            if (app.MainWindow != null && app.MainWindow.WindowState == WindowState.Minimized)
            {
                app.MainWindow.WindowState = WindowState.Normal;
            }
            app.poke();
        }
    }
}