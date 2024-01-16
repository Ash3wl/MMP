using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FEXNA_Library
{
    public class Data_Status
    {
        public int Id = 0;
        public string Name = "";
        public string Description = "";
        public int Turns = 1;
        public bool Negative = false;
        public float Damage_Per_Turn = 0;
        public bool Unselectable = false;
        public bool Ai_Controlled = false;
        public bool Attacks_Allies = false;
        public bool No_Magic = false;
        public List<int> Skills = new List<int> { };
        public int Image_Index = 0;
        public int Map_Anim_Id = -1;
        public Color Battle_Color = new Color(0, 0, 0, 0);
    }
}
