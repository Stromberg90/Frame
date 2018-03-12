using System.Reflection;
using Microsoft.Win32;

namespace Frame
{
    public class ImageViewerWm
    {
        public static readonly string VERSION = Assembly.GetEntryAssembly().GetName().Version.ToString();
        public int SlideshowInterval { get; set; } = 5;

        public static OpenFileDialog ShowOpenFileDialog()
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = true,
                AddExtension = true,
                Filter = FileFormats.FilterString
            };
            fileDialog.ShowDialog();

            return fileDialog;
        }
    }
}