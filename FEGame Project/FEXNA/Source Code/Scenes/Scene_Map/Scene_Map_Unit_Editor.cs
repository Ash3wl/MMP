#if !MONOGAME && DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using FEXNA.Windows.Command;
using FEXNA.Windows.Map;
using FEXNA.Windows.UserInterface.Command;
using FEXNA_Library;

namespace FEXNA
{
    enum Unit_Editor_Options { Unit, Add_Unit, Paste_Unit, Reinforcements, Options, Clear_Units, Mirror_Units, Save, Quit }
    class Scene_Map_Unit_Editor : Scene_Map
    {
        Window_Unit_Editor Unit_Editor;
        string Map_Data_Key, Save_Name;
        int Reinforcement_Index = -1;
        bool Esc_Pressed, Esc_Triggered;
        Map_Unit_Data Unit_Data;
        Window_Command_Scroll_Arrow Reinforcements_Window;
        Window_Command Clear_Unit_Window;
        Window_Confirmation Quit_Confirm_Window, Cancel_Editing_Confirm_Window, Delete_Reinforcement_Confirm_Window;

        public Scene_Map_Unit_Editor() { }

        protected override void initialize_base()
        {
            Scene_Type = "Scene_Map_Unit_Editor";
            main_window();
            camera = new Camera(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT, Vector2.Zero);
        }

        new public void set_map()
        {
            set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
        }
        public override void set_map(string chapter_id, string map_data_key, string unit_data_key, string event_data_key)
        {
            if (!string.IsNullOrEmpty(chapter_id))
                Save_Name = chapter_id;
            chapter_id = "";
            if (Unit_Data != null)
            {
                HashSet<Vector2> keys = new HashSet<Vector2>();
                keys.UnionWith(Unit_Data.Units.Keys);
                Dictionary<Vector2, Data_Unit> fgdsf = new Dictionary<Vector2, Data_Unit>();
                foreach (Vector2 key in keys)
                //for (int i = 0; i < keys.Count; i++)
                {
                    Data_Unit unit = Unit_Data.Units[key];
                    fgdsf[key + new Vector2(2, 2)] = unit;
                }
                //Unit_Data.Units = fgdsf;
            }

            Global.game_battalions = new Game_Battalions();
            Global.game_battalions.add_battalion(0);
            Global.game_battalions.current_battalion = 0;

            Global.game_actors = new Game_Actors();
            bool new_map = Unit_Data == null;
            reset_map(new_map);
            // Map Data
            Map_Data_Key = map_data_key;
            Data_Map map_data;
            if (map_data_key == "")
                map_data = new Data_Map();
            else
            {
                Data_Map loaded_map_data = get_map_data(map_data_key);
                map_data = new Data_Map(loaded_map_data.values, loaded_map_data.GetTileset());
            }
            // Unit Data
            if (Global.content_exists(@"Data/Map Data/Unit Data/" + unit_data_key))
                Unit_Data = Map_Content[0].Load<Map_Unit_Data>(@"Data/Map Data/Unit Data/" + unit_data_key);
            else if (Unit_Data == null)
                Unit_Data = new Map_Unit_Data();
            // Event Data

            Global.game_state.setup(chapter_id, map_data, Unit_Data, event_data_key);
            if (Global.test_battler_1.Generic)
                Global.test_battler_1.Actor_Id = Global.game_actors.next_actor_id();
            set_map_texture();
            if (new_map)
            {
                Global.player.center();
                Global.game_system.Instant_Move = true;
                Global.game_state.update();
                Global.game_system.update();
            }
            else
                Global.game_map.highlight_test();
        }

        public void update(KeyboardState key_state)
        {
            Esc_Triggered = !Esc_Pressed && key_state.IsKeyDown(Keys.Escape);
            update();
            Esc_Pressed = key_state.IsKeyDown(Keys.Escape);
        }

        protected override bool update_menu_map()
        {
            if (is_map_window_open)
            {
                if (Quit_Confirm_Window != null)
                {
                    Quit_Confirm_Window.update();
                    update_quit_confirm();
                }
                else if (Clear_Unit_Window != null && Clear_Unit_Window.active)
                {
                    Clear_Unit_Window.update();
                    update_unit_clear_menu();
                }
                else if (map_window_active)
                {
                    update_map_window();
                    update_map_menu();
                    return true;
                }
            }
            if (Reinforcements_Window != null)
            {
                if (Delete_Reinforcement_Confirm_Window != null)
                {
                    Delete_Reinforcement_Confirm_Window.update();
                    update_delete_reinforcement_menu();
                }
                else if (Reinforcements_Window.active)
                {
                    Reinforcements_Window.update();
                    update_reinforcements_menu();
                    return true;
                }
            }
            if (check_unit())
                return true;
            if (check_options())
                return true;
            return false;
        }

