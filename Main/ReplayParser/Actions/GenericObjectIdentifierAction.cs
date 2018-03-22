using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;
using ReplayParser.Interfaces.Action;

namespace ReplayParser.Actions
{
    public class GenericObjectIdentifierAction : GenericAction, IObjectIdentifierAction
    {
	    public GenericObjectIdentifierAction(ActionType actionType, int sequence, int frame, IPlayer player, int objectId)
		    : base(actionType, sequence, frame, player)
        {
		    this.ObjectIdentifier = objectId;
	    }

	    public int ObjectIdentifier { get; private set;}
	
	    public override String ToString() {
		
		    StringBuilder sb = new StringBuilder();
		    sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(ObjectIdentifier);

		    return sb.ToString();
	    }

    }
}
