using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using Frame.Properties;
using ImageMagick;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using TextAlignment = ImageMagick.TextAlignment;

namespace Frame
{
  public partial class TabItemControl : IDisposable, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ImageSettings ImageSettings { get; } = new ImageSettings();

    public ApplicationMode Mode
    {
      get => mode;
      set
      {
        if (value == mode) return;
        mode = value;
        NotifyPropertyChanged();
        UpdateFooter();
      }
    }

    public uint   CurrentSlideshowTime { get; set; }
    public string InitialImagePath     { get; set; }

    public int Index
    {
      get => index;
      set
      {
        if (value == index) return;
        index = value;
        NotifyPropertyChanged();
        UpdateFooter();
      }
    }

    public List<string> Paths { get; set; } = new List<string>();

    public bool IsValid => Paths.Any();

    ApplicationMode mode = ApplicationMode.Normal;
    int             index;

    public Image Image
    {
      get
      {
        LoadImage();

        var borderWidth = (int) Math.Max(2.0, (ImageSettings.Width * ImageSettings.Height) / 200000.0);
        var channelNum  = 0;
        if (ChannelsMontage)
        {
          using (var orginalImage = ImageSettings.ImageCollection[0])
          {
            ImageSettings.SavedSize = new FileInfo(orginalImage.FileName).Length;
            using (var images = new MagickImageCollection())
            {
              foreach (var img in ImageSettings.ImageCollection[ImageSettings.MipValue].Separate())
              {
                if (Settings.Default.SplitChannelsBorder)
                {
                  switch (channelNum)
                  {
                    case 0:
                    {
                      img.BorderColor = MagickColor.FromRgb(255, 0, 0);
                      img.Border(borderWidth);
                      break;
                    }
                    case 1:
                    {
                      img.BorderColor = MagickColor.FromRgb(0, 255, 0);
                      img.Border(borderWidth);
                      break;
                    }
                    case 2:
                    {
                      img.BorderColor = MagickColor.FromRgb(0, 0, 255);
                      img.Border(borderWidth);
                      break;
                    }
                  }

                  channelNum += 1;
                }

                if (ImageSettings.MipValue > 0)
                {
                  img.Resize(orginalImage.Width, orginalImage.Height);
                }

                images.Add(img);
              }

              var montageSettings =
                new MontageSettings
                {
                  Geometry = new MagickGeometry(ImageSettings.Width, ImageSettings.Height)
                };
              var result = images.Montage(montageSettings);
              ImageSettings.ImageCollection.Clear();
              ImageSettings.ImageCollection.Add(result);
            }
          }

          ImageSettings.HasMips = false;
        }

        if (Tiled)
        {
          var       images       = new MagickImageCollection();
          const int tileCount    = 8;
          var       orginalImage = ImageSettings.ImageCollection[0];
          ImageSettings.SavedSize = new FileInfo(orginalImage.FileName).Length;
          for (var i = 0; i <= tileCount; i++)
          {
            var image = ImageSettings.ImageCollection[ImageSettings.MipValue].Clone();
            if (ImageSettings.MipValue > 0)
            {
              image.Resize(orginalImage.Width, orginalImage.Height);
            }

            if (ImageSettings.DisplayChannel != Channels.Alpha)
            {
              image.Alpha(AlphaOption.Opaque);
            }

            images.Add(image);
          }

          var montageSettings =
            new MontageSettings
            {
              Geometry = new MagickGeometry(ImageSettings.Width, ImageSettings.Height)
            };
          ImageSettings.ImageCollection.Clear();
          ImageSettings.ImageCollection.Add(images.Montage(montageSettings));
          ImageSettings.HasMips = false;
        }

        UpdateFooter();
        switch (ImageSettings.DisplayChannel)
        {
          case Channels.Red:
          {
            var magickImage = ResizeCurrentMip();
            return magickImage.Separate(Channels.Red)
                              .ElementAt(0)?.ToBitmap();
          }
          case Channels.Green:
          {
            var magickImage = ImageSettings.ImageCollection[ImageSettings.MipValue];
            if (ImageSettings.MipValue > 0)
            {
              magickImage.Resize(ImageSettings.ImageCollection[0].Width, ImageSettings.ImageCollection[0].Height);
            }

            return magickImage.Separate(Channels.Green)
                              .ElementAt(0)?.ToBitmap();
          }
          case Channels.Blue:
          {
            var magickImage = ResizeCurrentMip();

            return magickImage.Separate(Channels.Blue)
                              .ElementAt(0)?.ToBitmap();
          }
          case Channels.Alpha:
          {
            var magickImage = ResizeCurrentMip();

            return magickImage.Separate(Channels.Alpha)
                              .ElementAt(0)?.ToBitmap();
          }
          default:
          {
            var magickImage = ResizeCurrentMip();

            magickImage.Alpha(AlphaOption.Opaque);
            return magickImage.ToBitmap();
          }
        }
      }
    }