        protected override bool update_menu_unit()
        {
            if (is_unit_command_window_open)
            {
                if (unit_command_window_active)
                {
                    update_unit_command_window();
                    update_unit_command_menu();
                    return true;
                }
            }
            if (Unit_Editor != null)
            {
                if (Unit_Editor.is_ready)
                {
                    Unit_Editor.update();
                    if (Cancel_Editing_Confirm_Window != null)
                        Cancel_Editing_Confirm_Window.update();
                    update_unit_edit_menu();
                }
                else
                    Unit_Editor.update();
                return true;
            }
            return false;
        }

        #region Unit Command Menu
        protected override void open_unit_menu(Canto_Records canto)
        {
            List<string> commands = new List<string>();
            // Actions:
            //   0 = Edit Unit
            //   1 = Move Unit
            //   2 = Change Team
            //   3 = Copy Unit
            //  10 = Remove Unit
            Index_Redirect = new List<int>();

            // Edit Unit
            commands.Add("Edit Unit");
            Index_Redirect.Add(0);
            // Move Unit
            commands.Add("Move Unit");
            Index_Redirect.Add(1);
            // Change Team
            commands.Add("Change Team");
            Index_Redirect.Add(2);
            // Copy Unit
            commands.Add("Copy Unit");
            Index_Redirect.Add(3);
            // Remove
            commands.Add("Remove Unit");
            Index_Redirect.Add(10);

            new_unit_command_window(commands, 80);
        }

        protected override void update_unit_command_menu()
        {
            Game_Unit unit = Global.game_map.units[Unit_Id];
            if (unit_command_is_canceled)
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Global.game_temp.menuing = false;
                close_unit_menu();
                Global.game_system.Selected_Unit_Id = -1;
                Global.game_map.move_range_visible = true;
                Global.game_map.highlight_test();
            }
            else if (unit_command_is_selected)
            {
                unit_menu_select(Index_Redirect[unit_command_window_index], unit);
            }
            else
            {
                switch (Index_Redirect[unit_command_window_index])
                {
                    // Change Team
                    case 2:
                        if (Global.Input.repeated(Inputs.Left) || Global.Input.repeated(Inputs.Right))
                        {
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                            int team = unit.team;
                            team = new_team(team, Global.Input.repeated(Inputs.Left));

                            Global.game_map.change_unit_team(unit.id, team);

                            Global.test_battler_1 = Test_Battle_Character_Data.from_data(
                                Unit_Data.Units[unit.loc].type, Unit_Data.Units[unit.loc].identifier, Unit_Data.Units[unit.loc].data);
                            string[] ary = Global.test_battler_1.to_string(unit.team);
                            Unit_Data.Units[Global.player.loc] = new Data_Unit(ary[0], ary[1], ary[2]);
                        }
                        break;
                }
            }
        }

        private int new_team(int old_team, bool left)
        {
            if (left)
                old_team--;
            else
                old_team++;

            old_team--;
            old_team = ((old_team + Constants.Team.NUM_TEAMS) % Constants.Team.NUM_TEAMS);
            old_team++;

            return old_team;
        }

