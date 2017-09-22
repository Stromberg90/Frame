using System.Collections.Generic;
using Optional;
using Optional.Unsafe;
using System.Linq;

namespace Frame {
    class ImageSet {
        public Option<List<string>> Paths { get; set; } = Option.Some(new List<string>());

        public bool IsValid() {
            if(!Paths.HasValue)
            {
                return false;
            }
            return Paths.ValueOrFailure().Any();
        }
    }
}