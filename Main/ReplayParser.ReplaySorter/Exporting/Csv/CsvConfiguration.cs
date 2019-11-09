namespace ReplayParser.ReplaySorter.Exporting
{
    public class CsvConfiguration : ICsvConfiguration
    {
        public char Delimiter { get; set; } = '\n';
        public char QuoteCharacter { get; set; } = '"';
        public char EscapeCharacter { get; set; } = '\\';
    }
}
