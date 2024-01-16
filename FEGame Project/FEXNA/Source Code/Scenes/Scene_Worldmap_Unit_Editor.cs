using System.Collections.Generic;

namespace FEXNA
{
    class Scene_Worldmap_Unit_Editor : Scene_Worldmap
    {
        protected override void set_chapters()
        {
            for (int i = 0; i < Global.Chapter_List.Count; i++)
            {
                Index_Redirect.Add(i);
            }
        }

        protected override void refresh_data(int previous_chapter)
        {
            Difficulty_Modes difficulty = Global.game_system.Difficulty_Mode;
            Global.game_system = new Game_System();
            Global.game_battalions = new Game_Battalions();
            Global.game_actors = new Game_Actors();
            Global.game_system.Difficulty_Mode = difficulty;

            Data_Window.set(Global.chapter_by_index(redirect).World_Map_Name, Global.chapter_by_index(redirect).World_Map_Lord_Id,
                Global.chapter_by_index(redirect).Preset_Data);
        }

        protected override bool hard_mode_enabled(int index)
        {
            return true;
        }

        protected override void start_chapter_worldmap_event()
        {
            if (Constants.WorldMap.HARD_MODE_BLOCKED.Contains(
                    Global.Chapter_List[redirect]) && Global.game_system.hard_mode)
                Global.game_system.play_se(System_Sounds.Buzzer);
            else
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                Phase = Worldmap_Phases.Fade_Out;
                Fade_Timer = Constants.WorldMap.WORLDMAP_FADE_TIME;
                Global.Audio.bgm_fade(Constants.WorldMap.WORLDMAP_FADE_TIME);
            }
        }

        protected override void start_chapter(string chapter, List<string> prior_chapters, bool standalone)
        {
            Global.game_system.New_Chapter_Id = chapter;
            Global.game_temp = new Game_Temp();
            Global.game_battalions = new Game_Battalions();
            Global.game_actors = new Game_Actors();
            Global.scene_change("Scene_Map_Unit_Editor");
        }
    }
}
