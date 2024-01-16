using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;
using FEXNA.Graphics.Map;
using FEXNA.Windows;
using FEXNA.Windows.Command;
using FEXNA.Windows.Map;
using FEXNA.Windows.UserInterface.Command;

namespace FEXNA
{
    enum Worldmap_Phases { Fade_In, Command_Process, ConfirmingChapter, Controls_Fade, Worldmap_Event, Fade_Out, Return_To_Title }
    class Scene_Worldmap : Scene_Base
    {
        readonly static HashSet<string> HARD_MODE_BLOCKED = new HashSet<string> { };
        protected const int WIDTH = 120;

        protected Worldmap_Phases Phase = Worldmap_Phases.Fade_In;
        protected int Fade_Timer = Constants.WorldMap.WORLDMAP_FADE_TIME;
        protected Window_WorldMap_Data Data_Window;
        private int Timer = 0;
        protected int Mode_Switch_Timer = 0;
        private bool Zoomed_Map_Visible = false;
        private int Zoomed_Fade_Timer = 0;
        private Vector2 Offset = Vector2.Zero, Target_Loc;
        private float Scroll_Speed = Constants.WorldMap.WORLDMAP_EVENT_SCROLL_SPEED;
        private Character_Sprite Lord_Sprite;
        protected Window_Command_Worldmap Command_Window;
        private Window_Command_Headered ChapterCommandWindow;
        private Sprite Map, Minimap, Minimap_Backing, Zoomed_Out_Map;
        private World_Minimap_ViewArea ViewArea;
        //private Button_Description Options_Button, Unit_Window_Button, Manage_Button; //Debug
        private Button_Description CancelButton, DifficultyButton;
        private Window_Options Options_Window;
        private Window_Unit Unit_Window;
        private Window_Manage_Items Manage_Window;
        private WindowRankingsOverview RankingsWindow;
        private Window_Prep_Trade Trade_Window;
        private Window_Supply Supply_Window;
        private Window_Status Status_Window;
        private Parchment_Confirm_Window Confirm_Window;
        private Parchment_Info_Window Hard_Mode_Blocked_Window;
        private bool Quit_Confirming;
        private bool Classic;
        private Window_Command Previous_Chapter_Selection_Window;

        protected List<int> Index_Redirect = new List<int>();
        protected int Index;
        private int Previous_Chapter_Index = 0;
        private List<string> Valid_Previous_Chapters = new List<string>();
        private int Unit_Anim_Count;
        private bool UnitWindowAvailable, RankingWindowAvailable;

        private List<Flashing_Worldmap_Object> Worldmap_Objects = new List<Flashing_Worldmap_Object>();
        private Worldmap_Beacon Beacon;
        private List<Worldmap_Unit> Units = new List<Worldmap_Unit>();
        private List<Worldmap_Unit> Clearing_Units = new List<Worldmap_Unit>();
        private int Tracking_Unit = -1;

        #region Accessors
        public virtual int redirect { get { return Index_Redirect[Command_Window.index]; } }
        private FEXNA_Library.Data_Chapter redirect_chapter { get { return Global.chapter_by_index(redirect); } }

        public Vector2 target_loc
        {
            set
            {
                Target_Loc = value;
                Scroll_Speed = Constants.WorldMap.WORLDMAP_EVENT_SCROLL_SPEED;
                Tracking_Unit = -1;
                foreach (Worldmap_Unit unit in Units)
                    unit.remove_all_tracking();
            }
        }

        public float scroll_speed { set { Scroll_Speed = Math.Max(0.001f, value); } }

        public bool scrolling
        {
            get
            {
                if (Zoomed_Fade_Timer > 0)
                    return true; return Offset != Target_Loc;
            }
        }

        public bool units_moving
        {
            get
            {
                for (int i = 0; i < Units.Count; i++)
                    if (Units[i].moving)
                        return true;
                return false;
            }
        }

        protected override bool has_convo_scene_button { get { return false; } }

        private bool cancel_button_triggered
        {
            get
            {
                return CancelButton.consume_trigger(MouseButtons.Left) ||
                    CancelButton.consume_trigger(TouchGestures.Tap) ||
                    Global.Input.mouse_click(MouseButtons.Right);
            }
        }
        #endregion

        public Scene_Worldmap()
        {
            initialize_base();
            Scene_Type = "Scene_Worldmap";
            Global.game_map = null;

            Global.save_file.Difficulty = Global.game_system.Difficulty_Mode;
            Classic = Global.save_file.Style == Mode_Styles.Classic;
            set_chapters();
            initialize_images();
            Offset = Target_Loc = redirect_chapter.World_Map_Loc -
                Constants.WorldMap.WORLDMAP_MAP_SPRITE_OFFSET;

            Global.Chapter_Text_Content.Unload();
            Global.chapter_text = Global.Chapter_Text_Content.Load<Dictionary<string, string>>(@"Data/Text/Worldmap");
        }

