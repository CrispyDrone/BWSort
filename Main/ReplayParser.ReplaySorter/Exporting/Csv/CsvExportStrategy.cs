using CsvHelper.Configuration.Attributes;
using CsvHelper;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;
using ReplayParser.ReplaySorter.Exporting.Interfaces;
using ReplayParser.ReplaySorter.IO;
using ReplayParser.ReplaySorter.Renaming;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading;

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

            [Index(19)]
            public string Path { get; set; }
        }

        #endregion

        public CsvExportStrategy(IEnumerable<File<IReplay>> replays, ICsvConfiguration csvConfiguration = null)
        {
            Replays = replays ?? throw new ArgumentNullException(nameof(replays));
            CsvConfiguration = csvConfiguration ?? new CsvConfiguration();
        }

        public ICsvConfiguration CsvConfiguration { get; }
        public IEnumerable<File<IReplay>> Replays { get; }
        public string Name => "Csv";

        public async Task<ServiceResult<ServiceResultSummary>> ExecuteAsync(Stream output, IProgress<int> progress = null)
        {
            return await ExecuteAsync(output, CancellationToken.None, progress);
        }

        public async Task<ServiceResult<ServiceResultSummary>> ExecuteAsync(Stream output, CancellationToken cancellationToken, IProgress<int> progress = null)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var errors = new List<string>();

            var counter = await WriteReplaysAsync(output, errors, cancellationToken, progress);

            stopwatch.Stop();

            var message = "";
            var replayCount = Replays.Count();

            if (cancellationToken.IsCancellationRequested)
            {
                message = $"Operation cancelled, exported {counter} replays with {errors.Count} in {stopwatch.Elapsed.TotalSeconds} seconds.";
            }
            else
            {
                if (errors.Count == 0)
                    message = $"Exported {replayCount} replays successfully without errors in {stopwatch.Elapsed.TotalSeconds} seconds.";
                else
                    message = $"Exported {replayCount} replays with {errors.Count} errors in {stopwatch.Elapsed.TotalSeconds} seconds.";
            }

            return new ServiceResult<ServiceResultSummary>(
                new ServiceResultSummary(
                    null,
                    message, 
                    stopwatch.Elapsed, 
                    counter, 
                    0
                ),
                true,
                errors
            );
        }

        private async Task<int> WriteReplaysAsync(Stream output, List<string> errors, CancellationToken cancellationToken, IProgress<int> progress)
        {
            int counter = 0;

            var csvConfiguration = new CsvHelper.Configuration.Configuration();
            csvConfiguration.Delimiter = CsvConfiguration.Delimiter.ToString();
            csvConfiguration.Escape = CsvConfiguration.EscapeCharacter;
            csvConfiguration.Quote = CsvConfiguration.QuoteCharacter;

            using (var writer = new StreamWriter(output))
            using (var csv = new CsvWriter(writer, csvConfiguration))
            {
                csv.WriteHeader<ReplayCsvRecord>();
                await csv.NextRecordAsync();

                foreach (var replay in Replays)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return counter;

                    try
                    {
                        csv.WriteRecord(ToCsvRecord(replay));
                        await csv.NextRecordAsync();
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{ex.Message}");
                        ErrorLogger.GetInstance()?.LogError($"{DateTime.Now} - ");
                    }
                    counter++;

                    if (progress != null)
                        progress.Report(counter);

                }
            }

            return counter;
        }

        private ReplayCsvRecord ToCsvRecord(File<IReplay> replay)
        {
            // you lack abstractions, this is almost all information that should just readily be available on a `Replay` type
            // actually you have the ReplayDecorator class, maybe you should use that one??
            var gameType = replay.Content.GameType;
            var players = new IPlayer[12];
            var playersOrderedByTeam = replay.Content.Players
                .GroupBy(p => p.ForceIdentifier)
                .OrderBy(p => p.Key);

            int counter = 0;
            foreach (var team in playersOrderedByTeam)
            {
                foreach (var player in team)
                {
                    players[counter] = player;
                    counter++;
                }
            }

            var map = replay.Content.ReplayMap.MapName;
            var duration = TimeSpan.FromSeconds(Math.Round(replay.Content.FrameCount / Constants.FastestFPS));
            var date = replay.Content.Timestamp.ToString("yy-MM-dd", CultureInfo.InvariantCulture);
            var fileName = Path.GetFileNameWithoutExtension(replay.FilePath);
            var path = replay.FilePath;

            var replayDecorator = ReplayDecorator.Create(replay);

            var record = new ReplayCsvRecord
            {
                GameType = gameType.ToString(),
                GameFormat = replayDecorator.GameFormat(),
                Matchup = replayDecorator.Matchup(),
                Player1 = players[0]?.Name,
                Player2 = players[1]?.Name,
                Player3 = players[2]?.Name,
                Player4 = players[3]?.Name,
                Player5 = players[4]?.Name,
                Player6 = players[5]?.Name,
                Player7 = players[6]?.Name,
                Player8 = players[7]?.Name,
                Player9 = players[8]?.Name,
                Player10 = players[9]?.Name,
                Player11 = players[10]?.Name,
                Player12 = players[11]?.Name,
                Map = map,
                Duration = duration.TotalSeconds + "s",
                Date = date,
                FileName = fileName,
                Path = path
            };

            return record;
        }
    }
}
