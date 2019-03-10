using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayParser.ReplaySorter.Configuration
{
    public class ReplaySorterAppConfiguration : IReplaySorterConfiguration
    {
        private string _logDirectory;

        public string LogDirectory => _logDirectory == null ? _logDirectory = (ConfigurationManager.AppSettings.Get("LogDirectory") ?? AppDomain.CurrentDomain.BaseDirectory) : _logDirectory;
    }
}
