using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class GameChatAction : AbstractAction
    {
        public GameChatAction(int sequence, int frame, IPlayer player, IPlayer sender, String message)
            : base(sequence, frame, player)
        {
            this.Sender = sender;
            this.Message = message;

            ActionType = ActionType.GameChat;
	    }

        public override ActionType ActionType { get; protected set; }

        public String Message { get; private set; }
        public IPlayer Sender { get; private set; }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(Sender.Name);
            sb.Append(", ");
            sb.Append(Message);

            return sb.ToString();
        }
    }
}
