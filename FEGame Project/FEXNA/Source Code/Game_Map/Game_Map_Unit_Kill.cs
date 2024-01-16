using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEXNA
{
    partial class Game_Map
    {
        private List<int> Dying_Units = new List<int>();
        private HashSet<int> True_Dying_Units = new HashSet<int>();
        private HashSet<int> Automated_Dying_Units = new HashSet<int>();
        private bool Waiting_To_Play_Death_Sound = false;

        #region Accessors
        public List<int> dying_units { get { return Dying_Units; } }

        public bool units_dying { get { return Dying_Units.Any(); } }

        public bool units_true_dying { get { return True_Dying_Units.Count != 0; } }
        #endregion

        protected void update_dying_units()
        {
            if (Dying_Units.Count > 0)
            {
                if (Global.scene.is_message_window_active)
                    return;
                if (Waiting_To_Play_Death_Sound)
                {
                    Global.Audio.play_se("Map Sounds", "Death");
                    Waiting_To_Play_Death_Sound = false;
                }
                bool skip_true_dying = True_Dying_Units.Count < Automated_Dying_Units.Count;
                bool automated_only = true;
                bool unit_true_killed = false;
                int i = 0;
                while (i < Dying_Units.Count)
                {
                    // Handle all not truly dying units before all truly dying units
                    if (skip_true_dying && True_Dying_Units.Contains(Dying_Units[i]))
                    {
                        i++;
                        continue;
                    }

                    if (Automated_Dying_Units.Contains(Dying_Units[i]))
                        Units[Dying_Units[i]].update_attack_graphics(); //Debug
                    else
                        automated_only = false;

                    if (!Units[Dying_Units[i]].changing_opacity())
                    {
                        // If the unit is dying from an event
                        if (Automated_Dying_Units.Contains(Dying_Units[i]))
                        {
                            // If the unit is also actually being killed
                            if (True_Dying_Units.Contains(Dying_Units[i]))
                            {
                                True_Dying_Units.Remove(Dying_Units[i]);
                                Units[Dying_Units[i]].kill();
                                unit_true_killed = true;
                            }
                            // Else just remove them from the map as events normally do
                            else
                            {
                                add_unit_move_range_update(Units[Dying_Units[i]]);
                                remove_unit(Dying_Units[i]);
                            }
                            Automated_Dying_Units.Remove(Dying_Units[i]);
                        }
                        //else if (Units[Dying_Units[i]].is_dead) //Debug
                        //    Units[Dying_Units[i]].kill();
                        Dying_Units.RemoveAt(i);
                    }
                    else
                        i++;
                }
                if (unit_true_killed)
                {
                    refresh_move_ranges();
                    Global.game_map.wait_for_move_update();
                    Global.game_state.any_trigger_events();
                }
                //if (Dying_Units.Count == 0 && automated_only) //Debug
                //    refresh_move_ranges();
            }
        }

        internal void add_dying_unit_animation(Game_Unit unit)
        {
            add_dying_unit_animation(unit.id);
        }
        internal void add_dying_unit_animation(int unit_id, bool automated = false)
        {
            if (!Dying_Units.Contains(unit_id))
            {
                Units[unit_id].kill_color();
                if(Units[unit_id].actor.id==29)
                {
                    Global.Audio.play_se("Map Sounds", "GrimaTransEffect");
                    get_scene_map().set_map_effect(Units[unit_id].loc, 0, (11)); //tgj
                }
                if (Units[unit_id].actor.has_skill("WARP")) //wark
                {
                    Global.Audio.play_se("Map Sounds", "Pure_Water");
                    get_scene_map().set_map_effect(Units[unit_id].loc, 0, (10));
                }
                else
                {
                    Waiting_To_Play_Death_Sound = Global.scene.is_message_window_active;
                    if (!Waiting_To_Play_Death_Sound)
                        Global.Audio.play_se("Map Sounds", "Death");
                }

                Dying_Units.Add(unit_id);
                if (automated)
                    Automated_Dying_Units.Add(unit_id);
            }
        }

        public void add_true_dying_unit_animation(int unit_id, bool automated = false)
        {
            if (!Dying_Units.Contains(unit_id))
            {
                play_death_quote(unit_id);
                add_dying_unit_animation(unit_id, true);
                True_Dying_Units.Add(unit_id);
            }
        }

        public void play_death_quote(int unit_id)
        {
            if (!string.IsNullOrEmpty(Global.game_state.get_death_quote(unit_id)))
            {
                Game_Unit dying_unit = Global.game_map.units[unit_id];
                Global.game_temp.message_text = Global.death_quotes[Global.game_state.get_death_quote(unit_id)];
                Global.scene.new_message_window();
                if (!dying_unit.is_opposition)
                    Global.scene.message_reverse();
                if (dying_unit.is_player_team || Global.game_system.loss_on_death.Contains(dying_unit.actor.id)) //Multi
                {
                    //Global.Audio.bgm_fade(15);
                    if (Global.game_system.loss_on_death.Contains(dying_unit.actor.id) ||
                        Global.game_system.ally_loss_on_death.Contains(dying_unit.actor.id))
                    {
                        //Global.Audio.play_bgm(Config.GAME_OVER_THEME);
                    }
                    else
                    {
                        //Global.Audio.play_bgm(Config.ALLY_DEATH_THEME);
                    }
                }
            }
        }
    }
}
