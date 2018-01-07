using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Frame
{
    public class SortingManager
    {
        ImageViewerWm ImageViewerVm { get; }

        public SortingManager(ImageViewerWm imageViewerVm)
        {
            ImageViewerVm = imageViewerVm;
        }

        void SortDecending()
        {
            SortAcending();
            ImageViewerVm.CurrentTab.Paths.Reverse();
            ImageViewerVm.CurrentTab.Index = ImageViewerVm.CurrentTab.Paths.IndexOf(ImageViewerVm.CurrentTab.Path);
        }

        public void SortAcending()
        {
            var id = 0;
            string initialImage;
            if (ImageViewerVm.CurrentTab.Paths.Count < ImageViewerVm.CurrentTab.Index)
            {
                initialImage = ImageViewerVm.CurrentTab.InitialImagePath;
            }
            else
            {
                initialImage = ImageViewerVm.CurrentTab.Path;
            }
            List<string> sortedPaths;
            switch (ImageViewerVm.CurrentTab.ImageSettings.SortMethod)
            {
                case SortMethod.Name:
                    {
                        sortedPaths = ImageViewerVm.CurrentTab.Paths.ToList();
                        sortedPaths.Sort();
                        break;
                    }

                case SortMethod.Date:
                    {
                        var keys = ImageViewerVm.CurrentTab.Paths.ToList().Select(s => new FileInfo(s).LastWriteTime).ToList();

                        var dateTimeLookup = keys.Zip(ImageViewerVm.CurrentTab.Paths.ToList(), (k, v) => new { k, v })
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
                        var keys = ImageViewerVm.CurrentTab.Paths.ToList().Select(s => new FileInfo(s).Length).ToList();
                        var sizeLookup = keys.Zip(ImageViewerVm.CurrentTab.Paths.ToList(), (k, v) => new { k, v })
                                              .ToLookup(x => x.k, x => x.v);

                        var idList = sizeLookup.SelectMany(pair => pair,
                                                     (pair, value) => new FileId<long>(value, pair.Key, id += 1)).ToList();

                        var dateIdDictionary = idList.ToDictionary(x => x.Item + x.Id, x => x.Id);
                        sortedPaths = TypeSort(idList, dateIdDictionary);
                        break;
                    }
                default:
                    {
                        return;
                    }
            }
            FindImageAfterSort(sortedPaths, initialImage);
        }

        public void FindImageAfterSort(List<string> sortedPaths, string initialImage)
        {
            ImageViewerVm.CurrentTab.Paths = sortedPaths;
            ImageViewerVm.CurrentTab.Index = sortedPaths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(initialImage));
        }

        static List<string> TypeSort<T>(IEnumerable<FileId<T>> idList, Dictionary<T, int> dictionary)
        {
            var idFileDictionary = idList.ToDictionary(x => x.Id, x => x.Path);

            var keys = dictionary.Keys.ToList();
            keys.Sort();

            return keys.Select(l => dictionary[l]).ToList().Select(l => idFileDictionary[l]).ToList();
        }

        public void Sort()
        {
            switch (ImageViewerVm.CurrentTab.ImageSettings.SortMode)
            {
                case SortMode.Ascending:
                    {
                        SortAcending();
                        break;
                    }
                case SortMode.Descending:
                    {
                        SortDecending();
                        break;
                    }
            }
        }
    }
}
