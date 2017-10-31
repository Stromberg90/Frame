namespace Frame
{
    public class ImageSettings
    {
        public ImageMagick.Channels DisplayChannel = ImageMagick.Channels.RGB;

        int mipValue;

        // Weird to have sort mode here?
        public SortMode CurrentSortMode { get; set; }

        public bool HasMips { get; set; }
        public int MipCount { get; set; }
        public int MipValue
        {
            get => HasMips ? mipValue : 0;
            set {
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
