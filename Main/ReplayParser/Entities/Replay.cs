using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Analyzers;

namespace ReplayParser.Entities
{
    public class Replay : IReplay
    {
        private IEnumerable<IAction> _actions = new List<IAction>();
        public IEnumerable<IAction> Actions
        {
            get
            {
                foreach (var a in _actions)
                    yield return a;
            }
        }

        // i want to be able to adjust these players, so their IsObserver properties are correct...
        private IEnumerable<IPlayer> _players = new List<IPlayer>();
        public IEnumerable<IPlayer> Players
        {
            get
            {
                foreach (var p in _players)
                    yield return p;
            }
        }

        public string GameCreator { get; private set; }
        public EngineType EngineType { get; private set; }
        public int FrameCount { get; private set; }
        public GameType GameType { get; private set; }
        public string GameName { get; private set; }
        public DateTime Timestamp { get; private set; }
        public IReplayMap ReplayMap {get; private set; }

        private IEnumerable<IPlayer> winner = new List<IPlayer>();
        private bool winnerChecked;
        public IEnumerable<IPlayer> Winners
        {
            get
            {
                // lazy load the winner
                if (winnerChecked == false)
                {
                    try
                    {
                        winner = WinAnalyzer.ExtractWinners(this);
                        winnerChecked = true;
                    }
                    catch (InvalidOperationException IOEx)
                    {
                        Console.WriteLine("Failed to extract winner from replay.");
                        Console.WriteLine(IOEx.Message);
                    }
                    
                }

                return winner;
            }
        }
        private IEnumerable<IPlayer> _observers = new List<IPlayer>();
        private bool observersChecked;
        public IEnumerable<IPlayer> Observers
        {
            get
            {
                if (observersChecked == false)
                {
                    try
                    {
                        _observers = ObserverAnalyzer.ExtractObservers(this);
                        observersChecked = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while extracting observers.");
                        Console.WriteLine(ex.Message);
                    }
                }
                return _observers;
            }
        }

        public Replay(Header header, IList<IAction> actions)
        {

            this._actions = actions;
            this.GameCreator = header.GameCreator;
            this.EngineType = header.EngineType;
            this.FrameCount = header.FrameCount;
            this.GameType = header.GameType;
            this.GameName = header.GameName;
            this._players = header.Players;
            this.Timestamp = header.TimeStamp;
            this.winner = null;


            // Not implemented yet
            this.ReplayMap = new ReplayMap(header.MapName, header.MapWidth, header.MapHeight);
        }
    }
}
