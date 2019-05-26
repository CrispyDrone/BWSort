using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class Duration : IReplayNameSection
    {
        //public readonly double SlowestFPS = (double)1000/167;
        //public readonly double SlowerFPS = (double)1000 / 111;
        //public readonly double SlowFPS = (double)1000 / 83;
        //public readonly double NormalFPS = (double)1000 / 67;
        //public readonly double FastFPS = (double)1000 / 56;
        //public readonly double FasterFPS = (double)1000 / 48;
        public readonly double FastestFPS = (double)1000 / 42;

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
            // is game speed written into the binary replay file?? For now assume all replays are on fastest
            double gamelengthInSeconds = Math.Round(Replay.FrameCount / FastestFPS);
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
