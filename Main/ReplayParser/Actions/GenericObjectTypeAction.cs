using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces.Action;
using ReplayParser.Interfaces;

namespace ReplayParser.Actions
{
    public class GenericObjectTypeAction : GenericAction, IObjectTypeAction
    {
	    public ObjectType ObjectType { get; private set; }

        public GenericObjectTypeAction(ActionType actionType, int sequence, int frame, IPlayer player, ObjectType objectType)
		    : base(actionType, sequence, frame, player)
        {
            this.ObjectType = objectType;
	    }
	
	    public override String ToString() {
		
		    StringBuilder sb = new StringBuilder();
		    sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(ObjectType);

		    return sb.ToString();
	    }
    }
}
