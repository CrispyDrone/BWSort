using System.Collections.Generic;

namespace ReplayParser.ReplaySorter.Exporting
{
    public class CsvFile
    {
        public string Path { get; set; }
        public Header Header { get; set; }
        public IEnumerable<Row> Rows { get; set; }
    }
}
