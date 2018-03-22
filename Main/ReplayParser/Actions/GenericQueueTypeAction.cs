using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;
using ReplayParser.Interfaces.Action;

namespace ReplayParser.Actions
{
    public class GenericQueueTypeAction : GenericAction, IQueueTypeAction
    {
        public QueueType QueueType { get; private set; }

        public GenericQueueTypeAction(ActionType actionType, int sequence, int frame, IPlayer player, QueueType queueType)
		    : base(actionType, sequence, frame, player)
        {
            this.QueueType = queueType;
	    }
	
	    public override String ToString() {
		
		    StringBuilder sb = new StringBuilder();
		    sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(QueueType);

		    return sb.ToString();
	    }
    }
}
