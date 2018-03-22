using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class UpgradeAction : AbstractAction
    {
        public UpgradeAction(int sequence, int frame, IPlayer player, UpgradeType upgradeType)
            : base(sequence, frame, player)
        {
            this.UpgradeType = upgradeType;

            ActionType = ActionType.Upgrade;
	    }

        public override ActionType ActionType { get; protected set; }

        public UpgradeType UpgradeType { get; private set; }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(UpgradeType);

            return sb.ToString();
        }
    }
}
