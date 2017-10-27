//TODO Data bindings
//TODO Option to always 100% Zoom
//TODO Better close icon
//TODO Show hotkey next to menuitem
//TODO GIF Support
//TODO Loading images without lag
//TODO Split into more files
//TODO Progress bar when loading large images
//TODO Options window, maybe I can add the hotkeys in there as well
//TODO Thumbnail using the render size first, then I can load in the real image in the background and replace.
//TODO Read sort setting from file explorer?
//TODO Mip toggle
//TODO Tiling image toggle
//TODO Auto Update

//CHANGLOG
    //Multiselect Files in Explorer
    //Single Instance

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Frame.Properties;
using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using Image = System.Drawing.Image;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using TextAlignment = ImageMagick.TextAlignment;

namespace Frame
{
    public partial class MainWindow
    {
        public Channels DisplayChannel
        {
            get => ImageViewerWm.CurrentTab.ImageSettings.DisplayChannel;

            set
            {
                ImageViewerWm.CurrentTab.ImageSettings.DisplayChannel = value;
                RefreshImage();
            }
        }

        public readonly TabControlManager tabControlManager;
        readonly SortingManager sortingManager;
        readonly FilesManager filesManager;
        FileSystemWatcher imageDirectoryWatcher;
        FileSystemWatcher parentDirectoryWatcher;

        public MainWindow()
        {
            InitializeComponent();
            tabControlManager = new TabControlManager(ImageTabControl, ImageViewerWm, ImageArea);
            sortingManager = new SortingManager(ImageViewerWm);
            filesManager = new FilesManager(sortingManager, ImageViewerWm);
            /*
            if (procs.Length > 1)
            {
                var sinfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true
                };
                Process.GetCurrentProcess().StartInfo = sinfo;
                Application.Current.Shutdown();
                var inp = procs[procs.Length - 1].StandardInput;
                // procs[procs.Length - 1].Start();
                inp.WriteLine(true);
            }
            else
            {
                var sinfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true
                };
                Process.GetCurrentProcess().StartInfo = sinfo;
            }*/

            RefreshUi();
            SetupSlideshow();
            UpdateFooter();
        }

        static DispatcherTimer _slideshowTimer;

        readonly About aboutDialog = new About();

        Process[] procs = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);

        ImageViewerWm ImageViewerWm { get; } = new ImageViewerWm();
        static string BackwardToForwardSlash(string v) => v.Replace('\\', '/');

        static MagickImage ErrorImage(string filepath)
        {
            var image = new MagickImage(MagickColors.White, 512, 512);
            new Drawables()
                .FontPointSize(18)
                .Font("Arial")
                .FillColor(MagickColors.Red)
                .TextAlignment(TextAlignment.Center)
                .Text(256, 256, $"Could not load\n{Path.GetFileName(filepath)}")
                .Draw(image);

            return image;
        }

        void CloseTabIndex(TabData data)
        {
            var newIndex = ImageTabControl.Items.IndexOf(data.tabItem);
            if (newIndex < 0)
            {
                CloseTab();
            }
            else
            {
                ImageViewerWm.CurrentTabIndex = newIndex;
                ImageTabControl.SelectedIndex = newIndex;
                CloseTab();
                if (ImageTabControl.SelectedIndex != newIndex)
                {
                    ImageTabControl.SelectedItem = ImageTabControl.SelectedItem;
                }
            }
        }

