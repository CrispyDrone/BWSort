using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;
using ReplayParser.Actions;

namespace ReplayParser.Analyzers
{
    public static class WinAnalyzer
    {
        //public static IPlayer ExtractWinner(IReplay replay) 
        //{
        //    IList<IPlayer> players = new List<IPlayer>(replay.Players);

        //    foreach (IAction action in replay.Actions) {

        //        if (action.ActionType == ActionType.LeaveGame) {

        //            LeaveGameAction a = (LeaveGameAction)action;
        //            switch (a.LeaveGameType) {
        //                case LeaveGameType.Dropped:
        //                    return null;

        //                case LeaveGameType.Quit:
        //                    players.Remove(a.Player);
        //                    break;
        //                default:
        //                    throw new InvalidOperationException();
        //            }
        //        }
        //    }

        //    if (players.Count() == 1) {
        //        return players[0];
        //    }

        //    return null;
        //}


        //  What about OBSERVERS?? They don't have to leave the game or don't get dropped before it ends?? Do they remain in the players list??
        // yes observers stay in the list... Have to find a way to remove them, how?
        public static IEnumerable<IPlayer> ExtractWinners(IReplay replay)
        {
            IList<IPlayer> players = new List<IPlayer>(replay.Players);
            IList<IPlayer> observers = new List<IPlayer>(replay.Observers);

            foreach (IAction action in replay.Actions)
            {
                if (action.ActionType == ActionType.LeaveGame)
                {

                    LeaveGameAction a = (LeaveGameAction)action;
                    switch (a.LeaveGameType)
                    {
                        case LeaveGameType.Dropped:
                            players.Remove(a.Player);
                            // once I get player teams to work, I have to only add this to players of the other team obviously...
                            foreach (var aPlayer in players)
                            {
                                aPlayer.OpponentDropped = true;
                            }
                            break;

                        case LeaveGameType.Quit:
                            players.Remove(a.Player);
                            break;
                        default:
                            players.Remove(a.Player);
                            break;
                            // why throw an exception if the actiontype has been identified to be leavegame? Maybe there are other possible values for the type of leavegame action...
                            //throw new InvalidOperationException();

                    }
                }
                //if (action.ActionType == ActionType.Build)
                //{
                //    BuildAction a = (BuildAction)action;
                //    if (a.Player.IsObserver == null)
                //    {
                //        a.Player.IsObserver = false;
                //    }
                //}
            }
            // your code for IsObserver doesn't make much sense, it's not remembered past this function!! 
            if (players.Count() > 1)
            {
                //for (int i = players.Count - 1; i >= 0; i--)
                //{
                //    if (players[i].IsObserver == null)
                //    {
                //        players[i].IsObserver = true;
                //        players.RemoveAt(i);
                //    }
                //}
                for (int i = players.Count - 1; i>=0; i--)
                {
                    if (observers.Contains(players[i]))
                    {
                        players.RemoveAt(i);
                    }
                }
            }
            // should be ExtractWinnerSSSS => what about 2v2, 3v3, 4v4 !!? 
            return players.AsEnumerable<IPlayer>();
        }
    }
}
