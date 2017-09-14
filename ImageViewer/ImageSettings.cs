namespace ImageViewer
{
    class ImageSettings
    {
        public ImageMagick.Channels displayChannel = ImageMagick.Channels.RGB;

        public SortMode CurrentSortMode { get; set; }
    }
}
