using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.ReplaySorter.ReplayRenamer
{
    public class Team : IReplayNameSection
    {
        public Team(IReplay areplay)
        {
            Replay = areplay;
            GenerateSection();
        }

        public IReplay Replay { get; set; }
        public List<string[]> Teams { get; set; }

        // Option 1
        public IEnumerable<IGrouping<int, IPlayer>> GroupedPlayers { get; set; }

        // Option 2
        //public IEnumerable<IList<IPlayer>> GroupedPlayers { get; set; }

        public CustomReplayNameSyntax Type { get { return CustomReplayNameSyntax.T; } }

        public void GenerateSection()
        {
            // ?????????? why don't opposing players belong to a different force!!!!! Seems to be an issue with UseMapSettings gametype?
            // Maybe i should just use WinningTeam and LosingTeam to determine teams, but I don't know if a win is counted for both players of team 1 in the next situation:
            // team 1 player A leaves game, team 1 playere B wins game 1v2
            // then again this seems like a rare situation... More rare than people hosting a (team) game as UMS...



            // Option 1, UseMapSettings method
            IEnumerable<IPlayer> PlayersMinusObservers;
            if (Replay.Observers != null)
            {
                PlayersMinusObservers = Replay.Players.Where(x => !Replay.Observers.Contains(x));
            }
            else
            {
                PlayersMinusObservers = Replay.Players;
            }

            // as noted above, this is a "hack fix" for 1v1 use map settings
            // it (most likely) won't work properly for team games use map settings since identifier is unique per player instead of per team
            if (Replay.GameType == Entities.GameType.UseMapSettings)
            {
                GroupedPlayers = PlayersMinusObservers.GroupBy(x => x.Identifier);
            }
            else
            {
                GroupedPlayers = PlayersMinusObservers.GroupBy(x => (int)x.ForceIdentifier);
            }

            foreach (var team in GroupedPlayers)
            {
                int NumberOfPlayersInTeam = team.Count();
                string[] aTeam = new string[NumberOfPlayersInTeam];
                int index = 0;

                foreach (var player in team)
                {
                    aTeam[index] = player.Name;
                    index++;
                }
                if (Teams == null)
                {
                    Teams = new List<string[]>();
                }
                Teams.Add(aTeam);
            }

            // Option 2, WinningTeam, LosingTeam method
            // In this case I would have to rework winningteam/losingteam because in Team Free For All I would now only get a separation into winners vs losers and not per team...
            // but in that case it would have to rely on ForceIdentifier, and this doesn't (seem to) work for UseMapSettings which brings us back to our original problem!!!

            // So you have to first separate based on winning/losing then on ForceIdentifier, then it should work in essentially all cases!

            //WinningTeam winningTeam = new WinningTeam(Replay);
            //LosingTeam losingTeams = new LosingTeam(Replay);

            //List<IList<IPlayer>> TeamsPlayers = new List<IList<IPlayer>>();
            //TeamsPlayers.Add(winningTeam.Players.ToList());
            //// now separate losingTeams, might need to seperate winningTeam as well, but in theory it shouldn't be necessary. And even so, that points out an error with the WinnerExtraction instead...
            //if (Teams == null)
            //{
            //    Teams = new List<string[]>();
            //}
            //Teams.Add(winningTeam.Names);
            //foreach (var aLosingTeam in losingTeams.GroupedPlayers)
            //{
            //    // add to TeamsPlayers
            //    TeamsPlayers.Add(aLosingTeam.ToList());

            //    int NumberOfPlayersInLosingTeam = aLosingTeam.Count();
            //    string[] aLosingTeamNames = new string[NumberOfPlayersInLosingTeam];
            //    int index = 0;

            //    foreach (var player in aLosingTeam)
            //    {
            //        aLosingTeamNames[index] = player.Name;
            //        index++;
            //    }
            //    if (Teams == null)
            //    {
            //        Teams = new List<string[]>();
            //    }
            //    Teams.Add(aLosingTeamNames);
            //}
            //// set to grouped players
            //GroupedPlayers = TeamsPlayers;
        }

        public string GetSection(string separator)
        {
            StringBuilder teams = new StringBuilder();
            foreach (var team in Teams)
            {
                StringBuilder aTeam = new StringBuilder();
                aTeam.Append('(');
                aTeam.Append(string.Join(separator, team));
                aTeam.Append(')');
                teams.Append(aTeam.ToString() + "vs"/*separator*/);
            }
            teams.Remove(teams.ToString().Length - 2, 2);
            return teams.ToString();
        }
    }
}
