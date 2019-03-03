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
        const int TileCount = 8;

        public readonly ImageSettings ImageSettings;

        FooterMode CurrentFooterMode;

        ApplicationMode mode;

        public ApplicationMode CurrentMode
        {
            get => mode;
            set
            {
                mode = value;
                OnPropertyChanged();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateFooter()
        {
            FooterModeText.Text = FooterModeTextP;
            FooterSizeText.Text = FooterSizeTextP;
            FooterChannelsText.Text = FooterChannelsTextP;
            FooterFilesizeText.Text = FooterFilesizeTextP;
            FooterZoomText.Text = FooterZoomTextP;
            FooterIndexText.Text = FooterIndexTextP;
            FooterMipIndexText.Text = FooterMipIndexTextP;
        }

        uint currentSlideshowTime;

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

        uint index;

        public uint Index
        {
            get => index;
            set
            {
                if(index != value || ImageSettings.Size == 0)
                {
                    index = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> Paths;

        IMagickImage ResizeCurrentMip()
        {
            var MagickImage = ImageSettings.ImageCollection[ImageSettings.MipValue];
            if (ImageSettings.MipValue > 0)
            {
                MagickImage.Resize(ImageSettings.ImageCollection[0].Width, ImageSettings.ImageCollection[0].Height);
            }

            return MagickImage;
        }

        public string Path => Index < Paths.Count ? Paths[(int)Index] : Paths[0];

        string Filename => new FileInfo(Paths[(int)Index]).Name;

        bool HasFirstImageLoaded;

        MainWindow ParentMainWindow =>
          Dispatcher.Invoke(() => (MainWindow)Window.GetWindow(this));

        public TabItemControl()
        {
            Margin = new Thickness(0.5);
            InitializeComponent();

            ImageSettings = new ImageSettings();
            Paths = new List<string>();

            ImagePresenter.PreviewKeyDown += ImagePresenterOnPreviewKeyDown;

            ImagePresenter.PropertyChanged += (sender, args) => { UpdateFooter(); };
            ImageSettings.PropertyChanged += (sender, args) =>
            {
                UpdateFooter();
                Header = Filename;
            };
            PropertyChanged += OnPropertyChanged;
            ImagePresenter.ImageArea.KeyDown += (sender, args) => { ParentMainWindow.ImageAreaKeyDown(sender, args); };
            ImagePresenter.ImageArea.Loaded += (sender, args) =>
            {
                ImagePresenter.Grid.Width = ImageSettings.Width;
                ImagePresenter.Grid.Height = ImageSettings.Height;
                if (HasFirstImageLoaded)
                {
                    return;
                }
                ResetView();
                HasFirstImageLoaded = true;
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
                if (CurrentMode == ApplicationMode.Slideshow)
                {
                    if (CurrentFooterMode == FooterMode.Visible)
                    {
                        return $"MODE: {CurrentMode} " + CurrentSlideshowTime;
                    }

                    return $"{CurrentMode} " + CurrentSlideshowTime;
                }

                return CurrentFooterMode == FooterMode.Visible ? $"MODE: {CurrentMode}" : $"{CurrentMode}";
            }
        }

        string FooterSizeTextP => CurrentFooterMode == FooterMode.Visible
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
                    case (Channels.Green):
                    case (Channels.Blue):
                        {
                            channel = ImageSettings.DisplayChannel.ToString();
                            break;
                        }
                    case (Channels.Red):
                        {
                            channel = "Red";
                            break;
                        }
                    case (Channels.Opacity):
                        {
                            channel = "Alpha";
                            break;
                        }
                }

                return CurrentFooterMode == FooterMode.Visible ? $"CHANNELS: {channel}" : $"{channel}";
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
                        return CurrentFooterMode == FooterMode.Visible
                          ? $"FILESIZE: {ImageSettings.Size}Bytes"
                          : $"{ImageSettings.Size}Bytes";
                    }

                    if (ImageSettings.Size < 1048576)
                    {
                        var filesize = (double)(ImageSettings.Size / 1024f);
                        return CurrentFooterMode == FooterMode.Visible ? $"FILESIZE: {filesize:N2}KB" : $"{filesize:N2}KB";
                    }
                    else
                    {
                        var filesize = (double)(ImageSettings.Size / 1024f) / 1024f;
                        return CurrentFooterMode == FooterMode.Visible ? $"FILESIZE: {filesize:N2}MB" : $"{filesize:N2}MB";
                    }
                }
                catch (FileNotFoundException)
                {
                    return "FILESIZE: ";
                }
            }
        }

        string FooterZoomTextP =>
          CurrentFooterMode == FooterMode.Visible ? $"ZOOM: {ImagePresenter.Zoom:N2}%" : $"{ImagePresenter.Zoom:N2}%";

        string FooterIndexTextP =>
          CurrentFooterMode == FooterMode.Visible ? $"INDEX: {Index + 1}/{Paths.Count}" : $"{Index + 1}/{Paths.Count}";

        string FooterMipIndexTextP
        {
            get
            {
                if (ImageSettings.HasMips)
                {
                    return CurrentFooterMode == FooterMode.Visible
                      ? $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}"
                      : $"{ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
                }

                return CurrentFooterMode == FooterMode.Visible ? "MIP: None" : "None";
            }
        }

        public void ResetView()
        {
            ParentMainWindow.Focus();

            if (ImagePresenter.ImageArea == null)
            {
                return;
            }

            ImagePresenter.Grid.Width = ImageSettings.Width;
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
                ImagePresenter.Zoom++;
            }

            while (ImagePresenter.Grid.Width * ImagePresenter.ScaleTransform.ScaleX >
                   ImagePresenter.ActualWidth ||
                   ImagePresenter.Grid.Height * ImagePresenter.ScaleTransform.ScaleX >
                   ImagePresenter.ActualHeight)
            {
                ImagePresenter.Zoom--;
            }
        }

        public bool IsTiled;
        public bool UsesChannelsMontage;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void LoadImage()
        {
            try
            {
                switch (System.IO.Path.GetExtension(Path))
                {
                    case ".gif":
                        {
                            ImagePresenter.ImageArea.LoadGif(Path, GifMode.Animated);
                            ImagePresenter.ImageArea.StartAnimate();
                            ImageSettings.Width = (int)ImagePresenter.ImageArea.Width;
                            ImageSettings.Height = (int)ImagePresenter.ImageArea.Height;
                            break;
                        }
                    case ".dds":
                        {
                            var defines = new DdsReadDefines { SkipMipmaps = false };
                            var readSettings = new MagickReadSettings(defines);
                            ImageSettings.ImageCollection = new MagickImageCollection(Path, readSettings);
                            ImageSettings.HasMips = ImageSettings.ImageCollection.Count > 1;
                            if (ImageSettings.HasMips)
                            {
                                ImageSettings.MipCount = ImageSettings.ImageCollection.Count;
                            }

                            break;
                        }
                    default:
                        {
                            ImageSettings.HasMips = false;
                            ImageSettings.MipValue = 0;
                            ImageSettings.ImageCollection = new MagickImageCollection
                            {
                              Path
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

            var ImageWidth = ImageSettings.Width;
            var ImageHeight = ImageSettings.Height;
            var BorderWidth = (int)Math.Max(2.0, (ImageWidth * ImageHeight) / 200000.0);

            if (ImageSettings.ImageCollection == null) return;

            var ChannelNum = 0;
            var SettingsImage = ImageSettings.ImageCollection[ImageSettings.MipValue];
            if (UsesChannelsMontage)
            {
                using (var OrginalImage = ImageSettings.ImageCollection[0])
                {
                    using (var Images = new MagickImageCollection())
                    {
                        foreach (var Channel in SettingsImage.Separate())
                        {
                            if (Settings.Default.SplitChannelsBorder)
                            {
                                switch (ChannelNum)
                                {
                                    case 0:
                                        {
                                            Channel.BorderColor = MagickColor.FromRgb(255, 0, 0);
                                            break;
                                        }
                                    case 1:
                                        {
                                            Channel.BorderColor = MagickColor.FromRgb(0, 255, 0);
                                            break;
                                        }
                                    case 2:
                                        {
                                            Channel.BorderColor = MagickColor.FromRgb(0, 0, 255);
                                            break;
                                        }
                                }

                                Channel.Border(BorderWidth);
                                ChannelNum++;
                            }

                            if (ImageSettings.MipValue > 0)
                            {
                                Channel.Resize(OrginalImage.Width, OrginalImage.Height);
                            }

                            Images.Add(Channel);
                        }

                        var MontageSettings =
                          new MontageSettings
                          {
                              Geometry = new MagickGeometry(ImageWidth, ImageHeight)
                          };
                        var Result = Images.Montage(MontageSettings);
                        ImageSettings.ImageCollection.Clear();
                        ImageSettings.ImageCollection.Add(Result);
                    }
                }

                ImageSettings.HasMips = false;
            }

            if (IsTiled)
            {
                var Images = new MagickImageCollection();
                var OrginalImage = ImageSettings.ImageCollection[0];

                for (var i = 0; i <= TileCount; i++)
                {
                    var Image = SettingsImage.Clone();
                    if (ImageSettings.MipValue > 0)
                    {
                        Image.Resize(OrginalImage.Width, OrginalImage.Height);
                    }

                    if (ImageSettings.DisplayChannel != Channels.Alpha)
                    {
                        Image.Alpha(AlphaOption.Opaque);
                    }

                    Images.Add(Image);
                }

                var MontageSettings =
                  new MontageSettings
                  {
                      Geometry = new MagickGeometry(ImageWidth, ImageHeight),
                  };
                ImageSettings.ImageCollection.Clear();
                ImageSettings.ImageCollection.Add(Images.Montage(MontageSettings));
                ImageSettings.HasMips = false;
                ImageSettings.Size *= TileCount + 1;
            }

            OnPropertyChanged();
            var MagickImage = ResizeCurrentMip();
            switch (ImageSettings.DisplayChannel)
            {
                case Channels.RGB:
                    {
                        MagickImage.Alpha(AlphaOption.Opaque);
                        ImagePresenter.ImageArea.Source = MagickImage.ToBitmapSource();
                        break;
                    }
                case Channels.Red:
                    {
                        ImagePresenter.ImageArea.Source = MagickImage.Separate(Channels.Red)
                                                                     .ElementAt(0)?.ToBitmapSource();
                        break;
                    }
                case Channels.Green:
                    {
                        ImagePresenter.ImageArea.Source = MagickImage.Separate(Channels.Green)
                                                                     .ElementAt(0)?.ToBitmapSource();
                        break;
                    }
                case Channels.Blue:
                    {
                        ImagePresenter.ImageArea.Source = MagickImage.Separate(Channels.Blue)
                                                                     .ElementAt(0)?.ToBitmapSource();
                        break;
                    }
                case Channels.Alpha:
                    {
                        ImagePresenter.ImageArea.Source = MagickImage.Separate(Channels.Alpha)
                                                                     .ElementAt(0)?.ToBitmapSource();
                        break;
                    }
                default:
                    {
                        MagickImage.Alpha(AlphaOption.Opaque);
                        ImagePresenter.ImageArea.Source = MagickImage.ToBitmapSource();
                        break;
                    }
            }
        }

        void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            ImageSettings.Dispose();
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
            ImagePresenter.ImageArea.StopAnimate();
            IsTiled = false;
            UsesChannelsMontage = false;

            if (CurrentMode == ApplicationMode.Slideshow)
            {
                CurrentSlideshowTime = 1;
            }
            else
            {
                CurrentMode = ApplicationMode.Normal;
            }

            switch (switchDirection)
            {
                case SwitchDirection.Next:
                    {
                        if (Index < Paths.Count - 1)
                        {
                            Index++;
                        }
                        else
                        {
                            Index = 0;
                        }

                        break;
                    }

                case SwitchDirection.Previous:
                    {
                        if (Paths.Count > 0)
                        {
                            if (Index > 0)
                            {
                                Index--;
                            }
                            else
                            {
                                Index = (uint)(Paths.Count - 1);
                            }
                        }

                        break;
                    }
            }
        }

        void Footer_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CurrentFooterMode = Footer.RenderSize.Width < 620 ? FooterMode.Collapsed : FooterMode.Visible;
            UpdateFooter();
        }

        void ImagePresenterOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            ParentMainWindow.ImageAreaKeyDown(sender, e);
        }
    }
}