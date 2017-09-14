// TODOS
// GIF Support
// Loading images without lag
// Split into more files
// Progress bar when loading large images
// Layout tabs side by side?
// Options window, maybe I can add the hotkeys in there as well
// Font size settings
// About dialog, with ImageMagick Credit
// Pick color under mouse, copy value to clipboard?
// Able to auto hide or just hide the buttons.

using ImageMagick;
using Microsoft.VisualBasic.FileIO;
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

namespace ImageViewer
{
    public partial class MainWindow
    {
        ImageViewerWM ImageViewerWM { get; set; } = new ImageViewerWM();
        static System.Windows.Threading.DispatcherTimer slideshowTimer;

        public MainWindow()
        {
            InitializeComponent();

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var newTab = new TabItem { Header = Title, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
                ImageViewerWM.Tabs.Add(new TabData(newTab, null, 0));

                ImageTabControl.Items.Add(newTab);

                ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;

                var filePath = Environment.GetCommandLineArgs()[1];
                ImageViewerWM.CurrentTab.InitialImagePath = filePath;
                var folderPath = Path.GetDirectoryName(filePath);
                SupportedImageFilesInDirectoryDispatch(folderPath);
                if (ImageViewerWM.CurrentTab.images.Paths.IndexOf(filePath) == -1)
                {
                    ImageViewerWM.CurrentTab.Index = 0;
                }
                else
                {
                    ImageViewerWM.CurrentTab.Index = ImageViewerWM.CurrentTab.images.Paths.IndexOf(filePath);
                }
            }

            RefreshUI();
            SetupSlideshow();
        }

