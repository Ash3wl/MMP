﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using FEXNA_Library;

namespace FEXNA
{
    partial class Game_Map
    {
        protected int Last_Added_Unit_Id = 0;
        protected List<int> Preparations_Unit_Team = null;

        #region Accessors
        public List<int> preparations_unit_team { get { return Preparations_Unit_Team; } }
        #endregion

        internal Game_Unit last_added_unit
        {
            get
            {
                if (!Units.ContainsKey(Last_Added_Unit_Id))
                    return null;
                return Units[Last_Added_Unit_Id];
            }
        }

        public void remove_unit(int id)
        {
            if (Units.ContainsKey(id))
            {
                if (!Units[id].gladiator)
                    Global.game_state.Update_Victory_Theme = true;
                //Last_Added_Unit something
                Units[id].remove_old_unit_location();
                Global.scene.remove_map_sprite(id);
                Units.Remove(id);
                Global.game_state.remove_ai_unit(id);
                foreach (List<int> team in Teams)
                    team.Remove(id);
                if (Global.game_system.Selected_Unit_Id == id)
                    Global.game_system.Selected_Unit_Id = -1;
                //Delete saved event
            }
        }
        public void completely_remove_unit(int id)
        {
            if (Last_Added_Unit_Id == id && Units.ContainsKey(Last_Added_Unit_Id))
            {
                remove_unit(id);
                Last_Added_Unit_Id--;
            }
            else
                remove_unit(id);
        }

        #region Add Units
        protected void add_unit(Vector2 loc, Data_Unit data)
        {
            add_unit(loc, data, data.identifier);
        }
        protected void add_unit(Vector2 loc, Data_Unit data, string identifier, int team = -1)
        {
            int count = Units.Count;

            if (string.IsNullOrEmpty(identifier))
                identifier = data.identifier;
            switch (data.type)
            {
                case "character":
                    add_character_unit(loc, team, identifier, data.data.Split('\n'));
                    break;
                case "generic":
                    add_generic_unit(loc, team, identifier, data.data.Split('\n'));
                    break;
                case "temporary":
                    add_temporary_unit(loc, team, identifier, data.data.Split('\n'));
                    break;
            }
            if (Units.Count > count)
            {
                var unit = Units[Last_Added_Unit_Id];
                // If a dead actor, give them 1 hp so they can limp around unless they're a dead PC
                if (unit.actor.is_out_of_lives() && unit.is_player_team)
                    unit.hp = 0;
                else
                    unit.actor.hp = Math.Max(1, unit.actor.hp);

                unit.actor.setup_items();
                unit.actor.staff_fix();
            }
        }

        protected void add_character_unit(Vector2 loc, int team, string identifier, string[] data_ary)
        {
            int id = Convert.ToInt32(data_ary[0].Split('|')[0]);
            if (team <= 0)
                team = Convert.ToInt32(data_ary[1].Split('|')[0]);
            int priority = Convert.ToInt32(data_ary[2].Split('|')[0]);
            int mission = Convert.ToInt32(data_ary[3].Split('|')[0]);
            new_unit_id();
            Units.Add(Last_Added_Unit_Id, new Game_Unit(Last_Added_Unit_Id, loc, team, priority, id));
            // If a dead actor, and not out of lives or anything, give them 1 hp so they can limp around
            if (Units[Last_Added_Unit_Id].is_dead && !Units[Last_Added_Unit_Id].actor.is_out_of_lives())
                Units[Last_Added_Unit_Id].actor.hp = 1;
            Units[Last_Added_Unit_Id].full_ai_mission = mission;
            if (identifier != "")
                Unit_Identifiers[identifier] = Last_Added_Unit_Id;
        }

