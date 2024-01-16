﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;

namespace FEXNA.Windows.Map.Info
{
    class Window_Objective_Info : Window_Map_Info
    {
        protected Objective_Window Window_Img;
        protected FE_Text Objective, Turn_Label, Turn_Count, Turn_Slash, Turn_Max, Enemy_Label, Enemy_Count, Seize_Count;
        protected readonly static Vector2 NAME_LOC = new Vector2(0, 6), TURN_LABEL_LOC = new Vector2(52, 22), TURN_LOC = new Vector2(28, 22),
            ENEMY_LABEL_LOC = new Vector2(24, 22), ENEMY_LOC = new Vector2(64, 22);

        #region Accessors
        private int lines { get { return Window_Img == null ? 1 : Window_Img.lines; } }

        private int turn { get { return Global.game_state.turn; } }
        #endregion

        public Window_Objective_Info()
        {
            LEFT_X = 0;
            RIGHT_X = Math.Max(Config.WINDOW_WIDTH / 2, Config.WINDOW_WIDTH - 87);
            TOP_Y = 4;
            refresh_positions();
            initialize();
            loc = new Vector2(RIGHT_X, TOP_Y - (this.lines * 16 + 16));
            refresh();
        }

        protected override void initialize()
        {
            base.initialize();
            loc = new Vector2(-86, TOP_Y);
        }

        protected override void initialize_images()
        {
            Window_Img = new Objective_Window();
            Window_Img.offset = new Vector2(5, 5);
            Window_Img.tint = new Color(240, 240, 240, 224);
            Objective = new FE_Text();
            Objective.Font = "FE7_Text";
            Objective.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Turn_Label = new FE_Text();
            Turn_Label.Font = "FE7_Text";
            Turn_Label.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Turn_Label.text = "Turn";
            Turn_Count = new FE_Text_Int();
            Turn_Count.Font = "FE7_Text";
            Turn_Count.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
            Turn_Slash = new FE_Text();
            Turn_Slash.Font = "FE7_Text";
            Turn_Slash.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Turn_Slash.text = "/";
            Turn_Max = new FE_Text_Int();
            Turn_Max.Font = "FE7_Text";
            Turn_Max.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
            Enemy_Label = new FE_Text();
            Enemy_Label.Font = "FE7_Text";
            Enemy_Label.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Enemy_Label.text = "Left";
            Enemy_Count = new FE_Text_Int();
            Enemy_Count.Font = "FE7_Text";
            Enemy_Count.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
            Seize_Count = new FE_Text_Int();
            Seize_Count.Font = "FE7_Text";
            Seize_Count.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
            draw_images();
        }

        protected override void set_images()
        {
            set_images(false);
        }
        public void set_images(bool player_called)
        {
            if (player_called)
                if (map_check() != Map_Info_State.Onscreen)
                    return;
            if (Offscreen && Y_Move_List.Count == 0)
                move_on();
            draw_images();
        }

        public void refresh_objective()
        {
            draw_images();
        }

        #region Refresh
        protected virtual void draw_images()
        {
            Objective.text = Global.game_system.Objective_Text;
            if (Objective.text == "Secure Stairway")
            {
                Objective.text = Global.game_system.VARIABLES[73] + "/3 Secured";
            }
            Objective.offset.X = Font_Data.text_width(Objective.text, "FE7_Text_Info") / 2;
            int lines = Math.Max(1, Objective.text.Split('\n').Length);
            switch (Global.game_system.Objective_Mode[0])
            {
                case 0:
                    Window_Img.lines = lines;
                    break;
                // Rout
                case 1:
                    Window_Img.lines = lines + 1;

                    int enemy_count = 0;
                    int[] player_group = null;
                    foreach (int[] group in Constants.Team.TEAM_GROUPS)
                        if (group.Contains(Global.game_state.team_turn))
                        {
                            player_group = group;
                            break;
                        }
                    if (player_group != null)
                        for (int i = 1; i <= Constants.Team.NUM_TEAMS; i++)
                            if (!player_group.Contains(i))
                                enemy_count += Global.game_map.teams[i].Count;
                    Enemy_Count.text = enemy_count.ToString();
                    Enemy_Label.loc = new Vector2(0, 16 * (lines - 1));
                    Enemy_Count.loc = new Vector2(0, 16 * (lines - 1));
                    break;
                // Time Limit
                case 2:
                    Window_Img.lines = lines + 1;
                    if (Global.game_system.Objective_Mode[1] == this.turn)
                    {
                        Turn_Count.text = "Last Turn";
                        Turn_Count.offset.X = -Font_Data.text_width(Turn_Count.text, "FE7_Text_Info") / 2;
                        Turn_Count.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Green");
                    }
                    else
                    {
                        Turn_Count.text = this.turn.ToString();
                        Turn_Count.offset.X = 0;
                        Turn_Count.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
                    }
                    Turn_Max.text = Global.game_system.Objective_Mode[1].ToString();
                    Turn_Label.loc = new Vector2(0, 16 * (lines - 1));
                    Turn_Count.loc = new Vector2(0, 16 * (lines - 1));
                    Turn_Slash.loc = new Vector2(0, 16 * (lines - 1));
                    Turn_Max.loc = new Vector2(0, 16 * (lines - 1));
                    break;
            }
            refresh_positions();
        }

