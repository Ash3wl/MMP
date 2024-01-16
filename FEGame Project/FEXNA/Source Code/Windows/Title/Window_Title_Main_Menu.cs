using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;
using FEXNA.Windows.UserInterface.Command;
using FEXNA.Windows.Command;

namespace FEXNA
{
#if !MONOGAME && DEBUG
    enum Main_Menu_States { Main_Menu, Metrics, Start_Game, Options, Test_Battle, Confirm_Quitting, Closing }
    enum Main_Menu_Selections { Resume, Start_Game, Options, Test_Battle, Quit }
#else
    enum Main_Menu_States { Main_Menu, Metrics, Start_Game, Options, Confirm_Quitting, Closing }
    enum Main_Menu_Selections { Resume, Start_Game, Options, Quit }
#endif
    enum Title_Actions { None, Resuming, New_Game, Load_Suspend, Load_Map_Save, World_Map, Quit }
    enum Start_Game_Options { Load_Suspend, Load_Map_Save, World_Map, Move, Copy, Delete }
    class Window_Title_Main_Menu
    {
        const int BLACK_SCEEN_FADE_TIMER = 15;
        const int BLACK_SCREEN_HOLD_TIMER = 8;
        const int PANEL_WIDTH = 208;

        private bool Active = false;
        private bool Visible = false;
        private bool LoadingSuspend = false, LoadingCheckpoint = false, DeletingFile = false;
        private Main_Menu_States State = Main_Menu_States.Main_Menu;
        private Title_Actions Action = Title_Actions.None;
        private Main_Menu_Selections Selection;
        private int Black_Screen_Fade_Timer = BLACK_SCEEN_FADE_TIMER;
        private int Black_Screen_Hold_Timer = BLACK_SCREEN_HOLD_TIMER;
        private int Black_Screen_Timer;
        private bool Enter_Pressed = false, Escape_Pressed = false;
        private int Options_Timer = 0;
        private List<int> Start_Game_Option_Redirect = new List<int>();
        private List<Inputs> locked_inputs = new List<Inputs>();

        private Menu_Background Background;
        private Sprite Black_Screen;
        private List<Title_Info_Panel> Panels = new List<Title_Info_Panel>();
        private List<FE_Text> Choices = new List<FE_Text>();
        private Window_Title_Start_Game Start_Game_Menu;
        private Window_Command Start_Game_Options_Menu;
        private Window_Confirmation Confirm_Window;
        private Parchment_Info_Window Metrics_Info_Window;
        private Game_Updated_Banner GameUpdated;
        private Window_Config_Options Options_Menu;
        private Sprite ChoiceBg;
#if !MONOGAME && DEBUG
        private FEXNA.Windows.Map.Window_Test_Battle_Setup Test_Battle_Window;
#endif

        #region Accessors
        public bool active
        {
            get { return Active; }
            set { Active = value; }
        }

        public bool resuming { get { return Action == Title_Actions.Resuming; } }

        public bool new_game { get { return Action == Title_Actions.New_Game; } }
        public bool load_suspend { get { return Action == Title_Actions.Load_Suspend; } }
        public bool load_map_save { get { return Action == Title_Actions.Load_Map_Save; } }
        public bool world_map { get { return Action == Title_Actions.World_Map; } }

        public bool quitting { get { return Action == Title_Actions.Quit; } }

        public bool closing { get { return State == Main_Menu_States.Closing; } }
        public bool closed { get { return Active && State == Main_Menu_States.Closing; } }

        public bool soft_rest_blocked { get { return State == Main_Menu_States.Options; } }
        #endregion

        public Window_Title_Main_Menu()
        {
            initialize();
            Selection = Global.suspend_file_info != null ? Main_Menu_Selections.Resume : Main_Menu_Selections.Start_Game;
            Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Green");
            set_black_screen_time();
        }

        protected virtual void set_black_screen_time()
        {
            Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
        }

        public void skip_fade()
        {
            if (Black_Screen_Timer > 0 && !Visible)
            {
                Black_Screen_Timer = 0;
                Visible = true;
                Active = true;
            }
        }

        protected void initialize()
        {
            if (Global.metrics_allowed && Global.metrics == Metrics_Settings.Not_Set)
                State = Main_Menu_States.Metrics;
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Black_Screen.tint = new Color(0, 0, 0, 0);
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            // Background
            Background = new Menu_Background();
            Background.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Status_Background");
            Background.vel = new Vector2(-0.25f, 0);
            Background.tile = new Vector2(3, 2);
            Background.stereoscopic = Config.MAPMENU_BG_DEPTH;
            // Resume
            Panels.Add(null);
            Panels.Add(null);

#if DEBUG && !MONOGAME
            for (int i = 0; i < 5; i++)
#else
            for(int i = 0; i < 4; i++)
#endif
            {
                Choices.Add(new FE_Text());
                Choices[i].Font = "FE7_Text";
                Choices[i].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
                Choices[i].stereoscopic = Config.TITLE_CHOICE_DEPTH;
            }
            Choices[1].text = "START GAME";
            Choices[2].text = "OPTIONS";
#if DEBUG && !MONOGAME
            Choices[3].text = "TEST BATTLE";
#endif
            Choices[Choices.Count - 1].text = "QUIT";

            WindowPanel window;
            window = new System_Color_Window();
            window.height = 12;
            window.offset = new Vector2(16, 8);
            window.loc = new Vector2(72, 40);
            window.width = PANEL_WIDTH;
            window.stereoscopic = Config.TITLE_MENU_DEPTH;
            ChoiceBg = window;

            Start_Game_Menu = new Window_Title_Start_Game(Global.latest_save_id);
            Start_Game_Menu.loc = new Vector2(56, 24);

            if (Global.is_update_found)
                GameUpdated = new Game_Updated_Banner(true);

            refresh_main_menu();
        }

