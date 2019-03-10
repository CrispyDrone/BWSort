using System;
using System.Configuration;

namespace ReplayParser.ReplaySorter.Configuration
{
    public class ReplaySorterAppConfiguration : IReplaySorterConfiguration
    {
        private string _logDirectory;

        public string LogDirectory => _logDirectory == null ? _logDirectory = (ConfigurationManager.AppSettings.Get("LogDirectory") ?? AppDomain.CurrentDomain.BaseDirectory) : _logDirectory;
    }
}
