using System.Linq;

namespace ImageViewer {
    struct ImageSet {
        public System.Collections.Generic.List<string> Paths { get; set; }

        public bool IsValid() {
            return Paths != null && Paths.Any();
        }
    }
}