        protected virtual void set_chapters()
        {
            Index = 0;
            Index_Redirect.Clear();
            for (int i = 0; i < Global.Chapter_List.Count; i++)
            {
                // If there is save data for a standalone chapter it must be playable
                if (!Classic && Global.data_chapters[Global.Chapter_List[i]].Standalone && Global.save_file.ContainsKey(Global.Chapter_List[i]))
                    Index_Redirect.Add(i);
                else
                {
                    // If all prior chapters have been completed
                    if (Global.save_file.chapter_available(Global.Chapter_List[i], Difficulty_Modes.Normal))
                        // If either it's not classic mode, or it is classic mode but there's no save data for the chapter
                        if (!(Classic && Global.save_file.ContainsKey(Global.Chapter_List[i])))
                            Index_Redirect.Add(i);
                }
            }
            // If no chapters have been played at all, then treat the file as classic mode to attempt to automatically select the first chapter
#if !DEBUG
            if (!Classic && Global.save_file.Count == 0)
                Classic = true;
#endif
            // Selects the chapter in Classic mode
            if (Classic)
            {
                // If any chapters have all of their followups already cleared, they're probably a gaiden that was skipped intentionally
                HashSet<int> chapters_with_completed_followups = new HashSet<int>(Index_Redirect.Where(y =>
                {
                    // Gets all chapters that follow off this chapter
                    var followups = Global.data_chapters
                        .Where(possible_followup_chapter =>
                            Global.save_file.previous_chapters(
                                possible_followup_chapter.Value.Id, Difficulty_Modes.Normal).Contains(Global.Chapter_List[y]));
                    // If there are any, and they've been completed
                    return followups.Any() && followups.All(x => Global.save_file.ContainsKey(x.Key));
                }));

                bool classic = true;
                // If all we have left are skipped gaidens, load every chapter up because they beat the game
                if (!Index_Redirect.Except(chapters_with_completed_followups).Any())
                {
                    Index_Redirect.Clear();
                    classic = false;
                }
                else
                {
                    Index_Redirect = Index_Redirect.Except(chapters_with_completed_followups).ToList();
                    chapters_with_completed_followups.Clear();
                }

                // If no chapters are valid for classic mode, reload chapters with classic off to try to find anything playable
                if (!Index_Redirect.Any())
                {
                    Classic = false;
                    set_chapters();
                    Classic = classic;
                }
                else
                {
                    // If only game starting chapters, add all of them that are viable and not beaten
                    if (Index_Redirect.All(x => !Global.data_chapters[Global.Chapter_List[x]].Prior_Chapters.Any()))
                    {
                        Index_Redirect = Global.data_chapters
                            .Where(x => !x.Value.Prior_Chapters.Any() &&
                                Global.save_file.chapter_available(x.Key, Difficulty_Modes.Normal))// && !Global.save_file.ContainsKey(x.Key)) //Debug
                            .Select(x => Global.Chapter_List.IndexOf(x.Key)).ToList();
                    }
                }

                // If no chapters have been played and there are multiple choices,
                // cancel classic because the player needs to select their first chapter
                if (Global.save_file.Count == 0 && Index_Redirect.Count > 1)
                {
                    Classic = false;
                }
                // If save data exists for all chapters, turn off classic
                else if (!Classic || Index_Redirect.All(x => Global.save_file.ContainsKey(Global.Chapter_List[x])))
                    Classic = false;
                else
                {
                    // If there's more than one chapter that can be selected
                    // First remove chapters that have no prior chapter at all, if possible
                    if (Index_Redirect.Count != 1)
                        if (Index_Redirect.Except(
                                Global.data_chapters
                                    .Where(x => !x.Value.Prior_Chapters.Any())
                                    .Select(x => Global.Chapter_List.IndexOf(x.Key))).Any())
                            Index_Redirect = Index_Redirect.Except(
                                Global.data_chapters
                                    .Where(x => !x.Value.Prior_Chapters.Any())
                                    .Select(x => Global.Chapter_List.IndexOf(x.Key))).ToList();
                    // Then select from among chapters that have followup chapters, because if they do they must be part of the main game right?
                    List<int> continuable_chapters = new List<int>();
                    if (Index_Redirect.Count != 1)
                        for (int i = 0; i < Index_Redirect.Count; i++)
                        {
                            foreach (FEXNA_Library.Data_Chapter possible_followup_chapter in Global.data_chapters.Values)
                                // If any chapter has the tested chapter as a previous chapter
                                if (Global.save_file.previous_chapters(possible_followup_chapter.Id, Difficulty_Modes.Normal).Contains(
                                        Global.Chapter_List[Index_Redirect[i]]))
                                //if (chapter.Prior_Chapters.Contains(Global.Chapter_List[Index_Redirect[i]])) //Debug
                                {
                                    continuable_chapters.Add(i);
                                    break;
                                }
                        }
                    if (Index_Redirect.Count != 1 && continuable_chapters.Count > 1)
                        Classic = false;
                    else
                    {
                        if (Index_Redirect.Count == 1)
                            Index = 0;
                        else
                            Index = continuable_chapters[0];
                    }
                }
            }
            // Jumps to the first unplayed chapter
            if (!Classic)
            {
                // If any chapters haven't been played yet, jump to the first one on the list
                var non_completed_chapters = Index_Redirect.Where(x => !Global.save_file.ContainsKey(Global.Chapter_List[x])).ToList();
                if (non_completed_chapters.Any())
                {
                    // If any chapters have all of their followups already cleared, they're probably a gaiden that was skipped intentionally
                    HashSet<int> chapters_with_completed_followups = new HashSet<int>(non_completed_chapters.Where(y =>
                    {
                        // Gets all chapters that follow off this chapter
                        var followups = Global.data_chapters
                            .Where(possible_followup_chapter =>
                                Global.save_file.previous_chapters(
                                    possible_followup_chapter.Value.Id, Difficulty_Modes.Normal).Contains(Global.Chapter_List[y]));
                        // If there are any, and they've been completed
                        return followups.Any() && followups.All(x => Global.save_file.ContainsKey(x.Key));
                    }));
                    // Remove chapters with completed followups, if there are any other chapters
                    if (chapters_with_completed_followups.Any() && non_completed_chapters.Except(chapters_with_completed_followups).Any())
                        non_completed_chapters = non_completed_chapters.Except(chapters_with_completed_followups).ToList();
                    Index = Index_Redirect.IndexOf(non_completed_chapters.First());
                }
                return;
                for (int i = 0; i < Index_Redirect.Count; i++)
                    if (!Global.save_file.ContainsKey(Global.Chapter_List[Index_Redirect[i]]))
                    {
                        Index = i;
                        break;
                    }
            }
        }

        protected void initialize_images()
        {
            // Command Window
            create_command_window();
            Index = redirect;
            // Data_Window
            Data_Window = new Window_WorldMap_Data();
            Data_Window.loc = new Vector2(4, 4);
            // Lord
            Lord_Sprite = new Character_Sprite();
            Lord_Sprite.draw_offset = new Vector2(0, 4); // (0, 8); //Debug
            Lord_Sprite.facing_count = 3;
            Lord_Sprite.frame_count = 3;
            Lord_Sprite.stereoscopic = Config.MAP_UNITS_DEPTH;
            Lord_Sprite.mirrored = Constants.Team.flipped_map_sprite(
                Constants.Team.PLAYER_TEAM);
            // Map
            Map = new Sprite(Global.Content.Load<Texture2D>(@"Graphics/Panoramas/Worldmap"));
            Map.stereoscopic = Config.MAP_MAP_DEPTH;
            Zoomed_Out_Map = new Sprite(Global.Content.Load<Texture2D>(@"Graphics/Panoramas/Worldmap"));
            Zoomed_Out_Map.loc = new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) / 2;
            Zoomed_Out_Map.offset = new Vector2(Zoomed_Out_Map.texture.Width, Zoomed_Out_Map.texture.Height) / 2;
            Zoomed_Out_Map.scale = new Vector2(
                Math.Max((float)(Config.WINDOW_WIDTH + Config.WMAP_ZOOMED_DEPTH * 4) / Zoomed_Out_Map.texture.Width,
                (float)(Config.WINDOW_HEIGHT) / Zoomed_Out_Map.texture.Height));//Debug
            Zoomed_Out_Map.opacity = 0;
            Zoomed_Out_Map.stereoscopic = Config.WMAP_ZOOMED_DEPTH;

            Minimap = new Sprite(Global.Content.Load<Texture2D>(@"Graphics/Panoramas/Worldmap"));
            Minimap.scale = Constants.WorldMap.WORLDMAP_MINIMAP_SCALE;
            Minimap.loc = new Vector2(Config.WINDOW_WIDTH - 1, Config.WINDOW_HEIGHT - 1) +
                Constants.WorldMap.WORLDMAP_MINIMAP_OFFSET;
            Minimap.offset = new Vector2(Minimap.texture.Width, Minimap.texture.Height);
            Minimap.stereoscopic = Config.WMAP_MINIMAP_DEPTH;

            Minimap_Backing = new Sprite(Global.Content.Load<Texture2D>(@"Graphics/White_Square"));
            Minimap_Backing.scale = new Vector2((((int)Math.Round(Minimap.scale.X * Minimap.texture.Width) + 2) / (float)Minimap_Backing.texture.Width),
                (((int)Math.Round(Minimap.scale.Y * Minimap.texture.Height) + 2) / (float)Minimap_Backing.texture.Height));
            Minimap_Backing.loc = new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) +
                Constants.WorldMap.WORLDMAP_MINIMAP_OFFSET;
            Minimap_Backing.offset = new Vector2(Minimap_Backing.texture.Width, Minimap_Backing.texture.Height);
            Minimap_Backing.tint = new Color(0, 0, 0, 255);
            Minimap_Backing.stereoscopic = Config.WMAP_MINIMAP_DEPTH;

            ViewArea = new World_Minimap_ViewArea(new Vector2((int)Math.Round(Config.WINDOW_WIDTH * Minimap.scale.X) + 2,
                (int)Math.Round(Config.WINDOW_HEIGHT * Minimap.scale.Y) + 2));
            ViewArea.loc = Minimap.loc - new Vector2((int)Math.Round(Minimap.scale.X * Minimap.texture.Width),
                (int)Math.Round(Minimap.scale.Y * Minimap.texture.Height)) - new Vector2(1, 1) -
                new Vector2((int)Math.Round(Config.WINDOW_WIDTH * Minimap.scale.X) + 2,
                (int)Math.Round(Config.WINDOW_HEIGHT * Minimap.scale.Y) + 2) / 2;
            ViewArea.stereoscopic = Config.WMAP_MINIMAP_DEPTH;

