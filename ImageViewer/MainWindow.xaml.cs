//CHANGLOG 1.6.5

//Bugs fixed.
//Pasting 1 image did not work

//Features
//Switched to OpenMP version of Magick.NET, image loading is now much faster.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Dragablz;
using Frame.Annotations;
using Frame.Properties;
using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using static System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

#nullable enable

namespace Frame {
    public interface ICommand {
        void Execute();
    }

    public partial class MainWindow : IDisposable {
        Visibility footerVisibility;
        static DispatcherTimer SlideshowTimer;

        readonly FilesManager filesManager;
        readonly SortingManager sortingManager;
        readonly TabControlManager tabControlManager;

        FileSystemWatcher ImageDirectoryWatcher;
        FileSystemWatcher ParentDirectoryWatcher;
        bool IsChangingSize = true;
        readonly Dictionary<CommandKeys, ICommand> commands;
        string DirectoryName;

        public class ToggleDisplayChannelCommand : ICommand {
            readonly CommandFunction func;
            readonly ValidateFunction validateFunction;
            readonly Channels Channel;

            public delegate void CommandFunction(Channels channel);

            public delegate bool ValidateFunction();

            public ToggleDisplayChannelCommand(CommandFunction func, Channels channel,
                                               ValidateFunction validateFunction = null) {
                this.func = func;
                this.Channel = channel;
            }

            public void Execute() {
                if (validateFunction != null && !validateFunction.Invoke()) {
                    return;
                }

                if (!ModifierKeyDown()) {
                    func(Channel);
                }
            }
        }

        public class Command : ICommand {
            readonly CommandFunction func;
            readonly ValidateFunction? validateFunction;

            public delegate void CommandFunction();

            public delegate bool ValidateFunction();

            public Command(CommandFunction func, ValidateFunction validateFunction = null) {
                this.func = func;
                this.validateFunction = validateFunction;
            }

            public void Execute() {
                if (validateFunction != null && !validateFunction.Invoke()) {
                    return;
                }

                func();
            }
        }

        struct CommandKeys {
            [UsedImplicitly] readonly Key key;
            [UsedImplicitly] readonly bool leftShift;
            [UsedImplicitly] readonly bool leftCtrl;

            public CommandKeys(Key key, params Key[] keys) {
                this.key = key;
                leftShift = false;
                leftCtrl = false;
                foreach (var key1 in keys) {
                    switch (key1) {
                        case Key.LeftShift: {
                            leftShift = true;
                            break;
                        }
                        case Key.LeftCtrl: {
                            leftCtrl = true;
                            break;
                        }
                    }
                }
            }

            public CommandKeys(Key key, bool leftShift, bool leftCtrl) {
                this.key = key;
                this.leftShift = leftShift;
                this.leftCtrl = leftCtrl;
            }
        }

        internal void LoadImage() {
            var CurrentTab = tabControlManager.CurrentTab;
            if (CurrentTab == null) return;
            if (CurrentTab.Paths.Count == 0) return;

            CurrentTab.LoadImage();
        }

