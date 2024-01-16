using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;
using FEXNA.Windows.Command;
using FEXNA_Library;

namespace FEXNA.Windows
{
    enum PrepCheckMapInputOptions { None, ViewMap, Formation, Options, Save }
    enum PrepCheckMapResults { None, StartChapter, Cancel, Info }

    class Window_Setup_CheckMap : Map.Map_Window_Base, ISelectionMenu
    {
        const int BLACK_SCEEN_FADE_TIMER = 16;
        const int BLACK_SCREEN_HOLD_TIMER = 40;
        const int MAP_DARKEN_TIME = 8;

        protected bool Active = false;
        protected bool StartingMap = false;
        protected int Map_Darken_Time = 0;
        protected Window_Command Command_Window;
        protected Sprite Map_Darken;
        protected Button_Description Start, B_Button, R_Button;

        private int SelectedIndex = -1;

        internal bool Canceled { get; private set; }

        #region Accessors
        public bool active
        {
            set
            {
                Active = value;
                if (Active)
                    Canceled = false;
            }
        }

        new public bool closed { get { return Black_Screen_Timer <= 0 && Map_Darken_Time >= MAP_DARKEN_TIME && Closing; } }

        new public bool visible { set { Visible = value; } }

        public bool starting_map { get { return StartingMap; } }

        public int index
        {
            get { return Command_Window.index; }
            set { Command_Window.immediate_index = value; }
        }

        public bool ready { get { return !Closing && Black_Screen_Timer <= 0 && Map_Darken_Time >= MAP_DARKEN_TIME; } }
        #endregion

        public Window_Setup_CheckMap()
        {
            initialize_sprites();
            Black_Screen_Timer = 1;
            update_black_screen();
        }

        protected override void set_black_screen_time()
        {
            Black_Screen_Fade_Timer = BLACK_SCEEN_FADE_TIMER;
            Black_Screen_Hold_Timer = BLACK_SCREEN_HOLD_TIMER;
            base.set_black_screen_time();
        }

        protected void initialize_sprites()
        {
            // Black Screen
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Black_Screen.tint = new Color(0, 0, 0, 255);
            // Map Darken
            Map_Darken = new Sprite();
            Map_Darken.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Map_Darken.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            update_map_darken_tint();
            // Command Window
            Command_Window = new Window_Command(new Vector2(Config.WINDOW_WIDTH / 2 - 40, 32),
                80, new List<string> { "View Map", "Formation", "Options", "Save" });
            Command_Window.text_offset = new Vector2(8, 0);
            Command_Window.glow = true;
            Command_Window.bar_offset = new Vector2(-8, 0);

            refresh_input_help();
        }

