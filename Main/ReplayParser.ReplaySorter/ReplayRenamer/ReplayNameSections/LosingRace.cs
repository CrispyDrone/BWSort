using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    class LosingRace : IReplayNameSection
    {
        public LosingRace(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public IReplay Replay { get; set; }

        public string[] Races { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.LR; } }

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
                //var loserTeam = Replay.Players.Where(x => x.Identifier != winnerId);
                //int NumberOfLosers = loserTeam.Count();
                //Races = new string[NumberOfLosers];
                //int index = 0;
                //foreach (var aLoser in loserTeam)
                //{
                //    Races[index] = aLoser.RaceType.ToString().First().ToString();
                //    index++;
                //}
                var winner = Replay.Winner;
                List<IPlayer> loserTeam;
                if (winner != null && winner.Count() != 0)
                {
                    loserTeam = Replay.Players.Where(x => !winner.Contains(x)).ToList();
                }
                else
                {
                    loserTeam = Replay.Players.ToList();
                }

                // remove observers
                var observers = Replay.Observers;
                foreach (var observer in observers)
                {
                    loserTeam.Remove(observer);
                }

                int NumberOfLosers = loserTeam.Count();
                Races = new string[NumberOfLosers];
                int index = 0;
                foreach (var aLoser in loserTeam)
                {
                    Races[index] = aLoser.RaceType.ToString().First().ToString();
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
            return string.Join(separator, Races);
        }
    }
}
