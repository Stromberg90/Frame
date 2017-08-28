// TODOS
// GIF Support
// Loading images without lag
// Split into more files
// Slide Show Mode

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SearchOption = System.IO.SearchOption;
using ImageMagick;


namespace ImageViewer
{
    public partial class MainWindow
    {
        private int before_compare_mode_index;
        private int current_index;

        private Image image_area;

        private ImageSet images;
        private bool in_toggle_mode;
        private ImageSet last_images;
        private bool scroll_key_down;
        static string[] supported_extensions = { "bmp", "gif", "ico", "jpg", "png", "wdp", "tiff", "tga", "dds", "hdr", "exr" };
        static string filter_string = ConstructFilterString();
        private Settings settings;
        // TODO
        // Move channel into the settings file?
        private Channels displayChannel = Channels.RGB;

        public MainWindow()
        {
            settings = new Settings();
            settings.Load();

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                var file_path = Environment.GetCommandLineArgs()[1];
                var folder_path = Path.GetDirectoryName(file_path);
                Supported_image_files_in_directory(folder_path);
                if (images.paths.ToList().IndexOf(file_path) == -1)
                {
                    current_index = 0;
                }
                else
                {
                    current_index = images.paths.ToList().IndexOf(file_path);
                }
            }
            else
            {
                File_browser();
            }

            if (images.Is_valid())
            {
                Sort_acending(SortMethod.Name);
            }

            InitializeComponent();

            border.Background = settings.Background;

            SortName.IsChecked = true;

        }

        private static string ConstructFilterString()
        {
            var new_filter_string = new System.Text.StringBuilder();
            new_filter_string.Append("Image files (");
            for (int i = 0; i < supported_extensions.Length; i++)
            {
                if (i < supported_extensions.Length)
                {
                    new_filter_string.Append("*." + supported_extensions[i] + ", ");
                }
                else
                {
                    new_filter_string.Append("*." + supported_extensions[i] + ")");
                }
            }
            new_filter_string.Append(" | ");
            for (int i = 0; i < supported_extensions.Length; i++)
            {
                if (i < supported_extensions.Length)
                {
                    new_filter_string.Append("*." + supported_extensions[i] + "; ");
                }
                else
                {
                    new_filter_string.Append("*." + supported_extensions[i]);
                }
            }
            return new_filter_string.ToString();
        }

        public SortMode Current_sort_mode { get; set; }


        private void Setup_file_watcher(string folder_path)
        {
            var watcher = new FileSystemWatcher
            {
                Path = folder_path,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => Supported_image_files_in_directory(folder_path);
            watcher.Created += (sender, args) => Supported_image_files_in_directory(folder_path);
            watcher.Deleted += (sender, args) => Supported_image_files_in_directory(folder_path);
            watcher.Renamed += (sender, args) => Supported_image_files_in_directory(folder_path);

            watcher.EnableRaisingEvents = true;
        }

        private bool File_browser()
        {
            var file_dialog = Show_open_file_dialog();
            if (string.IsNullOrEmpty(file_dialog.FileName))
                return false;

            var folder_path = Path.GetDirectoryName(file_dialog.FileName);

            Supported_image_files_in_directory(folder_path);
            if (images.paths.ToList().IndexOf(file_dialog.FileName) == -1)
            {
                current_index = 0;
            }
            else
            {
                current_index = images.paths.ToList().IndexOf(file_dialog.FileName);
            }
            return true;
        }

        private static OpenFileDialog Show_open_file_dialog()
        {
            var file_dialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = filter_string

            };
            file_dialog.ShowDialog();
            return file_dialog;
        }

