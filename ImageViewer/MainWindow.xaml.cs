// TODOS
// GIF Support
// Loading images without lag
// Split into more files
// Save window size and position on exit and use that on startup
// Add tabbar color to settings file.
// Layout tabs side by side?
// On top setting

using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SearchOption = System.IO.SearchOption;

namespace ImageViewer
{
    public partial class MainWindow
    {
        static System.Windows.Threading.DispatcherTimer slideshowTimer;
        int BeforeCompareModeIndex { get; set; }
        int CurrentSlideshowTime { get; set; }
        Image imageArea;
        bool InToggleMode { get; set; }
        int SlideshowInterval { get; set; } = 5;
        bool SlideshowMode { get; set; }
        List<TabData> Tabs { get; set; } = new List<TabData>();

        TabData CurrentTab
        {
            get
            {
                return Tabs[CurrentTabIndex];
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Settings.Load();

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var newTab = new TabItem { Header = Title, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
                Tabs.Add(new TabData(newTab, null, 0));

                ImageTabControl.Items.Add(newTab);

                ImageTabControl.SelectedIndex = CurrentTabIndex + 1;

                var filePath = Environment.GetCommandLineArgs()[1];
                CurrentTab.InitialImagePath = filePath;
                var folderPath = Path.GetDirectoryName(filePath);
                SupportedImageFilesInDirectory(folderPath);
                if (CurrentTab.images.Paths.IndexOf(filePath) == -1)
                {
                    CurrentTab.Index = 0;
                }
                else
                {
                    CurrentTab.Index = CurrentTab.images.Paths.IndexOf(filePath);
                }
            }

            RefreshUI();
            SetupSlideshow();
        }

        enum SlideshowIntervalSec
        {
            Sec1,
            Secs2,
            Secs3,
            Secs4,
            Secs5,
            Secs10,
            Secs20,
            Secs30
        }

        enum SwitchDirection
        {
            Next,
            Previous
        }

        public int CurrentTabIndex
        {
            get
            {
                if (ImageTabControl == null)
                {
                    return 0;
                }
                return ImageTabControl.SelectedIndex;
            }
        }
        public Channels DisplayChannel
        {
            get => CurrentTab.imageSettings.displayChannel;

            set
            {
                CurrentTab.imageSettings.displayChannel = value;
                RefreshImage();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            RawKeyHandling(e);

            if (!CanExcectute())
            {
                return;
            }

            ValidatedKeyHandling(e);
        }

        void ValidatedKeyHandling(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    {
                        SetDisplayChannel(Channels.Alpha);
                        break;
                    }
                case Key.R:
                    {
                        SetDisplayChannel(Channels.Red);
                        break;
                    }
                case Key.G:
                    {
                        SetDisplayChannel(Channels.Green);
                        break;
                    }
                case Key.B:
                    {
                        SetDisplayChannel(Channels.Blue);
                        break;
                    }
                case Key.F:
                    {
                        border.Reset();
                        break;
                    }
                case Key.D:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            DuplicateTab();
                        }
                        break;
                    }
                case Key.W:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            CloseTab();
                        }
                        break;
                    }
                case Key.S:
                    {
                        ToggleSlideshow();
                        break;
                    }
                case Key.Right:
                    {
                        SwitchImage(SwitchDirection.Next);
                        break;
                    }
                case Key.Left:
                    {
                        SwitchImage(SwitchDirection.Previous);
                        break;
                    }
                case Key.Space:
                    {
                        SwitchImage(SwitchDirection.Next);
                        break;
                    }
                case Key.Delete:
                    {
                        DeleteImage();
                        break;
                    }
            }
        }

        void ToggleSlideshow()
        {
            SlideshowMode = !SlideshowMode;
            if (SlideshowMode)
            {
                slideshowTimer.Start();
            }
            else
            {
                slideshowTimer.Stop();
            }
            UpdateTitle();
            CurrentSlideshowTime = 0;
        }

        void DeleteImage()
        {
            var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                          $"Delete {FileSystem.GetName(CurrentTab.images.Paths[CurrentTab.Index])}", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                FileSystem.DeleteFile(CurrentTab.images.Paths[CurrentTab.Index], UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
                if (CurrentTab.images.Paths.Count > 0)
                {
                    SupportedImageFilesInDirectory(Path.GetDirectoryName(CurrentTab.images.Paths[CurrentTab.Index]));

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

        void RawKeyHandling(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    {
                        if (InToggleMode)
                        {
                            ExitToggleMode();
                        }
                        else
                        {
                            Close();
                        }
                        break;
                    }
                case Key.N:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            AddNewTab();
                        }
                        break;
                    }
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (e.Source == border)
            {
                if (InToggleMode)
                {
                    ExitToggleMode();
                }
                if (FileBrowser())
                {
                    DisplayImage();
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (Tabs.Any() && CurrentTab.images.Paths.Any())
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (e.Delta > 0)
                    {
                        SwitchImage(SwitchDirection.Next);
                    }
                    else
                    {
                        SwitchImage(SwitchDirection.Previous);
                    }
                }
            }
        }

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

        static OpenFileDialog ShowOpenFileDialog()
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = FileFormats.filter_string
            };
            fileDialog.ShowDialog();
            return fileDialog;
        }

        static List<string> TypeSort<T>(IEnumerable<FileId<T>> id_list, Dictionary<T, int> dictionary)
        {
            var id_file_dictionary = id_list.ToDictionary(x => x.Id, x => x.Path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            var sorted_paths = keys.Select(l => dictionary[l]).ToList().Select(l => id_file_dictionary[l]).ToList();
            return sorted_paths;
        }

        void AddNewTab()
        {
            var file_dialog = ShowOpenFileDialog();
            if (string.IsNullOrEmpty(file_dialog.SafeFileName))
                return;

            var newTab = new TabItem { Header = file_dialog.FileName, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(file_dialog.FileName);
            Tabs.Add(new TabData(newTab, folderPath));

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;

            SupportedImageFilesInDirectory(folderPath);

            var filenameIndex = CurrentTab.images.Paths.IndexOf(file_dialog.FileName);
            if (filenameIndex == -1)
            {
                CurrentTab.Index = 0;
            }
            else
            {
                CurrentTab.Index = filenameIndex;
            }

            CurrentTab.InitialImagePath = file_dialog.FileName;

            RefreshTab();
        }

        void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(Path.GetFileName(filepath)))
                return;

            var newTab = new TabItem { Header = Path.GetFileName(filepath), IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(filepath);
            Tabs.Add(new TabData(newTab, folderPath));

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;

            SupportedImageFilesInDirectory(folderPath);

            var filenameIndex = CurrentTab.images.Paths.IndexOf(filepath);
            if (filenameIndex == -1)
            {
                CurrentTab.Index = 0;
            }
            else
            {
                CurrentTab.Index = filenameIndex;
            }

            CurrentTab.InitialImagePath = filepath;

            RefreshTab();
            SetupDirectoryWatcher();
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            if (CurrentTab.imageSettings.Current_sort_mode == SortMode.Descending)
            {
                var inital_image = CurrentTab.images.Paths[CurrentTab.Index];
                var file_paths_list = CurrentTab.images.Paths;
                file_paths_list.Reverse();
                FindImageAfterSort(file_paths_list, inital_image);
            }
            CurrentTab.imageSettings.Current_sort_mode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void Border_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            ReplaceImageInTab(filenames[0]);
            RefreshTab();
        }

        bool CanExcectute()
        {
            if (CurrentTabIndex < 0)
            {
                return false;
            }
            if (CurrentTab.Index == -1)
            {
                return false;
            }
            if (!Tabs.Any())
            {
                return false;
            }
            if (CurrentTab.images.Paths == null)
            {
                return false;
            }
            if (!CurrentTab.images.Paths.Any())
            {
                return false;
            }
            return true;
        }

        void CloseTab()
        {
            if (!CanExcectute())
            {
                return;
            }

            Tabs.RemoveAt(ImageTabControl.SelectedIndex);
            if (ImageTabControl.SelectedIndex == 0)
            {
                ImageArea.Source = null;
            }
            ImageTabControl.Items.RemoveAt(ImageTabControl.SelectedIndex);
        }

        void Compare_onclick(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            var compare_file = ShowOpenFileDialog().FileName;
            if (string.IsNullOrEmpty(compare_file))
                return;

            if (InToggleMode == false)
            {
                CurrentTab.last_images = CurrentTab.images;
                InToggleMode = true;
                BeforeCompareModeIndex = CurrentTab.Index;
            }
            CurrentTab.images.Paths = new List<string> { CurrentTab.images.Paths[CurrentTab.Index], compare_file };
            SetCurrentImage(0);
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Clipboard.SetText($"\"{BackwardToForwardSlash(CurrentTab.images.Paths[CurrentTab.Index])}\"");
        }

        void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            if (CurrentTab.imageSettings.Current_sort_mode == SortMode.Ascending)
            {
                var inital_image = CurrentTab.images.Paths[CurrentTab.Index];
                var file_paths_list = CurrentTab.images.Paths;
                file_paths_list.Reverse();
                FindImageAfterSort(file_paths_list, inital_image);
            }
            CurrentTab.imageSettings.Current_sort_mode = SortMode.Descending;
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
            if (CurrentTabIndex < 0 || CurrentTab.Index == -1)
            {
                return;
            }
            if (imageArea != null)
            {
                if (CurrentTab.images.IsValid())
                {
                    var image = Load_image(CurrentTab.images.Paths[CurrentTab.Index]);

                    imageArea.Source = image;
                    if (image.Height > border.ActualHeight)
                    {
                        imageArea.Height = border.ActualHeight;
                    }
                    else if (image.Width > border.ActualWidth)
                    {
                        imageArea.Width = border.ActualWidth;
                    }
                    else
                    {
                        if (image.Width < border.ActualWidth)
                        {
                            imageArea.Height = image.Height;
                        }
                        else
                        {
                            imageArea.Width = image.Width;
                        }
                    }

                    UpdateTitle();
                    border.Reset();
                    // TODO Read this from the tab, but on image next/previous it should overwrite this.
                    border.SetTransform(new TranslateTransform(100, 100), new ScaleTransform(20, 20));

                }
            }
        }

        void DuplicateTab()
        {
            if (!CanExcectute())
            {
                return;
            }

            var newTab = new TabItem { Header = Path.GetFileName(CurrentTab.images.Paths[CurrentTab.Index]), IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(Path.GetFileName(CurrentTab.images.Paths[CurrentTab.Index]));
            var tab = new TabData(newTab, folderPath, CurrentTab.Index)
            {
                InitialImagePath = CurrentTab.InitialImagePath,
                images = CurrentTab.images
            };
            Tabs.Add(tab);

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;
            RefreshTab();
        }

        void ExitToggleMode()
        {
            CurrentTab.images = CurrentTab.last_images;
            InToggleMode = false;
            SetCurrentImage(BeforeCompareModeIndex);
        }

        bool FileBrowser()
        {
            var fileDialog = ShowOpenFileDialog();
            if (string.IsNullOrEmpty(fileDialog.SafeFileName))
                return false;
            string filename = Path.GetFullPath(fileDialog.FileName);
            ReplaceImageInTab(filename);

            return true;
        }

        void FindImageAfterSort(List<string> sorted_paths, string initial_image)
        {
            CurrentTab.images.Paths = sorted_paths;
            CurrentTab.Index = sorted_paths.IndexOf(initial_image);
        }

        void ImageAreaOnLoaded(object sender, RoutedEventArgs e)
        {
            imageArea = sender as Image;
            if (imageArea != null)
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
            if (!CanExcectute())
            {
                return;
            }
            
            var folder_path = Path.GetDirectoryName(Tabs[ImageTabControl.SelectedIndex].InitialImagePath);
            SupportedImageFilesInDirectory(folder_path);

            RefreshTab();
        }

        BitmapSource Load_image(string filepath)
        {
            MagickImage image;
            try
            {
                image = new MagickImage(filepath);
            }
            catch (MagickCoderErrorException)
            {
                image = ErrorImage(filepath);
            }
            catch (MagickMissingDelegateErrorException)
            {
                image = ErrorImage(filepath);
            }

            switch (DisplayChannel)
            {
                case Channels.Red:
                    {
                        return image.Separate(Channels.Red).ElementAt(0)?.ToBitmapSource();
                    }
                case Channels.Green:
                    {
                        return image.Separate(Channels.Green).ElementAt(0)?.ToBitmapSource();
                    }
                case Channels.Blue:
                    {
                        return image.Separate(Channels.Blue).ElementAt(0)?.ToBitmapSource();
                    }
                case Channels.Alpha:
                    {
                        return image.Separate(Channels.Alpha).ElementAt(0)?.ToBitmapSource();
                    }
                default:
                    {
                        image.Alpha(AlphaOption.Opaque);
                        return image.ToBitmapSource();
                    }
            }
        }

        void OpenInImageEditor(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }
            if (!string.IsNullOrEmpty(Settings.ImageEditor))
            {
                if (File.Exists(Settings.ImageEditor))
                {
                    Process.Start(Settings.ImageEditor, CurrentTab.images.Paths[CurrentTab.Index]);
                    return;
                }
                if (MessageBox.Show("Editor not found\nDo you want to browse for editor?", "Missing file", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageEditorBrowse();
                }
            }
            else
            {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?", "Missing Image Editor", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageEditorBrowse();
                }
            }
            Settings.Save();
        }

        void ImageEditorBrowse()
        {
            var file_dialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"
            };
            if (file_dialog.ShowDialog() == true)
            {
                Settings.ImageEditor = file_dialog.FileName;
                Process.Start(Settings.ImageEditor, CurrentTab.images.Paths[CurrentTab.Index]);
            }
            else
            {
                return;
            }
        }

        void RefreshImage()
        {
            if (CurrentTab.images.IsValid())
            {
                var image = Load_image(CurrentTab.images.Paths[CurrentTab.Index]);

                imageArea.Source = image;

                UpdateTitle();
            }
        }

        void RefreshTab()
        {
            DisplayImage();
            UpdateTitle();
        }

        void RefreshUI()
        {
            border.Background = Settings.Background;
        }

        void ReplaceImageInTab(string filename)
        {
            var folderPath = Path.GetDirectoryName(filename);
            if (CurrentTabIndex < 0)
            {
                AddNewTab(filename);
            }
            CurrentTab.InitialImagePath = filename;
            SupportedImageFilesInDirectory(folderPath);

            var filenameIndex = CurrentTab.images.Paths.IndexOf(filename);
            if (filenameIndex == -1)
            {
                CurrentTab.Index = 0;
            }
            else
            {
                CurrentTab.Index = filenameIndex;
            }

            SetupDirectoryWatcher();
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            border.Reset();
        }

        void SetCurrentImage(int newIndex)
        {
            CurrentTab.Index = newIndex;
            DisplayImage();
        }

        void SetDisplayChannel(Channels channel)
        {
            if (!CanExcectute())
            {
                return;
            }

            AllChannels.IsChecked = false;
            RedChannel.IsChecked = false;
            GreenChannel.IsChecked = false;
            BlueChannel.IsChecked = false;
            AlphaChannel.IsChecked = false;

            switch (channel)
            {
                case Channels.RGB:
                    {
                        AllChannels.IsChecked = true;
                        DisplayChannel = Channels.RGB;
                        break;
                    }
                case Channels.Blue:
                    {
                        BlueChannel.IsChecked = true;
                        DisplayChannel = DisplayChannel == Channels.Blue ? Channels.RGB : Channels.Blue;
                        break;
                    }
                case Channels.Red:
                    {
                        RedChannel.IsChecked = true;
                        DisplayChannel = DisplayChannel == Channels.Red ? Channels.RGB : Channels.Red;
                        break;
                    }
                case Channels.Green:
                    {
                        GreenChannel.IsChecked = true;
                        DisplayChannel = DisplayChannel == Channels.Green ? Channels.RGB : Channels.Green;
                        break;
                    }
                case Channels.Alpha:
                    {
                        AlphaChannel.IsChecked = true;
                        DisplayChannel = DisplayChannel == Channels.Alpha ? Channels.RGB : Channels.Alpha;
                        break;
                    }
            }
        }

        void SetupDirectoryWatcher()
        {
            var index = ImageTabControl != null ? ImageTabControl.SelectedIndex : 0;
            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(Tabs[index].InitialImagePath),
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(Tabs[index].InitialImagePath));
            watcher.Created += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(Tabs[index].InitialImagePath));
            watcher.Deleted += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(Tabs[index].InitialImagePath));
            watcher.Renamed += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(Tabs[index].InitialImagePath));

            watcher.EnableRaisingEvents = true;
        }

        void SetupSlideshow()
        {
            slideshowTimer = new System.Windows.Threading.DispatcherTimer();
            slideshowTimer.Tick += Slideshow;
            slideshowTimer.Interval = new TimeSpan(0, 0, 1);
        }
        void Slideshow(object source, EventArgs e)
        {
            if (!CanExcectute())
            {
                SlideshowMode = false;
                return;
            }

            if (CurrentSlideshowTime < SlideshowInterval)
            {
                CurrentSlideshowTime += 1;
                UpdateTitle();
            }
            else
            {
                CurrentSlideshowTime = 0;
                slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (!SlideshowMode)
            {
                slideshowTimer.Stop();
                UpdateTitle();
                CurrentSlideshowTime = 0;
            }
        }

        void Slideshow10SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs10);

        void Slideshow1SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Sec1);

        void Slideshow20SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs20);

        void Slideshow2SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs2);

        void Slideshow30SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs30);

        void Slideshow3SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs3);

        void Slideshow4SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs4);

        void Slideshow5SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs5);

        void SlideshowIntervalUI(SlideshowIntervalSec newInterval)
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
                case SlideshowIntervalSec.Sec1:
                    {
                        Slideshow1SecUI.IsChecked = true;
                        SlideshowInterval = 1;
                        break;
                    }
                case SlideshowIntervalSec.Secs2:
                    {
                        Slideshow2SecUI.IsChecked = true;
                        SlideshowInterval = 2;
                        break;
                    }
                case SlideshowIntervalSec.Secs3:
                    {
                        Slideshow3SecUI.IsChecked = true;
                        SlideshowInterval = 3;
                        break;
                    }
                case SlideshowIntervalSec.Secs4:
                    {
                        Slideshow4SecUI.IsChecked = true;
                        SlideshowInterval = 4;
                        break;
                    }
                case SlideshowIntervalSec.Secs5:
                    {
                        Slideshow5SecUI.IsChecked = true;
                        SlideshowInterval = 5;
                        break;
                    }
                case SlideshowIntervalSec.Secs10:
                    {
                        Slideshow10SecUI.IsChecked = true;
                        SlideshowInterval = 10;
                        break;
                    }
                case SlideshowIntervalSec.Secs20:
                    {
                        Slideshow20SecUI.IsChecked = true;
                        SlideshowInterval = 20;
                        break;
                    }
                case SlideshowIntervalSec.Secs30:
                    {
                        Slideshow30SecUI.IsChecked = true;
                        SlideshowInterval = 30;
                        break;
                    }
            }
        }

        void Sort(SortMethod sort_method)
        {
            switch (CurrentTab.imageSettings.Current_sort_mode)
            {
                case SortMode.Ascending:
                    {
                        SortAcending(sort_method);
                        break;
                    }
                case SortMode.Descending:
                    {
                        Sort_decending(sort_method);
                        break;
                    }
            }
        }

        void Sort_by_date_modified(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void Sort_by_name(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void Sort_by_size(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Sort(SortMethod.Size);
            SortSize.IsChecked = true;
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void Sort_decending(SortMethod method)
        {
            SortAcending(method);
            CurrentTab.images.Paths.Reverse();
            CurrentTab.Index = CurrentTab.images.Paths.ToList().IndexOf(CurrentTab.images.Paths[CurrentTab.Index]);
        }

        void SortAcending(SortMethod method)
        {
            var id = 0;
            string initialImage;
            if (CurrentTab.images.Paths.Count < CurrentTab.Index)
            {
                initialImage = CurrentTab.InitialImagePath;
            }
            else
            {
                initialImage = CurrentTab.images.Paths[CurrentTab.Index];
            }
            List<string> sortedPaths;
            switch (method)
            {
                case SortMethod.Name:
                    {
                        sortedPaths = CurrentTab.images.Paths.ToList();
                        sortedPaths.Sort();
                        break;
                    }

                case SortMethod.Date:
                    {
                        var keys = CurrentTab.images.Paths.ToList().Select(s => new FileInfo(s).LastWriteTime).ToList();

                        var dateTimeLookup = keys.Zip(CurrentTab.images.Paths.ToList(), (k, v) => new { k, v })
                                                   .ToLookup(x => x.k, x => x.v);

                        var idList = dateTimeLookup.SelectMany(pair => pair,
                                                          (pair, value) => new FileId<DateTime>(value, pair.Key, id += 1))
                                                      .ToList();

                        var dateIdDictionary = idList.ToDictionary(x => x.Item.AddMilliseconds(x.Id), x => x.Id);
                        sortedPaths = TypeSort(idList, dateIdDictionary);
                        break;
                    }
                case SortMethod.Size:
                    {
                        var keys = CurrentTab.images.Paths.ToList().Select(s => new FileInfo(s).Length).ToList();
                        var size_lookup = keys.Zip(CurrentTab.images.Paths.ToList(), (k, v) => new { k, v })
                                              .ToLookup(x => x.k, x => x.v);

                        var id_list = size_lookup.SelectMany(pair => pair,
                                                     (pair, value) => new FileId<long>(value, pair.Key, id += 1)).ToList();

                        var date_id_dictionary = id_list.ToDictionary(x => x.Item + x.Id, x => x.Id);
                        sortedPaths = TypeSort(id_list, date_id_dictionary);
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
            FindImageAfterSort(sortedPaths, initialImage);
        }

        void StartSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            SlideshowMode = true;
            UpdateTitle();
            CurrentSlideshowTime = 0;
            slideshowTimer.Start();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void StopSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            SlideshowMode = false;
            UpdateTitle();
            CurrentSlideshowTime = 0;
            slideshowTimer.Stop();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void SupportedImageFilesInDirectory(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                if (CurrentTab.images.Paths != null)
                {
                    CurrentTab.images.Paths.Clear();
                }
                else
                {
                    CurrentTab.images.Paths = new List<string>();
                }

                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (!string.IsNullOrEmpty(extension))
                    {
                        if (FileFormats.supported_extensions.Contains(extension.Remove(0, 1), StringComparer.OrdinalIgnoreCase))
                        {
                            CurrentTab.images.Paths.Add(file);
                        }
                    }
                }

                if (CurrentTab.images.IsValid())
                {
                    SortAcending(SortMethod.Name);
                }
            });
        }
        void SwitchImage(SwitchDirection switchDirection)
        {
            if (SlideshowMode)
            {
                CurrentSlideshowTime = 0;
            }
            UpdateLayout();
            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (CurrentTab.Index < CurrentTab.images.Paths?.Count - 1)
                    {
                        SetCurrentImage(CurrentTab.Index += 1);
                    }
                    else
                    {
                        SetCurrentImage(0);
                    }
                    break;

                case SwitchDirection.Previous:
                    if (CurrentTab.images.Paths != null)
                    {
                        if (CurrentTab.Index > 0)
                        {
                            SetCurrentImage(CurrentTab.Index -= 1);
                        }
                        else
                        {
                            SetCurrentImage(CurrentTab.Index = CurrentTab.images.Paths.Count - 1);
                        }
                    }
                    break;
            }
        }
        void UIAddNewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        void UINext_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Next);
        }

        void UIPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Previous);
        }

        void UpdateTitle()
        {
            if (!CanExcectute())
            {
                return;
            }

            if (InToggleMode)
            {
                CurrentTab.Title = $" [Toggle] {CurrentTab.Filename}";
            }
            else
            {
                CurrentTab.Title = CurrentTab.Filename;
            }

            if (SlideshowMode)
            {
                CurrentTab.Title += $" [Slideshow] {CurrentSlideshowTime}";
            }

            switch (DisplayChannel)
            {
                case Channels.RGB:
                    {
                        CurrentTab.Title += $" [RGB_]";
                        break;
                    }
                case Channels.Red:
                    {
                        CurrentTab.Title += $" [R___]";
                        break;
                    }
                case Channels.Green:
                    {
                        CurrentTab.Title += $" [_G__]";
                        break;
                    }
                case Channels.Blue:
                    {
                        CurrentTab.Title += $" [__B_]";
                        break;
                    }
                case Channels.Alpha:
                    {
                        CurrentTab.Title += $" [___A]";
                        break;
                    }
            }
        }
        void View_in_explorer(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Process.Start("explorer.exe", "/select, " + CurrentTab.images.Paths[CurrentTab.Index]);
        }
    }
}