        protected void add_generic_unit(Vector2 loc, int team, string identifier, string[] data_ary)
        {
            Game_Actor actor = Global.game_actors.new_actor();
            actor.name = data_ary[0].Split('|')[0];
            actor.class_id = Convert.ToInt32(data_ary[1].Split('|')[0]); //Debug
            actor.gender = Convert.ToInt32(data_ary[2].Split('|')[0]);
            actor.level_down();
            actor.exp = 0;
            
            int level = Convert.ToInt32(data_ary[3].Split('|')[0]);
            int exp = Convert.ToInt32(data_ary[4].Split('|')[0]);
            int prepromote_levels = Convert.ToInt32(data_ary[6].Split('|')[0]);
            int build = Convert.ToInt32(data_ary[7].Split('|')[0]);
            int con = Convert.ToInt32(data_ary[8].Split('|')[0]);

            // If units were created with NUM_ITEMS = 6,
            // and then NUM_ITEMS was changed to 4 or 6, things would break
            // Come up with a longterm solution//Yeti
            for (int item_index = 0; item_index < Constants.Actor.NUM_ITEMS; item_index++)
            {
                string[] item_ary = data_ary[11 + item_index].Split('|')[0].Split(new string[] { ", " }, StringSplitOptions.None);
                actor.items[item_index] = new Item_Data(Convert.ToInt32(item_ary[0]),
                    Convert.ToInt32(item_ary[1]), Convert.ToInt32(item_ary[2]));
            }

            int index_after_items = 11 + Constants.Actor.NUM_ITEMS;
            string[] wexp_ary = data_ary[index_after_items + 0]
                .Split('|')[0].Split(new string[] { ", " }, StringSplitOptions.None);
            int[] wexp = Enumerable.Range(0, Global.weapon_types.Count - 1)
                .Select(x => x < wexp_ary.Length ? Convert.ToInt32(wexp_ary[x]) : 0)
                .ToArray();

            actor.setup_generic(
                Convert.ToInt32(data_ary[1].Split('|')[0]), level, exp,
                prepromote_levels, (Generic_Builds)build, con, wexp: wexp);
            
            if (team <= 0)
                team = Convert.ToInt32(data_ary[5].Split('|')[0]);
            int priority = Convert.ToInt32(data_ary[9].Split('|')[0]);
            int mission = Convert.ToInt32(data_ary[10].Split('|')[0]);
            new_unit_id();
            Units.Add(Last_Added_Unit_Id, new Game_Unit(Last_Added_Unit_Id, loc, team, priority, actor.id));
            Units[Last_Added_Unit_Id].full_ai_mission = mission;
            if (identifier != "")
                Unit_Identifiers[identifier] = Last_Added_Unit_Id;
        }

