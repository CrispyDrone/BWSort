using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter
{
    public class File<T>
    {
        public File(string originalFilePath, T content)
        {
            OriginalFilePath = originalFilePath;
            FilePath = originalFilePath;
            Content = content;
        }

        public string OriginalFilePath { get; }
        public string FilePath { get; set; }
        public string FutureFilePath { get; set; }
        public T Content { get; }

        //TODO add extension + filename + directory properties + tostring() override?
    }
}
