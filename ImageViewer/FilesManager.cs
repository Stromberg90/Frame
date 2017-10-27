using System;
using System.IO;
using System.Windows;
using Frame.Properties;

namespace Frame
{
    public class FilesManager
    {
        ImageViewerWm ImageViewerVm { get; }
        public SortingManager Manager { get; }

        public FilesManager(SortingManager sortingManager, ImageViewerWm imageViewerVm)
        {
            Manager = sortingManager;
            ImageViewerVm = imageViewerVm;
        }

        public void SupportedFiles(string folderpath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (String.IsNullOrEmpty(folderpath))
                {
                    return;
                }

                ImageViewerVm.CurrentTab.Paths.Clear();

                foreach (var file in Directory.EnumerateFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (!String.IsNullOrEmpty(extension))
                    {
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
                }

                if (ImageViewerVm.CurrentTab.IsValid)
                {
                    Manager.SortAcending(SortMethod.Name);
                }
            });
        }

        public static bool ValidFile(string file)
        {
            var extension = Path.GetExtension(Path.GetFileName(file));
            if (!string.IsNullOrEmpty(extension))
            {
                // Should find a way to check without upper case characters and that crap.
                if (Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
                {
                    return true;
                }
                if (Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
                {
                    return true;
                }
            }
            return false;
        }
    }
}