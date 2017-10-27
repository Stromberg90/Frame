using Microsoft.Win32;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frame
{
    public enum SlideshowInterval
    {
        Second1,
        Seconds2,
        Seconds3,
        Seconds4,
        Seconds5,
        Seconds10,
        Seconds20,
        Seconds30
    }

    public enum SwitchDirection
    {
        Next,
        Previous
    }

    public class ImageViewerWm
    {
        public static readonly string VERSION = "1.0.3";
        public List<TabData> Tabs { get; } = new List<TabData>();
        public int BeforeCompareModeIndex { get; set; }
        public int SlideshowInterval { get; set; } = 5;
        public int CurrentTabIndex { get; set; } = -1;
        public TabData CurrentTab => Tabs[CurrentTabIndex];


        public bool CanExcectute()
        {
            if (CurrentTabIndex < 0)
            {
                return false;
            }
            if (Tabs.Count == 0)
            {
                return false;
            }
            return CurrentTab.Index != -1 && CurrentTab.IsValid;
        }


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

        public void ImageEditorBrowse()
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"
            };
            if (fileDialog.ShowDialog() == true)
            {
                Properties.Settings.Default.ImageEditor = fileDialog.FileName;
                Process.Start(Properties.Settings.Default.ImageEditor, CurrentTab.Path);
            }
        }
    }
}