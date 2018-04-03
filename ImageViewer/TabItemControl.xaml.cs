using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using Frame.Annotations;
using Frame.Properties;
using ImageMagick;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using TextAlignment = ImageMagick.TextAlignment;

namespace Frame
{
  public partial class TabItemControl : IDisposable, INotifyPropertyChanged
  {
    public ImageSettings ImageSettings { get; } = new ImageSettings();

    public ApplicationMode Mode
    {
      get => mode;
      set
      {
        mode = value;
        OnPropertyChanged();
      }
    }

    void UpdateFooter()
    {
      FooterModeText.Text     = FooterModeTextP;
      FooterSizeText.Text     = FooterSizeTextP;
      FooterChannelsText.Text = FooterChannelsTextP;
      FooterFilesizeText.Text = FooterFilesizeTextP;
      FooterZoomText.Text     = FooterZoomTextP;
      FooterIndexText.Text    = FooterIndexTextP;
      FooterMipIndexText.Text = FooterMipIndexTextP;
    }

    public uint CurrentSlideshowTime
    {
      get => currentSlideshowTime;
      set
      {
        currentSlideshowTime = value;
        OnPropertyChanged();
      }
    }

    public string InitialImagePath { get; set; }

    public int Index
    {
      get => index;
      set
      {
        index = value;
        OnPropertyChanged();
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

        var imageWidth  = ImageSettings.Width;
        var imageHeight = ImageSettings.Height;
        var borderWidth = (int) Math.Max(2.0, (imageWidth * imageHeight) / 200000.0);
        var channelNum  = 0;
        if (ChannelsMontage)
        {
          using (var orginalImage = ImageSettings.ImageCollection[0])
          {
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
                  Geometry = new MagickGeometry(imageWidth, imageHeight)
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
              Geometry = new MagickGeometry(imageWidth, imageHeight),
            };
          ImageSettings.ImageCollection.Clear();
          ImageSettings.ImageCollection.Add(images.Montage(montageSettings));
          ImageSettings.HasMips =  false;
          ImageSettings.Size    *= tileCount + 1;
        }

        OnPropertyChanged();
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
            var magickImage = ResizeCurrentMip();
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
    }

    string Filename => new FileInfo(Paths[Index]).Name;

    readonly MainWindow mainWindow;
    uint                currentSlideshowTime;
    bool                firstImageLoaded;
    MainWindow          ParentMainWindow => Window.GetWindow(this) as MainWindow;

    public TabItemControl(MainWindow mainWindow)
    {
      Margin          = new Thickness(0.5);
      this.mainWindow = mainWindow;
      InitializeComponent();

      PropertyChanged += (sender, args) =>
      {
        UpdateFooter();
        UpdateTitle();
      };

      ImageArea.KeyDown += (sender, args) => { this.mainWindow.ImageAreaKeyDown(sender, args); };
      ImageArea.Paint += (sender, args) =>
      {
        if (firstImageLoaded) return;
        ResetView();
        firstImageLoaded = true;
      };
    }

    string FooterModeTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "MODE: ";
        }

        if (Mode == ApplicationMode.Slideshow)
        {
          return $"MODE: {Mode} " + CurrentSlideshowTime;
        }

        return $"MODE: {Mode}";
      }
    }

    string FooterSizeTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "SIZE: ";
        }

        return $"SIZE: {ImageSettings.Width}x{ImageSettings.Height}";
      }
    }

    string FooterChannelsTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "CHANNELS: ";
        }

        var channel = string.Empty;
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

        return $"CHANNELS: {channel}";
      }
    }

    string FooterFilesizeTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "FILESIZE: ";
        }

        try
        {
          if (ImageSettings.Size == 0)
          {
            return "FILESIZE: ";
          }

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
        catch (FileNotFoundException)
        {
          return "FILESIZE: ";
        }
      }
    }

    string FooterZoomTextP => !ImageSettings.ImageCollection.Any() ? "ZOOM: " : $"ZOOM: {ImageArea.Zoom}%";

    string FooterIndexTextP => !ImageSettings.ImageCollection.Any() ? "INDEX: " : $"INDEX: {Index + 1}/{Paths.Count}";

    string FooterMipIndexTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "MIP: ";
        }

        return ImageSettings.HasMips ? $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}" : "MIP: None";
      }
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

    public bool Tiled           { get; set; }
    public bool ChannelsMontage { get; set; }

    void UpdateTitle()
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
      catch (Exception)
      {
        ImageSettings.ImageCollection.Clear();
        ImageSettings.ImageCollection.Add(ErrorImage(Path));
      }
    }

    void ImageAreaOnMouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right)
      {
        return;
      }

      if (ParentMainWindow.DockLayout.ContextMenu != null)
      {
        ParentMainWindow.DockLayout.ContextMenu.IsOpen = true;
      }
    }

    void Dispose(bool disposing)
    {
      if (disposing)
      {
        mainWindow?.Dispose();
        WinFormsHost?.Dispose();
        ImageArea?.Dispose();
        ImageSettings?.Dispose();
      }
    }

    public void Dispose()
    {
      Dispose(true);
    }

    ~TabItemControl()
    {
      Dispose(false);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}