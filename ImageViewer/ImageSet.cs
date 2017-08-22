using System.Linq;
using System.Windows.Media.Imaging;

namespace ImageViewer {
    struct ImageSet {
        public string[] paths;

        public bool is_valid() {
            if (paths != null && paths.Any()) {
                return true;
            }
            return false;
        }
    }
}