            refresh_input_help();
        }

        protected virtual void create_command_window()
        {
            List<string> strs = new List<string>();
            foreach (int i in Index_Redirect)
                strs.Add(Global.chapter_by_index(i).World_Map_Name);
            //Command_Window = new Window_Command(new Vector2(8, 56), 80, strs);
            //Command_Window = new Window_Command_Scroll(new Vector2(8, 60), 96, 6, strs); //was 56 y pos, 
            Command_Window = new Window_Command_Worldmap(
                new Vector2(8, 60), WIDTH, 6, strs);
            refresh_rank_images();

            Command_Window.tint = new Color(224, 224, 224, 224);
            Command_Window.glow = true;
            Command_Window.immediate_index = Index;
            Command_Window.refresh_scroll();
        }

        protected virtual void refresh_rank_images()
        {
            List<string> ranks = new List<string>(), hard_ranks = new List<string>();
            foreach (int i in Index_Redirect)
            {
                if (Global.save_file != null)
                {
                    if (Global.save_file.ContainsKey(Global.Chapter_List[i], Difficulty_Modes.Normal))
                        ranks.Add(Global.save_file.displayed_rank(Global.Chapter_List[i], Difficulty_Modes.Normal));
                    else
                        ranks.Add("");
                    if (Global.save_file.ContainsKey(Global.Chapter_List[i], Difficulty_Modes.Hard))
                        hard_ranks.Add(Global.save_file.displayed_rank(Global.Chapter_List[i], Difficulty_Modes.Hard));
                    else
                        hard_ranks.Add("");
                }
                else
                {
                    ranks.Add("");
                    hard_ranks.Add("");
                }
            }
            Command_Window.refresh_ranks(ranks, hard_ranks);
        }

        private void refresh_input_help()
        {
            CancelButton = Button_Description.button(Inputs.B, 16);
            CancelButton.description = "Cancel";
            //CancelButton.stereoscopic = ; //Yeti

            DifficultyButton = Button_Description.button(Inputs.X, 80);
            DifficultyButton.description = "Change Difficulty";

            /* //Debug
            Options_Button = Button_Description.button(Inputs.Select, 0);
            Options_Button.loc = Command_Window.loc + new Vector2(8, 16 * (1 + ((Window_Command_Scroll)Command_Window).rows) - 4);
            Options_Button.description = "Options";
            Unit_Window_Button = Button_Description.button(Inputs.R, 0);
            Unit_Window_Button.loc = Command_Window.loc + new Vector2(8 + 72, 16 * (1 + ((Window_Command_Scroll)Command_Window).rows) - 4);
            Unit_Window_Button.description = "Info";
            Manage_Button = Button_Description.button(Inputs.Start, 0);
            Manage_Button.loc = Command_Window.loc + new Vector2(8 + 120, 16 * (1 + ((Window_Command_Scroll)Command_Window).rows) - 4);
            Manage_Button.description = "Manage";*/
        }

        public void refresh()
        {
            refresh_data_panel();

            Lord_Sprite.loc = redirect_chapter.World_Map_Loc;
            Lord_Sprite.texture = Scene_Map.get_team_map_sprite(
                Constants.Team.PLAYER_TEAM,
                Global.game_actors[redirect_chapter.World_Map_Lord_Id].map_sprite_name);

            if (Lord_Sprite.texture != null)
                Lord_Sprite.offset = new Vector2(
                    (Lord_Sprite.texture.Width / Lord_Sprite.frame_count) / 2,
                    (Lord_Sprite.texture.Height / Lord_Sprite.facing_count) - 8);
        }

        protected virtual void refresh_data_panel()
        {
            if (!hard_mode_enabled(redirect)) // if hard mode not available for this chapter //Yeti
            {
                Global.save_file.Difficulty = Global.game_system.Difficulty_Mode = Difficulty_Modes.Normal;
                Data_Window.set_mode(Global.game_system.Difficulty_Mode, false);
            }
            else
                Data_Window.set_mode(Global.game_system.Difficulty_Mode,
                    Global.save_file.Style != Mode_Styles.Classic);
            refresh_data();
        }

        protected virtual void refresh_data(int previous_chapter = -1)
        {
            Difficulty_Modes difficulty = Global.game_system.Difficulty_Mode;

            if (previous_chapter == -1)
            {
                Previous_Chapter_Index = -1;
                Valid_Previous_Chapters.Clear();
                if (redirect_chapter.Prior_Chapters.Count > 0)
                {
                    Valid_Previous_Chapters = Global.save_file.valid_previous_chapters(Global.Chapter_List[redirect], Global.save_file.Difficulty);
                    Previous_Chapter_Index = Valid_Previous_Chapters.Count - 1;
                }
            }
            else
                Previous_Chapter_Index = previous_chapter;

            if (redirect_chapter.Prior_Chapters.Count == 0 || Global.chapter_by_index(redirect).Standalone)
            //if (Global.chapter_by_index(redirect).Standalone) //Debug
            {
                Global.game_system = new Game_System();
                Global.game_battalions = new Game_Battalions();
                Global.game_actors = new Game_Actors();
                Global.game_system.Difficulty_Mode = difficulty;

                Data_Window.set(Global.chapter_by_index(redirect).World_Map_Name, Global.chapter_by_index(redirect).World_Map_Lord_Id,
                    Global.chapter_by_index(redirect).Preset_Data);
            }
            else
            {
                load_data();
                Global.game_battalions.current_battalion = Global.chapter_by_index(redirect).Battalion;
                Global.game_actors.heal_battalion();
                // Not sure why this happens //Yeti
                // what conflicts are caused by using the loaded system //Yeti
                Game_System system = Global.game_system;
                Global.game_system = new Game_System();
                Global.game_system.Difficulty_Mode = difficulty;
                // For now, setting the event data so it shows up in the monitor //Yeti
                Global.game_system.set_event_data(system.SWITCHES, system.VARIABLES);

                Data_Window.set(Global.chapter_by_index(redirect).World_Map_Name, Global.chapter_by_index(redirect).World_Map_Lord_Id,
                    new FEXNA_Library.Preset_Chapter_Data
                    {
                        Lord_Lvl = Global.game_actors[Global.chapter_by_index(redirect).World_Map_Lord_Id].level,
                        Units = Global.chapter_by_index(redirect).Preset_Data.Units + Global.battalion.actors.Count,
                        Gold = Global.chapter_by_index(redirect).Preset_Data.Gold + Global.battalion.gold,
                        Playtime = system.total_play_time
                    });
            }
            Global.save_file.Difficulty = Global.game_system.Difficulty_Mode;

            UnitWindowAvailable = Global.game_system.Style != Mode_Styles.Classic &&
                Global.battalion != null && Global.battalion.actors.Any();
            RankingWindowAvailable = Global.save_file.ContainsKey(
                Global.Chapter_List[redirect], Global.game_system.Difficulty_Mode);

            refresh_input_help();
        }

        private void load_data()
        {
            load_data(redirect_chapter.Prior_Chapters);
        }
        private void load_data(List<string> prior_chapters)
        {
            //Global.save_file.load_data(Valid_Previous_Chapters[Previous_Chapter_Index], Global.game_system.Difficulty_Mode,
            //    prior_chapters[prior_chapters.Count - 1]);
            Global.save_file.load_data(Global.Chapter_List[redirect], Global.game_system.Difficulty_Mode, Valid_Previous_Chapters[Previous_Chapter_Index], "");
            //Global.save_file.data[prior_chapters[prior_chapters.Count - 1] + Global.game_system.difficulty_append].load_data(); //Debug again
            //Global.save_file.data[Global.Chapter_List[redirect] + Global.game_system.difficulty_append].load_data(); //Debug
        }

        protected virtual bool hard_mode_enabled(int index)
        {
            if (Global.chapter_by_index(index).Standalone)
                return true;

            return Global.save_file.chapter_available(Global.Chapter_List[index], Difficulty_Modes.Hard);
        }

        public virtual void set_chapter(string id)
        {
            if (Index_Redirect.Any(x => Global.Chapter_List[x] == id))
            {
                int i = Index_Redirect.FindIndex(x => Global.Chapter_List[x] == id);
                set_chapter(i);
                return;
            }

            /* //Debug
            for (int i = 0; i < Index_Redirect.Count; i++)
            {
                if (Global.Chapter_List[Index_Redirect[i]] == id)
                {
                    set_chapter(i);
                    return;
                }
            }*/
            refresh();
        }
        protected void set_chapter(int index)
        {
            Command_Window.index = index;
            Command_Window.refresh_scroll();
            refresh();
            Offset = Target_Loc =
                Global.chapter_by_index(redirect).World_Map_Loc -
                Constants.WorldMap.WORLDMAP_MAP_SPRITE_OFFSET;
        }

        protected virtual void start_chapter(string chapter, List<string> prior_chapters, bool standalone)
        {
            Difficulty_Modes difficulty = Global.game_system.Difficulty_Mode;
            if (prior_chapters.Count == 0 || standalone)
            {
                Global.game_system.reset();
                Global.game_system.reset_event_variables();
                //Event_Processor.reset_variables(); //Debug
                int battalion_index = Global.data_chapters[chapter].Battalion;
                Global.game_battalions.add_battalion(battalion_index);
                Global.game_battalions.current_battalion = battalion_index;
            }
            else
            {
                load_data(prior_chapters);
                Global.game_actors.heal_battalion();
                Global.battalion.refresh_deployed();
            }
            Global.game_system.Difficulty_Mode = difficulty;
            if (Global.game_system.Style != Mode_Styles.Classic)
                Global.save_file.Difficulty = Global.game_system.Difficulty_Mode;
            Global.game_system.New_Chapter_Id = chapter;
            Global.game_system.new_chapter(prior_chapters, chapter,
                Valid_Previous_Chapters.Count == 0 ? "" : Valid_Previous_Chapters[Previous_Chapter_Index]);
            Global.game_temp = new Game_Temp();
            Global.save_file = null;
            Global.scene_change("Start_Chapter");
        }

        #region Update
        public override void update()
        {
            update_message();
            update_data();
            Player.update_anim();

            if (Input.ControlSchemeSwitched)
                refresh_input_help();

            if (update_soft_reset())
                return;

            // Manage Items
            if (Manage_Window != null)
                update_manage_items();
            // Unit Window
            else if (Unit_Window != null)
                update_unit();
            // Options
            else if (Options_Window != null)
                update_options();
            // Rankings
            else if (RankingsWindow != null)
                update_rankings();
            // Command Window
            else
                update_worldmap_command();

            update_loc();
        }

        private void update_manage_items()
        {
            Command_Window.update(false);

            Manage_Window.update();
            if (Status_Window != null)
            {
                Status_Window.update();
                update_status_menu();
                if (Status_Window == null)
                    Manage_Window.active = true;
            }
            else if (Trade_Window != null)
            {
                Trade_Window.update();
                update_trade_menu();
            }
            else if (Supply_Window != null)
            {
                Supply_Window.update();
                update_supply_menu();
            }
            else
            {
                update_manage_menu();
            }
        }
        private void update_unit()
        {
            Command_Window.update(false);

            if (Global.game_temp.status_menu_call)
                open_status_menu();
            if (Status_Window != null)
            {
                Status_Window.update();
                update_status_menu();
            }
            else
            {
                Unit_Window.update();
                update_unit_menu();
            }
        }
        private void update_options()
        {
            Command_Window.update(false);

            Options_Window.update();
            update_options_menu();
        }
        private void update_rankings()
        {
            Command_Window.update(false);

            RankingsWindow.update();
            update_rankings_menu();
        }
        private void update_worldmap_command()
        {
            Command_Window.update(
                Confirm_Window == null &&
                Previous_Chapter_Selection_Window == null &&
                Phase == Worldmap_Phases.Command_Process);
            CancelButton.Update(Confirm_Window == null &&
                (Phase == Worldmap_Phases.Command_Process ||
                Phase == Worldmap_Phases.ConfirmingChapter));
            DifficultyButton.Update(Confirm_Window == null &&
                (Phase == Worldmap_Phases.Command_Process ||
                Phase == Worldmap_Phases.ConfirmingChapter) &&
                can_change_difficulty());

            switch (Phase)
            {
                case Worldmap_Phases.Fade_In:
                    switch (Timer)
                    {
                        default:
                            if (Fade_Timer > 0)
                                Fade_Timer--;
                            if (Fade_Timer == Constants.WorldMap.WORLDMAP_FADE_TIME / 4)
                                if (!Classic)
                                    Global.Audio.play_bgm(Constants.WorldMap.WORLDMAP_THEME);
                            if (Fade_Timer == 0)
                                Phase = Worldmap_Phases.Command_Process;
                            break;
                    }
                    break;
                case Worldmap_Phases.Command_Process:
                    if (Classic)
                    {
                        select_chapter_fade();
                    }
                    else
                    {
                        if (Confirm_Window != null)
                        {
                            Confirm_Window.update();
                            update_confirm();
                        }
                        else
                        {
                            Target_Loc = Global.chapter_by_index(redirect).World_Map_Loc -
                                Constants.WorldMap.WORLDMAP_MAP_SPRITE_OFFSET;
                            update_command();
                        }
                    }
                    break;
                case Worldmap_Phases.ConfirmingChapter:
                    ChapterCommandWindow.update(
                        Confirm_Window == null &&
                        Previous_Chapter_Selection_Window == null);

                    if (Previous_Chapter_Selection_Window != null)
                        Previous_Chapter_Selection_Window.update(Confirm_Window == null);
                    if (Confirm_Window != null)
                    {
                        Confirm_Window.update();
                        update_confirm();
                    }
                    else if (Previous_Chapter_Selection_Window != null)
                    {
                        update_previous_chapter_selection();
                    }
                    else
                    {
                        update_chapter_selected();
                    }
                    break;
                case Worldmap_Phases.Controls_Fade:
                    if (Hard_Mode_Blocked_Window != null)
                    {
                        Hard_Mode_Blocked_Window.update();
                        if (Hard_Mode_Blocked_Window.is_ready)
                            if (Global.Input.triggered(Inputs.A))
                            {
                                Global.game_system.play_se(System_Sounds.Confirm);
                                Hard_Mode_Blocked_Window = null;
                            }
                    }
                    else
                    {
                        if (Fade_Timer > 0)
                        {
                            Fade_Timer--;
                            Data_Window.loc -= new Vector2(1, 0);
                            /* //Debug
                            Options_Button.loc -= new Vector2(1, 0);
                            Unit_Window_Button.loc -= new Vector2(1, 0);
                            Manage_Button.loc -= new Vector2(1, 0);*/
                            CancelButton.loc -= new Vector2(1, 0);
                            DifficultyButton.loc -= new Vector2(1, 0);
                            Command_Window.loc -= new Vector2(1, 0);
                            if (ChapterCommandWindow != null)
                                ChapterCommandWindow.loc -= new Vector2(1, 0);
                        }
                        if (Fade_Timer == 0 && !scrolling)
                            start_chapter_worldmap_event();
                    }
                    break;
                case Worldmap_Phases.Worldmap_Event:
                    if (!Global.game_system.is_interpreter_running)
                    {
                        Phase = Worldmap_Phases.Fade_Out;
                        Fade_Timer = Constants.WorldMap.WORLDMAP_FADE_TIME;
                        Global.Audio.bgm_fade(Constants.WorldMap.WORLDMAP_FADE_TIME);
                    }
                    break;
                case Worldmap_Phases.Fade_Out:
                    if (Fade_Timer > 0)
                        Fade_Timer--;
                    if (Fade_Timer == 0)
                        start_chapter(Global.Chapter_List[redirect],
                            redirect_chapter.Prior_Chapters, Global.chapter_by_index(redirect).Standalone);
                    break;
                case Worldmap_Phases.Return_To_Title:
                    if (Fade_Timer > 0)
                        Fade_Timer--;
                    if (Fade_Timer == 0)
                        Global.scene_change("Scene_Title_Load");
                    break;
            }
            Data_Window.update();
            ViewArea.update();
            update_frame();
            update_event_objects();
        }

        protected void update_previous_chapter_selection()
        {
            if (Previous_Chapter_Index != Previous_Chapter_Selection_Window.index)
                refresh_data(Previous_Chapter_Selection_Window.index);

            if (Previous_Chapter_Selection_Window.is_canceled())
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Previous_Chapter_Selection_Window = null;
                Command_Window.active = true;
            }
            else if (Previous_Chapter_Selection_Window.is_selected())
            {
                Previous_Chapter_Index =
                    Previous_Chapter_Selection_Window.selected_index();
                Previous_Chapter_Selection_Window.active = false;
                select_chapter();
                if (Confirm_Window == null)
                    Previous_Chapter_Selection_Window = null;
            }
        }

        protected void update_confirm()
        {
            if (Confirm_Window.is_ready)
            {
                if (Confirm_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Confirm_Window = null;
                    if (Previous_Chapter_Selection_Window != null)
                        Previous_Chapter_Selection_Window.active = true;
                    else
                        Command_Window.active = true;
                }
                else if (Confirm_Window.is_selected())
                {
                    if (Confirm_Window.index == 0)
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        if (Quit_Confirming)
                        {
                            Phase = Worldmap_Phases.Return_To_Title;
                            Fade_Timer = Constants.WorldMap.WORLDMAP_FADE_TIME;
                            Global.Audio.bgm_fade(Constants.WorldMap.WORLDMAP_FADE_TIME);
                            Confirm_Window.active = false;
                        }
                        else
                        {
                            Global.delete_map_save = true;
                            select_chapter_fade();
                            Confirm_Window = null;
                            Previous_Chapter_Selection_Window = null;
                        }
                    }
                    else
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Confirm_Window = null;
                        if (Previous_Chapter_Selection_Window != null)
                            Previous_Chapter_Selection_Window.active = true;
                        else
                            Command_Window.active = true;
                    }
                }
            }
        }

        protected void update_event_objects()
        {
            Flashing_Worldmap_Object.update_flash();
            if (Beacon != null)
                Beacon.update();
            foreach (Flashing_Worldmap_Object sprite in Worldmap_Objects)
                sprite.update();
            int i = 0;
            while (i < Clearing_Units.Count)
            {
                Clearing_Units[i].update();
                if (Clearing_Units[i].finished)
                    Clearing_Units.RemoveAt(i);
                else
                    i++;
            }
            i = 0;
            while (i < Units.Count)
            {
                Units[i].update();
                if (Units[i].tracking)
                {
                    Tracking_Unit = i;
                }
                if (Tracking_Unit == i)
                    update_tracking_unit();
                if (Units[i].is_removed)
                {
                    if (i == Tracking_Unit)
                        Tracking_Unit = -1;
                    else
                        Tracking_Unit--;
                    Clearing_Units.Add(Units[i]);
                    Units.RemoveAt(i);
                }
                else
                    i++;
            }
            if (Zoomed_Fade_Timer > 0)
            {
                Zoomed_Fade_Timer--;
                Zoomed_Out_Map.opacity = 256 *
                    (Zoomed_Map_Visible ?
                        (Constants.WorldMap.WORLDMAP_ZOOM_FADE_TIME - Zoomed_Fade_Timer) :
                        Zoomed_Fade_Timer) /
                    Constants.WorldMap.WORLDMAP_ZOOM_FADE_TIME;
            }
        }

        readonly static Vector2 TRACKING_OFFSET = new Vector2(48, 24);
        protected void update_tracking_unit()
        {
            if (!Units[Tracking_Unit].moving)
            {
                Tracking_Unit = -1;
                return;
            }
            // X
            if (Units[Tracking_Unit].loc.X > Offset.X + TRACKING_OFFSET.X)
            {
                if (Units[Tracking_Unit].loc.X > Worldmap_Unit.tracking_unit_max.X + TRACKING_OFFSET.X)
                    Offset.X = Worldmap_Unit.tracking_unit_max.X;
                else
                    Offset.X = Units[Tracking_Unit].loc.X - TRACKING_OFFSET.X;
            }
            else if (Units[Tracking_Unit].loc.X < Offset.X - TRACKING_OFFSET.X)
            {
                if (Units[Tracking_Unit].loc.X < Worldmap_Unit.tracking_unit_min.X - TRACKING_OFFSET.X)
                    Offset.X = Worldmap_Unit.tracking_unit_min.X;
                else
                    Offset.X = Units[Tracking_Unit].loc.X + TRACKING_OFFSET.X;
            }
            // Y
            if (Units[Tracking_Unit].loc.Y > Offset.Y + TRACKING_OFFSET.Y)
            {
                if (Units[Tracking_Unit].loc.Y > Worldmap_Unit.tracking_unit_max.Y + TRACKING_OFFSET.Y)
                    Offset.Y = Worldmap_Unit.tracking_unit_max.Y;
                else
                    Offset.Y = Units[Tracking_Unit].loc.Y - TRACKING_OFFSET.Y;
            }
            else if (Units[Tracking_Unit].loc.Y < Offset.Y - TRACKING_OFFSET.Y)
            {
                if (Units[Tracking_Unit].loc.Y < Worldmap_Unit.tracking_unit_min.Y - TRACKING_OFFSET.Y)
                    Offset.Y = Worldmap_Unit.tracking_unit_min.Y;
                else
                    Offset.Y = Units[Tracking_Unit].loc.Y + TRACKING_OFFSET.Y;
            }

            Target_Loc = Offset;
        }

        public override void update_data()
        {
            Global.game_system.update();
        }

        #region Menus
        private void update_options_menu()
        {
            if (Options_Window.closed)
            {
                Global.game_temp.menuing = false;
                Options_Window = null;
                Save_Data_Calling = true;
            }
        }

        private void open_manage_menu()
        {
            Global.game_state.reset();
            Global.game_map = new Game_Map();
            Manage_Window = new Window_Manage_Items();
        }

        private void update_manage_menu()
        {
            if (Manage_Window.closed)
            {
                Manage_Window = null;
                Global.game_map = null;
                Save_Data_Calling = true;
            }
            else if (Manage_Window.gaining_stats && Manage_Window.using_item)
            {
                if (Global.Input.triggered(Inputs.A) || Global.Input.triggered(Inputs.B))
                    Manage_Window.skip_stat_gain();
            }
            else if (Manage_Window.ready)
            {
                switch (Manage_Window.update_input())
                {
                    case PrepItemsInputResults.None:
                        break;
                    case PrepItemsInputResults.OpenTrade:
                        Trade_Window = new Window_Prep_Trade(
                            Manage_Window.trading_actor_id, Manage_Window.actor_id);
                        break;
                    case PrepItemsInputResults.Status:
                        Status_Window = new Window_Status(
                            Global.battalion.actors, Manage_Window.actor_id, true);
                        break;
                    case PrepItemsInputResults.Supply:
                        Supply_Window = new Window_Supply(Manage_Window.actor_id);
                        break;
                }
            }
        }

        private void open_rankings_menu()
        {
            RankingsWindow = new WindowRankingsOverview(
                Global.Chapter_List[redirect], Global.game_system.Difficulty_Mode);
        }

        private void update_rankings_menu()
        {
            if (RankingsWindow.closed)
            {
                Global.game_temp.menuing = false;
                RankingsWindow = null;
                Save_Data_Calling = true;
            }
        }

        protected void update_trade_menu()
        {
            if (Trade_Window.closed)
            {
                Trade_Window = null;
                Manage_Window.active = true;
            }
            else
            {
                if (Trade_Window.ready)
                {
                    if (Trade_Window.is_help_active)
                    {
                        if (Trade_Window.is_canceled())
                            Trade_Window.close_help();
                    }
                    else
                    {
                        if (Trade_Window.getting_help())
                            Trade_Window.open_help();
                        else if (Trade_Window.is_canceled())
                        {
                            if (Trade_Window.mode > 0)
                            {
                                Global.game_system.play_se(System_Sounds.Cancel);
                                Trade_Window.cancel();
                            }
                            else
                            {
                                Global.game_system.play_se(System_Sounds.Cancel);
                                Trade_Window.staff_fix();
                                Trade_Window.close();
                                Manage_Window.refresh_trade();
                            }
                            return;
                        }
                        else if (Trade_Window.is_selected())
                        {
                            Trade_Window.enter();
                        }
                    }
                }
            }
        }

        protected void update_supply_menu()
        {
            if (Supply_Window.closed)
            {
                Manage_Window.active = true;
                Supply_Window = null;
            }
            else if (Supply_Window.ready)
            {
                Supply_Window.update_input();
                if (Supply_Window.closing)
                    Manage_Window.refresh();

                /* //Debug
                if (Supply_Window.is_help_active)
                {
                    if (Global.Input.triggered(Inputs.B) || Global.Input.triggered(Inputs.R))
                        Supply_Window.close_help();
                }
                else if (!Supply_Window.trading)
                {
                    if (Global.Input.triggered(Inputs.B))
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Supply_Window.close();
                        Manage_Window.refresh();
                    }
                    else if (Global.Input.triggered(Inputs.A))
                    {
                        Supply_Window.trade();
                    }
                    else if (Global.Input.triggered(Inputs.R)) { } //Yeti
                }
                else if (Supply_Window.giving)
                {
                    if (Global.Input.triggered(Inputs.B))
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Supply_Window.cancel_trading();
                    }
                    else if (Global.Input.triggered(Inputs.A))
                        Supply_Window.give();
                    else if (Global.Input.triggered(Inputs.R))
                        Supply_Window.open_help();
                }
                else if (Supply_Window.restocking)
                {
                    if (Global.Input.triggered(Inputs.B))
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Supply_Window.cancel_trading();
                    }
                    else if (Global.Input.triggered(Inputs.A))
                        Supply_Window.restock();
                    else if (Global.Input.triggered(Inputs.R))
                        Supply_Window.open_help();
                }
                else if (Supply_Window.selecting_take)
                {
                    if (Global.Input.triggered(Inputs.B))
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Supply_Window.cancel_selecting_take();
                    }
                    else if (Global.Input.triggered(Inputs.A))
                        Supply_Window.take();
                    else if (Global.Input.triggered(Inputs.R))
                        Supply_Window.open_help();
                }
                else if (Supply_Window.taking)
                {
                    if (Global.Input.triggered(Inputs.B))
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Supply_Window.cancel_trading();
                    }
                    else if (Global.Input.triggered(Inputs.A))
                    {
                        if (Constants.Gameplay.CONVOY_ITEMS_STACK == Convoy_Stack_Types.Full)
                            Supply_Window.select_take();
                        else
                            Supply_Window.take();
                    }
                    else if (Global.Input.triggered(Inputs.X) && Constants.Gameplay.CONVOY_ITEMS_STACK == Convoy_Stack_Types.Full)
                    {
                        Supply_Window.take();
                    }
                    else if (Global.Input.triggered(Inputs.R))
                    {
                        Supply_Window.open_help();
                    }
                }*/
            }
        }

        private void open_unit_menu()
        {
            Global.game_state.reset();
            Global.game_map = new Game_Map();
            Unit_Window = new Window_Unit();// Window_Unit_Actor();
        }

        private void update_unit_menu()
        {
            if (Unit_Window.closed)
            {
                Global.game_temp.menuing = Options_Window != null;
                Unit_Window = null;
                Global.game_map = null;
            }
        }

        private void open_status_menu()
        {
            Global.game_temp.menuing = true;
            Global.game_temp.menu_call = false;
            Global.game_temp.status_menu_call = false;
            Global.game_system.play_se(System_Sounds.Confirm);
            List<int> team = new List<int>();
            if (Global.game_map.preparations_unit_team != null)
                team.AddRange(Global.game_map.preparations_unit_team);
            else
                team.AddRange(Global.game_map.teams[Global.game_temp.status_team]);
            int id = 0;
            for (int i = 0; i < team.Count; i++)
            {
                int unit_id = team[i];
                if (Global.game_temp.status_unit_id == unit_id)
                {
                    id = i;
                    break;
                }
            }
            Status_Window = new Window_Status(team, id);
        }

        private void update_status_menu()
        {
            if (Status_Window.closed)
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                if (Manage_Window != null)
                    Manage_Window.actor_id = Status_Window.current_actor;
                if (Unit_Window != null)
                    Unit_Window.unit_index = Status_Window.current_unit;
                Global.game_temp.status_team = 0;
                close_status_menu();
            }
        }

        private void close_status_menu()
        {
            if (Status_Window != null)
            {
                if (Unit_Window != null)
                {
                }
                else
                    Status_Window.close();
                Status_Window = null;
            }
        }
        #endregion

        protected void update_loc()
        {
            if (Offset != Target_Loc)
            {
                if ((Offset - Target_Loc).Length() <= 0.5f)
                    Offset = Target_Loc;
                else
                {
                    Vector2 offset = (Target_Loc + Offset * 3) / 4;
                    offset -= Offset;
                    float scroll_speed = Phase == Worldmap_Phases.Worldmap_Event ?
                        Scroll_Speed : Constants.WorldMap.WORLDMAP_SCROLL_SPEED;
                    if (offset.Length() > scroll_speed)
                    {
                        offset.Normalize();
                        offset *= scroll_speed;
                    }
                    Offset += offset;
                }
            }
        }

        protected void update_frame()
        {
            int frame = 0;
            if (Unit_Anim_Count >= 0 && Unit_Anim_Count < 32)
                frame = 0;
            else if (Unit_Anim_Count >= 32 && Unit_Anim_Count < 36)
                frame = 1;
            else if (Unit_Anim_Count >= 36 && Unit_Anim_Count < 68)
                frame = 2;
            else if (Unit_Anim_Count >= 68 && Unit_Anim_Count < 72)
                frame = 1;
            Unit_Anim_Count = (Unit_Anim_Count + 1) % 72;
            Lord_Sprite.frame = frame;
        }

        protected void update_command()
        {
            if (Mode_Switch_Timer > 0)
                Mode_Switch_Timer--;
            if (Index != redirect)
                refresh();
            Index = redirect;
            if (Command_Window.is_selected())
            {
                if (Constants.WorldMap.HARD_MODE_BLOCKED.Contains(Global.Chapter_List[redirect]) &&
                        Global.game_system.hard_mode)
                    Global.game_system.play_se(System_Sounds.Buzzer);
                else
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    List<string> commands =
                        new List<string> { "Start Chapter", "Options" };
                    if (UnitWindowAvailable)
                    {
                        commands.Add("Unit");
                        commands.Add("Manage");
                    }
                    if (RankingWindowAvailable)
                        commands.Add("Ranking");
                    ChapterCommandWindow = new Window_Command_Headered(
                        redirect_chapter.World_Map_Name,
                        Command_Window.loc + new Vector2(8, 16),
                        WIDTH - (16 + 16), commands);
                    ChapterCommandWindow.tint = new Color(224, 224, 224, 224);
                    ChapterCommandWindow.text_offset = new Vector2(4, 0);
                    Command_Window.active = false;
                    Command_Window.visible = false;
                    Phase = Worldmap_Phases.ConfirmingChapter;
                }
            }
            else if (Command_Window.is_canceled() || cancel_button_triggered)
            {
                Confirm_Window = new Parchment_Confirm_Window();
                Confirm_Window.loc = new Vector2(Config.WINDOW_WIDTH - 88, Config.WINDOW_HEIGHT - 48) / 2;
                Confirm_Window.set_text("Return to title?");
                Confirm_Window.add_choice("Yes", new Vector2(8, 16));
                Confirm_Window.add_choice("No", new Vector2(48, 16));
                Confirm_Window.size = new Vector2(88, 48);
                Confirm_Window.index = 1;
                Quit_Confirming = true;

                Command_Window.active = false;
            }
            else if (Global.Input.triggered(Inputs.X) ||
                DifficultyButton.consume_trigger(MouseButtons.Left) ||
                DifficultyButton.consume_trigger(TouchGestures.Tap))
            {
                switch_difficulty(true);
            }
            else if (Global.Input.triggered(Inputs.Left) ||
                Global.Input.gesture_triggered(TouchGestures.SwipeRight))
            {
                command_left();
            }
            else if (Global.Input.triggered(Inputs.Right) ||
                Global.Input.gesture_triggered(TouchGestures.SwipeLeft))
            {
                command_right();
            }
            // If not scrolling
            else if ((Offset - (Target_Loc)).Length() < Constants.WorldMap.WORLDMAP_SCROLL_SPEED)
            {
                if (Global.Input.triggered(Inputs.Select))
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    Options_Window = new Window_Options();
                }
                else if (UnitWindowAvailable && Global.Input.triggered(Inputs.Start))
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    open_manage_menu();
                }
                else if (UnitWindowAvailable && Command_Window.getting_help()) //Debug // Global.Input.triggered(Inputs.R))
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    open_unit_menu();
                }
            }
        }

        protected virtual void command_left()
        {
            switch_difficulty(false);
        }

        protected virtual void command_right()
        {
            switch_difficulty(true);
        }

        protected bool can_change_difficulty()
        {
            return Mode_Switch_Timer <= 0 &&
                Global.save_file.Style != Mode_Styles.Classic &&
                hard_mode_enabled(redirect); // Hard mode enabled
        }

        protected bool difficulty_change_button_visible()
        {
            return (Phase == Worldmap_Phases.Command_Process ||
                Phase == Worldmap_Phases.ConfirmingChapter) &&
                Global.save_file.Style != Mode_Styles.Classic &&
                hard_mode_enabled(redirect);
        }

        protected void switch_difficulty(bool increase)
        {
            if (can_change_difficulty())
            {
                Global.game_system.play_se(System_Sounds.Status_Page_Change);
                int difficulties = Enum_Values.GetEnumCount(typeof(Difficulty_Modes));
                if (increase)
                    Global.game_system.Difficulty_Mode =
                        (Difficulty_Modes)(((int)Global.game_system.Difficulty_Mode + 1) % difficulties);
                else
                    Global.game_system.Difficulty_Mode =
                        (Difficulty_Modes)(((int)Global.game_system.Difficulty_Mode - 1 + difficulties) % difficulties);
                Global.save_file.Difficulty = Global.game_system.Difficulty_Mode;
                refresh_data();
                Data_Window.set_mode(Global.game_system.Difficulty_Mode, true);
                Mode_Switch_Timer = Constants.WorldMap.WORLDMAP_MODE_SWITCH_DELAY;
            }
        }

        protected void update_chapter_selected()
        {
            if (Mode_Switch_Timer > 0)
                Mode_Switch_Timer--;
            if (ChapterCommandWindow.is_selected())
            {
                switch (ChapterCommandWindow.selected_index())
                {
                    case 0:
                        if (Valid_Previous_Chapters.Count > 1)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            List<string> strs = Valid_Previous_Chapters.Select(x => Global.data_chapters[x].World_Map_Name).ToList();
                            Previous_Chapter_Selection_Window = new Window_Command_Headered(
                                "Previous Chapter",
                                new Vector2((Config.WINDOW_WIDTH - 96) / 2, Config.WINDOW_HEIGHT / 2 - 24),
                                96, strs);
                            Previous_Chapter_Selection_Window.immediate_index = Previous_Chapter_Index;
                        }
                        else
                            select_chapter();
                        break;
                    case 1:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Options_Window = new Window_Options();
                        break;
                    case 2:
                        if (UnitWindowAvailable)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            open_unit_menu();
                        }
                        else if (RankingWindowAvailable)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            open_rankings_menu();
                        }
                        break;
                    case 3:
                        if (UnitWindowAvailable)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            open_manage_menu();
                        }
                        break;
                    case 4:
                        if (RankingWindowAvailable)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            open_rankings_menu();
                        }
                        break;
                }
            }
            else if (Global.Input.triggered(Inputs.X) ||
                DifficultyButton.consume_trigger(MouseButtons.Left) ||
                DifficultyButton.consume_trigger(TouchGestures.Tap))
            {
                switch_difficulty(true);
            }
            else if (ChapterCommandWindow.is_canceled() || cancel_button_triggered)
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Command_Window.active = true;
                Command_Window.visible = true;
                ChapterCommandWindow = null;
                Phase = Worldmap_Phases.Command_Process;
            }
        }

        protected void select_chapter()
        {
#if DEBUG
            if (Global.save_files_info != null && (Global.current_save_info.map_save_exists || Global.current_save_info.suspend_exists))
#else
                    if (Global.current_save_info.map_save_exists || Global.current_save_info.suspend_exists)
#endif
            {
                Confirm_Window = new Parchment_Confirm_Window();
                Confirm_Window.loc = new Vector2(Config.WINDOW_WIDTH - 152, Config.WINDOW_HEIGHT - 64) / 2;
                Confirm_Window.set_text("Temporary saves for this file\nwill be deleted. Proceed?");
                Confirm_Window.add_choice("Yes", new Vector2(32, 32));
                Confirm_Window.add_choice("No", new Vector2(88, 32));
                Confirm_Window.size = new Vector2(152, 64);
                Confirm_Window.index = 1;
                Quit_Confirming = false;
            }
            else
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                select_chapter_fade();
            }
        }

        protected void select_chapter_fade()
        {
            Phase = Worldmap_Phases.Controls_Fade;
            Fade_Timer = Classic ? 0 : Constants.WorldMap.WORLDMAP_CONTROLS_FADE_TIME;
            if (HARD_MODE_BLOCKED.Contains(Global.Chapter_List[redirect]) &&
                Global.game_system.Difficulty_Mode > Difficulty_Modes.Normal)
            {
                Hard_Mode_Blocked_Window = new Parchment_Info_Window();
                Hard_Mode_Blocked_Window.set_text(@"This chapter does not yet have
hard mode data, and will be
loaded in normal mode. Sorry!");
                Hard_Mode_Blocked_Window.size = new Vector2(160, 64);
                Hard_Mode_Blocked_Window.loc = new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) / 2 - Hard_Mode_Blocked_Window.size / 2;

                Global.game_system.Difficulty_Mode = Difficulty_Modes.Normal;
            }
        }

        protected virtual void start_chapter_worldmap_event()
        {
            Phase = Worldmap_Phases.Worldmap_Event;
            FEXNA_Library.Map_Event_Data events =
                Global.Content.Load<FEXNA_Library.Map_Event_Data>(@"Data/Map Data/Event Data/Worldmap");
            int event_index = 0;
            for (; event_index < events.Events.Count; event_index++)
                if (events.Events[event_index].name == Global.Chapter_List[redirect] + "Worldmap")
                    break;
            if (event_index >= events.Events.Count)
            {
                Phase = Worldmap_Phases.Fade_Out;
                Fade_Timer = Constants.WorldMap.WORLDMAP_FADE_TIME;
                Global.Audio.bgm_fade(Constants.WorldMap.WORLDMAP_FADE_TIME);
            }
            else
                Global.game_system.add_event(events.Events[event_index]);
        }
        #endregion

        #region Message
        protected override void main_window()
        {
            Message_Window = new Window_Worldmap_Message();
            Message_Window.stereoscopic = Config.CONVO_TEXT_DEPTH;
            Message_Window.face_stereoscopic = Config.CONVO_FACE_DEPTH;
        }

        public override void event_skip()
        {
            Global.Audio.bgm_fade(Constants.WorldMap.WORLDMAP_FADE_TIME);
            start_chapter(Global.Chapter_List[redirect],
                redirect_chapter.Prior_Chapters, Global.chapter_by_index(redirect).Standalone);
        }
        #endregion

        #region Events
        public void add_dot(int team, Vector2 loc)
        {
            Flashing_Worldmap_Object worldmap_object = new Worldmap_Dot(team);
            worldmap_object.loc = loc;
            worldmap_object.stereoscopic = Config.MAP_UNITS_DEPTH;
            Worldmap_Objects.Add(worldmap_object);
        }

        public void add_arrow(int team, int speed, Vector2[] waypoints)
        {
            Flashing_Worldmap_Object worldmap_object = new Worldmap_Arrow(team, speed, waypoints);
            worldmap_object.stereoscopic = Config.MAP_UNITS_DEPTH;
            Worldmap_Objects.Add(worldmap_object);
        }

        public void remove_dot(int index)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(index < Worldmap_Objects.Count, string.Format("Tried to remove a world map object at index {0},\nbut only {1} object{2} exist{3}.",
                index, Worldmap_Objects.Count, Worldmap_Objects.Count == 1 ? "" : "s", Worldmap_Objects.Count == 1 ? "s" : ""));
