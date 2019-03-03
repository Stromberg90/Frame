using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Frame
{
  public class SortingManager
  {
    TabControlManager TabControlManager { get; }

    public SortingManager(TabControlManager tabControlManager)
    {
      TabControlManager = tabControlManager;
    }

    void SortDecending()
    {
      SortAcending();
      var currentTab = TabControlManager.CurrentTab;
      currentTab.Paths.Reverse();
      currentTab.Index = (uint)currentTab.Paths.IndexOf(currentTab.Path);
    }

    void SortAcending()
    {
      var id           = 0;
      var currentTab   = TabControlManager.CurrentTab;
      var paths        = currentTab.Paths;
      var initialImage = paths.Count > currentTab.Index ? currentTab.InitialImagePath : currentTab.Path;

      List<string> sortedPaths;
      var          pathsList = paths.ToList();
      switch (currentTab.ImageSettings.SortMethod)
      {
        case SortMethod.Name:
        {
          sortedPaths = pathsList;
          sortedPaths.Sort();
          break;
        }

        case SortMethod.Date:
        {
          var keys = pathsList.Select(s => new FileInfo(s).LastWriteTime).ToList();

          var dateTimeLookup = keys.Zip(pathsList, (k, v) => new {k, v})
                                   .ToLookup(x => x.k, x => x.v);

          var idList = dateTimeLookup.SelectMany(pair => pair,
                                                 (pair, value) => new FileId<DateTime>(value, pair.Key, id++))
                                     .ToList();

          var dateIdDictionary = idList.ToDictionary(x => x.Item.AddMilliseconds(x.Id), x => x.Id);
          sortedPaths = TypeSort(idList, dateIdDictionary);
          break;
        }
        case SortMethod.Size:
        {
          var keys = pathsList.Select(s => new FileInfo(s).Length).ToList();
          var sizeLookup = keys.Zip(pathsList, (k, v) => new {k, v})
                               .ToLookup(x => x.k, x => x.v);

          var idList = sizeLookup.SelectMany(pair => pair,
                                             (pair, value) => new FileId<long>(value, pair.Key, id++)).ToList();

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FindImageAfterSort(List<string> sortedPaths, string initialImage)
    {
      var currentTab = TabControlManager.CurrentTab;
      currentTab.Paths = sortedPaths;
      currentTab.Index = (uint)sortedPaths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(initialImage));
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
      switch (TabControlManager.CurrentTab.ImageSettings.SortMode)
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