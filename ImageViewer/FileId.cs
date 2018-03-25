using System.Collections.Generic;

namespace Frame
{
    public struct FileId<T>
    {
        public FileId(string path, T item, int id)
        {
            Path = path;
            Item = item;
            Id = id;
        }

        public string Path { get; }
        public T Item { get; }
        public int Id { get; }

    public override bool Equals(object obj)
    {
      if (!(obj is FileId<T>))
      {
        return false;
      }

      var id = (FileId<T>)obj;
      return Path == id.Path &&
             EqualityComparer<T>.Default.Equals(Item, id.Item) &&
             Id == id.Id;
    }

      bool Equals(FileId<T> other)
      {
        return string.Equals(Path, other.Path) && EqualityComparer<T>.Default.Equals(Item, other.Item) && Id == other.Id;
      }

      public override int GetHashCode()
      {
        unchecked
        {
          var hashCode = (Path != null ? Path.GetHashCode() : 0);
          hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Item);
          hashCode = (hashCode * 397) ^ Id;
          return hashCode;
        }
      }

      public static bool operator ==(FileId<T> left, FileId<T> right)
      {
        return left.Equals(right);
      }

      public static bool operator !=(FileId<T> left, FileId<T> right)
      {
        return !left.Equals(right);
      }
    }
}