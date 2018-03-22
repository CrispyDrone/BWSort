using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces.Action;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class MinimapPingAction : AbstractAction, IMapPositionAction
    {
        public MinimapPingAction(int sequence, int frame, IPlayer player, IMapPosition mapPosition)
            : base(sequence, frame, player)
        {
            this.MapPosition = mapPosition;

            ActionType = ActionType.MinimapPing;
	    }

        public override ActionType ActionType { get; protected set; }

        public IMapPosition MapPosition { get; private set; }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(MapPosition);

            return sb.ToString();
        }
    }
}
