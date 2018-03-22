using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    class WinningRace : IReplayNameSection
    {
        public WinningRace(Interfaces.IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public Interfaces.IReplay Replay { get; set; }

        public string[] Races { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.WR; } }

        public void GenerateSection()
        {
            try
            {
                //var winner = Replay.Winner;
                //int winnerId;
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
                //Races = new string[NumberOfWinners];
                //int index = 0;
                //foreach (var aWinner in winnerTeam)
                //{
                //    Races[index] = aWinner.RaceType.ToString().First().ToString();
                //    index++;
                //}
                var winnerTeam = Replay.Winner;

                if (winnerTeam.Count() == 0)
                {
                    throw new NullReferenceException();
                }
                int NumberOfWinners = winnerTeam.Count();
                Races = new string[NumberOfWinners];
                int index = 0;
                foreach (var aWinner in winnerTeam)
                {
                    Races[index] = aWinner.RaceType.ToString().First().ToString();
                    index++;
                }
            }
            catch (NullReferenceException nullex)
            {
                Console.WriteLine("No winner.");
                Console.WriteLine(nullex.Message);
                Races = new string[] { "NoWinner" };
            }
            
        }

        public string GetSection(string separator)
        {
            return string.Join(separator.ToString(), Races);
        }
    }
}
