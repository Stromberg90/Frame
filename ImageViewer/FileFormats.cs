using System.Text;
using Frame.Properties;

namespace Frame
{
    public static class FileFormats
    {
        public static readonly string FilterString = ConstructFilterString();
        static string ConstructFilterString()
        {
            var newFilterString = new StringBuilder();
            newFilterString.Append("Image files (");

            for (var i = 0; i < Settings.Default.SupportedExtensions.Count; i++)
            {
                var fileExt = "*." + Settings.Default.SupportedExtensions[i];
                if (i < Settings.Default.SupportedExtensions.Count)
                {
                    newFilterString.Append(fileExt + ", ");
                }
                else
                {
                    newFilterString.Append(fileExt + ")");
                }
            }
            newFilterString.Append(" | ");
            for (var i = 0; i < Settings.Default.SupportedExtensions.Count; i++)
            {
                var fileExt = "*." + Settings.Default.SupportedExtensions[i];
                if (i < Settings.Default.SupportedExtensions.Count)
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
