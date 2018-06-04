using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Frame.Annotations;
using Frame.Properties;
using ImageMagick;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextAlignment = ImageMagick.TextAlignment;

namespace Frame
{
    public partial class TabItemControl : IDisposable, INotifyPropertyChanged
  {
    public readonly ImageSettings ImageSettings;

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

    public string InitialImagePath;

    public int Index
    {
      get => index;
      set
      {
        index = value;
        OnPropertyChanged();
      }
    }

    public List<string> Paths;

    ApplicationMode mode = ApplicationMode.Normal;
    int             index;

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

    string Filename => new FileInfo(Paths[Index]).Name;

    uint                         currentSlideshowTime;
    bool                         firstImageLoaded;
    readonly System.Timers.Timer gifTimer;
    bool                         collapseFooterText;

    MainWindow ParentMainWindow =>
      Dispatcher.Invoke(() => (MainWindow) Window.GetWindow(this));

    protected override void OnSelected(RoutedEventArgs e)
    {
      base.OnSelected(e);
      if (ImageSettings.IsGif)
      {
        gifTimer.Start();
      }
    }

    protected override void OnUnselected(RoutedEventArgs e)
    {
      base.OnUnselected(e);
      gifTimer.Stop();
    }

    public TabItemControl()
    {
      Margin = new Thickness(0.5);
      InitializeComponent();

      ImageSettings = new ImageSettings();
      Paths         = new List<string>();

      ImagePresenter.PreviewKeyDown += ImagePresenterOnPreviewKeyDown;

      gifTimer         =  new System.Timers.Timer();
      gifTimer.Elapsed += GifAnim;

      ImagePresenter.PropertyChanged += (sender, args) => { UpdateFooter(); };
      ImageSettings.PropertyChanged += (sender, args) =>
      {
        UpdateFooter();
        Header = Filename;
      };
      PropertyChanged                  += OnPropertyChanged;
      ImagePresenter.ImageArea.KeyDown += (sender, args) => { ParentMainWindow.ImageAreaKeyDown(sender, args); };
      ImagePresenter.ImageArea.Loaded += (sender, args) =>
      {
        ImagePresenter.Grid.Width  = ImageSettings.Width;
        ImagePresenter.Grid.Height = ImageSettings.Height;
        if (firstImageLoaded) return;
        ResetView();
        firstImageLoaded = true;
      };
    }

    void OnPropertyChanged(object o, PropertyChangedEventArgs args)
    {
      if (args.PropertyName == nameof(Index))
      {
        Application.Current?.Dispatcher.Invoke(() => { ParentMainWindow.RefreshImage(); });
        ResetView();
      }

      UpdateFooter();
      Header = Filename;
    }

    string FooterModeTextP
    {
      get
      {
        if (Mode == ApplicationMode.Slideshow)
        {
          if (!collapseFooterText)
          {
            return $"MODE: {Mode} " + CurrentSlideshowTime;
          }

          return $"{Mode} " + CurrentSlideshowTime;
        }

        return !collapseFooterText ? $"MODE: {Mode}" : $"{Mode}";
      }
    }

    string FooterSizeTextP => !collapseFooterText
      ? $"SIZE: {ImageSettings.Width}x{ImageSettings.Height}"
      : $"{ImageSettings.Width}x{ImageSettings.Height}";

    string FooterChannelsTextP
    {
      get
      {
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

        return !collapseFooterText ? $"CHANNELS: {channel}" : $"{channel}";
      }
    }

    string FooterFilesizeTextP
    {
      get
      {
        try
        {
          if (ImageSettings.Size == 0)
          {
            return "FILESIZE: ";
          }

          if (ImageSettings.Size < 1024)
          {
            return !collapseFooterText
              ? $"FILESIZE: {ImageSettings.Size}Bytes"
              : $"{ImageSettings.Size}Bytes";
          }

          if (ImageSettings.Size < 1048576)
          {
            var filesize = (double) (ImageSettings.Size / 1024f);
            return !collapseFooterText ? $"FILESIZE: {filesize:N2}KB" : $"{filesize:N2}KB";
          }
          else
          {
            var filesize = (double) (ImageSettings.Size / 1024f) / 1024f;
            return !collapseFooterText ? $"FILESIZE: {filesize:N2}MB" : $"{filesize:N2}MB";
          }
        }
        catch (FileNotFoundException)
        {
          return "FILESIZE: ";
        }
      }
    }

