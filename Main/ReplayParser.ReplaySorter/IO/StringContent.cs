using System.Collections.Generic;

namespace ReplayParser.ReplaySorter.IO
{
    public class StringContent
    {
        public StringContent(string header, string footer, IEnumerable<string> content)
        {
            Header = header;
            Footer = footer;
            Content = content;
        }

        public string Header { get; }
        public string Footer { get; }
        public IEnumerable<string> Content { get; }
    }
}
