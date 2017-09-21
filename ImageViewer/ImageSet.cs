using System.Collections.Generic;
using System.Linq;

namespace Frame {
    struct ImageSet {
        public List<string> Paths { get; set; }

        public bool IsValid() {
            return Paths != null && Paths.Any();
        }
    }
}