using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using ArrayExtension;
using ListExtension;
using Vector2Extension;

namespace FEXNA_Library
{
    public class Data_Chapter
    {
        public string Id = "";
        /// <summary>
        /// Chapters that precede this chapter.
        /// When this chapter is started the data from these chapters will be loaded.
        /// Battalions will be loaded from one chapter each, with preference to the start of the list.
        /// </summary>
        public List<string> Prior_Chapters = new List<string>();
        /// <summary>
        /// A subset of Prior_Chapters listing the prior chapters to load ranking data from.
        /// If this is empty, rankings are loaded from all prior chapters.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<string> Prior_Ranking_Chapters = new List<string>();
        /// <summary>
        /// Chapters that need to be completed to access this chapter.
        /// This is combined with Prior_Chapters to determine if a chapter is available.
        /// ONLY battaltion data will be loaded from these chapters, and ONLY if no other prior chapters have the same battalions.
        /// </summary>
        [ContentSerializer(Optional = true)]
        public List<string> Completed_Chapters = new List<string>();
        [ContentSerializer(Optional = true)]
        public bool Standalone;
        public string Chapter_Name = "";
        [ContentSerializer(Optional = true)]
        public string World_Map_Name = "";
        [ContentSerializer(Optional = true)]
        public Microsoft.Xna.Framework.Vector2 World_Map_Loc;
        [ContentSerializer(Optional = true)]
        public int World_Map_Lord_Id;
        public List<string> Turn_Themes = new List<string>();
        public List<string> Battle_Themes = new List<string>();
        [ContentSerializer(Optional = true)]
        public int Battalion;
        public string Text_Key = "";
        public string Event_Data_Id = "";
        public int Ranking_Turns;
        public int Ranking_Combat = 0;
        public int Ranking_Exp = 0;
        public int Ranking_Completion = 0;
        [ContentSerializer(Optional = true)]
        public Preset_Chapter_Data Preset_Data;
        [ContentSerializer(Optional = true)]
        public List<string> Progression_Ids = new List<string>();


        public string ChapterTransitionName
        {
            get { return string.Format("{0}:{1}", World_Map_Name, Chapter_Name); }
        }
        public string TitleName
        {
            get { return string.Format("{0}:{1}", Id, Chapter_Name); }
        }

        #region Serialization
        public static Data_Chapter Read(BinaryReader input)
        {
            Data_Chapter result = new Data_Chapter();
            // Id
            result.Id = input.ReadString();
            result.Prior_Chapters.read(input);
            result.Prior_Ranking_Chapters.read(input);
            result.Completed_Chapters.read(input);
            result.Standalone = input.ReadBoolean();
            result.Chapter_Name = input.ReadString();
            result.World_Map_Name = input.ReadString();
            result.World_Map_Loc = result.World_Map_Loc.read(input);
            result.World_Map_Lord_Id = input.ReadInt32();

            result.Turn_Themes.read(input);
            result.Battle_Themes.read(input);

            result.Battalion = input.ReadInt32();
            result.Text_Key = input.ReadString();
            result.Event_Data_Id = input.ReadString();

            result.Ranking_Turns = input.ReadInt32();
            result.Ranking_Combat = input.ReadInt32();
            result.Ranking_Exp = input.ReadInt32();
            result.Ranking_Completion = input.ReadInt32();

            result.Preset_Data = Preset_Chapter_Data.Read(input);
            //Preset_Data = new Preset_Chapter_Data(input.ReadInt32(), input.ReadInt32(), input.ReadInt32(), input.ReadInt32());

            result.Progression_Ids.read(input);

            return result;
        }

        public void Write(BinaryWriter output)
        {
            output.Write(Id);
            Prior_Chapters.write(output);
            Prior_Ranking_Chapters.write(output);
            Completed_Chapters.write(output);
            output.Write(Standalone);
            output.Write(Chapter_Name);
            output.Write(World_Map_Name);
            World_Map_Loc.write(output);
            output.Write(World_Map_Lord_Id);

            Turn_Themes.write(output);
            Battle_Themes.write(output);

            output.Write(Battalion);
            output.Write(Text_Key);
            output.Write(Event_Data_Id);

            output.Write(Ranking_Turns);
            output.Write(Ranking_Combat);
            output.Write(Ranking_Exp);
            output.Write(Ranking_Completion);

            Preset_Data.Write(output);

            Progression_Ids.write(output);
        }
        #endregion

        public override string ToString()
        {
            return string.Format("{0}, {1}", Id, Chapter_Name);
        }

        public Data_Chapter() { }
        public Data_Chapter(Data_Chapter chapter)
        {
            Id = chapter.Id;
            Progression_Ids = new List<string>(chapter.Progression_Ids);
            Prior_Chapters = new List<string>(chapter.Prior_Chapters);
            Prior_Ranking_Chapters = new List<string>(chapter.Prior_Ranking_Chapters);
            Completed_Chapters = new List<string>(chapter.Completed_Chapters);
            Standalone = chapter.Standalone;
            Chapter_Name = chapter.Chapter_Name;
            World_Map_Name = chapter.World_Map_Name;
            World_Map_Loc = chapter.World_Map_Loc;
            World_Map_Lord_Id = chapter.World_Map_Lord_Id;

            Turn_Themes = new List<string>(chapter.Turn_Themes);
            Battle_Themes = new List<string>(chapter.Battle_Themes);

            Battalion = chapter.Battalion;
            Text_Key = chapter.Text_Key;
            Event_Data_Id = chapter.Event_Data_Id;

            Ranking_Turns = chapter.Ranking_Turns;
            Ranking_Combat = chapter.Ranking_Combat;
            Ranking_Exp = chapter.Ranking_Exp;
            Ranking_Completion = chapter.Ranking_Completion;

            Preset_Data = new Preset_Chapter_Data(chapter.Preset_Data);
        }

