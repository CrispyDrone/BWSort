using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;

namespace ReplayParser.Actions
{
    public class GenericAction : AbstractAction 
    {
	    public GenericAction(ActionType actionType, int sequence, int timestamp, IPlayer player) 
		    : base(sequence, timestamp, player)
        {
		    ActionType = actionType;
	    }

        public override ActionType ActionType { get; protected set; }
    }
}
