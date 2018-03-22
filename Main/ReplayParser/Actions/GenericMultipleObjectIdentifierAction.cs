using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;

namespace ReplayParser.Actions
{
    public class GenericMultipleObjectIdentifierAction : GenericAction
    {
        public GenericMultipleObjectIdentifierAction(ActionType actionType, int sequence, int frame, IPlayer player, IEnumerable<int> objectIds)
		    : base(actionType, sequence, frame, player)
        {
            this.ObjectIdentifiers = objectIds;
	    }

        public IEnumerable<int> ObjectIdentifiers { get; private set; }
	
	    public override String ToString() {
		
		    StringBuilder sb = new StringBuilder();
		    sb.Append(base.ToString());
            sb.Append(", ");
		    sb.Append(ObjectIdentifiers.Count());
		
		    sb.Append(", [");
		    int i = 0;
		    foreach (int obj in ObjectIdentifiers) {
                sb.Append(obj);

                if (i + 1 != ObjectIdentifiers.Count())
				    sb.Append(", ");
			    i++;
		    }
		    sb.Append("]");

		    return sb.ToString();
	    }

    }
}
