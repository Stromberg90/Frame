//TODO Show hotkey next to menuitem
//TODO Data bindings
//TODO Single instance window, where it opens files in the current instance of the program.
//TODO GIF Support
//TODO Loading images without lag
//TODO Split into more files
//TODO Progress bar when loading large images
//TODO Options window, maybe I can add the hotkeys in there as well
//TODO Thumbnail using the render size first, then I can load in the real image in the background and replace.
//TODO Read sort setting from file explorer?
//TODO Mip toggle
//TODO Tiling image toggle

// BUG: There is sometimes a bug, where when changing between tabs it will change the image.

using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        readonly TabControlManager tabControlManager;
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

        static System.Windows.Threading.DispatcherTimer slideshowTimer;

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
                .TextAlignment(ImageMagick.TextAlignment.Center)
                .Text(256, 256, $"Could not load\n{Path.GetFileName(filepath)}")
                .Draw(image);

            return image;
        }

        void CloseTabIndex(TabData data)
        {
            var currentlySelectedItem = ImageTabControl.SelectedItem;
            var currentlySelectedIndex = ImageTabControl.SelectedIndex;
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
                if (currentlySelectedIndex != newIndex)
                {
                    ImageTabControl.SelectedItem = currentlySelectedItem;
                }
            }
        }

        void ValidatedKeyHandling(System.Windows.Forms.KeyEventArgs e)
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.A:
                {
                    ToggleDisplayChannel(Channels.Alpha);
                    break;
                }
                case System.Windows.Forms.Keys.R:
                {
                    ToggleDisplayChannel(Channels.Red);
                    break;
                }
                case System.Windows.Forms.Keys.G:
                {
                    ToggleDisplayChannel(Channels.Green);
                    break;
                }
                case System.Windows.Forms.Keys.B:
                {
                    ToggleDisplayChannel(Channels.Blue);
                    break;
                }
                case System.Windows.Forms.Keys.F:
                {
                    ResetView();
                    break;
                }
                case System.Windows.Forms.Keys.D:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        DuplicateTab();
                    }
                    break;
                }
                case System.Windows.Forms.Keys.W:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        CloseTab();
                    }
                    break;
                }
                case System.Windows.Forms.Keys.S:
                {
                    ToggleSlideshow();
                    break;
                }
                case System.Windows.Forms.Keys.Right:
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
                case System.Windows.Forms.Keys.Left:
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
                case System.Windows.Forms.Keys.Space:
                {
                    SwitchImage(SwitchDirection.Next);
                    break;
                }
                case System.Windows.Forms.Keys.Delete:
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
                slideshowTimer.Start();
            }
            else
            {
                slideshowTimer.Stop();
            }
            ImageViewerWm.CurrentTab.UpdateTitle();
            ImageViewerWm.CurrentTab.CurrentSlideshowTime = 1;
            UpdateFooter();
        }

        void DeleteImage()
        {
            var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                $"{Properties.Resources.Delete}{FileSystem.GetName(ImageViewerWm.CurrentTab.Path)}",
                MessageBoxButton.YesNo);

            if (res != MessageBoxResult.Yes) return;

            FileSystem.DeleteFile(ImageViewerWm.CurrentTab.Path, UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin);

            if (ImageViewerWm.CurrentTab.Paths.Count > 0)
            {
                filesManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWm.CurrentTab.Path));

                SwitchImage(SwitchDirection.Next);
            }
            else
            {
                if (FileBrowser())
                {
                    DisplayImage();
                }
            }
        }

        void RawKeyHandling(System.Windows.Forms.KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Escape:
                {
                    Close();
                    break;
                }
                case System.Windows.Forms.Keys.N:
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        AddNewTab(string.Empty);
                    }
                    break;
                }
            }
        }

        void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                var fileDialog = ImageViewerWm.ShowOpenFileDialog();
                filepath = fileDialog.FileName;
            }

            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }

            tabControlManager.AddTab(filepath);

            var folderPath = Path.GetDirectoryName(filepath);
            filesManager.SupportedFiles(folderPath);

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
                var initalImage = ImageViewerWm.CurrentTab.Path;
                var filePathsList = ImageViewerWm.CurrentTab.Paths;
                filePathsList.Reverse();
                sortingManager.FindImageAfterSort(filePathsList, initalImage);
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
                var inital_image = ImageViewerWm.CurrentTab.Path;
                var file_paths_list = ImageViewerWm.CurrentTab.Paths;
                file_paths_list.Reverse();
                sortingManager.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWm.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
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

            var image = LoadImage(ImageViewerWm.CurrentTab.Path);

            ImageArea.Image = image;

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
                if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow)
                {
                    FooterModeText.Text = $"Mode: {ImageViewerWm.CurrentTab.Mode} " +
                                          ImageViewerWm.CurrentTab.CurrentSlideshowTime;
                }
                else
                {
                    FooterModeText.Text = $"Mode: {ImageViewerWm.CurrentTab.Mode}";
                }
                {
                    FooterSizeText.Text = $"Size: {ImageViewerWm.CurrentTab.Width}x{ImageViewerWm.CurrentTab.Height}";

                    var channel = string.Empty;
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
                {
                    if (ImageViewerWm.CurrentTab.Size < 1024)
                    {
                        FooterFilesizeText.Text = $"Filesize: {ImageViewerWm.CurrentTab.Size}Bytes";
                    }
                    else if (ImageViewerWm.CurrentTab.Size < 1048576)
                    {
                        var filesize = (double) (ImageViewerWm.CurrentTab.Size / 1024f);
                        FooterFilesizeText.Text = $"Filesize: {filesize:N2}KB";
                    }
                    else
                    {
                        var filesize = (double) (ImageViewerWm.CurrentTab.Size / 1024f) / 1024f;
                        FooterFilesizeText.Text = $"Filesize: {filesize:N2}MB";
                    }
                }
                FooterZoomText.Text = $"Zoom: {ImageArea.Zoom}%";
                FooterIndexText.Text =
                    $"Index: {ImageViewerWm.CurrentTab.Index + 1}/{ImageViewerWm.CurrentTab.Paths.Count}";
            }
        }

        void DuplicateTab()
        {
            if (!ImageViewerWm.CanExcectute())
            {
                return;
            }

            var folderPath = Path.GetDirectoryName(ImageViewerWm.CurrentTab.Path);
            var tab = new TabData(folderPath, ImageViewerWm.CurrentTab.Index)
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

        bool FileBrowser()
        {
            var fileDialog = ImageViewerWm.ShowOpenFileDialog();
            if (string.IsNullOrEmpty(fileDialog.SafeFileName))
                return false;
            var filename = Path.GetFullPath(fileDialog.FileName);
            ReplaceImageInTab(filename);

            return true;
        }

        void ImageTabControl_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[]) e.Data.GetData("FileName");
            AddNewTab(Path.GetFullPath(filenames[0]));
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
                slideshowTimer.Start();
            }

            var folderPath = Path.GetDirectoryName(ImageViewerWm.Tabs[ImageTabControl.SelectedIndex].InitialImagePath);
            var initPath = ImageViewerWm.CurrentTab.Paths[(ImageViewerWm.CurrentTab.Index)];
            filesManager.SupportedFiles(folderPath);
            sortingManager.FindImageAfterSort(ImageViewerWm.CurrentTab.Paths, initPath);


            UpdateView();
        }

        System.Drawing.Image LoadImage(string filepath)
        {
            MagickImage image;
            MagickImageCollection imageCollection;


            try
            {
                if (Path.GetExtension(filepath) == ".gif")
                {
                    imageCollection = new MagickImageCollection(filepath);
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
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ImageEditor))
            {
                if (File.Exists(Properties.Settings.Default.ImageEditor))
                {
                    Process.Start(Properties.Settings.Default.ImageEditor, ImageViewerWm.CurrentTab.Path);
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
            Properties.Settings.Default.Save();
        }

        void RefreshImage()
        {
            if (!ImageViewerWm.CurrentTab.IsValid) return;

            var image = LoadImage(ImageViewerWm.CurrentTab.Path);

            ImageArea.Image = image;
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
            ImageArea.GridColor = System.Drawing.Color.FromArgb(255, Properties.Settings.Default.BackgroundColor.R,
                Properties.Settings.Default.BackgroundColor.G, Properties.Settings.Default.BackgroundColor.B);
        }

        void ReplaceImageInTab(string filename)
        {
            var folderPath = Path.GetDirectoryName(filename);
            if (ImageViewerWm.CurrentTabIndex <= 0)
            {
                AddNewTab(filename);
            }
            ImageViewerWm.CurrentTab.InitialImagePath = filename;
            filesManager.SupportedFiles(folderPath);

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

            imageDirectoryWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath),
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
                Path = Directory.GetParent(Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath)).FullName,
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
                filesManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath));
            imageDirectoryWatcher.Created += (sender, args) =>
                filesManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath));
            imageDirectoryWatcher.Deleted += (sender, args) =>
                filesManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath));
            imageDirectoryWatcher.Renamed += (sender, args) =>
                filesManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWm.CurrentTab.InitialImagePath));

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
                        //Hangs the program
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
            slideshowTimer = new System.Windows.Threading.DispatcherTimer();
            slideshowTimer.Tick += Slideshow;
            slideshowTimer.Interval = new TimeSpan(0, 0, 1);
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
                slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (ImageViewerWm.CurrentTab.Mode == ApplicationMode.Slideshow) return;

            slideshowTimer.Stop();
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
            slideshowTimer.Start();
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
            slideshowTimer.Stop();
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
            Properties.Settings.Default.Reset();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = Properties.Settings.Default.WindowLocation.X;
            Top = Properties.Settings.Default.WindowLocation.Y;

            Width = Properties.Settings.Default.WindowSize.Width;
            Height = Properties.Settings.Default.WindowSize.Height;

            WindowState = (WindowState) Properties.Settings.Default.WindowState;

            e.Handled = true;
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var location = new System.Drawing.Point((int) Left, (int) Top);
            Properties.Settings.Default.WindowLocation = location;
            Properties.Settings.Default.WindowState = (int) WindowState;
            if (WindowState == WindowState.Normal)
            {
                var size = new System.Drawing.Size((int) Width, (int) Height);
                Properties.Settings.Default.WindowSize = size;
            }
            else
            {
                var size = new System.Drawing.Size((int) RestoreBounds.Width, (int) RestoreBounds.Height);
                Properties.Settings.Default.WindowSize = size;
            }

            Properties.Settings.Default.Save();
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
                    if (Properties.Settings.Default.ReplaceImageOnDrop)
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

        void ImageArea_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            RawKeyHandling(e);
            ValidatedKeyHandling(e);
            e.Handled = true;
        }

        void ImageArea_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e == null || e.Button != System.Windows.Forms.MouseButtons.Left) return;
            if (!FileBrowser()) return;
            DisplayImage();
            ResetView();
        }

        void ImageArea_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (WinFormsHost.ContextMenu != null)
            {
                WinFormsHost.ContextMenu.IsOpen |= e.Button == System.Windows.Forms.MouseButtons.Right;
            }
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var keyConverter = new System.Windows.Forms.KeysConverter();
            System.Windows.Forms.Keys key;
            try
            {
                key = (System.Windows.Forms.Keys) keyConverter.ConvertFromString(e.Key.ToString());
            }
            catch (ArgumentException)
            {
                return;
            }

            var convertedKeyEventArgs = new System.Windows.Forms.KeyEventArgs(key);
            ImageArea_KeyDown(sender, convertedKeyEventArgs);
            e.Handled = true;
        }

        void WinFormsHost_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var filePath = Environment.GetCommandLineArgs()[1];
                AddNewTab(filePath);
            }
        }

        void ImageArea_ZoomChanged(object sender, EventArgs e)
        {
            UpdateFooter();
        }
    }
}