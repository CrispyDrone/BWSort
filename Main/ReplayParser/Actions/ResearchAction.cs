using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class ResearchAction : AbstractAction
    {
        public ResearchAction(int sequence, int frame, IPlayer player, TechType techType)
            : base(sequence, frame, player)
        {
            this.TechType = techType;

            ActionType = ActionType.Research;
	    }

        public override ActionType ActionType { get; protected set; }

        public TechType TechType { get; private set; }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(TechType);

            return sb.ToString();
        }
    }
}
