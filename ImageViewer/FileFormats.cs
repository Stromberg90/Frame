using System.Text;

namespace Frame
{
    public static class FileFormats
    {
        public static string FilterString = ConstructFilterString();
        static string ConstructFilterString()
        {
            var newFilterString = new StringBuilder();
            newFilterString.Append("Image files (");

            for (var i = 0; i < Properties.Settings.Default.SupportedExtensions.Count; i++)
            {
                if (i < Properties.Settings.Default.SupportedExtensions.Count)
                {
                    newFilterString.Append("*." + Properties.Settings.Default.SupportedExtensions[i] + ", ");
                }
                else
                {
                    newFilterString.Append("*." + Properties.Settings.Default.SupportedExtensions[i] + ")");
                }
            }
            newFilterString.Append(" | ");
            for (var i = 0; i < Properties.Settings.Default.SupportedExtensions.Count; i++)
            {
                if (i < Properties.Settings.Default.SupportedExtensions.Count)
                {
                    newFilterString.Append("*." + Properties.Settings.Default.SupportedExtensions[i] + "; ");
                }
                else
                {
                    newFilterString.Append("*." + Properties.Settings.Default.SupportedExtensions[i]);
                }
            }
            return newFilterString.ToString();
        }

    }
}