    IMagickImage ResizeCurrentMip()
    {
      var magickImage = ImageSettings.ImageCollection[ImageSettings.MipValue];
      if (ImageSettings.MipValue > 0)
      {
        magickImage.Resize(ImageSettings.ImageCollection[0].Width, ImageSettings.ImageCollection[0].Height);
      }

      return magickImage;
    }

    public string Path => Index < Paths.Count ? Paths[Index] : Paths[0];

    string Title
    {
      set => Header = value;
      get => Header.ToString();
    }

    string Filename => new FileInfo(Paths[Index]).Name;

    readonly MainWindow mainWindow;

    public TabItemControl(MainWindow mainWindow)
    {
      Margin          = new Thickness(0.5);
      this.mainWindow = mainWindow;
      InitializeComponent();
      ImageArea.KeyDown          += (sender, args) => { this.mainWindow.ImageAreaKeyDown(sender, args); };
      ImageArea.AllowDoubleClick =  true;
      ImageArea.MouseDoubleClick += (sender, args) => { this.mainWindow.WindowMouseDoubleClick(sender, args); };
      ImageArea.MouseDown        += ImageAreaOnMouseDown;
      ImageArea.ImageChanged     += ResetViewClick;
    }

    void WinFormsHostLoaded(object sender, RoutedEventArgs e)
    {
      if (Environment.GetCommandLineArgs().Length <= 1) return;

      foreach (var filePath in Environment.GetCommandLineArgs().Skip(1))
      {
        mainWindow.AddNewTab(filePath);
      }
    }

    void UpdateFooter()
    {
      if (!ImageSettings.ImageCollection.Any())
      {
        FooterModeText.Text     = "MODE: ";
        FooterSizeText.Text     = "SIZE: ";
        FooterChannelsText.Text = "CHANNELS: ";
        FooterFilesizeText.Text = "FILESIZE: ";
        FooterZoomText.Text     = "ZOOM: ";
        FooterIndexText.Text    = "INDEX: ";
        FooterMipIndexText.Text = "MIP: ";
      }
      else
      {
        var channel = string.Empty;

        FooterModeText.Text     = FooterMode;
        FooterSizeText.Text     = FooterSize;
        FooterFilesizeText.Text = FooterFilesize;
        FooterIndexText.Text    = FooterIndex;
        FooterMipIndexText.Text = FooterMipIndex;
        switch (ImageSettings.DisplayChannel)
        {
          case (Channels.RGB):
          {
            channel = ImageSettings.DisplayChannel.ToString();
            break;
          }
          case (Channels.Red):
          {
            channel = "Red";
            break;
          }
          case (Channels.Green):
          {
            channel = ImageSettings.DisplayChannel.ToString();
            break;
          }
          case (Channels.Blue):
          {
            channel = ImageSettings.DisplayChannel.ToString();
            break;
          }
          case (Channels.Opacity):
          {
            channel = "Alpha";
            break;
          }
        }

        FooterChannelsText.Text = $"Channels: {channel}";
        FooterZoomText.Text     = $"Zoom: {ImageArea.Zoom}%";
      }
    }

    public void ResetViewClick(object sender, EventArgs e)
    {
      ResetView();
    }

    public void ResetView()
    {
      if (ImageArea == null)
      {
        return;
      }

      if (Settings.Default.ImageFullZoom)
      {
        ImageArea.Zoom = 100;
        return;
      }

      if (ImageArea.Size.Width < ImageSettings.Width ||
          ImageArea.Size.Height < ImageSettings.Height)
      {
        ImageArea.ZoomToFit();
      }
      else
      {
        ImageArea.Zoom = 100;
      }
    }

    void ImageAreaZoomChanged(object sender, EventArgs e) => UpdateFooter();

    public string FooterMode
    {
      get
      {
        if (Mode == ApplicationMode.Slideshow)
        {
          return $"MODE: {Mode} " + CurrentSlideshowTime;
        }

        return $"MODE: {Mode}";
      }
    }

    public string FooterSize => $"SIZE: {ImageSettings.Width}x{ImageSettings.Height}";


    public string FooterFilesize
    {
      get
      {
        if (ImageSettings.Size < 1024)
        {
          return $"FILESIZE: {ImageSettings.Size}Bytes";
        }

        if (ImageSettings.Size < 1048576)
        {
          var filesize = (double) (ImageSettings.Size / 1024f);
          return $"FILESIZE: {filesize:N2}KB";
        }
        else
        {
          var filesize = (double) (ImageSettings.Size / 1024f) / 1024f;
          return $"FILESIZE: {filesize:N2}MB";
        }
      }
    }

    public string FooterIndex     => $"INDEX: {Index + 1}/{Paths.Count}";
    public bool   Tiled           { get; set; }
    public bool   ChannelsMontage { get; set; }

