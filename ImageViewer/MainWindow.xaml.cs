using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Dragablz;
using Frame.Properties;
using ImageMagick;
using Dragablz.Dockablz;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Frame {
    public static class StaticMethods {
        static public List<TabablzControl> TabControls {
            get {
                var tabablzControls = new List<TabablzControl>();
                var currentMainWindow = StaticMethods.currentMainWindow();
                if(currentMainWindow == null) {
                    tabablzControls.Add(CurrentTabControl);
                    return tabablzControls;
                }

                switch(currentMainWindow.DockLayout.Content) {
                    case Branch children: {
                        tabablzControls.AddRange(getTabablzControls(children));
                        break;
                    }
                    case TabablzControl tabablzControl: {
                        tabablzControls.Add(tabablzControl);
                        break;
                    }
                }

                return tabablzControls;
            }
        }

        static public TabablzControl CurrentTabControl {
            get {
                var currentMainWindow = StaticMethods.currentMainWindow();

                var tabItemControl = currentMainWindow.ImageTabControl;

                switch(currentMainWindow.DockLayout.Content) {
                    case Branch children: {
                        foreach(var control in getTabablzControls(children)) {
                            foreach(var controlItem in control.Items) {
                                if(!(controlItem is TabItemControl itemTabItemControl)) {
                                    continue;
                                }

                                if(itemTabItemControl.ImagePresenter.ScrollViewer.IsFocused) {
                                    tabItemControl = control;
                                }
                            }
                        }

                        break;
                    }
                    case TabablzControl tabablzControl: {
                        tabItemControl = tabablzControl;
                        break;
                    }
                }

                currentMainWindow.ImageTabControl = tabItemControl;
                return tabItemControl;
            }
        }

        static public int CurrentTabIndex => CurrentTabControl.SelectedIndex;

        // Optional<TabItemControl> maybe?
        static public TabItemControl CurrentTab {
            get {
                var currentMainWindow = StaticMethods.currentMainWindow();
                var tabControl = CurrentTabControl;
                if(currentMainWindow == null || tabControl.Items.IsEmpty) {
                    return null;
                }

                return tabControl.SelectedItem as TabItemControl;
            }
        }

        static MainWindow currentMainWindow() {
            var result = System.Windows.Application.Current.MainWindow as MainWindow;
            foreach(var window in System.Windows.Application.Current.Windows) {
                if(window.GetType() != typeof(MainWindow)) {
                    continue;
                }

                if(((MainWindow)window).IsActive) {
                    result = window as MainWindow;
                }
            }
            return result;
        }

        static List<TabablzControl> getTabablzControls(Branch branch) {
            var controls = new List<TabablzControl>();

            switch(branch.FirstItem) {
                case TabablzControl _: {
                    controls.Add((TabablzControl)branch.FirstItem);
                    break;
                }
                case Branch _: {
                    controls.AddRange(getTabablzControls((Branch)branch.FirstItem));
                    break;
                }
            }

            switch(branch.SecondItem) {
                case TabablzControl secondItem: {
                    controls.Add(secondItem);
                    break;
                }
                case Branch _: {
                    controls.AddRange(getTabablzControls((Branch)branch.SecondItem));
                    break;
                }
            }

            return controls;
        }

        public static void addTab(TabablzControl tabablzControl, string filepath) {
            var folderpath = Path.GetDirectoryName(filepath);
            var ext = Path.GetExtension(filepath);

            TabItemControl new_tab = null;
            if(ext == ".gif") {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    new_tab = new TabItemControl {
                        Header = Path.GetFileName(filepath),
                    };
                    new_tab.ImagePresenter.ImageArea.loadAimatedGif(filepath);
                }, DispatcherPriority.Normal);
            }
            else {
                using(var image = new MagickImage(filepath)) {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        new_tab = new TabItemControl {
                            Header = Path.GetFileName(filepath),
                        };
                        new_tab.ImagePresenter.ImageArea.Source = image.ToBitmapSource();
                    }, DispatcherPriority.Normal);
                }
            }

            if(new_tab == null) {
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                tabablzControl.AddToSource(new_tab);

                tabablzControl.SelectedIndex = tabablzControl.Items.Count - 1;
                tabablzControl.UpdateLayout();

                if(tabablzControl.SelectedItem is TabItemControl) {
                    var image_presenter = (tabablzControl.SelectedItem as TabItemControl).ImagePresenter;
                    var image_height = image_presenter.ImageArea.ActualHeight;
                    var grid_height = image_presenter.ScrollViewer.ActualHeight;
                    var image_width = image_presenter.ImageArea.ActualWidth;
                    var grid_width = image_presenter.ScrollViewer.ActualWidth;
                    if(image_height > grid_height || image_width > grid_width) {
                        var zoom_by_height = (grid_height / image_height) * 100;
                        var zoom_by_width = (grid_width / image_width) * 100;
                        if(zoom_by_height < zoom_by_width) {
                            image_presenter.Zoom = zoom_by_height;
                        }
                        else {
                            image_presenter.Zoom = zoom_by_width;
                        }
                    }

                    //tabablzControl.AfterSelectionChanged += new_tab.selectionChanged;
                }
            }, DispatcherPriority.Normal);

            var supportedExtensions = Settings.Default.SupportedExtensions;
            foreach(var file in Directory.GetFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly)) {
                var trimmed_ext = Path.GetExtension(file).Remove(0, 1);
                if(supportedExtensions.Contains(trimmed_ext)
                  || supportedExtensions.Contains(trimmed_ext.ToLower()) ||
                  supportedExtensions.Contains(trimmed_ext.ToUpper())) {
                    new_tab.Paths.Add(file);
                }
            }
            new_tab.Index = new_tab.Paths.IndexOf(filepath);
        }
    }

    public partial class MainWindow {
        public static readonly string FilterString = constructFilterString();

        static string constructFilterString() {
            var newFilterString = new StringBuilder();
            newFilterString.Append("Image files (");

            for(var i = 0; i < Settings.Default.SupportedExtensions.Count; i++) {
                var file_extension = "*." + Settings.Default.SupportedExtensions[i];
                if(i < Settings.Default.SupportedExtensions.Count) {
                    newFilterString.Append(file_extension).Append(", ");
                }
                else {
                    newFilterString.Append(file_extension).Append(")");
                }
            }
            newFilterString.Append(" | ");
            for(var i = 0; i < Settings.Default.SupportedExtensions.Count; i++) {
                var file_extension = "*." + Settings.Default.SupportedExtensions[i];
                if(i < Settings.Default.SupportedExtensions.Count) {
                    newFilterString.Append(file_extension).Append("; ");
                }
                else {
                    newFilterString.Append(file_extension);
                }
            }
            return newFilterString.ToString();
        }

        public MainWindow() {
            AutoUpdater.ShowSkipButton = false;

            InitializeComponent();

            Loaded += windowLoaded;
            Closing += windowClosing;

            KeyDown += keyDown;
            PreviewMouseDoubleClick += previewMouseDoubleClick;

            //ImageTabControl.AfterSelectionChanged += imageTabControlAfterSelectionChanged;
        }

        private void imageTabControlAfterSelectionChanged(object source, System.Windows.Controls.SelectionChangedEventArgs e) {
            Debug.WriteLine("Selection changed");
            if(e.AddedItems.Count > 0) {
                var tab_item = (TabItemControl)e.AddedItems[0];
                if(tab_item != null) {
                    tab_item.ImagePresenter.Focus();
                    Keyboard.Focus(tab_item.ImagePresenter);
                    e.Handled = true;
                }
            }
        }

        void previewMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if(ImageTabControl.HasItems) {
                e.Handled = false;
                return;
            }
            using(var file_dialog = new OpenFileDialog { Multiselect = true, AddExtension = true, Filter = FilterString }) {
                file_dialog.ShowDialog();
                if(file_dialog.FileNames.Length > 0) {
                    ThreadStart add_tabs_thread = () => {
                        foreach(var filepath in file_dialog.FileNames) {
                            StaticMethods.addTab(ImageTabControl, filepath);
                        }
                    };
                    new Thread(add_tabs_thread).Start();
                }
            }
        }

        void keyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            Console.WriteLine(StaticMethods.CurrentTab.Header);
            //StaticMethods.CurrentTab.Focus();
            if(e.Key == Key.Tab) {
                e.Handled = true;
                return;
            }
            if(e.Key == Key.O) {
                var result = System.Windows.MessageBox.Show("Do a full GC Collect?", "GC Collect", MessageBoxButton.YesNo);
                if(result == MessageBoxResult.Yes) {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                e.Handled = true;
                return;
            }

            //var tab_item = StaticMethods.CurrentTab;
            //if(tab_item != null) {
            //    tab_item.ImagePresenter.Focus();
            //   Keyboard.Focus(tab_item.ImagePresenter);
            //}
            e.Handled = false;
        }

        void windowLoaded(object sender, RoutedEventArgs e) {
            Left = Settings.Default.WindowLocation.X;
            Top = Settings.Default.WindowLocation.Y;

            Width = Settings.Default.WindowSize.Width;
            Height = Settings.Default.WindowSize.Height;

            WindowState = (WindowState)Settings.Default.WindowState;

            e.Handled = true;
        }

        void windowClosing(object sender, CancelEventArgs e) {
            Loaded -= windowLoaded;
            Closing -= windowClosing;
            KeyDown -= keyDown;
            PreviewMouseDoubleClick -= previewMouseDoubleClick;
            //ImageTabControl.AfterSelectionChanged -= imageTabControlAfterSelectionChanged;

            Settings.Default.WindowLocation = new Point((int)Left, (int)Top);
            Settings.Default.WindowState = (int)WindowState;
            var newSize = new Size();

            if(WindowState == WindowState.Normal) {
                newSize.Width = (int)Width;
                newSize.Height = (int)Height;
            }
            else {
                newSize.Width = (int)RestoreBounds.Width;
                newSize.Height = (int)RestoreBounds.Height;
            }
            Settings.Default.WindowSize = newSize;

            Settings.Default.Save();
        }

        void aboutClick(object sender, RoutedEventArgs e) {
            if(WindowState == WindowState.Maximized) {
                var rect = Screen.GetWorkingArea(new Point((int)Left, (int)Top));
                App.AboutDialog.Top = rect.Top + (ActualHeight / 2.0) - (App.AboutDialog.Height / 2.0);
                App.AboutDialog.Left = rect.Left + (ActualWidth / 2.0) - (App.AboutDialog.Width / 2.0);
            }
            else {
                App.AboutDialog.Top = Top + (ActualHeight / 2.0) - (App.AboutDialog.Height / 2.0);
                App.AboutDialog.Left = Left + (ActualWidth / 2.0) - (App.AboutDialog.Width / 2.0);
            }

            App.AboutDialog.ShowDialog();
        }

        void checkForUpdateOnClick(object sender, RoutedEventArgs e) {
            AutoUpdater.Start("http://www.dropbox.com/s/2b0gna7rz889b5u/Update.xml?dl=1");
        }
    }
}