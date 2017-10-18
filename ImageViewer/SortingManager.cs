using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Optional;

namespace Frame
{
    class SortingManager
    {
        ImageViewerWM ImageViewerVM { get; set; }

        public SortingManager(ImageViewerWM imageViewerVM)
        {
            ImageViewerVM = imageViewerVM;
        }

        public void SortDecending(SortMethod method)
        {
            SortAcending(method);
            ImageViewerVM.CurrentTab.Paths.Reverse();
            ImageViewerVM.CurrentTab.Index = ImageViewerVM.CurrentTab.Paths.IndexOf(ImageViewerVM.CurrentTab.Path);
        }

        public void SortAcending(SortMethod method)
        {
            var id = 0;
            string initialImage;
            if (ImageViewerVM.CurrentTab.Paths.Count < ImageViewerVM.CurrentTab.Index)
            {
                initialImage = ImageViewerVM.CurrentTab.InitialImagePath;
            }
            else
            {
                initialImage = ImageViewerVM.CurrentTab.Path;
            }
            List<string> sortedPaths;
            switch (method)
            {
                case SortMethod.Name:
                    {
                        sortedPaths = ImageViewerVM.CurrentTab.Paths.ToList();
                        sortedPaths.Sort();
                        break;
                    }

                case SortMethod.Date:
                    {
                        var keys = ImageViewerVM.CurrentTab.Paths.ToList().Select(s => new FileInfo(s).LastWriteTime).ToList();

                        var dateTimeLookup = keys.Zip(ImageViewerVM.CurrentTab.Paths.ToList(), (k, v) => new { k, v })
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
                        var keys = ImageViewerVM.CurrentTab.Paths.ToList().Select(s => new FileInfo(s).Length).ToList();
                        var size_lookup = keys.Zip(ImageViewerVM.CurrentTab.Paths.ToList(), (k, v) => new { k, v })
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
            ImageViewerVM.CurrentTab.Paths = sorted_paths;
            ImageViewerVM.CurrentTab.Index = sorted_paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(initial_image));
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
            switch (ImageViewerVM.CurrentTab.ImageSettings.CurrentSortMode)
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

        // Move this somewhere else, doesn't have anything to do with sorting
        public void SupportedFiles(string folderpath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(folderpath))
                {
                    return;
                }

                ImageViewerVM.CurrentTab.Paths.Clear();

                foreach (var file in Directory.EnumerateFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (!string.IsNullOrEmpty(extension))
                    {
                        // Should find a way to check without upper case characters and that crap.
                        if (Properties.Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
                        {
                            ImageViewerVM.CurrentTab.Paths.Add(file);
                            continue;
                        }
                        if (Properties.Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
                        {
                            ImageViewerVM.CurrentTab.Paths.Add(file);
                        }
                    }
                }

                if (ImageViewerVM.CurrentTab.IsValid())
                {
                    SortAcending(SortMethod.Name);
                }
            });
        }
    }
}
