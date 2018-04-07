using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.Entities
{
    public class Player : IPlayer
    {

        public ColourType ColourType { get; private set; }
        public byte ForceIdentifier { get; private set; }
        public int Identifier { get; private set; }
        public String Name { get; private set; }
        public RaceType RaceType { get; private set; }
        public SlotType SlotType { get; private set; }
        public byte Spot { get; private set; }
        public PlayerType PlayerType { get; private set; }
        public bool? OpponentDropped { get; set; }
        public bool? IsObserver { get; set; }

        public Player(int identifier, SlotType slotType, PlayerType playerType,
			    RaceType raceType, byte forceId, String name,
			    ColourType colourType, byte spot) 
        {
		
		    this.Identifier = identifier;
		    this.SlotType   = slotType;
		    this.PlayerType = playerType;
		    this.RaceType   = raceType;
            this.ForceIdentifier = forceId;
		    this.Name       = name;
		    this.ColourType = colourType;
		    this.Spot       = spot;
            this.OpponentDropped = null;
            this.IsObserver = null;
	    }
    }
}
