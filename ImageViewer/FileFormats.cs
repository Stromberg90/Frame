using System.Text;

namespace ImageViewer
{
    public static class FileFormats
    {
        public static string[] supported_extensions = {
            "bmp", "gif", "ico", "jpg", "png", "wdp", "tiff", "tif", "tga", "dds", "hdr", "exr",
            "xpm", "xbm", "psd"
        };
        public static string filter_string = ConstructFilterString();

        static string ConstructFilterString()
        {
            var new_filter_string = new StringBuilder();
            new_filter_string.Append("Image files (");
            for (int i = 0; i < supported_extensions.Length; i++)
            {
                if (i < supported_extensions.Length)
                {
                    new_filter_string.Append("*." + supported_extensions[i] + ", ");
                }
                else
                {
                    new_filter_string.Append("*." + supported_extensions[i] + ")");
                }
            }
            new_filter_string.Append(" | ");
            for (int i = 0; i < supported_extensions.Length; i++)
            {
                if (i < supported_extensions.Length)
                {
                    new_filter_string.Append("*." + supported_extensions[i] + "; ");
                }
                else
                {
                    new_filter_string.Append("*." + supported_extensions[i]);
                }
            }
            return new_filter_string.ToString();
        }

    }
}
