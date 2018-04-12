using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
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

    int mipValue;

    public MagickImageCollection ImageCollection { get; set; } = new MagickImageCollection();

    public int Width
    {
      get
      {
        if (ImageCollection == null ||
            ImageCollection.Count == 0)
        {
          return 0;
        }

        return MipValue > 0 ? ImageCollection[0].Width : ImageCollection[MipValue].Width;
      }
    }

    public int Height
    {
      get
      {
        if (ImageCollection == null ||
            ImageCollection.Count == 0)
        {
          return 0;
        }

        return MipValue > 0 ? ImageCollection[0].Height : ImageCollection[MipValue].Height;
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
          newSize = new FileInfo(ImageCollection[MipValue].FileName).Length;
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

    public SortMode   SortMode   { get; set; }
    public SortMethod SortMethod { get; set; }

    public bool HasMips  { get; set; }
    public int  MipCount { get; set; }

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

    public bool IsGif { get; set; }
    public int CurrentFrame { get; set; } = 0;
    public int EndFrame { get; set; }

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
  }
}