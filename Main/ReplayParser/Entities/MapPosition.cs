using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.Entities
{
    public class MapPosition : IMapPosition
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public MapPosition(int x, int y)
        {
            X = x; 
            Y = y;
        }

        public override String ToString()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(X);
            sb.Append(", ");
            sb.Append(Y);
            sb.Append("]");

            return sb.ToString();
        }
    }
}
