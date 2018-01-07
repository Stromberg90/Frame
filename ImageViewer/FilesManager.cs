using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using Frame.Properties;

namespace Frame
{
    public class FilesManager
    {
        ImageViewerWm ImageViewerVm { get; }
        SortingManager Manager { get; }

        public FilesManager(SortingManager sortingManager, ImageViewerWm imageViewerVm)
        {
            Manager = sortingManager;
            ImageViewerVm = imageViewerVm;
        }

        public void SupportedFiles(string folderpath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrEmpty(folderpath))
                {
                    return;
                }

                ImageViewerVm.CurrentTab.Paths.Clear();

                foreach (var file in Directory.EnumerateFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (string.IsNullOrEmpty(extension)) continue;
                    // Should find a way to check without upper case characters and that crap.
                    if (Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
                    {
                        ImageViewerVm.CurrentTab.Paths.Add(file);
                        continue;
                    }
                    if (Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
                    {
                        ImageViewerVm.CurrentTab.Paths.Add(file);
                    }
                }

                if (ImageViewerVm.CurrentTab.IsValid)
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

        public static bool ValidFile(string file)
        {
            var extension = Path.GetExtension(Path.GetFileName(file));
            if (string.IsNullOrEmpty(extension)) return false;
            return Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)) || 
                   Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1));
        }
    }
}