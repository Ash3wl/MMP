using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#if DEBUG
using Microsoft.Xna.Framework.Content;
#endif

namespace FEXNA
{
    enum Title_States { Opening, Press_Start, Menu, Closing, Skip_To_Menu }
    class Scene_Title : Scene_Title_Load
    {
        Title_States State = Title_States.Opening;
        protected int Class_Reel_Timer = 0;
        protected Title_Background Background;
        protected Sprite IS_Logo;
        protected Press_Start Start_Image;
        protected Window_Title_Main_Menu Main_Menu;
        protected bool Enter_Pressed = false;
        protected bool Starting = false;
        protected bool Loading_Suspend = false, Quitting = false;

#if DEBUG
        public Texture2D get_team_map_sprite(int team, string name)
        {
            return Scene_Map.get_team_map_sprite(team, name);
        }
#endif

        protected override void initialize()
        {
            Scene_Type = "Scene_Title";
            Sword = new Sprite();
            Sword.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Title Sword");
            Sword.loc = new Vector2(Config.WINDOW_WIDTH / 2, -44);
            Sword.offset = new Vector2(Sword.texture.Width / 2, 0);
            Sword.stereoscopic = Config.TITLE_SWORD_DEPTH;
            FE_Logo = new Sprite();
            FE_Logo.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Fire Emblem Logo");
            FE_Logo.loc = new Vector2(36, 56 + 8);
            FE_Logo.stereoscopic = Config.TITLE_LOGO_DEPTH;
            IS_Logo = new Sprite();
            IS_Logo.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Immortal Sword Logo");
            IS_Logo.loc = new Vector2(Config.WINDOW_WIDTH / 2, 56 + 8);
            IS_Logo.offset = new Vector2(IS_Logo.texture.Width / 2, 0);
            IS_Logo.stereoscopic = Config.TITLE_LOGO_DEPTH;
            Flash = new Sprite();
            Flash.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            if (Global.scene.scene_type == "Scene_Soft_Reset")
                Flash.tint = new Color(0, 0, 0, 0);
            else
                Flash.tint = new Color(255, 255, 255, 255);
            Flash.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Background = new Title_Background();
            Background.stereoscopic = Config.TITLE_BG_DEPTH;
            Start_Image = new Press_Start(Global.Content.Load<Texture2D>(@"Graphics/Pictures/Press Start"));
            Start_Image.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT - 32);
            Start_Image.visible = false;
            Start_Image.stereoscopic = Config.TITLE_CHOICE_DEPTH;
            Global.load_save_info = true;
            Global.check_for_updates();
            if (Global.scene.scene_type == "Scene_Soft_Reset")
            {
                State = Title_States.Skip_To_Menu;
                Timer = Config.TITLE_GAME_START_TIME;
                Global.Audio.play_bgm("FE7 Main Theme", true);
            }
            else
                Global.Audio.play_bgm("FE7 Main Theme");
        }

        #region Update
        protected void make_press_start()
        {
            Start_Image.reset();
            Start_Image.visible = true;
        }

        public void update(KeyboardState key_state)
        {
            if (update_soft_reset())
                return;
            if (State != Title_States.Menu || !Main_Menu.active)
                Background.update();
            Player.update_anim();
            //if (State == Title_States.Press_Start) //Yeti
                Start_Image.update();
            switch (State)
            {
                case Title_States.Opening:
                    Flash.opacity -= 32;
                    if (Flash.opacity <= 0)
                    {
                        switch (Timer)
                        {
                            case 32:
                                make_press_start();
                                Timer = 0;
                                State = Title_States.Press_Start;
                                break;
                            default:
                                Timer++;
                                break;
                        }
                    }
                    break;
                case Title_States.Press_Start:
                    update_press_start(key_state);
                    break;
                case Title_States.Menu:
                    update_menu(key_state);
                    break;
                case Title_States.Closing:
                    Main_Menu.update(key_state);
                    // If trying to load suspend and failed
                    if (Loading_Suspend && !Global.suspend_load_successful)
                    {
                        Loading_Suspend = false;
                        State = Title_States.Menu;
                        Main_Menu.cancel_resume();
                        return;
                    }
                    Timer++;
                    if (Timer >= Config.TITLE_GAME_START_TIME)
                    {
                        // If shutting down
                        if (Quitting)
                            Global.quit();
                        // If loading suspend
                        else if (Loading_Suspend)
                            Global.scene_change("Load_Suspend");
                        // Else start game
                        else
                        {
                            Global.game_system.Difficulty_Mode = Global.save_file.Difficulty;
                            start_game();
                        }
                    }
                    break;
                case Title_States.Skip_To_Menu:
                    if (!Global.load_save_info)
                    {
                        if (Main_Menu == null)
                        {
                            Main_Menu = new Window_Title_Main_Menu();
                            Main_Menu.skip_fade();
                        }
                        else
                            Main_Menu.update(key_state, false);
                        Timer--;
                        if (Timer <= 0)
                            State = Title_States.Menu;
                    }
                    break;
            }
            Enter_Pressed = key_state.IsKeyDown(Keys.Enter);
        }

