using ImageMagick;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.IO.Path;
using Color = System.Windows.Media.Color;
using Image = System.Drawing.Image;
using TextAlignment = ImageMagick.TextAlignment;

namespace Frame
{
    public sealed class TabData : IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public readonly TabItem tabItem = TabItem();
        const double Margin = 0.5;

        public ImageSettings ImageSettings { get; private set; } = new ImageSettings();

        // INotifyPropertyChanged so I can update the header without having to call UpdateTitle() explicitly.
        public ApplicationMode Mode
        {
            get => mode;
            set
            {
                if (value == mode) return;
                mode = value;
                NotifyPropertyChanged();
            }
        }

        public uint CurrentSlideshowTime { get; set; }
        public string InitialImagePath { get; set; }

        public int Index
        {
            get => index;
            set
            {
                if (value == index) return;
                index = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> Paths { get; set; } = new List<string>();

        public bool IsValid => Paths.Any();

        ApplicationMode mode = ApplicationMode.Normal;
        int index;
        static readonly Color AlmostWhite = Color.FromRgb(240, 240, 240);

        public Image Image
        {
            get
            {
                GC.Collect();
                //TODO Make it so I don't have to reload the image, when doing tiling and channels montage, or switching channels.
                if (!Hibernate)
                {
                    LoadImage();

                    var borderWidth = (int)Math.Max(2.0, (ImageSettings.Width * ImageSettings.Height) / 200000.0);
                    var channelNum = 0;
                    if (ChannelsMontage)
                    {
                        using (var orginalImage = ImageSettings.ImageCollection[0])
                        {
                            using (var images = new MagickImageCollection())
                            {
                                foreach (var img in ImageSettings.ImageCollection[ImageSettings.MipValue].Separate())
                                {
                                    if (Properties.Settings.Default.SplitChannelsBorder)
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
                        var images = new MagickImageCollection();
                        const int tileCount = 8;
                        var orginalImage = ImageSettings.ImageCollection[0];
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
                }
                Hibernate = false;

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

        public bool Hibernate { private get; set; }

        public string Path => Index < Paths.Count ? Paths[Index] : Paths[0];

        string Title
        {
            set => ((TextBlock) ((StackPanel) tabItem.Header).Children[0]).Text = value;
            get => ((TextBlock) ((StackPanel) tabItem.Header).Children[0]).Text;
        }

        string Filename => new System.IO.FileInfo(Paths[Index]).Name;

        static TabItem TabItem()
        {


            var tabInternalControl = new StackPanel {Orientation = Orientation.Horizontal};
            tabInternalControl.Children.Add(new TextBlock());

            return new TabItem
            {
                Header = tabInternalControl,
                IsTabStop = false,
                FocusVisualStyle = null,
                Margin = new Thickness(Margin),
                Foreground = new SolidColorBrush(AlmostWhite),
            };
        }

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

        public string FooterIndex => $"INDEX: {Index + 1}/{Paths.Count}";
        public bool Tiled { get; set; }
        public bool ChannelsMontage { get; set; }

        public string FooterMipIndex => ImageSettings.HasMips ? $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}" : "MIP: None";

        TabData(string tabPath)
        {
            InitialImagePath = tabPath;
        }


        TabData(string tabPath, int currentIndex) : this(tabPath)
        {
            Index = currentIndex;
        }

        public static TabData CreateTabData(TabData tb)
        {
            return new TabData(GetDirectoryName(tb.Path), tb.Index)
            {
                InitialImagePath = tb.InitialImagePath,
                Paths = tb.Paths,
                ImageSettings = new ImageSettings
                {
                    DisplayChannel = tb.ImageSettings.DisplayChannel,
                    SortMode = tb.ImageSettings.SortMode
                }
            };
        }

        public static TabData CreateTabData(string path)
        {
            return new TabData(path);
        }

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
                .Text(256, 256, $"Could not load\n{GetFileName(filepath)}")
                .Draw(image);

            return image;
        }

        void LoadImage()
        {
            try
            {
                switch (GetExtension(Path))
                {
                    case ".gif":
                    {
                        ImageSettings.HasMips = false;
                        ImageSettings.MipValue = 0;
                        ImageSettings.ImageCollection.Clear();
                        ImageSettings.ImageCollection.Add(Path);
                        break;
                    }
                    case ".dds":
                    {
                        var defines = new DdsReadDefines {SkipMipmaps = false};
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
            finally
            {
                GC.Collect();
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
    }
}