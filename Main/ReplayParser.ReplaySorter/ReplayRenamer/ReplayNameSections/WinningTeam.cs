using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.Diagnostics;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class WinningTeam : IReplayNameSection
    {
        public WinningTeam(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public IReplay Replay { get; set; }

        public string[] Names { get; set; }

        // Option 2
        //public IEnumerable<IPlayer> Players
        //{
        //    get
        //    {
        //        return Replay.Winner;
        //    }
        //}

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.WT; } }

        public void GenerateSection()
        {
            try
            {
                //int winnerId;
                //var winner = Replay.Winner;
                //// what is FORCEID??? Why isn't it different for opposing teams??
                ////var winnerForceId = winner.ForceIdentifier;
                ////var winnerTeam = Replay.Players.Where(x => x.ForceIdentifier == winnerForceId);
                //if (winner != null)
                //{
                //    winnerId = winner.Identifier;
                //}
                //else
                //{
                //    throw new NullReferenceException();
                //}
                //var winnerTeam = Replay.Players.Where(x => x.Identifier == winnerId);
                //int NumberOfWinners = winnerTeam.Count();
                //Names = new string[NumberOfWinners];
                //int index = 0;
                //foreach (var aWinner in winnerTeam)
                //{
                //    Names[index] = aWinner.Name;
                //    index++;
                //}

                var winnerTeam = Replay.Winners;
                if (winnerTeam == null || winnerTeam.Count() == 0)
                {
                    throw new NullReferenceException();
                }
                int NumberOfWinners = winnerTeam.Count();
                Names = new string[NumberOfWinners];
                int index = 0;
                foreach (var aWinner in winnerTeam)
                {
                    Names[index] = aWinner.Name;
                    index++;
                }
            }
            catch (NullReferenceException /*nullex*/)
            {
                //Console.WriteLine("No winner.");
                //Console.WriteLine(nullex.Message);
                Names = new string[] { "NoWinner" };
            }

        }

        public string GetSection(string separator)
        {
            return "(" + string.Join(separator.ToString(), Names) + ")";
        }
    }
}
