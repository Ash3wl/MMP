using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Windows.UserInterface.Command;

namespace FEXNA
{
    enum Save_Phases { Load_File, Cleanup, Save_Data, Overwrite_Confirm, World_Map }
    class Scene_Save : Scene_Base
    {
        const int WAIT_TIME = 20;

        protected Save_Phases Phase = Save_Phases.Load_File;
        protected int Timer;
        protected Parchment_Confirm_Window Save_Overwrite_Window;
        protected FE_Text Chapter_Name;
        //protected int Next_Chapter_Index = 0;
        protected bool Saving_Complete = false;
        protected bool Hard = false;
        protected bool All_Saves_Overwritten = true;

        public override void update()
        {
            Player.update_anim();
            switch (Phase)
            {
                case Save_Phases.Load_File:
                    Global.load_save_file = true;
                    Global.ignore_options_load = true;
                    Phase = Save_Phases.Cleanup;
                    break;
                case Save_Phases.Cleanup:
                    Global.game_actors.heal_battalion();
                    Global.game_actors.temp_clear();
                    Phase = Save_Phases.Save_Data;
                    break;
                case Save_Phases.Save_Data:
                    if (Timer > 0)
                        Timer--;
                    else
                    {
                        if (Global.save_file == null)
                            Phase = Save_Phases.World_Map;
                        else
                        {
                            // If we've run out of chapters to write to, write the save file
                            if (Saving_Complete)//Next_Chapter_Index >= Global.game_system.Chapter_Save_Progression_Keys.Length)
                            {
                                Timer = WAIT_TIME;
                                Save_Data_Calling = true;
                                Phase = Save_Phases.World_Map;
                            }
                            // Check if data for this file exists already
                            else
                            {
                                //string chapter = Global.game_state.chapter_id; //Debug
                                //if (Hard)
                                //    chapter += Config.DIFFICULTY_SAVE_APPEND[Difficulty_Modes.Hard];
                                //string chapter_progression = Global.game_system.Chapter_Save_Progression_Keys[Next_Chapter_Index];
                                // If so test if we actually want to overwrite
                                if (Global.save_file.ContainsKey(Global.game_state.chapter_id, Hard ? Difficulty_Modes.Hard : Difficulty_Modes.Normal))
                                {
                                    create_save_overwrite_window();
                                    Phase = Save_Phases.Overwrite_Confirm;
                                }
                                // Otherwise save the file
                                else
                                {
                                    foreach (string progression_id in Global.game_system.Chapter_Save_Progression_Keys)
                                        Global.save_file.save_data(Global.game_state.chapter_id, Hard ? Difficulty_Modes.Hard : Difficulty_Modes.Normal,
                                            progression_id, Global.game_system.previous_chapter_id);
                                    //Global.save_file.save_data(chapter, chapter_progression); //Debug
                                    close_save_overwrite_window();
                                }
                            }
                        }
                    }
                    break;
                case Save_Phases.Overwrite_Confirm:
                    update_save_overwrite();
                    break;
                case Save_Phases.World_Map:
                    if (!Save_Data_Calling)
                    {
                        if (Timer > 0)
                            Timer--;
                        else
                        {
                            Global.scene_change("Scene_Worldmap");
                            if (All_Saves_Overwritten)
                                Global.delete_suspend = true;
                            Global.load_save_info = true;
                        }
                    }
                    break;
            }
        }

        protected void create_save_overwrite_window()
        {
            Save_Overwrite_Window = new Parchment_Confirm_Window();
            Save_Overwrite_Window.set_text("Overwrite save?");
            Save_Overwrite_Window.add_choice("Yes", new Vector2(16, 32));
            Save_Overwrite_Window.add_choice("No", new Vector2(56, 32));
            Save_Overwrite_Window.size = new Vector2(112, 64);
            Save_Overwrite_Window.loc = new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) / 2 - new Vector2(112, 64) / 2;

            //string chapter_name = "";
            //foreach (var chapter in Global.data_chapters.Values)
            //    if (chapter.Id == Global.game_state.chapter_id)
                //if (chapter.Id == Global.game_system.Chapter_Save_Progression_Keys[Next_Chapter_Index]) //Debug
            //    {
                    //chapter_name = chapter.World_Map_Name; //Debug
                    string chapter_name = Global.data_chapters[Global.game_state.chapter_id].World_Map_Name;
                    if (Hard)
                        chapter_name += " (Hard)";
                    else if (Global.game_system.hard_mode)
                        chapter_name += " (Normal)";
            //        break;
            //    }
            Chapter_Name = new FE_Text();
            Chapter_Name.loc = Save_Overwrite_Window.loc + new Vector2(8, 24);
            Chapter_Name.Font = "FE7_Convo";
            Chapter_Name.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Convo_Red");
            Chapter_Name.text = chapter_name;
        }

        protected void update_save_overwrite()
        {
            Save_Overwrite_Window.update();
            if (Save_Overwrite_Window.is_ready)
            {
                if (Save_Overwrite_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    All_Saves_Overwritten = false;
                    close_save_overwrite_window();
                }
                else if (Save_Overwrite_Window.is_selected())
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    switch (Save_Overwrite_Window.index)
                    {
                        // Yes
                        case 0:
                            //string next_chapter = Global.game_system.Chapter_Save_Progression_Keys[Next_Chapter_Index]; //Debug
                            //Global.save_file.save_data(chapter, next_chapter);
                            foreach(string progression_id in Global.game_system.Chapter_Save_Progression_Keys)
                                Global.save_file.save_data(Global.game_state.chapter_id, Hard ? Difficulty_Modes.Hard : Difficulty_Modes.Normal,
                                    progression_id, Global.game_system.previous_chapter_id);
                            break;
                        // No
                        case 1:
                            All_Saves_Overwritten = false;
                            break;
                    }
                    close_save_overwrite_window();
                }
            }
        }

        protected void close_save_overwrite_window()
        {
            Save_Overwrite_Window = null;
            if (Hard)
            {
                Hard = false;
                Saving_Complete = true;
                //Next_Chapter_Index++; //Debug
            }
            else
            {
                if (Global.game_system.hard_mode)
                    Hard = true;
                else
                    Saving_Complete = true;
                    //Next_Chapter_Index++; //Debug
            }
            Phase = Save_Phases.Save_Data;
            Timer = WAIT_TIME;
        }

        public override void draw(SpriteBatch sprite_batch, GraphicsDevice device, RenderTarget2D[] render_targets)
        {
            if (Save_Overwrite_Window != null)
            {
                Save_Overwrite_Window.draw(sprite_batch);
                if (Save_Overwrite_Window.is_ready)
                {
                    sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    Chapter_Name.draw(sprite_batch);
                    sprite_batch.End();
                }
            }
        }
    }
}
