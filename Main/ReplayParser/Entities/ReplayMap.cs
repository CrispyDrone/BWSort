using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReplayParser.Interfaces;

namespace ReplayParser.Entities
{
    public class ReplayMap : IReplayMap
    {
        public ReplayMap(string mapName, int mapWidth, int mapHeight)
        {
            this.MapName = mapName;
            this.MapWidth = mapWidth;
            this.MapHeight = mapHeight;
        }

        public string MapName { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
    }
}
