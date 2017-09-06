using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer
{
    class ImageSettings
    {
        public ImageMagick.Channels displayChannel = ImageMagick.Channels.RGB;

        public SortMode Current_sort_mode { get; set; }
    }
}
