using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Entities;

namespace ReplayParser.Interfaces
{
    public interface IAction
    {
        ActionType ActionType { get; }
        int Sequence { get; }
        int Frame { get; }
        IPlayer Player{ get; }
    }
}
