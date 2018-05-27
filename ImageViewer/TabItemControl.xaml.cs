using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Frame.Annotations;
using Frame.Properties;
using ImageMagick;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
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

    //TODO Change this into a field(should be faster) and move this into the LoadImage function.
    public BitmapSource Image
    {
      get
      {
        if (ImageSettings.IsGif)
        {
          var image = ImageSettings.ImageCollection[ImageSettings.CurrentFrame];
          gifTimer.Interval = ImageSettings.ImageCollection[ImageSettings.CurrentFrame].AnimationDelay == 0
            ? 1
            : ImageSettings.ImageCollection[ImageSettings.CurrentFrame].AnimationDelay * 10;
          return image.ToBitmapSource();
        }

        LoadImage();

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
        switch (ImageSettings.DisplayChannel)
        {
          case Channels.Red:
          {
            var magickImage = ResizeCurrentMip();
            return magickImage.Separate(Channels.Red)
                              .ElementAt(0)?.ToBitmapSource();
          }
          case Channels.Green:
          {
            var magickImage = ResizeCurrentMip();
            return magickImage.Separate(Channels.Green)
                              .ElementAt(0)?.ToBitmapSource();
          }
          case Channels.Blue:
          {
            var magickImage = ResizeCurrentMip();
            return magickImage.Separate(Channels.Blue)
                              .ElementAt(0)?.ToBitmapSource();
          }
          case Channels.Alpha:
          {
            var magickImage = ResizeCurrentMip();
            return magickImage.Separate(Channels.Alpha)
                              .ElementAt(0)?.ToBitmapSource();
          }
          default:
          {
            var magickImage = ResizeCurrentMip();
            magickImage.Alpha(AlphaOption.Opaque);
            return magickImage.ToBitmapSource();
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

    uint                                currentSlideshowTime;
    bool                                firstImageLoaded;
    public readonly System.Timers.Timer gifTimer;
    bool                                _collapseFooterText;
    MainWindow                          ParentMainWindow => Window.GetWindow(this) as MainWindow;

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

    Point? lastCenterPositionOnTarget;
    Point? lastMousePositionOnTarget;
    Point? lastDragPoint;
    double zoom = 100;

    public double Zoom
    {
      get { return zoom; }
      set
      {
        OnZoomChanged(this, new RoutedPropertyChangedEventArgs<double>(zoom, value));
        UpdateFooter();
        zoom = value;
      }
    }

    public TabItemControl()
    {
      Margin = new Thickness(0.5);
      InitializeComponent();

      ImageAreaScrollViewwer.ScrollChanged            += OnImageAreaScrollViewwerScrollChanged;
      ImageAreaScrollViewwer.MouseLeftButtonUp        += OnMouseLeftButtonUp;
      ImageAreaScrollViewwer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
      ImageAreaScrollViewwer.PreviewMouseWheel        += OnPreviewMouseWheel;

      ImageAreaScrollViewwer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
      ImageAreaScrollViewwer.MouseMove                  += OnMouseMove;

      ImageAreaScrollViewwer.GotFocus  += WinFormsHostOnGotFocus;
      ImageAreaScrollViewwer.LostFocus += WinFormsHostOnLostFocus;

      gifTimer         =  new System.Timers.Timer();
      gifTimer.Elapsed += GifAnim;

      ImageSettings.PropertyChanged += (sender, args) =>
      {
        UpdateFooter();
        UpdateTitle();
      };
      PropertyChanged   += OnPropertyChanged;
      ImageArea.KeyDown += (sender, args) => { ParentMainWindow.ImageAreaKeyDown(sender, args); };
      ImageArea.Loaded += (sender, args) =>
      {
        grid.Width  = Image.Width;
        grid.Height = Image.Height;
        if (firstImageLoaded) return;
        ResetView();
        firstImageLoaded = true;
      };
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
      if (lastDragPoint.HasValue)
      {
        var posNow = e.GetPosition(ImageAreaScrollViewwer);

        var dX = posNow.X - lastDragPoint.Value.X;
        var dY = posNow.Y - lastDragPoint.Value.Y;

        lastDragPoint = posNow;

        ImageAreaScrollViewwer.ScrollToHorizontalOffset(ImageAreaScrollViewwer.HorizontalOffset - dX);
        ImageAreaScrollViewwer.ScrollToVerticalOffset(ImageAreaScrollViewwer.VerticalOffset - dY);
      }
    }

    void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var mousePos = e.GetPosition(ImageAreaScrollViewwer);
      if (mousePos.X <= ImageAreaScrollViewwer.ViewportWidth && mousePos.Y <
          ImageAreaScrollViewwer.ViewportHeight)
      {
        ImageAreaScrollViewwer.Cursor = Cursors.SizeAll;
        lastDragPoint                 = mousePos;
        Mouse.Capture(ImageAreaScrollViewwer);
      }
    }

    void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
      lastMousePositionOnTarget = Mouse.GetPosition(grid);

      if (e.Delta > 0)
      {
        Zoom += 10 * (Zoom / 100);
      }

      if (e.Delta < 0)
      {
        Zoom -= 10 * (Zoom / 100);
      }

      e.Handled = true;
    }

    void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      ImageAreaScrollViewwer.Cursor = Cursors.Arrow;
      ImageAreaScrollViewwer.ReleaseMouseCapture();
      lastDragPoint = null;
    }

    void OnZoomChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      scaleTransform.ScaleX = e.NewValue / 100;
      scaleTransform.ScaleY = e.NewValue / 100;

      var centerOfViewport = new Point(ImageAreaScrollViewwer.ViewportWidth / 2,
                                       ImageAreaScrollViewwer.ViewportHeight / 2);
      lastCenterPositionOnTarget = ImageAreaScrollViewwer.TranslatePoint(centerOfViewport, grid);
    }

    void OnImageAreaScrollViewwerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
      {
        Point? targetBefore = null;
        Point? targetNow    = null;

        if (!lastMousePositionOnTarget.HasValue)
        {
          if (lastCenterPositionOnTarget.HasValue)
          {
            var centerOfViewport = new Point(ImageAreaScrollViewwer.ViewportWidth / 2,
                                             ImageAreaScrollViewwer.ViewportHeight / 2);
            Point centerOfTargetNow =
              ImageAreaScrollViewwer.TranslatePoint(centerOfViewport, grid);

            targetBefore = lastCenterPositionOnTarget;
            targetNow    = centerOfTargetNow;
          }
        }
        else
        {
          targetBefore = lastMousePositionOnTarget;
          targetNow    = Mouse.GetPosition(grid);

          lastMousePositionOnTarget = null;
        }

        if (targetBefore.HasValue)
        {
          double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
          double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

          double multiplicatorX = e.ExtentWidth / grid.Width;
          double multiplicatorY = e.ExtentHeight / grid.Height;

          double newOffsetX = ImageAreaScrollViewwer.HorizontalOffset -
                              dXInTargetPixels * multiplicatorX;
          double newOffsetY = ImageAreaScrollViewwer.VerticalOffset -
                              dYInTargetPixels * multiplicatorY;

          if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
          {
            return;
          }

          ImageAreaScrollViewwer.ScrollToHorizontalOffset(newOffsetX);
          ImageAreaScrollViewwer.ScrollToVerticalOffset(newOffsetY);
        }
      }
    }

    void WinFormsHostOnLostFocus(object o, RoutedEventArgs routedEventArgs) { }

    void WinFormsHostOnGotFocus(object o, RoutedEventArgs routedEventArgs) { }

    void OnPropertyChanged(object o, PropertyChangedEventArgs args)
    {
      if (args.PropertyName == nameof(Index))
      {
        Application.Current?.Dispatcher.Invoke(() => { ParentMainWindow.RefreshImage(); });
        ResetView();
      }

      UpdateFooter();
      UpdateTitle();
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
          if (!_collapseFooterText)
          {
            return $"MODE: {Mode} " + CurrentSlideshowTime;
          }

          return $"{Mode} " + CurrentSlideshowTime;
        }

        if (!_collapseFooterText)
        {
          return $"MODE: {Mode}";
        }

        return $"{Mode}";
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

        if (!_collapseFooterText)
        {
          return $"SIZE: {ImageSettings.Width}x{ImageSettings.Height}";
        }

        return $"{ImageSettings.Width}x{ImageSettings.Height}";
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

        if (!_collapseFooterText)
        {
          return $"CHANNELS: {channel}";
        }

        return $"{channel}";
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
            if (!_collapseFooterText)
            {
              return $"FILESIZE: {ImageSettings.Size}Bytes";
            }

            return $"{ImageSettings.Size}Bytes";
          }

          if (ImageSettings.Size < 1048576)
          {
            var filesize = (double) (ImageSettings.Size / 1024f);
            if (!_collapseFooterText)
            {
              return $"FILESIZE: {filesize:N2}KB";
            }

            return $"{filesize:N2}KB";
          }
          else
          {
            var filesize = (double) (ImageSettings.Size / 1024f) / 1024f;
            if (!_collapseFooterText)
            {
              return $"FILESIZE: {filesize:N2}MB";
            }

            return $"{filesize:N2}MB";
          }
        }
        catch (FileNotFoundException)
        {
          return "FILESIZE: ";
        }
      }
    }

    string FooterZoomTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "ZOOM: ";
        }

        if (!_collapseFooterText)
        {
          return $"ZOOM: {Zoom:N2}%";
        }

        return $"{Zoom:N2}%";
      }
    }

    string FooterIndexTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "INDEX: ";
        }

        if (!_collapseFooterText)
        {
          return $"INDEX: {Index + 1}/{Paths.Count}";
        }

        return $"{Index + 1}/{Paths.Count}";
      }
    }

    string FooterMipIndexTextP
    {
      get
      {
        if (!ImageSettings.ImageCollection.Any())
        {
          return "MIP: ";
        }

        if (ImageSettings.HasMips)
        {
          if (!_collapseFooterText)
          {
            return $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
          }

          return $"{ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
        }

        return !_collapseFooterText ? "MIP: None" : "None";
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
        Zoom = 100;
        return;
      }

      if (grid.Width < ImageSettings.Width ||
          grid.Height < ImageSettings.Height)
      {
        ZoomToFit();
      }
      else
      {
        Zoom = 100;
      }

      ParentMainWindow.Focus();
    }

    void ZoomToFit()
    {
//      throw new NotImplementedException();
    }

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
            ImageSettings.ImageCollection = new MagickImageCollection(Path);
            ImageSettings.ImageCollection.Coalesce();
            if (!gifTimer.Enabled)
            {
              gifTimer.Start();
              gifTimer.Interval =
                ImageSettings.ImageCollection[ImageSettings.CurrentFrame].AnimationDelay;
            }

            ImageSettings.IsGif    = true;
            ImageSettings.HasMips  = false;
            ImageSettings.EndFrame = ImageSettings.ImageCollection.Count - 1;
            ImageSettings.MipValue = 0;
            break;
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
              new MagickImage(Path)
            };
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

      Application.Current?.Dispatcher.Invoke(() => { ParentMainWindow.RefreshImage(); });
    }

    void ImageAreaOnMouseDown(object sender, MouseEventArgs e)
    {
      if (e.RightButton != MouseButtonState.Pressed)
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
        CurrentSlideshowTime = 1;

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

      GC.Collect();
      GC.WaitForPendingFinalizers();
    }

    void Footer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (Footer.RenderSize.Width < 620)
      {
        _collapseFooterText = true;
      }
      else
      {
        _collapseFooterText = false;
      }

      UpdateFooter();
    }

    void ImageAreaScrollViewwer_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
      ParentMainWindow.ImageAreaKeyDown(sender, e);
    }
  }
}