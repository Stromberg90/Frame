using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ImageMagick;
using static System.IO.Path;
using Color = System.Windows.Media.Color;
using Image = System.Drawing.Image;
using TextAlignment = ImageMagick.TextAlignment;

namespace Frame
{
    public enum ApplicationMode
    {
        Normal,
        Slideshow
    }

    public class TabData
    {
        public Action<TabData> CloseTabAction;
        public TabItem tabItem = TabItem();
        const double Margin = 0.5;

        public ImageSettings ImageSettings { get; set; } = new ImageSettings();

        // INotifyPropertyChanged so I can update the header without having to call UpdateTitle() explicitly.
        public ApplicationMode Mode { get; set; } = ApplicationMode.Normal;

        public uint CurrentSlideshowTime { get; set; }
        public string InitialImagePath { get; set; }
        public int Index { get; set; }
        public List<string> Paths { get; set; } = new List<string>();

        public bool IsValid => Paths.Any();

        MagickImageCollection imageCollection = new MagickImageCollection();

        public Image Image
        {
            get
            {
                //TODO Make it so I don't have to reload the image, when doing tiling and channels montage, or switching channels.
                if (!Hibernate)
                {
                    LoadImage();
                }
                Hibernate = false;


                if (SplitChannels)
                {
                    using (var images = new MagickImageCollection())
                    {
                        foreach (var img in imageCollection[ImageSettings.MipValue].Separate())
                        {
                            images.Add(img);
                        }
                        var montageSettings =
                            new MontageSettings
                            {
                                Geometry = new MagickGeometry(Width, Height)
                            };
                        var result = images.Montage(montageSettings);
                        imageCollection.Clear();
                        imageCollection.Add(result);
                    }
                    ImageSettings.HasMips = false;
                }
                if (Tiled)
                {
                    var images = new MagickImageCollection();
                    const int tileCount = 4;
                    for (var i = 0; i < tileCount; i++)
                    {
                        var image = imageCollection[ImageSettings.MipValue].Clone();
                        if (ImageSettings.DisplayChannel != Channels.Alpha)
                        {
                            image.Alpha(AlphaOption.Opaque);
                        }
                        images.Add(image);
                    }
                    var montageSettings =
                        new MontageSettings
                        {
                            Geometry = new MagickGeometry(Width, Height)                            
                        };

                    imageCollection.Clear();
                    imageCollection.Add(images.Montage(montageSettings));
                    ImageSettings.HasMips = false;
                }

                switch (ImageSettings.DisplayChannel)
                {
                    case Channels.Red:
                    {
                        return imageCollection[ImageSettings.MipValue].Separate(Channels.Red)
                            .ElementAt(0)?.ToBitmap();
                    }
                    case Channels.Green:
                    {
                        return imageCollection[ImageSettings.MipValue].Separate(Channels.Green)
                            .ElementAt(0)?.ToBitmap();
                    }
                    case Channels.Blue:
                    {
                        return imageCollection[ImageSettings.MipValue].Separate(Channels.Blue)
                            .ElementAt(0)?.ToBitmap();
                    }
                    case Channels.Alpha:
                    {
                        return imageCollection[ImageSettings.MipValue].Separate(Channels.Alpha)
                            .ElementAt(0)?.ToBitmap();
                    }
                    default:
                    {
                        var image = imageCollection[ImageSettings.MipValue].Clone();
                        image.Alpha(AlphaOption.Opaque);
                        return image.ToBitmap();
                    }
                }
            }
        }

        public bool Hibernate { get; set; }

        public string Path => Index < Paths.Count ? Paths[Index] : Paths[0];

        public string Title
        {
            set => ((TextBlock) ((StackPanel) tabItem.Header).Children[0]).Text = value;
            get => ((TextBlock) ((StackPanel) tabItem.Header).Children[0]).Text;
        }

        public string Filename => new System.IO.FileInfo(Paths[Index]).Name;

        static TabItem TabItem()
        {
            var closeTabButton = new Button
            {
                Content = "-",
                IsTabStop = false,
                Margin = new Thickness(Margin),
                FocusVisualStyle = null,
                Background = new SolidColorBrush(Color.FromArgb(0, 240, 240, 240)),
                Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                BorderThickness = new Thickness(0)
            };
            var tabInternalControl = new StackPanel {Orientation = Orientation.Horizontal};
            tabInternalControl.Children.Add(new TextBlock());
            tabInternalControl.Children.Add(closeTabButton);

            return new TabItem
            {
                Header = tabInternalControl,
                IsTabStop = false,
                FocusVisualStyle = null,
                Margin = new Thickness(Margin),
                Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240))
            };
        }

        public int Width => imageCollection[ImageSettings.MipValue].Width;

        public int Height => imageCollection[ImageSettings.MipValue].Height;

        public long Size => imageCollection[ImageSettings.MipValue].FileSize;

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

        public string FooterSize => $"SIZE: {Width}x{Height}";


        public string FooterFilesize
        {
            get
            {
                if (Size < 1024)
                {
                    return $"FILESIZE: {Size}Bytes";
                }
                if (Size < 1048576)
                {
                    var filesize = (double) (Size / 1024f);
                    return $"FILESIZE: {filesize:N2}KB";
                }
                else
                {
                    var filesize = (double) (Size / 1024f) / 1024f;
                    return $"FILESIZE: {filesize:N2}MB";
                }
            }
        }

        public string FooterIndex => $"INDEX: {Index + 1}/{Paths.Count}";
        public bool Tiled { get; set; }
        public bool SplitChannels { get; set; }

        public string FooterMipIndex
        {
            get
            {
                if (ImageSettings.HasMips)
                {
                    return $"MIP: {ImageSettings.MipValue + 1}/{ImageSettings.MipCount}";
                }
                return "MIP: None";
            }
        }

        public TabData(string tabPath)
        {
            InitialImagePath = tabPath;
            ((Button) ((StackPanel) tabItem.Header).Children[1]).Click += TabData_Click;
            ((StackPanel) tabItem.Header).MouseDown += TabData_MouseDown;
        }

        void TabData_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.MiddleButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                CloseTabAction?.Invoke(this);
            }
        }

        public TabData(string tabPath, int currentIndex) : this(tabPath)
        {
            Index = currentIndex;
        }

        void TabData_Click(object sender, RoutedEventArgs e)
        {
            CloseTabAction?.Invoke(this);
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
                        imageCollection.Clear();
                        imageCollection.Add(Path);
                        break;
                    }
                    case ".dds":
                    {
                        var defines = new DdsReadDefines {SkipMipmaps = false};
                        var readSettings = new MagickReadSettings(defines);
                        imageCollection = new MagickImageCollection(Path, readSettings);
                        ImageSettings.HasMips = imageCollection.Count > 1;
                        if (ImageSettings.HasMips)
                        {
                            ImageSettings.MipCount = imageCollection.Count;
                        }
                        break;
                    }
                    default:
                    {
                        ImageSettings.HasMips = false;
                        ImageSettings.MipValue = 0;
                        imageCollection = new MagickImageCollection(Path);
                        break;
                    }
                }
            }
            catch (MagickCoderErrorException)
            {
                imageCollection.Clear();
                imageCollection.Add(ErrorImage(Path));
            }
            catch (MagickMissingDelegateErrorException)
            {
                imageCollection.Clear();
                imageCollection.Add(ErrorImage(Path));
            }
            catch (MagickCorruptImageErrorException)
            {
                imageCollection.Clear();
                imageCollection.Add(ErrorImage(Path));
            }
            finally
            {
                GC.Collect();
            }
        }
    }
}