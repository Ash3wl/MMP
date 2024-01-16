using System.Collections.Generic;

namespace FEXNA_Library
{
    public class Data_Terrain
    {
        public int Id = 1;
        public string Name = "Plains";
        public int Avoid = 0;
        public int Def = 0;
        public int Res = 0;
        public bool Stats_Visible = true;
        public int Step_Sound_Group = 0;
        public string Platform_Rename = "";
        public string Background_Rename = "";
        public int Dust_Type = 0;
        public bool Fire_Through = false;
        public int[][] Move_Costs = { new int[] { 1, 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1, 1 }, new int[] { 1, 1, 1, 1, 1 } };
        public int[] Heal;
        public int Minimap = 1;
        public List<int> Minimap_Group = new List<int> {};

        public override string ToString()
        {
            return string.Format("{0} {1}: stats {2}, {3}, {4}", Id, Name, Avoid, Def, Res);
        }
    }
}
