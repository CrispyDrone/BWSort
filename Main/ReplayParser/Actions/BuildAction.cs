using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces.Action;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class BuildAction : AbstractAction, IOrderTypeAction, IMapPositionAction, IObjectTypeAction
    {
        public BuildAction(int sequence, int frame, IPlayer player, OrderType orderType, 
            IMapPosition mapPosition, ObjectType objectType)
            : base(sequence, frame, player)
        {
            this.OrderType = orderType;
            this.MapPosition = mapPosition;
            this.ObjectType = objectType;

            ActionType = ActionType.Build;
	    }

        public override ActionType ActionType { get; protected set; }

        public OrderType OrderType { get; private set; }

        public IMapPosition MapPosition { get; private set; }

        public ObjectType ObjectType { get; private set; }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(OrderType);
            sb.Append(", ");
            sb.Append(MapPosition);
            sb.Append(", ");
            sb.Append(ObjectType);

            return sb.ToString();
        }
    }
}
