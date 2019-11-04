using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Exporting.Interfaces;
using ReplayParser.ReplaySorter.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System;

namespace ReplayParser.ReplaySorter.Exporting.Strategies
{
    public class CsvExportStrategy : IExportStrategy
    {
        private List<string> _errors = new List<string>();
        private StringBuilder _output = new StringBuilder();
        private List<string> _properties = new List<string>
        {
            "GameType",
            "GameFormat",
            "Matchup",
            "Player1",
            "Player2",
            "Player3",
            "Player4",
            "Player5",
            "Player6",
            "Player7",
            "Player8",
            "Player9",
            "Player10",
            "Player11",
            "Player12",
            "Map",
            "Duration",
            "Date",
            "FileName"
        };

        public CsvExportStrategy(ICsvConfiguration csvConfiguration, IEnumerable<File<IReplay>> replays)
        {
            CsvConfiguration = csvConfiguration;
            Replays = replays;
        }

        public ICsvConfiguration CsvConfiguration { get; }
        public IEnumerable<File<IReplay>> Replays { get; }
        public string Name => "Csv";

        public ServiceResult<ServiceResultSummary<StringContent>> Execute()
        {
            _errors.Clear();
            _output.Clear();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int counter = 0;
            if (Replays != null)
            {
                WriteHeader();
                foreach (var replay in Replays)
                {
                    try
                    {
                        WriteReplay(replay);
                    }
                    catch (Exception ex)
                    {
                        _errors.Add($"{ex.Message}");
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - ");
                    }
                    counter++;
                }
            }
            stopwatch.Stop();

            return new ServiceResult<ServiceResultSummary<StringContent>>(
                new ServiceResultSummary<StringContent>(
                    new StringContent(
                        _output.ToString()
                    ), 
                    "", 
                    stopwatch.Elapsed, 
                    counter, 
                    0
                ),
                true,
                _errors
            );
        }

        private void WriteHeader()
        {
            foreach (var field in _properties)
            {
                WriteField(field);
            }
        }

        private void WriteReplay(File<IReplay> replay)
        {

        }

        private void WriteField(string field)
        {
        }
    }
}
