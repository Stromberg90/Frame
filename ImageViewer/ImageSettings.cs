namespace Frame
{
    public class ImageSettings
    {
        public ImageMagick.Channels DisplayChannel = ImageMagick.Channels.RGB;
        // Weird to have sort mode here?
        public SortMode CurrentSortMode { get; set; }
    }
}
