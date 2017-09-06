// TODOS
// GIF Support
// Loading images without lag
// Split into more files
// Save window size and position on exit and use that on startup
// Drag and drop, drop into image area replaces tab with that image, drop into tab bar makes a new tab with image.
// Add tabbar color to settings file.
// Duplicate tab option

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
using System.Windows.Media.Imaging;
using SearchOption = System.IO.SearchOption;

namespace ImageViewer
{
    public partial class MainWindow
    {
        // TODO Expose somewhere, dropdown or something.
        int SlideshowInterval = 5;
        int CurrentSlideshowTime;
        int before_compare_mode_index;

        List<TabData> tabs = new List<TabData>();
        int cTabIndex;

        public int CTabIndex
        {
            get
            {
                if (ImageTabControl == null)
                {
                    return cTabIndex;
                }
                return ImageTabControl.SelectedIndex;
            }
        }

        bool slideshow_mode;
        static System.Windows.Threading.DispatcherTimer slideShowTimer;

        Image image_area;

        ImageSet images;
        bool in_toggle_mode;
        ImageSet last_images;
        bool scroll_key_down;
        Settings settings;

        public Channels DisplayChannel
        {
            get => tabs[CTabIndex].imageSettings.displayChannel;

            set
            {
                tabs[CTabIndex].imageSettings.displayChannel = value;
                Refresh_Image();
            }
        }

        public MainWindow()
        {
            settings = new Settings();
            settings.Load();

            var newTab = new TabItem { Header = Title, IsTabStop = false, FocusVisualStyle = null};
            tabs.Add(new TabData(newTab, null, 0));

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var file_path = Environment.GetCommandLineArgs()[1];
                tabs[CTabIndex].initialImagePath = file_path;
                var folder_path = Path.GetDirectoryName(file_path);
                Supported_image_files_in_directory(folder_path);
                if (images.paths.ToList().IndexOf(file_path) == -1)
                {
                    tabs[CTabIndex].currentIndex = 0;
                }
                else
                {
                    tabs[CTabIndex].currentIndex = images.paths.ToList().IndexOf(file_path);
                }
            }
            else
            {
                File_browser();
            }

            InitializeComponent();

            ImageTabControl.Items.Add(newTab);

            ImageTabControl.SelectedIndex = cTabIndex += 1;

            RefreshUI();
            SetupSlideshow();
        }

        void SetupSlideshow()
        {
            slideShowTimer = new System.Windows.Threading.DispatcherTimer();
            slideShowTimer.Tick += Slideshow;
            slideShowTimer.Interval = new TimeSpan(0, 0, 1);
        }

        void RefreshUI()
        {
            border.Background = settings.Background;
        }

        void Setup_file_watcher(string folder_path)
        {
            var watcher = new FileSystemWatcher
            {
                Path = folder_path,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => Supported_image_files_in_directory(Path.GetDirectoryName(tabs[ImageTabControl.SelectedIndex].initialImagePath));
            watcher.Created += (sender, args) => Supported_image_files_in_directory(Path.GetDirectoryName(tabs[ImageTabControl.SelectedIndex].initialImagePath));
            watcher.Deleted += (sender, args) => Supported_image_files_in_directory(Path.GetDirectoryName(tabs[ImageTabControl.SelectedIndex].initialImagePath));
            watcher.Renamed += (sender, args) => Supported_image_files_in_directory(Path.GetDirectoryName(tabs[ImageTabControl.SelectedIndex].initialImagePath));

            watcher.EnableRaisingEvents = true;
        }

        bool File_browser()
        {
            var file_dialog = Show_open_file_dialog();
            if (string.IsNullOrEmpty(file_dialog.SafeFileName))
                return false;

            var folder_path = Path.GetDirectoryName(file_dialog.FileName);
            tabs[CTabIndex].initialImagePath = file_dialog.FileName;
            Supported_image_files_in_directory(folder_path);

            var filenameIndex = images.paths.IndexOf(file_dialog.FileName);
            if (filenameIndex == -1)
            {
                tabs[CTabIndex].currentIndex = 0;
            }
            else
            {
                tabs[CTabIndex].currentIndex = filenameIndex;
            }

            return true;
        }

        static OpenFileDialog Show_open_file_dialog()
        {
            var file_dialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = FileFormats.filter_string

            };
            file_dialog.ShowDialog();
            return file_dialog;
        }