        private void Supported_image_files_in_directory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var ls =
                Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                         .Where(
                             s =>
                                 s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".wdp", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".dds", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".hdr", StringComparison.OrdinalIgnoreCase) ||
                                 s.EndsWith(".exr", StringComparison.OrdinalIgnoreCase))
                         .ToArray();

            images.paths = ls;
            if (images.paths.Any())
            {
                Setup_file_watcher(path);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

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

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
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

            if (e.Key == settings.Hotkeys[Commands.NextImage])
            {
                Switch_Image(SwitchDirection.Next);
            }
            else if (e.Key == settings.Hotkeys[Commands.PreviousImage])
            {
                Switch_Image(SwitchDirection.Previous);
            }
            else if (e.Key == settings.Hotkeys[Commands.DeleteImage])
            {
                var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                $"Delete {FileSystem.GetName(images.paths[current_index])}", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    FileSystem.DeleteFile(images.paths[current_index], UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin);
                    if (images.paths.Length > 0)
                    {
                        Switch_Image(SwitchDirection.Next);
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
            switch (e.Key)
            {
                // TODO
                // Add these to the settings file
                // UI Controls
                case Key.R:
                    {
                        ToggleRedChannel();
                        break;
                    }
                case Key.G:
                    {
                        ToggleGreenChannel();
                        break;
                    }
                case Key.B:
                    {
                        ToggleBlueChannel();
                        break;
                    }
                case Key.F:
                    {
                        border.Reset();
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

        private void Exit_toggle_mode()
        {
            images = last_images;
            in_toggle_mode = false;
            Set_current_image(before_compare_mode_index);
        }

        private void Set_current_image(int new_index)
        {
            current_index = new_index;
            Display_image();
        }

        enum SwitchDirection
        {
            Next,
            Previous
        }

        private void Switch_Image(SwitchDirection switchDirection)
        {
            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    image_area.Source = null;
                    UpdateLayout();
                    if (current_index < images.paths.Length - 1)
                    {
                        Set_current_image(current_index += 1);
                    }
                    //Wraps around.
                    else
                    {
                        Set_current_image(0);
                    }
                    break;
                case SwitchDirection.Previous:
                    image_area.Source = null;
                    UpdateLayout();
                    if (current_index > 0)
                    {
                        Set_current_image(current_index -= 1);
                    }
                    //Wraps around.
                    else
                    {
                        Set_current_image(current_index = images.paths.Length - 1);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Imagearea_onloaded(object sender, RoutedEventArgs e)
        {
            image_area = sender as Image;
            if (image_area != null)
            {
                Display_image();
            }
        }

        private void Display_image()
        {
            if (images.Is_valid())
            {
                var image = Load_image(images.paths[current_index]);

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

                if (in_toggle_mode)
                {
                    Title = $"[Toggle] {new FileInfo(images.paths[current_index]).Name}";
                }
                else
                {
                    Title = new FileInfo(images.paths[current_index]).Name;
                }
                border.Reset();
            }
        }

        private void Refresh_Image()
        {
            if (images.Is_valid())
            {
                var image = Load_image(images.paths[current_index]);

                image_area.Source = image;

                if (in_toggle_mode)
                {
                    Title = $"[Toggle] {new FileInfo(images.paths[current_index]).Name}";
                }
                else
                {
                    Title = new FileInfo(images.paths[current_index]).Name;
                }
            }
        }

        private BitmapSource Load_image(string filepath)
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

            // TODO
            // Add alpha channel toggle.
            switch (displayChannel)
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
                default:
                    {
                        img.Alpha(AlphaOption.Opaque);
                        return img.ToBitmapSource();
                    }
            }
        }

        private void Sort_acending(SortMethod method)
        {
            var id = 0;
            var initial_image = images.paths[current_index];
            switch (method)
            {
                case SortMethod.Name:
                    {
                        var file_paths_list_sorted = images.paths.ToList();
                        file_paths_list_sorted.Sort();

                        Find_image_after_sort(file_paths_list_sorted, initial_image);
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
                        var sorted_paths = Type_sort(id_list, date_id_dictionary);

                        Find_image_after_sort(sorted_paths, initial_image);
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
                        var sorted_paths = Type_sort(id_list, date_id_dictionary);

                        Find_image_after_sort(sorted_paths, initial_image);
                        break;
                    }
                default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(SortMethod), method, null);
                    }
            }
        }

        private static List<string> Type_sort<T>(IEnumerable<FileId<T>> id_list, Dictionary<T, int> dictionary)
        {
            var id_file_dictionary = id_list.ToDictionary(x => x.Id, x => x.Path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            var sorted_paths = keys.Select(l => dictionary[l]).ToList().Select(l => id_file_dictionary[l]).ToList();
            return sorted_paths;
        }

        private void Find_image_after_sort(List<string> sorted_paths, string initial_image)
        {
            images.paths = sorted_paths.ToArray();
            current_index = sorted_paths.IndexOf(initial_image);
        }

        private void Sort_decending(SortMethod method)
        {
            Sort_acending(method);
            var initial_image = images.paths[current_index];
            images.paths = images.paths.Reverse().ToArray();
            current_index = images.paths.ToList().IndexOf(initial_image);
        }

        private void View_in_explorer(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", "/select, " + images.paths[current_index]);
        }

        private void Sort_by_name(object sender, RoutedEventArgs e)
        {
            Sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        private void Sort_by_size(object sender, RoutedEventArgs e)
        {
            Sort(SortMethod.Size);
            SortSize.IsChecked = true;
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
        }

        private void Sort_by_date_modified(object sender, RoutedEventArgs e)
        {
            Sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        private void Sort(SortMethod sort_method)
        {
            switch (Current_sort_mode)
            {
                case SortMode.Ascending:
                    {
                        Sort_acending(sort_method);
                        break;
                    }
                case SortMode.Decending:
                    {
                        Sort_decending(sort_method);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (Current_sort_mode == SortMode.Ascending)
            {
                var inital_image = images.paths[current_index];
                var file_paths_list = images.paths.Reverse().ToList();
                Find_image_after_sort(file_paths_list, inital_image);
            }
            Current_sort_mode = SortMode.Decending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        private void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (Current_sort_mode == SortMode.Decending)
            {
                var inital_image = images.paths[current_index];
                var file_paths_list = images.paths.Reverse().ToList();
                Find_image_after_sort(file_paths_list, inital_image);
            }
            Current_sort_mode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        private void Compare_onclick(object sender, RoutedEventArgs e)
        {
            var compare_file = Show_open_file_dialog().FileName;
            if (string.IsNullOrEmpty(compare_file))
                return;

            if (in_toggle_mode == false)
            {
                last_images = images;
                in_toggle_mode = true;
                before_compare_mode_index = current_index;
            }
            images.paths = new[] { images.paths[current_index], compare_file };
            Set_current_image(0);
        }

        private class FileId<T>
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

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            border.Reset();
        }

        private void Display_all_channels(object sender, RoutedEventArgs e)
        {
            DisplayAllChannels();
        }

        private void DisplayAllChannels()
        {
            displayChannel = Channels.RGB;
            AllChannels.IsChecked = true;
            RedChannel.IsChecked = false;
            GreenChannel.IsChecked = false;
            BlueChannel.IsChecked = false;
            Refresh_Image();
        }

        private void Display_red_channel(object sender, RoutedEventArgs e)
        {
            ToggleRedChannel();
        }

        private void ToggleRedChannel()
        {
            if (displayChannel == Channels.Red)
            {
                AllChannels.IsChecked = true;
                RedChannel.IsChecked = false;
                GreenChannel.IsChecked = false;
                BlueChannel.IsChecked = false;
            }
            else
            {
                AllChannels.IsChecked = false;
                RedChannel.IsChecked = true;
                GreenChannel.IsChecked = false;
                BlueChannel.IsChecked = false;
            }
            displayChannel = displayChannel == Channels.Red ? Channels.RGB : Channels.Red;
            Refresh_Image();
        }

        private void Display_green_channel(object sender, RoutedEventArgs e)
        {
            ToggleGreenChannel();
        }

        private void ToggleGreenChannel()
        {
            if (displayChannel == Channels.Green)
            {
                AllChannels.IsChecked = true;
                RedChannel.IsChecked = false;
                GreenChannel.IsChecked = false;
                BlueChannel.IsChecked = false;
            }
            else
            {
                AllChannels.IsChecked = false;
                RedChannel.IsChecked = false;
                GreenChannel.IsChecked = true;
                BlueChannel.IsChecked = false;
            }
            displayChannel = displayChannel == Channels.Green ? Channels.RGB : Channels.Green;
            Refresh_Image();
        }

        private void Display_blue_channel(object sender, RoutedEventArgs e)
        {
            ToggleBlueChannel();
        }

        private void ToggleBlueChannel()
        {
            if (displayChannel == Channels.Blue)
            {
                AllChannels.IsChecked = true;
                RedChannel.IsChecked = false;
                GreenChannel.IsChecked = false;
                BlueChannel.IsChecked = false;
            }
            else
            {
                AllChannels.IsChecked = false;
                RedChannel.IsChecked = false;
                GreenChannel.IsChecked = false;
                BlueChannel.IsChecked = true;
            }
            displayChannel = displayChannel == Channels.Blue ? Channels.RGB : Channels.Blue;
            Refresh_Image();
        }
    }
}