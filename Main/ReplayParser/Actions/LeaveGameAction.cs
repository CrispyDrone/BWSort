using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class LeaveGameAction : AbstractAction
    {
        public LeaveGameAction(int sequence, int frame, IPlayer player, LeaveGameType leaveGameType)
            : base(sequence, frame, player)
        {
            this.LeaveGameType = leaveGameType;

            ActionType = ActionType.LeaveGame;
	    }

        public override ActionType ActionType { get; protected set; }

        public LeaveGameType LeaveGameType { get; private set; }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(LeaveGameType);

            return sb.ToString();
        }
    }
}
