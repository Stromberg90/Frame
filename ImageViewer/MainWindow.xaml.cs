//CHANGLOG 1.6

//Bugs fixed.
//When dragging tab from maximized window, it now changes it to not tbe maximized.
//New wpf image box, so docking helpers show up properly.
//Sort settings now stick when duplicating tab.
//When files changed in directory, it now keeps the opened image.

//Features
//UI Improvments, tabs color matches the rest.
//Toggle Bars, hides window frame and footer.
//Collapses footer at lower widths.
//Double click to open image


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
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

namespace Frame
{
    public partial class MainWindow : IDisposable
    {
        public Visibility FooterVisibility = Visibility.Visible;
        static DispatcherTimer slideshowTimer;

        readonly FilesManager filesManager;
        readonly SortingManager sortingManager;

        readonly TabControlManager tabControlManager;
        FileSystemWatcher imageDirectoryWatcher;
        FileSystemWatcher parentDirectoryWatcher;
        bool changingSize = true;
        Dictionary<CommandKeys, ICommand> commands;
        string directoryName;

        public class ToggleDisplayChannelCommand : ICommand
        {
            readonly CommandFunction func;
            readonly Channels channel;

            public delegate void CommandFunction(Channels channel);

            public ToggleDisplayChannelCommand(CommandFunction func, Channels channel)
            {
                this.func = func;
                this.channel = channel;
            }

            public void Execute()
            {
                if (!ModifierKeyDown())
                {
                    func(channel);
                }
            }
        }

        public class Command : ICommand
        {
            readonly CommandFunction func;
            readonly ValidateFunction validateFunction;

            public delegate void CommandFunction();

            public delegate void ValidateFunction();


            public Command(CommandFunction func, ValidateFunction validateFunction = null, params object[] args)
            {
                this.func = func;
                this.validateFunction = validateFunction;
            }

            public void Execute()
            {
                validateFunction?.Invoke();
                func();
            }
        }

        struct CommandKeys
        {
            [UsedImplicitly] Key key;
            [UsedImplicitly] bool LeftShift;
            [UsedImplicitly] bool LeftCtrl;

            public CommandKeys(Key key, params Key[] keys)
            {
                this.key = key;
                LeftShift = false;
                LeftCtrl = false;
                foreach (var key1 in keys)
                {
                    switch (key1)
                    {
                        case Key.LeftShift:
                        {
                            LeftShift = true;
                            break;
                        }
                        case Key.LeftCtrl:
                        {
                            LeftCtrl = true;
                            break;
                        }
                    }
                }
            }

            public CommandKeys(Key key, bool leftShift, bool leftCtrl)
            {
                this.key = key;
                LeftShift = leftShift;
                LeftCtrl = leftCtrl;
            }
        }

