using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;
using ReplayParser.Actions;

namespace ReplayParser.Analyzers
{
    public static class ObserverAnalyzer
    {
        // All players should execute build actions, train/morph units while observers can not.
        public static IEnumerable<IPlayer> ExtractObservers(IReplay replay)
        {
            IList<IPlayer> players = new List<IPlayer>(replay.Players);

            foreach (IAction action in replay.Actions)
            {
                if (action.ActionType == ActionType.Build || action.ActionType == ActionType.Train || action.ActionType == ActionType.UnitMorph)
                {
                    players.Remove(action.Player);
                }
            }
            foreach (var player in players)
            {
                player.IsObserver = true;
            }
            return players;
        }
    }
}
