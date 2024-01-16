using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;
using FEXNA.Graphics.Preparations;
using FEXNA.Graphics.Text;
using FEXNA.Windows.Command.Items;

namespace FEXNA
{
    enum PrepPickUnitsInputResults { None, Status, UnitWindow, Closing }

    class Window_Prep_PickUnits : Windows.Map.Map_Window_Base
    {
        const int BLACK_SCEEN_FADE_TIMER = 8;
        const int BLACK_SCREEN_HOLD_TIMER = 4;
        const int START_BLACK_SCEEN_FADE_TIMER = 16;
        const int START_BLACK_SCREEN_HOLD_TIMER = 40;

        protected bool Active = true;
        protected bool Trading = false;
        protected bool Pressed_Start = false;
        protected int Units_Deployed = 0;
        protected bool[] Unit_Deployed_Flags;
        protected List<int> Units_To_Deploy = new List<int>(), Units_To_UnDeploy = new List<int>();
        protected Window_Prep_PickUnits_Unit Unit_Window;
        protected Window_Command_Item_Preparations Item_Window;
        protected Pick_Units_Items_Header Item_Header;
        protected FE_Text Goal;
        protected Button_Description R_Button, Start, Select;
        protected Sprite Backing_1, Backing_2;

        #region Accessors
        public bool active { set { Active = value; } }

        public bool pressed_start { get { return Pressed_Start; } }

        public bool ready { get { return !Closing && Black_Screen_Timer <= 0; } }

        public int actor_id
        {
            get { return Unit_Window.actor_id; }
            set
            {
                Unit_Window.actor_id = value;
                Unit_Window.refresh_scroll();
                refresh();
            }
        }

        protected bool unit_count_maxed { get { return Units_Deployed >= Global.game_map.deployment_points.Count; } }
        #endregion

        public Window_Prep_PickUnits()
        {
            int other_units = 0;
            foreach (int unit_id in Global.game_map.allies)
                if (Global.game_map.deployment_points.Contains(Global.game_map.units[unit_id].loc))
                    Units_Deployed++;
                else
                    other_units++;
            //foreach (Vector2 loc in Global.game_map.deployment_points)
            //    if (Global.game_map.get_unit(loc) != null)
            //        Units_Deployed++;
            // Units
            Unit_Deployed_Flags = new bool[Global.battalion.actors.Count];
            for (int i = 0; i < Global.battalion.actors.Count; i++)
                Unit_Deployed_Flags[i] = Global.battalion.is_actor_deployed(i);
            initialize_sprites(other_units);
            update_black_screen();
        }

        protected override void set_black_screen_time()
        {
            Black_Screen_Fade_Timer = BLACK_SCEEN_FADE_TIMER;
            Black_Screen_Hold_Timer = BLACK_SCREEN_HOLD_TIMER;
            base.set_black_screen_time();
        }

        protected void initialize_sprites(int other_units)
        {
            // Black Screen
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Black_Screen.tint = new Color(0, 0, 0, 255);
            // Background
            Background = new Menu_Background();
            Background.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Preparation_Background");
            (Background as Menu_Background).vel = new Vector2(0, -1 / 3f);
            (Background as Menu_Background).tile = new Vector2(1, 2);
            Background.stereoscopic = Config.PREP_BG_DEPTH;
            // Unit Window
            Unit_Window = new Window_Prep_PickUnits_Unit(Global.game_map.deployment_points.Count, Units_Deployed, other_units);
            Unit_Window.stereoscopic = Config.PREPUNIT_WINDOW_DEPTH;
            Unit_Window.IndexChanged += Unit_Window_IndexChanged;
            // //Yeti
            Backing_1 = new Sprite();
            Backing_1.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Backing_1.loc = new Vector2(24, 160);
            Backing_1.src_rect = new Rectangle(0, 112, 104, 32);
            Backing_1.tint = new Color(224, 224, 224, 128);
            Backing_1.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;
            Goal = new FE_Text();
            Goal.loc = Backing_1.loc + new Vector2(48, 0);
            Goal.Font = "FE7_Text";
            Goal.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Goal.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;
            Backing_2 = new Sprite();
            Backing_2.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Backing_2.loc = new Vector2(176, 160);
            Backing_2.src_rect = new Rectangle(0, 144, 136, 32);
            Backing_2.tint = new Color(224, 224, 224, 128);
            Backing_2.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;

            refresh_input_help();

            refresh();
        }

