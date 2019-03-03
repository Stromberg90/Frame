using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Frame.Properties;

namespace Frame
{
    public class FilesManager
    {
        readonly SortingManager Manager;
        readonly TabControlManager TabControlManager;

        public FilesManager(SortingManager sortingManager, TabControlManager tabControlManager)
        {
            Manager = sortingManager;
            TabControlManager = tabControlManager;
        }

        public void SupportedFiles(string folderpath)
        {
            if (string.IsNullOrEmpty(folderpath))
            {
                return;
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                var tabItemControl = TabControlManager.CurrentTab;
                tabItemControl.Paths.Clear();

                Parallel.ForEach(Directory.EnumerateFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly), file =>
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (string.IsNullOrEmpty(extension)) return;
                    var supportedExtensions = Settings.Default.SupportedExtensions;
                    if (supportedExtensions.Contains(extension.Remove(0, 1))
                      || supportedExtensions.Contains(extension.ToLower().Remove(0, 1)))
                    {
                        tabItemControl.Paths.Add(file);
                    }
                });

                if (tabItemControl.Paths.Any())
                {
                    Manager.Sort();
                }
            });
        }

        public static string[] FilterSupportedFiles(string[] files)
        {
            var supportedFiles = new List<string>();
            if (files == null || files.Length <= 0)
            {
                return supportedFiles.ToArray();
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Parallel.ForEach(files, (file) =>
                {
                    var extension = Path.GetExtension(Path.GetFileName(file));
                    if (string.IsNullOrEmpty(extension)) return;
                    var supportedExtensions = Settings.Default.SupportedExtensions;
                    if (supportedExtensions.Contains(extension.Remove(0, 1))
                      || supportedExtensions.Contains(extension.ToLower().Remove(0, 1)) ||
                      supportedExtensions.Contains(extension.ToUpper().Remove(0, 1)))
                    {
                        supportedFiles.Add(file);
                    }
                });
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