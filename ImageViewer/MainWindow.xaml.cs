#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using SearchOption = System.IO.SearchOption;

#endregion

namespace ImageViewer {
    [Guid("43D72BD8-1F76-4334-84EB-9E4912E24463")]
    public partial class MainWindow {
        private int before_compare_mode_index;
        private int current_index;

        private Image image_area;

        private ImageSet images;
        private bool in_compared_mode;
        private ImageSet last_images;
        private bool scroll_key_down;

        public MainWindow() {
            if (Environment.GetCommandLineArgs().Length > 1) {
                var file_path = Environment.GetCommandLineArgs()[1];
                var folder_path = Path.GetDirectoryName(file_path);
                supported_image_files_in_directory(folder_path);
                if (images.paths.ToList().IndexOf(file_path) == -1)
                {
                    current_index = 0;
                }
                else
                {
                    current_index = images.paths.ToList().IndexOf(file_path);
                }
            }
            else {
                file_browser();
            }

            if (images.is_valid()) {
                sort_acending(SortMethod.Name);                
            }

            InitializeComponent();
            SortName.IsChecked = true;
        }

        public SortMode current_sort_mode { get; set; }


        private void setup_file_watcher(string folder_path) {
            var watcher = new FileSystemWatcher {
                Path = folder_path,
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => supported_image_files_in_directory(folder_path);
            watcher.Created += (sender, args) => supported_image_files_in_directory(folder_path);
            watcher.Deleted += (sender, args) => supported_image_files_in_directory(folder_path);
            watcher.Renamed += (sender, args) => supported_image_files_in_directory(folder_path);

            watcher.EnableRaisingEvents = true;
        }

        private bool file_browser() {
            var file_dialog = show_open_file_dialog();
            if (string.IsNullOrEmpty(file_dialog.FileName))
                return false;

            var folder_path = Path.GetDirectoryName(file_dialog.FileName);

            supported_image_files_in_directory(folder_path);
            if (images.paths.ToList().IndexOf(file_dialog.FileName) == -1) {
                current_index = 0;
            }
            else {
                current_index = images.paths.ToList().IndexOf(file_dialog.FileName);
            }
            return true;
        }

        private static OpenFileDialog show_open_file_dialog() {
            var file_dialog = new OpenFileDialog {
                Multiselect = false,
                AddExtension = true,
                Filter =
                    "Image files (*.bmp, *.gif, *.ico, *.jpg, *.png, *.wdp, *.tiff) | *.bmp; *.gif; *.ico; *.jpg; *.png; *.wdp; *.tiff"
            };
            file_dialog.ShowDialog();
            return file_dialog;
        }

        private void supported_image_files_in_directory(string path) {
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
                                 s.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
                         .ToArray();

            images.paths = ls;
            if (images.paths.Any()) {
                setup_file_watcher(path);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e) {
            base.OnMouseWheel(e);

            if (scroll_key_down) {
                if (e.Delta > 0) {
                    next_image();
                }
                else {
                    previous_image();
                }
            }
        }

        protected override void OnMouseDoubleClick(MouseButtonEventArgs e) {
            if (in_compared_mode) {
                exit_compare_mode();
            }
            if (file_browser()) {
                display_image();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e) {
            base.OnKeyUp(e);
            switch (e.Key) {
                case Key.LeftCtrl: {
                    scroll_key_down = false;
                    break;
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);

            switch (e.Key) {
                case Key.Delete: {
                    var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                        $"Delete {FileSystem.GetName(images.paths[current_index])}", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes) {
                        FileSystem.DeleteFile(images.paths[current_index], UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                        if (images.paths.Length > 0) {
                            next_image();
                        }
                        else {
                            Close();
                        }
                    }
                    break;
                }
                case Key.LeftCtrl: {
                    scroll_key_down = true;
                    break;
                }
                case Key.Right: {
                    next_image();
                    break;
                }
                case Key.Left: {
                    previous_image();
                    break;
                }
                case Key.Escape: {
                    if (in_compared_mode) {
                        exit_compare_mode();
                    }
                    else {
                        Close();
                    }
                    break;
                }
            }
        }

        private void exit_compare_mode() {
            images = last_images;
            in_compared_mode = false;
            set_current_image(before_compare_mode_index);
        }

        private void set_current_image(int new_index) {
            current_index = new_index;
            display_image();
        }

        private void previous_image() {
            image_area.Source = null;
            UpdateLayout();
            if (current_index > 0) {
                set_current_image(current_index -= 1);
            }
            //Wraps around.
            else {
                set_current_image(current_index = images.paths.Length - 1);
            }
        }

        private void next_image() {
            image_area.Source = null;
            UpdateLayout();
            if (current_index < images.paths.Length - 1) {
                set_current_image(current_index += 1);
            }
            //Wraps around.
            else {
                set_current_image(0);
            }
        }

        private void imagearea_onloaded(object sender, RoutedEventArgs e) {
            image_area = sender as Image;
            if (image_area != null) {
                display_image();
            }
        }

        private void display_image() {
            if (images.is_valid()) {
                var image = load_image(images.paths[current_index]);

                image_area.Source = image;
                if (image.Height > border.ActualHeight) {
                    image_area.Height = border.ActualHeight;
                }
                else if (image.Width > border.ActualWidth) {
                    image_area.Width = border.ActualWidth;
                }
                else {
                    if (image.Width < border.ActualWidth) {
                        image_area.Height = image.Height;
                    }
                    else {
                        image_area.Width = image.Width;
                    }
                }

                if (in_compared_mode) {
                    Title = $"[Compare] {new FileInfo(images.paths[current_index]).Name}";
                }
                else {
                    Title = $"{new FileInfo(images.paths[current_index]).Name}";
                }
                border.Reset();
            }
        }

        private static BitmapImage load_image(string filepath) {
            var bm = new BitmapImage();
            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read)) {
                bm.BeginInit();
                bm.CacheOption = BitmapCacheOption.OnLoad;
                bm.StreamSource = fs;
                bm.EndInit();
            }
            bm.Freeze();
            return bm;
        }

        private void sort_acending(SortMethod method) {
            var id = 0;
            var initial_image = images.paths[current_index];
            switch (method) {
                case SortMethod.Name: {
                    var file_paths_list_sorted = images.paths.ToList();
                    file_paths_list_sorted.Sort();

                    find_image_after_sort(file_paths_list_sorted, initial_image);
                    break;
                }

                case SortMethod.Date: {
                    var keys = images.paths.ToList().Select(s => new FileInfo(s).LastWriteTime).ToList();

                    var date_time_lookup = keys.Zip(images.paths.ToList(), (k, v) => new {k, v})
                                               .ToLookup(x => x.k, x => x.v);

                    var id_list = date_time_lookup.SelectMany(pair => pair,
                                                      (pair, value) => new FileId<DateTime>(value, pair.Key, id += 1))
                                                  .ToList();

                    var date_id_dictionary = id_list.ToDictionary(x => x.item.AddMilliseconds(x.id), x => x.id);
                    var sorted_paths = type_sort(id_list, date_id_dictionary);

                    find_image_after_sort(sorted_paths, initial_image);
                    break;
                }
                case SortMethod.Size: {
                    var keys = images.paths.ToList().Select(s => new FileInfo(s).Length).ToList();
                    var size_lookup = keys.Zip(images.paths.ToList(), (k, v) => new {k, v})
                                          .ToLookup(x => x.k, x => x.v);

                    var id_list = size_lookup.SelectMany(pair => pair,
                                                 (pair, value) => new FileId<long>(value, pair.Key, id += 1)).ToList();


                    var date_id_dictionary = id_list.ToDictionary(x => x.item + x.id, x => x.id);
                    var sorted_paths = type_sort(id_list, date_id_dictionary);

                    find_image_after_sort(sorted_paths, initial_image);
                    break;
                }
                default: {
                    throw new ArgumentOutOfRangeException(nameof(SortMethod), method, null);
                }
            }
        }

        private static List<string> type_sort<T>(IEnumerable<FileId<T>> id_list, Dictionary<T, int> dictionary) {
            var id_file_dictionary = id_list.ToDictionary(x => x.id, x => x.path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            var sorted_paths = keys.Select(l => dictionary[l]).ToList().Select(l => id_file_dictionary[l]).ToList();
            return sorted_paths;
        }

        private void find_image_after_sort(List<string> sorted_paths, string initial_image) {
            images.paths = sorted_paths.ToArray();
            current_index = sorted_paths.IndexOf(initial_image);
        }

        private void sort_decending(SortMethod method) {
            sort_acending(method);
            var initial_image = images.paths[current_index];
            images.paths = images.paths.Reverse().ToArray();
            current_index = images.paths.ToList().IndexOf(initial_image);
        }

        private void view_in_explorer(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe", "/select, " + images.paths[current_index]);
        }

        private void sort_by_name(object sender, RoutedEventArgs e) {
            sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        private void sort_by_size(object sender, RoutedEventArgs e) {
            sort(SortMethod.Size);
            SortSize.IsChecked = true;
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
        }

        private void sort_by_date_modified(object sender, RoutedEventArgs e) {
            sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        private void sort(SortMethod sort_method) {
            switch (current_sort_mode) {
                case SortMode.Ascending: {
                    sort_acending(sort_method);
                    break;
                }
                case SortMode.Decending: {
                    sort_decending(sort_method);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void decending_sort_mode(object sender, RoutedEventArgs e) {
            if (current_sort_mode == SortMode.Ascending) {
                var inital_image = images.paths[current_index];
                var file_paths_list = images.paths.Reverse().ToList();
                find_image_after_sort(file_paths_list, inital_image);
            }
            current_sort_mode = SortMode.Decending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        private void ascending_sort_mode(object sender, RoutedEventArgs e) {
            if (current_sort_mode == SortMode.Decending) {
                var inital_image = images.paths[current_index];
                var file_paths_list = images.paths.Reverse().ToList();
                find_image_after_sort(file_paths_list, inital_image);
            }
            current_sort_mode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        private void compare_onclick(object sender, RoutedEventArgs e) {
            var compare_file = show_open_file_dialog().FileName;
            if (string.IsNullOrEmpty(compare_file))
                return;

            if (in_compared_mode == false) {
                last_images = images;
                in_compared_mode = true;
                before_compare_mode_index = current_index;
            }
            images.paths = new[] {images.paths[current_index], compare_file};
            set_current_image(0);
        }

        private class FileId<T> {
            public FileId(string path, T item, int id) {
                this.path = path;
                this.item = item;
                this.id = id;
            }

            public string path { get; }
            public T item { get; }
            public int id { get; }
        }
    }
}