    string FooterZoomTextP =>
      !collapseFooterText ? $"ZOOM: {ImagePresenter.Zoom:N2}%" : $"{ImagePresenter.Zoom:N2}%";

    string FooterIndexTextP =>
      !collapseFooterText ? $"INDEX: {Index + 1}/{Paths.Count}" : $"{Index + 1}/{Paths.Count}";

    string FooterMipIndexTextP
    {
      get
      {
        if (ImageSettings.HasMips)
        {
          return !collapseFooterText
            ? $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}"
            : $"{ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
        }

        return !collapseFooterText ? "MIP: None" : "None";
      }
    }

    public void ResetView()
    {
      ParentMainWindow.Focus();

      if (ImagePresenter.ImageArea == null)
      {
        return;
      }

      ImagePresenter.Grid.Width  = ImageSettings.Width;
      ImagePresenter.Grid.Height = ImageSettings.Height;

      ImagePresenter.Zoom = 100;

      if (Settings.Default.ImageFullZoom)
      {
        return;
      }

      if (ImagePresenter.ActualWidth < ImageSettings.Width ||
          ImagePresenter.ActualHeight < ImageSettings.Height)
      {
        ZoomToFit();
      }
    }

    void ZoomToFit()
    {
      while (ImagePresenter.Grid.Width * ImagePresenter.ScaleTransform.ScaleX <
             ImagePresenter.ActualWidth ||
             ImagePresenter.Grid.Height * ImagePresenter.ScaleTransform.ScaleX <
             ImagePresenter.ActualHeight)
      {
        ImagePresenter.Zoom += 1;
      }

      while (ImagePresenter.Grid.Width * ImagePresenter.ScaleTransform.ScaleX >
             ImagePresenter.ActualWidth ||
             ImagePresenter.Grid.Height * ImagePresenter.ScaleTransform.ScaleX >
             ImagePresenter.ActualHeight)
      {
        ImagePresenter.Zoom -= 1;
      }
    }

    public bool Tiled;
    public bool ChannelsMontage;

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