        protected void refresh_main_menu()
        {
            if (Global.suspend_file_info != null)
            {
                Panels[0] = new Suspend_Info_Panel(true);
                Panels[0].loc = new Vector2(72, 40);
                Panels[0].stereoscopic = Config.TITLE_MENU_DEPTH;
                Choices[0].text = "RESUME";
            }
            else
            {
                Panels[0] = null;
                Choices[0].text = "";
            }
            Panels[1] = new StartGame_Info_Panel(
                Global.latest_save_id, PANEL_WIDTH, true);
            ((StartGame_Info_Panel)Panels[1]).active = Global.latest_save_id != -1;
            Panels[1].loc = new Vector2(72, 40 + (Global.suspend_file_info == null ? 0 : 16));
            Panels[1].stereoscopic = Config.TITLE_MENU_DEPTH;
            for (int i = 0; i < Choices.Count; i++)
            {
                Choices[i].loc = new Vector2(76, i * 16 + 28);
                Choices[i].draw_offset = new Vector2(
                    0, -(Global.suspend_file_info == null ? 1 : 0) * 16);
            }

            if (ChoiceBg is System_Color_Window)
            {
                (ChoiceBg as System_Color_Window).color_override = Global.current_save_info == null ? 0 :
                    Constants.Difficulty.DIFFICULTY_COLOR_REDIRECT[Global.current_save_info.difficulty];
            }
            ChoiceBg.draw_offset = new Vector2(0, - (Global.suspend_file_info == null ? 1 : 0) * 16);
        }

        public void update(KeyboardState key_state)
        {
            update(key_state, true);
        }
        public void update(KeyboardState key_state, bool input)
        {
            // Black Screen
            update_black_screen();
            
            if (Background != null)
                Background.update();
            if (GameUpdated != null)
                GameUpdated.update();

            if (Active && input)
                switch (State)
                {
                    case Main_Menu_States.Main_Menu:
                        if (GameUpdated == null && Global.is_update_found)
                            GameUpdated = new Game_Updated_Banner(false);


                        if (main_menu_mouse_selection())
                        {
                            select_main_menu_item();
                        }
                        else if (main_menu_touch_selection())
                        {
                            select_main_menu_item();
                        }
                        else
                        {
                            update_input();
                            if (enter_pressed(key_state) || Global.Input.triggered(Inputs.A) || Global.Input.triggered(Inputs.Start))
                            {
                                select_main_menu_item();
                            }
                            else if (Global.Input.triggered(Inputs.B))
                            {
                                Global.game_system.play_se(System_Sounds.Cancel);
                                Active = false;
                                State = Main_Menu_States.Closing;
                                Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
                                if (Black_Screen != null)
                                    Black_Screen.visible = true;
                            }
                        }
                        break;
                    case Main_Menu_States.Metrics:
                        update_metrics(key_state);
                        break;
                    case Main_Menu_States.Start_Game:
                        update_start_game(key_state);
                        break;
                    case Main_Menu_States.Options:
                        update_options(key_state);
                        break;
#if !MONOGAME && DEBUG
                    case Main_Menu_States.Test_Battle:
                        Test_Battle_Window.update();
                        if (Test_Battle_Window.closed)
                        {
                            Test_Battle_Window = null;
                            State = Main_Menu_States.Main_Menu;
                        }
                        break;
#endif
                    case Main_Menu_States.Confirm_Quitting:
                        update_confirm_quitting(key_state);
                        break;
                }
            Enter_Pressed = key_state.IsKeyDown(Keys.Enter);
            Escape_Pressed = key_state.IsKeyDown(Keys.Escape);
        }

        private bool main_menu_mouse_selection()
        {
            Rectangle rect = selection_hitbox((int)Selection);
            return Global.Input.mouse_clicked_rectangle(MouseButtons.Left, rect);
        }
        private bool main_menu_touch_selection()
        {
            Rectangle rect = selection_actual_hitbox((int)Selection);
            return Global.Input.gesture_rectangle(TouchGestures.Tap, rect) ||
                Global.Input.gesture_rectangle(TouchGestures.DoubleTap, rect);
        }

