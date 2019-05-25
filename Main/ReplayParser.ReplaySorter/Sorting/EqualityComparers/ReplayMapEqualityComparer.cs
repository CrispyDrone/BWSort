using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;
using ReplayParser.ReplaySorter.IO;

namespace ReplayParser.ReplaySorter.Sorting
{
    public class ReplayMapEqualityComparer : IEqualityComparer<IReplayMap>
    {
        public bool Equals(IReplayMap x, IReplayMap y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
                return false;
            if (x.MapWidth != y.MapWidth)
            {
                return false;
            }
            if (x.MapHeight != y.MapHeight)
            {
                return false;
            }
            if (RemoveInvalidCharsMapName(x.MapName) != RemoveInvalidCharsMapName(y.MapName))
            {
                return false;
            }
            return true;
        }

        public int GetHashCode(IReplayMap obj)
        {
            return obj.MapWidth.GetHashCode() + obj.MapHeight.GetHashCode();
        }

        private string RemoveInvalidCharsMapName(string MapName)
        {
            //foreach (char invalidChar in Sorter.InvalidFileChars)
            //{
            //    MapName = MapName.Replace(invalidChar.ToString(), "");
            //}
            //foreach (char invalidChar in Sorter.InvalidFileCharsAdditional)
            //{
            //    MapName = MapName.Replace(invalidChar.ToString(), "");
            //}
            MapName = FileHandler.RemoveInvalidChars(MapName);
            return MapName;
        }
    }
}
