using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    class MatchUp : IReplayNameSection
    {
        public MatchUp(IReplay areplay, Teams team)
        {
            Replay = areplay;
            teams = team;
            GenerateSection();
        }

        public IReplay Replay { get; set; }

        public string[] TeamRaces { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.MU; } }

        public Teams teams { get; set; }

        public void GenerateSection()
        {
            // should pass Team as a parameter to the MatchUp constructor
            //Team teams = new Team(this.Replay);
            if (teams.PlayerNamesByTeam != null)
            {
                TeamRaces = new string[teams.PlayerNamesByTeam.Count];
                int teamnumber = 0;
                foreach (var team in teams.GroupedPlayers)
                {
                    StringBuilder races = new StringBuilder();
                    foreach (var player in team)
                    {
                        races.Append(player.RaceType.ToString().First());
                    }
                    TeamRaces[teamnumber] = races.ToString();
                    teamnumber++;
                }
            }
            else
            {
                throw new NullReferenceException("Unable to distinguish player from observer. No players in game!");
            }
        }

        public string GetSection(string separator)
        {
            return string.Join("v", TeamRaces);
        }
    }
}
