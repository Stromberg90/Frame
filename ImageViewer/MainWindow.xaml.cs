// TODOS
// GIF Support
// Loading images without lag
// Split into more files
// Progress bar when loading large images
// Layout tabs side by side?
// Options window, maybe I can add the hotkeys in there as well
// Font size settings
// Pick color under mouse, copy value to clipboard?
// Add zoom scale to footer
// Can I make a thumbnail using the render size first, then I can load in the real image in the background and replace.
// Data bindings
// Tiling image toggle
// Mip toggle
// Make a option where dragging the image creates a new tab instead of replacing it.
// Show hotkey next to menuitem
// Single instance window, where it opens files in the current instance of the program.
// Can I read sort setting from file explorer?

using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Optional;
using Optional.Unsafe;

namespace Frame
{
    public partial class MainWindow
    {
        public Channels DisplayChannel
        {
            get => ImageViewerWM.CurrentTab.ImageSettings.displayChannel;

            set
            {
                ImageViewerWM.CurrentTab.ImageSettings.displayChannel = value;
                RefreshImage();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            /*
            if (procs.Length > 1)
            {
                var sinfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true
                };
                Process.GetCurrentProcess().StartInfo = sinfo;
                Application.Current.Shutdown();
                var inp = procs[procs.Length - 1].StandardInput;
                // procs[procs.Length - 1].Start();
                inp.WriteLine(true);
            }
            else
            {
                var sinfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true
                };
                Process.GetCurrentProcess().StartInfo = sinfo;
            }*/

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                TabData tab = new TabData(null, 0) { CloseTabAction = CloseTabIndex };
                ImageViewerWM.Tabs.Add(tab);

                ImageTabControl.Items.Add(tab.tabItem);

                ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;

                var filePath = Environment.GetCommandLineArgs()[1];
                ImageViewerWM.CurrentTab.InitialImagePath = filePath;
                var folderPath = Path.GetDirectoryName(filePath);
                SupportedImageFilesInDirectoryDispatch(folderPath);
                if (ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().IndexOf(filePath) == -1)
                {
                    ImageViewerWM.CurrentTab.Index = 0;
                }
                else
                {
                    ImageViewerWM.CurrentTab.Index = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().IndexOf(filePath);
                }
            }

            RefreshUI();
            SetupSlideshow();
            UpdateFooter();
            ImageArea.CenterToImage();
        }
        static System.Windows.Threading.DispatcherTimer slideshowTimer;

        About aboutDialog = new About();

        Process[] procs = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (ImageViewerWM.Tabs.Any() && ImageViewerWM.CurrentTab.Images.IsValid())
            {
                if ((e != null) && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (e.Delta > 0)
                    {
                        SwitchImage(SwitchDirection.Next);
                    }
                    else
                    {
                        SwitchImage(SwitchDirection.Previous);
                    }
                }
            }
        }
        ImageViewerWM ImageViewerWM { get; set; } = new ImageViewerWM();
        static string BackwardToForwardSlash(string v) => v.Replace('\\', '/');

        static MagickImage ErrorImage(string filepath)
        {
            MagickImage image = new MagickImage(MagickColors.White, 512, 512);
            new Drawables()
            .FontPointSize(18)
            .Font("Arial")
            .FillColor(MagickColors.Red)
            .TextAlignment(ImageMagick.TextAlignment.Center)
            .Text(256, 256, $"Could not load\n{Path.GetFileName(filepath)}")
            .Draw(image);

            return image;
        }

