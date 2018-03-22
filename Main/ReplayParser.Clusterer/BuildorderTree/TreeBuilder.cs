using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Actions;
using ReplayParser.Interfaces;

namespace ReplayParser.Clusterer.BuildorderTree
{
    class TreeBuilder
    {
        private List<IReplay> m_replays;
        private NodeList<BuildAction> m_roots;
        private NodeList<BuildAction> m_allGames;

        public NodeList<BuildAction> Roots { get { return m_roots; } }
        public NodeList<BuildAction> AllGames { get { return m_allGames; } }

        public TreeBuilder(List<IReplay> replays)
        {
            this.m_replays = replays;
        }

        public void Build()
        {
            NodeList<BuildAction> roots = new NodeList<BuildAction>();
            NodeList<BuildAction> allgames = new NodeList<BuildAction>();
            foreach (var replay in m_replays)
            {
                foreach (var player in replay.Players)
                {
                    var actions = replay.Actions.Where(x => x.Player == player
                                                && x.ActionType == Entities.ActionType.Build)
                                                .OrderBy(y => y.Sequence)
                                                .Cast<BuildAction>();

                    if (actions.Count() > 0)
                    {
                        BuildAction action = actions.ElementAt(0);
                        Node<BuildAction> node = new Node<BuildAction>(1, action, buildTree(actions));
                        allgames.Add(node);

                        if (roots.Where(x => x.Value.ObjectType == action.ObjectType).Count() == 0)
                        {
                            roots.Add(node);
                        }
                    }
                }
            }

            countOccurances(roots, allgames);
            m_roots = roots;
            m_allGames = allgames;
        }

        // TODO: Move to tree instead
        private string depthFirstSearch(Node<BuildAction> root, NodeList<BuildAction> neighbours)
        {
            if (root == null || neighbours == null) return null;

            string result = "";
            foreach(var n in neighbours)
            {
                result += root.Value.ObjectType + " -> " + n.Value.ObjectType + "; ";
                result += depthFirstSearch(n, n.Neighbors);
            }

            return result;
        }

        public override string ToString()
        {
            string result = "digraph G { ";
        	foreach(var r in this.Roots)
            {
                
                result += "subgraph " + r.Value.ObjectType + "{ "; // TODO: Get readable name
                foreach(var n in r.Neighbors)
                {
                    result += r.Value.ObjectType + " -> " + n.Value.ObjectType + "; ";
                    result += depthFirstSearch(n, n.Neighbors);
                    
                }
                result += "}";
            }

            foreach (var rr in this.Roots)
            {
                result += "start -> " + rr.Value.ObjectType + "; ";
            }

            result += "start [shape=Mdiamond]; ";
	        result += "end [shape=Msquare]; ";
            result += "}";
            return result;
        }

        private NodeList<BuildAction> buildTree(IEnumerable<BuildAction> actions)
        {
            // Hvorfor kan jeg ikke bruge Except? Det her sucks
            List<BuildAction> rest = actions.ToList();
            rest.RemoveAt(0);
            if (rest.Count == 0) return null;

            BuildAction action = rest.ElementAt(0);
            NodeList<BuildAction> result = new NodeList<BuildAction>();
            result.Add(new Node<BuildAction>(1, action, buildTree(rest)));
            return result;
        }

        private void countOccurances(NodeList<BuildAction> roots, NodeList<BuildAction> allgames)
        {
            if (roots == null || allgames == null) return;
            foreach (Node<BuildAction> root in roots)
            {
                foreach (Node<BuildAction> game in allgames)
                {
                    if (root.Value.ObjectType == game.Value.ObjectType)
                    {
                        root.Occurances++;
                        countOccurances(root.Neighbors, game.Neighbors);
                    }
                }
            }
        }


    }
}
