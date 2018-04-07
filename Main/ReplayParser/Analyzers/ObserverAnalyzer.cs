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
                // make collection of actions instead + what is ActionType.Target ?? how is A-Move recorded? Which actions can and can't observers do???
                if (action.ActionType == ActionType.Build || action.ActionType == ActionType.Train || action.ActionType == ActionType.UnitMorph || action.ActionType == ActionType.BuildingMorph || action.ActionType == ActionType.Research || action.ActionType == ActionType.UseCheat)
                {
                    if (players.Count > 0)
                        players.Remove(action.Player);
                    else
                        break;
                        //return null;
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
