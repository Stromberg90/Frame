//TODO Show file format info, like DDS(BC5), DDS(DXT1)
//TODO Uppgrade settings?
//TODO Sort event, so I can update footer.
//TODO GIF Support
//TODO Recent Files
//TODO Data bindings
//TODO Thumbnail using the render size, then I can load in the real image in the background, like when I open a folder make thumbnails for "all" the images in the folder.
//TODO Bar at the buttom with thumbnails of the images in the folder
//TODO Show hotkey next to menuitem
//TODO Progress bar when loading large images
//TODO Read sort setting from file explorer?
//TODO Slideshow Random Image Option, And Loop option
//TODO Thumbnail on tab
//TODO Folder browser

//BUG First time toggling the context menu, it shows up then goes away right away.
//BUG Background color/settings don't update for all tabs.
//BUG Probably several memory leaks, but there is definitly one when opening a bunch of tabs then closing them, it does not come down to the startup memory.
//BUG Doesn't reload if the current image changes.

//CHANGLOG
//1.0.4.3
//Fixed some problems with the tabs
//1.5
//Dragable and tearable tabs, tiling tabs.
//Reworked the tab system, faster to switch between tabs and remembers zoom and pan.
//Copy to path now uses windows style slashes.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using AutoUpdaterDotNET;
using Dragablz;
using Frame.Properties;
using ImageMagick;
using Microsoft.VisualBasic.FileIO;
using static System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Frame
{
  public partial class MainWindow
  {
    Channels DisplayChannel
    {
      get => tabControlManager.CurrentTab.ImageSettings.DisplayChannel;

      set
      {
        tabControlManager.CurrentTab.ImageSettings.DisplayChannel = value;
        RefreshImage();
      }
    }

    readonly TabControlManager tabControlManager;
    readonly SortingManager sortingManager;
    readonly FilesManager filesManager;
    FileSystemWatcher imageDirectoryWatcher;
    FileSystemWatcher parentDirectoryWatcher;

    public MainWindow()
    {
      AutoUpdater.ShowSkipButton = false;

      Settings.Default.PropertyChanged += (sender, args) => RefreshUi();
      Settings.Default.SettingsLoaded += (sender, args) => RefreshUi();

      InitializeComponent();
      ImageTabControl.NewItemFactory += NewTabItem;
      tabControlManager = new TabControlManager(ImageTabControl, ImageViewerWm, this);
      sortingManager = new SortingManager(ImageViewerWm, tabControlManager);
      filesManager = new FilesManager(sortingManager, ImageViewerWm, tabControlManager);

      CheckForUpdates();
      SetupSlideshow();
    }

    static DispatcherTimer slideshowTimer;

    readonly About aboutDialog = new About();
    readonly OptionsWindow optionsDialog = new OptionsWindow();
    int closingMainWindowCount;

    ImageViewerWm ImageViewerWm { get; } = new ImageViewerWm();
    static string BackwardToForwardSlash(string v) => v.Replace('\\', '/');


    void ValidatedKeyHandling(KeyEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      switch (e.KeyCode)
      {
        //TODO: Turn this into the command pattern.
        case Keys.A:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          ToggleDisplayChannel(Channels.Alpha);
          break;
        }
        case Keys.R:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          ToggleDisplayChannel(Channels.Red);
          break;
        }
        case Keys.G:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          ToggleDisplayChannel(Channels.Green);
          break;
        }
        case Keys.B:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          ToggleDisplayChannel(Channels.Blue);
          break;
        }
        case Keys.F:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          tabControlManager.CurrentTab.ResetView();
          break;
        }
        case Keys.D:
        {
          if (Keyboard.IsKeyDown(Key.LeftCtrl))
          {
            DuplicateTab();
          }

          break;
        }
        case Keys.W:
        {
          if (Keyboard.IsKeyDown(Key.LeftCtrl))
          {
            CloseTab();
          }

          break;
        }
        case Keys.S:
        {
          if (Keyboard.IsKeyDown(Key.LeftCtrl))
          {
            ChannelsMontage();
          }
          else
          {
            ToggleSlideshow();
          }

          break;
        }
        case Keys.T:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          TileImage();
          break;
        }
        case Keys.Right:
        {
          if (Keyboard.IsKeyDown(Key.LeftCtrl))
          {
            if (ImageTabControl.SelectedIndex == ImageTabControl.Items.Count - 1) return;
            ImageTabControl.SelectedIndex += 1;
          }
          else
          {
            SwitchImage(SwitchDirection.Next);
          }

          break;
        }
        case Keys.Left:
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
        case Keys.Space:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          SwitchImage(SwitchDirection.Next);
          break;
        }
        case Keys.Delete:
        {
          if (ModifierKeyDown())
          {
            return;
          }

          DeleteImage();
          break;
        }
        case Keys.Add:
        {
          tabControlManager.CurrentTab.ImageSettings.MipValue -= 1;
          RefreshImage();
          break;
        }
        case Keys.Subtract:
        {
          tabControlManager.CurrentTab.ImageSettings.MipValue += 1;
          RefreshImage();
          break;
        }
      }
    }

    void TileImage()
    {
      tabControlManager.CurrentTab.Tiled = !tabControlManager.CurrentTab.Tiled;
      RefreshImage();
      tabControlManager.CurrentTab.ResetView();
    }

    void ChannelsMontage()
    {
      tabControlManager.CurrentTab.ChannelsMontage = !tabControlManager.CurrentTab.ChannelsMontage;
      RefreshImage();
      tabControlManager.CurrentTab.ResetView();
    }

    static bool ModifierKeyDown()
    {
      return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
             Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ||
             Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
    }

    void ToggleSlideshow()
    {
      if (tabControlManager.CurrentTab.Mode == ApplicationMode.Slideshow)
      {
        tabControlManager.CurrentTab.Mode = ApplicationMode.Normal;
      }
      else
      {
        tabControlManager.CurrentTab.Mode = ApplicationMode.Slideshow;
      }

      if (tabControlManager.CurrentTab.Mode == ApplicationMode.Slideshow)
      {
        slideshowTimer.Start();
      }
      else
      {
        slideshowTimer.Stop();
      }

      tabControlManager.CurrentTab.UpdateTitle();
      tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
    }

    void DeleteImage()
    {
      var result = MessageBox.Show(this, "Do you want to move this file to the recycle bin?",
        $"{Properties.Resources.Delete}{FileSystem.GetName(tabControlManager.CurrentTab.Path)}",
        MessageBoxButton.YesNo);

      if (result != MessageBoxResult.Yes) return;

      FileSystem.DeleteFile(tabControlManager.CurrentTab.Path, UIOption.OnlyErrorDialogs,
        RecycleOption.SendToRecycleBin);

      if (tabControlManager.CurrentTab.Paths.Count > 0)
      {
        filesManager.SupportedFiles(Path.GetDirectoryName(tabControlManager.CurrentTab.Path));

        SwitchImage(SwitchDirection.Next);
      }
      else
      {
        FileBrowser();
      }
    }

    void RawKeyHandling(KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Escape:
        {
          Close();
          break;
        }
        case Keys.N:
        {
          if (Keyboard.IsKeyDown(Key.LeftCtrl))
          {
            AddNewTab(string.Empty);
          }

          break;
        }
      }
    }

    public TabItemControl GetNewTab(string filepath)
    {
      if (string.IsNullOrEmpty(filepath))
      {
        filepath = ImageViewerWm.ShowOpenFileDialog().FileName;
      }

      if (string.IsNullOrEmpty(filepath))
      {
        return null;
      }

      if (!FilesManager.ValidFile(filepath)) return null;

      var item = tabControlManager.GetTab(filepath);

      filesManager.SupportedFiles(Path.GetDirectoryName(filepath));

      var filenameIndex =
        tabControlManager.CurrentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

      tabControlManager.CurrentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

      tabControlManager.CurrentTab.InitialImagePath = filepath;

      UpdateView();
      SetupDirectoryWatcher();

      return item;
    }

    public void AddNewTab(string filepath)
    {
      if (string.IsNullOrEmpty(filepath))
      {
        filepath = ImageViewerWm.ShowOpenFileDialog().FileName;
      }

      if (string.IsNullOrEmpty(filepath))
      {
        return;
      }
      
      if (!FilesManager.ValidFile(filepath)) return;

      if (tabControlManager.TabControl.SelectedIndex != -1)
      {
        tabControlManager.TabControl.SelectedIndex = ImageTabControl.Items.Count - 1;
      }

      tabControlManager.AddTab(filepath);

      if (tabControlManager.TabControl.Visibility == Visibility.Collapsed)
      {
        tabControlManager.TabControl.Visibility = Visibility.Visible;
      }

      filesManager.SupportedFiles(Path.GetDirectoryName(filepath));

      var filenameIndex =
        tabControlManager.CurrentTab.Paths.FindIndex(x => Path.GetFileName(x) == Path.GetFileName(filepath));

      tabControlManager.CurrentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

      tabControlManager.CurrentTab.InitialImagePath = filepath;

      UpdateView();
      SetupDirectoryWatcher();
    }

    public void AscendingSort(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      if (tabControlManager.CurrentTab.ImageSettings.SortMode == SortMode.Descending)
      {
        ReversePaths();
      }

      tabControlManager.CurrentTab.ImageSettings.SortMode = SortMode.Ascending;
      SortDecending.IsChecked = false;
      SortAscending.IsChecked = true;
    }

    void CloseTab()
    {
      tabControlManager.CloseSelectedTab();
      Focus();
    }

    public void CopyPathToClipboard(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      Clipboard.SetText($"\"{tabControlManager.CurrentTab.Path}\"");
    }

    public void CopyFilenameToClipboard(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      Clipboard.SetText($"\"{BackwardToForwardSlash(Path.GetFileName(tabControlManager.CurrentTab.Path))}\"");
    }

    public void DecendingSort(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }


      if (tabControlManager.CurrentTab.ImageSettings.SortMode == SortMode.Ascending)
      {
        ReversePaths();
      }

      tabControlManager.CurrentTab.ImageSettings.SortMode = SortMode.Descending;
      SortDecending.IsChecked = true;
      SortAscending.IsChecked = false;
    }

    void ReversePaths()
    {
      var initalImage = tabControlManager.CurrentTab.Path;
      var filePathsList = tabControlManager.CurrentTab.Paths;
      filePathsList.Reverse();
      sortingManager.FindImageAfterSort(filePathsList, initalImage);
    }

    void DisplayAllChannels(object sender, RoutedEventArgs e)
    {
      SetDisplayChannel(Channels.RGB);
    }

    void DisplayAlphaChannel(object sender, RoutedEventArgs e)
    {
      SetDisplayChannel(Channels.Alpha);
    }

    void DisplayBlueChannel(object sender, RoutedEventArgs e)
    {
      SetDisplayChannel(Channels.Blue);
    }

    void DisplayGreenChannel(object sender, RoutedEventArgs e)
    {
      SetDisplayChannel(Channels.Green);
    }

    void DisplayRedChannel(object sender, RoutedEventArgs e)
    {
      SetDisplayChannel(Channels.Red);
    }

    void DisplayImage()
    {
      if (tabControlManager.CurrentTab == null)
      {
        return;
      }

      if (tabControlManager.CurrentTabIndex < 0 || tabControlManager.CurrentTab.Index == -1)
      {
        return;
      }

      if (tabControlManager.CurrentTab.ImageArea == null || !tabControlManager.CurrentTab.IsValid) return;

      tabControlManager.CurrentTab.ImageArea.Image = tabControlManager.CurrentTab.Image;

      tabControlManager.CurrentTab.UpdateTitle();
    }

    void DuplicateTab()
    {
      //TODO: Some events should probably be on key released instead of pressed.
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      AddNewTab(tabControlManager.CurrentTab.Path);
      Focus();
    }

    TabItemControl NewTabItem()
    {
      var fileDialog = new OpenFileDialog
      {
        Multiselect = false,
        AddExtension = true,
        Filter = FileFormats.FilterString
      };
      fileDialog.ShowDialog();

      return string.IsNullOrEmpty(fileDialog.SafeFileName) ? null : GetNewTab(Path.GetFullPath(fileDialog.FileName));
    }

    public void FileBrowser()
    {
      var fileDialog = ImageViewerWm.ShowOpenFileDialog();
      if (!fileDialog.SafeFileNames.Any())
        return;

      foreach (var fileName in fileDialog.FileNames)
      {
        AddNewTab(Path.GetFullPath(fileName));
      }
    }

    void ImageTabControlDrop(object sender, DragEventArgs e)
    {
      var filenames = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
      if (filenames != null)
      {
        var supportedFilenames = FilesManager.FilterSupportedFiles(filenames);
        foreach (var filename in supportedFilenames)
        {
          AddNewTab(filename);
        }
      }

      e.Handled = true;
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
        Settings.Default.ImageEditor = fileDialog.FileName;
        Process.Start(Settings.Default.ImageEditor, tabControlManager.CurrentTab.Path);
      }
    }

    public void OpenInImageEditor(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      if (!string.IsNullOrEmpty(Settings.Default.ImageEditor))
      {
        if (File.Exists(Settings.Default.ImageEditor))
        {
          Process.Start(Settings.Default.ImageEditor, tabControlManager.CurrentTab.Path);
          return;
        }

        if (MessageBox.Show("Editor not found\nDo you want to browse for editor?",
              Properties.Resources.FileMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
          ImageEditorBrowse();
        }
      }
      else
      {
        if (MessageBox.Show("No image editor specified in settings file\nDo you want to browse for editor?",
              Properties.Resources.ImageEditorMissing, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
          ImageEditorBrowse();
        }
      }

      Settings.Default.Save();
    }

    void RefreshImage()
    {
      if (!tabControlManager.CurrentTab.IsValid) return;

      tabControlManager.CurrentTab.ImageArea.Image = tabControlManager.CurrentTab.Image;
      tabControlManager.CurrentTab.UpdateTitle();
    }

    void UpdateView()
    {
      DisplayImage();
    }

    void RefreshUi()
    {
      if (!(ImageTabControl.SelectedItem is TabItemControl))
      {
        return;
      }

      ((TabItemControl) ImageTabControl.SelectedItem).ImageArea.GridColor = Settings.Default.BackgroundColor;
    }

    void ReplaceImageInTab(string filename)
    {
      if (!FilesManager.ValidFile(filename)) return;

      if (tabControlManager.CurrentTabIndex < 0)
      {
        AddNewTab(filename);
      }

      tabControlManager.CurrentTab.InitialImagePath = filename;
      filesManager.SupportedFiles(Path.GetDirectoryName(filename));

      var filenameIndex = tabControlManager.CurrentTab.Paths.IndexOf(filename);
      tabControlManager.CurrentTab.Index = filenameIndex == -1 ? 0 : filenameIndex;

      SetupDirectoryWatcher();
    }

    void SetCurrentImage(int newIndex)
    {
      tabControlManager.CurrentTab.Index = newIndex;
      DisplayImage();
    }

    void SetDisplayChannel(Channels channel)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      switch (channel)
      {
        case Channels.RGB:
        {
          DisplayChannel = Channels.RGB;
          break;
        }
        case Channels.Red:
        {
          DisplayChannel = Channels.Red;
          break;
        }
        case Channels.Green:
        {
          DisplayChannel = Channels.Green;
          break;
        }
        case Channels.Blue:
        {
          DisplayChannel = Channels.Blue;
          break;
        }
        case Channels.Alpha:
        {
          DisplayChannel = Channels.Alpha;
          break;
        }
      }
    }

    void ToggleDisplayChannel(Channels channel)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      switch (channel)
      {
        case Channels.RGB:
        {
          DisplayChannel = Channels.RGB;
          break;
        }
        case Channels.Red:
        {
          DisplayChannel = DisplayChannel == Channels.Red ? Channels.RGB : Channels.Red;
          break;
        }
        case Channels.Green:
        {
          DisplayChannel = DisplayChannel == Channels.Green ? Channels.RGB : Channels.Green;
          break;
        }
        case Channels.Blue:
        {
          DisplayChannel = DisplayChannel == Channels.Blue ? Channels.RGB : Channels.Blue;
          break;
        }
        case Channels.Alpha:
        {
          DisplayChannel = DisplayChannel == Channels.Alpha ? Channels.RGB : Channels.Alpha;
          break;
        }
      }
    }

    void SetupDirectoryWatcher()
    {
      var directoryName = Path.GetDirectoryName(tabControlManager.CurrentTab.InitialImagePath);
      if (directoryName == null)
      {
        return;
      }

      imageDirectoryWatcher = null;
      imageDirectoryWatcher = new FileSystemWatcher
      {
        Path = directoryName,
        NotifyFilter =
          NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
          NotifyFilters.DirectoryName
      };

      parentDirectoryWatcher = null;
      if (directoryName != Directory.GetDirectoryRoot(directoryName))
      {
        parentDirectoryWatcher = new FileSystemWatcher
        {
          Path = Directory.GetParent(directoryName).FullName,
          NotifyFilter =
            NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName |
            NotifyFilters.DirectoryName
        };

        parentDirectoryWatcher.Changed += ParentDirectoryChanged;
        parentDirectoryWatcher.Created += ParentDirectoryChanged;
        parentDirectoryWatcher.Deleted += ParentDirectoryChanged;
        parentDirectoryWatcher.Renamed += ParentDirectoryChanged;

        parentDirectoryWatcher.EnableRaisingEvents = true;
      }

      imageDirectoryWatcher.Changed += (sender, args) =>
        filesManager.SupportedFiles(directoryName);
      imageDirectoryWatcher.Created += (sender, args) =>
        filesManager.SupportedFiles(directoryName);
      imageDirectoryWatcher.Deleted += (sender, args) =>
        filesManager.SupportedFiles(directoryName);
      imageDirectoryWatcher.Renamed += (sender, args) =>
        filesManager.SupportedFiles(directoryName);

      imageDirectoryWatcher.EnableRaisingEvents = true;
    }

    void ParentDirectoryChanged(object sender, FileSystemEventArgs args)
    {
      Current.Dispatcher.Invoke(() =>
      {
        // Need to check all tabs
        switch (args.ChangeType)
        {
          case WatcherChangeTypes.Deleted:
          {
            if (Path.GetDirectoryName(tabControlManager.CurrentTab.InitialImagePath) == args.FullPath)
            {
              CloseTab();
            }

            break;
          }
          case WatcherChangeTypes.Changed:
            break;
          case WatcherChangeTypes.Renamed:
          {
            var renamedArgs = (RenamedEventArgs) args;
            var newFile = Path.Combine(renamedArgs.FullPath,
              Path.GetFileName(tabControlManager.CurrentTab.Path) ??
              throw new InvalidOperationException("It was the null"));
            if (Path.GetDirectoryName(tabControlManager.CurrentTab.InitialImagePath) ==
                renamedArgs.OldFullPath)
            {
              ReplaceImageInTab(newFile);
            }

            break;
          }
          case WatcherChangeTypes.All:
            break;
        }
      });
    }

    void SetupSlideshow()
    {
      slideshowTimer = new DispatcherTimer();
      slideshowTimer.Tick += Slideshow;
      slideshowTimer.Interval = new TimeSpan(0, 0, 1);
    }

    void Slideshow(object source, EventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        tabControlManager.CurrentTab.Mode = ApplicationMode.Normal;
        return;
      }

      if (tabControlManager.CurrentTab.CurrentSlideshowTime < ImageViewerWm.SlideshowInterval)
      {
        tabControlManager.CurrentTab.CurrentSlideshowTime += 1;
        tabControlManager.CurrentTab.UpdateTitle();
      }
      else
      {
        tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
        slideshowTimer.Stop();
        SwitchImage(SwitchDirection.Next);
        slideshowTimer.Start();
      }

      if (tabControlManager.CurrentTab.Mode == ApplicationMode.Slideshow) return;

      slideshowTimer.Stop();
      tabControlManager.CurrentTab.UpdateTitle();
      tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
    }

    void Slideshow10SecUI_Click(object sender, RoutedEventArgs e) =>
      SlideshowIntervalUi(SlideshowInterval.Seconds10);

    void Slideshow1SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Second1);

    void Slideshow20SecUI_Click(object sender, RoutedEventArgs e) =>
      SlideshowIntervalUi(SlideshowInterval.Seconds20);

    void Slideshow2SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds2);

    void Slideshow30SecUI_Click(object sender, RoutedEventArgs e) =>
      SlideshowIntervalUi(SlideshowInterval.Seconds30);

    void Slideshow3SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds3);

    void Slideshow4SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds4);

    void Slideshow5SecUI_Click(object sender, RoutedEventArgs e) => SlideshowIntervalUi(SlideshowInterval.Seconds5);

    void SlideshowIntervalUi(SlideshowInterval newInterval)
    {
      switch (newInterval)
      {
        case SlideshowInterval.Second1:
        {
          ImageViewerWm.SlideshowInterval = 1;
          break;
        }
        case SlideshowInterval.Seconds2:
        {
          ImageViewerWm.SlideshowInterval = 2;
          break;
        }
        case SlideshowInterval.Seconds3:
        {
          ImageViewerWm.SlideshowInterval = 3;
          break;
        }
        case SlideshowInterval.Seconds4:
        {
          ImageViewerWm.SlideshowInterval = 4;
          break;
        }
        case SlideshowInterval.Seconds5:
        {
          ImageViewerWm.SlideshowInterval = 5;
          break;
        }
        case SlideshowInterval.Seconds10:
        {
          ImageViewerWm.SlideshowInterval = 10;
          break;
        }
        case SlideshowInterval.Seconds20:
        {
          ImageViewerWm.SlideshowInterval = 20;
          break;
        }
        case SlideshowInterval.Seconds30:
        {
          ImageViewerWm.SlideshowInterval = 30;
          break;
        }
      }
    }

    public void SortByDateModified(object sender, RoutedEventArgs e)
    {
      if (tabControlManager.CurrentTab == null)
      {
        return;
      }

      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Date;
      sortingManager.Sort();
      SortDate.IsChecked = true;
      SortName.IsChecked = false;
      SortSize.IsChecked = false;
    }

    public void SortByName(object sender, RoutedEventArgs e)
    {
      if (tabControlManager.CurrentTab == null)
      {
        return;
      }

      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Name;
      sortingManager.Sort();
      SortDate.IsChecked = false;
      SortName.IsChecked = true;
      SortSize.IsChecked = false;
    }

    public void SortBySize(object sender, RoutedEventArgs e)
    {
      if (tabControlManager.CurrentTab == null)
      {
        return;
      }

      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      tabControlManager.CurrentTab.ImageSettings.SortMethod = SortMethod.Size;
      sortingManager.Sort();
      SortName.IsChecked = false;
      SortDate.IsChecked = false;
      SortSize.IsChecked = true;
    }

    void StartSlideshowUiClick(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      tabControlManager.CurrentTab.Mode = ApplicationMode.Slideshow;
      tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
      slideshowTimer.Start();
    }

    void StopSlideshowUiClick(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      tabControlManager.CurrentTab.Mode = ApplicationMode.Normal;
      tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
      slideshowTimer.Stop();
    }

    void SwitchImage(SwitchDirection switchDirection)
    {
      tabControlManager.CurrentTab.ImageSettings.MipValue = 0;
      tabControlManager.CurrentTab.Tiled = false;
      tabControlManager.CurrentTab.ChannelsMontage = false;
      if (tabControlManager.CurrentTab.Mode == ApplicationMode.Slideshow)
      {
        tabControlManager.CurrentTab.CurrentSlideshowTime = 1;
      }

      switch (switchDirection)
      {
        case SwitchDirection.Next:
          if (tabControlManager.CurrentTab.Index < tabControlManager.CurrentTab.Paths.Count - 1)
          {
            SetCurrentImage(tabControlManager.CurrentTab.Index += 1);
          }
          else
          {
            SetCurrentImage(0);
          }

          break;

        case SwitchDirection.Previous:
          if (tabControlManager.CurrentTab.Paths.Any())
          {
            if (tabControlManager.CurrentTab.Index > 0)
            {
              SetCurrentImage(tabControlManager.CurrentTab.Index -= 1);
            }
            else
            {
              SetCurrentImage(tabControlManager.CurrentTab.Index = tabControlManager.CurrentTab.Paths.Count - 1);
            }
          }

          break;
      }

      tabControlManager.CurrentTab.ResetView();
    }

    public void ViewInExplorer(object sender, RoutedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      Process.Start("explorer.exe", "/select, " + tabControlManager.CurrentTab.Path);
    }

    public void AlwaysOnTopClick(object sender, RoutedEventArgs e)
    {
      if (tabControlManager.CurrentTab == null)
      {
        return;
      }

      Topmost = !Topmost;
      AlwaysOnTopUi.IsChecked = Topmost;
    }

    void WindowLoaded(object sender, RoutedEventArgs e)
    {
      Left = Settings.Default.WindowLocation.X;
      Top = Settings.Default.WindowLocation.Y;

      Width = Settings.Default.WindowSize.Width;
      Height = Settings.Default.WindowSize.Height;

      WindowState = (WindowState) Settings.Default.WindowState;

      e.Handled = true;
    }

    void WindowClosing(object sender, CancelEventArgs e)
    {
      closingMainWindowCount = 0;
      foreach (var window in Current.Windows)
      {
        if (window.GetType() == typeof(MainWindow))
        {
          closingMainWindowCount++;
        }
      }

      Settings.Default.WindowLocation = new Point((int) Left, (int) Top);
      Settings.Default.WindowState = (int) WindowState;
      if (WindowState == WindowState.Normal)
      {
        Settings.Default.WindowSize = new Size((int) Width, (int) Height);
      }
      else
      {
        Settings.Default.WindowSize = new Size((int) RestoreBounds.Width, (int) RestoreBounds.Height);
      }

      Settings.Default.Save();
    }

    public void AboutClick(object sender, RoutedEventArgs e)
    {
      if (WindowState == WindowState.Maximized)
      {
        var rect = Screen.GetWorkingArea(new Point((int) Left, (int) Top));
        aboutDialog.Top = rect.Top + (ActualHeight / 2.0) - (aboutDialog.Height / 2.0);
        aboutDialog.Left = rect.Left + (ActualWidth / 2.0) - (aboutDialog.Width / 2.0);
      }
      else
      {
        aboutDialog.Top = Top + (ActualHeight / 2.0) - (aboutDialog.Height / 2.0);
        aboutDialog.Left = Left + (ActualWidth / 2.0) - (aboutDialog.Width / 2.0);
      }

      aboutDialog.ShowDialog();
    }

    void WindowClosed(object sender, EventArgs e)
    {
      if (closingMainWindowCount == 1)
      {
        Current.Shutdown();
      }
    }

    void ImageAreaDragDrop(object sender, DragEventArgs e)
    {
      var filenames = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
      if (filenames != null)
      {
        var supportedFilenames = FilesManager.FilterSupportedFiles(filenames);
        if (supportedFilenames.Length == 0)
        {
          return;
        }

        if (supportedFilenames.Length > 1)
        {
          foreach (var filename in supportedFilenames)
          {
            AddNewTab(filename);
          }
        }
        else
        {
          if (Settings.Default.ReplaceImageOnDrop)
          {
            ReplaceImageInTab(supportedFilenames[0]);
          }
          else
          {
            AddNewTab(supportedFilenames[0]);
          }
        }
      }

      e.Handled = true;
      UpdateView();
    }

    public void ImageAreaKeyDown(object sender, KeyEventArgs e)
    {
      RawKeyHandling(e);
      ValidatedKeyHandling(e);
      e.Handled = true;
    }

    public void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
      string keyString;
      switch (e.Key)
      {
        case Key.LeftShift:
        {
          keyString = Keys.LShiftKey.ToString();
          break;
        }
        case Key.LeftCtrl:
        {
          keyString = Keys.LControlKey.ToString();
          break;
        }
        case Key.System:
        case Key.LeftAlt:
        {
          keyString = Keys.Alt.ToString();
          break;
          }
        default:
        {
          keyString = e.Key.ToString();
          break;
        }
      }

      Keys key;
      key = (Keys) new KeysConverter().ConvertFromString(keyString);
      ImageAreaKeyDown(sender, new KeyEventArgs(key));
      e.Handled = true;
    }

    public void TileImageOnClick(object sender, RoutedEventArgs e) => TileImage();

    public void ChannelsMontageOnClick(object sender, RoutedEventArgs e) => ChannelsMontage();

    public void OptionsOnClick(object sender, RoutedEventArgs e)
    {
      if (WindowState == WindowState.Maximized)
      {
        var rect = Screen.GetWorkingArea(new Point((int) Left, (int) Top));
        optionsDialog.Top = rect.Top + (ActualHeight / 2.0) - (optionsDialog.Height / 2.0);
        optionsDialog.Left = rect.Left + (ActualWidth / 2.0) - (optionsDialog.Width / 2.0);
      }
      else
      {
        optionsDialog.Top = Top + (ActualHeight / 2.0) - (optionsDialog.Height / 2.0);
        optionsDialog.Left = Left + (ActualWidth / 2.0) - (optionsDialog.Width / 2.0);
      }

      optionsDialog.ShowDialog();
    }

    public void CheckForUpdateOnClick(object sender, RoutedEventArgs e) => CheckForUpdates();

    static void CheckForUpdates() => AutoUpdater.Start("http://www.dropbox.com/s/2b0gna7rz889b5u/Update.xml?dl=1");

    void ImageTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (!tabControlManager.CanExcectute())
      {
        return;
      }

      Focus();
    }

    void ResetViewClick(object sender, RoutedEventArgs e)
    {
      if (tabControlManager.CanExcectute())
      {
        tabControlManager.CurrentTab.ResetView();
      }
    }

    void OpenFilesClick(object sender, RoutedEventArgs e)
    {
      FileBrowser();
    }
  }
}