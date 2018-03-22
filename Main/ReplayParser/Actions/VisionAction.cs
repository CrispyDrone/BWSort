using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class VisionAction : AbstractAction
    {
        public VisionAction(int sequence, int frame, IPlayer player)
            : base(sequence, frame, player)
        {

            ActionType = ActionType.Vision;
	    }

        public override ActionType ActionType { get; protected set; }
    }
}