        public Channels DisplayChannel
        {
            get => ImageViewerWM.CurrentTab.imageSettings.displayChannel;

            set
            {
                ImageViewerWM.CurrentTab.imageSettings.displayChannel = value;
                RefreshImage();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            RawKeyHandling(e);

            ValidatedKeyHandling(e);
        }

        void ValidatedKeyHandling(KeyEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

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
                        zoomBorder.Reset();
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
            ImageViewerWM.CurrentTab.InSlideshowMode = !ImageViewerWM.CurrentTab.InSlideshowMode;
            if (ImageViewerWM.CurrentTab.InSlideshowMode)
            {
                slideshowTimer.Start();
            }
            else
            {
                slideshowTimer.Stop();
            }
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
        }

        void DeleteImage()
        {
            var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                          $"{Properties.Resources.Delete}{FileSystem.GetName(ImageViewerWM.CurrentTab.Path)}", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                FileSystem.DeleteFile(ImageViewerWM.CurrentTab.Path, UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
                if (ImageViewerWM.CurrentTab.images.Paths.Count > 0)
                {
                    SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.CurrentTab.Path));

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
                        if(ImageViewerWM.Tabs.Count == 0)
                        {
                            Close();
                            break;
                        }
                        if (ImageViewerWM.CurrentTab.InToggleMode)
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
            if (e != null)
            {
                if (e.Source == zoomBorder || e.Source == ImageArea)
                {
                    if (FileBrowser())
                    {
                        DisplayImage();
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ImageViewerWM.Tabs.Any() && ImageViewerWM.CurrentTab.images.Paths.Any())
            {
                if ((e != null) && Keyboard.IsKeyDown(Key.LeftCtrl))
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

        void AddNewTab()
        {
            var file_dialog = ImageViewerWM.ShowOpenFileDialog();
            if (string.IsNullOrEmpty(file_dialog.SafeFileName))
                return;

            var newTab = new TabItem { Header = file_dialog.FileName, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(file_dialog.FileName);
            ImageViewerWM.Tabs.Add(new TabData(newTab, folderPath));

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;

            SupportedImageFilesInDirectoryDispatch(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.images.Paths.IndexOf(file_dialog.FileName);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            ImageViewerWM.CurrentTab.InitialImagePath = file_dialog.FileName;

            RefreshTab();
        }

        void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(Path.GetFileName(filepath)))
                return;

            var newTab = new TabItem { Header = Path.GetFileName(filepath), IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(filepath);
            ImageViewerWM.Tabs.Add(new TabData(newTab, folderPath));

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;

            SupportedImageFilesInDirectoryDispatch(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.images.Paths.IndexOf(filepath);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            ImageViewerWM.CurrentTab.InitialImagePath = filepath;

            RefreshTab();
            SetupDirectoryWatcher();
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.imageSettings.CurrentSortMode == SortMode.Descending)
            {
                var inital_image = ImageViewerWM.CurrentTab.Path;
                var file_paths_list = ImageViewerWM.CurrentTab.images.Paths;
                file_paths_list.Reverse();
                ImageViewerWM.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWM.CurrentTab.imageSettings.CurrentSortMode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void Border_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            ReplaceImageInTab(filenames[0]);
            RefreshTab();
        }

        void CloseTab()
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.Tabs.RemoveAt(ImageTabControl.SelectedIndex);
            if (ImageTabControl.SelectedIndex == 0)
            {
                ImageArea.Source = null;
                GC.Collect();
            }
            ImageTabControl.Items.RemoveAt(ImageTabControl.SelectedIndex);
        }

        void Compare_onclick(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            var compare_file = ImageViewerWM.ShowOpenFileDialog().FileName;
            if (string.IsNullOrEmpty(compare_file))
                return;

            if (ImageViewerWM.CurrentTab.InToggleMode == false)
            {
                ImageViewerWM.CurrentTab.last_images = ImageViewerWM.CurrentTab.images;
                ImageViewerWM.CurrentTab.InToggleMode = true;
                ImageViewerWM.BeforeCompareModeIndex = ImageViewerWM.CurrentTab.Index;
            }
            ImageViewerWM.CurrentTab.images.Paths = new List<string> { ImageViewerWM.CurrentTab.Path, compare_file };
            SetCurrentImage(0);
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(ImageViewerWM.CurrentTab.Path)}\"");
        }

        void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.imageSettings.CurrentSortMode == SortMode.Ascending)
            {
                var inital_image = ImageViewerWM.CurrentTab.Path;
                var file_paths_list = ImageViewerWM.CurrentTab.images.Paths;
                file_paths_list.Reverse();
                ImageViewerWM.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWM.CurrentTab.imageSettings.CurrentSortMode = SortMode.Descending;
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
                if (ImageViewerWM.CurrentTab.images.IsValid())
                {
                    var image = LoadImage(ImageViewerWM.CurrentTab.Path);

                    ImageArea.Source = image;
                    if (image.Height > zoomBorder.ActualHeight)
                    {
                        ImageArea.Height = zoomBorder.ActualHeight;
                    }
                    else if (image.Width > zoomBorder.ActualWidth)
                    {
                        ImageArea.Width = zoomBorder.ActualWidth;
                    }
                    else
                    {
                        if (image.Width < zoomBorder.ActualWidth)
                        {
                            ImageArea.Height = image.Height;
                        }
                        else
                        {
                            ImageArea.Width = image.Width;
                        }
                    }

                    ImageViewerWM.CurrentTab.UpdateTitle();
                    UpdateFooter();
                    zoomBorder.SetScaleTransform(ImageViewerWM.CurrentTab.Scale);
                    zoomBorder.SetTranslateTransform(ImageViewerWM.CurrentTab.Pan);

                }
            }
        }

        void UpdateFooter()
        {
            // Should probably save this info when reading the image the first time.
            {
                var fileinfo = new MagickImageInfo(ImageViewerWM.CurrentTab.images.Paths[ImageViewerWM.CurrentTab.Index]);
                FooterSizeText.Text = $"Size: {fileinfo.Width}x{fileinfo.Height}";
                string channel = Channels.RGB.ToString();
                switch (DisplayChannel)
                {
                    case (Channels.Red):
                        {
                            channel = "Red";
                            break;
                        }
                    case (Channels.Green):
                        {
                            channel = "Green";
                            break;
                        }
                    case (Channels.Blue):
                        {
                            channel = "Blue";
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
                var fileinfo = new FileInfo(ImageViewerWM.CurrentTab.images.Paths[ImageViewerWM.CurrentTab.Index]);
                if (fileinfo.Length < 1024)
                {
                    FooterCompressionMethodText.Text = $"Filesize: {fileinfo.Length}Bytes";
                }
                else if(fileinfo.Length < 1048576)
                {
                    var filesize = (double)(fileinfo.Length / 1024f);
                    FooterCompressionMethodText.Text = $"Filesize: {filesize:N2}KB";
                }
                else
                {
                    var filesize = (double)(fileinfo.Length / 1024f) / 1024f;
                    FooterCompressionMethodText.Text = $"Filesize: {filesize:N2}MB";
                }
            }
        }

        void DuplicateTab()
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            var newTab = new TabItem { Header = Path.GetFileName(ImageViewerWM.CurrentTab.Path), IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(ImageViewerWM.CurrentTab.Path);
            var tab = new TabData(newTab, folderPath, ImageViewerWM.CurrentTab.Index)
            {
                InitialImagePath = ImageViewerWM.CurrentTab.InitialImagePath,
                images = ImageViewerWM.CurrentTab.images
            };
            ImageViewerWM.Tabs.Insert(ImageViewerWM.CurrentTabIndex + 1, tab);

            ImageTabControl.Items.Insert(ImageViewerWM.CurrentTabIndex + 1, newTab);

            ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;
            RefreshTab();
        }

        void ExitToggleMode()
        {
            ImageViewerWM.CurrentTab.images = ImageViewerWM.CurrentTab.last_images;
            ImageViewerWM.CurrentTab.InToggleMode = false;
            SetCurrentImage(ImageViewerWM.BeforeCompareModeIndex);
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
            ImageArea = sender as Image;
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

            if (e.RemovedItems.Count > 0)
            {
                foreach (var tab in ImageViewerWM.Tabs)
                {
                    if (tab.tabItem == (TabItem)e.RemovedItems[0])
                    {
                        ImageViewerWM.Tabs[ImageViewerWM.Tabs.IndexOf(tab)].Pan.X = zoomBorder.GetTranslateTransform().X;
                        ImageViewerWM.Tabs[ImageViewerWM.Tabs.IndexOf(tab)].Pan.Y = zoomBorder.GetTranslateTransform().Y;
                        ImageViewerWM.Tabs[ImageViewerWM.Tabs.IndexOf(tab)].Scale.ScaleX = zoomBorder.GetScaleTransform().ScaleX;
                        ImageViewerWM.Tabs[ImageViewerWM.Tabs.IndexOf(tab)].Scale.ScaleY = zoomBorder.GetScaleTransform().ScaleY;
                    }
                }
            }

            if(ImageViewerWM.CurrentTab.InSlideshowMode)
            {
                slideshowTimer.Start();
            }

            var folder_path = Path.GetDirectoryName(ImageViewerWM.Tabs[ImageTabControl.SelectedIndex].InitialImagePath);
            SupportedImageFilesInDirectoryDispatch(folder_path);

            RefreshTab();
        }

        BitmapSource LoadImage(string filepath)
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
            finally
            {
                GC.Collect();
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
            if (ImageViewerWM.CurrentTab.images.IsValid())
            {
                var image = LoadImage(ImageViewerWM.CurrentTab.Path);

                ImageArea.Source = image;

                ImageViewerWM.CurrentTab.UpdateTitle();
                UpdateFooter();
            }
        }

        void RefreshTab()
        {
            DisplayImage();
            ImageViewerWM.CurrentTab.UpdateTitle();
            UpdateFooter();
        }

        void RefreshUI()
        {
            zoomBorder.Background = new SolidColorBrush(Color.FromRgb(Properties.Settings.Default.BackgroundColor.R, Properties.Settings.Default.BackgroundColor.G, Properties.Settings.Default.BackgroundColor.B));
        }

        void ReplaceImageInTab(string filename)
        {
            var folderPath = Path.GetDirectoryName(filename);
            if (ImageViewerWM.CurrentTabIndex < 0)
            {
                AddNewTab(filename);
            }
            ImageViewerWM.CurrentTab.InitialImagePath = filename;
            SupportedImageFilesInDirectoryDispatch(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.images.Paths.IndexOf(filename);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            SetupDirectoryWatcher();
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            zoomBorder.Reset();
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
                Path = Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath),
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));
            watcher.Created += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));
            watcher.Deleted += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));
            watcher.Renamed += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));

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
            if (!ImageViewerWM.CanExcectute())
            {
                ImageViewerWM.CurrentTab.InSlideshowMode = false;
                return;
            }

            if (ImageViewerWM.CurrentTab.CurrentSlideshowTime < ImageViewerWM.SlideshowInterval)
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime += 1;
                ImageViewerWM.CurrentTab.UpdateTitle();
            }
            else
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
                slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (!ImageViewerWM.CurrentTab.InSlideshowMode)
            {
                slideshowTimer.Stop();
                ImageViewerWM.CurrentTab.UpdateTitle();
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
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

            ImageViewerWM.Sort(SortMethod.Date);
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

            ImageViewerWM.Sort(SortMethod.Name);
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

            ImageViewerWM.Sort(SortMethod.Size);
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

            ImageViewerWM.CurrentTab.InSlideshowMode = true;
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
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

            ImageViewerWM.CurrentTab.InSlideshowMode = false;
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
            slideshowTimer.Stop();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void SupportedImageFilesInDirectoryDispatch(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ImageViewerWM.SupportedImageFilesInDirectory(path);
            });
        }

        void SwitchImage(SwitchDirection switchDirection)
        {
            if (ImageViewerWM.CurrentTab.InSlideshowMode)
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
            }
            UpdateLayout();
            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (ImageViewerWM.CurrentTab.Index < ImageViewerWM.CurrentTab.images.Paths?.Count - 1)
                    {
                        SetCurrentImage(ImageViewerWM.CurrentTab.Index += 1);
                    }
                    else
                    {
                        SetCurrentImage(0);
                    }
                    break;

                case SwitchDirection.Previous:
                    if (ImageViewerWM.CurrentTab.images.Paths != null)
                    {
                        if (ImageViewerWM.CurrentTab.Index > 0)
                        {
                            SetCurrentImage(ImageViewerWM.CurrentTab.Index -= 1);
                        }
                        else
                        {
                            SetCurrentImage(ImageViewerWM.CurrentTab.Index = ImageViewerWM.CurrentTab.images.Paths.Count - 1);
                        }
                    }
                    break;
            }
            zoomBorder.Reset();
        }
        void UIAddNewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
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
    }
}