        const float MAX_TURNS_SCORE_MULT = 0.5f;
        const float MIN_TURNS_SCORE_MULT = 2.5f;
        const float MAX_COMBAT_SCORE_MULT = 0.5f;
        const float MIN_COMBAT_SCORE_MULT = 2.0f;
        const float MAX_EXP_SCORE_MULT = 1.5f;
        const float MIN_EXP_SCORE_MULT = 0.25f;

        public int turns_ranking(int turns)
        {
            if (Ranking_Turns == 0)
                return 0;

            return ranking_value(turns, Ranking_Turns,
                MIN_TURNS_SCORE_MULT, MAX_TURNS_SCORE_MULT);
        }
        public int combat_ranking(int combat)
        {
            //if (Ranking_Combat == 0)
            //    return 0;

            return ranking_value(combat, Ranking_Combat,
                MIN_COMBAT_SCORE_MULT, MAX_COMBAT_SCORE_MULT);
        }
        public int exp_ranking(int exp)
        {
            if (Ranking_Exp == 0)
                return 0;

            return ranking_value(exp, Ranking_Exp,
                MIN_EXP_SCORE_MULT, MAX_EXP_SCORE_MULT);
        }
        public int completion_ranking(int completion)
        {
            if (Ranking_Completion == 0)
                return 0;

            return ranking_value(completion, Ranking_Completion * 0.8f, 0,
                Chapters.DataRanking.MAX_INDIVIDUAL_SCORE / 100f);
        }

        private static int ranking_value(int value, float par, float minMult, float maxMult)
        {
            float rank;
            // Less than 100
            if (value < par ^ minMult > maxMult)
            {
                if (minMult < 1)
                    rank = (value - (minMult * par)) / (par - (minMult * par));
                else
                    rank = ((minMult * par) - value) / ((minMult - 1) * par);

                return (int)MathHelper.Lerp(0, 100, rank);
            }
            // More than 100
            {
                if (maxMult < 1)
                    rank = (value - (maxMult * par)) / ((1 - maxMult) * par);
                else
                    rank = ((maxMult * par) - value) / ((maxMult - 1) * par);

                return (int)MathHelper.Lerp(
                    Chapters.DataRanking.MAX_INDIVIDUAL_SCORE, 100, rank);
            }
        }

        public static float get_turns_par(int turns, float rank) //wark these get pars were internal instead of public
        {
            return get_par(turns, rank, MIN_TURNS_SCORE_MULT, MAX_TURNS_SCORE_MULT);
        }
        public static float get_combat_par(int combat, float rank)
        {
            return get_par(combat, rank, MIN_COMBAT_SCORE_MULT, MAX_COMBAT_SCORE_MULT);
        }
        public static float get_exp_par(int exp, float rank)
        {
            return get_par(exp, rank, MIN_EXP_SCORE_MULT, MAX_EXP_SCORE_MULT);
        }
        internal static float get_completion_par(int completion, float rank)
        {
            return get_par(completion / 0.8f, rank, 0, 1.5f);
        }

        private static float get_par(float value, float rank, float minMult, float maxMult)
        {
            // Less than 100
            if (rank < 100)
            {
                rank = MathHelper.Lerp(minMult, 1, rank / 100);
            }
            // More than 100
            else
            {
                rank = MathHelper.Lerp(1, maxMult, (rank - 100) /
                    (Chapters.DataRanking.MAX_INDIVIDUAL_SCORE - 100f));
            }
            if (rank == 0)
                return 0;
            return value / rank;
        }

        public List<string> get_previous_chapters(Dictionary<string, Data_Chapter> ChapterData)
        {
            return ChapterData.Where(x => x.Value.Progression_Ids.Intersect(Prior_Chapters).Any())
                .Select(x => x.Key)
                .ToList();

            return new List<string>();
        }
    }

    public struct Preset_Chapter_Data
    {
        public int Lord_Lvl;
        public int Units;
        public int Gold;
        public int Playtime;
        
        #region Serialization
        internal static Preset_Chapter_Data Read(BinaryReader input)
        {
            Preset_Chapter_Data result = new Preset_Chapter_Data();
            result.Lord_Lvl = input.ReadInt32();
            result.Units = input.ReadInt32();
            result.Gold = input.ReadInt32();
            result.Playtime = input.ReadInt32();
            return result;
        }

        public void Write(BinaryWriter output)
        {
            output.Write(Lord_Lvl);
            output.Write(Units);
            output.Write(Gold);
            output.Write(Playtime);
        }
        #endregion

        internal Preset_Chapter_Data(int lord_level, int units, int gold, int playtime)
        {
            Lord_Lvl = lord_level;
            Units = units;
            Gold = gold;
            Playtime = playtime;
        }
        internal Preset_Chapter_Data(Preset_Chapter_Data data)
        {
            Lord_Lvl = data.Lord_Lvl;
            Units = data.Units;
            Gold = data.Gold;
            Playtime = data.Playtime;
        }
    }
}
