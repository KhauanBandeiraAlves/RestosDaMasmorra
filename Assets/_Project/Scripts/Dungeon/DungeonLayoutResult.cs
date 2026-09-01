using System.Collections.Generic;

namespace RestosDaMasmorra.Dungeon
{
    public class DungeonLayoutResult
    {
        public bool Success;
        public string FailureReason = "";
        public int Seed;
        public List<PlacedRoom> Rooms = new List<PlacedRoom>();
        public PlacedRoom Entrance;
        public PlacedRoom Boss;
        public List<PlacedRoom> MainPath = new List<PlacedRoom>();
        public int BranchCount;
    }
}