        public MainWindow() {
            AutoUpdater.ShowSkipButton = false;

            InitializeComponent();

            tabControlManager = new TabControlManager(ImageTabControl);
            sortingManager = new SortingManager(tabControlManager);
            filesManager = new FilesManager(sortingManager, tabControlManager);

            CheckForUpdates();
            SetupSlideshow();

            commands = new Dictionary<CommandKeys, ICommand> {
        {
          new CommandKeys(Key.A),
          new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Alpha, tabControlManager.CanExcectute)
        },
        {
          new CommandKeys(Key.R),
          new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Red, tabControlManager.CanExcectute)
        },
        {
          new CommandKeys(Key.G),
          new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Green, tabControlManager.CanExcectute)
        },
        {
          new CommandKeys(Key.B),
          new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Blue, tabControlManager.CanExcectute)
        },
        {new CommandKeys(Key.F), new Command(ResetView, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.T), new Command(TileImage, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Space), new Command(NextImage, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Delete), new Command(DeleteImage, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.D, Key.LeftCtrl), new Command(DuplicateTab, tabControlManager.CanExcectute)},
        {
          new CommandKeys(Key.W, Key.LeftCtrl),
          new Command(tabControlManager.CloseSelectedTab, tabControlManager.CanExcectute)
        },
        {new CommandKeys(Key.S, Key.LeftCtrl), new Command(ChannelsMontage, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.S), new Command(ToggleSlideshow, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Right, Key.LeftCtrl), new Command(NextTab, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Left, Key.LeftCtrl), new Command(PreviousTab, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Right), new Command(NextImage, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Left), new Command(PreviousImage, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Add), new Command(LowerMip, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Subtract), new Command(HigherMip, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Space, Key.LeftCtrl), new Command(ToggleBars, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.V, Key.LeftCtrl), new Command(Paste)},
        {new CommandKeys(Key.C, Key.LeftCtrl), new Command(Copy, tabControlManager.CanExcectute)},
        {new CommandKeys(Key.Escape), new Command(Close)},
        {new CommandKeys(Key.N, Key.LeftCtrl), new Command(AddNewTab)},
      };
        }

        void Copy() {
            Clipboard.SetImage(tabControlManager.CurrentTab.ImagePresenter.ImageArea.Source as BitmapSource);
        }

        void Paste() {
            if (Clipboard.ContainsFileDropList()) {
                var filenames = new List<string>();

                Parallel.ForEach(Clipboard.GetFileDropList().Cast<string>(), filepath => {
                    filenames.Add(filepath);
                });

                var supportedFilenames = FilesManager.FilterSupportedFiles(filenames.ToArray());
                if (supportedFilenames.Length > 0) {
                    Parallel.ForEach(supportedFilenames, AddNewTab);
                }
            }
        }

        void NextImage() {
            tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Next);
        }

        void PreviousImage() {
            tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Previous);
        }

        void ResetView() {
            tabControlManager.CurrentTab.ResetView();
        }

        void HigherMip() {
            tabControlManager.CurrentTab.ImageSettings.MipValue++;
            //tabControlManager.CurrentTab.HigherMip();
            RefreshImage();
        }

        void LowerMip() {
            tabControlManager.CurrentTab.ImageSettings.MipValue--;
            //tabControlManager.CurrentTab.LowerMip();
            RefreshImage();
        }

        void PreviousTab() {
            if (VisualSelectedIndex() > 0) {
                var Indecies = ImageTabControl.GetOrderedHeaders().ToList();

                if (Indecies[VisualSelectedIndex() - 1].Content is TabItemControl nextTabItem)
                    ImageTabControl.SelectedIndex = ImageTabControl.Items.IndexOf(nextTabItem);
            }
        }

        void NextTab() {
            if (VisualSelectedIndex() == tabControlManager.CurrentTabControl.Items.Count - 1) return;

            var Indecies = ImageTabControl.GetOrderedHeaders().ToList();

            if (Indecies[VisualSelectedIndex() + 1].Content is TabItemControl nextTabItem)
                ImageTabControl.SelectedIndex = ImageTabControl.Items.IndexOf(nextTabItem);
        }

        void DuplicateTab() {
            if (!tabControlManager.CanExcectute()) {
                return;
            }

            var CurrentTab = tabControlManager.CurrentTab;
            var Filepath = CurrentTab.Path;
            if (string.IsNullOrEmpty(Filepath)) {
                var fileDialog = new OpenFileDialog {
                    Multiselect = true,
                    AddExtension = true,
                    Filter = FileFormats.FilterString
                };
                fileDialog.ShowDialog();
                Filepath = fileDialog.FileName;
            }

            if (!FilesManager.ValidFile(Filepath)) return;

            var duplicate_tab = tabControlManager.CurrentTab;
            var currentTabControl = tabControlManager.CurrentTabControl;
            if (currentTabControl.SelectedIndex != -1) {
                TabablzControl.AddItem(TabControlManager.GetTab(Filepath), duplicate_tab, AddLocationHint.After);
                currentTabControl.SelectedIndex = currentTabControl.Items.Count - 1;
            }
            else {
                var addedTab = tabControlManager.AddTab(Filepath);
            }

            duplicate_tab = tabControlManager.CurrentTab;
            duplicate_tab.InitialImagePath = Filepath;
            duplicate_tab.Footer.Visibility = footerVisibility;
            duplicate_tab.ImageSettings.SortMethod = CurrentTab.ImageSettings.SortMethod;
            duplicate_tab.ImageSettings.SortMode = CurrentTab.ImageSettings.SortMode;

            filesManager.SupportedFiles(Path.GetDirectoryName(Filepath));

            var FilenameIndex =
              duplicate_tab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(Filepath));

            duplicate_tab.Index = FilenameIndex == -1 ? 0 : (uint)FilenameIndex;

            DisplayImage();
        }

        int VisualSelectedIndex() {
            return VisualIndex((TabItemControl)ImageTabControl.SelectedItem);
        }

        int VisualIndex(TabItemControl obj) {
            var OrderedHeaders = ImageTabControl.GetOrderedHeaders().ToList();
            var Index = 0;
            foreach (var header in OrderedHeaders) {
                if (header.Content is TabItemControl tabItem && Equals(obj, tabItem)) {
                    return Index;
                }

                Index++;
            }

            return -1;
        }

        void TileImage() {
            var CurrentTab = tabControlManager.CurrentTab;
            CurrentTab.IsTiled = !CurrentTab.IsTiled;
            CurrentTab.CurrentMode = CurrentTab.CurrentMode == ApplicationMode.Tiled ? ApplicationMode.Normal : ApplicationMode.Tiled;
            RefreshImage();
        }

        void ChannelsMontage() {
            var CurrentTab = tabControlManager.CurrentTab;
            CurrentTab.UsesChannelsMontage = !CurrentTab.UsesChannelsMontage;
            CurrentTab.CurrentMode = CurrentTab.CurrentMode == ApplicationMode.ChannelsMontage
              ? ApplicationMode.Normal
              : ApplicationMode.ChannelsMontage;
            RefreshImage();
        }

        static bool ModifierKeyDown() {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                   Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ||
                   Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        void ToggleSlideshow() {
            var CurrentTab = tabControlManager.CurrentTab;
            if (CurrentTab.CurrentMode == ApplicationMode.Slideshow) {
                CurrentTab.CurrentMode = ApplicationMode.Normal;
                SlideshowTimer.Stop();
            }
            else {
                CurrentTab.CurrentMode = ApplicationMode.Slideshow;
                SlideshowTimer.Start();
            }

            CurrentTab.CurrentSlideshowTime = 0;
        }

        void DeleteImage() {
            var CurrentTab = tabControlManager.CurrentTab;
            var Result = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                         $"{Properties.Resources.Delete}{FileSystem.GetName(CurrentTab.Path)}",
                                         MessageBoxButton.YesNo);

            if (Result != MessageBoxResult.Yes) return;

            FileSystem.DeleteFile(CurrentTab.Path, UIOption.OnlyErrorDialogs,
                                  RecycleOption.SendToRecycleBin);

            if (CurrentTab.Paths.Count > 0) {
                tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Next);
            }
            else {
                FileBrowser();
            }
        }

        void RawKeyHandling(System.Windows.Input.KeyEventArgs e) {
            commands.TryGetValue(
              new CommandKeys(e.Key, Keyboard.IsKeyDown(Key.LeftShift), Keyboard.IsKeyDown(Key.LeftCtrl)),
              out var cmd);
            cmd?.Execute();
        }

        public void AddNewTab() {
            AddNewTab(string.Empty);
        }

        public void AddNewTab(string filepath) {
            if (string.IsNullOrEmpty(filepath)) {
                var fileDialog = new OpenFileDialog {
                    Multiselect = true,
                    AddExtension = true,
                    Filter = FileFormats.FilterString
                };
                fileDialog.ShowDialog();
                filepath = fileDialog.FileName;
            }

            if (!FilesManager.ValidFile(filepath)) return;

            var CurrentTab = tabControlManager.CurrentTab;
            var CurrentTabControl = tabControlManager.CurrentTabControl;

            if (CurrentTabControl.SelectedIndex != -1) {
                TabablzControl.AddItem(TabControlManager.GetTab(filepath), CurrentTab, AddLocationHint.After);
                CurrentTabControl.SelectedIndex = CurrentTabControl.Items.Count - 1;
            }
            else {
                var AddedTab = tabControlManager.AddTab(filepath);
            }

            CurrentTab = tabControlManager.CurrentTab;

            filesManager.SupportedFiles(Path.GetDirectoryName(filepath));

            var FilenameIndex =
              CurrentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

            CurrentTab.Index = FilenameIndex == -1 ? 0 : (uint)FilenameIndex;

            CurrentTab.Footer.Visibility = footerVisibility;
        }

        void AscendingSort(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;

            var CurrentTab = tabControlManager.CurrentTab;
            if (CurrentTab.ImageSettings.SortMode == SortMode.Descending) ReversePaths();

            CurrentTab.ImageSettings.SortMode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;

            Clipboard.SetText($"\"{tabControlManager.CurrentTab.Path}\"");
        }

        void CopyFilenameToClipboard(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;

            Clipboard.SetText($"\"{Path.GetFileName(tabControlManager.CurrentTab.Path)}\"");
        }

        void DecendingSort(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;

            var current_tab = tabControlManager.CurrentTab;
            if (current_tab.ImageSettings.SortMode == SortMode.Ascending) ReversePaths();

            current_tab.ImageSettings.SortMode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        void ReversePaths() {
            var InitalImage = tabControlManager.CurrentTab.Path;
            var FilepathsList = tabControlManager.CurrentTab.Paths;
            FilepathsList.Reverse();
            sortingManager.FindImageAfterSort(FilepathsList, InitalImage);
        }

        void DisplayImage() {
            var current_tab = tabControlManager.CurrentTab;
            if (current_tab == null) return;

            if (tabControlManager.CurrentTabIndex < 0) return;

            if (current_tab.ImagePresenter.ImageArea == null || current_tab.Paths.Count == 0) return;

            current_tab.LoadImage();
        }

        void FileBrowser() {
            var FileDialog = new OpenFileDialog {
                Multiselect = true,
                AddExtension = true,
                Filter = FileFormats.FilterString
            };
            FileDialog.ShowDialog();
            if (FileDialog.SafeFileNames.Length == 0) {
                return;
            }

            foreach (var filename in FileDialog.FileNames) {
                AddNewTab(Path.GetFullPath(filename));
            }
        }

        void ImageEditorBrowse() {
            var FileDialog = new OpenFileDialog {
                Multiselect = false,
                AddExtension = true,
                Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"
            };
            if (FileDialog.ShowDialog() == true) {
                Settings.Default.ImageEditor = FileDialog.FileName;
                Process.Start(Settings.Default.ImageEditor, tabControlManager.CurrentTab.Path);
            }
        }

        void OpenInImageEditor(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;

            if (!string.IsNullOrEmpty(Settings.Default.ImageEditor)) {
                if (File.Exists(Settings.Default.ImageEditor)) {
                    Process.Start(Settings.Default.ImageEditor, tabControlManager.CurrentTab.Path);
                    return;
                }

                if (MessageBox.Show("Image editor not found\nDo you want to browse for editor?",
                                    Properties.Resources.FileMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                    ImageEditorBrowse();
                }
            }
            else {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?",
                                    Properties.Resources.ImageEditorMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                    ImageEditorBrowse();
                }
            }

            Settings.Default.Save();
        }

        internal void RefreshImage() {
            var CurrentTab = tabControlManager.CurrentTab;
            if (CurrentTab == null) return;
            if (CurrentTab.Paths.Count == 0) return;

            CurrentTab.RefreshImage();
        }

        void ReplaceImageInTab(string filename) {
            if (!FilesManager.ValidFile(filename)) return;

            if (tabControlManager.CurrentTabIndex < 0) {
                AddNewTab(filename);
            }
            else {
                var CurrentTab = tabControlManager.CurrentTab;
                CurrentTab.InitialImagePath = filename;
                filesManager.SupportedFiles(Path.GetDirectoryName(filename));

                var Index = CurrentTab.Paths.IndexOf(filename);
                CurrentTab.Index = Index == -1 ? 0 : (uint)Index;
            }
        }

        void ToggleDisplayChannel(Channels channel) {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.SetDisplayChannel(channel);
        }

        void OnDeleted(object sender, FileSystemEventArgs args) {
            Current.Dispatcher.Invoke(() => {
                var tabItemControl = tabControlManager.CurrentTab;
                var filename = tabItemControl.Path;
                var currentTabPaths = tabItemControl.Paths;
                filesManager.SupportedFiles(DirectoryName);
                if (args.FullPath != filename) {
                    sortingManager.FindImageAfterSort(currentTabPaths, filename);
                }
            });
        }

        void OnCreated(object sender, FileSystemEventArgs args) {
            Current.Dispatcher.Invoke(() => {
                var tabItemControl = tabControlManager.CurrentTab;
                var filename = tabItemControl.Path;
                var currentTabPaths = tabItemControl.Paths;
                filesManager.SupportedFiles(DirectoryName);
                sortingManager.FindImageAfterSort(currentTabPaths, filename);
            });
        }

        void OnRenamed(object sender, RenamedEventArgs args) {
            Current.Dispatcher.Invoke(() => {
                var tabItemControl = tabControlManager.CurrentTab;
                var filename = tabItemControl.Path;
                var currentTabPaths = tabItemControl.Paths;
                if (filename == args.OldFullPath) {
                    filename = args.FullPath;
                }

                filesManager.SupportedFiles(DirectoryName);
                sortingManager.FindImageAfterSort(currentTabPaths, filename);
            });
        }

        void ParentDirectoryChanged(object sender, FileSystemEventArgs args) {
            Current.Dispatcher.Invoke(() => {
                // Need to check all tabs
                var toBeClosed = new List<TabItemControl>();
                foreach (var tabablzControl in tabControlManager.TabControls) {
                    foreach (TabItemControl tabItemControl in tabablzControl.Items) {
                        switch (args.ChangeType) {
                            case WatcherChangeTypes.Deleted: {
                                if (Path.GetDirectoryName(tabItemControl.InitialImagePath) == args.FullPath) {
                                    toBeClosed.Add(tabItemControl);
                                }

                                break;
                            }
                            case WatcherChangeTypes.Changed: {
                                break;
                            }
                            case WatcherChangeTypes.Renamed: {
                                var renamedArgs = (RenamedEventArgs)args;
                                var newFile = Path.Combine(renamedArgs.FullPath,
                                                     Path.GetFileName(tabItemControl.Path) ??
                                                     throw new InvalidOperationException("It was the null"));
                                if (Path.GetDirectoryName(tabItemControl.InitialImagePath) ==
                              renamedArgs.OldFullPath) {
                                    ReplaceImageInTab(newFile);
                                }

                                break;
                            }
                            case WatcherChangeTypes.All: {
                                break;
                            }
                            case WatcherChangeTypes.Created: {
                                break;
                            }
                        }
                    }
                }

                foreach (var tab in toBeClosed) {
                    tabControlManager.CloseTab(tab);
                }
            });
        }

        void SetupSlideshow() {
            SlideshowTimer = new DispatcherTimer();
            SlideshowTimer.Tick += Slideshow;
            SlideshowTimer.Interval = new TimeSpan(0, 0, 1);
        }

        int SlideshowInterval { get; } = 5;

        void Slideshow(object source, EventArgs e) {
            if (!tabControlManager.CanExcectute()) {
                tabControlManager.CurrentTab.CurrentMode = ApplicationMode.Normal;
                return;
            }

            if (tabControlManager.CurrentTab.CurrentSlideshowTime < SlideshowInterval) {
                tabControlManager.CurrentTab.CurrentSlideshowTime++;
            }
            else {
                tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
                SlideshowTimer.Stop();
                tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Next);
                SlideshowTimer.Start();
            }

            if (tabControlManager.CurrentTab.CurrentMode == ApplicationMode.Slideshow) return;

            SlideshowTimer.Stop();
            tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
        }

        void SortByDateModified(object sender, RoutedEventArgs e) {
            if (tabControlManager.CurrentTab == null) return;

            if (!tabControlManager.CanExcectute()) return;

            tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Date;
            sortingManager.Sort();
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void SortByName(object sender, RoutedEventArgs e) {
            if (tabControlManager.CurrentTab == null) return;

            if (!tabControlManager.CanExcectute()) return;

            tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Name;
            sortingManager.Sort();
            SortDate.IsChecked = false;
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
        }

        void SortBySize(object sender, RoutedEventArgs e) {
            if (tabControlManager.CurrentTab == null) return;

            if (!tabControlManager.CanExcectute()) return;

            tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Size;
            sortingManager.Sort();
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
            SortSize.IsChecked = true;
        }

        void ViewInExplorer(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;

            Process.Start("explorer.exe", "/select, " + tabControlManager.CurrentTab.Path);
        }

        void AlwaysOnTopClick(object sender, RoutedEventArgs e) {
            if (tabControlManager.CurrentTab == null) return;

            Topmost = !Topmost;
            AlwaysOnTopUi.IsChecked = Topmost;
        }

        void WindowLoaded(object sender, RoutedEventArgs e) {
            Left = Settings.Default.WindowLocation.X;
            Top = Settings.Default.WindowLocation.Y;

            IsChangingSize = true;
            Width = Settings.Default.WindowSize.Width;
            Height = Settings.Default.WindowSize.Height;

            WindowState = (WindowState)Settings.Default.WindowState;
            IsChangingSize = false;

            e.Handled = true;
        }

        void WindowClosing(object sender, CancelEventArgs e) {
            Settings.Default.WindowLocation = new Point((int)Left, (int)Top);
            Settings.Default.WindowState = (int)WindowState;
            var newSize = new Size {
                Width = WindowState == WindowState.Normal ? (int)Width : (int)RestoreBounds.Width,
                Height = WindowState == WindowState.Normal ? (int)Height : (int)RestoreBounds.Height
            };
            Settings.Default.WindowSize = newSize;

            Settings.Default.Save();
        }

        void WindowClosed(object sender, EventArgs e) {
            Dispose();
            if (App.GetMainWindows().Count == 0) {
                Current.Shutdown();
            }
        }

        void AboutClick(object sender, RoutedEventArgs e) {
            if (WindowState == WindowState.Maximized) {
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

        void DockLayoutDragDrop(object sender, DragEventArgs e) {
            //var bitmap = e.Data.GetData(DataFormats.Bitmap);
            //var html = e.Data.GetData(DataFormats.Html);
            var filenames = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (filenames == null) {
                return;
            }

            if (e.OriginalSource is DependencyObject current) {
                while (!(VisualTreeHelper.GetParent(current) is TabablzControl)) {
                    current = VisualTreeHelper.GetParent(current);
                }

                var tabablzControl = VisualTreeHelper.GetParent(current) as TabablzControl;
                (tabablzControl?.SelectedItem as TabItemControl)?.ImagePresenter.ScrollViewer.Focus();
            }

            var supportedFilenames = FilesManager.FilterSupportedFiles(filenames);
            if (supportedFilenames.Length == 0) {
                return;
            }

            if (supportedFilenames.Length > 1) {
                Parallel.ForEach(supportedFilenames, AddNewTab);
            }
            else {
                if (Settings.Default.ReplaceImageOnDrop) {
                    ReplaceImageInTab(supportedFilenames[0]);
                }
                else {
                    AddNewTab(supportedFilenames[0]);
                }
            }

            Keyboard.Focus(this);

            e.Handled = true;
        }

        public void ImageAreaKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            RawKeyHandling(e);
            Keyboard.Focus(this);
            e.Handled = true;
        }

        void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            try {
                ImageAreaKeyDown(sender, e);
            }
            catch (ArgumentException) { }
            finally {
                e.Handled = true;
            }
        }

        void TileImageOnClick(object sender, RoutedEventArgs e) {
            TileImage();
        }

        void ChannelsMontageOnClick(object sender, RoutedEventArgs e) {
            ChannelsMontage();
        }

        void OptionsOnClick(object sender, RoutedEventArgs e) {
            if (WindowState == WindowState.Maximized) {
                var rect = Screen.GetWorkingArea(new Point((int)Left, (int)Top));
                App.OptionsDialog.Top = rect.Top + (ActualHeight / 2.0) - (App.OptionsDialog.Height / 2.0);
                App.OptionsDialog.Left = rect.Left + (ActualWidth / 2.0) - (App.OptionsDialog.Width / 2.0);
            }
            else {
                App.OptionsDialog.Top = Top + (ActualHeight / 2.0) - (App.OptionsDialog.Height / 2.0);
                App.OptionsDialog.Left = Left + (ActualWidth / 2.0) - (App.OptionsDialog.Width / 2.0);
            }

            App.OptionsDialog.ShowDialog();
        }

        void CheckForUpdateOnClick(object sender, RoutedEventArgs e) {
            CheckForUpdates();
        }

        static void CheckForUpdates() {
            //AutoUpdater.Start("http://www.dropbox.com/s/2b0gna7rz889b5u/Update.xml?dl=1");
        }

        void ResetViewClick(object sender, RoutedEventArgs e) {
            if (tabControlManager.CanExcectute()) tabControlManager.CurrentTab.ResetView();
        }

        void OpenFilesClick(object sender, RoutedEventArgs e) {
            if (tabControlManager.CurrentTab == null || e.OriginalSource is ScrollViewer ||
                e.OriginalSource is MenuItem) {
                FileBrowser();
                Keyboard.Focus(this);
            }
        }

        public void Dispose() {
            ImageDirectoryWatcher?.Dispose();
            ParentDirectoryWatcher?.Dispose();
        }

        void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (IsChangingSize) {
                return;
            }

            Settings.Default.WindowState = (int)WindowState;
            var newSize = new Size {
                Width = WindowState == WindowState.Normal ? (int)Width : (int)RestoreBounds.Width,
                Height = WindowState == WindowState.Normal ? (int)Height : (int)RestoreBounds.Height
            };
            Settings.Default.WindowSize = newSize;

            Settings.Default.Save();
        }

        void DisplayAllChannels(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel = Channels.RGB;
        }

        void DisplayRedChannel(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
              tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Red ? Channels.RGB : Channels.Red;
        }

        void DisplayGreenChannel(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
              tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Green
                ? Channels.RGB
                : Channels.Green;
        }

        void DisplayBlueChannel(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
              tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Blue
                ? Channels.RGB
                : Channels.Blue;
        }

        void DisplayAlphaChannel(object sender, RoutedEventArgs e) {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
              tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Alpha
                ? Channels.RGB
                : Channels.Alpha;
        }

        void OnClickToggleBars(object sender, RoutedEventArgs e) {
            ToggleBars();
        }

        void ToggleBars() {
            if (!tabControlManager.CanExcectute()) return;
            footerVisibility = tabControlManager.CurrentTab.Footer.Visibility == Visibility.Visible
              ? Visibility.Collapsed
              : Visibility.Visible;
            WindowStyle = footerVisibility != Visibility.Visible ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            foreach (var tabControl in tabControlManager.TabControls) {
                tabControl.IsHeaderPanelVisible = footerVisibility == Visibility.Visible;
                foreach (TabItemControl tabItemControl in tabControl.Items) {
                    var scrollViewer = tabItemControl.ImagePresenter.ScrollViewer;
                    scrollViewer.VerticalScrollBarVisibility = footerVisibility != Visibility.Visible
                      ? ScrollBarVisibility.Hidden
                      : ScrollBarVisibility.Auto;
                    scrollViewer.HorizontalScrollBarVisibility = footerVisibility != Visibility.Visible
                      ? ScrollBarVisibility.Hidden
                      : ScrollBarVisibility.Auto;
                    tabItemControl.Footer.Visibility = footerVisibility;
                }
            }
        }

        void ImageTabControl_OnIsDraggingWindowChanged(object sender, RoutedPropertyChangedEventArgs<bool> e) {
            if (WindowState == WindowState.Maximized) {
                WindowState = WindowState.Normal;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}