        public MainWindow()
        {
            AutoUpdater.ShowSkipButton = false;

            InitializeComponent();

            tabControlManager = new TabControlManager(ImageTabControl);
            sortingManager = new SortingManager(tabControlManager);
            filesManager = new FilesManager(sortingManager, tabControlManager);

            CheckForUpdates();
            SetupSlideshow();

            commands = new Dictionary<CommandKeys, ICommand>
            {
                {new CommandKeys(Key.A), new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Alpha)},
                {new CommandKeys(Key.R), new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Red)},
                {new CommandKeys(Key.G), new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Green)},
                {new CommandKeys(Key.B), new ToggleDisplayChannelCommand(ToggleDisplayChannel, Channels.Blue)},
                {new CommandKeys(Key.F), new Command(ResetView)},
                {new CommandKeys(Key.T), new Command(TileImage)},
                {new CommandKeys(Key.Space), new Command(NextImage)},
                {new CommandKeys(Key.Delete), new Command(DeleteImage)},
                {new CommandKeys(Key.D, Key.LeftCtrl), new Command(DuplicateTab)},
                {new CommandKeys(Key.W, Key.LeftCtrl), new Command(CloseTab)},
                {new CommandKeys(Key.S, Key.LeftCtrl), new Command(ChannelsMontage)},
                {new CommandKeys(Key.S), new Command(ToggleSlideshow)},
                {new CommandKeys(Key.Right, Key.LeftCtrl), new Command(NextTab)},
                {new CommandKeys(Key.Left, Key.LeftCtrl), new Command(PreviousTab)},
                {new CommandKeys(Key.Right), new Command(NextImage)},
                {new CommandKeys(Key.Left), new Command(PreviousImage)},
                {new CommandKeys(Key.Add), new Command(LowerMip)},
                {new CommandKeys(Key.Subtract), new Command(HigherMip)},
                {new CommandKeys(Key.Space, Key.LeftCtrl), new Command(ToggleBars)},
            };
        }

        void NextImage()
        {
            tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Next);
        }

        void PreviousImage()
        {
            tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Previous);
        }

        void ResetView()
        {
            tabControlManager.CurrentTab.ResetView();
        }

        void ValidatedKeyHandling(System.Windows.Input.KeyEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;
            commands.TryGetValue(
                new CommandKeys(e.Key, Keyboard.IsKeyDown(Key.LeftShift), Keyboard.IsKeyDown(Key.LeftCtrl)),
                out var cmd);
            cmd?.Execute();
        }

        void HigherMip()
        {
            tabControlManager.CurrentTab.ImageSettings.MipValue += 1;
            RefreshImage();
        }

        void LowerMip()
        {
            tabControlManager.CurrentTab.ImageSettings.MipValue -= 1;
            RefreshImage();
        }

        void PreviousTab()
        {
            if (VisualSelectedIndex() > 0)
            {
                var indecies = ImageTabControl.GetOrderedHeaders().ToList();

                if (indecies[VisualSelectedIndex() - 1].Content is TabItemControl nextTabItem)
                    ImageTabControl.SelectedIndex = ImageTabControl.Items.IndexOf(nextTabItem);
            }
        }

        void NextTab()
        {
            if (VisualSelectedIndex() == tabControlManager.CurrentTabControl.Items.Count - 1) return;

            var indecies = ImageTabControl.GetOrderedHeaders().ToList();

            if (indecies[VisualSelectedIndex() + 1].Content is TabItemControl nextTabItem)
                ImageTabControl.SelectedIndex = ImageTabControl.Items.IndexOf(nextTabItem);
        }

        void DuplicateTab()
        {
            if (!tabControlManager.CanExcectute())
            {
                return;
            }

//      AddNewTab(tabControlManager.CurrentTab.Path);
            var oldTab = tabControlManager.CurrentTab;
            var filepath = oldTab.Path;
            if (string.IsNullOrEmpty(filepath))
            {
                var fileDialog = new OpenFileDialog
                {
                    Multiselect = true,
                    AddExtension = true,
                    Filter = FileFormats.FilterString
                };
                fileDialog.ShowDialog();
                filepath = fileDialog.FileName;
            }

            if (!FilesManager.ValidFile(filepath)) return;

            var currentTab = tabControlManager.CurrentTab;
            var currentTabControl = tabControlManager.CurrentTabControl;
            if (currentTabControl.SelectedIndex != -1)
            {
                TabablzControl.AddItem(TabControlManager.GetTab(filepath), currentTab, AddLocationHint.After);
                currentTabControl.SelectedIndex = currentTabControl.Items.Count - 1;
            }
            else
            {
                var addedTab = tabControlManager.AddTab(filepath);
                addedTab.ImageSettings.PropertyChanged += ImageSettings_PropertyChanged;
            }

            currentTab = tabControlManager.CurrentTab;
            currentTab.InitialImagePath = filepath;
            currentTab.ImageSettings.IsGif = false;
            currentTab.Footer.Visibility = FooterVisibility;
            currentTab.ImageSettings.SortMethod = oldTab.ImageSettings.SortMethod;
            currentTab.ImageSettings.SortMode = oldTab.ImageSettings.SortMode;

            filesManager.SupportedFiles(Path.GetDirectoryName(filepath));

            var filenameIndex =
                currentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

            currentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

            DisplayImage();
            SetupDirectoryWatcher();
        }

        int VisualSelectedIndex()
        {
            return VisualIndex((TabItemControl) ImageTabControl.SelectedItem);
        }

        int VisualIndex(TabItemControl obj)
        {
            var orderedHeaders = ImageTabControl.GetOrderedHeaders().ToList();
            var index = 0;
            foreach (var header in orderedHeaders)
            {
                if (header.Content is TabItemControl tabItem && Equals(obj, tabItem))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        void TileImage()
        {
            var currentTab = tabControlManager.CurrentTab;
            currentTab.Tiled = !currentTab.Tiled;
            RefreshImage();
            currentTab.ResetView();
        }

        void ChannelsMontage()
        {
            var currentTab = tabControlManager.CurrentTab;
            currentTab.ChannelsMontage = !currentTab.ChannelsMontage;
            RefreshImage();
            currentTab.ResetView();
        }

        static bool ModifierKeyDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                   Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ||
                   Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }

        void ToggleSlideshow()
        {
            var currentTab = tabControlManager.CurrentTab;
            if (currentTab.Mode == ApplicationMode.Slideshow)
            {
                currentTab.Mode = ApplicationMode.Normal;
                slideshowTimer.Stop();
            }
            else
            {
                currentTab.Mode = ApplicationMode.Slideshow;
                slideshowTimer.Start();
            }

            currentTab.CurrentSlideshowTime = 0;
        }

        void DeleteImage()
        {
            var currentTab = tabControlManager.CurrentTab;
            var result = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                $"{Properties.Resources.Delete}{FileSystem.GetName(currentTab.Path)}",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes) return;

            FileSystem.DeleteFile(currentTab.Path, UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin);

            if (currentTab.Paths.Count > 0)
            {
                filesManager.SupportedFiles(Path.GetDirectoryName(currentTab.Path));

                tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Next);
            }
            else
            {
                FileBrowser();
            }
        }

        void RawKeyHandling(System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                {
                    Close();
                    break;
                }
                case Key.N:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl)) AddNewTab(string.Empty);

                    break;
                }
            }
        }

        public void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                var fileDialog = new OpenFileDialog
                {
                    Multiselect = true,
                    AddExtension = true,
                    Filter = FileFormats.FilterString
                };
                fileDialog.ShowDialog();
                filepath = fileDialog.FileName;
            }

            if (!FilesManager.ValidFile(filepath)) return;

            var currentTab = tabControlManager.CurrentTab;
            var currentTabControl = tabControlManager.CurrentTabControl;
            if (currentTabControl.SelectedIndex != -1)
            {
                TabablzControl.AddItem(TabControlManager.GetTab(filepath), currentTab, AddLocationHint.After);
                currentTabControl.SelectedIndex = currentTabControl.Items.Count - 1;
            }
            else
            {
                var addedTab = tabControlManager.AddTab(filepath);
                addedTab.ImageSettings.PropertyChanged += ImageSettings_PropertyChanged;
            }

            currentTab = tabControlManager.CurrentTab;

            filesManager.SupportedFiles(Path.GetDirectoryName(filepath));

            var filenameIndex =
                currentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

            currentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

