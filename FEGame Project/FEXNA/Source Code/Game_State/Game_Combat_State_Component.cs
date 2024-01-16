﻿using System.IO;
using Microsoft.Xna.Framework;
using System.Linq;

namespace FEXNA.State
{
    abstract class Game_Combat_State_Component : Game_State_Component
    {
        protected bool In_Staff_Use = false;

        protected bool Skip_Battle = false;
        protected bool Transition_To_Battle = false;
        protected int Battle_Transition_Timer = 0;
        protected bool Skipping_Battle_Scene = false;
        protected bool Map_Battle = false;

        #region Serialization
        internal override void write(BinaryWriter writer)
        {
            writer.Write(In_Staff_Use);
        }

        internal override void read(BinaryReader reader)
        {
            In_Staff_Use = reader.ReadBoolean();
        }
        #endregion

        #region Accessors
        public bool transition_to_battle
        {
            get { return Transition_To_Battle || Battle_Transition_Timer != 0; }
        }

        public int battle_transition_timer
        {
            get
            {
                return Transition_To_Battle ?
                    Battle_Transition_Timer :
                    Constants.BattleScene.BATTLE_TRANSITION_TIME - Battle_Transition_Timer;
            }
        }

        protected int exp_gauge_gain
        {
            get { return Global.game_state.exp_gauge_gain; }
            set { Global.game_state.exp_gauge_gain = value; }
        }
        #endregion

        protected Combat_Map_Object attackable_map_object(int id)
        {
            return Global.game_map.attackable_map_object(id);
        }

        protected int combat_distance(int id1, int id2)
        {
            return Global.game_map.combat_distance(id1, id2);
        }

        protected void set_animation_mode(int id1, int id2, bool fighting, Combat_Data data)
        {
            bool scene_battle = is_scene_battle(id1, id2, fighting, data);
            // If not using map battle mode, ensure battlers actually have sprites for scene battle mode
            if (scene_battle)
            {
                foreach (int id in new int[] { id1, id2 })
                {
                    if (!Units.ContainsKey(id))
                    {
                        scene_battle = false;
                        break;
                    }
                    Game_Unit unit = Units[id];
                    if (Global.game_state.dance_active && id == id1 ? !FE_Battler_Image_Wrapper.test_for_battler(unit, Global.weapon_types[0].AnimName) :
                        !FE_Battler_Image_Wrapper.test_for_battler(unit))
                    {
                        scene_battle = false;
                        break;
                    }
                }
            }
            Global.game_system.Battle_Mode = scene_battle ?
                Animation_Modes.Full : Animation_Modes.Map;
        }

        private bool is_scene_battle(int id1, int id2, bool fighting, Combat_Data data)
        {
            if (Skip_Battle)
                return false;
            if (Global.game_temp.scripted_battle)
                return Global.game_temp.scripted_battle_stats.scene_battle;
            // If map animation forced, for various reasons
            else if ((In_Staff_Use && ((Staff_Data)data).mode == Staff_Modes.Torch) ||
                    Units[id1].trample_activated) // make this not hardcoded //Debug
                return false;
            // If either battler is a boss, or it's a scripted battle with animations forced, always go to the full battle scene
            else if (fighting && (Global.game_temp.scripted_battle || is_boss_anim_forced(id1, id2)))
                return true;

            bool scene_battle = true;
            switch ((Animation_Modes)Global.game_options.animation_mode)
            {
                // If solo animation mode, get the animation mode the fighting units should use
                case Animation_Modes.Solo:
                    scene_battle = false;
                    // Healing staff use or Dancing
                    if ((In_Staff_Use || Global.game_state.dance_active) && id2 != -1 &&
                        !Units[id1].is_attackable_team(Units[id2]))
                    {
                        foreach (int id in new int[] { id1 })
                            if (id != -1 && !scene_battle)
                                if (Units[id].is_ally && !Global.game_actors.is_temp_actor(Units[id].actor))
                                    if (Units[id].actor.individual_animation != (int)Animation_Modes.Map)
                                        scene_battle = (Animation_Modes)Units[id].actor.individual_animation == Animation_Modes.Full;
                                        //anim_mode = (Animation_Modes)Units[id].actor.individual_animation; //Debug
                    }
                    // Anything else
                    else
                        foreach (int id in new int[] { id1, id2 })
                            if (id != -1 && !scene_battle)
                                if (Units[id].is_ally && !Global.game_actors.is_temp_actor(Units[id].actor))
                                    if (Units[id].actor.individual_animation != (int)Animation_Modes.Map)
                                        scene_battle = (Animation_Modes)Units[id].actor.individual_animation == Animation_Modes.Full;
                                        //anim_mode = (Animation_Modes)Units[id].actor.individual_animation; //Debug
                    break;
                case Animation_Modes.Full:
                    scene_battle = true;
                    break;
                case Animation_Modes.Map:
                    scene_battle = false;
                    break;
                case Animation_Modes.Player_Only:
                    scene_battle = Constants.Team.PLAYABLE_TEAMS.Contains(Team_Turn);
                    break;
            }
            // Switch animation mode if holding L
            if (Global.Input.pressed(Inputs.L))
            {
                scene_battle = !scene_battle;
                /*if (anim_mode == Animation_Modes.Map) //Debug
                    anim_mode = (int)Animation_Modes.Full;
                else if (anim_mode == (int)Animation_Modes.Full)
                    anim_mode = Animation_Modes.Map;*/
            }
            return scene_battle;
        }

        private bool is_boss_anim_forced(int id1, int id2)
        {
            return Constants.Map.FORCE_BOSS_ANIMATIONS &&
                (Units[id1].boss || (Units.ContainsKey(id2) && Units[id2].boss));
        }
    }
}
