using System.Text;

namespace Frame
{
    public static class FileFormats
    {
        public static string filter_string = ConstructFilterString();
        static string ConstructFilterString()
        {
            var new_filter_string = new StringBuilder();
            new_filter_string.Append("Image files (");

            for (int i = 0; i < Properties.Settings.Default.SupportedExtensions.Count; i++)
            {
                if (i < Properties.Settings.Default.SupportedExtensions.Count)
                {
                    new_filter_string.Append("*." + Properties.Settings.Default.SupportedExtensions[i] + ", ");
                }
                else
                {
                    new_filter_string.Append("*." + Properties.Settings.Default.SupportedExtensions[i] + ")");
                }
            }
            new_filter_string.Append(" | ");
            for (int i = 0; i < Properties.Settings.Default.SupportedExtensions.Count; i++)
            {
                if (i < Properties.Settings.Default.SupportedExtensions.Count)
                {
                    new_filter_string.Append("*." + Properties.Settings.Default.SupportedExtensions[i] + "; ");
                }
                else
                {
                    new_filter_string.Append("*." + Properties.Settings.Default.SupportedExtensions[i]);
                }
            }
            return new_filter_string.ToString();
        }

    }
}