    public string FooterMipIndex => ImageSettings.HasMips
      ? $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}"
      : "MIP: None";

    public void UpdateTitle()
    {
      Title = Filename;
    }

    static MagickImage ErrorImage(string filepath)
    {
      var image = new MagickImage(MagickColors.White, 512, 512);
      new Drawables()
        .FontPointSize(18)
        .Font("Arial")
        .FillColor(MagickColors.Red)
        .TextAlignment(TextAlignment.Center)
        .Text(256, 256, $"Could not load\n{System.IO.Path.GetFileName(filepath)}")
        .Draw(image);

      return image;
    }

    void LoadImage()
    {
      ImageSettings.SavedSize = null;
      try
      {
        switch (System.IO.Path.GetExtension(Path))
        {
          case ".gif":
          {
            ImageSettings.HasMips  = false;
            ImageSettings.MipValue = 0;
            ImageSettings.ImageCollection.Clear();
            ImageSettings.ImageCollection.Add(Path);
            break;
          }
          case ".dds":
          {
            var defines      = new DdsReadDefines {SkipMipmaps = false};
            var readSettings = new MagickReadSettings(defines);
            ImageSettings.ImageCollection = new MagickImageCollection(Path, readSettings);
            ImageSettings.HasMips         = ImageSettings.ImageCollection.Count > 1;
            if (ImageSettings.HasMips)
            {
              ImageSettings.MipCount = ImageSettings.ImageCollection.Count;
            }

            break;
          }
          default:
          {
            ImageSettings.HasMips         = false;
            ImageSettings.MipValue        = 0;
            ImageSettings.ImageCollection = new MagickImageCollection(Path);
            break;
          }
        }
      }
      catch (MagickCoderErrorException)
      {
        ImageSettings.ImageCollection.Clear();
        ImageSettings.ImageCollection.Add(ErrorImage(Path));
      }
      catch (MagickMissingDelegateErrorException)
      {
        ImageSettings.ImageCollection.Clear();
        ImageSettings.ImageCollection.Add(ErrorImage(Path));
      }
      catch (MagickCorruptImageErrorException)
      {
        ImageSettings.ImageCollection.Clear();
        ImageSettings.ImageCollection.Add(ErrorImage(Path));
      }
    }

    public void Dispose()
    {
      Dispose(true);
    }

    void Dispose(bool disposing)
    {
      if (disposing && ImageSettings.ImageCollection != null)
      {
        ImageSettings.ImageCollection.Dispose();
        ImageSettings.ImageCollection = null;
      }
    }

    void ImageAreaOnMouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right)
      {
        return;
      }

      if (TabItemControlDockPanel.ContextMenu != null)
      {
        TabItemControlDockPanel.ContextMenu.IsOpen = true;
      }
    }

    void AlwaysOnTopClick(object sender, RoutedEventArgs e)
    {
      mainWindow.AlwaysOnTopClick(sender, e);
    }

    void OpenInImageEditor(object sender, RoutedEventArgs e)
    {
      mainWindow.OpenInImageEditor(sender, e);
    }

    void CopyPathToClipboard(object sender, RoutedEventArgs e)
    {
      mainWindow.CopyPathToClipboard(sender, e);
    }

    void CopyFilenameToClipboard(object sender, RoutedEventArgs e)
    {
      mainWindow.CopyFilenameToClipboard(sender, e);
    }

    void ViewInExplorer(object sender, RoutedEventArgs e)
    {
      mainWindow.ViewInExplorer(sender, e);
    }

    void TileImageOnClick(object sender, RoutedEventArgs e)
    {
      mainWindow.TileImageOnClick(sender, e);
    }

    void ChannelsMontageOnClick(object sender, RoutedEventArgs e)
    {
      mainWindow.ChannelsMontageOnClick(sender, e);
    }

    void AscendingSort(object sender, RoutedEventArgs e)
    {
      mainWindow.AscendingSort(sender, e);
    }

    void DecendingSort(object sender, RoutedEventArgs e)
    {
      mainWindow.DecendingSort(sender, e);
    }

    void SortByName(object sender, RoutedEventArgs e)
    {
      mainWindow.SortByName(sender, e);
    }

    void SortByDateModified(object sender, RoutedEventArgs e)
    {
      mainWindow.SortByDateModified(sender, e);
    }

    void SortBySize(object sender, RoutedEventArgs e)
    {
      mainWindow.SortBySize(sender, e);
    }

    void AboutClick(object sender, RoutedEventArgs e)
    {
      mainWindow.AboutClick(sender, e);
    }

    void OptionsOnClick(object sender, RoutedEventArgs e)
    {
      mainWindow.OptionsOnClick(sender, e);
    }

    void CheckForUpdateOnClick(object sender, RoutedEventArgs e)
    {
      mainWindow.CheckForUpdateOnClick(sender, e);
    }
  }
}