        protected void refresh_input_help()
        {
            /*R_Button = new Sprite(); //Debug
            R_Button.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            R_Button.loc = Backing_1.loc + new Vector2(32, 16 - 4);
            R_Button.src_rect = new Rectangle(104, 120, 40, 16);
            R_Button.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;*/
            R_Button = Button_Description.button(Inputs.R,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(126, 122, 24, 16));
            R_Button.loc = Backing_1.loc + new Vector2(32, 16 - 4);
            R_Button.offset = new Vector2(2, -2);
            R_Button.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;
            /*Start = new Sprite();
            Start.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Start.loc = Backing_2.loc + new Vector2(32, 0 - 1);
            Start.src_rect = new Rectangle(104, 40, 72, 16);
            Start.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;
            Select = new Sprite();
            Select.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Select.loc = Backing_2.loc + new Vector2(24, 16 - 1);
            Select.src_rect = new Rectangle(104, 72, 96, 16);
            Select.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;*/
            Start = Button_Description.button(Inputs.Start,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(142, 41, 32, 16));
            Start.loc = Backing_2.loc + new Vector2(32, 0 - 1);
            Start.offset = new Vector2(0, -1);
            Start.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;
            Select = Button_Description.button(Inputs.Select,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(150, 73, 48, 16));
            Select.loc = Backing_2.loc + new Vector2(24, 16 - 1);
            Select.offset = new Vector2(-12, -1);
            Select.stereoscopic = Config.PREPUNIT_INPUTHELP_DEPTH;
        }

        protected void refresh()
        {
            // Item Window
            Item_Window = new Window_Command_Item_Preparations(
                Unit_Window.actor_id, new Vector2(8, 52), true);
            Item_Window.face_shown = false;
            Item_Window.stereoscopic = Config.PREPUNIT_UNIT_INFO_DEPTH;
            Item_Header = new Pick_Units_Items_Header(Unit_Window.actor_id, Item_Window.width);
            Item_Header.loc = Item_Window.loc - new Vector2(4, 36);
            Item_Header.stereoscopic = Config.PREPUNIT_UNIT_INFO_DEPTH;
            // Goal
            Goal.text = Global.game_system.Objective_Text;
            Goal.offset = new Vector2(Font_Data.text_width(Goal.text) / 2, 0);
        }

        #region Update
        public override void update()
        {
            Unit_Window.update(Active && ready);

            base.update();
            if (Input.ControlSchemeSwitched)
                refresh_input_help();
        }

        private void Unit_Window_IndexChanged(object sender, EventArgs e)
        {
            refresh();
        }

