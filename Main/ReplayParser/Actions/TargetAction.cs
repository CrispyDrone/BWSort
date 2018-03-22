using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces.Action;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class TargetAction : AbstractAction, IMapPositionAction, IObjectIdentifierAction, IObjectTypeAction, IOrderTypeAction, IQueueTypeAction
    {


        public TargetAction(int sequence, int frame, IPlayer player, 
            IMapPosition mapPosition, int objectId, 
            ObjectType objectType, OrderType orderType, 
            QueueType queueType)      
            : base(sequence, frame, player)
        {
            this.MapPosition = mapPosition;
            this.ObjectIdentifier = objectId;
            this.ObjectType = objectType;
            this.OrderType = orderType;
            this.QueueType = queueType;

            ActionType = ActionType.Target;
        }

        public override ActionType ActionType { get; protected set; }
        public IMapPosition MapPosition { get; private set; }
        public ObjectType ObjectType { get; private set; }
        public QueueType QueueType { get; private set; }
        public int ObjectIdentifier { get; private set; }
        public OrderType OrderType { get; private set; }
	
	    public override String ToString() 
        {
		    StringBuilder sb = new StringBuilder();
		    sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(MapPosition);
		    sb.Append(", ");
            sb.Append(ObjectIdentifier);
		    sb.Append(", ");
            sb.Append(ObjectType);
            sb.Append(", ");
            sb.Append(OrderType);
		    sb.Append(", ");
            sb.Append(QueueType);
		
		    return sb.ToString();
	    }
    }
}