        private void select_main_menu_item()
        {
            switch (Selection)
            {
                case Main_Menu_Selections.Resume:
                    Action = Title_Actions.Resuming;
                    break;
                case Main_Menu_Selections.Start_Game:
                    Global.game_system.play_se(System_Sounds.Confirm);
                    State = Main_Menu_States.Start_Game;
                    Start_Game_Menu.file_id = Global.latest_save_id;
                    break;
                case Main_Menu_Selections.Options:
                    Global.game_system.play_se(System_Sounds.Confirm);
                    Options_Menu = new Window_Config_Options();
                    Options_Menu.stereoscopic = Config.TITLE_OPTIONS_DEPTH;
                    State = Main_Menu_States.Options;
                    break;
#if !MONOGAME && DEBUG
                case Main_Menu_Selections.Test_Battle:

                    Global.game_system.play_se(System_Sounds.Confirm);
                    Global.game_state.reset();
                    Global.game_map = new Game_Map();
                    Global.game_temp = new Game_Temp();
                    Global.game_battalions = new Game_Battalions();
                    Global.game_actors = new Game_Actors();
                    Test_Battle_Window = new Windows.Map.Window_Test_Battle_Setup();
                    State = Main_Menu_States.Test_Battle;
                    break;
#endif
                case Main_Menu_Selections.Quit:
                    State = Main_Menu_States.Confirm_Quitting;
                    string caption = "Are you sure you\nwant to quit?";

                    Confirm_Window = new Window_Confirmation();
                    Confirm_Window.set_text(caption);
                    Confirm_Window.add_choice("Yes", new Vector2(16, 32));
                    Confirm_Window.add_choice("No", new Vector2(56, 32));
                    int text_width = Font_Data.text_width(caption, "FE7_Convo");
                    text_width = text_width + 16 + (text_width % 8 == 0 ? 0 : (8 - text_width % 8));
                    Confirm_Window.size = new Vector2(Math.Max(88, text_width), 64);
                    Confirm_Window.loc = (new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) - Confirm_Window.size) / 2;
                    Confirm_Window.stereoscopic = Config.TITLE_CHOICE_DEPTH;
                    break;
            }
        }

        private void update_metrics(KeyboardState key_state)
        {
            if (Confirm_Window != null)
            {
                Confirm_Window.update();
                if (Metrics_Info_Window == null)
                {
                    if (Confirm_Window.is_ready)
                    {
                        bool proceed = false, accepted = false;
                        if (Confirm_Window.is_selected() ||  enter_pressed(key_state) ||
                            Global.Input.triggered(Inputs.Start))
                        {
                            proceed = true;
                            Global.game_system.play_se(System_Sounds.Confirm);
                            switch (Confirm_Window.index)
                            {
                                case 0:
                                    Global.metrics = Metrics_Settings.On;
                                    accepted = true;
                                    break;
                                case 1:
                                    Global.metrics = Metrics_Settings.Off;
                                    break;
                            }
                        }
                        else if (Confirm_Window.is_canceled() || escape_pressed(key_state))
                        {
                            Global.game_system.play_se(System_Sounds.Cancel);
                            proceed = true;
                            Global.metrics = Metrics_Settings.Off;
                        }
                        if (proceed)
                        {
                            Global.save_config = true;
                            Confirm_Window.active = false;
                            Confirm_Window.visible = false;

                            Metrics_Info_Window = new Parchment_Info_Window();
                            if (accepted)
                                Metrics_Info_Window.set_text(
@"Thank you for participating. Metrics
collection can be turned on or off
at any time from the options menu.");
                            else
                                Metrics_Info_Window.set_text(
@"If you change your mind, metrics
collection can be turned on or off
at any time from the options menu.");
                            Metrics_Info_Window.size = new Vector2(184, 64);
                            Metrics_Info_Window.loc = (new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) - Metrics_Info_Window.size) / 2;
                            Metrics_Info_Window.stereoscopic = Config.TITLE_CHOICE_DEPTH - 1;
                        }
                    }
                }
                else
                {
                    Metrics_Info_Window.update();
                    if (Metrics_Info_Window.is_ready)
                    {
                        if (enter_pressed(key_state) || Global.Input.triggered(Inputs.A) || Global.Input.triggered(Inputs.Start) ||
                            escape_pressed(key_state) || Global.Input.triggered(Inputs.B))
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Metrics_Info_Window = null;
                            Confirm_Window = null;
                            State = Main_Menu_States.Main_Menu;
                        }
                    }
                }
            }
            else
            {
                Confirm_Window = new Parchment_Confirm_Window();
                Confirm_Window.set_text(
@"Would you like to provide
anonymous usage data to
the developers for use in
improving this game?");
                Confirm_Window.add_choice("Yes", new Vector2(32, 64));
                Confirm_Window.add_choice("No", new Vector2(80, 64));
                Confirm_Window.size = new Vector2(136, 96);
                Confirm_Window.loc = (new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) - Confirm_Window.size) / 2;
                Confirm_Window.index = 0;
                Confirm_Window.stereoscopic = Config.TITLE_CHOICE_DEPTH - 1;
            }
        }

