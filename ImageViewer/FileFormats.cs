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
                var fileExt = "*." + Properties.Settings.Default.SupportedExtensions[i];
                if (i < Properties.Settings.Default.SupportedExtensions.Count)
                {
                    newFilterString.Append(fileExt + ", ");
                }
                else
                {
                    newFilterString.Append(fileExt + ")");
                }
            }
            newFilterString.Append(" | ");
            for (var i = 0; i < Properties.Settings.Default.SupportedExtensions.Count; i++)
            {
                var fileExt = "*." + Properties.Settings.Default.SupportedExtensions[i];
                if (i < Properties.Settings.Default.SupportedExtensions.Count)
                {
                    newFilterString.Append(fileExt + "; ");
                }
                else
                {
                    newFilterString.Append(fileExt);
                }
            }
            return newFilterString.ToString();
        }

    }
}