        void Supported_image_files_in_directory(string path)
        {

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (images.paths != null)
            {
                images.paths.Clear();
            }
            else
            {
                images.paths = new List<string>();
            }

            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (FileFormats.supported_extensions.Contains(Path.GetExtension(file).Remove(0, 1), StringComparer.OrdinalIgnoreCase))
                {
                    images.paths.Add(file);
                }
            }

            if (images.Is_valid())
            {
                // TODO File watcher crashes atm, different thread calling and so on, need a dispatcher?
                // Setup_file_watcher(path);
                Sort_acending(SortMethod.Name);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (tabs.Any() && images.paths.Any())
            {
                if (scroll_key_down)
                {
                    if (e.Delta > 0)
                    {
                        Switch_Image(SwitchDirection.Next);
                    }
                    else
                    {
                        Switch_Image(SwitchDirection.Previous);
                    }
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (e.Source == border || e.Source == ImageArea)
            {
                if (in_toggle_mode)
                {
                    Exit_toggle_mode();
                }
                if (File_browser())
                {
                    Display_image();
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            switch (e.Key)
            {
                case Key.LeftCtrl:
                    {
                        scroll_key_down = false;
                        break;
                    }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == settings.Hotkeys[Command.NextImage])
            {
                if (tabs.Any() && images.paths.Any())
                {
                    Switch_Image(SwitchDirection.Next);
                }
            }
            else if (e.Key == settings.Hotkeys[Command.PreviousImage])
            {
                if (tabs.Any() && images.paths.Any())
                {
                    Switch_Image(SwitchDirection.Previous);
                }
            }
            else if (e.Key == settings.Hotkeys[Command.DeleteImage])
            {
                if (tabs.Any() && images.paths.Any())
                {
                    var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                              $"Delete {FileSystem.GetName(images.paths[tabs[CTabIndex].currentIndex])}", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        FileSystem.DeleteFile(images.paths[tabs[CTabIndex].currentIndex], UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                        if (images.paths.Count > 0)
                        {
                            Switch_Image(SwitchDirection.Next);
                            Supported_image_files_in_directory(Path.GetDirectoryName(images.paths[tabs[CTabIndex].currentIndex]));
                        }
                        else
                        {
                            if (File_browser())
                            {
                                Display_image();
                            }
                        }
                    }
                }
            }
            switch (e.Key)
            {
                // TODO
                // Add these to the settings file
                // UI Controls
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
                            var file_dialog = Show_open_file_dialog();
                            if (string.IsNullOrEmpty(file_dialog.SafeFileName))
                                return;

                            var newTab = new TabItem { Header = file_dialog.FileName, IsTabStop = false, FocusVisualStyle = null };

                            var folder_path = Path.GetDirectoryName(file_dialog.FileName);
                            tabs.Add(new TabData(newTab, folder_path));

                            ImageTabControl.Items.Add(newTab);

                            ImageTabControl.SelectedIndex = CTabIndex + 1;

                            Supported_image_files_in_directory(folder_path);

                            var filenameIndex = images.paths.ToList().IndexOf(file_dialog.FileName);
                            if (filenameIndex == -1)
                            {
                                tabs[CTabIndex].currentIndex = 0;
                            }
                            else
                            {
                                tabs[CTabIndex].currentIndex = filenameIndex;
                            }

                            tabs[CTabIndex].initialImagePath = file_dialog.FileName;

                            RefreshTab();
                        }
                        break;
                    }
                case Key.W:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            tabs.RemoveAt(ImageTabControl.SelectedIndex);
                            ImageTabControl.Items.RemoveAt(ImageTabControl.SelectedIndex);
                        }
                        break;
                    }
                case Key.S:
                    {
                        slideshow_mode = slideshow_mode != true;
                        if (slideshow_mode)
                        {
                            slideShowTimer.Start();
                        }
                        else
                        {
                            slideShowTimer.Stop();
                        }
                        UpdateTitle();
                        CurrentSlideshowTime = 0;
                        break;
                    }
                case Key.LeftCtrl:
                    {
                        scroll_key_down = true;
                        break;
                    }
                case Key.Escape:
                    {
                        if (in_toggle_mode)
                        {
                            Exit_toggle_mode();
                        }
                        else
                        {
                            Close();
                        }
                        break;
                    }
            }
        }