        protected void update_start_game(KeyboardState key_state)
        {
            Start_Game_Menu.update();
            if (Start_Game_Options_Menu != null)
                Start_Game_Options_Menu.update(Start_Game_Options_Menu.active);
            if (Confirm_Window != null)
                Confirm_Window.update();
            if (Start_Game_Options_Menu != null)
            {
                if (Confirm_Window != null)
                {
                    if (Confirm_Window.is_canceled() || escape_pressed(key_state) ||
                        Start_Game_Menu.is_canceled())
                    {
                        if (Confirm_Window.is_ready)
                        {
                            Global.game_system.play_se(System_Sounds.Cancel);
                            Confirm_Window = null;
                            Start_Game_Options_Menu.active = true;
                            if (LoadingSuspend || LoadingCheckpoint)
                                Start_Game_Menu.close_preview();
                            LoadingSuspend = false;
                            LoadingCheckpoint = false;
                            DeletingFile = false;
                        }
                    }
                    else if (Confirm_Window.is_selected())
                    {
                        if (Confirm_Window.is_ready)
                            confirm_window_choice();
                    }
                }
                else
                {
                    if (escape_pressed(key_state) ||
                        Start_Game_Options_Menu.is_canceled() ||
                        Start_Game_Menu.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Start_Game_Options_Menu = null;
                        Start_Game_Menu.active = true;
                    }
                    else if (Start_Game_Options_Menu.is_selected())
                    {
                        confirm_existing_file_menu();
                    }
                }
            }
            else
            {
                if (Start_Game_Menu.selecting_difficulty)
                {
                    // Selecting difficulty, starts a new file with the current difficulty and style
                    if (Start_Game_Menu.is_selected())
                        confirm_selecting_difficulty();
                    else if (escape_pressed(key_state) || Start_Game_Menu.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Start_Game_Menu.selecting_difficulty = false;
                    }
                }
                else if (Start_Game_Menu.selecting_style)
                {
                    // Selecting style
                    if (Start_Game_Menu.is_selected())
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Start_Game_Menu.selecting_difficulty = true;
                    }
                    else if (escape_pressed(key_state) || Start_Game_Menu.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Start_Game_Menu.selecting_style = false;
                    }
                }
                else if (Start_Game_Menu.waiting_for_io)
                {
                    if (!Global.copying && !Global.move_file && !Global.delete_file)
                    {
                        Start_Game_Menu.waiting_for_io = false;
                        Start_Game_Menu.refresh_page();
                    }
                }
                else if (Start_Game_Menu.copying)
                {
                    if (Start_Game_Menu.is_selected())
                        confirm_copy();
                    else if (escape_pressed(key_state) || Start_Game_Menu.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Start_Game_Menu.copying = false;
                    }
                }
                else if (Start_Game_Menu.moving_file)
                {
                    if (Start_Game_Menu.is_selected())
                        confirm_move();
                    else if (escape_pressed(key_state) || Start_Game_Menu.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Start_Game_Menu.moving_file = false;
                    }
                }
                else
                {
                    if (Start_Game_Menu.is_selected())
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        // New Game
                        if (Global.save_files_info == null ||
                            !Global.save_files_info.ContainsKey(Start_Game_Menu.file_id))
                        {
                            //Start_Game_Menu.selecting_style = true;
                            Start_Game_Menu.selecting_difficulty = true;
                        }
                        // Existing File
                        else
                        {
                            confirm_selecting_existing_file();
                        }
                    }
                    else if (escape_pressed(key_state) || Start_Game_Menu.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        State = Main_Menu_States.Main_Menu;
                        refresh_main_menu();
                    }
                }
            }
        }

        private void update_confirm_quitting(KeyboardState key_state)
        {
            Confirm_Window.update();
            if (Confirm_Window.is_ready)
            {
                if (Confirm_Window.is_selected())
                {
                    switch (Confirm_Window.index)
                    {
                        case 0:
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Action = Title_Actions.Quit;
                            break;
                        case 1:
                            Global.game_system.play_se(System_Sounds.Cancel);
                            State = Main_Menu_States.Main_Menu;
                            Confirm_Window = null;
                            break;
                    }
                }
                else if (Confirm_Window.is_canceled() || escape_pressed(key_state))
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    State = Main_Menu_States.Main_Menu;
                    Confirm_Window = null;
                }
            }
        }

        private void confirm_selecting_difficulty()
        {
            Global.game_system.play_se(System_Sounds.Confirm);
            Action = Title_Actions.New_Game;
            Global.save_file = new FEXNA.IO.Save_File();
            Global.save_file.Style = Start_Game_Menu.SelectedStyle;
            Global.save_file.Difficulty = (Difficulty_Modes)(int)Start_Game_Menu.selected_index();
            Global.game_options.reset_options();
            Global.start_game_file_id = Start_Game_Menu.file_id;
            Global.start_new_game = true;
        }
        private void confirm_move()
        {
            if (Global.save_files_info.ContainsKey(Start_Game_Menu.file_id))
                Global.game_system.play_se(System_Sounds.Buzzer);
            else
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                Global.start_game_file_id = Start_Game_Menu.move_file_id;
                Global.move_file = true;
                Global.move_to_file_id = Start_Game_Menu.file_id;
                Start_Game_Menu.moving_file = false;

                Start_Game_Menu.waiting_for_io = true;
            }
        }
        private void confirm_copy()
        {
            if (Global.save_files_info.ContainsKey(Start_Game_Menu.file_id))
                Global.game_system.play_se(System_Sounds.Buzzer);
            else
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                Global.start_game_file_id = Start_Game_Menu.move_file_id;
                Global.copying = true;
                Global.move_to_file_id = Start_Game_Menu.file_id;
                Start_Game_Menu.copying = false;

                Start_Game_Menu.waiting_for_io = true;
            }
        }
        private void confirm_selecting_existing_file()
        {
            List<string> strs = new List<string>();
            int width;

            Start_Game_Option_Redirect.Clear();
            strs.Add(string.IsNullOrEmpty(
                Global.save_files_info[Start_Game_Menu.file_id].chapter_id) ?
                "Start Game" : "Select Chapter");
            Start_Game_Option_Redirect.Add((int)Start_Game_Options.World_Map);
            if (Global.save_files_info[Start_Game_Menu.file_id].suspend_exists)
            {
                strs.Add("Continue");
                Start_Game_Option_Redirect.Add((int)Start_Game_Options.Load_Suspend);
            }
            if (Global.save_files_info[Start_Game_Menu.file_id].map_save_exists)
            {
                strs.Add("Checkpoint");
                Start_Game_Option_Redirect.Add((int)Start_Game_Options.Load_Map_Save);
            }
            if (Global.save_files_info.Count < Config.SAVES_PER_PAGE * Config.SAVE_PAGES)
            {
                strs.Add("Move");
                Start_Game_Option_Redirect.Add((int)Start_Game_Options.Move);
                strs.Add("Copy");
                Start_Game_Option_Redirect.Add((int)Start_Game_Options.Copy);
            }
            strs.Add("Delete");
            Start_Game_Option_Redirect.Add((int)Start_Game_Options.Delete);
            width = 80;

            Vector2 loc = Start_Game_Menu.loc +
                new Vector2(256 - width, Start_Game_Menu.selected_index() * 24 - 20);
            loc.Y = Math.Min(loc.Y, (Config.WINDOW_HEIGHT - 4) - (strs.Count + 1) * 16);
            Start_Game_Options_Menu = new Window_Command(loc, width, strs);
            Start_Game_Options_Menu.stereoscopic = Config.TITLE_CHOICE_DEPTH;
            Start_Game_Menu.active = false;
        }
        private void confirm_existing_file_menu()
        {
            Global.game_system.play_se(System_Sounds.Confirm);
            switch ((Start_Game_Options)Start_Game_Option_Redirect[Start_Game_Options_Menu.index])
            {
                case Start_Game_Options.Load_Suspend:
                    LoadingSuspend = true;
                    Start_Game_Menu.preview_suspend();
                    Start_Game_Options_Menu.active = false;
                    create_file_confirm_window("Load suspend?");
                    break;
                case Start_Game_Options.Load_Map_Save:
                    LoadingCheckpoint = true;
                    Start_Game_Menu.preview_checkpoint();
                    Start_Game_Options_Menu.active = false;
                    create_file_confirm_window("Load checkpoint?");
                    break;
                case Start_Game_Options.World_Map:
                    Action = Title_Actions.World_Map;
                    Global.start_game_file_id = Start_Game_Menu.file_id;
                    break;
                case Start_Game_Options.Move:
                    Start_Game_Options_Menu = null;
                    Start_Game_Menu.active = true;
                    Start_Game_Menu.moving_file = true;
                    break;
                case Start_Game_Options.Copy:
                    Start_Game_Options_Menu = null;
                    Start_Game_Menu.active = true;
                    Start_Game_Menu.copying = true;
                    break;
                case Start_Game_Options.Delete:
                    DeletingFile = true;
                    Start_Game_Options_Menu.active = false;
                    create_file_confirm_window("Are you sure?");
                    break;
            }
        }
        private void confirm_window_choice()
        {
            if (LoadingSuspend)
            {
                switch (Confirm_Window.index)
                {
                    case 0:
                        Action = Title_Actions.Load_Suspend;
                        Global.start_game_file_id = Start_Game_Menu.file_id;
                        break;
                    case 1:
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Confirm_Window = null;
                        Start_Game_Menu.close_preview();
                        Start_Game_Options_Menu.active = true;
                        break;
                }
                LoadingSuspend = false;
            }
            else if (LoadingCheckpoint)
            {
                switch (Confirm_Window.index)
                {
                    case 0:
                        Action = Title_Actions.Load_Map_Save;
                        Global.start_game_file_id = Start_Game_Menu.file_id;
                        break;
                    case 1:
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Confirm_Window = null;
                        Start_Game_Menu.close_preview();
                        Start_Game_Options_Menu.active = true;
                        break;
                }
                LoadingCheckpoint = false;
            }
            else if (DeletingFile)
            {
                switch (Confirm_Window.index)
                {
                    case 0:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Global.start_game_file_id = Start_Game_Menu.file_id;
                        Global.delete_file = true;

                        Start_Game_Menu.waiting_for_io = true;
                        Start_Game_Menu.active = true;
                        Confirm_Window = null;
                        Start_Game_Options_Menu = null;
                        break;
                    case 1:
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Confirm_Window = null;
                        Start_Game_Options_Menu.active = true;
                        break;
                }
                DeletingFile = false;
            }
        }

        private void create_file_confirm_window(string caption)
        {
            Confirm_Window = new Window_Confirmation();
            Vector2 loc = Start_Game_Options_Menu.loc + new Vector2(-8, 24 + Start_Game_Options_Menu.index * 16);
            if (loc.Y >= 136)
                loc.Y = Start_Game_Options_Menu.loc.Y + (-40 + Start_Game_Options_Menu.index * 16);
            Confirm_Window.loc = loc;
            Confirm_Window.set_text(caption);
            Confirm_Window.add_choice("Yes", new Vector2(16, 16));
            Confirm_Window.add_choice("No", new Vector2(56, 16));
            int text_width = Font_Data.text_width(caption, "FE7_Convo");
            text_width = text_width + 16 + (text_width % 8 == 0 ? 0 : (8 - text_width % 8));
            Confirm_Window.size = new Vector2(Math.Max(88, text_width), 48);
            Confirm_Window.index = 1;
            Confirm_Window.stereoscopic = Config.TITLE_CHOICE_DEPTH;
        }

        protected void update_options(KeyboardState key_state)
        {
            if (Options_Timer <= 0)
            {
                Options_Menu.update();
                // Options menu closing
                if (Options_Menu.closed)
                {
                    Options_Menu = null;
                    State = Main_Menu_States.Main_Menu;
                }
                else if (Options_Menu.is_ready)
                {
                    if (Options_Menu.accepting)
                    {
                        // Changing key config
                        if (Options_Menu.index >= Options_Menu.non_control_options)
                        {
                            Options_Menu.update_key_config(key_state);
                            if (!Options_Menu.accepting)
                            {
                                if (key_state.IsKeyDown(Keys.Escape))
                                    Global.game_system.play_se(System_Sounds.Cancel);
                                else
                                    Global.game_system.play_se(System_Sounds.Confirm);
                                Options_Timer = 2;
                            }
                        }
                        else
                        {
                            if (enter_pressed(key_state) || Global.Input.triggered(Inputs.A) || Global.Input.triggered(Inputs.Start))
                            {
                                Global.game_system.play_se(System_Sounds.Confirm);
                                Options_Menu.set();
                                Options_Menu.accepting = false;
                            }
                            else if (Global.Input.triggered(Inputs.B))
                            {
                                Global.game_system.play_se(System_Sounds.Cancel);
                                Options_Menu.reset();
                                Options_Menu.accepting = false;
                            }
                        }
                    }
                    else
                    {
                        if (enter_pressed(key_state) || Options_Menu.is_selected() || Global.Input.triggered(Inputs.Start))
                        {
                            if (Options_Menu.is_option_enabled)
                            {
                                Global.game_system.play_se(System_Sounds.Confirm);
                                Options_Menu.accepting = true;
                                Options_Menu.start_key_config(key_state);
                            }
                            else
                                Global.game_system.play_se(System_Sounds.Buzzer);
                        }
                        // Options menu begins closing
                        else if (Options_Menu.is_canceled())
                        {
                            Global.game_system.play_se(System_Sounds.Cancel);
                            Global.save_config = true;
                            Options_Menu.close();
                        }
                    }
                }
            }
            else
                Options_Timer--;
        }

        protected void update_input()
        {
            if (Global.Input.repeated(Inputs.Up) && !locked_inputs.Contains(Inputs.Up))
            {
                move_up();
            }
            if (Global.Input.repeated(Inputs.Down) && !locked_inputs.Contains(Inputs.Down))
            {
                move_down();
            }
            if (!Global.Input.pressed(Inputs.Up))
                locked_inputs.Remove(Inputs.Up);
            if (!Global.Input.pressed(Inputs.Down))
                locked_inputs.Remove(Inputs.Down);

            // Mouse movement
            if (Input.IsControllingOnscreenMouse)
                switch (State)
                {
                    case Main_Menu_States.Main_Menu:
                        for (int i = 0; i < Choices.Count; i++)
                        {
                            if ((int)Selection != i)
                            {
                                Rectangle rect = selection_hitbox(i);
                                if (rect.Contains(
                                    (int)Global.Input.mousePosition.X,
                                    (int)Global.Input.mousePosition.Y))
                                {
                                    if (i == (int)Main_Menu_Selections.Resume &&
                                            Global.suspend_file_info == null)
                                        i++;
                                    /* //Debug
                                    if (i > (int)Main_Menu_Selections.Resume &&
                                            Global.suspend_file_info == null)
                                        i--;*/
                                    move_to(i);
                                    break;
                                }
                            }
                        }
                        break;
                }
            // Touch movement
            else if (Input.ControlScheme == ControlSchemes.Touch)
                switch (State)
                {
                    case Main_Menu_States.Main_Menu:
                        for (int i = 0; i < Choices.Count; i++)
                        {
                            if ((int)Selection != i)
                            {
                                Rectangle rect = selection_actual_hitbox(i);
                                if (Global.Input.gesture_rectangle(
                                    TouchGestures.Tap, rect))
                                {
                                    if (i == (int)Main_Menu_Selections.Resume &&
                                            Global.suspend_file_info == null)
                                        i++;
                                    /* //Debug
                                    if (i > (int)Main_Menu_Selections.Resume &&
                                            Global.suspend_file_info == null)
                                        i--;*/
                                    move_to(i);
                                    break;
                                }
                            }
                        }
                        break;
                }
        }

        private Rectangle selection_hitbox(int i)
        {
            Vector2 loc = Choices[i].loc -
                new Vector2(ChoiceBg.offset.X, 0);
            if (Global.suspend_file_info == null)
            {
                if (i == (int)Main_Menu_Selections.Resume)
                    return new Rectangle((int)loc.X, (int)loc.Y, 0, 0);
                loc -= new Vector2(0, 16);
            }
            if (i > (int)Selection)
            {
                bool no_panel_at_selection =
                    Panels.Count <= (int)Selection || Panels[(int)Selection] == null;
                float panel_offset = no_panel_at_selection ?
                        0 : Panels[(int)Selection].height - 16;
                panel_offset = 0;
                loc -= new Vector2(0, panel_offset);
            }
            Rectangle rect = new Rectangle((int)loc.X, (int)loc.Y, PANEL_WIDTH, 16);
            return rect;
        }

        private Rectangle selection_actual_hitbox(int i)
        {
            Vector2 loc = Choices[i].loc -
                new Vector2(ChoiceBg.offset.X, 0);
            if (Global.suspend_file_info == null)
            {
                if (i == (int)Main_Menu_Selections.Resume)
                    return new Rectangle((int)loc.X, (int)loc.Y, 0, 0);
                loc -= new Vector2(0, 16);
            }
            if (i > (int)Selection)
            {
                bool no_panel_at_selection =
                    Panels.Count <= (int)Selection || Panels[(int)Selection] == null;
                float panel_offset = no_panel_at_selection ?
                        0 : Panels[(int)Selection].height - 16;
                loc += new Vector2(0, panel_offset);
            }
            int height = 16;
            if (i < Panels.Count && i == (int)Selection)
                height = Panels[i].height;
            Rectangle rect = new Rectangle((int)loc.X, (int)loc.Y, PANEL_WIDTH, height);
            return rect;
        }

        #region Movement
        protected virtual void move_down()
        {
            switch (State)
            {
                case Main_Menu_States.Main_Menu:
                    int selections = Enum_Values.GetEnumCount(typeof(Main_Menu_Selections));
                    move_to(((int)Selection + 1) % selections);

                    if ((int)Selection == selections - 1)
                        locked_inputs.Add(Inputs.Down);





                    /*Global.game_system.play_se(System_Sounds.Menu_Move1);

                    int selections = Enum_Values.GetEnumCount(typeof(Main_Menu_Selections));
                    Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
                    Selection = (Main_Menu_Selections)(((int)Selection + 1) % selections);
                    if (Global.suspend_file_info == null && Selection == Main_Menu_Selections.Resume)
                        Selection++;
                    Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Green");
                    if ((int)Selection == selections - 1)
                        locked_inputs.Add(Inputs.Down);*/
                    break;
            }
        }
        protected virtual void move_up()
        {
            switch (State)
            {
                case Main_Menu_States.Main_Menu:
                    int selections = Enum_Values.GetEnumCount(typeof(Main_Menu_Selections));
                    move_to(((int)Selection - 1 + selections) % selections);

                    if (Selection == Main_Menu_Selections.Resume ||
                            (Global.suspend_file_info == null && Selection == Main_Menu_Selections.Resume + 1))
                        locked_inputs.Add(Inputs.Up);

                    /*Global.game_system.play_se(System_Sounds.Menu_Move1);

                    int selections = Enum_Values.GetEnumCount(typeof(Main_Menu_Selections));
                    Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
                    Selection = (Main_Menu_Selections)(((int)Selection - 1 + selections) % selections);
                    if (Global.suspend_file_info == null && Selection == Main_Menu_Selections.Resume)
                        Selection = (Main_Menu_Selections)(selections - 1);
                    Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Green");
                    if (Selection == Main_Menu_Selections.Resume || (Global.suspend_file_info == null && Selection == Main_Menu_Selections.Resume + 1))
                        locked_inputs.Add(Inputs.Up);*/
                    break;
            }
        }

        protected virtual void move_to(int index)
        {
            switch (State)
            {
                case Main_Menu_States.Main_Menu:
                    Global.game_system.play_se(System_Sounds.Menu_Move1);

                    int selections = Enum_Values.GetEnumCount(typeof(Main_Menu_Selections));
                    Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");

                    int old_selection = (int)Selection;
                    Selection = (Main_Menu_Selections)index;
                    if (Global.suspend_file_info == null && Selection == Main_Menu_Selections.Resume)
                    {
                        if ((int)Selection > old_selection ||
                                (old_selection == selections - 1 && (int)Selection == 0))
                            Selection++;
                        else
                            Selection = (Main_Menu_Selections)
                                (((int)Selection + (int)Main_Menu_Selections.Quit) %
                                ((int)Main_Menu_Selections.Quit + 1));
                    }
                    Choices[(int)Selection].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Green");
                    break;
            }
        }
        #endregion

        public void cancel_resume()
        {
            Action = Title_Actions.None;
            Active = true;
        }

        protected bool enter_pressed(KeyboardState key_state)
        {
            return !Enter_Pressed && key_state.IsKeyDown(Keys.Enter);
        }

        protected bool escape_pressed(KeyboardState key_state)
        {
            return !Escape_Pressed && key_state.IsKeyDown(Keys.Escape);
        }

        protected void update_black_screen()
        {
            if (Black_Screen != null)
                Black_Screen.visible = Black_Screen_Timer > 0;
            if (Black_Screen_Timer > 0)
            {
                Black_Screen_Timer--;
                if (Black_Screen != null)
                {
                    if (Black_Screen_Timer > Black_Screen_Fade_Timer + (Black_Screen_Hold_Timer / 2))
                        Black_Screen.TintA = (byte)Math.Min(255,
                            (Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2) - Black_Screen_Timer) * (256 / Black_Screen_Fade_Timer));
                    else
                        Black_Screen.TintA = (byte)Math.Min(255,
                            Black_Screen_Timer * (256 / Black_Screen_Fade_Timer));
                }
                if (Black_Screen_Timer == Black_Screen_Fade_Timer + (Black_Screen_Hold_Timer / 2))
                {
                    black_screen_switch();
                }
                if (Black_Screen_Timer == 0)
                    Active = true;
            }
        }

        protected virtual void black_screen_switch()
        {
            Visible = !Visible;
        }

        #region Draw
        public void draw(SpriteBatch sprite_batch)
        {
            if (Visible)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                if (Background != null)
                    Background.draw(sprite_batch);
                sprite_batch.End();
                switch (State)
                {
                    case Main_Menu_States.Start_Game:
                        Start_Game_Menu.draw(sprite_batch);
                        if (Start_Game_Options_Menu != null)
                        {
                            Start_Game_Options_Menu.draw(sprite_batch);
                        }
                        if (Confirm_Window != null)
                            Confirm_Window.draw(sprite_batch);
                        break;
                    default:
                        if (GameUpdated != null)
                            GameUpdated.draw(sprite_batch);
                        switch (Selection)
                        {
                            case Main_Menu_Selections.Resume:
                                if (Panels[0] != null)
                                    Panels[0].Draw(sprite_batch);
                                break;
                            case Main_Menu_Selections.Start_Game:
                                if (Panels[1] != null)
                                {
                                    sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                                    Panels[1].Draw(sprite_batch);
                                    sprite_batch.End();
                                }
                                break;
                        }
                        sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                        for (int i = 0; i < Choices.Count && i <= (int)Selection; i++)
                        {
                            if (i != (int)Selection || i >= Panels.Count || Panels[i] == null)
                                if (!string.IsNullOrEmpty(Choices[i].text))
                                    ChoiceBg.draw(sprite_batch, new Vector2(0, i * -16));
                            Choices[i].draw(sprite_batch);
                        }
                        for (int i = (int)Selection + 1; i < Choices.Count; i++)
                        {
                            Vector2 panel_offset = -new Vector2(0, (Panels.Count <= (int)Selection || Panels[(int)Selection] == null) ?
                                    0 : Panels[(int)Selection].height - 16);
                            ChoiceBg.draw(sprite_batch, new Vector2(0, i * -16) + panel_offset);
                            Choices[i].draw(sprite_batch, panel_offset);
                        }
                        sprite_batch.End();
                        if (Confirm_Window != null)
                            Confirm_Window.draw(sprite_batch);
                        if (Metrics_Info_Window != null)
                            Metrics_Info_Window.draw(sprite_batch);
                        break;
                }
#if !MONOGAME && DEBUG
                if (Test_Battle_Window != null)
                    Test_Battle_Window.draw(sprite_batch);
#endif
                if (Options_Menu != null)
                    Options_Menu.draw(sprite_batch);
            }

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Black_Screen.draw(sprite_batch);
            sprite_batch.End();
        }
        #endregion
    }
}