        protected void add_temporary_unit(Vector2 loc, int team, string identifier, string[] data_ary)
        {
            Game_Actor actor = Global.game_actors.new_actor();
            actor.name = data_ary[0].Split('|')[0];
            actor.class_id = Convert.ToInt32(data_ary[1].Split('|')[0]);
            actor.gender = Convert.ToInt32(data_ary[2].Split('|')[0]);
            actor.level = Convert.ToInt32(data_ary[3].Split('|')[0]);
            actor.exp = Convert.ToInt32(data_ary[4].Split('|')[0]);
            //actor.maxhp = Convert.ToInt32(data_ary[6].Split('|')[0]);
            actor.hp = Convert.ToInt32(data_ary[7].Split('|')[0]);
            /*actor.stats[0] = Convert.ToInt32(data_ary[8].Split('|')[0]);
            actor.stats[1] = Convert.ToInt32(data_ary[9].Split('|')[0]);
            actor.stats[2] = Convert.ToInt32(data_ary[10].Split('|')[0]);
            actor.stats[3] = Convert.ToInt32(data_ary[11].Split('|')[0]);
            actor.stats[4] = Convert.ToInt32(data_ary[12].Split('|')[0]);
            actor.stats[5] = Convert.ToInt32(data_ary[13].Split('|')[0]);
            actor.stats[6] = Convert.ToInt32(data_ary[14].Split('|')[0]);*/

            // If units were created with NUM_ITEMS = 6,
            // and then NUM_ITEMS was changed to 4 or 6, things would break
            // Come up with a longterm solution//Yeti
            for (int item_index = 0; item_index < Constants.Actor.NUM_ITEMS; item_index++)
            {
                string[] item_ary = data_ary[17 + item_index].Split('|')[0].Split(new string[] { ", " }, StringSplitOptions.None);
                actor.items[item_index] = new Item_Data(Convert.ToInt32(item_ary[0]),
                    Convert.ToInt32(item_ary[1]), Convert.ToInt32(item_ary[2]));
            }

            int index_after_items = 17 + Constants.Actor.NUM_ITEMS;
            string[] wexp_ary = data_ary[index_after_items]
                .Split('|')[0].Split(new string[] { ", " }, StringSplitOptions.None);
            for (int i = 0; i < Global.weapon_types.Count - 1; i++)
                if (i < wexp_ary.Length)
                    actor.wexp_set(Global.weapon_types[i + 1], Convert.ToInt32(wexp_ary[i]), false);
            actor.clear_wlvl_up();
            
            if (team <= 0)
                team = Convert.ToInt32(data_ary[5].Split('|')[0]);
            int priority = Convert.ToInt32(data_ary[15].Split('|')[0]);
            int mission = Convert.ToInt32(data_ary[16].Split('|')[0]);
            new_unit_id();
            Units.Add(Last_Added_Unit_Id, new Game_Unit(Last_Added_Unit_Id, loc, team, priority, actor.id));
            Units[Last_Added_Unit_Id].full_ai_mission = mission;
            if (identifier != "")
                Unit_Identifiers[identifier] = Last_Added_Unit_Id;
        }

        public bool add_actor_unit(int team, Vector2 loc, int actor_id, string identifier)
        {
#if DEBUG
            if (Global.scene.is_map_scene && Global.scene.scene_type != "Scene_Map_Unit_Editor" && !Global.data_actors.ContainsKey(actor_id))
                Print.message("Adding an actor unit with actor id " + actor_id.ToString() + "\nThis actor id has no data defined.\nAre you sure this id is correct?");
#endif
            new_unit_id();
            Units.Add(Last_Added_Unit_Id, new Game_Unit(Last_Added_Unit_Id, loc, team, 0, actor_id));
            var unit = Units[Last_Added_Unit_Id];

            // If a dead actor, give them 1 hp so they can limp around unless they're a dead PC
            if (unit.actor.is_out_of_lives() && unit.is_player_team)
                unit.hp = 0;
            else
                unit.actor.hp = Math.Max(1, unit.actor.hp);
            if (identifier != "")
                Unit_Identifiers[identifier] = Last_Added_Unit_Id;
            return true;
        }
        public bool replace_actor_unit(int team, Vector2 loc, int actor_id, int old_unit_id)
        {
            if (Last_Added_Unit_Id == old_unit_id && Units.ContainsKey(Last_Added_Unit_Id))
            {
                completely_remove_unit(Last_Added_Unit_Id);
            }
            return add_actor_unit(team, loc, actor_id, "");
        }

        public bool add_undeployed_battalion_unit(int team, Vector2 loc, int index, string identifier)
        {
            int id = Global.battalion.undeployed_actor(index);
            if (id == -1)
                return false;
            return add_actor_unit(team, loc, id, identifier);
        }