#endif
            Worldmap_Objects.RemoveAt(index);
        }

        public void add_beacon(Vector2 loc)
        {
            Beacon = new Worldmap_Beacon();
            Beacon.loc = loc;
            Beacon.stereoscopic = Config.MAP_UNITS_DEPTH;
        }

        public void remove_beacon()
        {
            Beacon = null;
        }

        public void add_unit(int team, string filename, Vector2 loc)
        {
            Worldmap_Unit unit = new Worldmap_Unit(team, filename);
            unit.loc = loc;
            unit.stereoscopic = Config.MAP_UNITS_DEPTH;
            Units.Add(unit);
        }

        public void queue_unit_move(int index, int speed, Vector2[] waypoints)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(index < Units.Count, string.Format(
                "Trying to move a world map sprite, but\nthe index is past the end of the unit list\nIndex: {0}, Unit count: {1}",
                index, Units.Count));
#endif   
            Units[index].queue_move(speed, waypoints);
        }
        public void queue_unit_idle(int index)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(index < Units.Count, string.Format(
                "Trying to idle a world map sprite, but\nthe index is past the end of the unit list\nIndex: {0}, Unit count: {1}",
                index, Units.Count));
#endif   
            Units[index].queue_idle();
        }
        public void queue_unit_pose(int index)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(index < Units.Count, string.Format(
                "Trying to pose a world map sprite, but\nthe index is past the end of the unit list\nIndex: {0}, Unit count: {1}",
                index, Units.Count));
