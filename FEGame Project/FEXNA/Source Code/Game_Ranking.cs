using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FEXNAVersionExtension;
using FEXNA_Library;
using FEXNA_Library.Chapters;

namespace FEXNA
{
    public class Game_Ranking
    {
        protected string Chapter_Id;
        internal DataRanking Data { get; private set; }

        #region Accessors
        private Data_Chapter chapter_data
        {
            get { return Global.data_chapters[Chapter_Id]; }
        }

        public int turns { get { return Data.Turns; } }
        public int combat { get { return Data.Combat; } }
        public int exp { get { return Data.Exp; } }

        public int turns_display { get { return Data.Actual_Turns;} }
        public int combat_display { get { return Data.Actual_Combat; } }
        public int exp_display { get { return Data.Actual_Exp; } }
        public int completion_display { get { return Data.Actual_Completion; } }

        public string turns_letter
        {
            get { return DataRanking.individual_score_letter(this.turns); }
        }
        public string combat_letter
        {
            get { return DataRanking.individual_score_letter(this.combat); }
        }
        public string exp_letter
        {
            get { return DataRanking.individual_score_letter(this.exp); }
        }

        public string completion_letter
        {
            get
            {
                return DataRanking.individual_score_letter(
                    Data.Completion);
            }
        }

        public int ranking_index
        {
            get { return Data.RankingIndex; }
        }

        public int score { get { return Data.Score; } }
        public string rank
        {
            get
            {
                return Data.Rank;
            }
        }
        #endregion

        #region Serialization
        public void write(BinaryWriter writer)
        {
            writer.Write(Chapter_Id);
            writer.Write(Data.Actual_Turns);
            writer.Write(Data.Actual_Combat);
            writer.Write(Data.Actual_Exp);
            writer.Write(Data.Actual_Survival);
            writer.Write(Data.Actual_Completion);
        }

        public static Game_Ranking read(BinaryReader reader)
        {
            string chapter = reader.ReadString();
            int turns = reader.ReadInt32();
            int combat = reader.ReadInt32();
            int exp = reader.ReadInt32();
            int survival = 0;
            int completion = 0;
            if (!Global.LOADED_VERSION.older_than(0, 4, 0, 5))
            {
                survival = reader.ReadInt32();
                completion = reader.ReadInt32();
            }
            var ranking = new DataRanking(turns, combat, exp, survival, completion);

            return new Game_Ranking(chapter, ranking);
        }
        #endregion

        public Game_Ranking() :
            this(Global.game_state.chapter_id,
                Global.game_system.chapter_turn, Global.game_system.chapter_damage_taken,
                Global.game_system.chapter_exp_gain, Global.game_system.chapter_deaths,
                Global.game_system.chapter_completion) { }
        public Game_Ranking(Game_Ranking ranking) :
            this(ranking.Chapter_Id, new DataRanking(ranking.Data)) { }
        public Game_Ranking(string chapter_id, int turns, int combat, int exp, int survival, int completion)
            : this(chapter_id, new DataRanking(
                turns, combat, exp, survival, completion)) { }
        public Game_Ranking(string chapter_id, DataRanking ranking)
        {
            Chapter_Id = chapter_id;

            Data = ranking;
            Data.set_par(this.chapter_data);
        }
    }
}
