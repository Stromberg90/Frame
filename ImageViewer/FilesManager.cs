using System.Collections.Generic;
using System.IO;
using System.Windows;
using Frame.Properties;

namespace Frame
{
  public class FilesManager
  {
    SortingManager    Manager           { get; }
    TabControlManager TabControlManager { get; }

    public FilesManager(SortingManager sortingManager, TabControlManager tabControlManager)
    {
      Manager           = sortingManager;
      TabControlManager = tabControlManager;
    }

    public void SupportedFiles(string folderpath)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (string.IsNullOrEmpty(folderpath))
        {
          return;
        }

        var tabItemControl = TabControlManager.CurrentTab;
        tabItemControl.Paths.Clear();

        foreach (var file in Directory.EnumerateFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly))
        {
          var extension = Path.GetExtension(Path.GetFileName(file));
          if (string.IsNullOrEmpty(extension)) continue;
          var supportedExtensions = Settings.Default.SupportedExtensions;
          if (supportedExtensions.Contains(extension.Remove(0, 1))
              || supportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
          {
            tabItemControl.Paths.Add(file);
          }
        }

        if (tabItemControl.IsValid)
        {
          Manager.Sort();
        }
      });
    }

    public static string[] FilterSupportedFiles(string[] files)
    {
      var supportedFiles = new List<string>();
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (files == null || files.Length <= 0) return;
        foreach (var file in files)
        {
          var extension = Path.GetExtension(Path.GetFileName(file));
          if (string.IsNullOrEmpty(extension)) continue;
          var supportedExtensions = Settings.Default.SupportedExtensions;
          if (supportedExtensions.Contains(extension.Remove(0, 1))
              || supportedExtensions.Contains(extension.ToLower().Remove(0, 1)) ||
              supportedExtensions.Contains(extension.ToUpper().Remove(0, 1)))
          {
            supportedFiles.Add(file);
          }
        }
      });
      return supportedFiles.ToArray();
    }

    public static bool ValidFile(string filepath)
    {
      if (string.IsNullOrEmpty(filepath)) return false;
      var extension = Path.GetExtension(Path.GetFileName(filepath));
      if (string.IsNullOrEmpty(extension)) return false;
      return Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)) ||
             Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1));
    }
  }
}