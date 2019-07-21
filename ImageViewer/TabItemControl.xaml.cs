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

namespace Frame {
    public partial class TabItemControl : IDisposable, INotifyPropertyChanged {
        const int TileCount = 8;

        public ImageSettings ImageSettings;

        FooterMode FooterMode;

        ApplicationMode mode;

        public ApplicationMode CurrentMode {
            get => mode;
            set {
                mode = value;
                OnPropertyChanged();
            }
        }

        void UpdateFooter() {
            FooterModeText.Text = GetFooterModeText();
            FooterSizeText.Text = GetFooterSizeText();
            FooterChannelsText.Text = GetFooterChannelsText();
            FooterFilesizeText.Text = GetFooterFilesizeText();
            FooterZoomText.Text = GetFooterZoomText();
            FooterIndexText.Text = GetFooterIndexText();
            FooterMipIndexText.Text = GetFooterMipIndexText();
        }

        uint currentSlideshowTime;

        public uint CurrentSlideshowTime {
            get => currentSlideshowTime;
            set {
                currentSlideshowTime = value;
                OnPropertyChanged();
            }
        }

        public string InitialImagePath;

        uint index;

        public uint Index {
            get => index;
            set {
                if (index != value || ImageSettings.Size == 0) {
                    index = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> Paths;

        IMagickImage ResizeCurrentMip() {
            var MagickImage = ImageSettings.ImageCollection[ImageSettings.MipValue];
            if (ImageSettings.MipValue > 0) {
                MagickImage.Resize(ImageSettings.ImageCollection[0].Width, ImageSettings.ImageCollection[0].Height);
            }

            return MagickImage;
        }

        public string Path => Index < Paths.Count ? Paths[(int)Index] : Paths[0];

        string Filename => new FileInfo(Paths[(int)Index]).Name;

        bool HasFirstImageLoaded;

        MainWindow ParentMainWindow {
            get {
                return (MainWindow)Window.GetWindow(this);
            }
        }

        public TabItemControl() {
            Margin = new Thickness(0.5);
            InitializeComponent();

            ImageSettings = new ImageSettings();
            Paths = new List<string>();

            Footer.SizeChanged += footerOnSizeChanged;
            ImagePresenter.PreviewKeyDown += imagePresenterOnPreviewKeyDown;
            ImagePresenter.PropertyChanged += presenterProperyChanged;
            ImageSettings.PropertyChanged += settingsProperyChanged;
            PropertyChanged += onPropertyChanged;
            ImagePresenter.ImageArea.KeyDown += imageAreaKeyDown;
            ImagePresenter.ImageArea.Loaded += imageAreaLoaded;
        }

        private void imageAreaKeyDown(object sender, KeyEventArgs e) {
            if (ParentMainWindow != null) {
                ParentMainWindow.ImageAreaKeyDown(sender, e);
            }
        }

        private void presenterProperyChanged(object sender, PropertyChangedEventArgs e) {
            UpdateFooter();
        }

        private void settingsProperyChanged(object sender, PropertyChangedEventArgs e) {
            UpdateFooter();
            Header = Filename;
        }

        private void imageAreaLoaded(object sender, RoutedEventArgs e) {
            ImagePresenter.Grid.Width = ImageSettings.Width;
            ImagePresenter.Grid.Height = ImageSettings.Height;
            if (HasFirstImageLoaded) {
                return;
            }
            ResetView();
            HasFirstImageLoaded = true;
        }

        void onPropertyChanged(object o, PropertyChangedEventArgs args) {
            if (args.PropertyName == nameof(Index)) {
                Application.Current?.Dispatcher.Invoke(() => { ParentMainWindow.RefreshImage(); });
                ResetView();
            }

            UpdateFooter();
            Header = Filename;
        }

        string GetFooterModeText() {
            if (CurrentMode == ApplicationMode.Slideshow) {
                if (FooterMode == FooterMode.Visible) {
                    return $"MODE: {CurrentMode} " + CurrentSlideshowTime;
                }
                else {
                    return $"{CurrentMode} " + CurrentSlideshowTime;
                }
            }
            else {
                if (FooterMode == FooterMode.Visible) {
                    return $"MODE: {CurrentMode}";
                }
                else {
                    return $"{CurrentMode}";
                }
            }
        }

        string GetFooterSizeText() {
            if (FooterMode == FooterMode.Visible) {
                return $"SIZE: {ImageSettings.Width}x{ImageSettings.Height}";
            }
            else {
                return $"{ImageSettings.Width}x{ImageSettings.Height}";
            }
        }

        string GetFooterChannelsText() {
            var channel = string.Empty;
            switch (ImageSettings.DisplayChannel) {
                case (Channels.RGB):
                case (Channels.Green):
                case (Channels.Blue): {
                    channel = ImageSettings.DisplayChannel.ToString();
                    break;
                }
                case (Channels.Red): {
                    channel = "Red";
                    break;
                }
                case (Channels.Opacity): {
                    channel = "Alpha";
                    break;
                }
            }

            return FooterMode == FooterMode.Visible ? $"CHANNELS: {channel}" : $"{channel}";
        }

        string GetFooterFilesizeText() {
            try {
                if (ImageSettings.Size == 0) {
                    return "FILESIZE: ";
                }

                if (ImageSettings.Size < 1024) {
                    return FooterMode == FooterMode.Visible
                      ? $"FILESIZE: {ImageSettings.Size}Bytes"
                      : $"{ImageSettings.Size}Bytes";
                }

                if (ImageSettings.Size < 1048576) {
                    var filesize = (double)(ImageSettings.Size / 1024f);
                    return FooterMode == FooterMode.Visible ? $"FILESIZE: {filesize:N2}KB" : $"{filesize:N2}KB";
                }
                else {
                    var filesize = (double)(ImageSettings.Size / 1024f) / 1024f;
                    return FooterMode == FooterMode.Visible ? $"FILESIZE: {filesize:N2}MB" : $"{filesize:N2}MB";
                }
            }
            catch (FileNotFoundException) {
                return "FILESIZE: ";
            }
        }

        string GetFooterZoomText() {
            if (FooterMode == FooterMode.Visible) {
                return $"ZOOM: {ImagePresenter.Zoom:N2}%";
            }
            else {
                return $"{ImagePresenter.Zoom:N2}%";
            }
        }

        string GetFooterIndexText() {
            if (FooterMode == FooterMode.Visible) {
                return $"INDEX: {Index + 1}/{Paths.Count}";
            }
            else {
                return $"{Index + 1}/{Paths.Count}";
            }
        }

        string GetFooterMipIndexText() {
            if (ImageSettings.HasMips) {
                if (FooterMode == FooterMode.Visible) {
                    return $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
                }
                else {
                    return $"{ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
                }
            }
            else {
                if (FooterMode == FooterMode.Visible) {
                    return "MIP: None";
                }
                else {
                    return "None";
                }
            }
        }

        public void ResetView() {
            ParentMainWindow.Focus();

            if (ImagePresenter.ImageArea == null) {
                return;
            }

            ImagePresenter.Grid.Width = ImageSettings.Width;
            ImagePresenter.Grid.Height = ImageSettings.Height;

            ImagePresenter.Zoom = 100;

            if (Settings.Default.ImageFullZoom) {
                return;
            }

            if (ImagePresenter.ActualWidth < ImageSettings.Width ||
                ImagePresenter.ActualHeight < ImageSettings.Height) {
                ZoomToFit();
            }
        }

        void ZoomToFit() {
            while (ImagePresenter.Grid.Width * ImagePresenter.ScaleTransform.ScaleX <
                   ImagePresenter.ActualWidth ||
                   ImagePresenter.Grid.Height * ImagePresenter.ScaleTransform.ScaleX <
                   ImagePresenter.ActualHeight) {
                ImagePresenter.Zoom++;
            }

            while (ImagePresenter.Grid.Width * ImagePresenter.ScaleTransform.ScaleX >
                   ImagePresenter.ActualWidth ||
                   ImagePresenter.Grid.Height * ImagePresenter.ScaleTransform.ScaleX >
                   ImagePresenter.ActualHeight) {
                ImagePresenter.Zoom--;
            }
        }

        public bool IsTiled;
        public bool UsesChannelsMontage;

        static MagickImage ErrorImage(string filepath) {
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

        internal void LoadImage() {
            // Notes
            // Channel Splitting, tiling or switching mip levels I load the picture again
            // When channel splitting it loads the image twice.
            try {
                switch (System.IO.Path.GetExtension(Path)) {
                    case ".gif": {
                        ImagePresenter.ImageArea.LoadGif(Path, GifMode.Animated);
                        ImagePresenter.ImageArea.StartAnimate();
                        ImageSettings.Width = (int)ImagePresenter.ImageArea.Width;
                        ImageSettings.Height = (int)ImagePresenter.ImageArea.Height;
                        break;
                    }
                    case ".dds": {
                        var defines = new DdsReadDefines { SkipMipmaps = false };
                        var readSettings = new MagickReadSettings(defines);
                        ImageSettings.ImageCollection = new MagickImageCollection(Path, readSettings);
                        ImageSettings.HasMips = ImageSettings.ImageCollection.Count > 1;
                        if (ImageSettings.HasMips) {
                            ImageSettings.MipCount = ImageSettings.ImageCollection.Count;
                        }
                        break;
                    }
                    default: {
                        ImageSettings.HasMips = false;
                        ImageSettings.MipValue = 0;
                        ImageSettings.ImageCollection = new MagickImageCollection { Path };
                        break;
                    }
                }
            }
            catch (Exception) {
                ImageSettings.ImageCollection.Clear();
                ImageSettings.ImageCollection.Add(ErrorImage(Path));
            }

            if (ImageSettings.ImageCollection == null) {
                return;
            };

            var width = ImageSettings.Width;
            var height = ImageSettings.Height;
            var border_width = (int)Math.Max(2.0, (width * height) / 200000.0);

            var channel_num = 0;
            var settings_image = ImageSettings.ImageCollection[ImageSettings.MipValue];
            if (UsesChannelsMontage) {
                using (var orginal_image = ImageSettings.ImageCollection[0]) {
                    using (var images = new MagickImageCollection()) {
                        foreach (var channel in settings_image.Separate()) {
                            if (Settings.Default.SplitChannelsBorder) {
                                switch (channel_num) {
                                    case 0: {
                                        channel.BorderColor = MagickColor.FromRgb(255, 0, 0);
                                        break;
                                    }
                                    case 1: {
                                        channel.BorderColor = MagickColor.FromRgb(0, 255, 0);
                                        break;
                                    }
                                    case 2: {
                                        channel.BorderColor = MagickColor.FromRgb(0, 0, 255);
                                        break;
                                    }
                                }

                                channel.Border(border_width);
                                channel_num++;
                            }

                            if (ImageSettings.MipValue > 0) {
                                channel.Resize(orginal_image.Width, orginal_image.Height);
                            }

                            images.Add(channel);
                        }

                        var MontageSettings =
                          new MontageSettings {
                              Geometry = new MagickGeometry(width, height)
                          };
                        var Result = images.Montage(MontageSettings);
                        ImageSettings.ImageCollection.Clear();
                        ImageSettings.ImageCollection.Add(Result);
                    }
                }

                ImageSettings.HasMips = false;
            }

            if (IsTiled) {
                using (var Images = new MagickImageCollection()) {
                    var OrginalImage = ImageSettings.ImageCollection[0];

                    for (var i = 0; i <= TileCount; i++) {
                        var Image = settings_image.Clone();
                        if (ImageSettings.MipValue > 0) {
                            Image.Resize(OrginalImage.Width, OrginalImage.Height);
                        }

                        if (ImageSettings.DisplayChannel != Channels.Alpha) {
                            Image.Alpha(AlphaOption.Opaque);
                        }

                        Images.Add(Image);
                    }

                    var MontageSettings =
                      new MontageSettings {
                          Geometry = new MagickGeometry(width, height),
                      };
                    ImageSettings.ImageCollection.Clear();
                    ImageSettings.ImageCollection.Add(Images.Montage(MontageSettings));
                }
                ImageSettings.HasMips = false;
                ImageSettings.Size *= TileCount + 1;
            }

            OnPropertyChanged();
            var magick_image = ResizeCurrentMip();
            switch (ImageSettings.DisplayChannel) {
                case Channels.RGB: {
                    magick_image.Alpha(AlphaOption.Opaque);
                    ImagePresenter.ImageArea.Source = magick_image.ToBitmapSource();
                    break;
                }
                case Channels.Red: {
                    ImagePresenter.ImageArea.Source = magick_image.Separate(Channels.Red)
                                                                 .ElementAt(0)?.ToBitmapSource();
                    break;
                }
                case Channels.Green: {
                    ImagePresenter.ImageArea.Source = magick_image.Separate(Channels.Green)
                                                                 .ElementAt(0)?.ToBitmapSource();
                    break;
                }
                case Channels.Blue: {
                    ImagePresenter.ImageArea.Source = magick_image.Separate(Channels.Blue)
                                                                 .ElementAt(0)?.ToBitmapSource();
                    break;
                }
                case Channels.Alpha: {
                    ImagePresenter.ImageArea.Source = magick_image.Separate(Channels.Alpha)
                                                                 .ElementAt(0)?.ToBitmapSource();
                    break;
                }
                default: {
                    magick_image.Alpha(AlphaOption.Opaque);
                    ImagePresenter.ImageArea.Source = magick_image.ToBitmapSource();
                    break;
                }
            }
        }

        void Dispose(bool disposing) {
            if (!disposing) {
                return;
            }

            Footer.SizeChanged -= footerOnSizeChanged;
            ImagePresenter.PreviewKeyDown -= imagePresenterOnPreviewKeyDown;
            ImagePresenter.PropertyChanged -= presenterProperyChanged;
            ImageSettings.PropertyChanged -= settingsProperyChanged;
            PropertyChanged -= onPropertyChanged;
            ImagePresenter.ImageArea.KeyDown -= imageAreaKeyDown;
            ImagePresenter.ImageArea.Loaded -= imageAreaLoaded;

            if (ImageSettings != null) {
                ImageSettings.Dispose();
                ImageSettings = null;
            }

            if (Paths != null) {
                Paths.Clear();
                Paths = null;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        ~TabItemControl() {
            Dispose(false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SwitchImage(SwitchDirection switchDirection) {
            ImageSettings.MipValue = 0;
            ImagePresenter.ImageArea.StopAnimate();
            IsTiled = false;
            UsesChannelsMontage = false;

            if (CurrentMode == ApplicationMode.Slideshow) {
                CurrentSlideshowTime = 1;
            }
            else {
                CurrentMode = ApplicationMode.Normal;
            }

            switch (switchDirection) {
                case SwitchDirection.Next: {
                    if (Index < Paths.Count - 1) {
                        Index++;
                    }
                    else {
                        Index = 0;
                    }
                    break;
                }

                case SwitchDirection.Previous: {
                    if (Paths.Count > 0) {
                        if (Index > 0) {
                            Index--;
                        }
                        else {
                            Index = (uint)(Paths.Count - 1);
                        }
                    }
                    break;
                }
            }
        }

        void footerOnSizeChanged(object sender, SizeChangedEventArgs e) {
            if (Footer.RenderSize.Width < 620) {
                FooterMode = FooterMode.Collapsed;
            }
            else {
                FooterMode = FooterMode.Visible;
            }
            UpdateFooter();
        }

        void imagePresenterOnPreviewKeyDown(object sender, KeyEventArgs e) {
            ParentMainWindow.ImageAreaKeyDown(sender, e);
        }
    }
}