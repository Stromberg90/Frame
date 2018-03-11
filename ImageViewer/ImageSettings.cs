using System.IO;
using ImageMagick;

namespace Frame
{
  public class ImageSettings
  {
    public Channels DisplayChannel = Channels.RGB;

    int mipValue;

    public MagickImageCollection ImageCollection = new MagickImageCollection();

    public int Width => MipValue > 0 ? ImageCollection[0].Width : ImageCollection[MipValue].Width;

    public int Height => MipValue > 0 ? ImageCollection[0].Height : ImageCollection[MipValue].Height;

    //TODO: Seems like a bad way to go about this, I should rework the tab system.
    public long? SavedSize;

    public long Size => SavedSize.HasValue ? SavedSize.Value : new FileInfo(ImageCollection[MipValue].FileName).Length;

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
  }
}