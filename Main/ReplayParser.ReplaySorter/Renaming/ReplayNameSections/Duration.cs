using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.CustomFormat;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class Duration : IReplayNameSection
    {
        public Duration(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        IReplay Replay { get; set; }

        public TimeSpan Value;


        public CustomReplayNameSyntax Type
        {
            get
            {
                return CustomReplayNameSyntax.DU;
            }
        }

        public void GenerateSection()
        {
            double gamelengthInSeconds = Math.Round(Replay.FrameCount / Constants.FastestFPS);
            Value = TimeSpan.FromSeconds(gamelengthInSeconds);
        }

        public string GetSection(string separator = "")
        {
            StringBuilder Duration = new StringBuilder();
            if (Value.Hours != 0)
            {
                Duration.Append(Value.Hours + "h");
            }
            Duration.Append(Value.Minutes + "m");
            Duration.Append(Value.Seconds + "s");
            return Duration.ToString();
        }
    }
}
