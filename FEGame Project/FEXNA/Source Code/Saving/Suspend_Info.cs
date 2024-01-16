using System;
using System.IO;
using FEXNAVersionExtension;

namespace FEXNA.IO
{
    public class Suspend_Info
    {
        protected string Chapter_Id;
        protected string Lord_Actor_Face;
        protected int Turn;
        protected int Units;
        protected int Playtime;
        protected int Gold;
        protected int Save_Id;
        protected bool Preparations;
        protected bool HomeBase;
        protected Difficulty_Modes Difficulty;
        protected Mode_Styles Style;
        protected DateTime Time;

        #region Serialization
        public void write(BinaryWriter writer)
        {
            writer.Write(Chapter_Id);
            writer.Write(Lord_Actor_Face);
            writer.Write(Turn);
            writer.Write(Units);
            writer.Write(Playtime);
            writer.Write(Gold);
            writer.Write(Save_Id);
            writer.Write(Preparations);
            writer.Write(HomeBase);
            writer.Write((int)Difficulty);
            writer.Write((int)Style);
            writer.Write(Time.ToBinary());
        }

        public static Suspend_Info read(BinaryReader reader)
        {
            Suspend_Info result = new Suspend_Info();
            result.Chapter_Id = reader.ReadString();
            result.Lord_Actor_Face = reader.ReadString();
            result.Turn = reader.ReadInt32();
            result.Units = reader.ReadInt32();
            result.Playtime = reader.ReadInt32();
            result.Gold = reader.ReadInt32();
            result.Save_Id = reader.ReadInt32();
            if (!Global.LOADED_VERSION.older_than(0, 5, 4, 0))
            {
                result.Preparations = reader.ReadBoolean();
                result.HomeBase = reader.ReadBoolean();
            }
            else
            {
                result.Preparations = result.Turn <= 0;
                result.HomeBase = false;
            }
            result.Difficulty = (Difficulty_Modes)reader.ReadInt32();
            result.Style = (Mode_Styles)reader.ReadInt32();
            result.Time = DateTime.FromBinary(reader.ReadInt64());
            return result;
        }
        #endregion

        #region Accessors
        public string chapter_id { get { return Chapter_Id; } }
        public string lord_actor_face { get { return Lord_Actor_Face; } }
        public int turn { get { return Turn; } }
        public int units { get { return Units; } }
        public int playtime { get { return Playtime; } }
        public int gold { get { return Gold; } }
        public int save_id
        {
            get { return Save_Id; }
            set { Save_Id = value; }
        }
        public bool preparations { get { return Preparations; } }
        public bool home_base { get { return HomeBase; } }
        public Difficulty_Modes difficulty { get { return Difficulty; } }
        public Mode_Styles style { get { return Style; } }
        public DateTime time { get { return Time; } }
        #endregion

        public static Suspend_Info get_suspend_info(int file_id)
        {
            Suspend_Info result = new Suspend_Info();
            result.Chapter_Id = Global.game_state.chapter_id;
            if (Global.game_system.preparations)
                result.Lord_Actor_Face = Global.game_actors[Global.battalion.actors[0]].face_name;
            else if (Global.game_map.team_leaders[Constants.Team.PLAYER_TEAM] != -1 &&
                    Global.game_map.units.ContainsKey(Global.game_map.team_leaders[Constants.Team.PLAYER_TEAM]))
                result.Lord_Actor_Face = Global.game_map.units[Global.game_map.team_leaders[Constants.Team.PLAYER_TEAM]].actor.face_name;
            else
                result.Lord_Actor_Face = "";
            result.Turn = Global.game_system.chapter_turn;
            result.Units = Global.game_map.teams[Constants.Team.PLAYER_TEAM].Count;
            result.Playtime = Global.game_system.total_play_time;
            result.Gold = Global.battalion.gold;
            result.Save_Id = file_id;
            result.Preparations = Global.game_system.preparations;
            result.HomeBase = Global.game_system.home_base;
            result.Difficulty = Global.game_system.Difficulty_Mode;
            result.Style = Global.game_system.Style;
            result.Time = DateTime.Now;
            return result;
        }
    }
}