        protected override void unit_menu_select(int option, Game_Unit unit)
        {
            switch (option)
            {
                case 0: // Edit Unit
                    Global.game_system.play_se(System_Sounds.Confirm);

                    unit_command_window_visible = false;
                    unit_command_window_active = false;
                    Global.test_battler_1 = Test_Battle_Character_Data.from_data(
                        Unit_Data.Units[unit.loc].type, Unit_Data.Units[unit.loc].identifier, Unit_Data.Units[unit.loc].data);
                    Unit_Editor = new Window_Unit_Editor();
                    break;
                case 1: // Move Unit
                    Global.game_system.play_se(System_Sounds.Confirm);
                    Global.game_state.moving_editor_unit = true;
                    Global.game_temp.menuing = false;
                    close_unit_menu();
                    Global.game_map.move_range_visible = true;
                    Global.game_map.highlight_test();
                    break;
                case 3: // Copy Unit
                    Global.game_system.play_se(System_Sounds.Confirm);
                    Global.test_battler_1 = Test_Battle_Character_Data.from_data(
                        Unit_Data.Units[unit.loc].type, Unit_Data.Units[unit.loc].identifier, Unit_Data.Units[unit.loc].data);
                    if (Global.test_battler_1.Generic)
                        Global.test_battler_1.Actor_Id = Global.game_actors.next_actor_id();
                    Global.game_temp.menuing = false;
                    close_unit_menu();
                    Global.game_system.Selected_Unit_Id = -1;
                    Global.game_map.move_range_visible = true;
                    Global.game_map.highlight_test();
                    break;
                case 10: // Remove Unit
                    Global.game_system.play_se(System_Sounds.Confirm);
                    // Remove unit and refresh the map
                    Unit_Data.Units.Remove(unit.loc);
                    set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                    // Close menu
                    Global.game_temp.menuing = false;
                    Global.game_system.Selected_Unit_Id = -1;
                    Global.game_map.move_range_visible = true;
                    Global.game_map.highlight_test();
                    break;
            }
        }

        private void close_unit_editor()
        {
            Global.game_temp.menuing = false;
            Unit_Editor = null;
            close_unit_menu();
            Global.game_system.Selected_Unit_Id = -1;
            Reinforcement_Index = -1;
            set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
            Global.game_map.move_range_visible = true;
            Global.game_map.highlight_test();
        }

