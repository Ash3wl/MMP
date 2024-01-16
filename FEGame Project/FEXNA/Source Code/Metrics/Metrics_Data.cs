using System;

namespace FEXNA.Metrics
{
    internal class Metrics_Data
    {
        private string Chapter;
        private DateTime StartTime;
        private Difficulty_Modes Difficulty;
        private Mode_Styles Style;
        private int PlayTime,
            RankTurns, RankCombat, RankExp, RankCompletion, RankSurvival,
            Deployed, DeployedLvl, Battalion, BattalionLvl;
        private Gameplay_Metrics Gameplay;

        public Metrics_Data(Gameplay_Metrics gameplay)
        {
            Chapter = Global.game_system.chapter_id;
            StartTime = Global.game_system.chapter_start_time;
            Difficulty = Global.game_system.Difficulty_Mode;
            Style = Global.game_system.Style;
            PlayTime = Global.game_system.chapter_play_time;
            RankTurns = Global.game_system.chapter_turn;
            RankCombat = Global.game_system.chapter_damage_taken;
            RankExp = Global.game_system.chapter_exp_gain;
            RankCompletion = Global.game_system.chapter_completion;
            RankSurvival = Global.game_system.chapter_deaths;
            Deployed = Global.game_system.deployed_unit_count;
            DeployedLvl = Global.game_system.deployed_unit_avg_level;
            Battalion = Global.battalion.actors.Count;
            BattalionLvl = Global.battalion.average_level;

            Gameplay = gameplay;
        }

        public string query_string()
        {
            int start_time = (int)(StartTime - new DateTime(1970, 1, 1)).TotalSeconds;
            string result = string.Format(
                "chapter={0}&starttime={1}&difficulty={2}&style={3}&playtime={4}&rankturns={5}&rankcombat={6}" +
                "&rankexp={7}&rankcompletion={8}&ranksurvival={9}&deployed={10}&deployedlvl={11}&battalion={12}&battalionlvl={13}",
                Chapter, start_time, (int)Difficulty, (int)Style, PlayTime,
                RankTurns, RankCombat, RankExp, RankCompletion, RankSurvival,
                Deployed, DeployedLvl, Battalion, BattalionLvl);
            return result;
        }

        public string gameplay_string()
        {
            return Gameplay.query_string();
        }
    }
}