        new internal PrepPickUnitsInputResults update_input()
        {
            // Close this window
            if (Global.Input.triggered(Inputs.B))
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                close();
                return PrepPickUnitsInputResults.Closing;
            }
            else if (Global.Input.triggered(Inputs.Start))
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                close(true);
                return PrepPickUnitsInputResults.Closing;
            }
            else if (Global.Input.triggered(Inputs.Select))
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                this.active = false;
                return PrepPickUnitsInputResults.UnitWindow;
            }

            // Select unit
            var selected_index = Unit_Window.consume_triggered(
                Inputs.A, MouseButtons.Left, TouchGestures.Tap);
            if (selected_index.IsSomething)
            {
                switch_unit(selected_index);
                return PrepPickUnitsInputResults.None;
            }

            // Status screen
            var status_index = Unit_Window.consume_triggered(
                Inputs.R, MouseButtons.Right, TouchGestures.LongPress);
            if (status_index.IsSomething)
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                active = false;
                return PrepPickUnitsInputResults.Status;
            }

            return PrepPickUnitsInputResults.None;
        }

        public bool switch_unit()
        {
            return switch_unit(Unit_Window.index);
        }
        public bool switch_unit(Game_Actor actor)
        {
            Unit_Window.actor_id = actor.id;
            return switch_unit();
        }
        public bool switch_unit(int index)
        {
            bool result = false;
            if (Unit_Deployed_Flags[index])
            {
                // If forced, buzz
                if (Global.game_map.forced_deployment.Contains(actor_id))
                {
                    Global.game_system.play_se(System_Sounds.Buzzer);
                }
                // If pre-deployed, buzz
                else if (!Global.game_map.deployment_points.Contains(Global.game_map.units[
                    Global.game_map.get_unit_id_from_actor(actor_id)].loc))
                {
                    Global.game_system.play_se(System_Sounds.Buzzer);
                }
                else
                {
                    // If Unit was deployed and has been removed, add them back to the team
                    if (Units_To_UnDeploy.Contains(index))
                    {
                        // If too many units, buzz
                        if (unit_count_maxed)
                            Global.game_system.play_se(System_Sounds.Buzzer);
                        else
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Units_Deployed++;
                            Units_To_UnDeploy.Remove(index);
                            Unit_Window.refresh_unit(true);
                            result = true;
                        }
                    }
                    // Else undeploy unit
                    else
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Units_Deployed--;
                        Units_To_UnDeploy.Add(index);
                        Unit_Window.refresh_unit(false);
                        result = true;
                    }
                }
            }
            else
            {
                // If Unit wasn't deployed and and wants to be, add them to the team
                if (!Units_To_Deploy.Contains(index))
                {
                    // If too many units, buzz
                    if (unit_count_maxed)
                        Global.game_system.play_se(System_Sounds.Buzzer);
                    else
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Units_Deployed++;
                        Units_To_Deploy.Add(index);
                        Unit_Window.refresh_unit(true);
                        result = true;
                    }
                }
                // Else undeploy unit
                else
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Units_Deployed--;
                    Units_To_Deploy.Remove(index);
                    Unit_Window.refresh_unit(false);
                    result = true;
                }
            }
            Unit_Window.unit_count = Units_Deployed;
            return result;
        }

        public bool actor_deployed(int actor_id)
        {
            bool result = false;
            int temp_actor_id = Unit_Window.actor_id;
            Unit_Window.actor_id = actor_id;
            if (!Units_To_UnDeploy.Contains(Unit_Window.index))
                if (Units_To_Deploy.Contains(Unit_Window.index) || Unit_Deployed_Flags[Unit_Window.index])
                    result = true;
            Unit_Window.actor_id = temp_actor_id;
            return result;
        }

        public List<int>[] unit_changes()
        {
            return new List<int>[] { Units_To_UnDeploy, Units_To_Deploy };
        }

        new public void close()
        {
            close(false);
        }
        public void close(bool start)
        {
            Pressed_Start = start;
            if (Pressed_Start)
            {
                Black_Screen_Fade_Timer = START_BLACK_SCEEN_FADE_TIMER;
                Black_Screen_Hold_Timer = START_BLACK_SCREEN_HOLD_TIMER;
            }
            Closing = true;
            Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
            if (Black_Screen != null)
                Black_Screen.visible = true;
            Global.game_system.Preparations_Actor_Id = actor_id;
        }
        #endregion

        #region Draw
        protected override void draw_window(SpriteBatch sprite_batch)
        {
            // //Yeti
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Backing_1.draw(sprite_batch);
            Backing_2.draw(sprite_batch);
            Goal.draw(sprite_batch);
            R_Button.Draw(sprite_batch);
            Start.Draw(sprite_batch);
            Select.Draw(sprite_batch);
            sprite_batch.End();

            // Unit Window
            Unit_Window.draw(sprite_batch);

            //Item Windows
            Item_Window.draw(sprite_batch);

            // Headers
            Item_Header.draw(sprite_batch);
        }
        #endregion
    }
}
