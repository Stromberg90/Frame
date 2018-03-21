using System.Collections.Generic;
using System.IO;
using System.Windows;
using Frame.Properties;

namespace Frame
{
  public class FilesManager
  {
    ImageViewerWm     ImageViewerVm     { get; }
    SortingManager    Manager           { get; }
    TabControlManager TabControlManager { get; }

    public FilesManager(SortingManager sortingManager, ImageViewerWm imageViewerVm, TabControlManager tabControlManager)
    {
      Manager           = sortingManager;
      ImageViewerVm     = imageViewerVm;
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
          // Should find a way to check without upper case characters and that crap.
          if (Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
          {
            tabItemControl.Paths.Add(file);
            continue;
          }

          if (Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
          {
            tabItemControl.Paths.Add(file);
          }
        }

        if (tabItemControl.IsValid)
        {
          Manager.SortAcending();
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
          if (Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
          {
            supportedFiles.Add(file);
            continue;
          }

          if (Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
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