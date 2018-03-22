using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.Entities;

namespace ReplayParser.Actions
{
    public class HotKeyAction : AbstractAction
    {
        public HotKeyAction(int sequence, int frame, IPlayer player, HotKeyActionType hotKeyActionType, byte hotKeySlot)
            : base(sequence, frame, player)
        {
            this.HotKeyActionType = hotKeyActionType;
            this.HotKeySlot = hotKeySlot;

            ActionType = ActionType.HotKey;
	    }

        public override ActionType ActionType { get; protected set; }

        public HotKeyActionType HotKeyActionType { get; private set; }
        public byte HotKeySlot { get; private set; }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());
            sb.Append(", ");
            sb.Append(HotKeyActionType);
            sb.Append(", ");
            sb.Append(HotKeySlot);

            return sb.ToString();
        }
    }
}
