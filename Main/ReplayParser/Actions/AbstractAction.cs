using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;

namespace ReplayParser.Actions
{
    public abstract class AbstractAction : IAction
    {

        public AbstractAction(int sequence, int frame, IPlayer player)
        {

            this.Sequence = sequence;
            this.Frame = frame;
            this.Player = player;
        }

        public int Sequence { get; private set; }

        public int Frame { get; private set; }

        public IPlayer Player { get; private set; }

        
	    public override String ToString() 
        {
		    StringBuilder sb = new StringBuilder();
		
		    sb.Append(Sequence);
		    sb.Append(", ");
		    sb.Append(Frame);
		    sb.Append(", ");
		    sb.Append(Player.Name);
		    sb.Append(", ");
		    sb.Append(ActionType);
		
		    return sb.ToString();
	    }

        public abstract ActionType ActionType { get; protected set;  }
    }
}