//            currentTab.InitialImagePath = filepath;
//            currentTab.ImageSettings.IsGif = false;
            currentTab.Footer.Visibility = FooterVisibility;

//            DisplayImage();
            SetupDirectoryWatcher();
        }

        void ImageSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshImage();
        }

        void AscendingSort(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;

            var currentTab = tabControlManager.CurrentTab;
            if (currentTab.ImageSettings.SortMode == SortMode.Descending) ReversePaths();

            currentTab.ImageSettings.SortMode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void CloseTab()
        {
            tabControlManager.CloseSelectedTab();
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;

            Clipboard.SetText($"\"{tabControlManager.CurrentTab.Path}\"");
        }

        void CopyFilenameToClipboard(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;

            Clipboard.SetText($"\"{Path.GetFileName(tabControlManager.CurrentTab.Path)}\"");
        }

        void DecendingSort(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;


            var currentTab = tabControlManager.CurrentTab;
            if (currentTab.ImageSettings.SortMode == SortMode.Ascending) ReversePaths();

            currentTab.ImageSettings.SortMode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        void ReversePaths()
        {
            var initalImage = tabControlManager.CurrentTab.Path;
            var filePathsList = tabControlManager.CurrentTab.Paths;
            filePathsList.Reverse();
            sortingManager.FindImageAfterSort(filePathsList, initalImage);
        }

        void DisplayImage()
        {
            Dispatcher.Invoke(() =>
            {
                var currentTab = tabControlManager.CurrentTab;
                if (currentTab == null) return;

                if (tabControlManager.CurrentTabIndex < 0) return;

                if (currentTab.ImagePresenter.ImageArea == null || !currentTab.IsValid) return;

                currentTab.ImagePresenter.ImageArea.Source = currentTab.Image;
            });
        }

        void FileBrowser()
        {
            var fileDialog1 = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                Filter = FileFormats.FilterString
            };
            fileDialog1.ShowDialog();
            var fileDialog = fileDialog1;
            if (!fileDialog.SafeFileNames.Any())
                return;

            foreach (var fileName in fileDialog.FileNames) AddNewTab(Path.GetFullPath(fileName));
        }

        void ImageEditorBrowse()
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"
            };
            if (fileDialog.ShowDialog() == true)
            {
                Settings.Default.ImageEditor = fileDialog.FileName;
                Process.Start(Settings.Default.ImageEditor, tabControlManager.CurrentTab.Path);
            }
        }

        void OpenInImageEditor(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;

            if (!string.IsNullOrEmpty(Settings.Default.ImageEditor))
            {
                if (File.Exists(Settings.Default.ImageEditor))
                {
                    Process.Start(Settings.Default.ImageEditor, tabControlManager.CurrentTab.Path);
                    return;
                }

                if (MessageBox.Show("Image editor not found\nDo you want to browse for editor?",
                        Properties.Resources.FileMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    ImageEditorBrowse();
            }
            else
            {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?",
                        Properties.Resources.ImageEditorMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    ImageEditorBrowse();
            }

            Settings.Default.Save();
        }

        internal void RefreshImage()
        {
            Current.Dispatcher.Invoke(() =>
            {
                var currentTab = tabControlManager.CurrentTab;
                if (currentTab == null) return;
                if (!currentTab.IsValid) return;

                currentTab.ImagePresenter.ImageArea.Source = currentTab.Image;
            });
        }

        void ReplaceImageInTab(string filename)
        {
            if (!FilesManager.ValidFile(filename)) return;

            if (tabControlManager.CurrentTabIndex < 0)
            {
                AddNewTab(filename);
            }
            else
            {
                var currentTab = tabControlManager.CurrentTab;
                currentTab.InitialImagePath = filename;
                filesManager.SupportedFiles(Path.GetDirectoryName(filename));

                var filenameIndex = currentTab.Paths.IndexOf(filename);
                currentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;
                currentTab.ImageSettings.IsGif = false;
                SetupDirectoryWatcher();
            }
        }

        void ToggleDisplayChannel(Channels channel)
        {
            if (!tabControlManager.CanExcectute()) return;
            switch (channel)
            {
                case Channels.RGB:
                {
                    tabControlManager.CurrentTab.ImageSettings.DisplayChannel = Channels.RGB;
                    break;
                }
                case Channels.Red:
                {
                    tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                        tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Red
                            ? Channels.RGB
                            : Channels.Red;
                    break;
                }
                case Channels.Green:
                {
                    tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                        tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Green
                            ? Channels.RGB
                            : Channels.Green;
                    break;
                }
                case Channels.Blue:
                {
                    tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                        tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Blue
                            ? Channels.RGB
                            : Channels.Blue;
                    break;
                }
                case Channels.Alpha:
                {
                    tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                        tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Alpha
                            ? Channels.RGB
                            : Channels.Alpha;
                    break;
                }
            }

            RefreshImage();
        }

        void SetupDirectoryWatcher()
        {
            directoryName = Path.GetDirectoryName(tabControlManager.CurrentTab.InitialImagePath);
            if (directoryName == null) return;

            imageDirectoryWatcher = null;
            imageDirectoryWatcher = new FileSystemWatcher
            {
                Path = directoryName,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            parentDirectoryWatcher = null;
            if (directoryName != Directory.GetDirectoryRoot(directoryName))
            {
                parentDirectoryWatcher = new FileSystemWatcher
                {
                    Path = Directory.GetParent(directoryName).FullName,
                    NotifyFilter =
                        NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                        NotifyFilters.DirectoryName
                };

                parentDirectoryWatcher.Changed += ParentDirectoryChanged;
                parentDirectoryWatcher.Created += ParentDirectoryChanged;
                parentDirectoryWatcher.Deleted += ParentDirectoryChanged;
                parentDirectoryWatcher.Renamed += ParentDirectoryChanged;

                parentDirectoryWatcher.EnableRaisingEvents = true;
            }

            imageDirectoryWatcher.Changed += (sender, args) => { DisplayImage(); };
            imageDirectoryWatcher.Created += OnCreated;
            imageDirectoryWatcher.Deleted += OnDeleted;

            imageDirectoryWatcher.Renamed += OnRenamed;

            imageDirectoryWatcher.EnableRaisingEvents = true;
        }

        void OnDeleted(object sender, FileSystemEventArgs args)
        {
            Current.Dispatcher.Invoke(() =>
            {
                var tabItemControl = tabControlManager.CurrentTab;
                var filename = tabItemControl.Path;
                var currentTabPaths = tabItemControl.Paths;
                filesManager.SupportedFiles(directoryName);
                if (args.FullPath != filename)
                {
                    sortingManager.FindImageAfterSort(currentTabPaths, filename);
                }
            });
        }

        void OnCreated(object sender, FileSystemEventArgs args)
        {
            Current.Dispatcher.Invoke(() =>
            {
                var tabItemControl = tabControlManager.CurrentTab;
                var filename = tabItemControl.Path;
                var currentTabPaths = tabItemControl.Paths;
                filesManager.SupportedFiles(directoryName);
                sortingManager.FindImageAfterSort(currentTabPaths, filename);
            });
        }

        void OnRenamed(object sender, RenamedEventArgs args)
        {
            Current.Dispatcher.Invoke(() =>
            {
                var tabItemControl = tabControlManager.CurrentTab;
                var filename = tabItemControl.Path;
                var currentTabPaths = tabItemControl.Paths;
                if (filename == args.OldFullPath)
                {
                    filename = args.FullPath;
                }

                filesManager.SupportedFiles(directoryName);
                sortingManager.FindImageAfterSort(currentTabPaths, filename);
            });
        }

        void ParentDirectoryChanged(object sender, FileSystemEventArgs args)
        {
            Current.Dispatcher.Invoke(() =>
            {
                // Need to check all tabs
                var currentTab = tabControlManager.CurrentTab;
                switch (args.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                    {
                        if (Path.GetDirectoryName(currentTab.InitialImagePath) == args.FullPath) CloseTab();

                        break;
                    }
                    case WatcherChangeTypes.Changed:
                    {
                        break;
                    }
                    case WatcherChangeTypes.Renamed:
                    {
                        var renamedArgs = (RenamedEventArgs) args;
                        var newFile = Path.Combine(renamedArgs.FullPath,
                            Path.GetFileName(currentTab.Path) ??
                            throw new InvalidOperationException("It was the null"));
                        if (Path.GetDirectoryName(currentTab.InitialImagePath) ==
                            renamedArgs.OldFullPath)
                            ReplaceImageInTab(newFile);

                        break;
                    }
                    case WatcherChangeTypes.All:
                    {
                        break;
                    }
                    case WatcherChangeTypes.Created:
                    {
                        break;
                    }
                }
            });
        }

        void SetupSlideshow()
        {
            slideshowTimer = new DispatcherTimer();
            slideshowTimer.Tick += Slideshow;
            slideshowTimer.Interval = new TimeSpan(0, 0, 1);
        }

        int SlideshowInterval { get; } = 5;

        void Slideshow(object source, EventArgs e)
        {
            if (!tabControlManager.CanExcectute())
            {
                tabControlManager.CurrentTab.Mode = ApplicationMode.Normal;
                return;
            }

            if (tabControlManager.CurrentTab.CurrentSlideshowTime < SlideshowInterval)
            {
                tabControlManager.CurrentTab.CurrentSlideshowTime += 1;
            }
            else
            {
                tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
                slideshowTimer.Stop();
                tabControlManager.CurrentTab.SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (tabControlManager.CurrentTab.Mode == ApplicationMode.Slideshow) return;

            slideshowTimer.Stop();
            tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
        }

        void SortByDateModified(object sender, RoutedEventArgs e)
        {
            if (tabControlManager.CurrentTab == null) return;

            if (!tabControlManager.CanExcectute()) return;

            tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Date;
            sortingManager.Sort();
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void SortByName(object sender, RoutedEventArgs e)
        {
            if (tabControlManager.CurrentTab == null) return;

            if (!tabControlManager.CanExcectute()) return;

            tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Name;
            sortingManager.Sort();
            SortDate.IsChecked = false;
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
        }

        void SortBySize(object sender, RoutedEventArgs e)
        {
            if (tabControlManager.CurrentTab == null) return;

            if (!tabControlManager.CanExcectute()) return;

            tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Size;
            sortingManager.Sort();
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
            SortSize.IsChecked = true;
        }

//    void SwitchImage(SwitchDirection switchDirection)
//    {
//      tabControlManager.CurrentTab.ImageSettings.Reset();
//      tabControlManager.CurrentTab.Tiled           = false;
//      tabControlManager.CurrentTab.ChannelsMontage = false;
//
//      if (tabControlManager.CurrentTab.Mode == ApplicationMode.Slideshow)
//        tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
//
//      switch (switchDirection)
//      {
//        case SwitchDirection.Next:
//          if (tabControlManager.CurrentTab.Index < tabControlManager.CurrentTab.Paths.Count - 1)
//            SetCurrentImage(tabControlManager.CurrentTab.Index += 1);
//          else
//            SetCurrentImage(0);
//
//          break;
//
//        case SwitchDirection.Previous:
//          if (tabControlManager.CurrentTab.Paths.Any())
//            if (tabControlManager.CurrentTab.Index > 0)
//              SetCurrentImage(tabControlManager.CurrentTab.Index -= 1);
//            else
//              SetCurrentImage(tabControlManager.CurrentTab.Index = tabControlManager.CurrentTab.Paths.Count - 1);
//
//          break;
//      }
//
//      tabControlManager.CurrentTab.ResetView();
//    }

        void ViewInExplorer(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;

            Process.Start("explorer.exe", "/select, " + tabControlManager.CurrentTab.Path);
        }

        void AlwaysOnTopClick(object sender, RoutedEventArgs e)
        {
            if (tabControlManager.CurrentTab == null) return;

            Topmost = !Topmost;
            AlwaysOnTopUi.IsChecked = Topmost;
        }

        void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Left = Settings.Default.WindowLocation.X;
            Top = Settings.Default.WindowLocation.Y;

            changingSize = true;
            Width = Settings.Default.WindowSize.Width;
            Height = Settings.Default.WindowSize.Height;

            WindowState = (WindowState) Settings.Default.WindowState;
            changingSize = false;

            e.Handled = true;
        }

        void WindowClosing(object sender, CancelEventArgs e)
        {
            Settings.Default.WindowLocation = new Point((int) Left, (int) Top);
            Settings.Default.WindowState = (int) WindowState;
            var newSize = new Size
            {
                Width = WindowState == WindowState.Normal ? (int) Width : (int) RestoreBounds.Width,
                Height = WindowState == WindowState.Normal ? (int) Height : (int) RestoreBounds.Height
            };
            Settings.Default.WindowSize = newSize;

            Settings.Default.Save();
        }

        void WindowClosed(object sender, EventArgs e)
        {
            Dispose();
            if (App.GetMainWindows().Count == 0)
            {
                Current.Shutdown();
            }
        }

        void AboutClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                var rect = Screen.GetWorkingArea(new Point((int) Left, (int) Top));
                App.AboutDialog.Top = rect.Top + ActualHeight / 2.0 - App.AboutDialog.Height / 2.0;
                App.AboutDialog.Left = rect.Left + ActualWidth / 2.0 - App.AboutDialog.Width / 2.0;
            }
            else
            {
                App.AboutDialog.Top = Top + ActualHeight / 2.0 - App.AboutDialog.Height / 2.0;
                App.AboutDialog.Left = Left + ActualWidth / 2.0 - App.AboutDialog.Width / 2.0;
            }

            App.AboutDialog.ShowDialog();
        }

        void ImageAreaDragDrop(object sender, DragEventArgs e)
        {
            var filenames = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
            if (filenames == null)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject current)
            {
                while (!(VisualTreeHelper.GetParent(current) is TabablzControl))
                {
                    current = VisualTreeHelper.GetParent(current);
                }

                var tabablzControl = VisualTreeHelper.GetParent(current) as TabablzControl;
                (tabablzControl?.SelectedItem as TabItemControl)?.ImagePresenter.ScrollViewer.Focus();
            }


            var supportedFilenames = FilesManager.FilterSupportedFiles(filenames);
            if (!supportedFilenames.Any())
            {
                return;
            }

            if (supportedFilenames.Length > 1)
            {
                foreach (var filename in supportedFilenames)
                {
                    AddNewTab(filename);
                }
            }
            else
            {
                if (Settings.Default.ReplaceImageOnDrop)
                {
                    ReplaceImageInTab(supportedFilenames[0]);
                }
                else
                {
                    AddNewTab(supportedFilenames[0]);
                }
            }

//            DisplayImage();

            Keyboard.Focus(this);

            e.Handled = true;
        }

        public void ImageAreaKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RawKeyHandling(e);
            ValidatedKeyHandling(e);
            Keyboard.Focus(this);
            e.Handled = true;
        }

        void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                ImageAreaKeyDown(sender, e);
            }
            catch (ArgumentException)
            {
            }
            finally
            {
                e.Handled = true;
            }
        }

        void TileImageOnClick(object sender, RoutedEventArgs e)
        {
            TileImage();
        }

        void ChannelsMontageOnClick(object sender, RoutedEventArgs e)
        {
            ChannelsMontage();
        }

        void OptionsOnClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                var rect = Screen.GetWorkingArea(new Point((int) Left, (int) Top));
                App.OptionsDialog.Top = rect.Top + ActualHeight / 2.0 - App.OptionsDialog.Height / 2.0;
                App.OptionsDialog.Left = rect.Left + ActualWidth / 2.0 - App.OptionsDialog.Width / 2.0;
            }
            else
            {
                App.OptionsDialog.Top = Top + ActualHeight / 2.0 - App.OptionsDialog.Height / 2.0;
                App.OptionsDialog.Left = Left + ActualWidth / 2.0 - App.OptionsDialog.Width / 2.0;
            }

            App.OptionsDialog.ShowDialog();
        }

        void CheckForUpdateOnClick(object sender, RoutedEventArgs e)
        {
            CheckForUpdates();
        }

        static void CheckForUpdates()
        {
            AutoUpdater.Start("http://www.dropbox.com/s/2b0gna7rz889b5u/Update.xml?dl=1");
        }

        void ResetViewClick(object sender, RoutedEventArgs e)
        {
            if (tabControlManager.CanExcectute()) tabControlManager.CurrentTab.ResetView();
        }

        void OpenFilesClick(object sender, RoutedEventArgs e)
        {
            if (tabControlManager.CurrentTab == null || e.OriginalSource is ScrollViewer ||
                e.OriginalSource is MenuItem)
            {
                FileBrowser();
                Keyboard.Focus(this);
            }
        }

        public void Dispose()
        {
            imageDirectoryWatcher?.Dispose();
            parentDirectoryWatcher?.Dispose();
        }

        void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (changingSize)
            {
                return;
            }

            Settings.Default.WindowState = (int) WindowState;
            var newSize = new Size
            {
                Width = WindowState == WindowState.Normal ? (int) Width : (int) RestoreBounds.Width,
                Height = WindowState == WindowState.Normal ? (int) Height : (int) RestoreBounds.Height
            };
            Settings.Default.WindowSize = newSize;

            Settings.Default.Save();
        }

        void DisplayAllChannels(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel = Channels.RGB;
        }

        void DisplayRedChannel(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Red ? Channels.RGB : Channels.Red;
        }

        void DisplayGreenChannel(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Green
                    ? Channels.RGB
                    : Channels.Green;
        }

        void DisplayBlueChannel(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Blue
                    ? Channels.RGB
                    : Channels.Blue;
        }

        void DisplayAlphaChannel(object sender, RoutedEventArgs e)
        {
            if (!tabControlManager.CanExcectute()) return;
            tabControlManager.CurrentTab.ImageSettings.DisplayChannel =
                tabControlManager.CurrentTab.ImageSettings.DisplayChannel == Channels.Alpha
                    ? Channels.RGB
                    : Channels.Alpha;
        }

        void OnClickToggleBars(object sender, RoutedEventArgs e)
        {
            ToggleBars();
        }

        void ToggleBars()
        {
            if (!tabControlManager.CanExcectute()) return;
            FooterVisibility = tabControlManager.CurrentTab.Footer.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            WindowStyle = FooterVisibility != Visibility.Visible ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            foreach (var tabControl in tabControlManager.TabControls)
            {
                tabControl.IsHeaderPanelVisible = FooterVisibility == Visibility.Visible;
                foreach (TabItemControl tabItemControl in tabControl.Items)
                {
                    var scrollViewer = tabItemControl.ImagePresenter.ScrollViewer;
                    scrollViewer.VerticalScrollBarVisibility = FooterVisibility != Visibility.Visible
                        ? ScrollBarVisibility.Hidden
                        : ScrollBarVisibility.Auto;
                    scrollViewer.HorizontalScrollBarVisibility = FooterVisibility != Visibility.Visible
                        ? ScrollBarVisibility.Hidden
                        : ScrollBarVisibility.Auto;
                    tabItemControl.Footer.Visibility = FooterVisibility;
                }
            }
        }

        void ImageTabControl_OnIsDraggingWindowChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
        }
    }
}