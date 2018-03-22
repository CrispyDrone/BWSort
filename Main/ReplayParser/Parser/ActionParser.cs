using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;
using ReplayParser.Actions;
using ReplayParser.Interfaces;

namespace ReplayParser.Parser
{
    public class ActionParser : AbstractParser
    {
	    private static int MESSAGE_SIZE   = 80;
	
	    private IDictionary<SlotType, IPlayer> players = new Dictionary<SlotType, IPlayer>();
	
	    private int     sequence;
	    private int     frame;
	    private IPlayer player;

        public ActionParser(byte[] data, IList<IPlayer> players)
            : base(data)
        { 

            foreach (var p in players)
            {
                //if (p.PlayerType == PlayerType.Computer)
                //{
                //    // is slot type being read wrong?? or why do all computers have -1 (= none??) as slottype ??
                //    SlotType ComputerSlotType = SlotType.None;
                //    while (/*this.players.Where(x => x.Key == ComputerSlotType).Count() != 0*/ /*Too slow*/ this.players.ContainsKey(ComputerSlotType))
                //    {
                //        ComputerSlotType++;
                //    }
                //    this.players.Add(new KeyValuePair<SlotType, IPlayer>(ComputerSlotType, p));
                //}
                //else
                //{
                    this.players.Add(p.SlotType, p);
                //}
            }
			    

        }

        public List<IAction> ParseActions()
        {
		
		    List<IAction> actions = new List<IAction>();
		
		    int length = _data.Length;	
		    int sectionStart = _input.Position;
		
		    while (_input.Position - sectionStart < length) {

			    frame = _input.ReadInt();

			    int blockSize  = _input.ReadUnsignedByte();
			    int blockStart = _input.Position;
			
			    while ((_input.Position - blockStart) < blockSize) {

				    SlotType slotType = (SlotType)_input.ReadByte();
				
                    if(players.ContainsKey(slotType))
				        player = players[slotType];
                    // since I gave a random key to computers, that doesn't reflect their actual key, I need to look for it
                    // can't do this, this is way too slow lol...
                    //else if (players.Where(x => x.Key == slotType).Count() != 0)
                    //{
                    //    player = players.Where(x => x.Key == slotType).First().Value;
                    //}
				
				    if (player == null) {
					    //throw new ArgumentException("Player not found: " + slotType);
				    }
				
                    

				    ActionType actionType = (ActionType)_input.ReadByte();
				    AbstractAction action;

                    if (actionType == ActionType.Terminate)
                        goto Terminate;

				    switch (actionType) {
                        case ActionType.MakeGamePublic:
                        case ActionType.MergeDarkArchon:
                        case ActionType.MissionBriefingStart:
                        case ActionType.StartGame:
                        case ActionType.Stim:
                        case ActionType.CancelAddon:
                        case ActionType.CancelUpgrade:
                        case ActionType.CancelResearch:
                        case ActionType.CancelNuke:
                        case ActionType.MergeArchon:
                        case ActionType.BuildSubunit:
                        case ActionType.ReaverStop:
                        case ActionType.CarrierStop:
                        case ActionType.CancelUnitMorph:
                        case ActionType.CancelConstruction:
                        case ActionType.ResumeGame:
                        case ActionType.PauseGame:
                        case ActionType.RestartGame:
                        case ActionType.KeepAlive:
					        action = new GenericAction(actionType, sequence, frame, player);
                            break;
                        case ActionType.CancelTrain:
                        case ActionType.Unload:
                            action = ParseGenericObjectIdentifierAction(actionType);
                            break;
                        case ActionType.Select:
                        case ActionType.ShiftSelect:
                        case ActionType.ShiftDeselect:
					        action = ParseGenericMultipleObjectIdentifierAction(actionType);
					        break;
                        case ActionType.BuildingMorph:
                        case ActionType.UnitMorph:
                        case ActionType.Train:
					        action = ParseGenericObjectTypeAction(actionType);
					        break;
                        case ActionType.Unburrow:
                        case ActionType.Burrow:
                        case ActionType.HoldPosition:
                        case ActionType.UnloadAll:
                        case ActionType.Siege:
                        case ActionType.Unsiege:
                        case ActionType.Decloak:
                        case ActionType.Cloak:
                        case ActionType.ReturnCargo:
                        case ActionType.Stop:
					        action = ParseGenericQueueTypeAction(actionType);
					        break;
				        case ActionType.RightClick:     action = ParseRightClickAction();   break;
                        case ActionType.GameChat:       action = ParseGameChatAction();     break;
                        case ActionType.LegacyGameChat: action = ParseGameChatAction();     break;
				        case ActionType.Build:		    action = ParseBuildAction();        break;
                        case ActionType.Lift:           action = ParseLiftAction();         break;
                        case ActionType.Target:         action = ParseTargetAction();       break;
                        case ActionType.Upgrade:        action = ParseUpgradeAction();      break;
                        case ActionType.Research:       action = ParseResearchAction();     break;
                        case ActionType.HotKey:         action = ParseHotKeyAction();       break;
                        case ActionType.LeaveGame:      action = ParseLeaveGameAction();    break;
                        case ActionType.Ally:           action = ParseAllyAction();         break;
                        case ActionType.Vision:         action = ParseVisionAction();       break;
                        case ActionType.MinimapPing:    action = ParseMinimapPingAction();  break;
                        default:
                            action = null;
                            break;
				    }

                    if (action != null)
                        actions.Add(action);
                    sequence++;
			    }
		    }

            Terminate:
		
		    return actions;
	    }

