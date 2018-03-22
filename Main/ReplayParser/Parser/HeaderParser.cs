using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;

namespace ReplayParser.Parser
{
    public class HeaderParser : AbstractParser
    {
        
	    private static int GAME_NAME_SIZE    = 28;
	    private static int CREATOR_NAME_SIZE = 24;
	    private static int PLAYER_NAME_SIZE  = 25;
	    private static int MAP_NAME_SIZE     = 26;
	
	    private static int PLAYER_COLOUR_OFFSET = 593;
	    private static int PLAYER_SPOT_OFFSET   = 625;

        public HeaderParser(byte[] data)
            : base(data)
        {
        }

        public Header ParseHeader()
        {
				
		    EngineType gameEngine = (EngineType)_input.ReadByte();
		
		    int frameCount = _input.ReadInt();		
		    byte fillb = _input.ReadByte();
		    byte fillc = _input.ReadByte();
		    byte filld = _input.ReadByte();

            long gameTime = _input.ReadInt();
            DateTime creation = new DateTime(1970, 1, 1).AddSeconds(gameTime);
		
		    byte[] ka2 = new byte[8];
		    _input.ReadBytes(ref ka2);
		    int ka3 = _input.ReadInt();
		
		    String gameName = parseString(GAME_NAME_SIZE);
		
		    int mapWidth  = _input.ReadUnsignedShort();
		    int mapHeight = _input.ReadUnsignedShort();
		
		    byte[] fill2 = new byte[16];
		    _input.ReadBytes(ref fill2);
		
		    String gameCreator = parseString(CREATOR_NAME_SIZE);
		
		    byte mapTypeId = _input.ReadByte();
		    String mapName = parseString(MAP_NAME_SIZE);
		
		    byte[] fill3 = new byte[6];
		    _input.ReadBytes(ref fill3);
		
		    GameType gameType = (GameType)_input.ReadByte();
		
		    byte[] fill4 = new byte[31];
		    _input.ReadBytes(ref fill4);


            IList<IPlayer> players = ParsePlayers();

            return new Header(gameEngine, creation, gameType, gameCreator, mapName, frameCount, gameName, mapWidth, mapHeight, players);
	    }

        private IList<IPlayer> ParsePlayers()	
        {
		    IList<IPlayer> players = new List<IPlayer>();
		    for (int i = 0; i < 12; i++) {
			    IPlayer player = ParsePlayer(i);
			    if (player.PlayerType != PlayerType.None) {
				    players.Add(player);
			    }
		    }

            return players;
	    }

        private IPlayer ParsePlayer(int index)
        {
            // identifier, slottype are wrong?? weird values		
		    int identifier = _input.ReadInt();
		    SlotType slotType = (SlotType)_input.ReadInt();
		
		    PlayerType playerType = (PlayerType)_input.ReadByte();
		    RaceType raceType = (RaceType)_input.ReadByte();
		    byte forceId = _input.ReadByte();
		
            // name is missing 4 characters with UTF8 encoding means 4 bytes...
		    String name = parseString(PLAYER_NAME_SIZE);
		
		    ColourType colourType = ColourType.Unknown;
		    byte spot = 0;
		
		    if (index < 8) {
			    int colourOffset = PLAYER_COLOUR_OFFSET + (index * 4);
			    colourType = (ColourType)_data[colourOffset];
		
			    int spotOffset = PLAYER_SPOT_OFFSET + index;
			    spot = _data[spotOffset];
		    }


            return new Player(identifier, slotType, playerType, raceType, forceId, name, colourType, spot);
	    }
    }
}
