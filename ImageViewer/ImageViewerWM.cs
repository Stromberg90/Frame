using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Frame
{
    public enum SlideshowInterval
    {
        Second1,
        Seconds2,
        Seconds3,
        Seconds4,
        Seconds5,
        Seconds10,
        Seconds20,
        Seconds30
    }

    public enum SwitchDirection
    {
        Next,
        Previous
    }

    class ImageViewerWM
    {
        public static readonly string VERSION = "1.0.1";
        public List<TabData> Tabs { get; set; } = new List<TabData>();
        public int BeforeCompareModeIndex { get; set; }
        public int SlideshowInterval { get; set; } = 5;
        public int CurrentTabIndex { get; set; } = -1;
        public TabData CurrentTab
        {
            get
            {
                return Tabs[CurrentTabIndex];
            }
        }


        public bool CanExcectute()
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


        public static OpenFileDialog ShowOpenFileDialog()
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

        public void ImageEditorBrowse()
        {
            var file_dialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"
            };
            if (file_dialog.ShowDialog() == true)
            {
                Properties.Settings.Default.ImageEditor = file_dialog.FileName;
                Process.Start(Properties.Settings.Default.ImageEditor, CurrentTab.Path);
            }
            else
            {
                return;
            }
        }

        public void SortDecending(SortMethod method)
        {
            SortAcending(method);
            CurrentTab.images.Paths.Reverse();
            CurrentTab.Index = CurrentTab.images.Paths.ToList().IndexOf(CurrentTab.Path);
        }

        public void SortAcending(SortMethod method)
        {
            var id = 0;
            string initialImage;
            if (CurrentTab.images.Paths.Count < CurrentTab.Index)
            {
                initialImage = CurrentTab.InitialImagePath;
            }
            else
            {
                initialImage = CurrentTab.Path;
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

        public void FindImageAfterSort(List<string> sorted_paths, string initial_image)
        {
            CurrentTab.images.Paths = sorted_paths;
            CurrentTab.Index = sorted_paths.IndexOf(initial_image);
        }

        public static List<string> TypeSort<T>(IEnumerable<FileId<T>> id_list, Dictionary<T, int> dictionary)
        {
            var id_file_dictionary = id_list.ToDictionary(x => x.Id, x => x.Path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            var sorted_paths = keys.Select(l => dictionary[l]).ToList().Select(l => id_file_dictionary[l]).ToList();
            return sorted_paths;
        }

        public void Sort(SortMethod sort_method)
        {
            switch (CurrentTab.imageSettings.CurrentSortMode)
            {
                case SortMode.Ascending:
                    {
                        SortAcending(sort_method);
                        break;
                    }
                case SortMode.Descending:
                    {
                        SortDecending(sort_method);
                        break;
                    }
            }
        }

        public void SupportedImageFilesInDirectory(string path)
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
                    // Should find a way to check without upper case characters and that crap.
                    if (Properties.Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
                    {
                        CurrentTab.images.Paths.Add(file);
                        continue;
                    }
                    if (Properties.Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
                    {
                        CurrentTab.images.Paths.Add(file);
                    }
                }
            }

            if (CurrentTab.images.IsValid())
            {
                SortAcending(SortMethod.Name);
            }
        }
    }
}