        private GenericObjectIdentifierAction ParseGenericObjectIdentifierAction(ActionType actionType)
        {
            int objectId = _input.ReadUnsignedShort();
            return new GenericObjectIdentifierAction(actionType, sequence, frame, player, objectId);
        }

	    private GenericMultipleObjectIdentifierAction ParseGenericMultipleObjectIdentifierAction(ActionType actionType)
        {
		    byte count = _input.ReadByte();
		    IList<int> objects = new List<int>();
		
		    for (int i = 0; i < count; i++) {
			    int unit = _input.ReadUnsignedShort();
			    objects.Add(unit);
		    }
		
		    GenericMultipleObjectIdentifierAction action = new GenericMultipleObjectIdentifierAction(actionType, sequence, frame, player, objects);
		
		    return action;
	    }
        private GenericObjectTypeAction ParseGenericObjectTypeAction(ActionType actionType)
        {

		    ObjectType objectType = (ObjectType)_input.ReadShort();
            return new GenericObjectTypeAction(actionType, sequence, frame, player, objectType);
	    }

        private GenericQueueTypeAction ParseGenericQueueTypeAction(ActionType actionType)
        {
		    QueueType queueType = (QueueType)_input.ReadByte();
		    return new GenericQueueTypeAction(actionType, sequence, frame, player, queueType);
        }


        private RightClickAction ParseRightClickAction()
        {
            IMapPosition mapPosition = ParseMapPosition();

            short memoryId = _input.ReadShort();
            ObjectType objectType = (ObjectType)_input.ReadShort();
            QueueType queueType = (QueueType)_input.ReadByte();

            return new RightClickAction(sequence, frame, player, mapPosition, memoryId, objectType, queueType);
        }

        private GameChatAction ParseGameChatAction()
        {
            byte playerId = _input.ReadByte();	
		
		    IPlayer sender = null;
		    foreach (IPlayer p in players.Values) {
			    if (p.Identifier == playerId) {
				    sender = p;
			    }
		    }
		
		    String message = parseString(MESSAGE_SIZE);
		    return new GameChatAction(sequence, frame, player, sender, message);
        }

        private BuildAction ParseBuildAction()
        {
            IMapPosition mapPosition = ParseMapPosition();
            OrderType orderType = (OrderType)_input.ReadByte();
            ObjectType objectType = (ObjectType)_input.ReadShort();

            return new BuildAction(sequence, frame, player, orderType, mapPosition, objectType);
        }

        private LiftAction ParseLiftAction()
        {
            IMapPosition position = ParseMapPosition();
            return new LiftAction(sequence, frame, player, position);
        }

        private TargetAction ParseTargetAction()
        {
            IMapPosition position = ParseMapPosition();
            int objectId = _input.ReadUnsignedShort();
            OrderType orderType = (OrderType)_input.ReadByte();
            ObjectType objectType = (ObjectType)_input.ReadShort();
            QueueType queueType = (QueueType)_input.ReadByte();

            return new TargetAction(sequence, frame, player, position, objectId, objectType, orderType, queueType);
        }

        private UpgradeAction ParseUpgradeAction()
        {
            UpgradeType upgrade = (UpgradeType)_input.ReadByte();
            return new UpgradeAction(sequence, frame, player, upgrade);
        }

        private ResearchAction ParseResearchAction()
        {
            TechType technology = (TechType)_input.ReadByte();
            return new ResearchAction(sequence, frame, player, technology);
        }

        private HotKeyAction ParseHotKeyAction()
        {
            HotKeyActionType type = (HotKeyActionType)_input.ReadByte();
            byte slot = _input.ReadByte();
            return new HotKeyAction(sequence, frame, player, type, slot);
        }

        private LeaveGameAction ParseLeaveGameAction()
        {
            LeaveGameType leaveGameType = (LeaveGameType)_input.ReadByte();
            return new LeaveGameAction(sequence, frame, player, leaveGameType);
        }

        private AllyAction ParseAllyAction()
        {
            _input.ReadInt(); // TODO - parse ally data properly
            return new AllyAction(sequence, frame, player);
        }

        private VisionAction ParseVisionAction()
        {
            _input.ReadShort(); // TODO - parse vision data properly
            return new VisionAction(sequence, frame, player);
        }
        private MinimapPingAction ParseMinimapPingAction()
        {
            IMapPosition mapPosition = ParseMapPosition();
            return new MinimapPingAction(sequence, frame, player, mapPosition);
        }

        private IMapPosition ParseMapPosition()
        {
	
		int x = _input.ReadUnsignedShort();
		int y = _input.ReadUnsignedShort();
		
		return new MapPosition(x, y);
	}
    }
}