        protected void refresh_positions()
        {
            BOTTOM_Y = Math.Max(Config.WINDOW_HEIGHT / 2,
                Config.WINDOW_HEIGHT - (12 + (this.lines * 16) + (Global.game_options.controller == 0 ? 16 : 0)));
            if (Offscreen)
                loc.Y = TOP_Y - (this.lines * 16 + 16);
        }

        protected override void refresh()
        {
            refresh_graphic_object(Window_Img);
            refresh_graphic_object(Objective);

            copy_stereo(Turn_Label);
            copy_stereo(Turn_Count);
            copy_stereo(Turn_Slash);
            copy_stereo(Turn_Max);
            copy_stereo(Enemy_Label);
            copy_stereo(Enemy_Count);
        }
        #endregion

        #region Update
        public override void update()
        {
            Map_Info_State map_ready = map_check();
            if (map_ready == Map_Info_State.Refresh && Offscreen)
                move_on();
            if (!Offscreen)
            {
                if (is_on_top && is_on_right && Y_Move_List.Count == 0)
                    move_off();
                else if ((is_on_bottom || (Global.player.is_true_on_left() && is_self_on_bottom)) && Y_Move_List.Count == 0)
                    move_off();
            }
            else
            {
                if (Y_Move_List.Count == 0 && map_ready != Map_Info_State.Offscreen)
                    move_on();
            }
            update_position();
            refresh();
            // reset images //Yeti
        }
        #endregion

        #region Movement
        protected override void move_off()
        {
            if (is_self_on_top)
                move_off_top();
            else
                move_off_bottom();
        }

        protected virtual void move_on()
        {
            draw_images();
            if (Global.player.is_true_on_left() || Global.player.is_true_on_bottom())
                move_on_top();
            else
                move_on_bottom();
        }

        protected void move_off_top()
        {
            if (Offscreen) 
                return;
            Y_Move_List = new List<int> { TOP_Y - (this.lines * 16 + 16), TOP_Y - 48, TOP_Y - 40, TOP_Y - 24 };
            Offscreen = true;
        }

        protected void move_off_bottom()
        {
            if (Offscreen) 
                return;
            //Y_Move_List = new List<int> { BOTTOM_Y + 48, BOTTOM_Y + 48, BOTTOM_Y + 40, BOTTOM_Y + 24 }; //Debug
            Y_Move_List = new List<int> { BOTTOM_Y + (this.lines * 16 + 40), BOTTOM_Y + 56, BOTTOM_Y + 40, BOTTOM_Y + 24 };
            Offscreen = true;
        }

        protected void move_on_top()
        {
            Y_Move_List = new List<int> { TOP_Y - 0, TOP_Y - 8, TOP_Y - 16, TOP_Y - 24, TOP_Y - 40 };
            Offscreen = false;
        }

        protected void move_on_bottom()
        {
            Y_Move_List = new List<int> { BOTTOM_Y + 0, BOTTOM_Y + 8, BOTTOM_Y + 16, BOTTOM_Y + 24, BOTTOM_Y + 40 };
            Offscreen = false;
        }
        #endregion

        public override void draw(SpriteBatch sprite_batch)
        {
            Window_Img.draw(sprite_batch, new Vector2(-5, -5));
            Objective.draw(sprite_batch, -(NAME_LOC + new Vector2(42, -2)));
            if (Global.game_system.Objective_Text == "Secure Stairway")
            {
                Turn_Label.draw(sprite_batch, -(loc + TURN_LABEL_LOC + new Vector2(-2, -2)));
                Turn_Slash.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-2, -2)));
                Turn_Max.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-3 + 24, -2)));
                Turn_Count.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-3, -2)));
            }
            switch(Global.game_system.Objective_Mode[0])
            {
                // Rout
                case 1:
                    Enemy_Label.draw(sprite_batch, -(loc + ENEMY_LABEL_LOC + new Vector2(-2, -2)));
                    Enemy_Count.draw(sprite_batch, -(loc + ENEMY_LOC + new Vector2(-1, -2)));
                    break;
                // Time Limit
                case 2:
                    if (Global.game_system.Objective_Mode[1] != this.turn)
                    {
                        Turn_Label.draw(sprite_batch, -(loc + TURN_LABEL_LOC + new Vector2(-2, -2)));
                        Turn_Slash.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-2, -2)));
                        Turn_Max.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-3 + 24, -2)));
                        Turn_Count.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-3, -2)));
                    }
                    else
                        Turn_Count.draw(sprite_batch, -(loc + TURN_LOC + new Vector2(-3 + 16, -2)));
                    break;
            }
        }
    }
}