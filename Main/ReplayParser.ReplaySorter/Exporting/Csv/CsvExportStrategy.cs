using CsvHelper.Configuration.Attributes;
using CsvHelper;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Exporting.Interfaces;
using ReplayParser.ReplaySorter.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System;

namespace ReplayParser.ReplaySorter.Exporting.Strategies
{
    public class CsvExportStrategy : IExportStrategy
    {
        #region ReplayCsvRecord class

        private class ReplayCsvRecord
        {
            [Index(0)]
            public string GameType { get; set; }

            [Index(1)]
            public string GameFormat { get; set; }

            [Index(2)]
            public string Matchup { get; set; }

            [Index(3)]
            public string Player1 { get; set; }

            [Index(4)]
            public string Player2 { get; set; }

            [Index(5)]
            public string Player3 { get; set; }

            [Index(6)]
            public string Player4 { get; set; }

            [Index(7)]
            public string Player5 { get; set; }

            [Index(8)]
            public string Player6 { get; set; }

            [Index(9)]
            public string Player7 { get; set; }
            
            [Index(10)]
            public string Player8 { get; set; }

            [Index(11)]
            public string Player9 { get; set; }

            [Index(12)]
            public string Player10 { get; set; }

            [Index(13)]
            public string Player11 { get; set; }

            [Index(14)]
            public string Player12 { get; set; }

            [Index(15)]
            public string Map { get; set; }

            [Index(16)]
            public string Duration { get; set; }

            [Index(17)]
            public string Date { get; set; }

            [Index(18)]
            public string FileName { get; set; }
        }

        #endregion

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
            var errors = new List<string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            int counter = 0;
            var output = "";

            if (Replays != null)
            {
                using (var writer = new StringWriter())
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteHeader<ReplayCsvRecord>();

                    foreach (var replay in Replays)
                    {
                        try
                        {
                            csv.WriteRecord(ToCsvRecord(replay));
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{ex.Message}");
                            ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - ");
                        }
                        counter++;
                    }

                    // This is not good... I want to decouple the actual writing to a file from the csv content generation, but obviously this could be a massive
                    // memory drain... I guess you should use buffers
                    output = writer.ToString();
                }
            }

            stopwatch.Stop();

            return new ServiceResult<ServiceResultSummary<StringContent>>(
                new ServiceResultSummary<StringContent>(
                    new StringContent(
                        output
                    ), 
                    "", 
                    stopwatch.Elapsed, 
                    counter, 
                    0
                ),
                true,
                errors
            );
        }

        private ReplayCsvRecord ToCsvRecord(File<IReplay> replay)
        {
            var record = new ReplayCsvRecord();

            //todo: mapping

            return record;
        }
    }
}
