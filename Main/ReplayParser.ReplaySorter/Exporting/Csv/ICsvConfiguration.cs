﻿namespace ReplayParser.ReplaySorter.Exporting
{
    //todo: unnecessary (?)
    public interface ICsvConfiguration
    {
        char Delimiter { get; set; }
        char QuoteCharacter { get; set; }
        char EscapeCharacter { get; set; }
    }
}
