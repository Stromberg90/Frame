using System.IO;
using System.Windows;

namespace Frame
{
    public class FilesManager
    {
        ImageViewerWm ImageViewerVm { get; }
        public readonly SortingManager SortingManager;

        public FilesManager(SortingManager sortingManager, ImageViewerWm imageViewerVm)
        {
            SortingManager = sortingManager;
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
                    if (!string.IsNullOrEmpty(extension))
                    {
                        // Should find a way to check without upper case characters and that crap.
                        if (Properties.Settings.Default.SupportedExtensions.Contains(extension.Remove(0, 1)))
                        {
                            ImageViewerVm.CurrentTab.Paths.Add(file);
                            continue;
                        }
                        if (Properties.Settings.Default.SupportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
                        {
                            ImageViewerVm.CurrentTab.Paths.Add(file);
                        }
                    }
                }

                if (ImageViewerVm.CurrentTab.IsValid)
                {
                    SortingManager.SortAcending(SortMethod.Name);
                }
            });
        }
    }
}