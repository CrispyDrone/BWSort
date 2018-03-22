using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.Entities
{
    public class Header {

        public EngineType EngineType { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public GameType GameType { get; private set; }
        public String GameCreator { get; private set; }
        public String MapName { get; private set; }
        public int FrameCount { get; private set; }
        public String GameName { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        private IList<IPlayer> _players = new List<IPlayer>();
        public IEnumerable<IPlayer> Players 
        {
            get 
            {
                foreach (var p in _players)
                    yield return p;
            }   
        }
	

	    public Header(
			    EngineType engineType,
			    DateTime timestamp,
			    GameType gameType,
			    String gameCreator,
			    String mapName,
			    int frameCount,
			    String gameName,
			    int mapWidth,
			    int mapHeight,
			    IList<IPlayer> players
            ) 
        {
		
		    this.EngineType  = engineType;
		    this.TimeStamp   = timestamp;
		    this.GameType    = gameType;
		    this.GameCreator = gameCreator;
		    this.MapName     = mapName;
		    this.FrameCount  = frameCount;
		    this.GameName    = gameName;
		    this.MapWidth    = mapWidth;
		    this.MapHeight   = mapHeight;
		    this._players    = players;
	    }
    }
}
