using System.Linq;

namespace ImageViewer {
    struct ImageSet {
        public string[] paths;

        public bool Is_valid() {
            return paths != null && paths.Any() ? true : false;
        }
    }
}