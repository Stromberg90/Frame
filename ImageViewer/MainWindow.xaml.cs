// TODOS
// Show hotkey next to menuitem
// Data bindings
// Single instance window, where it opens files in the current instance of the program.
// GIF Support
// Loading images without lag
// Split into more files
// Progress bar when loading large images
// Options window, maybe I can add the hotkeys in there as well
// Can I make a thumbnail using the render size first, then I can load in the real image in the background and replace.
// Make a option where dragging the image creates a new tab instead of replacing it.
// Can I read sort setting from file explorer?
// Mip toggle
// Tiling image toggle

// BUGS:
// There is sometimes a bug, where when changing between tabs it will change the image.

using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using System;
using Optional;
using System.IO;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Frame
{
    public partial class MainWindow
    {
        public Channels DisplayChannel
        {
            get => ImageViewerWM.CurrentTab.ImageSettings.displayChannel;

            set
            {
                ImageViewerWM.CurrentTab.ImageSettings.displayChannel = value;
                RefreshImage();
            }
        }

        TabControlManager tabControlManager;
        SortingManager sortingManager;
        FileSystemWatcher imageDirectoryWatcher;
        FileSystemWatcher parentDirectoryWatcher;

        public MainWindow()
        {
            InitializeComponent();
            tabControlManager = new TabControlManager(ImageTabControl, ImageViewerWM, ImageArea);
            sortingManager = new SortingManager(ImageViewerWM);
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

            RefreshUI();
            SetupSlideshow();
            UpdateFooter();
        }

        static System.Windows.Threading.DispatcherTimer slideshowTimer;

        About aboutDialog = new About();

        Process[] procs = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);

        ImageViewerWM ImageViewerWM { get; set; } = new ImageViewerWM();
        static string BackwardToForwardSlash(string v) => v.Replace('\\', '/');

        static MagickImage ErrorImage(string filepath)
        {
            MagickImage image = new MagickImage(MagickColors.White, 512, 512);
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
            var currently_selected_item = ImageTabControl.SelectedItem;
            var currently_selected_index = ImageTabControl.SelectedIndex;
            int newIndex = ImageTabControl.Items.IndexOf(data.tabItem);
            if (newIndex < 0)
            {
                CloseTab();
            }
            else
            {
                ImageViewerWM.CurrentTabIndex = newIndex;
                ImageTabControl.SelectedIndex = newIndex;
                CloseTab();
                if (currently_selected_index != newIndex)
                {
                    ImageTabControl.SelectedItem = currently_selected_item;
                }
            }
        }

        void ValidatedKeyHandling(System.Windows.Forms.KeyEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
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
            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                ImageViewerWM.CurrentTab.Mode = ApplicationMode.Normal;
            }
            else
            {
                ImageViewerWM.CurrentTab.Mode = ApplicationMode.Slideshow;
            }
            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                slideshowTimer.Start();
            }
            else
            {
                slideshowTimer.Stop();
            }
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 1;
            UpdateFooter();
        }

        void DeleteImage()
        {
            var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                          $"{Properties.Resources.Delete}{FileSystem.GetName(ImageViewerWM.CurrentTab.Path)}", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                FileSystem.DeleteFile(ImageViewerWM.CurrentTab.Path, UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);

                if (ImageViewerWM.CurrentTab.Paths.Count > 0)
                {
                    sortingManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWM.CurrentTab.Path));

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
                var file_dialog = ImageViewerWM.ShowOpenFileDialog();
                filepath = file_dialog.FileName;
            }

            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }

            tabControlManager.AddTab(filepath);

            var folderPath = Path.GetDirectoryName(filepath);
            sortingManager.SupportedFiles(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            ImageViewerWM.CurrentTab.InitialImagePath = filepath;

            UpdateView();
            SetupDirectoryWatcher();
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode == SortMode.Descending)
            {
                var inital_image = ImageViewerWM.CurrentTab.Path;
                var file_paths_list = ImageViewerWM.CurrentTab.Paths;
                file_paths_list.Reverse();
                sortingManager.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Ascending;
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
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(ImageViewerWM.CurrentTab.Path)}\"");
        }

        void CopyFilenameToClipboard(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(Path.GetFileName(ImageViewerWM.CurrentTab.Path))}\"");
        }

        void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode == SortMode.Ascending)
            {
                var inital_image = ImageViewerWM.CurrentTab.Path;
                var file_paths_list = ImageViewerWM.CurrentTab.Paths;
                file_paths_list.Reverse();
                sortingManager.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Descending;
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
            if (ImageViewerWM.CurrentTabIndex < 0 || ImageViewerWM.CurrentTab.Index == -1)
            {
                return;
            }
            if (ImageArea != null)
            {
                if (ImageViewerWM.CurrentTab.IsValid())
                {
                    var image = LoadImage(ImageViewerWM.CurrentTab.Path);

                    ImageArea.Image = image;

                    ImageViewerWM.CurrentTab.UpdateTitle();
                    UpdateFooter();
                }
            }
        }

        void UpdateCheckboxes()
        {
            SetDisplayChannel(ImageViewerWM.CurrentTab.ImageSettings.displayChannel);
        }

        void UpdateFooter()
        {
            if (ImageViewerWM.CurrentTabIndex == -1 || !ImageViewerWM.CanExcectute())
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
                if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
                {
                    FooterModeText.Text = $"Mode: {ImageViewerWM.CurrentTab.Mode} " + ImageViewerWM.CurrentTab.CurrentSlideshowTime;
                }
                else
                {
                    FooterModeText.Text = $"Mode: {ImageViewerWM.CurrentTab.Mode}";
                }
                {
                    FooterSizeText.Text = $"Size: {ImageViewerWM.CurrentTab.Width}x{ImageViewerWM.CurrentTab.Height}";

                    string channel = string.Empty;
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
                    if (ImageViewerWM.CurrentTab.Size < 1024)
                    {
                        FooterFilesizeText.Text = $"Filesize: {ImageViewerWM.CurrentTab.Size}Bytes";
                    }
                    else if (ImageViewerWM.CurrentTab.Size < 1048576)
                    {
                        var filesize = (double)(ImageViewerWM.CurrentTab.Size / 1024f);
                        FooterFilesizeText.Text = $"Filesize: {filesize:N2}KB";
                    }
                    else
                    {
                        var filesize = (double)(ImageViewerWM.CurrentTab.Size / 1024f) / 1024f;
                        FooterFilesizeText.Text = $"Filesize: {filesize:N2}MB";
                    }
                }
                FooterZoomText.Text = $"Zoom: {ImageArea.Zoom}%";
                FooterIndexText.Text = $"Index: {ImageViewerWM.CurrentTab.Index + 1}/{ImageViewerWM.CurrentTab.Paths.Count}";
            }
        }

        void DuplicateTab()
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            var folderPath = Path.GetDirectoryName(ImageViewerWM.CurrentTab.Path);
            var tab = new TabData(folderPath, ImageViewerWM.CurrentTab.Index)
            {
                InitialImagePath = ImageViewerWM.CurrentTab.InitialImagePath,
                Paths = ImageViewerWM.CurrentTab.Paths,
                CloseTabAction = CloseTabIndex
            };

            var init_path = ImageViewerWM.CurrentTab.Path;

            tab.ImageSettings = new ImageSettings { displayChannel = ImageViewerWM.CurrentTab.ImageSettings.displayChannel, CurrentSortMode = ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode };

            ImageViewerWM.Tabs.Insert(ImageViewerWM.CurrentTabIndex + 1, tab);

            ImageTabControl.Items.Insert(ImageViewerWM.CurrentTabIndex + 1, tab.tabItem);

            ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;
        }

        bool FileBrowser()
        {
            var fileDialog = ImageViewerWM.ShowOpenFileDialog();
            if (string.IsNullOrEmpty(fileDialog.SafeFileName))
                return false;
            string filename = Path.GetFullPath(fileDialog.FileName);
            ReplaceImageInTab(filename);

            return true;
        }

        void ImageAreaOnLoaded(object sender, RoutedEventArgs e)
        {
            ImageArea.Image = sender as System.Drawing.Image;
            if (ImageArea != null)
            {
                DisplayImage();
            }
        }

        void ImageTabControl_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            AddNewTab(Path.GetFullPath(filenames[0]));
        }

        void ImageTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageViewerWM.CurrentTabIndex = ImageTabControl.SelectedIndex;

            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                slideshowTimer.Start();
            }

            var folder_path = Path.GetDirectoryName(ImageViewerWM.Tabs[ImageTabControl.SelectedIndex].InitialImagePath);
            var init_path = ImageViewerWM.CurrentTab.Paths[(ImageViewerWM.CurrentTab.Index)];
            sortingManager.SupportedFiles(folder_path);
            sortingManager.FindImageAfterSort(ImageViewerWM.CurrentTab.Paths, init_path);


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
                    image = (MagickImage)imageCollection[0];
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

            ImageViewerWM.CurrentTab.Size = image.FileSize;
            ImageViewerWM.CurrentTab.Width = image.Width;
            ImageViewerWM.CurrentTab.Height = image.Height;

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
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ImageEditor))
            {
                if (File.Exists(Properties.Settings.Default.ImageEditor))
                {
                    Process.Start(Properties.Settings.Default.ImageEditor, ImageViewerWM.CurrentTab.Path);
                    return;
                }
                if (MessageBox.Show("Editor not found\nDo you want to browse for editor?", Properties.Resources.FileMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageViewerWM.ImageEditorBrowse();
                }
            }
            else
            {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?", Properties.Resources.ImageEditorMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageViewerWM.ImageEditorBrowse();
                }
            }
            Properties.Settings.Default.Save();
        }

        void RefreshImage()
        {
            if (ImageViewerWM.CurrentTab.IsValid())
            {

                var image = LoadImage(ImageViewerWM.CurrentTab.Path);

                ImageArea.Image = image;


                ImageViewerWM.CurrentTab.UpdateTitle();
                UpdateFooter();
            }
        }

        void UpdateView()
        {
            DisplayImage();
            ResetView();
        }

        void RefreshUI()
        {
            ImageArea.GridColor = System.Drawing.Color.FromArgb(255, Properties.Settings.Default.BackgroundColor.R, Properties.Settings.Default.BackgroundColor.G, Properties.Settings.Default.BackgroundColor.B);
        }

        void ReplaceImageInTab(string filename)
        {
            var folderPath = Path.GetDirectoryName(filename);
            if (ImageViewerWM.CurrentTabIndex <= 0)
            {
                AddNewTab(filename);
            }
            ImageViewerWM.CurrentTab.InitialImagePath = filename;
            sortingManager.SupportedFiles(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.Paths.IndexOf(filename);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            SetupDirectoryWatcher();

            ResetView();
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
        }

        void ResetView()
        {
            if (ImageArea.Size.Width < ImageViewerWM.CurrentTab.Width || ImageArea.Size.Height < ImageViewerWM.CurrentTab.Height)
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
            ImageViewerWM.CurrentTab.Index = newIndex;
            DisplayImage();
        }

        void SetDisplayChannel(Channels channel)
        {
            if (!ImageViewerWM.CanExcectute())
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
            if (!ImageViewerWM.CanExcectute())
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
                Path = Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath),
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
                Path = Directory.GetParent(Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath)).FullName,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            parentDirectoryWatcher.Changed += ParentDirectoryChanged;
            parentDirectoryWatcher.Created += ParentDirectoryChanged;
            parentDirectoryWatcher.Deleted += ParentDirectoryChanged;
            parentDirectoryWatcher.Renamed += ParentDirectoryChanged;

            parentDirectoryWatcher.EnableRaisingEvents = true;

            imageDirectoryWatcher.Changed += (sender, args) => sortingManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath));
            imageDirectoryWatcher.Created += (sender, args) => sortingManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath));
            imageDirectoryWatcher.Deleted += (sender, args) => sortingManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath));
            imageDirectoryWatcher.Renamed += (sender, args) => sortingManager.SupportedFiles(Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath));

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
                            if (Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath) == args.FullPath)
                            {
                                CloseTab();
                            }
                            break;
                        }
                    case WatcherChangeTypes.Changed:
                        break;
                    case WatcherChangeTypes.Renamed:
                        {
                            var renamed_args = (RenamedEventArgs)args;
                            if (Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath) == renamed_args.OldFullPath)
                            {
                                ImageViewerWM.CurrentTab.InitialImagePath = renamed_args.FullPath;
                                CloseTab();
                            }
                            //Hangs the program
                            break;
                            if (Path.GetDirectoryName(ImageViewerWM.CurrentTab.InitialImagePath) == renamed_args.OldFullPath)
                            {
                                var newFile = Path.Combine(renamed_args.FullPath, Path.GetFileName(ImageViewerWM.CurrentTab.Path));
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
            if (!ImageViewerWM.CanExcectute())
            {
                ImageViewerWM.CurrentTab.Mode = ApplicationMode.Normal;
                return;
            }

            if (ImageViewerWM.CurrentTab.CurrentSlideshowTime < ImageViewerWM.SlideshowInterval)
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime += 1;
                UpdateFooter();
                ImageViewerWM.CurrentTab.UpdateTitle();
            }
            else
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 1;
                slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (ImageViewerWM.CurrentTab.Mode != ApplicationMode.Slideshow)
            {
                slideshowTimer.Stop();
                ImageViewerWM.CurrentTab.UpdateTitle();
                UpdateFooter();
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 1;
            }
        }

        void Slideshow10SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds10);

        void Slideshow1SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Second1);

        void Slideshow20SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds20);

        void Slideshow2SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds2);

        void Slideshow30SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds30);

        void Slideshow3SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds3);

        void Slideshow4SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds4);

        void Slideshow5SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds5);

        void SlideshowIntervalUI(SlideshowInterval newInterval)
        {
            Slideshow1SecUI.IsChecked = false;
            Slideshow2SecUI.IsChecked = false;
            Slideshow3SecUI.IsChecked = false;
            Slideshow4SecUI.IsChecked = false;
            Slideshow5SecUI.IsChecked = false;
            Slideshow10SecUI.IsChecked = false;
            Slideshow20SecUI.IsChecked = false;
            Slideshow30SecUI.IsChecked = false;

            switch (newInterval)
            {
                case SlideshowInterval.Second1:
                    {
                        Slideshow1SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 1;
                        break;
                    }
                case SlideshowInterval.Seconds2:
                    {
                        Slideshow2SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 2;
                        break;
                    }
                case SlideshowInterval.Seconds3:
                    {
                        Slideshow3SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 3;
                        break;
                    }
                case SlideshowInterval.Seconds4:
                    {
                        Slideshow4SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 4;
                        break;
                    }
                case SlideshowInterval.Seconds5:
                    {
                        Slideshow5SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 5;
                        break;
                    }
                case SlideshowInterval.Seconds10:
                    {
                        Slideshow10SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 10;
                        break;
                    }
                case SlideshowInterval.Seconds20:
                    {
                        Slideshow20SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 20;
                        break;
                    }
                case SlideshowInterval.Seconds30:
                    {
                        Slideshow30SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 30;
                        break;
                    }
            }
        }

        void Sort_by_date_modified(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            sortingManager.Sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void Sort_by_name(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            sortingManager.Sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void Sort_by_size(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
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
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.CurrentTab.Mode = ApplicationMode.Slideshow;
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 1;
            slideshowTimer.Start();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void StopSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.CurrentTab.Mode = ApplicationMode.Normal;
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 1;
            slideshowTimer.Stop();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void SwitchImage(SwitchDirection switchDirection)
        {
            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 1;
            }

            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (ImageViewerWM.CurrentTab.Index < ImageViewerWM.CurrentTab.Paths.Count - 1)
                    {
                        SetCurrentImage(ImageViewerWM.CurrentTab.Index += 1);
                    }
                    else
                    {
                        SetCurrentImage(0);
                    }
                    break;

                case SwitchDirection.Previous:
                    if (ImageViewerWM.CurrentTab.Paths.Any())
                    {
                        if (ImageViewerWM.CurrentTab.Index > 0)
                        {
                            SetCurrentImage(ImageViewerWM.CurrentTab.Index -= 1);
                        }
                        else
                        {
                            SetCurrentImage(ImageViewerWM.CurrentTab.Index = ImageViewerWM.CurrentTab.Paths.Count - 1);
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

        void UINext_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Next);
        }

        void UIPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Previous);
        }

        void ViewInExplorer(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            Process.Start("explorer.exe", "/select, " + ImageViewerWM.CurrentTab.Path);
        }

        void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            AlwaysOnTopUI.IsChecked = Topmost;
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

            WindowState = (WindowState)Properties.Settings.Default.WindowState;

            e.Handled = true;
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var location = new System.Drawing.Point((int)Left, (int)Top);
            Properties.Settings.Default.WindowLocation = location;
            Properties.Settings.Default.WindowState = (int)WindowState;
            if (WindowState == WindowState.Normal)
            {
                var size = new System.Drawing.Size((int)Width, (int)Height);
                Properties.Settings.Default.WindowSize = size;
            }
            else
            {
                var size = new System.Drawing.Size((int)RestoreBounds.Width, (int)RestoreBounds.Height);
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

        void ImageArea_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            ReplaceImageInTab(filenames[0]);
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
            if (e != null && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (FileBrowser())
                {
                    DisplayImage();
                    ResetView();
                }
            }
        }

        void ImageArea_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            winFormsHost.ContextMenu.IsOpen |= e.Button == System.Windows.Forms.MouseButtons.Right;
        }

        void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var keyConverter = new System.Windows.Forms.KeysConverter();
            System.Windows.Forms.Keys key;
            try
            {
                key = (System.Windows.Forms.Keys)keyConverter.ConvertFromString(e.Key.ToString());
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