#endif   
            Units[index].queue_pose();
        }

        public void queue_unit_remove(int index, bool immediately, bool kill)
        {
            if (immediately)
            {
                if (index == Tracking_Unit)
                    Tracking_Unit = -1;
                else
                    Tracking_Unit--;
                Units[index].remove(kill);
                Clearing_Units.Add(Units[index]);
                Units.RemoveAt(index);
            }
            else
                Units[index].queue_remove(kill);
        }

        public void queue_unit_tracking(int index, Vector2 min, Vector2 max)
        {
            Units[index].queue_tracking(min, max);
        }

        public void clear_removing_units()
        {
            int i = 0;
            while (i < Units.Count)
            {
                Units[i].remove_if_queued();
                if (Units[i].is_removed)
                {
                    if (i == Tracking_Unit)
                        Tracking_Unit = -1;
                    else
                        Tracking_Unit--;
                    Clearing_Units.Add(Units[i]);
                    Units.RemoveAt(i);
                }
                else
                    i++;
            }
        }

        public bool zoomed_map_visible
        {
            set
            {
                Zoomed_Map_Visible = value;
                Zoomed_Fade_Timer = Constants.WorldMap.WORLDMAP_ZOOM_FADE_TIME;
                Zoomed_Out_Map.opacity = Zoomed_Map_Visible ? 0 : 255;
            }
        }
        #endregion

        #region Draw
        public override void draw(SpriteBatch sprite_batch, GraphicsDevice device, RenderTarget2D[] render_targets)
        {
            Vector2 offset = new Vector2((int)Offset.X, (int)Offset.Y) - (new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) / 2);

            device.SetRenderTarget(render_targets[0]);
            device.Clear(Color.Transparent);
            // Draw controls and menus
            if (Phase < Worldmap_Phases.Worldmap_Event || Phase == Worldmap_Phases.Return_To_Title)
            {
                if (!Classic)
                    draw_controls(sprite_batch, offset);
            }
            // Draw world map event objects
            else
                draw_events(sprite_batch, Zoomed_Map_Visible ? Vector2.Zero : offset);

            // Draws the map
            device.SetRenderTarget(render_targets[1]);
            device.Clear(Color.Transparent);
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Map.draw(sprite_batch, offset + Constants.WorldMap.WORLDMAP_MAP_OFFSET);
            sprite_batch.End();

            // Copies the menus onto the map
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            int alpha = 255;
            if (Phase == Worldmap_Phases.Controls_Fade)
                alpha = Fade_Timer * 255 / Constants.WorldMap.WORLDMAP_CONTROLS_FADE_TIME;
            sprite_batch.Draw(render_targets[0], Vector2.Zero, new Color(alpha, alpha, alpha, alpha));
            sprite_batch.End();

            if (Previous_Chapter_Selection_Window != null)
            {
                Previous_Chapter_Selection_Window.draw(sprite_batch);
            }
            if (Confirm_Window != null)
                Confirm_Window.draw(sprite_batch);
            if (Hard_Mode_Blocked_Window != null)
                Hard_Mode_Blocked_Window.draw(sprite_batch);

            // Draws the composite image, to allow fading the whole thing
            device.SetRenderTarget(render_targets[0]);
            device.Clear(Color.Transparent);
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            int fade_alpha = 255;
            if (Phase != Worldmap_Phases.Controls_Fade)
                fade_alpha = ((Phase == Worldmap_Phases.Fade_Out ||
                    Phase == Worldmap_Phases.Return_To_Title) ?
                        Fade_Timer :
                        (Constants.WorldMap.WORLDMAP_FADE_TIME - Fade_Timer)) *
                    255 / Constants.WorldMap.WORLDMAP_FADE_TIME;
            sprite_batch.Draw(render_targets[1], Vector2.Zero, new Color(fade_alpha, fade_alpha, fade_alpha, fade_alpha));
            sprite_batch.End();

            if (Options_Window != null)
                Options_Window.draw(sprite_batch);
            if (RankingsWindow != null)
                RankingsWindow.draw(sprite_batch);
            if (Manage_Window != null)
            {
                if ((Trade_Window == null || !Trade_Window.visible) &&
                        (Supply_Window == null || !Supply_Window.visible))
                    Manage_Window.draw(sprite_batch);

                if (Trade_Window != null) Trade_Window.draw(sprite_batch);
                if (Supply_Window != null) Supply_Window.draw(sprite_batch);
            }
            if (Unit_Window != null) Unit_Window.draw(sprite_batch);
            if (Status_Window != null) Status_Window.draw(sprite_batch);

            base.draw(sprite_batch, device, render_targets);
        }

        protected void draw_controls(SpriteBatch sprite_batch, Vector2 offset)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Lord_Sprite.draw(sprite_batch, offset);
            // Minimap
            Minimap_Backing.draw(sprite_batch);
            Minimap.draw(sprite_batch);
            ViewArea.draw(
                sprite_batch,
                -(Offset + Constants.WorldMap.WORLDMAP_MAP_OFFSET -
                    Constants.WorldMap.WORLDMAP_MAP_SPRITE_OFFSET) * Minimap.scale);
            sprite_batch.End();

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Data_Window.draw(sprite_batch);

            CancelButton.Draw(sprite_batch);
            if (difficulty_change_button_visible())
                DifficultyButton.Draw(sprite_batch);
            /* //Debug
            Options_Button.Draw(sprite_batch);
            if (Unit_Window_Available)
            {
                Unit_Window_Button.Draw(sprite_batch);
                Manage_Button.Draw(sprite_batch);
            }*/
            sprite_batch.End();
            // Command window
            // moved here to draw above data window so the top arrow doesn't get covered; move back if using scrollbar //Debug
            Command_Window.draw(sprite_batch);
            if (ChapterCommandWindow != null)
                ChapterCommandWindow.draw(sprite_batch);
        }

        protected void draw_events(SpriteBatch sprite_batch, Vector2 offset)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Zoomed_Out_Map.draw(sprite_batch);
            if (Beacon != null)
                Beacon.draw(sprite_batch, offset);
            foreach (Flashing_Worldmap_Object sprite in Worldmap_Objects)
                sprite.draw(sprite_batch, offset);
            foreach (Worldmap_Unit unit in Units.OrderBy(x => x.loc.Y))
                unit.draw(sprite_batch, offset);
            sprite_batch.End();

            Effect effect = Global.effect_shader();
            if (effect != null)
                effect.CurrentTechnique = effect.Techniques["Technique1"];
            foreach (Worldmap_Unit unit in Clearing_Units)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, effect);
                if (effect != null)
                    unit.set_sprite_batch_effects(effect);
                unit.draw(sprite_batch, offset);
                sprite_batch.End();
            }

            draw_message(sprite_batch);
        }
        #endregion
    }
}