        void RefreshTab()
        {
            Display_image();
            UpdateTitle();
        }

        void Slideshow(object source, EventArgs e)
        {
            if (tabs.Any() && images.paths.Any())
            {
                if (CurrentSlideshowTime < SlideshowInterval)
                {
                    CurrentSlideshowTime += 1;
                    UpdateTitle();
                }
                else
                {
                    CurrentSlideshowTime = 0;
                    slideShowTimer.Stop();
                    Switch_Image(SwitchDirection.Next);
                    slideShowTimer.Start();
                }
            }
            else
            {
                slideshow_mode = false;
            }
            if (!slideshow_mode)
            {
                slideShowTimer.Stop();
                UpdateTitle();
                CurrentSlideshowTime = 0;
            }

        }

        void Exit_toggle_mode()
        {
            images = last_images;
            in_toggle_mode = false;
            Set_current_image(before_compare_mode_index);
        }

        void Set_current_image(int new_index)
        {
            tabs[CTabIndex].currentIndex = new_index;
            Display_image();
        }

        enum SwitchDirection
        {
            Next,
            Previous
        }

        void Switch_Image(SwitchDirection switchDirection)
        {
            if (slideshow_mode)
            {
                CurrentSlideshowTime = 0;
            }
            image_area.Source = null;
            UpdateLayout();
            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (tabs[CTabIndex].currentIndex < images.paths.Count - 1)
                    {
                        Set_current_image(tabs[CTabIndex].currentIndex += 1);
                    }
                    else
                    {
                        Set_current_image(0);
                    }
                    break;
                case SwitchDirection.Previous:

                    if (tabs[CTabIndex].currentIndex > 0)
                    {
                        Set_current_image(tabs[CTabIndex].currentIndex -= 1);
                    }
                    else
                    {
                        Set_current_image(tabs[CTabIndex].currentIndex = images.paths.Count - 1);
                    }
                    break;
            }
        }

        void Imagearea_onloaded(object sender, RoutedEventArgs e)
        {
            image_area = sender as Image;
            if (image_area != null)
            {
                Display_image();
            }
        }

        void Display_image()
        {
            if (image_area != null)
            {
                if (images.Is_valid())
                {
                    var image = Load_image(images.paths[tabs[CTabIndex].currentIndex]);

                    image_area.Source = image;
                    if (image.Height > border.ActualHeight)
                    {
                        image_area.Height = border.ActualHeight;
                    }
                    else if (image.Width > border.ActualWidth)
                    {
                        image_area.Width = border.ActualWidth;
                    }
                    else
                    {
                        if (image.Width < border.ActualWidth)
                        {
                            image_area.Height = image.Height;
                        }
                        else
                        {
                            image_area.Width = image.Width;
                        }
                    }

                    UpdateTitle();
                    border.Reset();
                }
            }
        }

        void UpdateTitle()
        {
            if (images.paths == null)
            {
                return;
            }

            string title;

            if (in_toggle_mode)
            {
                title = $" [Toggle] {new FileInfo(images.paths[tabs[CTabIndex].currentIndex]).Name}";
            }
            else
            {
                title = new FileInfo(images.paths[tabs[CTabIndex].currentIndex]).Name;
            }

            if (slideshow_mode)
            {
                title += $" [Slideshow] {CurrentSlideshowTime}";
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
            tabs[CTabIndex].tabItem.Header = title;
        }

        void Refresh_Image()
        {
            if (images.Is_valid())
            {
                var image = Load_image(images.paths[tabs[CTabIndex].currentIndex]);

                image_area.Source = image;

                UpdateTitle();
            }
        }

        BitmapSource Load_image(string filepath)
        {
            MagickImage img;
            try
            {
                img = new MagickImage(filepath);
            }
            catch (MagickMissingDelegateErrorException)
            {
                img = new MagickImage(MagickColors.White, 512, 512);
                new Drawables()
                  .FontPointSize(18)
                  .Font("Arial")
                  .FillColor(MagickColors.Red)
                  .TextAlignment(ImageMagick.TextAlignment.Center)
                  .Text(256, 256, $"Could not load\n{Path.GetFileName(filepath)}")
                  .Draw(img);
            }

            switch (DisplayChannel)
            {
                case Channels.Red:
                    {
                        return img.Separate(Channels.Red).ElementAt(0)?.ToBitmapSource();
                    }
                case Channels.Green:
                    {
                        return img.Separate(Channels.Green).ElementAt(0)?.ToBitmapSource();
                    }
                case Channels.Blue:
                    {
                        return img.Separate(Channels.Blue).ElementAt(0)?.ToBitmapSource();
                    }
                case Channels.Alpha:
                    {
                        return img.Separate(Channels.Alpha).ElementAt(0)?.ToBitmapSource();
                    }
                default:
                    {
                        img.Alpha(AlphaOption.Opaque);
                        return img.ToBitmapSource();
                    }
            }
        }

        void Sort_acending(SortMethod method)
        {
            var id = 0;
            var initial_image = images.paths[tabs[CTabIndex].currentIndex];
            List<string> sorted_paths;
            switch (method)
            {
                case SortMethod.Name:
                    {
                        sorted_paths = images.paths.ToList();
                        sorted_paths.Sort();

                        break;
                    }

                case SortMethod.Date:
                    {
                        var keys = images.paths.ToList().Select(s => new FileInfo(s).LastWriteTime).ToList();

                        var date_time_lookup = keys.Zip(images.paths.ToList(), (k, v) => new { k, v })
                                                   .ToLookup(x => x.k, x => x.v);

                        var id_list = date_time_lookup.SelectMany(pair => pair,
                                                          (pair, value) => new FileId<DateTime>(value, pair.Key, id += 1))
                                                      .ToList();

                        var date_id_dictionary = id_list.ToDictionary(x => x.Item.AddMilliseconds(x.Id), x => x.Id);
                        sorted_paths = Type_sort(id_list, date_id_dictionary);

                        break;
                    }
                case SortMethod.Size:
                    {
                        var keys = images.paths.ToList().Select(s => new FileInfo(s).Length).ToList();
                        var size_lookup = keys.Zip(images.paths.ToList(), (k, v) => new { k, v })
                                              .ToLookup(x => x.k, x => x.v);

                        var id_list = size_lookup.SelectMany(pair => pair,
                                                     (pair, value) => new FileId<long>(value, pair.Key, id += 1)).ToList();


                        var date_id_dictionary = id_list.ToDictionary(x => x.Item + x.Id, x => x.Id);
                        sorted_paths = Type_sort(id_list, date_id_dictionary);

                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(SortMethod), method, null);
                    }
            }
            Find_image_after_sort(sorted_paths, initial_image);

        }

        static List<string> Type_sort<T>(IEnumerable<FileId<T>> id_list, Dictionary<T, int> dictionary)
        {
            var id_file_dictionary = id_list.ToDictionary(x => x.Id, x => x.Path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            var sorted_paths = keys.Select(l => dictionary[l]).ToList().Select(l => id_file_dictionary[l]).ToList();
            return sorted_paths;
        }

        void Find_image_after_sort(List<string> sorted_paths, string initial_image)
        {
            images.paths = sorted_paths;
            tabs[CTabIndex].currentIndex = sorted_paths.IndexOf(initial_image);
        }

        void Sort_decending(SortMethod method)
        {
            Sort_acending(method);
            images.paths.Reverse();
            tabs[CTabIndex].currentIndex = images.paths.ToList().IndexOf(images.paths[tabs[CTabIndex].currentIndex]);
        }

        void View_in_explorer(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select, " + images.paths[tabs[CTabIndex].currentIndex]);
        }

        void Sort_by_name(object sender, RoutedEventArgs e)
        {
            Sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void Sort_by_size(object sender, RoutedEventArgs e)
        {
            Sort(SortMethod.Size);
            SortSize.IsChecked = true;
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void Sort_by_date_modified(object sender, RoutedEventArgs e)
        {
            Sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void Sort(SortMethod sort_method)
        {
            switch (tabs[CTabIndex].imageSettings.Current_sort_mode)
            {
                case SortMode.Ascending:
                    {
                        Sort_acending(sort_method);
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
            if (tabs[CTabIndex].imageSettings.Current_sort_mode == SortMode.Ascending)
            {
                var inital_image = images.paths[tabs[CTabIndex].currentIndex];
                var file_paths_list = images.paths;
                file_paths_list.Reverse();
                Find_image_after_sort(file_paths_list, inital_image);
            }
            tabs[CTabIndex].imageSettings.Current_sort_mode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (tabs[CTabIndex].imageSettings.Current_sort_mode == SortMode.Descending)
            {
                var inital_image = images.paths[tabs[CTabIndex].currentIndex];
                var file_paths_list = images.paths;
                file_paths_list.Reverse();
                Find_image_after_sort(file_paths_list, inital_image);
            }
            tabs[CTabIndex].imageSettings.Current_sort_mode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void Compare_onclick(object sender, RoutedEventArgs e)
        {
            var compare_file = Show_open_file_dialog().FileName;
            if (string.IsNullOrEmpty(compare_file))
                return;

            if (in_toggle_mode == false)
            {
                last_images = images;
                in_toggle_mode = true;
                before_compare_mode_index = tabs[CTabIndex].currentIndex;
            }
            images.paths = new List<string> { images.paths[tabs[CTabIndex].currentIndex], compare_file };
            Set_current_image(0);
        }

        class FileId<T>
        {
            public FileId(string path, T item, int id)
            {
                Path = path;
                Item = item;
                Id = id;
            }

            public string Path { get; }
            public T Item { get; }
            public int Id { get; }
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
            if (ImageTabControl.SelectedIndex < 0)
            {
                return;
            }
            var folder_path = Path.GetDirectoryName(tabs[ImageTabControl.SelectedIndex].initialImagePath);
            Supported_image_files_in_directory(folder_path);

            RefreshTab();
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText($"\"{BackwardToForwardSlash(images.paths[tabs[CTabIndex].currentIndex])}\"");
        }

        static string BackwardToForwardSlash(string v) => v.Replace('\\', '/');

        void OpenInImageEditor(object sender, RoutedEventArgs e)
        {
            if (settings.ImageEditor != null)
            {
                Process.Start(settings.ImageEditor, images.paths[tabs[CTabIndex].currentIndex]);
            }
            else
            {
                MessageBox.Show("No image editor specified in settings file", "Missing Image Editor", MessageBoxButton.OK);
            }
        }

        void UIPrevious_Click(object sender, RoutedEventArgs e)
        {
            Switch_Image(SwitchDirection.Previous);
        }

        void UINext_Click(object sender, RoutedEventArgs e)
        {
            Switch_Image(SwitchDirection.Next);
        }
    }
}