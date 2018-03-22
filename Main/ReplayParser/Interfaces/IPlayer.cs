using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;

namespace ReplayParser.Interfaces
{
    public interface IPlayer
    {

        ColourType ColourType { get; }
        byte ForceIdentifier { get; }
        int Identifier { get; }
        String Name { get; }
        RaceType RaceType { get; }
        SlotType SlotType { get; }
        byte Spot { get; }
        PlayerType PlayerType { get; }
        // try adding property OpponentDropped ?
        bool? OpponentDropped { get; set; }
        // try determining whether player is an observer depending on whether he has build something in the game
        // doesn't work yet for replay.players...
        bool? IsObserver { get; set; }
    }
}
