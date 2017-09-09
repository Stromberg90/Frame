using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer
{
    class FileId<T>
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
    }
}
