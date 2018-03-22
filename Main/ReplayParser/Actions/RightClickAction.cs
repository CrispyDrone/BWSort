using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Interfaces;
using ReplayParser.Interfaces.Action;

namespace ReplayParser.Actions
{
    public class RightClickAction : AbstractAction, IMapPositionAction, IObjectTypeAction, IQueueTypeAction
    {

        public IMapPosition MapPosition { get; private set; }
        public ObjectType ObjectType { get; private set; }
        public QueueType QueueType { get; private set; }

        public RightClickAction(int sequence, int frame, IPlayer player, 
            IMapPosition mapPosition, short memoryId, 
            ObjectType objectType, QueueType queueType)      
            : base(sequence, frame, player)
        {
            this.MapPosition = mapPosition;
            this.MemoryIdentifier = memoryId;
            this.ObjectType = objectType;
            this.QueueType = queueType;

            ActionType = ActionType.RightClick;
        }

        public override ActionType ActionType { get; protected set; }

        public short MemoryIdentifier { get; private set; }
	
	    public override String ToString() 
        {
		    StringBuilder sb = new StringBuilder();
		    sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(MapPosition);
		    sb.Append(", ");
            sb.Append(MemoryIdentifier);
		    sb.Append(", ");
            sb.Append(ObjectType);
		    sb.Append(", ");
            sb.Append(QueueType);
		
		    return sb.ToString();
	    }
    }
}
