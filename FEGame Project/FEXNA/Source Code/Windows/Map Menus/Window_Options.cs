using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;

namespace FEXNA.Windows.Map
{
    class Window_Options : Map_Window_Base
    {
        #region Options
        public readonly static Options_Data[] OPTIONS_DATA = new Options_Data[] {
            new Options_Data { Label = "Animation", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 3, new string[] { "1", "Show animations" }),
                new KeyValuePair<int, string[]>(18, new string[] { "2", "Show animations on the player turn" }),
                new KeyValuePair<int, string[]>(30, new string[] { "Off", "Turn off combat animation" }),
                new KeyValuePair<int, string[]>(53, new string[] { "Solo", "Set animation for each unit manually" }) }},
            new Options_Data { Label = "Game Speed", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "Norm", "Set unit movement speed" }),
                new KeyValuePair<int, string[]>(31, new string[] { "Fast", "Set unit movement speed (fast)" }) }},
            new Options_Data { Label = "Text Speed",Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "Slow", "Set message speed (slow)" }),
                new KeyValuePair<int, string[]>(31, new string[] { "Norm", "Set message speed"}),
                new KeyValuePair<int, string[]>(62, new string[] { "Fast", "Set message speed (fast)" }),
                new KeyValuePair<int, string[]>(93, new string[] { "Max", "Set message speed (autoscroll)" }) }},
            new Options_Data { Label = "Combat", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "Basic", "Show basic Combat Info window" }),
                new KeyValuePair<int, string[]>(31, new string[] { "Detail", "Show detailed Combat Info window" }),
                new KeyValuePair<int, string[]>(70, new string[] { "OFF", "Turn Combat Info window off" }) }},
            new Options_Data { Label = "Unit", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "Detail", "Show detailed unit window" }),
                new KeyValuePair<int, string[]>(31, new string[] { "Panel", "Show normal unit window" }),
                new KeyValuePair<int, string[]>(62, new string[] { "Burst", "Show unit window with tail" }),
                new KeyValuePair<int, string[]>(93, new string[] { "OFF", "Turn unit window off" }) }},
            new Options_Data { Label = "Enemy Data", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "Basic", "Show basic enemy window" }),
                new KeyValuePair<int, string[]>(31, new string[] { "Detail", "Show detailed enemy window" }),
                new KeyValuePair<int, string[]>(70, new string[] { "OFF", "Turn enemy window off" }) }},
            new Options_Data { Label = "Terrain", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Turn Terrain window on or off" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Turn Terrain window on or off" }) }},
            new Options_Data { Label = "Show Objective", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Set Chapter Goal display" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Set Chapter Goal display" }) }},
            new Options_Data { Label = "Grid",
                Gauge = true, GaugeMin = 0, GaugeMax = 16, GaugeInterval = 1,
                GaugeWidth = 72, GaugeOffset = 24,
                Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "{0}", "Set grid display" }) }},
            new Options_Data { Label = "Range Preview", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Set move range preview display" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Set move range preview display" }) }},
            new Options_Data { Label = "HP Gauges", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "Basic", "Set HP Gauge display" }),
                new KeyValuePair<int, string[]>(31, new string[] { "Advanced", "Set HP Gauge display" }),
                new KeyValuePair<int, string[]>(80, new string[] { "Injured", "Set HP Gauge display (injured only)" }),
                new KeyValuePair<int, string[]>(120, new string[] { "OFF", "Set HP Gauge display" }) }},
            new Options_Data { Label = "Controller", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Show controller help" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Turn controller help off" }),
                new KeyValuePair<int, string[]>(46, new string[] { "Vintage", "Remove blank tiles around map" }) }},
            new Options_Data { Label = "Subtitle Help", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Set Easy/Help Scroll display" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Set Easy/Help Scroll display" }) }},
            new Options_Data { Label = "Autocursor", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Set cursor to start on main hero" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Set cursor to start on main hero" }), }},
                //new KeyValuePair<int, string[]>(46, new string[] { "Madelyn", "Set cursor to start on main hero" })
            new Options_Data { Label = "Autoend Turns", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "ON", "Set turn to end automatically" }),
                new KeyValuePair<int, string[]>(23, new string[] { "OFF", "Set turn to end automatically" }),
                new KeyValuePair<int, string[]>(46, new string[] { "Prompt", "Opens menu after last unit has moved" }) }},
            new Options_Data { Label = "Music",
                Gauge = true, GaugeMin = 0, GaugeMax = 100, GaugeInterval = 10,
                GaugeWidth = 64, GaugeOffset = 32,
                Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "{0}", "Asjust music volume" }) }},
            new Options_Data { Label = "Sound Effects",
                Gauge = true, GaugeMin = 0, GaugeMax = 100, GaugeInterval = 10,
                GaugeWidth = 64, GaugeOffset = 32,
                Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "{0}", "Adjust sound effects volume" }) }},
            new Options_Data { Label = "Window Color", Options = new KeyValuePair<int,string[]>[] {
                new KeyValuePair<int, string[]>( 0, new string[] { "1", "Change window color" }),
                new KeyValuePair<int, string[]>(18, new string[] { "2", "Change window color" }),
                new KeyValuePair<int, string[]>(33, new string[] { "3", "Change window color" }),
                new KeyValuePair<int, string[]>(48, new string[] { "4", "Change window color" }) }}
        };
        #endregion

        const int ROWS_AT_ONCE = (Config.WINDOW_HEIGHT - 80) / 16;
        const int OPTIONS_OFFSET = 40;
        const int CHOICES_OFFSET = OPTIONS_OFFSET + 128;

        protected int Row = 0;
        protected int Scroll = 0;
        protected Vector2 Offset = Vector2.Zero;
        protected bool SoloAnim_Call = false, SoloAnim_Allowed;
        protected bool Map_Info_Changed = false;
        protected FE_Banner Banner;
        protected Sprite Banner_Text;
        protected List<FE_Text>[] Option_Labels = new List<FE_Text>[OPTIONS_DATA.Length];
        protected FE_Text[] Option_Data = new FE_Text[OPTIONS_DATA.Length];
        protected Stat_Bar[] OptionGauges = new Stat_Bar[OPTIONS_DATA.Length];
        protected Hand_Cursor Other_Cursor;
        protected Sprite Icons;
        protected System_Color_Window Description_Window;
        protected FE_Text Description;
        protected SoloAnim_Button Solo_Icon;
        protected Page_Arrow Up_Page_Arrow, Down_Page_Arrow;

        Rectangle Data_Scissor_Rect = new Rectangle(0, 40, Config.WINDOW_WIDTH, ROWS_AT_ONCE * 16);
        RasterizerState Scissor_State = new RasterizerState { ScissorTestEnable = true };

        #region Accessors
        // This actually changes/gets the individual options
        protected byte column
        {
            get { return OPTIONS_DATA[Row].Gauge ? (byte)0 : Global.game_options.Data[Row]; }
            set { Global.game_options.Data[Row] = value; }
        }

        protected int column_max { get { return OPTIONS_DATA[Row].Options.Length; } }

        public bool soloanim_call
        {
            get { return SoloAnim_Call; }
            set { SoloAnim_Call = value; }
        }

        protected bool on_soloanim { get { return Row == (int)Options.Animation_Mode && column == (int)Animation_Modes.Solo; } }
        #endregion

        public Window_Options()
        {
            SoloAnim_Allowed = Global.scene.is_map_scene;
            initialize_sprites();
            update_black_screen();
        }

        protected void initialize_sprites()
        {
            // Black Screen
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Black_Screen.tint = new Color(0, 0, 0, 255);
            // Banner
            Banner = new FE_Banner();
            Banner.width = 120;
            Banner.loc = new Vector2(OPTIONS_OFFSET, 8);
            Banner.stereoscopic = Config.OPTIONS_BANNER_DEPTH;
            Banner_Text = new Sprite();
            Banner_Text.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Banner_Text");
            Banner_Text.src_rect = new Rectangle(0, 0, 96, 16);
            Banner_Text.loc = new Vector2(OPTIONS_OFFSET + 24 + 2, 8 + 8);
            Banner_Text.stereoscopic = Config.OPTIONS_BANNER_DEPTH;
            // Background
            Background = new Menu_Background();
            Background.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Option_Background");
            (Background as Menu_Background).vel = new Vector2(-0.25f, 0);
            (Background as Menu_Background).tile = new Vector2(3, 1);
            Background.stereoscopic = Config.MAPMENU_BG_DEPTH;
            // Labels
            for (int i = 0; i < OPTIONS_DATA.Length; i++)
            {
                Option_Labels[i] = new List<FE_Text>();

                Option_Labels[i].Add(new FE_Text());
                Option_Labels[i][0].loc = new Vector2(OPTIONS_OFFSET + 16, i * 16) + new Vector2(Data_Scissor_Rect.X, Data_Scissor_Rect.Y);
                Option_Labels[i][0].Font = "FE7_Text";
                Option_Labels[i][0].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
                Option_Labels[i][0].text = OPTIONS_DATA[i].Label;
                Option_Labels[i][0].stereoscopic = Config.OPTIONS_OPTIONS_DEPTH;
                for (int j = 0; j < OPTIONS_DATA[i].Options.Length; j++)
                {
                    Option_Labels[i].Add(new FE_Text());
                    Option_Labels[i][j + 1].loc = new Vector2(CHOICES_OFFSET + OPTIONS_DATA[i].Options[j].Key, i * 16) +
                        new Vector2(Data_Scissor_Rect.X, Data_Scissor_Rect.Y);
                    Option_Labels[i][j + 1].Font = "FE7_Text";
                    Option_Labels[i][j + 1].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Grey");
                    Option_Labels[i][j + 1].text = OPTIONS_DATA[i].Options[j].Value[0];
                    Option_Labels[i][j + 1].stereoscopic = Config.OPTIONS_OPTIONS_DEPTH;
                }
            }
            // Gauges
            for (int i = 0; i < OPTIONS_DATA.Length; i++)
                if (OPTIONS_DATA[i].Gauge)
                {
                    OptionGauges[i] = new Stat_Bar();
                    OptionGauges[i].offset = new Vector2(-2, -8);
                    OptionGauges[i].bar_width = OPTIONS_DATA[i].GaugeWidth;
                    OptionGauges[i].stereoscopic = Config.OPTIONS_OPTIONS_DEPTH;
            }
            // Data
            for (int i = 0; i < OPTIONS_DATA.Length; i++)
            {
                Option_Data[i] = new FE_Text();
                set_data(i);
                Option_Data[i].stereoscopic = Config.OPTIONS_OPTIONS_DEPTH;
            }
            // Cursor
            Cursor = new Hand_Cursor();
            Cursor.min_distance_y = 4;
            Cursor.override_distance_y = 16;
            Cursor.loc = cursor_loc();
            Cursor.stereoscopic = Config.OPTIONS_CURSOR_DEPTH;
            // Left Cursor
            Other_Cursor = new Hand_Cursor();
            Other_Cursor.loc = new Vector2(OPTIONS_OFFSET - 12, cursor_loc().Y);
            Other_Cursor.stereoscopic = Config.OPTIONS_CURSOR_DEPTH;
            // Icons
            Icons = new Sprite();
            Icons.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Options_Icons");
            Icons.loc = new Vector2(OPTIONS_OFFSET, 0) + new Vector2(Data_Scissor_Rect.X, Data_Scissor_Rect.Y);
            Icons.stereoscopic = Config.OPTIONS_OPTIONS_DEPTH;
            // Description Window
            Description_Window = new System_Color_Window();
            Description_Window.loc = new Vector2(60, 156);
            Description_Window.width = 200;
            Description_Window.height = 24;
            Description_Window.small = true;
            Description_Window.stereoscopic = Config.OPTIONS_DESC_DEPTH;
            // Description
            Description = new FE_Text();
            Description.loc = new Vector2(72, 160);
            Description.Font = "FE7_Text";
            Description.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Description.stereoscopic = Config.OPTIONS_DESC_DEPTH;
            // Solo Anim Button Icon
            Solo_Icon = new SoloAnim_Button();
            Solo_Icon.loc = new Vector2(CHOICES_OFFSET + 20 + OPTIONS_DATA[(int)Options.Animation_Mode].Options[(int)Animation_Modes.Solo].Key,
                16 * (int)Options.Animation_Mode) + new Vector2(Data_Scissor_Rect.X, Data_Scissor_Rect.Y);
            Solo_Icon.stereoscopic = Config.OPTIONS_OPTIONS_DEPTH;
            // Page Arrows
            Up_Page_Arrow = new Page_Arrow();
            Up_Page_Arrow.loc = new Vector2(CHOICES_OFFSET - 4, Data_Scissor_Rect.Y - 4);
            Up_Page_Arrow.angle = MathHelper.PiOver2;
            Up_Page_Arrow.stereoscopic = Config.OPTIONS_ARROWS_DEPTH;
            Down_Page_Arrow = new Page_Arrow();
            Down_Page_Arrow.loc = new Vector2(CHOICES_OFFSET - 4, Data_Scissor_Rect.Y + Data_Scissor_Rect.Height + 4);
            Down_Page_Arrow.mirrored = true;
            Down_Page_Arrow.angle = MathHelper.PiOver2;
            Down_Page_Arrow.stereoscopic = Config.OPTIONS_ARROWS_DEPTH;

            refresh_arrow_visibility();
            update_loc();
        }

        protected void refresh_arrow_visibility()
        {
            Up_Page_Arrow.visible = Scroll > 0;
            Down_Page_Arrow.visible = Scroll < OPTIONS_DATA.Length - (ROWS_AT_ONCE);
        }

        protected void set_data(int i)
        {
            int option_index;
            if (OPTIONS_DATA[i].Gauge)
                option_index = 0;
            else
                option_index = Global.game_options.Data[i];

            Option_Data[i].loc = new Vector2(CHOICES_OFFSET + OPTIONS_DATA[i].Options[option_index].Key, i * 16) +
                new Vector2(Data_Scissor_Rect.X, Data_Scissor_Rect.Y);
            Option_Data[i].Font = "FE7_Text";
            Option_Data[i].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");

            if (OPTIONS_DATA[i].Gauge)
            {
                OptionGauges[i].loc = new Vector2(CHOICES_OFFSET + OPTIONS_DATA[i].Options[option_index].Key, i * 16) +
                    new Vector2(Data_Scissor_Rect.X, Data_Scissor_Rect.Y);
                OptionGauges[i].fill_width = OPTIONS_DATA[i].GaugeWidth *
                    (Global.game_options.Data[i] - OPTIONS_DATA[i].GaugeMin) /
                    (OPTIONS_DATA[i].GaugeMax - OPTIONS_DATA[i].GaugeMin);

                Option_Data[i].text = Option_Labels[i][option_index + 1].text = string.Format(
                    OPTIONS_DATA[i].Options[option_index].Value[0], Global.game_options.Data[i]);
                Option_Data[i].offset = Option_Labels[i][option_index + 1].offset =
                    new Vector2(Option_Data[i].text_width, 0);
                Option_Data[i].draw_offset = Option_Labels[i][option_index + 1].draw_offset =
                    new Vector2(OPTIONS_DATA[i].gauge_offset, 0);
            }
            else
                Option_Data[i].text = Option_Labels[i][option_index + 1].text =
                    OPTIONS_DATA[i].Options[option_index].Value[0];
        }

        protected void update_loc()
        {
            set_data(Row);
            Cursor.set_loc(cursor_loc());
            Description.text = OPTIONS_DATA[Row].Options[column].Value[1];
        }

        #region Update
        protected void update_cursor_location()
        {
            int target_y = 16 * Scroll;
            if (Math.Abs(Offset.Y - target_y) <= 16 / 4)
                Offset.Y = target_y;
            if (Math.Abs(Offset.Y - target_y) <= 16)
                Offset.Y = Additional_Math.int_closer((int)Offset.Y, target_y, 16 / 4);
            else
                Offset.Y = ((int)(Offset.Y + target_y)) / 2;

            Cursor.update(); //Yeti
        }

        protected Vector2 cursor_loc()
        {
            int x = OPTIONS_DATA[Row].Options[column].Key;
            return new Vector2(x - 16 + CHOICES_OFFSET, (Row - Scroll) * 16 + Data_Scissor_Rect.Y);
        }

        public override void update()
        {
            base.update();
            update_cursor_location();
            Solo_Icon.update();
            Up_Page_Arrow.update();
            Down_Page_Arrow.update();
        }

        protected override void update_input()
        {
            if (!Closing && Black_Screen_Timer <= 0)
            {
                if (Global.Input.repeated(Inputs.Down))
                {
                    if (Row < OPTIONS_DATA.Length - 1)
                    {
                        if (move_down())
                            Global.game_system.play_se(System_Sounds.Menu_Move1);
                    }
                }
                else if (Global.Input.repeated(Inputs.Up))
                {
                    if (Row > 0)
                    {
                        if (move_up())
                            Global.game_system.play_se(System_Sounds.Menu_Move1);
                    }
                }
                else if (Global.Input.repeated(Inputs.Left))
                {
                    if (can_move_left)
                    {
                        Global.game_system.play_se(System_Sounds.Menu_Move2);
                        move_left();
                    }
                }
                else if (Global.Input.repeated(Inputs.Right))
                {
                    if (can_move_right)
                    {
                        Global.game_system.play_se(System_Sounds.Menu_Move2);
                        move_right();
                    }
                }
                else if (Global.Input.triggered(Inputs.B))
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    if (Map_Info_Changed && Global.scene.is_map_scene)
                        ((Scene_Map)Global.scene).create_info_windows();
                    close();
                }
                else if (Global.Input.triggered(Inputs.A) && on_soloanim && SoloAnim_Allowed)
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    SoloAnim_Call = true;
                }
            }
        }

        protected override void close()
        {
            base.close();
        }

        protected override void black_screen_switch()
        {
            Visible = !Visible;
            if (!Closing)
            {
            }
        }
        #endregion

        #region Movement
        protected bool move_down()
        {
            int row = Row;
            int distance = Global.Input.speed_up_input() ? ROWS_AT_ONCE : 1;
            Module.scroll_window_move_down(ref Row, ref Scroll, distance, ROWS_AT_ONCE, OPTIONS_DATA.Length, false);
            //Row++; //Debug
            //if (Row > (ROWS_AT_ONCE - 2) + Scroll && Scroll < DATA.Length - (ROWS_AT_ONCE))
            //    Scroll++;
            refresh_arrow_visibility();
            update_loc();
            Other_Cursor.loc = new Vector2(Other_Cursor.loc.X, cursor_loc().Y);
            return Row != row;
        }
        protected bool move_up()
        {
            int row = Row;
            int distance = Global.Input.speed_up_input() ? ROWS_AT_ONCE : 1;
            Module.scroll_window_move_up(ref Row, ref Scroll, distance, ROWS_AT_ONCE, OPTIONS_DATA.Length, false);
            //Row--; //Debug
            //if (Row - 1 < Scroll && Scroll > 0)
            //    Scroll--;
            refresh_arrow_visibility();
            update_loc();
            Other_Cursor.loc = new Vector2(Other_Cursor.loc.X, cursor_loc().Y);
            return Row != row;
        }

        private bool can_move_left
        {
            get
            {
                if (OPTIONS_DATA[Row].Gauge)
                    return Global.game_options.Data[Row] > OPTIONS_DATA[Row].GaugeMin;
                else
                    return column > 0;
            }
        }
        private bool can_move_right
        {
            get
            {
                if (OPTIONS_DATA[Row].Gauge)
                    return Global.game_options.Data[Row] < OPTIONS_DATA[Row].GaugeMax;
                else
                    return column < column_max - 1;
            }
        }

        protected void move_left()
        {
            if (OPTIONS_DATA[Row].Gauge)
                Global.game_options.Data[Row] = (byte)Math.Max(
                    Global.game_options.Data[Row] - OPTIONS_DATA[Row].GaugeInterval,
                    OPTIONS_DATA[Row].GaugeMin);
            else
                column--;
            refresh_options();
            update_loc();
        }
        protected void move_right()
        {
            if (OPTIONS_DATA[Row].Gauge)
                Global.game_options.Data[Row] = (byte)Math.Min(
                    Global.game_options.Data[Row] + OPTIONS_DATA[Row].GaugeInterval,
                    OPTIONS_DATA[Row].GaugeMax);
            else
                column++;
            refresh_options();
            update_loc();
        }
        #endregion

        protected void refresh_options()
        {
            switch (Row)
            {
                case (int)Options.Unit_Window:
                case (int)Options.Enemy_Window:
                case (int)Options.Terrain_Window:
                case (int)Options.Objective_Window:
                    Map_Info_Changed = true;
                    break;
                case (int)Options.Controller:
                    Map_Info_Changed = true;
                    if (Global.scene.is_map_scene)
                    {
                        Global.game_system.Instant_Move = true;
                        Global.game_map.center(Global.player.loc, true, forced: true);
                    }
                    break;
                case (int)Options.Auto_Turn_End:
                    Global.game_state.block_auto_turn_end();
                    //Global.game_state.update_autoend_turn(); //Debug
                    break;
                case (int)Options.Music_Volume:
                    Global.game_options.update_music_volum();
                    break;
                case (int)Options.Sound_Volume:
                    Global.game_options.update_sound_volume();
                    break;
            }
        }

        #region Draw
        protected override void draw_window(SpriteBatch sprite_batch)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Description_Window.draw(sprite_batch);
            Description.draw(sprite_batch);
            Banner.draw(sprite_batch);
            Banner_Text.draw(sprite_batch);
            sprite_batch.End();
            // Labels
            sprite_batch.GraphicsDevice.ScissorRectangle = Scene_Map.fix_rect_to_screen(Data_Scissor_Rect);
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, Scissor_State);
            foreach (FE_Text label in Option_Labels.SelectMany(x => x))
                label.draw(sprite_batch, Offset);
            foreach (FE_Text data in Option_Data)
                data.draw(sprite_batch, Offset);
            foreach (Stat_Bar gauge in OptionGauges.Where(x => x != null))
                gauge.draw(sprite_batch, Offset);
            Icons.draw(sprite_batch, Offset);
            if (on_soloanim && SoloAnim_Allowed)
                Solo_Icon.draw(sprite_batch);
            sprite_batch.End();
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            // Cursor
            Cursor.draw(sprite_batch);
            Other_Cursor.draw(sprite_batch);
            // Page Arrows
            Up_Page_Arrow.draw(sprite_batch);
            Down_Page_Arrow.draw(sprite_batch);
            sprite_batch.End();
        }
        #endregion
    }

    struct Options_Data
    {
        public string Label;
        public bool Gauge;
        public byte GaugeMin, GaugeMax, GaugeInterval;
        public int GaugeWidth, GaugeOffset;
        public KeyValuePair<int, string[]>[] Options;

        internal int gauge_offset { get { return GaugeWidth + GaugeOffset; } }
    }
}
