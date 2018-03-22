using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class LosingTeam : IReplayNameSection
    {
        public LosingTeam(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public IReplay Replay { get; set; }

        public string[] Names { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.LT; } }

        public void GenerateSection()
        {
            try
            {
                //var winner = Replay.Winner;
                //int winnerId;
                //// what is FORCEID??? Why isn't it different for opposing teams??
                ////var winnerForceId = winner.ForceIdentifier;
                ////var loserTeam = Replay.Players.Where(x => x.ForceIdentifier != winnerForceId);
                //if (winner != null)
                //{
                //    winnerId = winner.Identifier;
                //}
                //else
                //{
                //    winnerId = -1;
                //    Console.WriteLine("No winner.");
                //}
                //var loserTeam = Replay.Players.Where(x => x.Identifier != winnerId).ToList();
                //var observers = Replay.Observers;
                //foreach (var observer in observers)
                //{
                //    loserTeam.Remove(observer);
                //}
                //// observers???
                //// could check on resources mined/units made/...

                //int NumberOfLosers = loserTeam.Count();
                //Names = new string[NumberOfLosers];
                //int index = 0;
                //foreach (var aLoser in loserTeam)
                //{
                //    Names[index] = aLoser.Name;
                //    index++;
                //}

                var winner = Replay.Winner;
                var loserTeam = Replay.Players.Where(x => !winner.Contains(x)).ToList();
                var observers = Replay.Observers;
                foreach (var observer in observers)
                {
                    loserTeam.Remove(observer);
                }
                // observers???
                // could check on resources mined/units made/...

                int NumberOfLosers = loserTeam.Count();
                Names = new string[NumberOfLosers];
                int index = 0;
                foreach (var aLoser in loserTeam)
                {
                    Names[index] = aLoser.Name;
                    index++;
                }
            }
            // not necessary any more?
            catch (NullReferenceException nullex)
            {
                Console.WriteLine(nullex.Message);
            }
            
        }

        public string GetSection(string separator)
        {
            return "(" + string.Join(separator.ToString(), Names) + ")";
        }
    }
}