        protected void update_press_start(KeyboardState key_state)
        {
            if (Config.CLASS_REEL_WAIT_TIME > -1)
            {
                Class_Reel_Timer++;
                if (Class_Reel_Timer > (Config.CLASS_REEL_WAIT_TIME * Config.FRAME_RATE))
                {
                    Global.scene_change("Scene_Class_Reel");
                    return;
                }
            }
            Start_Image.opacity += 16;
            if (enter_pressed(key_state) ||
                Global.Input.triggered(Inputs.Start) ||
                Global.Input.triggered(Inputs.A) ||
                Global.Input.any_mouse_triggered ||
                Global.Input.gesture_triggered(TouchGestures.Tap))
            {
                Class_Reel_Timer = 0;
                //Start_Image.visible = false; //Yeti
                Global.Audio.play_se("System Sounds", "Press_Start");
                Main_Menu = new Window_Title_Main_Menu();
                State = Title_States.Menu;
            }
        }

        protected void update_menu(KeyboardState key_state)
        {
            if (Main_Menu.soft_rest_blocked)
                SoftResetBlocked = true;
            Main_Menu.update(key_state);
            if (Main_Menu.closed)
            {
                Main_Menu = null;
                Start_Image.reset();
                Start_Image.visible = true;
                State = Title_States.Press_Start;
            }
            if (Main_Menu == null || !Main_Menu.active)
                return;
            Start_Image.visible = false;
            Main_Menu.active = false;
            if (Main_Menu.resuming)
            {
                Global.loading_suspend = true;
                Loading_Suspend = true;
                State = Title_States.Closing;
                Timer = 0;
            }
            else if (Main_Menu.new_game)
            {
                Save_Data_Calling = true;
                Loading_Suspend = false;
                State = Title_States.Closing;
                Timer = 0;
                if (Global.save_files_info == null)
                    Global.save_files_info = new Dictionary<int, FEXNA.IO.Save_Info>();
                Global.save_files_info.Add(Global.start_game_file_id, FEXNA.IO.Save_Info.new_file());
            }
            else if (Main_Menu.load_suspend)
            {
                Global.loading_suspend = true;
                Loading_Suspend = true;
                State = Title_States.Closing;
                Timer = 0;
            }
            else if (Main_Menu.load_map_save)
            {
                Suspend_Filename = Config.MAP_SAVE_FILENAME;
                Global.loading_suspend = true;
                Loading_Suspend = true;
                State = Title_States.Closing;
                Timer = 0;
            }
            else if (Main_Menu.world_map)
            {
                Global.load_save_file = true;
                Loading_Suspend = false;
                State = Title_States.Closing;
                Timer = 0;
            }
            else if (Main_Menu.quitting)
            {
                Quitting = true;
                State = Title_States.Closing;
                Timer = 0;
            }
            else
                Main_Menu.active = true;
        }

        protected bool enter_pressed(KeyboardState key_state)
        {
            return !Enter_Pressed && key_state.IsKeyDown(Keys.Enter);
        }

        protected void start_game()
        {
            Global.scene_change("Start_Game");
            Global.Audio.bgm_fade(30);
        }
        #endregion

        #region Draw
        public override void draw(SpriteBatch sprite_batch, GraphicsDevice device, RenderTarget2D[] render_targets)
        {
            device.SetRenderTarget(render_targets[1]);
            device.Clear(Color.Transparent);

            if (Main_Menu == null || !Main_Menu.active)
            {
                Background.draw(sprite_batch);
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                //Sword.draw(sprite_batch);
                //IS_Logo.draw(sprite_batch);
                //FE_Logo.draw(sprite_batch);
                Start_Image.draw(sprite_batch);
                Flash.draw(sprite_batch);
                sprite_batch.End();
            }
            if (Main_Menu != null)
                Main_Menu.draw(sprite_batch);

            device.SetRenderTarget(render_targets[0]);
            device.Clear(Color.Transparent);
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            int alpha = State == Title_States.Skip_To_Menu ? ((Config.TITLE_GAME_START_TIME - Timer) * 255 / Config.TITLE_GAME_START_TIME) :
                (State == Title_States.Closing ? (Config.TITLE_GAME_START_TIME - Timer) * 255 / Config.TITLE_GAME_START_TIME : 255);
            sprite_batch.Draw(render_targets[1], Vector2.Zero, new Color(alpha, alpha, alpha, alpha));
            sprite_batch.End();
        }
        #endregion
    }
}