        public bool add_temp_unit(int team, Vector2 loc, int class_id, int gender, string identifier)
        {
            new_unit_id();
            Units.Add(Last_Added_Unit_Id, new Game_Unit(Last_Added_Unit_Id, loc, team, 0));
            Units[Last_Added_Unit_Id].actor.class_id = class_id;
            Units[Last_Added_Unit_Id].actor.gender = gender;
            Units[Last_Added_Unit_Id].refresh_sprite();
            if (identifier != "")
                Unit_Identifiers[identifier] = Last_Added_Unit_Id;
            return true;
        }
        public bool add_gladiator(int team, Vector2 loc, int class_id, int gender, string identifier)
        {
            bool update_victory_theme = Global.game_state.Update_Victory_Theme;
            bool result = add_temp_unit(team, loc, class_id, gender, identifier);
            if (result)
                last_added_unit.gladiator = true;
            Global.game_state.Update_Victory_Theme = update_victory_theme;
            return result;
        }

        public bool add_reinforcement_unit(int team, Vector2 loc, int index, string identifier)
        {
            if (index >= Unit_Data.Reinforcements.Count)
                return false;
            //new_unit_id(); //This shouldn't be needed right
            add_unit(loc, Unit_Data.Reinforcements[index], identifier, team);
            return true;
        }
        #endregion

        #region Add Destroyables
        public void add_destroyable_object(Vector2 loc, int hp, string event_name)
        {
            if (is_off_map(loc))
                return;
            new_unit_id();
            Destroyable_Objects.Add(Last_Added_Unit_Id, new Destroyable_Object(Last_Added_Unit_Id, loc, hp, event_name));
            Destroyable_Locations[(int)loc.X, (int)loc.Y] = Last_Added_Unit_Id + 1;
        }

        internal void remove_destroyable(int id, bool refresh_move_ranges = false) //private //Yeti
        {
            if (Destroyable_Objects.ContainsKey(id))
            {
                Destroyable_Locations[(int)Destroyable_Objects[id].loc.X, (int)Destroyable_Objects[id].loc.Y] = 0;
                Global.game_state.activate_event_by_name(Destroyable_Objects[id].event_name);
                Destroyable_Objects.Remove(id);

                if (refresh_move_ranges)
                    Refresh_All_Ranges = true;
            }
        }
        #endregion

        #region Add Siege Engines
        public void add_siege_engine(Vector2 loc, Item_Data item)
        {
            if (is_off_map(loc))
                return;
            new_unit_id();
            Siege_Engines.Add(Last_Added_Unit_Id, new Siege_Engine(Last_Added_Unit_Id, loc, item));
            Siege_Locations[(int)loc.X, (int)loc.Y] = Last_Added_Unit_Id + 1;
            if (get_unit(loc) != null)
                get_unit(loc).refresh_sprite();
            clear_updated_attack_ranges();
            clear_updated_staff_ranges();
        }

        protected void remove_siege_engine(int id)
        {
            if (Siege_Engines.ContainsKey(id))
            {
                Siege_Locations[(int)Siege_Engines[id].loc.X, (int)Siege_Engines[id].loc.Y] = 0;
                Siege_Engines.Remove(id);
                if (get_scene_map() != null)
                    get_scene_map().remove_map_sprite(id);
            }
        }
        #endregion

        protected void new_unit_id()
        {
            Last_Added_Unit_Id++;
            while (Units.ContainsKey(Last_Added_Unit_Id))
                Last_Added_Unit_Id++;
            if (Global.Audio.playing_map_theme())
                Global.game_state.Update_Victory_Theme = true;
        }

        public void init_preparations_unit_team()
        {
            clear_preparations_unit_team();
            Preparations_Unit_Team = new List<int>();

            for (int i = 0; i < Global.battalion.actors.Count; i++)
            {
                add_actor_unit(Global.game_state.team_turn, Config.OFF_MAP, Global.battalion.actors[i], "");
                Preparations_Unit_Team.Add(Last_Added_Unit_Id);
            }
        }

        public void clear_preparations_unit_team()
        {
            if (Preparations_Unit_Team != null)
                for (int i = 0; i < Preparations_Unit_Team.Count; i++)
                    remove_unit(Preparations_Unit_Team[i]);
            Preparations_Unit_Team = null;
        }
    }
}