    internal void LoadImage()
    {
      if (ImageSettings.IsGif)
      {
        var image = ImageSettings.ImageCollection[ImageSettings.CurrentFrame];
        gifTimer.Interval               = image.AnimationDelay == 0 ? 1 : image.AnimationDelay * 10;
        ImagePresenter.ImageArea.Source = image.ToBitmapSource();
        return;
      }

      try
      {
        switch (System.IO.Path.GetExtension(Path))
        {
          case ".gif":
          {
            ImageSettings.ImageCollection = new MagickImageCollection(Path);
            ImageSettings.ImageCollection.Coalesce();
            if (!gifTimer.Enabled)
            {
              gifTimer.Start();
              gifTimer.Interval = ImageSettings.ImageCollection[ImageSettings.CurrentFrame].AnimationDelay;
            }

            ImageSettings.IsGif    = true;
            ImageSettings.HasMips  = false;
            ImageSettings.EndFrame = ImageSettings.ImageCollection.Count - 1;
            ImageSettings.MipValue = 0;
            return;
          }
          case ".dds":
          {
            ImageSettings.IsGif        = false;
            ImageSettings.CurrentFrame = 0;
            gifTimer.Stop();
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
            ImageSettings.IsGif        = false;
            ImageSettings.CurrentFrame = 0;
            gifTimer.Stop();
            ImageSettings.HasMips  = false;
            ImageSettings.MipValue = 0;
            ImageSettings.ImageCollection = new MagickImageCollection
            {
              Path
            };
            break;
          }
        }
      }
      catch (Exception e)
      {
        ImageSettings.ImageCollection.Clear();
        ImageSettings.ImageCollection.Add(ErrorImage(Path));
      }

      var imageWidth    = ImageSettings.Width;
      var imageHeight   = ImageSettings.Height;
      var borderWidth   = (int) Math.Max(2.0, (imageWidth * imageHeight) / 200000.0);
      var channelNum    = 0;
      var settingsImage = ImageSettings.ImageCollection[ImageSettings.MipValue];
      if (ChannelsMontage)
      {
        using (var orginalImage = ImageSettings.ImageCollection[0])
        {
          using (var images = new MagickImageCollection())
          {
            foreach (var img in settingsImage.Separate())
            {
              if (Settings.Default.SplitChannelsBorder)
              {
                switch (channelNum)
                {
                  case 0:
                  {
                    img.BorderColor = MagickColor.FromRgb(255, 0, 0);
                    break;
                  }
                  case 1:
                  {
                    img.BorderColor = MagickColor.FromRgb(0, 255, 0);
                    break;
                  }
                  case 2:
                  {
                    img.BorderColor = MagickColor.FromRgb(0, 0, 255);
                    break;
                  }
                }

                img.Border(borderWidth);
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
          var image = settingsImage.Clone();
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
      var magickImage = ResizeCurrentMip();
      switch (ImageSettings.DisplayChannel)
      {
        case Channels.RGB:
        {
          magickImage.Alpha(AlphaOption.Opaque);
          ImagePresenter.ImageArea.Source = magickImage.ToBitmapSource();
          break;
        }
        case Channels.Red:
        {
          ImagePresenter.ImageArea.Source = magickImage.Separate(Channels.Red)
                                                       .ElementAt(0)?.ToBitmapSource();
          break;
        }
        case Channels.Green:
        {
          ImagePresenter.ImageArea.Source = magickImage.Separate(Channels.Green)
                                                       .ElementAt(0)?.ToBitmapSource();
          break;
        }
        case Channels.Blue:
        {
          ImagePresenter.ImageArea.Source = magickImage.Separate(Channels.Blue)
                                                       .ElementAt(0)?.ToBitmapSource();
          break;
        }
        case Channels.Alpha:
        {
          ImagePresenter.ImageArea.Source = magickImage.Separate(Channels.Alpha)
                                                       .ElementAt(0)?.ToBitmapSource();
          break;
        }
        default:
        {
          magickImage.Alpha(AlphaOption.Opaque);
          ImagePresenter.ImageArea.Source = magickImage.ToBitmapSource();
          break;
        }
      }
    }

    void GifAnim(object sender, EventArgs e)
    {
      if (ImageSettings.CurrentFrame < ImageSettings.EndFrame)
      {
        ImageSettings.CurrentFrame += 1;
      }
      else
      {
        ImageSettings.CurrentFrame = 0;
      }

      Application.Current?.Dispatcher.Invoke(ParentMainWindow.RefreshImage);
    }

    void Dispose(bool disposing)
    {
      if (!disposing)
      {
        return;
      }

      gifTimer.Close();
      ImageSettings?.Dispose();
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

    public void SwitchImage(SwitchDirection switchDirection)
    {
      ImageSettings.Reset();
      Tiled           = false;
      ChannelsMontage = false;

      if (Mode == ApplicationMode.Slideshow)
      {
        CurrentSlideshowTime = 1;
      }
      else
      {
        Mode = ApplicationMode.Normal;
      }

      switch (switchDirection)
      {
        case SwitchDirection.Next:
        {
          if (Index < Paths.Count - 1)
          {
            Index += 1;
          }
          else
          {
            Index = 0;
          }

          break;
        }

        case SwitchDirection.Previous:
        {
          if (Paths.Any())
          {
            if (Index > 0)
            {
              Index -= 1;
            }
            else
            {
              Index = Paths.Count - 1;
            }
          }

          break;
        }
      }
    }

    void Footer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      collapseFooterText = Footer.RenderSize.Width < 620;
      UpdateFooter();
    }

    void ImagePresenterOnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      ParentMainWindow.ImageAreaKeyDown(sender, e);
    }
  }
}