        protected void update_unit_edit_menu()
        {
            if (Cancel_Editing_Confirm_Window != null)
            {
                if (Cancel_Editing_Confirm_Window.is_ready)
                {
                    if (Cancel_Editing_Confirm_Window.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Cancel_Editing_Confirm_Window = null;
                        Unit_Editor.active = true;
                    }
                    else if (Cancel_Editing_Confirm_Window.is_selected())
                    {
                        if (Cancel_Editing_Confirm_Window.index == 0)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            close_unit_editor();
                        }
                        else
                        {
                            Global.game_system.play_se(System_Sounds.Cancel);
                            Unit_Editor.active = true;
                        }
                        Cancel_Editing_Confirm_Window = null;
                    }
                }
            }
            else
            {
                Game_Unit unit = Global.game_map.units[Global.game_system.Selected_Unit_Id];
                if (Global.Input.triggered(Inputs.B)) //Debug
                {
                    if (Unit_Editor.is_ready)
                    {
                        //Global.Audio.play_se("System Sounds", "Help_Open");
                        //Global.game_system.play_se(System_Sounds.Help_Open);
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Unit_Editor.active = false;
                        Cancel_Editing_Confirm_Window = new Window_Confirmation();
                        Cancel_Editing_Confirm_Window.loc = new Vector2(56, 32);
                        Cancel_Editing_Confirm_Window.set_text("Cancel editing?\nChanges will be lost.");
                        Cancel_Editing_Confirm_Window.add_choice("Yes", new Vector2(16, 32));
                        Cancel_Editing_Confirm_Window.add_choice("No", new Vector2(56, 32));
                        Cancel_Editing_Confirm_Window.size = new Vector2(104, 64);
                        Cancel_Editing_Confirm_Window.index = 1;
                    }
                }
                else if (Esc_Triggered) //Debug
                {
                    if (Unit_Editor.is_ready)
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        close_unit_editor();
                    }
                }
                else if (Global.Input.triggered(Inputs.Start)) //Global.Input.triggered(Inputs.A) //Debug
                {
                    if (Unit_Editor.is_ready)
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        if (Reinforcement_Index != -1)
                        {
                            string[] ary = Global.test_battler_1.to_string(unit.team);
                            Unit_Data.Reinforcements[Reinforcement_Index] = new Data_Unit(ary[0], ary[1], ary[2]);
                        }
                        else
                        {
                            string[] ary = Global.test_battler_1.to_string(unit.team);
                            Unit_Data.Units[Global.player.loc] = new Data_Unit(ary[0], ary[1], ary[2]);
                        }
                        Global.game_temp.menuing = false;
                        Unit_Editor = null;
                        close_unit_menu();
                        Global.game_system.Selected_Unit_Id = -1;
                        Reinforcement_Index = -1;
                        set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                        Global.game_map.move_range_visible = true;
                        Global.game_map.highlight_test();
                    }
                }
            }
        }

        public void move_unit()
        {
            Unit_Data.Units[Global.player.loc] = Unit_Data.Units[Global.game_map.get_selected_unit().loc];
            Unit_Data.Units.Remove(Global.game_map.get_selected_unit().loc);
        }
        #endregion

        #region Map Command Menu
        protected override void open_map_menu()
        {
            List<string> commands = new List<string> { "Unit", "Add Unit", "Paste Unit", "Reinforcements", "Options", "Clear Units", "Mirror Units", "Save", "Quit" };
            new_map_window(commands, 80);
            map_window_color = Window_Unit_Team.TEAM - 1;
            if (Global.game_map.get_unit(Global.player.loc) != null)
            {
                set_map_window_text_color(0, "Grey");
                set_map_window_text_color(1, "Grey");
            }
        }

        protected override void update_map_menu()
        {
            if (this.map_command_is_canceled)
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Global.game_temp.menuing = false;
                close_map_menu();
                Global.game_map.highlight_test();
            }
            else if (this.map_command_is_selected)
            {
                switch (this.map_window_selected_index)
                {
                    case (int)Unit_Editor_Options.Unit:
                        if (Global.game_map.teams[Window_Unit_Team.TEAM].Count > 0)
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            new_unit_window();
                            close_map_menu();
                        }
                        else
                            Global.game_system.play_se(System_Sounds.Buzzer);
                        break;
                    case (int)Unit_Editor_Options.Add_Unit: // Add Unit
                        if (Global.game_map.get_unit(Global.player.loc) != null)
                            Global.game_system.play_se(System_Sounds.Buzzer);
                        else
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            // Add a unit and refresh the map
                            Unit_Data.Units.Add(Global.player.loc, new Data_Unit("character", "", "1|Actor ID\n1|Team\n0|AI Priority\n3|AI Mission"));
                            set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                            // Close menu
                            Global.game_temp.menuing = false;
                            close_map_menu();
                            Global.game_map.highlight_test();
                        }
                        break;
                    case (int)Unit_Editor_Options.Paste_Unit: // Paste Unit
                        if (Global.game_map.get_unit(Global.player.loc) != null)
                            Global.game_system.play_se(System_Sounds.Buzzer);
                        else
                        {
                            Global.game_system.play_se(System_Sounds.Confirm);
                            // Add a unit and refresh the map
                            string[] ary = Global.test_battler_1.to_string();
                            Unit_Data.Units.Add(Global.player.loc, new Data_Unit(ary[0], ary[1], ary[2]));
                            set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                            // Close menu
                            Global.game_temp.menuing = false;
                            close_map_menu();
                            Global.game_map.highlight_test();
                        }
                        break;
                    case (int)Unit_Editor_Options.Reinforcements: // Reinforcements
                        Global.game_system.play_se(System_Sounds.Confirm);
                        open_reinforcements_menu();
                        close_map_menu();
                        break;
                    case (int)Unit_Editor_Options.Options: // Options
                        Global.game_system.play_se(System_Sounds.Confirm);
                        new_options_window();
                        close_map_menu();
                        break;
                    case (int)Unit_Editor_Options.Clear_Units: // Clear Units
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Clear_Unit_Window = new Window_Command(new Vector2(
                            8 + (Global.player.is_on_left() ? (Config.WINDOW_WIDTH - (80 + 32)) : 16), 80),
                            80, new List<string> { "Confirm", "Cancel" });
                        Clear_Unit_Window.immediate_index = 1;
                        map_window_active = false;
                        break;
                    case (int)Unit_Editor_Options.Mirror_Units: // Clear Units
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Clear_Unit_Window = new Window_Command(new Vector2(
                            8 + (Global.player.is_on_left() ? (Config.WINDOW_WIDTH - (80 + 32)) : 16), 96),
                            80, new List<string> { "Confirm", "Cancel" });
                        Clear_Unit_Window.immediate_index = 1;
                        map_window_active = false;
                        break;
                    case (int)Unit_Editor_Options.Save: // Save
                        Global.game_system.play_se(System_Sounds.Confirm);
                        
                        XmlWriterSettings settings = new XmlWriterSettings();
                        settings.Indent = true;
                        string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        //string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        path += @"\SavedUnitData\";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        using (XmlWriter writer = XmlWriter.Create(
                            path + Save_Name + ".xml", settings))
                        {
                            IntermediateSerializer.Serialize(writer, Unit_Data, null);
                        }
                        break;
                    case (int)Unit_Editor_Options.Quit: // Quit
                        Global.game_system.play_se(System_Sounds.Confirm);

                        Quit_Confirm_Window = new Window_Confirmation();
                        int height = 48;
                        Quit_Confirm_Window.loc = map_window_loc +
                            new Vector2(0, 24 + map_window_selected_index * 16);
                        if (Quit_Confirm_Window.loc.Y + height > Config.WINDOW_HEIGHT)
                            Quit_Confirm_Window.loc = map_window_loc +
                                new Vector2(0, map_window_selected_index * 16 - 40);
                        Quit_Confirm_Window.set_text("Are you sure?");
                        Quit_Confirm_Window.add_choice("Yes", new Vector2(16, 16));
                        Quit_Confirm_Window.add_choice("No", new Vector2(56, 16));
                        Quit_Confirm_Window.size = new Vector2(88, height);
                        Quit_Confirm_Window.index = 1;
                        map_window_active = false;
                        break;
                }
            }
            else
            {
                switch (this.map_window_index)
                {
                    // Change Team
                    case (int)Unit_Editor_Options.Unit:
                        if (Global.Input.repeated(Inputs.Left) || Global.Input.repeated(Inputs.Right))
                        {
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                            int team = Window_Unit_Team.TEAM;
                            team = new_team(team, Global.Input.repeated(Inputs.Left));

                            Window_Unit_Team.TEAM = team;
                            map_window_color = Window_Unit_Team.TEAM - 1;
                        }
                        break;
                }
            }
        }

        internal override Window_Unit unit_window()
        {
            return new Window_Unit_Team();
        }

        protected void update_unit_clear_menu()
        {
            if (Clear_Unit_Window.is_canceled())
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Clear_Unit_Window = null;
                map_window_active = true;
            }
            else if (Clear_Unit_Window.is_selected())
            {
                switch (Clear_Unit_Window.selected_index())
                {
                    case 0:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        switch (this.map_window_index)
                        {
                            case (int)Unit_Editor_Options.Clear_Units:
                                Unit_Data.Units.Clear();
                                Unit_Data.Reinforcements.Clear();
                                set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                                break;
                            case (int)Unit_Editor_Options.Mirror_Units:
                                Dictionary<Vector2, Data_Unit> unit_data = Unit_Data.Units
                                    .Select(p => new KeyValuePair<Vector2, Data_Unit>(
                                        new Vector2((Global.game_map.width - 1) - p.Key.X, p.Key.Y), p.Value))
                                    .ToDictionary(p => p.Key, p => p.Value);
                                Unit_Data.Units = unit_data;
                                set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                                break;
                        }
                        // Close menu
                        Global.game_temp.menuing = false;
                        Clear_Unit_Window = null;
                        close_map_menu();
                        Global.game_map.highlight_test();
                        break;
                    case 1:
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Clear_Unit_Window = null;
                        map_window_active = true;
                        break;
                }
            }
        }

        protected void open_reinforcements_menu()
        {
            List<string> commands = new List<string> { "Reinforcements:" };
            //foreach (Data_Unit unit in Unit_Data.Reinforcements)
            for (int i = 0; i < Unit_Data.Reinforcements.Count; i++)
            {
                Test_Battle_Character_Data test_battler = Test_Battle_Character_Data.from_data(
                    Unit_Data.Reinforcements[i].type, Unit_Data.Reinforcements[i].identifier, Unit_Data.Reinforcements[i].data);
                string str;// = "Team " + test_battler.Team.ToString() + ": ";
                if (test_battler.Generic)
                {
                    str = String.Format("{0}- Team {1}: {2}, {3}({4}), Lv: {5}",
                        i, test_battler.Team, test_battler.Name, Global.data_classes[test_battler.Class_Id].Name,
                        (Generic_Builds)test_battler.Build, test_battler.Level);
                    /*str += test_battler.Name + ", ";
                    string class_name = Global.data_classes[test_battler.Class_Id].Name; // should get shortened versions
                    str += class_name + "(";
                    str += ((Generic_Build)test_battler.Build).ToString() + "), ";
                    str += "Lv: " + test_battler.Level;*/
                }
                else
                    str = String.Format("{0}- Team {1}: {2}",
                        i, test_battler.Team, Global.data_actors[test_battler.Actor_Id].Name);
                //str += Global.data_actors[test_battler.Actor_Id].Name;
                commands.Add(str);
            }
            commands.Add("New Reinforcement");
            commands.Add("Paste Reinforcement");
            Reinforcements_Window = new Window_Command_Scroll_Arrow(Reinforcements_Window,
                //new Vector2(8 + (Global.player.is_on_left() ? Config.WINDOW_WIDTH - 224 : 0), 24), 216, 9, commands); //Debug
                new Vector2(8 + (Global.player.is_on_left() ? Config.WINDOW_WIDTH - 224 : 0), 24), 240, Config.WINDOW_HEIGHT / 16 - 3, commands);
            Reinforcements_Window.set_text_color(0, "Blue");
        }

        protected void update_reinforcements_menu()
        {
            if (Reinforcements_Window.is_canceled())
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                Global.game_temp.menuing = false;
                Reinforcements_Window = null;
                Global.game_map.highlight_test();
            }
            else if (Reinforcements_Window.is_selected())
            {
                // Add Reinforcement
                if (Reinforcements_Window.selected_index() ==
                    Unit_Data.Reinforcements.Count + 1)
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    Reinforcement_Index = Reinforcements_Window.index - 1;
                    int scroll = Reinforcements_Window.scroll;
                    // Add a unit
                    Unit_Data.Reinforcements.Add(new Data_Unit("character", "", "1|Actor ID\n1|Team\n0|AI Priority\n3|AI Mission"));
                    /*// Refresh the map
                    set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                    // Close menu
                    Global.game_temp.menuing = false;
                    Reinforcements_Window = null;
                    Global.game_map.highlight_test();*/

                    open_reinforcements_menu();
                    Reinforcements_Window.immediate_index = Reinforcement_Index + 1;
                    Reinforcements_Window.scroll = scroll;
                    Reinforcements_Window.refresh_scroll();
                }
                // Paste Reinforcement
                else if (Reinforcements_Window.selected_index() ==
                    Unit_Data.Reinforcements.Count + 2)
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    Reinforcement_Index = Reinforcements_Window.index - 2;
                    int scroll = Reinforcements_Window.scroll;
                    // Add a unit
                    string[] ary = Global.test_battler_1.to_string();
                    Unit_Data.Reinforcements.Add(new Data_Unit(ary[0], ary[1], ary[2]));
                    /*// Refresh the map
                    set_map(Global.game_system.New_Chapter_Id, Map_Data_Key, "", "");
                    // Close menu
                    Global.game_temp.menuing = false;
                    Reinforcements_Window = null;
                    Global.game_map.highlight_test();*/

                    open_reinforcements_menu();
                    Reinforcements_Window.immediate_index = Reinforcement_Index + 3;
                    Reinforcements_Window.scroll = scroll;
                    Reinforcements_Window.refresh_scroll();
                }
                else if (Reinforcements_Window.selected_index() == 0)
                {
                    Global.game_system.play_se(System_Sounds.Buzzer);
                }
                else
                {
                    Global.game_system.play_se(System_Sounds.Confirm);

                    Reinforcement_Index = Reinforcements_Window.index - 1;
                    Reinforcements_Window = null;
                    Global.game_map.add_reinforcement_unit(-1, Config.OFF_MAP, Reinforcement_Index, "");
                    Global.game_system.Selected_Unit_Id = Global.game_map.last_added_unit.id;
                    Global.test_battler_1 = Test_Battle_Character_Data.from_data(
                        Unit_Data.Reinforcements[Reinforcement_Index].type, Unit_Data.Reinforcements[Reinforcement_Index].identifier, Unit_Data.Reinforcements[Reinforcement_Index].data);
                    Global.test_battler_1.Actor_Id = Global.game_map.last_added_unit.actor.id;
                    Unit_Editor = new Window_Unit_Editor(true);
                }
            }
            // Delete reinforcements
            else if (Global.Input.triggered(Inputs.X))
            {
                // Add Reinforcement
                if (Reinforcements_Window.index == Unit_Data.Reinforcements.Count + 1 ||
                    Reinforcements_Window.index == Unit_Data.Reinforcements.Count + 2 ||
                    Reinforcements_Window.index == 0)
                {
                    Global.game_system.play_se(System_Sounds.Buzzer);
                }
                else
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Reinforcements_Window.active = false;
                    Delete_Reinforcement_Confirm_Window = new Window_Confirmation();
                    Delete_Reinforcement_Confirm_Window.loc = new Vector2(Global.player.is_on_left() ? 16 : Config.WINDOW_WIDTH - 96, 32);
                    Delete_Reinforcement_Confirm_Window.set_text("Delete this\nreinforcement?");
                    Delete_Reinforcement_Confirm_Window.add_choice("Yes", new Vector2(8, 32));
                    Delete_Reinforcement_Confirm_Window.add_choice("No", new Vector2(48, 32));
                    Delete_Reinforcement_Confirm_Window.size = new Vector2(88, 64);
                    Delete_Reinforcement_Confirm_Window.index = 1;
                }
            }
            else if (Global.Input.repeated(Inputs.Left) || Global.Input.repeated(Inputs.Right))
            {
                if (Reinforcements_Window.index > 0 && Reinforcements_Window.index <= Unit_Data.Reinforcements.Count)
                {
                    Global.game_system.play_se(System_Sounds.Menu_Move2);
                    Data_Unit unit = Unit_Data.Reinforcements[Reinforcements_Window.index - 1];
                    Test_Battle_Character_Data test_battler = Test_Battle_Character_Data.from_data(unit.type, unit.identifier, unit.data);
                    int team = test_battler.Team;
                    team = new_team(team, Global.Input.repeated(Inputs.Left));

                    test_battler.Team = team;
                    string[] ary = test_battler.to_string();
                    Unit_Data.Reinforcements[Reinforcements_Window.index - 1] = new Data_Unit(ary[0], ary[1], ary[2]);

                    open_reinforcements_menu();
                }
                // also set scroll, and make the cursor not jump in from the top //Yeti
            }
        }

        protected void update_delete_reinforcement_menu()
        {
            if (Delete_Reinforcement_Confirm_Window.is_ready)
            {
                if (Delete_Reinforcement_Confirm_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Reinforcements_Window.active = true;
                    Delete_Reinforcement_Confirm_Window = null;
                }
                else if (Delete_Reinforcement_Confirm_Window.is_selected())
                {
                    if (Delete_Reinforcement_Confirm_Window.index == 1)
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Reinforcements_Window.active = true;
                        Delete_Reinforcement_Confirm_Window = null;
                    }
                    else
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Delete_Reinforcement_Confirm_Window = null;

                        Reinforcement_Index = Reinforcements_Window.index - 1;
                        int scroll = Reinforcements_Window.scroll;
                        Unit_Data.Reinforcements.RemoveAt(Reinforcement_Index);
                        open_reinforcements_menu();
                        Reinforcements_Window.immediate_index = Math.Max(1,
                            Math.Min(Unit_Data.Reinforcements.Count, Reinforcement_Index + 1));
                        Reinforcements_Window.scroll = scroll;
                        Reinforcements_Window.refresh_scroll();
                    }
                }
            }
        }

        protected void update_quit_confirm()
        {
            if (Quit_Confirm_Window.is_ready)
            {
                if (Quit_Confirm_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Quit_Confirm_Window = null;
                    map_window_active = true;
                }
                else if (Quit_Confirm_Window.is_selected())
                {
                    if (Quit_Confirm_Window.index == 0)
                    {
                        Global.quit();
                    }
                    else
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        Quit_Confirm_Window = null;
                        map_window_active = true;
                    }
                }
            }
        }
        #endregion

#if DEBUG

        internal override void open_debug_menu() { }
#endif

        protected override bool update_soft_reset() { return false; }

        protected override void draw_menus(SpriteBatch sprite_batch)
        {
            base.draw_menus(sprite_batch);

            if (Clear_Unit_Window != null)
                Clear_Unit_Window.draw(sprite_batch);
            if (Reinforcements_Window != null) Reinforcements_Window.draw(sprite_batch);
            if (Unit_Editor != null) Unit_Editor.draw(sprite_batch);
            if (Quit_Confirm_Window != null) Quit_Confirm_Window.draw(sprite_batch);
            if (Cancel_Editing_Confirm_Window != null) Cancel_Editing_Confirm_Window.draw(sprite_batch);
            if (Delete_Reinforcement_Confirm_Window != null) Delete_Reinforcement_Confirm_Window.draw(sprite_batch);
        }

        protected override void clear_menus()
        {
            Unit_Editor = null;
            Reinforcements_Window = null;
            Clear_Unit_Window = null;
            Quit_Confirm_Window = null;
            Cancel_Editing_Confirm_Window = null;
            Delete_Reinforcement_Confirm_Window = null;
            base.clear_menus();
        }
    }
}
#endif