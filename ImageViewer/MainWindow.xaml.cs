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
        int slideshowInterval = 5;
        int currentSlideshowTime;
        int beforeCompareModeIndex;

        List<TabData> tabs = new List<TabData>();

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

        bool slideshowMode;
        static System.Windows.Threading.DispatcherTimer slideshowTimer;

        Image imageArea;

        bool inToggleMode;

        public Channels DisplayChannel
        {
            get => tabs[CurrentTabIndex].imageSettings.displayChannel;

            set
            {
                tabs[CurrentTabIndex].imageSettings.displayChannel = value;
                RefreshImage();
            }
        }

        public MainWindow()
        {
            Settings.Load();

            var newTab = new TabItem { Header = Title, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
            tabs.Add(new TabData(newTab, null, 0));

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var filePath = Environment.GetCommandLineArgs()[1];
                tabs[CurrentTabIndex].initialImagePath = filePath;
                var folderPath = Path.GetDirectoryName(filePath);
                SupportedImageFilesInDirectory(folderPath);
                if (tabs[CurrentTabIndex].images.paths.IndexOf(filePath) == -1)
                {
                    tabs[CurrentTabIndex].currentIndex = 0;
                }
                else
                {
                    tabs[CurrentTabIndex].currentIndex = tabs[CurrentTabIndex].images.paths.IndexOf(filePath);
                }
            }
            else
            {
                FileBrowser();
            }

            InitializeComponent();

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;

            RefreshUI();
            SetupSlideshow();
        }

        void SetupSlideshow()
        {
            slideshowTimer = new System.Windows.Threading.DispatcherTimer();
            slideshowTimer.Tick += Slideshow;
            slideshowTimer.Interval = new TimeSpan(0, 0, 1);
        }

        void RefreshUI()
        {
            border.Background = Settings.Background;
        }

        void SetupDirectoryWatcher()
        {
            var index = ImageTabControl != null ? ImageTabControl.SelectedIndex : 0;
            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(tabs[index].initialImagePath),
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(tabs[index].initialImagePath));
            watcher.Created += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(tabs[index].initialImagePath));
            watcher.Deleted += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(tabs[index].initialImagePath));
            watcher.Renamed += (sender, args) => SupportedImageFilesInDirectory(Path.GetDirectoryName(tabs[index].initialImagePath));

            watcher.EnableRaisingEvents = true;
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

        void ReplaceImageInTab(string filename)
        {
            var folderPath = Path.GetDirectoryName(filename);
            if (CurrentTabIndex < 0)
            {
                AddNewTab(filename);
            }
            tabs[CurrentTabIndex].initialImagePath = filename;
            SupportedImageFilesInDirectory(folderPath);

            var filenameIndex = tabs[CurrentTabIndex].images.paths.IndexOf(filename);
            if (filenameIndex == -1)
            {
                tabs[CurrentTabIndex].currentIndex = 0;
            }
            else
            {
                tabs[CurrentTabIndex].currentIndex = filenameIndex;
            }

            SetupDirectoryWatcher();
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

        void SupportedImageFilesInDirectory(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                if (tabs[CurrentTabIndex].images.paths != null)
                {
                    tabs[CurrentTabIndex].images.paths.Clear();
                }
                else
                {
                    tabs[CurrentTabIndex].images.paths = new List<string>();
                }

                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (!string.IsNullOrEmpty(extension))
                    {
                        if (FileFormats.supported_extensions.Contains(extension.Remove(0, 1), StringComparer.OrdinalIgnoreCase))
                        {
                            tabs[CurrentTabIndex].images.paths.Add(file);
                        }
                    }
                }

                if (tabs[CurrentTabIndex].images.Is_valid())
                {
                    SortAcending(SortMethod.Name);
                }
            });
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (tabs.Any() && tabs[CurrentTabIndex].images.paths.Any())
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

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (e.Source == border || e.Source == ImageArea)
            {
                if (inToggleMode)
                {
                    ExitToggleMode();
                }
                if (FileBrowser())
                {
                    DisplayImage();
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!CanExcectute())
            {
                return;
            }


            if (e.Key == Settings.NextImage)
            {

                SwitchImage(SwitchDirection.Next);

            }
            else if (e.Key == Settings.PreviousImage)
            {

                SwitchImage(SwitchDirection.Previous);

            }
            else if (e.Key == Settings.DeleteImage)
            {

                var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                              $"Delete {FileSystem.GetName(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex])}", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    FileSystem.DeleteFile(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex], UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                    if (tabs[CurrentTabIndex].images.paths.Count > 0)
                    {
                        SwitchImage(SwitchDirection.Next);
                        // SupportedImageFilesInDirectory(Path.GetDirectoryName(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]));
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
                case Key.N:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            AddNewTab();
                        }
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
                        slideshowMode = !slideshowMode;
                        if (slideshowMode)
                        {
                            slideshowTimer.Start();
                        }
                        else
                        {
                            slideshowTimer.Stop();
                        }
                        UpdateTitle();
                        currentSlideshowTime = 0;
                        break;
                    }
                case Key.Escape:
                    {
                        if (inToggleMode)
                        {
                            ExitToggleMode();
                        }
                        else
                        {
                            Close();
                        }
                        break;
                    }
            }
        }

        void DuplicateTab()
        {
            if (!CanExcectute())
            {
                return;
            }

            var newTab = new TabItem { Header = Path.GetFileName(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]), IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(Path.GetFileName(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]));
            var tab = new TabData(newTab, folderPath, tabs[CurrentTabIndex].currentIndex)
            {
                initialImagePath = tabs[CurrentTabIndex].initialImagePath
            };
            tabs.Add(tab);

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;
            RefreshTab();
        }

        void CloseTab()
        {
            if (!CanExcectute())
            {
                return;
            }

            tabs.RemoveAt(ImageTabControl.SelectedIndex);
            if (ImageTabControl.SelectedIndex == 0)
            {
                ImageArea.Source = null;
            }
            ImageTabControl.Items.RemoveAt(ImageTabControl.SelectedIndex);
        }

        void AddNewTab()
        {
            var file_dialog = ShowOpenFileDialog();
            if (string.IsNullOrEmpty(file_dialog.SafeFileName))
                return;

            var newTab = new TabItem { Header = file_dialog.FileName, IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(file_dialog.FileName);
            tabs.Add(new TabData(newTab, folderPath));

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;

            SupportedImageFilesInDirectory(folderPath);

            var filenameIndex = tabs[CurrentTabIndex].images.paths.IndexOf(file_dialog.FileName);
            if (filenameIndex == -1)
            {
                tabs[CurrentTabIndex].currentIndex = 0;
            }
            else
            {
                tabs[CurrentTabIndex].currentIndex = filenameIndex;
            }

            tabs[CurrentTabIndex].initialImagePath = file_dialog.FileName;

            RefreshTab();
        }

        void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(Path.GetFileName(filepath)))
                return;

            var newTab = new TabItem { Header = Path.GetFileName(filepath), IsTabStop = false, FocusVisualStyle = null, Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };

            var folderPath = Path.GetDirectoryName(filepath);
            tabs.Add(new TabData(newTab, folderPath));

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = CurrentTabIndex + 1;

            SupportedImageFilesInDirectory(folderPath);

            var filenameIndex = tabs[CurrentTabIndex].images.paths.IndexOf(filepath);
            if (filenameIndex == -1)
            {
                tabs[CurrentTabIndex].currentIndex = 0;
            }
            else
            {
                tabs[CurrentTabIndex].currentIndex = filenameIndex;
            }

            tabs[CurrentTabIndex].initialImagePath = filepath;

            RefreshTab();
            SetupDirectoryWatcher();
        }

        void RefreshTab()
        {
            DisplayImage();
            UpdateTitle();
        }

        void Slideshow(object source, EventArgs e)
        {
            if (!CanExcectute())
            {
                slideshowMode = false;
                return;
            }

            if (currentSlideshowTime < slideshowInterval)
            {
                currentSlideshowTime += 1;
                UpdateTitle();
            }
            else
            {
                currentSlideshowTime = 0;
                slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (!slideshowMode)
            {
                slideshowTimer.Stop();
                UpdateTitle();
                currentSlideshowTime = 0;
            }

        }

        void ExitToggleMode()
        {
            tabs[CurrentTabIndex].images = tabs[CurrentTabIndex].last_images;
            inToggleMode = false;
            SetCurrentImage(beforeCompareModeIndex);
        }

        void SetCurrentImage(int newIndex)
        {
            tabs[CurrentTabIndex].currentIndex = newIndex;
            DisplayImage();
        }

        enum SwitchDirection
        {
            Next,
            Previous
        }

        void SwitchImage(SwitchDirection switchDirection)
        {
            if (slideshowMode)
            {
                currentSlideshowTime = 0;
            }
            imageArea.Source = null;
            UpdateLayout();
            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (tabs[CurrentTabIndex].currentIndex < tabs[CurrentTabIndex].images.paths?.Count - 1)
                    {
                        SetCurrentImage(tabs[CurrentTabIndex].currentIndex += 1);
                    }
                    else
                    {
                        SetCurrentImage(0);
                    }
                    break;
                case SwitchDirection.Previous:
                    if (tabs[CurrentTabIndex].images.paths != null)
                    {
                        if (tabs[CurrentTabIndex].currentIndex > 0)
                        {
                            SetCurrentImage(tabs[CurrentTabIndex].currentIndex -= 1);
                        }
                        else
                        {
                            SetCurrentImage(tabs[CurrentTabIndex].currentIndex = tabs[CurrentTabIndex].images.paths.Count - 1);
                        }
                    }
                    break;
            }
        }

        void ImageAreaOnLoaded(object sender, RoutedEventArgs e)
        {
            imageArea = sender as Image;
            if (imageArea != null)
            {
                DisplayImage();
            }
        }

        void DisplayImage()
        {
            if(tabs[CurrentTabIndex].currentIndex == -1)
            {
                return;
            }
            if (imageArea != null)
            {
                if (tabs[CurrentTabIndex].images.Is_valid())
                {
                    var image = Load_image(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]);

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
                }
            }
        }

        void UpdateTitle()
        {
            if (!CanExcectute())
            {
                return;
            }

            string title;

            if (inToggleMode)
            {
                title = $" [Toggle] {new FileInfo(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]).Name}";
            }
            else
            {
                title = new FileInfo(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]).Name;
            }

            if (slideshowMode)
            {
                title += $" [Slideshow] {currentSlideshowTime}";
            }

            switch (DisplayChannel)
            {
                case Channels.RGB:
                    {
                        title += $" [RGB_]";
                        break;
                    }
                case Channels.Red:
                    {
                        title += $" [R___]";
                        break;
                    }
                case Channels.Green:
                    {
                        title += $" [_G__]";
                        break;
                    }
                case Channels.Blue:
                    {
                        title += $" [__B_]";
                        break;
                    }
                case Channels.Alpha:
                    {
                        title += $" [___A]";
                        break;
                    }
            }
            tabs[CurrentTabIndex].tabItem.Header = title;
        }

        void RefreshImage()
        {
            if (tabs[CurrentTabIndex].images.Is_valid())
            {
                var image = Load_image(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]);

                imageArea.Source = image;

                UpdateTitle();
            }
        }

        BitmapSource Load_image(string filepath)
        {
            MagickImage image;
            try
            {
                image = new MagickImage(filepath);
            }
            catch (MagickMissingDelegateErrorException)
            {
                image = new MagickImage(MagickColors.White, 512, 512);
                new Drawables()
                  .FontPointSize(18)
                  .Font("Arial")
                  .FillColor(MagickColors.Red)
                  .TextAlignment(ImageMagick.TextAlignment.Center)
                  .Text(256, 256, $"Could not load\n{Path.GetFileName(filepath)}")
                  .Draw(image);
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

        void SortAcending(SortMethod method)
        {
            var id = 0;
            string initialImage;
            if (tabs[CurrentTabIndex].images.paths.Count < tabs[CurrentTabIndex].currentIndex)
            {
                initialImage = tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex];
            }
            else
            {
                initialImage = tabs[CurrentTabIndex].initialImagePath;
            }
            List<string> sortedPaths;
            switch (method)
            {
                case SortMethod.Name:
                    {
                        sortedPaths = tabs[CurrentTabIndex].images.paths.ToList();
                        sortedPaths.Sort();
                        break;
                    }

                case SortMethod.Date:
                    {
                        var keys = tabs[CurrentTabIndex].images.paths.ToList().Select(s => new FileInfo(s).LastWriteTime).ToList();

                        var dateTimeLookup = keys.Zip(tabs[CurrentTabIndex].images.paths.ToList(), (k, v) => new { k, v })
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
                        var keys = tabs[CurrentTabIndex].images.paths.ToList().Select(s => new FileInfo(s).Length).ToList();
                        var size_lookup = keys.Zip(tabs[CurrentTabIndex].images.paths.ToList(), (k, v) => new { k, v })
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

        static List<string> TypeSort<T>(IEnumerable<FileId<T>> id_list, Dictionary<T, int> dictionary)
        {
            var id_file_dictionary = id_list.ToDictionary(x => x.Id, x => x.Path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            var sorted_paths = keys.Select(l => dictionary[l]).ToList().Select(l => id_file_dictionary[l]).ToList();
            return sorted_paths;
        }

        void FindImageAfterSort(List<string> sorted_paths, string initial_image)
        {
            tabs[CurrentTabIndex].images.paths = sorted_paths;
            tabs[CurrentTabIndex].currentIndex = sorted_paths.IndexOf(initial_image);
        }

        void Sort_decending(SortMethod method)
        {
            SortAcending(method);
            tabs[CurrentTabIndex].images.paths.Reverse();
            tabs[CurrentTabIndex].currentIndex = tabs[CurrentTabIndex].images.paths.ToList().IndexOf(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]);
        }

        void View_in_explorer(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Process.Start("explorer.exe", "/select, " + tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]);
        }

        bool CanExcectute()
        {
            if(tabs[CurrentTabIndex].currentIndex == -1)
            {
                return false;
            }
            if (CurrentTabIndex < 0)
            {
                return false;
            }
            if (!tabs.Any())
            {
                return false;
            }
            if (!tabs[CurrentTabIndex].images.paths.Any())
            {
                return false;
            }
            return true;
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

        void Sort(SortMethod sort_method)
        {
            switch (tabs[CurrentTabIndex].imageSettings.Current_sort_mode)
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

        void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            if (tabs[CurrentTabIndex].imageSettings.Current_sort_mode == SortMode.Ascending)
            {
                var inital_image = tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex];
                var file_paths_list = tabs[CurrentTabIndex].images.paths;
                file_paths_list.Reverse();
                FindImageAfterSort(file_paths_list, inital_image);
            }
            tabs[CurrentTabIndex].imageSettings.Current_sort_mode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            if (tabs[CurrentTabIndex].imageSettings.Current_sort_mode == SortMode.Descending)
            {
                var inital_image = tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex];
                var file_paths_list = tabs[CurrentTabIndex].images.paths;
                file_paths_list.Reverse();
                FindImageAfterSort(file_paths_list, inital_image);
            }
            tabs[CurrentTabIndex].imageSettings.Current_sort_mode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
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

            if (inToggleMode == false)
            {
                tabs[CurrentTabIndex].last_images = tabs[CurrentTabIndex].images;
                inToggleMode = true;
                beforeCompareModeIndex = tabs[CurrentTabIndex].currentIndex;
            }
            tabs[CurrentTabIndex].images.paths = new List<string> { tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex], compare_file };
            SetCurrentImage(0);
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            border.Reset();
        }

        void Display_all_channels(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.RGB);
        }

        void Display_red_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Red);
        }

        void Display_green_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Green);
        }

        void Display_alpha_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Alpha);
        }

        void Display_blue_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Blue);
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

        void ImageTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabs[CurrentTabIndex].currentIndex == -1)
            {
                return;
            }
            if (ImageTabControl.SelectedIndex < 0)
            {
                return;
            }
            if (tabs.Count == 1)
            {
                return;
            }
            var folder_path = Path.GetDirectoryName(tabs[ImageTabControl.SelectedIndex].initialImagePath);
            SupportedImageFilesInDirectory(folder_path);

            RefreshTab();
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            Clipboard.SetText($"\"{BackwardToForwardSlash(tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex])}\"");
        }

        static string BackwardToForwardSlash(string v) => v.Replace('\\', '/');

        void OpenInImageEditor(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }
            // Check if I can find the Image Editor 
            if (Settings.ImageEditor != null)
            {
                Process.Start(Settings.ImageEditor, tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]);
            }
            else
            {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?", "Missing Image Editor", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var file_dialog = new OpenFileDialog
                    {
                        Multiselect = false,
                        AddExtension = true,
                        Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"

                    };
                    file_dialog.ShowDialog();

                    Settings.ImageEditor = file_dialog.FileName;
                    Process.Start(Settings.ImageEditor, tabs[CurrentTabIndex].images.paths[tabs[CurrentTabIndex].currentIndex]);
                    // TODO Add this after I've implemented save.
                    // Settings.Save();
                }
            }
        }

        void UIPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Previous);
        }

        void UINext_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Next);
        }

        void UIAddNewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        void ImageTabControl_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            AddNewTab(Path.GetFullPath(filenames[0]));
        }

        void Border_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            ReplaceImageInTab(filenames[0]);
            RefreshTab();
        }

        void StartSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!CanExcectute())
            {
                return;
            }

            slideshowMode = true;
            UpdateTitle();
            currentSlideshowTime = 0;
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

            slideshowMode = false;
            UpdateTitle();
            currentSlideshowTime = 0;
            slideshowTimer.Stop();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void Slideshow1SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Sec1);

        void Slideshow2SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs2);

        void Slideshow3SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs3);

        void Slideshow4SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs4);

        void Slideshow5SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs5);

        void Slideshow10SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs10);

        void Slideshow20SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs20);

        void Slideshow30SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowIntervalSec.Secs30);

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

        void SlideshowIntervalUI(SlideshowIntervalSec newInterval)
        {
            if (!CanExcectute())
            {
                return;
            }

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
                        slideshowInterval = 1;
                        break;
                    }
                case SlideshowIntervalSec.Secs2:
                    {
                        Slideshow2SecUI.IsChecked = true;
                        slideshowInterval = 2;
                        break;
                    }
                case SlideshowIntervalSec.Secs3:
                    {
                        Slideshow3SecUI.IsChecked = true;
                        slideshowInterval = 3;
                        break;
                    }
                case SlideshowIntervalSec.Secs4:
                    {
                        Slideshow4SecUI.IsChecked = true;
                        slideshowInterval = 4;
                        break;
                    }
                case SlideshowIntervalSec.Secs5:
                    {
                        Slideshow5SecUI.IsChecked = true;
                        slideshowInterval = 5;
                        break;
                    }
                case SlideshowIntervalSec.Secs10:
                    {
                        Slideshow10SecUI.IsChecked = true;
                        slideshowInterval = 10;
                        break;
                    }
                case SlideshowIntervalSec.Secs20:
                    {
                        Slideshow20SecUI.IsChecked = true;
                        slideshowInterval = 20;
                        break;
                    }
                case SlideshowIntervalSec.Secs30:
                    {
                        Slideshow30SecUI.IsChecked = true;
                        slideshowInterval = 30;
                        break;
                    }
            }
        }
    }
}