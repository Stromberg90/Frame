using System.Linq;

namespace ImageViewer {
    struct ImageSet {
        public System.Collections.Generic.List<string> paths;

        public bool Is_valid() {
            return paths != null && paths.Any();
        }
    }
}