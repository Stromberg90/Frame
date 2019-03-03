using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Frame.Annotations;
using ImageMagick;

namespace Frame
{
    public class ImageSettings : IDisposable, INotifyPropertyChanged
    {
        Channels displayChannel = Channels.RGB;

        public Channels DisplayChannel
        {
            get => displayChannel;
            set
            {
                displayChannel = value;
                OnPropertyChanged();
            }
        }

        public MagickImageCollection ImageCollection;

        int width;

        public int Width
        {
            get
            {
                if (ImageCollection == null
                    || ImageCollection.Count == 0)
                {
                    return width;
                }

                return MipValue > 0 ? ImageCollection[0].Width : ImageCollection[MipValue].Width;
            }
            set
            {
                width = value;
            }
        }

        int height;

        public int Height
        {
            get
            {
                if (ImageCollection == null ||
                    ImageCollection.Count == 0)
                {
                    return height;
                }

                return MipValue > 0 ? ImageCollection[0].Height : ImageCollection[MipValue].Height;
            }
            set
            {
                height = value;
            }
        }

        long size;

        public long Size
        {
            get
            {
                var newSize = size;
                try
                {
                    if (ImageCollection != null)
                    {
                        newSize = new FileInfo(ImageCollection[MipValue].FileName).Length;
                    }
                }
                catch (FileNotFoundException) { }
                catch (ArgumentException) { }
                finally
                {
                    size = newSize;
                }

                return size;
            }
            set => size = value;
        }

        public SortMode SortMode;
        public SortMethod SortMethod;

        public bool HasMips;
        public int MipCount;

        int mipValue;

        public int MipValue
        {
            get => HasMips ? mipValue : 0;
            set
            {
                {
                    if (!HasMips)
                    {
                        mipValue = 0;
                    }
                    else
                    {
                        if (value >= MipCount)
                        {
                            mipValue = MipCount - 1;
                        }
                        else if (value < 0)
                        {
                            mipValue = 0;
                        }
                        else
                        {
                            mipValue = value;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            ImageCollection?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Reset()
        {
            MipValue = 0;
        }
    }
}