        protected void refresh_input_help()
        {
            /*Start = new Sprite();
            Start.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Start.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT) + new Vector2(-128, -16);
            Start.src_rect = new Rectangle(104, 40, 72, 16);
            Start.offset = new Vector2(0, 4);
            B_Button = new Sprite();
            B_Button.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            B_Button.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT) + new Vector2(-16, -16);
            B_Button.src_rect = new Rectangle(104, 104, 40, 16);
            B_Button.offset = new Vector2(0, 4);
            R_Button = new Sprite();
            R_Button.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            R_Button.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT) + new Vector2(80, -16);
            R_Button.src_rect = new Rectangle(104, 120, 40, 16);
            R_Button.offset = new Vector2(0, 4);*/
            Start = Button_Description.button(Inputs.Start,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(142, 41, 32, 16));
            Start.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT) + new Vector2(-128, -16);
            Start.offset = new Vector2(0, 3);
            B_Button = Button_Description.button(Inputs.B,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(123, 105, 24, 16));
            B_Button.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT) + new Vector2(-16, -16);
            B_Button.offset = new Vector2(0, 3);
            R_Button = Button_Description.button(Inputs.R,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(126, 122, 24, 16));
            R_Button.loc = new Vector2(Config.WINDOW_WIDTH / 2, Config.WINDOW_HEIGHT) + new Vector2(80, -16);
            R_Button.offset = new Vector2(0, 2);
        }

        #region Update
        public override void update()
        {
            if (Map_Darken_Time < MAP_DARKEN_TIME)
            {
                Map_Darken_Time++;
                update_map_darken_tint();
                if (Map_Darken_Time >= MAP_DARKEN_TIME)
                    Visible = !Closing;
            }
            bool input = Active && ready &&
                !(Global.scene as Scene_Map).confirming_something;
            Command_Window.update(input);
            update_ui(input);
            base.update();
        }

        private void update_ui(bool input)
        {
            reset_selected();

            if (Input.ControlSchemeSwitched)
                refresh_input_help();

            Start.Update(input);
            B_Button.Update(input);
            R_Button.Update(input);

            if (input)
            {
                if (Global.Input.triggered(Inputs.Start) ||
                    Start.consume_trigger(MouseButtons.Left) ||
                    Start.consume_trigger(TouchGestures.Tap))
                {
                    SelectedIndex = (int)PrepCheckMapResults.StartChapter;
                }
                else if (B_Button.consume_trigger(MouseButtons.Left) ||
                    B_Button.consume_trigger(TouchGestures.Tap))
                {
                    SelectedIndex = (int)PrepCheckMapResults.Cancel;
                }
                else if (R_Button.consume_trigger(MouseButtons.Left) ||
                    R_Button.consume_trigger(TouchGestures.Tap))
                {
                    SelectedIndex = (int)PrepCheckMapResults.Info;
                }
            }
        }

        public Maybe<int> selected_index()
        {
            if (SelectedIndex < 0)
                return Maybe<int>.Nothing;
            return SelectedIndex;
        }

        public bool is_selected()
        {
            return SelectedIndex >= 0;
        }

        public bool is_canceled()
        {
            return Canceled;
        }

        public void reset_selected()
        {
            SelectedIndex = -1;
            Canceled = false;
        }

        new internal PrepCheckMapInputOptions update_input()
        {
            if (Command_Window.is_canceled())
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Canceled = true;
            }
            else if (Command_Window.is_selected())
            {
                switch (Command_Window.selected_index())
                {
                    // View Map
                    case 0:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Global.game_map.highlight_test();
                        this.visible = false;
                        close();
                        return PrepCheckMapInputOptions.ViewMap;
                    // Formation
                    case 1:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        this.visible = false;
                        close();
                        return PrepCheckMapInputOptions.Formation;
                    // Options
                    case 2:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        return PrepCheckMapInputOptions.Options;
                    // Save
                    case 3:
                        return PrepCheckMapInputOptions.Save;
                }
            }
            return PrepCheckMapInputOptions.None;
        }

        protected void update_map_darken_tint()
        {
            Map_Darken.tint = new Color(0, 0, 0, (Closing ? (MAP_DARKEN_TIME - Map_Darken_Time) : Map_Darken_Time) * 128 / MAP_DARKEN_TIME);
        }

        new public void close()
        {
            close(false);
        }
        public void close(bool start)
        {
            StartingMap = start;
            Closing = true;
            if (StartingMap)
            {
                Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
                if (Black_Screen != null)
                    Black_Screen.visible = true;
            }
            else
            {
                Map_Darken_Time = 0;
            }
        }
        #endregion

        #region Draw
        public override void draw(SpriteBatch sprite_batch)
        {
            if (Visible || (Closing ? !StartingMap : Map_Darken_Time < MAP_DARKEN_TIME))
            {
                if (Visible || !(Closing && StartingMap))
                    draw_map_darken(sprite_batch);
                if (Visible)
                {
                    sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    // //Yeti
                    Start.Draw(sprite_batch);
                    B_Button.Draw(sprite_batch);
                    R_Button.Draw(sprite_batch);
                    sprite_batch.End();

                    Command_Window.draw(sprite_batch);
                }
            }

            base.draw(sprite_batch);
        }

        public void draw_map_darken(SpriteBatch sprite_batch)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Map_Darken.draw(sprite_batch);
            sprite_batch.End();
        }
        #endregion
    }
}