        void CloseTabIndex(TabData data)
        {
            var currently_selected_item = ImageTabControl.SelectedItem;
            var currently_selected_index = ImageTabControl.SelectedIndex;
            int newIndex = ImageTabControl.Items.IndexOf(data.tabItem);
            if (newIndex < 0)
            {
                CloseTab();
            }
            else
            {
                ImageViewerWM.CurrentTabIndex = newIndex;
                ImageTabControl.SelectedIndex = newIndex;
                CloseTab();
                if (currently_selected_index != newIndex)
                {
                    ImageTabControl.SelectedItem = currently_selected_item;
                }
            }
        }
        void ValidatedKeyHandling(System.Windows.Forms.KeyEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.A:
                    {
                        SetDisplayChannel(Channels.Alpha);
                        break;
                    }
                case System.Windows.Forms.Keys.R:
                    {
                        SetDisplayChannel(Channels.Red);
                        break;
                    }
                case System.Windows.Forms.Keys.G:
                    {
                        SetDisplayChannel(Channels.Green);
                        break;
                    }
                case System.Windows.Forms.Keys.B:
                    {
                        SetDisplayChannel(Channels.Blue);
                        break;
                    }
                case System.Windows.Forms.Keys.F:
                    {
                        ResetView();
                        break;
                    }
                case System.Windows.Forms.Keys.D:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            DuplicateTab();
                        }
                        break;
                    }
                case System.Windows.Forms.Keys.W:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            CloseTab();
                        }
                        break;
                    }
                case System.Windows.Forms.Keys.S:
                    {
                        ToggleSlideshow();
                        break;
                    }
                case System.Windows.Forms.Keys.Right:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            if (ImageTabControl.SelectedIndex != ImageTabControl.Items.Count - 1)
                            {
                                ImageTabControl.SelectedIndex += 1;
                            }
                        }
                        else
                        {
                            SwitchImage(SwitchDirection.Next);
                        }
                        break;
                    }
                case System.Windows.Forms.Keys.Left:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            if (ImageTabControl.SelectedIndex > 0)
                            {
                                ImageTabControl.SelectedIndex -= 1;
                            }
                        }
                        else
                        {
                            SwitchImage(SwitchDirection.Previous);
                        }
                        break;
                    }
                case System.Windows.Forms.Keys.Space:
                    {
                        SwitchImage(SwitchDirection.Next);
                        break;
                    }
                case System.Windows.Forms.Keys.Delete:
                    {
                        DeleteImage();
                        break;
                    }
            }
        }

        void ToggleSlideshow()
        {
            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                ImageViewerWM.CurrentTab.Mode = ApplicationMode.Normal;
            }
            else
            {
                ImageViewerWM.CurrentTab.Mode = ApplicationMode.Slideshow;
            }
            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                slideshowTimer.Start();
            }
            else
            {
                slideshowTimer.Stop();
            }
            ImageViewerWM.CurrentTab.UpdateTitle();
            UpdateFooter();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
        }

        void DeleteImage()
        {
            var res = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
                                          $"{Properties.Resources.Delete}{FileSystem.GetName(ImageViewerWM.CurrentTab.Path)}", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                FileSystem.DeleteFile(ImageViewerWM.CurrentTab.Path, UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);
                if (ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().Count > 0)
                {
                    SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.CurrentTab.Path));

                    SwitchImage(SwitchDirection.Next);
                }
                else
                {
                    if (FileBrowser())
                    {
                        DisplayImage();
                    }
                }
            }
        }

        void RawKeyHandling(System.Windows.Forms.KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Escape:
                    {
                        Close();
                        break;
                    }
                case System.Windows.Forms.Keys.N:
                    {
                        if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        {
                            AddNewTab();
                        }
                        break;
                    }
            }
        }
        void AddNewTab()
        {
            var file_dialog = ImageViewerWM.ShowOpenFileDialog();
            if (string.IsNullOrEmpty(file_dialog.SafeFileName))
                return;

            var folderPath = Path.GetDirectoryName(file_dialog.FileName);

            TabData item = new TabData(folderPath)
            {
                CloseTabAction = CloseTabIndex,
            };
            ImageViewerWM.Tabs.Add(item);

            ImageTabControl.Items.Add(item.tabItem);

            if (ImageViewerWM.CurrentTabIndex == -1)
            {
                ImageViewerWM.CurrentTabIndex = 0;
                ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex;
                SupportedImageFilesInDirectoryDispatch(folderPath);
            }
            else
            {
                ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;
                SupportedImageFilesInDirectoryDispatch(folderPath);
            }


            var filenameIndex = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().IndexOf(file_dialog.FileName);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            ImageViewerWM.CurrentTab.InitialImagePath = file_dialog.FileName;

            RefreshTab();
        }

        void AddNewTab(string filepath)
        {
            if (string.IsNullOrEmpty(Path.GetFileName(filepath)))
                return;

            var folderPath = Path.GetDirectoryName(filepath);
            TabData item = new TabData(folderPath) { CloseTabAction = CloseTabIndex };
            ImageViewerWM.Tabs.Add(item);

            ImageTabControl.Items.Add(item.tabItem);

            if (ImageViewerWM.CurrentTabIndex == -1)
            {
                ImageViewerWM.CurrentTabIndex = 0;
                SupportedImageFilesInDirectoryDispatch(folderPath);
                ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex;
            }
            else
            {
                SupportedImageFilesInDirectoryDispatch(folderPath);
                ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;
            }

            SupportedImageFilesInDirectoryDispatch(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().IndexOf(filepath);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            ImageViewerWM.CurrentTab.InitialImagePath = filepath;

            RefreshTab();
            SetupDirectoryWatcher();
        }

        void Ascending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode == SortMode.Descending)
            {
                var inital_image = ImageViewerWM.CurrentTab.Path;
                var file_paths_list = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure();
                file_paths_list.Reverse();
                ImageViewerWM.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Ascending;
            SortDecending.IsChecked = false;
            SortAscending.IsChecked = true;
        }

        void CloseTab()
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.Tabs.RemoveAt(ImageTabControl.SelectedIndex);
            if (ImageTabControl.SelectedIndex == 0)
            {
                ImageArea.Image = null;
                GC.Collect();
            }
            ImageTabControl.Items.RemoveAt(ImageTabControl.SelectedIndex);
            UpdateFooter();
        }

        void Compare_onclick(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            var compare_file = ImageViewerWM.ShowOpenFileDialog().FileName;
            if (string.IsNullOrEmpty(compare_file))
                return;

            ImageViewerWM.CurrentTab.Images.Paths = Option.Some(new List<string> { ImageViewerWM.CurrentTab.Path, compare_file });
            SetCurrentImage(0);
        }

        void CopyPathToClipboard(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(ImageViewerWM.CurrentTab.Path)}\"");
        }

        void CopyFilenameToClipboard(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            Clipboard.SetText($"\"{BackwardToForwardSlash(Path.GetFileName(ImageViewerWM.CurrentTab.Path))}\"");
        }

        void Decending_sort_mode(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode == SortMode.Ascending)
            {
                var inital_image = ImageViewerWM.CurrentTab.Path;
                var file_paths_list = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure();
                file_paths_list.Reverse();
                ImageViewerWM.FindImageAfterSort(file_paths_list, inital_image);
            }
            ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode = SortMode.Descending;
            SortDecending.IsChecked = true;
            SortAscending.IsChecked = false;
        }

        void Display_all_channels(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.RGB);
        }

        void Display_alpha_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Alpha);
        }

        void Display_blue_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Blue);
        }

        void Display_green_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Green);
        }

        void Display_red_channel(object sender, RoutedEventArgs e)
        {
            SetDisplayChannel(Channels.Red);
        }

        void DisplayImage()
        {
            if (ImageViewerWM.CurrentTabIndex < 0 || ImageViewerWM.CurrentTab.Index == -1)
            {
                return;
            }
            if (ImageArea != null)
            {

                if (ImageViewerWM.CurrentTab.Images.IsValid())
                {
                    var image = LoadImage(ImageViewerWM.CurrentTab.Path);

                    ImageArea.Image = image;

                    ImageViewerWM.CurrentTab.UpdateTitle();
                    UpdateFooter();
                }
            }
        }

        void UpdateFooter()
        {
            if (ImageViewerWM.CurrentTabIndex == -1)
            {
                FooterModeText.Text = "Mode: ";
                FooterSizeText.Text = "Size: ";
                FooterChannelsText.Text = "Channels: ";
                FooterFilesizeText.Text = "Filesize: ";
            }
            else
            {
                if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
                {
                    FooterModeText.Text = $"Mode: {ImageViewerWM.CurrentTab.Mode} " + ImageViewerWM.CurrentTab.CurrentSlideshowTime;
                }
                else
                {
                    FooterModeText.Text = $"Mode: {ImageViewerWM.CurrentTab.Mode}";
                }
                {
                    FooterSizeText.Text = $"Size: {ImageViewerWM.CurrentTab.Width}x{ImageViewerWM.CurrentTab.Height}";

                    string channel = string.Empty;
                    switch (DisplayChannel)
                    {
                        case (Channels.RGB):
                            {
                                channel = DisplayChannel.ToString();
                                break;
                            }
                        case (Channels.Red):
                            {
                                channel = "Red";
                                break;
                            }
                        case (Channels.Green):
                            {
                                channel = DisplayChannel.ToString();
                                break;
                            }
                        case (Channels.Blue):
                            {
                                channel = DisplayChannel.ToString();
                                break;
                            }
                        case (Channels.Opacity):
                            {
                                channel = "Alpha";
                                break;
                            }
                    }
                    FooterChannelsText.Text = $"Channels: {channel}";
                }
                {
                    if (ImageViewerWM.CurrentTab.Size < 1024)
                    {
                        FooterFilesizeText.Text = $"Filesize: {ImageViewerWM.CurrentTab.Size}Bytes";
                    }
                    else if (ImageViewerWM.CurrentTab.Size < 1048576)
                    {
                        var filesize = (double)(ImageViewerWM.CurrentTab.Size / 1024f);
                        FooterFilesizeText.Text = $"Filesize: {filesize:N2}KB";
                    }
                    else
                    {
                        var filesize = (double)(ImageViewerWM.CurrentTab.Size / 1024f) / 1024f;
                        FooterFilesizeText.Text = $"Filesize: {filesize:N2}MB";
                    }
                }
            }
        }

        void DuplicateTab()
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }


            var folderPath = Path.GetDirectoryName(ImageViewerWM.CurrentTab.Path);
            var tab = new TabData(folderPath, ImageViewerWM.CurrentTab.Index)
            {
                InitialImagePath = ImageViewerWM.CurrentTab.InitialImagePath,
                Images = ImageViewerWM.CurrentTab.Images,
                CloseTabAction = CloseTabIndex
            };

            tab.ImageSettings = new ImageSettings { displayChannel = ImageViewerWM.CurrentTab.ImageSettings.displayChannel, CurrentSortMode = ImageViewerWM.CurrentTab.ImageSettings.CurrentSortMode };

            var orginalTabPan = new System.Drawing.Point(ImageArea.Location.X, ImageArea.Location.Y);
            var orginalTabScale = ImageArea.Zoom;

            ImageViewerWM.Tabs.Insert(ImageViewerWM.CurrentTabIndex + 1, tab);

            ImageTabControl.Items.Insert(ImageViewerWM.CurrentTabIndex + 1, tab.tabItem);

            ImageTabControl.SelectedIndex = ImageViewerWM.CurrentTabIndex + 1;


            ImageViewerWM.CurrentTab.Pan = orginalTabPan;
            ImageViewerWM.CurrentTab.Scale = orginalTabScale;
            ImageArea.Location = orginalTabPan;
            ImageArea.Zoom = orginalTabScale;
        }

        bool FileBrowser()
        {
            var fileDialog = ImageViewerWM.ShowOpenFileDialog();
            if (string.IsNullOrEmpty(fileDialog.SafeFileName))
                return false;
            string filename = Path.GetFullPath(fileDialog.FileName);
            ReplaceImageInTab(filename);

            return true;
        }

        void ImageAreaOnLoaded(object sender, RoutedEventArgs e)
        {
            ImageArea.Image = sender as System.Drawing.Image;
            if (ImageArea != null)
            {
                DisplayImage();
            }
        }

        void ImageTabControl_Drop(object sender, DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            AddNewTab(Path.GetFullPath(filenames[0]));
        }

        void ImageTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ImageViewerWM.CurrentTabIndex = ImageTabControl.SelectedIndex;
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            if (e.RemovedItems.Count > 0)
            {
                foreach (var tab in ImageViewerWM.Tabs)
                {
                    if (tab.tabItem == (TabItem)e.RemovedItems[0])
                    {
                        ImageViewerWM.Tabs[ImageViewerWM.Tabs.IndexOf(tab)].Pan = new System.Drawing.Point(ImageArea.Location.X, ImageArea.Location.Y);
                        ImageViewerWM.Tabs[ImageViewerWM.Tabs.IndexOf(tab)].Scale = ImageArea.Zoom;
                    }
                }
            }

            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                slideshowTimer.Start();
            }

            var folder_path = Path.GetDirectoryName(ImageViewerWM.Tabs[ImageTabControl.SelectedIndex].InitialImagePath);
            SupportedImageFilesInDirectoryDispatch(folder_path);

            RefreshTab();
            ImageArea.CenterToImage();

            ImageArea.Zoom = ImageViewerWM.CurrentTab.Scale;
            ImageArea.Location = ImageViewerWM.CurrentTab.Pan;
        }

        System.Drawing.Image LoadImage(string filepath)
        {
            MagickImage image;
            MagickImageCollection imageCollection;

            try
            {
                if (Path.GetExtension(filepath) == ".gif")
                {
                    imageCollection = new MagickImageCollection(filepath);
                    image = (MagickImage)imageCollection[0];
                }
                else
                {
                    image = new MagickImage(filepath);
                }
            }
            catch (MagickCoderErrorException)
            {
                image = ErrorImage(filepath);
            }
            catch (MagickMissingDelegateErrorException)
            {
                image = ErrorImage(filepath);
            }
            finally
            {
                GC.Collect();
            }

            ImageViewerWM.CurrentTab.Size = image.FileSize;
            ImageViewerWM.CurrentTab.Width = image.Width;
            ImageViewerWM.CurrentTab.Height = image.Height;

            switch (DisplayChannel)
            {
                case Channels.Red:
                    {
                        return image.Separate(Channels.Red).ElementAt(0)?.ToBitmap();
                    }
                case Channels.Green:
                    {
                        return image.Separate(Channels.Green).ElementAt(0)?.ToBitmap();
                    }
                case Channels.Blue:
                    {
                        return image.Separate(Channels.Blue).ElementAt(0)?.ToBitmap();
                    }
                case Channels.Alpha:
                    {
                        return image.Separate(Channels.Alpha).ElementAt(0)?.ToBitmap();
                    }
                default:
                    {
                        image.Alpha(AlphaOption.Opaque);
                        return image.ToBitmap();
                    }
            }
        }

        void OpenInImageEditor(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ImageEditor))
            {
                if (File.Exists(Properties.Settings.Default.ImageEditor))
                {
                    Process.Start(Properties.Settings.Default.ImageEditor, ImageViewerWM.CurrentTab.Path);
                    return;
                }
                if (MessageBox.Show("Editor not found\nDo you want to browse for editor?", Properties.Resources.FileMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageViewerWM.ImageEditorBrowse();
                }
            }
            else
            {
                if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?", Properties.Resources.ImageEditorMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageViewerWM.ImageEditorBrowse();
                }
            }
            Properties.Settings.Default.Save();
        }

        void RefreshImage()
        {
            if (ImageViewerWM.CurrentTab.Images.IsValid())
            {

                var image = LoadImage(ImageViewerWM.CurrentTab.Path);

                ImageArea.Image = image;


                ImageViewerWM.CurrentTab.UpdateTitle();
                UpdateFooter();
            }
        }

        void RefreshTab()
        {
            DisplayImage();
            if (ImageViewerWM.CurrentTab.Images.IsValid())
            {
                ImageViewerWM.CurrentTab.UpdateTitle();
            }
            UpdateFooter();
        }

        void RefreshUI()
        {
            ImageArea.GridColor = System.Drawing.Color.FromArgb(255, Properties.Settings.Default.BackgroundColor.R, Properties.Settings.Default.BackgroundColor.G, Properties.Settings.Default.BackgroundColor.B);
        }

        void ReplaceImageInTab(string filename)
        {
            var folderPath = Path.GetDirectoryName(filename);
            if (ImageViewerWM.CurrentTabIndex < 0)
            {
                AddNewTab(filename);
            }
            ImageViewerWM.CurrentTab.InitialImagePath = filename;
            SupportedImageFilesInDirectoryDispatch(folderPath);

            var filenameIndex = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().IndexOf(filename);
            if (filenameIndex == -1)
            {
                ImageViewerWM.CurrentTab.Index = 0;
            }
            else
            {
                ImageViewerWM.CurrentTab.Index = filenameIndex;
            }

            SetupDirectoryWatcher();

            ResetView();
        }

        void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
        }

        void ResetView()
        {
            ImageArea.ZoomToFit();
        }

        void SetCurrentImage(int newIndex)
        {
            ImageViewerWM.CurrentTab.Index = newIndex;
            DisplayImage();
        }

        void SetDisplayChannel(Channels channel)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            switch (channel)
            {
                case Channels.RGB:
                    {
                        AllChannels.IsChecked = true;
                        RedChannel.IsChecked = false;
                        GreenChannel.IsChecked = false;
                        BlueChannel.IsChecked = false;
                        AlphaChannel.IsChecked = false;
                        DisplayChannel = Channels.RGB;
                        break;
                    }
                case Channels.Red:
                    {
                        if (DisplayChannel == Channels.Red)
                        {
                            AllChannels.IsChecked = true;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = false;
                        }
                        else
                        {
                            AllChannels.IsChecked = false;
                            RedChannel.IsChecked = true;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = false;
                        }

                        DisplayChannel = DisplayChannel == Channels.Red ? Channels.RGB : Channels.Red;

                        break;
                    }
                case Channels.Green:
                    {

                        if (DisplayChannel == Channels.Green)
                        {
                            AllChannels.IsChecked = true;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = false;
                        }
                        else
                        {
                            AllChannels.IsChecked = false;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = true;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = false;
                        }
                        DisplayChannel = DisplayChannel == Channels.Green ? Channels.RGB : Channels.Green;

                        break;
                    }
                case Channels.Blue:
                    {

                        if (DisplayChannel == Channels.Blue)
                        {
                            AllChannels.IsChecked = true;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = false;
                        }
                        else
                        {
                            AllChannels.IsChecked = false;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = true;
                            AlphaChannel.IsChecked = false;
                        }
                        DisplayChannel = DisplayChannel == Channels.Blue ? Channels.RGB : Channels.Blue;

                        break;
                    }
                case Channels.Alpha:
                    {

                        if (DisplayChannel == Channels.Alpha)
                        {
                            AllChannels.IsChecked = true;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = false;
                        }
                        else
                        {
                            AllChannels.IsChecked = false;
                            RedChannel.IsChecked = false;
                            GreenChannel.IsChecked = false;
                            BlueChannel.IsChecked = false;
                            AlphaChannel.IsChecked = true;
                        }
                        DisplayChannel = DisplayChannel == Channels.Alpha ? Channels.RGB : Channels.Alpha;

                        break;
                    }
            }
        }

        void SetupDirectoryWatcher()
        {
            var index = ImageTabControl != null ? ImageTabControl.SelectedIndex : 0;
            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath),
                NotifyFilter =
                    NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
                    NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));
            watcher.Created += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));
            watcher.Deleted += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));
            watcher.Renamed += (sender, args) => SupportedImageFilesInDirectoryDispatch(Path.GetDirectoryName(ImageViewerWM.Tabs[index].InitialImagePath));

            watcher.EnableRaisingEvents = true;
        }

        void SetupSlideshow()
        {
            slideshowTimer = new System.Windows.Threading.DispatcherTimer();
            slideshowTimer.Tick += Slideshow;
            slideshowTimer.Interval = new TimeSpan(0, 0, 1);
        }
        void Slideshow(object source, EventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                ImageViewerWM.CurrentTab.Mode = ApplicationMode.Normal;
                return;
            }

            if (ImageViewerWM.CurrentTab.CurrentSlideshowTime < ImageViewerWM.SlideshowInterval)
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime += 1;
                UpdateFooter();
                ImageViewerWM.CurrentTab.UpdateTitle();
            }
            else
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
                slideshowTimer.Stop();
                SwitchImage(SwitchDirection.Next);
                slideshowTimer.Start();
            }

            if (ImageViewerWM.CurrentTab.Mode != ApplicationMode.Slideshow)
            {
                slideshowTimer.Stop();
                ImageViewerWM.CurrentTab.UpdateTitle();
                UpdateFooter();
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
            }
        }

        void Slideshow10SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds10);

        void Slideshow1SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Second1);

        void Slideshow20SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds20);

        void Slideshow2SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds2);

        void Slideshow30SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds30);

        void Slideshow3SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds3);

        void Slideshow4SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds4);

        void Slideshow5SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUI(SlideshowInterval.Seconds5);

        void SlideshowIntervalUI(SlideshowInterval newInterval)
        {
            Slideshow1SecUI.IsChecked = false;
            Slideshow2SecUI.IsChecked = false;
            Slideshow3SecUI.IsChecked = false;
            Slideshow4SecUI.IsChecked = false;
            Slideshow5SecUI.IsChecked = false;
            Slideshow10SecUI.IsChecked = false;
            Slideshow20SecUI.IsChecked = false;
            Slideshow30SecUI.IsChecked = false;

            switch (newInterval)
            {
                case SlideshowInterval.Second1:
                    {
                        Slideshow1SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 1;
                        break;
                    }
                case SlideshowInterval.Seconds2:
                    {
                        Slideshow2SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 2;
                        break;
                    }
                case SlideshowInterval.Seconds3:
                    {
                        Slideshow3SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 3;
                        break;
                    }
                case SlideshowInterval.Seconds4:
                    {
                        Slideshow4SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 4;
                        break;
                    }
                case SlideshowInterval.Seconds5:
                    {
                        Slideshow5SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 5;
                        break;
                    }
                case SlideshowInterval.Seconds10:
                    {
                        Slideshow10SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 10;
                        break;
                    }
                case SlideshowInterval.Seconds20:
                    {
                        Slideshow20SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 20;
                        break;
                    }
                case SlideshowInterval.Seconds30:
                    {
                        Slideshow30SecUI.IsChecked = true;
                        ImageViewerWM.SlideshowInterval = 30;
                        break;
                    }
            }
        }

        void Sort_by_date_modified(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.Sort(SortMethod.Date);
            SortDate.IsChecked = true;
            SortName.IsChecked = false;
            SortSize.IsChecked = false;
        }

        void Sort_by_name(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.Sort(SortMethod.Name);
            SortName.IsChecked = true;
            SortSize.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void Sort_by_size(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.Sort(SortMethod.Size);
            SortSize.IsChecked = true;
            SortName.IsChecked = false;
            SortDate.IsChecked = false;
        }

        void StartSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.CurrentTab.Mode = ApplicationMode.Slideshow;
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
            slideshowTimer.Start();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void StopSlideshowUI_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            ImageViewerWM.CurrentTab.Mode = ApplicationMode.Normal;
            ImageViewerWM.CurrentTab.UpdateTitle();
            ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
            slideshowTimer.Stop();
            StartSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
            StopSlideshowUI.IsEnabled = !StartSlideshowUI.IsEnabled;
        }

        void SupportedImageFilesInDirectoryDispatch(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ImageViewerWM.SupportedImageFilesInDirectory(path);
            });
        }

        void SwitchImage(SwitchDirection switchDirection)
        {
            if (ImageViewerWM.CurrentTab.Mode == ApplicationMode.Slideshow)
            {
                ImageViewerWM.CurrentTab.CurrentSlideshowTime = 0;
            }

            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    if (ImageViewerWM.CurrentTab.Index < ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure()?.Count - 1)
                    {
                        SetCurrentImage(ImageViewerWM.CurrentTab.Index += 1);
                    }
                    else
                    {
                        SetCurrentImage(0);
                    }
                    break;

                case SwitchDirection.Previous:
                    if (ImageViewerWM.CurrentTab.Images.Paths.HasValue)
                    {
                        if (ImageViewerWM.CurrentTab.Index > 0)
                        {
                            SetCurrentImage(ImageViewerWM.CurrentTab.Index -= 1);
                        }
                        else
                        {
                            SetCurrentImage(ImageViewerWM.CurrentTab.Index = ImageViewerWM.CurrentTab.Images.Paths.ValueOrFailure().Count - 1);
                        }
                    }
                    break;
            }
            ResetView();
        }
        void UIAddNewTab_Click(object sender, RoutedEventArgs e)
        {
            AddNewTab();
        }

        void UINext_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Next);
        }

        void UIPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            SwitchImage(SwitchDirection.Previous);
        }

        void ViewInExplorer(object sender, RoutedEventArgs e)
        {
            if (!ImageViewerWM.CanExcectute())
            {
                return;
            }

            Process.Start("explorer.exe", "/select, " + ImageViewerWM.CurrentTab.Path);
        }

        void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
            AlwaysOnTopUI.IsChecked = Topmost;
        }

        void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reset();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = Properties.Settings.Default.WindowLocation.X;
            Top = Properties.Settings.Default.WindowLocation.Y;

            Width = Properties.Settings.Default.WindowSize.Width;
            Height = Properties.Settings.Default.WindowSize.Height;

            WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var location = new System.Drawing.Point((int)Left, (int)Top);
            Properties.Settings.Default.WindowLocation = location;
            Properties.Settings.Default.WindowState = (int)WindowState;
            if (WindowState == WindowState.Normal)
            {
                var size = new System.Drawing.Size((int)Width, (int)Height);
                Properties.Settings.Default.WindowSize = size;
            }
            else
            {
                var size = new System.Drawing.Size((int)RestoreBounds.Width, (int)RestoreBounds.Height);
                Properties.Settings.Default.WindowSize = size;
            }

            Properties.Settings.Default.Save();
        }

        void About_Click(object sender, RoutedEventArgs e)
        {
            aboutDialog.Top = Top + (Height / 2.0) - (aboutDialog.Height / 2.0);
            aboutDialog.Left = Left + (Width / 2.0) - (aboutDialog.Width / 2.0);
            aboutDialog.ShowDialog();
        }

        void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        void ImageArea_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            var filenames = (string[])e.Data.GetData("FileName");
            ReplaceImageInTab(filenames[0]);
            RefreshTab();
        }

        void ImageArea_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            RawKeyHandling(e);
            ValidatedKeyHandling(e);
        }

        void ImageArea_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e != null && e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (FileBrowser())
                {
                    DisplayImage();
                    ResetView();
                }
            }
        }

        void ImageArea_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            winFormsHost.ContextMenu.IsOpen |= e.Button == System.Windows.Forms.MouseButtons.Right;
        }
    }
}