        void ValidatedKeyHandling(KeyEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.A:
                {
                    ToggleDisplayChannel(Channels.Alpha);
                    break;
                }
                case Keys.R:
                {
                    ToggleDisplayChannel(Channels.Red);
                    break;
                }
                case Keys.G:
                {
                    ToggleDisplayChannel(Channels.Green);
                    break;
                }
                case Keys.B:
                {
                    ToggleDisplayChannel(Channels.Blue);
                    break;
                }
                case Keys.F:
                {
                    ResetView();
                    break;
                }
                case Keys.D:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        DuplicateTab();
                    }
                    break;
                }
                case Keys.W:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        CloseTab();
                    }
                    break;
                }
                case Keys.S:
                {
                    ToggleSlideshow();
                    break;
                }
                case Keys.Right:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (ImageTabControl.SelectedIndex != ImageTabControl.Items.Count - 1)
                        {
                            ImageTabControl.SelectedIndex += 1;
                        }
                    }
                    else
                    {
                        SwitchImage(SwitchDirection.Next);
                    }
                    break;
                }
                case Keys.Left:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (ImageTabControl.SelectedIndex > 0)
                        {
                            ImageTabControl.SelectedIndex -= 1;
                        }
                    }
                    else
                    {
                        SwitchImage(SwitchDirection.Previous);
                    }
                    break;
                }
                case Keys.Space:
                {
                    SwitchImage(SwitchDirection.Next);
                    break;
                }
                case Keys.Delete:
                {
                    DeleteImage();
                    break;
                }
            }
        }

        void ToggleSlideshow()
        {
            if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                ImageViewerWm.CurrentTab.Mode = ApplicationMode.Normal;
            }
            else
            {
                ImageViewerWm.CurrentTab.Mode = ApplicationMode.Slideshow;
            }
            if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                _slideshowTimer.Start();
            }
            else
            {
                _slideshowTimer.Stop();
            }
            ImageViewerWm.CurrentTab.UpdateTitle();
            ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
            UpdateFooter();
        }

        void DeleteImage()
        {
            var result = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                $"{Properties.Resources.Delete}{FileSystem.GetName(ImageViewerWm.CurrentTab.Path)}",
                MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes) return;

            FileSystem.DeleteFile(ImageViewerWm.CurrentTab.Path, UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin);

            if (ImageViewerWm.CurrentTab.Paths.Count > 0)
            {
                filesManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWm.CurrentTab.Path));

                SwitchImage(SwitchDirection.Next);
            }
            else
            {
                FileBrowser();
            }
        }

        void RawKeyHandling(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                {
                    Close();
                    break;
                }
                case Keys.N:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        AddNewTab(string.Empty);
                    }
                    break;
                }
            }
        }

        public void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                filepath = ImageViewerWm.ShowOpenFileDialog().FileName;
            }

            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }

            if (!FilesManager.ValidFile(filepath)) return;

            tabControlManager.AddTab(filepath);

            filesManager.SupportedFiles(Path.GetDirectoryName(filepath));

            var filenameIndex =
                ImageViewerWm.CurrentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

            ImageViewerWm.CurrentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

            ImageViewerWm.CurrentTab.InitialImagePath = filepath;

            UpdateView();
            SetupDirectoryWatcher();
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            if (ImageViewerWm.CurrentTab.ImageSettings.CurrentSortMode == SortMode.Descending)
            {
                ReversePaths();
            }
            ImageViewerWm.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void CloseTab()
        {
            tabControlManager.CloseSelectedTab();
            UpdateFooter();
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(ImageViewerWm.CurrentTab.Path)}\"");
        }

        void CopyFilenameToClipboard(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(Path.GetFileName(ImageViewerWm.CurrentTab.Path))}\"");
        }

        void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            if (ImageViewerWm.CurrentTab.ImageSettings.CurrentSortMode == SortMode.Ascending)
            {
                ReversePaths();
            }
            ImageViewerWm.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        void ReversePaths()
        {
            var initalImage = ImageViewerWm.CurrentTab.Path;
            var filePathsList = ImageViewerWm.CurrentTab.Paths;
            filePathsList.Reverse();
            sortingManager.FindImageAfterSort(filePathsList, initalImage);
        }

        void Display_all_channels(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.RGB);
        }

        void Display_alpha_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Alpha);
        }

        void Display_blue_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Blue);
        }

        void Display_green_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Green);
        }

        void Display_red_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Red);
        }

        void DisplayImage()
        {
            if (ImageViewerWm.CurrentTabIndex < 0 || ImageViewerWm.CurrentTab.Index == -1)
            {
                return;
            }

            if (ImageArea == null || !ImageViewerWm.CurrentTab.IsValid) return;

            ImageArea.Image = LoadImage(ImageViewerWm.CurrentTab.Path);

            ImageViewerWm.CurrentTab.UpdateTitle();
            UpdateFooter();
        }

        void UpdateFooter()
        {
            if (ImageViewerWm.CurrentTabIndex == -1 || !ImageViewerWm.CanExcectute())
            {
                FooterModeText.Text = "Mode: ";
                FooterSizeText.Text = "Size: ";
                FooterChannelsText.Text = "Channels: ";
                FooterFilesizeText.Text = "Filesize: ";
                FooterZoomText.Text = "Zoom: ";
                FooterIndexText.Text = "Index: ";
            }
            else
            {
                var channel = string.Empty;

                FooterModeText.Text = ImageViewerWm.CurrentTab.FooterMode;
                FooterSizeText.Text = ImageViewerWm.CurrentTab.FooterSize;
                FooterFilesizeText.Text = ImageViewerWm.CurrentTab.FooterFilesize;
                FooterIndexText.Text = ImageViewerWm.CurrentTab.FooterIndex;
                {
                    switch (DisplayChannel)
                    {
                        case (Channels.RGB):
                        {
                            channel = DisplayChannel.ToString();
                            break;
                        }
                        case (Channels.Red):
                        {
                            channel = "Red";
                            break;
                        }
                        case (Channels.Green):
                        {
                            channel = DisplayChannel.ToString();
                            break;
                        }
                        case (Channels.Blue):
                        {
                            channel = DisplayChannel.ToString();
                            break;
                        }
                        case (Channels.Opacity):
                        {
                            channel = "Alpha";
                            break;
                        }
                    }
                    FooterChannelsText.Text = $"Channels: {channel}";
                }
                FooterZoomText.Text = $"Zoom: {ImageArea.Zoom}%";                  
            }
        }

        void DuplicateTab()
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            var tab = new TabData(Path.GetDirectoryName(ImageViewerWm.CurrentTab.Path), ImageViewerWm.CurrentTab.Index)
            {
                InitialImagePath = ImageViewerWm.CurrentTab.InitialImagePath,
                Paths = ImageViewerWm.CurrentTab.Paths,
                CloseTabAction = CloseTabIndex,
                ImageSettings = new ImageSettings
                {
                    DisplayChannel = ImageViewerWm.CurrentTab.ImageSettings.DisplayChannel,
                    CurrentSortMode = ImageViewerWm.CurrentTab.ImageSettings.CurrentSortMode
                }
            };


            ImageViewerWm.Tabs.Insert(ImageViewerWm.CurrentTabIndex + 1, tab);

            ImageTabControl.Items.Insert(ImageViewerWm.CurrentTabIndex + 1, tab.tabItem);

            ImageTabControl.SelectedIndex = ImageViewerWm.CurrentTabIndex + 1;
        }

        void FileBrowser()
        {
            var fileDialog = ImageViewerWm.ShowOpenFileDialog();
            if (!fileDialog.SafeFileNames.Any())
                return;

            foreach (var fileName in fileDialog.FileNames)
            {
                AddNewTab(Path.GetFullPath(fileName));
            }
        }

        void ImageTabControl_Drop(object sender, DragEventArgs e)
        {
            //BUG I suspect the other drop event is stealing it's lunch.
            if (e?.Data?.GetData("FileName") is string[] filenames)
            {
                AddNewTab(Path.GetFullPath(filenames[0]));
            }
        }

        void ImageTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageViewerWm.CurrentTabIndex = ImageTabControl.SelectedIndex;

            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                _slideshowTimer.Start();
            }

            var folderPath = Path.GetDirectoryName(ImageViewerWm.Tabs[ImageTabControl.SelectedIndex].InitialImagePath);
            var initPath = ImageViewerWm.CurrentTab.Paths[(ImageViewerWm.CurrentTab.Index)];
            filesManager.SupportedFiles(folderPath);
            sortingManager.FindImageAfterSort(ImageViewerWm.CurrentTab.Paths, initPath);
            UpdateView();
        }

        Image LoadImage(string filepath)
        {
            MagickImage image;
            try
            {
                if (Path.GetExtension(filepath) == ".gif")
                {
                    var imageCollection = new MagickImageCollection(filepath);
                    image = (MagickImage) imageCollection[0];
                }
                else
                {
                    image = new MagickImage(filepath);
                }
            }
            catch (MagickCoderErrorException)
            {
                image = ErrorImage(filepath);
            }
            catch (MagickMissingDelegateErrorException)
            {
                image = ErrorImage(filepath);
            }
            finally
            {
                GC.Collect();
            }

            ImageViewerWm.CurrentTab.Size = image.FileSize;
            ImageViewerWm.CurrentTab.Width = image.Width;
            ImageViewerWm.CurrentTab.Height = image.Height;

            switch (DisplayChannel)
            {
                case Channels.Red:
                {
                    return image.Separate(Channels.Red).ElementAt(0)?.ToBitmap();
                }
                case Channels.Green:
                {
                    return image.Separate(Channels.Green).ElementAt(0)?.ToBitmap();
                }
                case Channels.Blue:
                {
                    return image.Separate(Channels.Blue).ElementAt(0)?.ToBitmap();
                }
                case Channels.Alpha:
                {
                    return image.Separate(Channels.Alpha).ElementAt(0)?.ToBitmap();
                }
                default:
                {
                    image.Alpha(AlphaOption.Opaque);
                    return image.ToBitmap();
                }
            }
        }

        void OpenInImageEditor(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }
            if (!string.IsNullOrEmpty(Settings.Default.ImageEditor))
            {
                if (File.Exists(Settings.Default.ImageEditor))
                {
                    Process.Start(Settings.Default.ImageEditor, ImageViewerWm.CurrentTab.Path);
                    return;
                }
                if (MessageBox.Show("Editor not found\nDo you want to browse for editor?",
                        Properties.Resources.FileMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageViewerWm.ImageEditorBrowse();
                }
            }
            else
            {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?",
                        Properties.Resources.ImageEditorMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageViewerWm.ImageEditorBrowse();
                }
            }
            Settings.Default.Save();
        }

        void RefreshImage()
        {
            if (!ImageViewerWm.CurrentTab.IsValid) return;

            ImageArea.Image = LoadImage(ImageViewerWm.CurrentTab.Path);
            ImageViewerWm.CurrentTab.UpdateTitle();
            UpdateFooter();
        }

        void UpdateView()
        {
            DisplayImage();
            ResetView();
        }

        void RefreshUi()
        {
            ImageArea.GridColor = Color.FromArgb(255, Settings.Default.BackgroundColor.R,
                Settings.Default.BackgroundColor.G, Settings.Default.BackgroundColor.B);
        }

        void ReplaceImageInTab(string filename)
        {
            if (!FilesManager.ValidFile(filename)) return;

            if (ImageViewerWm.CurrentTabIndex <= 0)
            {
                AddNewTab(filename);
            }
            ImageViewerWm.CurrentTab.InitialImagePath = filename;
            filesManager.SupportedFiles(Path.GetDirectoryName(filename));

            var filenameIndex = ImageViewerWm.CurrentTab.Paths.IndexOf(filename);
            ImageViewerWm.CurrentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

            SetupDirectoryWatcher();
            ResetView();
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
        }

        void ResetView()
        {
            if (ImageArea.Size.Width < ImageViewerWm.CurrentTab.Width ||
                ImageArea.Size.Height < ImageViewerWm.CurrentTab.Height)
            {
                ImageArea.ZoomToFit();
            }
            else
            {
                ImageArea.Zoom = 100;
            }
        }

        void SetCurrentImage(int newIndex)
        {
            ImageViewerWm.CurrentTab.Index = newIndex;
            DisplayImage();
        }

        void SetDisplayChannel(Channels channel)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            switch (channel)
            {
                case Channels.RGB:
                {
                    DisplayChannel = Channels.RGB;
                    break;
                }
                case Channels.Red:
                {
                    DisplayChannel = Channels.Red;
                    break;
                }
                case Channels.Green:
                {
                    DisplayChannel = Channels.Green;
                    break;
                }
                case Channels.Blue:
                {
                    DisplayChannel = Channels.Blue;
                    break;
                }
                case Channels.Alpha:
                {
                    DisplayChannel = Channels.Alpha;
                    break;
                }
            }
        }

        void ToggleDisplayChannel(Channels channel)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            switch (channel)
            {
                case Channels.RGB:
                {
                    DisplayChannel = Channels.RGB;
                    break;
                }
                case Channels.Red:
                {
                    DisplayChannel = DisplayChannel == Channels.Red ? Channels.RGB : Channels.Red;
                    break;
                }
                case Channels.Green:
                {
                    DisplayChannel = DisplayChannel == Channels.Green ? Channels.RGB : Channels.Green;
                    break;
                }
                case Channels.Blue:
                {
                    DisplayChannel = DisplayChannel == Channels.Blue ? Channels.RGB : Channels.Blue;
                    break;
                }
                case Channels.Alpha:
                {
                    DisplayChannel = DisplayChannel == Channels.Alpha ? Channels.RGB : Channels.Alpha;
                    break;
                }
            }
        }

        void SetupDirectoryWatcher()
        {
            if (imageDirectoryWatcher != null)
            {
                parentDirectoryWatcher.EnableRaisingEvents = true;
                imageDirectoryWatcher.Dispose();
                imageDirectoryWatcher = null;
            }

            var directoryName = Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath);
            imageDirectoryWatcher = new FileSystemWatcher
            {
                Path = directoryName,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            if (parentDirectoryWatcher != null)
            {
                parentDirectoryWatcher.EnableRaisingEvents = false;
                parentDirectoryWatcher.Dispose();
                parentDirectoryWatcher = null;
            }

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

            imageDirectoryWatcher.Changed += (sender, args) =>
                filesManager.SupportedFiles(directoryName);
            imageDirectoryWatcher.Created += (sender, args) =>
                filesManager.SupportedFiles(directoryName);
            imageDirectoryWatcher.Deleted += (sender, args) =>
                filesManager.SupportedFiles(directoryName);
            imageDirectoryWatcher.Renamed += (sender, args) =>
                filesManager.SupportedFiles(directoryName);

            imageDirectoryWatcher.EnableRaisingEvents = true;
        }

        void ParentDirectoryChanged(object sender, FileSystemEventArgs args)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Need to check all tabs
                switch (args.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                    {
                        if (Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath) == args.FullPath)
                        {
                            CloseTab();
                        }
                        break;
                    }
                    case WatcherChangeTypes.Changed:
                        break;
                    case WatcherChangeTypes.Renamed:
                    {
                        var renamedArgs = (RenamedEventArgs) args;
                        if (Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath) ==
                            renamedArgs.OldFullPath)
                        {
                            ImageViewerWm.CurrentTab.InitialImagePath = renamedArgs.FullPath;
                            CloseTab();
                        }
                        //BUG Hangs the program
                        break;
                        if (Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath) ==
                            renamedArgs.OldFullPath)
                        {
                            var newFile = Path.Combine(renamedArgs.FullPath,
                                Path.GetFileName(ImageViewerWm.CurrentTab.Path));
                            ReplaceImageInTab(newFile);
                        }
                        break;
                    }
                    case WatcherChangeTypes.All:
                        break;
                }
            });
        }

        void SetupSlideshow()
        {
            _slideshowTimer = new DispatcherTimer();
            _slideshowTimer.Tick += Slideshow;
            _slideshowTimer.Interval = new TimeSpan(0, 0, 1);
        }

        void Slideshow(object source, EventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                ImageViewerWm.CurrentTab.Mode = ApplicationMode.Normal;
                return;
            }

            if (ImageViewerWm.CurrentTab.CurrentSlideshowTime < ImageViewerWm.SlideshowInterval)
            {
                ImageViewerWm.CurrentTab.CurrentSlideshowTime += 1;
                UpdateFooter();
                ImageViewerWm.CurrentTab.UpdateTitle();
            }
            else
            {
                ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
                _slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                _slideshowTimer.Start();
            }

            if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow) return;

            _slideshowTimer.Stop();
            ImageViewerWm.CurrentTab.UpdateTitle();
            UpdateFooter();
            ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
        }

        void Slideshow10SecUI_Click(object sender, RoutedEventArgs e) =>
            SlideshowIntervalUi(SlideshowInterval.Seconds10);

        void Slideshow1SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Second1);

        void Slideshow20SecUI_Click(object sender, RoutedEventArgs e) =>
            SlideshowIntervalUi(SlideshowInterval.Seconds20);

        void Slideshow2SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds2);

        void Slideshow30SecUI_Click(object sender, RoutedEventArgs e) =>
            SlideshowIntervalUi(SlideshowInterval.Seconds30);

        void Slideshow3SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds3);

        void Slideshow4SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds4);

        void Slideshow5SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds5);

        void SlideshowIntervalUi(SlideshowInterval newInterval)
        {
            Slideshow1SecUi.IsChecked = false;
            Slideshow2SecUi.IsChecked = false;
            Slideshow3SecUi.IsChecked = false;
            Slideshow4SecUi.IsChecked = false;
            Slideshow5SecUi.IsChecked = false;
            Slideshow10SecUi.IsChecked = false;
            Slideshow20SecUi.IsChecked = false;
            Slideshow30SecUi.IsChecked = false;

            switch (newInterval)
            {
                case SlideshowInterval.Second1:
                {
                    Slideshow1SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 1;
                    break;
                }
                case SlideshowInterval.Seconds2:
                {
                    Slideshow2SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 2;
                    break;
                }
                case SlideshowInterval.Seconds3:
                {
                    Slideshow3SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 3;
                    break;
                }
                case SlideshowInterval.Seconds4:
                {
                    Slideshow4SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 4;
                    break;
                }
                case SlideshowInterval.Seconds5:
                {
                    Slideshow5SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 5;
                    break;
                }
                case SlideshowInterval.Seconds10:
                {
                    Slideshow10SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 10;
                    break;
                }
                case SlideshowInterval.Seconds20:
                {
                    Slideshow20SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 20;
                    break;
                }
                case SlideshowInterval.Seconds30:
                {
                    Slideshow30SecUi.IsChecked = true;
                    ImageViewerWm.SlideshowInterval = 30;
                    break;
                }
            }
        }

        void SortByDateModified(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            sortingManager.Sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void SortByName(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            sortingManager.Sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void SortBySize(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            sortingManager.Sort(SortMethod.Size);
            SortSize.IsChecked = true;
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void StartSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            ImageViewerWm.CurrentTab.Mode = ApplicationMode.Slideshow;
            ImageViewerWm.CurrentTab.UpdateTitle();
            ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
            _slideshowTimer.Start();
            StartSlideshowUi.IsEnabled = !StartSlideshowUi.IsEnabled;
            StopSlideshowUi.IsEnabled = !StartSlideshowUi.IsEnabled;
        }

        void StopSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            ImageViewerWm.CurrentTab.Mode = ApplicationMode.Normal;
            ImageViewerWm.CurrentTab.UpdateTitle();
            ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
            _slideshowTimer.Stop();
            StartSlideshowUi.IsEnabled = !StartSlideshowUi.IsEnabled;
            StopSlideshowUi.IsEnabled = !StartSlideshowUi.IsEnabled;
        }

        void SwitchImage(SwitchDirection switchDirection)
        {
            if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
            }

            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (ImageViewerWm.CurrentTab.Index < ImageViewerWm.CurrentTab.Paths.Count - 1)
                    {
                        SetCurrentImage(ImageViewerWm.CurrentTab.Index += 1);
                    }
                    else
                    {
                        SetCurrentImage(0);
                    }
                    break;

                case SwitchDirection.Previous:
                    if (ImageViewerWm.CurrentTab.Paths.Any())
                    {
                        if (ImageViewerWm.CurrentTab.Index > 0)
                        {
                            SetCurrentImage(ImageViewerWm.CurrentTab.Index -= 1);
                        }
                        else
                        {
                            SetCurrentImage(ImageViewerWm.CurrentTab.Index = ImageViewerWm.CurrentTab.Paths.Count - 1);
                        }
                    }
                    break;
            }
            ResetView();
        }

        void UIAddNewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab(string.Empty);
        }

        void ViewInExplorer(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            Process.Start("explorer.exe", "/select, " + ImageViewerWm.CurrentTab.Path);
        }

        void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            AlwaysOnTopUi.IsChecked = Topmost;
        }

        void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Reset();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = Settings.Default.WindowLocation.X;
            Top = Settings.Default.WindowLocation.Y;

            Width = Settings.Default.WindowSize.Width;
            Height = Settings.Default.WindowSize.Height;

            WindowState = (WindowState) Settings.Default.WindowState;

            e.Handled = true;
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.WindowLocation = new Point((int) Left, (int) Top);
            Settings.Default.WindowState = (int) WindowState;
            if (WindowState == WindowState.Normal)
            {
                Settings.Default.WindowSize = new Size((int) Width, (int) Height);
            }
            else
            {
                Settings.Default.WindowSize = new Size((int) RestoreBounds.Width, (int) RestoreBounds.Height);
            }

            Settings.Default.Save();
        }

        void About_Click(object sender, RoutedEventArgs e)
        {
            aboutDialog.Top = Top + (Height / 2.0) - (aboutDialog.Height / 2.0);
            aboutDialog.Left = Left + (Width / 2.0) - (aboutDialog.Width / 2.0);
            aboutDialog.ShowDialog();
        }

        void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void ImageArea_DragDrop(object sender, DragEventArgs e)
        {
            var filenames = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
            if (filenames != null)
            {
                if (filenames.Length > 1)
                {
                    foreach (var filename in filenames)
                    {
                        AddNewTab(filename);
                    }
                }
                else
                {
                    if (Settings.Default.ReplaceImageOnDrop)
                    {
                        ReplaceImageInTab(filenames[0]);
                    }
                    else
                    {
                        AddNewTab(filenames[0]);
                    }
                }
            }
            UpdateView();
        }

        void ImageArea_KeyDown(object sender, KeyEventArgs e)
        {
            RawKeyHandling(e);
            ValidatedKeyHandling(e);
            e.Handled = true;
        }

        void ImageArea_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e == null || e.Button != MouseButtons.Left) return;
            FileBrowser();
        }

        void ImageArea_MouseClick(object sender, MouseEventArgs e)
        {
            if (WinFormsHost.ContextMenu != null)
            {
                WinFormsHost.ContextMenu.IsOpen |= e.Button == MouseButtons.Right;
            }
        }

        void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Keys key;
            try
            {
                key = (Keys) new KeysConverter().ConvertFromString(e.Key.ToString());
            }
            catch (ArgumentException)
            {
                return;
            }

            ImageArea_KeyDown(sender, new KeyEventArgs(key));
            e.Handled = true;
        }

        void WinFormsHost_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length <= 1) return;

            foreach (var filePath in Environment.GetCommandLineArgs().Skip(1))
            {                    
                AddNewTab(filePath);
            }
        }

        void ImageArea_ZoomChanged(object sender, EventArgs e)
        {
            UpdateFooter();
        }
    }
}