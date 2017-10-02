using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Optional.Unsafe;

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

    class ImageViewerWM
    {
        public static readonly string VERSION = "1.0.2";
        public List<TabData> Tabs { get; set; } = new List<TabData>();
        public int BeforeCompareModeIndex { get; set; }
        public int SlideshowInterval { get; set; } = 5;
        public int CurrentTabIndex { get; set; } = -1;
        public TabData CurrentTab
        {
            get
            {
                return Tabs[CurrentTabIndex];
            }
        }


        public bool CanExcectute()
        {
            if (CurrentTabIndex < 0)
            {
                return false;
            }
            if (CurrentTab.Index == -1)
            {
                return false;
            }
            if (!Tabs.Any())
            {
                return false;
            }
            if(!CurrentTab.IsValid())
            {
                return false;
            }
            return true;
        }


        public static OpenFileDialog ShowOpenFileDialog()
        {
            var fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = FileFormats.filter_string
            };
            fileDialog.ShowDialog();

            return fileDialog;
        }

        public void ImageEditorBrowse()
        {
            var file_dialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                Filter = "Executable Files (*.exe, *.lnk)|*.exe;*.lnk"
            };
            if (file_dialog.ShowDialog() == true)
            {
                Properties.Settings.Default.ImageEditor = file_dialog.FileName;
                Process.Start(Properties.Settings.Default.ImageEditor, CurrentTab.Path);
            }
            else
            {
                return;
            }
        }
    }
}
