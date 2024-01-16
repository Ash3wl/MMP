using System;
using System.Collections.Generic;
using System.Linq;

namespace FEXNA
{
    internal class Combat_Round_Data
    {
        public int Attacker;
        public List<int?> Stats;
        public Attack_Result Result;
        public bool First_Attack = true;

        internal Combat_Round_Data() { }
        internal Combat_Round_Data(Game_Unit battler_1, Game_Unit battler_2, int distance)
        {
            List<int?> ary = Combat.combat_stats(battler_1.id, battler_2 == null ? null : (int?)battler_2.id, distance);
            Stats = ary;
        }
        internal Combat_Round_Data(Game_Unit battler_1, Game_Unit battler_2, Scripted_Combat_Stats stats)
        {
            Stats = new List<int?>();
            for (int i = 0; i < 4; i++)
                Stats.Add(stats.Stats_1.Count < i + 1 ? null : (int?)stats.Stats_1[i]);
            for (int i = 0; i < 4; i++)
                Stats.Add(stats.Stats_2.Count < i + 1 ? null : (int?)stats.Stats_2[i]);
        }

        internal void set_attack(Game_Unit battler_1, Game_Unit battler_2, int distance, List<Combat_Action_Data> action_data)
        {
            Game_Unit battler = Attacker == 1 ? battler_1 : battler_2;
            Game_Unit target = Attacker == 2 ? battler_1 : battler_2;
            // In case of skills like Astra that turn skill_activated back on at the end of reset_skills()
            battler.skill_activated = false;
            if (target != null)
                target.skill_activated = false;

            List<int?> combat_stats;
            int hit = 100, dmg = 0;
            bool hit_skill_changed = false;
            if (target != null)
            {
                combat_stats = Combat.combat_stats(battler.id, target.id, distance);
                if (combat_stats[0] != null)
                    hit = (int)combat_stats[0];
                if (combat_stats[1] != null)
                    dmg = (int)combat_stats[1];
                hit_skill_changed = false;
            }


            FEXNA_Library.Data_Weapon weapon = battler.actor.weapon;
            // Pre hit skill check, Defender (Bastion)
            if (!skip_skill_update(battler, target) && target != null)
            {
                target.prehit_def_skill_check(battler);
                if (target.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 2,
                        Trigger = (int)Combat_Action_Triggers.Attack,
                        Skill_Ids = target.hit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    if (hit != (int)(Attacker == 1 ? action_data.Last().Hit1 : action_data.Last().Hit2))
                        hit_skill_changed = true;
                    // Turns skills activated at all back off so further checks can be accurate
                    target.skill_activated = false;
                }
            }
            // Pre hit skill check, Attcker (Spiral Dive?)
            if (!skip_skill_update(battler, target))
            {
                if (First_Attack)
                    battler.activate_masteries();
                battler.prehit_skill_check(target, distance);
                 if (battler.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 1,
                        Trigger = (int)Combat_Action_Triggers.Attack,
                        Skill_Ids = battler.hit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    if (hit != (int)(Attacker == 1 ? action_data.Last().Hit1 : action_data.Last().Hit2))
                        hit_skill_changed = true;
                    // Turns skills activated at all back off so further checks can be accurate
                    battler.skill_activated = false;
                }
            }
            bool is_hit = true, is_crt = false;
            if (target != null)
            {
                combat_stats = Combat.combat_stats(battler.id, target.id, distance);
                // Hit hasn't changed
                hit = attack_hit(hit, combat_stats[0] ?? hit, hit_skill_changed);
                int crt = combat_stats[2] ?? -1;
                if (target.actor.has_skill("MEMBER") && hit != 0 && hit != 100)
                    hit = (hit + 200)/3;
                // Hit test
                is_hit = true;
                is_crt = false;

                if (Stats[Attacker == 1 ? 0 : 4] != null)
                    Combat.test_hit(hit, crt, distance, out is_hit, out is_crt);

#if DEBUG
                //Cheat codes
                if (
                    false &&
                    Global.scene.scene_type != "Scene_Test_Battle" &&
                    !(this is Staff_Round_Data))
                {
                    // Only for PCs
                    if (true)
                    {
                        is_hit = battler.is_player_team ||
                            (is_hit && (!target.is_ally ||
                                combat_stats[1] * (is_crt ? Constants.Combat.CRIT_MULT : 1) < target.hp)); //Debug
                        if (battler.is_player_team)
                            is_crt = Global.game_system.roll_rng(50);
                    }
                    // For NPCs also
                    else
                    {
                        is_hit = !battler.is_attackable_team(Constants.Team.PLAYER_TEAM) ||
                            (is_hit && (!target.is_ally || combat_stats[1] < target.hp)); //Debug
                        is_crt = !battler.is_attackable_team(Constants.Team.PLAYER_TEAM) || !is_hit;
                        is_crt = is_crt && Global.game_system.roll_rng(50);//!battler.is_attackable_team(Config.PLAYER_TEAM); //Debug
                    }

                    //is_hit = !battler.is_attackable_team(Config.PLAYER_TEAM); //Debug
                    //is_crt = !battler.is_attackable_team(Config.PLAYER_TEAM);
                    //is_crt = Global.game_system.roll_rng(50);//!battler.is_attackable_team(Config.PLAYER_TEAM); //Debug
                }
#endif
            }
            // Post hit skill activation (Astra)
            if (!skip_skill_update(battler, target))
            {
                if (First_Attack)
                    battler.mastery_hit_confirm(is_hit);
                battler.hit_skill_check(is_hit, is_crt, target, distance);
                if (battler.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 1,
                        Trigger = (int)Combat_Action_Triggers.Skill,
                        Skill_Ids = battler.hit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns 'skills activated at all' back off so further checks can be accurate
                    battler.skill_activated = false;
                }
            }
            battler.hit_skill_update();
            // On hit skill activation (Determination) //Yeti
            
            if (target != null && !skip_skill_update(battler, target)) //yeti
            {
                target.onhit_skill_check(is_hit, target, distance);
                if (target.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 2,
                        Trigger = (int)Combat_Action_Triggers.Hit,
                        Skill_Ids = target.onhit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    target.skill_activated = false;
                }
            }
            // Calculate attack

            if (target != null)
            {
                combat_stats = Combat.combat_stats(battler.id, target.id, distance);
                if (combat_stats[1] != null)
                    dmg = attack_dmg(dmg, (int)combat_stats[1]);
            }

            Result = calculate_attack(battler, target, distance, dmg, is_hit, is_crt, weapon);

            if (battler_2 != null)
                cause_damage(battler_1.id, battler_2.id, true);
            // Attack end skill activation (Adept) //Yeti
            if (!skip_skill_update(battler, target))
            {
                battler.posthit_skill_check(target);
                if (battler.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 1,
                        Trigger = (int)Combat_Action_Triggers.Return,
                        Skill_Ids = battler.posthit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    battler.skill_activated = false;
                }

            }
            // Reset skills
            if (!skip_skill_update(battler, target))
            {
                battler.reset_skills();
                if (target != null)
                    target.reset_skills();
            }
        }
        internal void set_attack(Game_Unit battler_1, Game_Unit battler_2, int distance, List<Combat_Action_Data> action_data, Scripted_Combat_Stats stats)
        {
            Game_Unit battler = Attacker == 1 ? battler_1 : battler_2;
            Game_Unit target = Attacker == 2 ? battler_1 : battler_2;
            // In case of skills like Astra that turn skill_activated back on at the end of reset_skills()
            battler.skill_activated = false;
            if (target != null)
                target.skill_activated = false;

            FEXNA_Library.Data_Weapon weapon = battler.actor.weapon;
            // Pre hit skill check, Defender (Bastion)
            if (!skip_skill_update(battler, target) && target != null)
            {
                //target.prehit_def_skill_check(battler); //Debug
                if (target.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 2,
                        Trigger = (int)Combat_Action_Triggers.Attack,
                        Skill_Ids = target.hit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    target.skill_activated = false;
                }
            }
            // Pre hit skill check, Attcker (Spiral Dive?)
            if (!skip_skill_update(battler, target))
            {
                //if (First_Attack) //Debug
                //    battler.activate_masteries();
                //battler.prehit_skill_check(target, distance); //Debug
                 if (battler.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 1,
                        Trigger = (int)Combat_Action_Triggers.Attack,
                        Skill_Ids = battler.hit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    battler.skill_activated = false;
                }
            }
            // Hit test
            //bool hit = stats.Result != Attack_Results.Miss; //Debug
            //bool crt = hit && stats.Result == Attack_Results.Crit;

            // Post hit skill activation (Astra)
            if (!skip_skill_update(battler, target))
            {
                //if (First_Attack) //Debug
                //    battler.mastery_hit_confirm(hit);
                //battler.hit_skill_check(hit, crt, target, distance); //Debug
                if (battler.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 1,
                        Trigger = (int)Combat_Action_Triggers.Skill,
                        Skill_Ids = battler.hit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    battler.skill_activated = false;
                }
            }
            battler.hit_skill_update();
            // On hit skill activation (Determination) //Yeti
            
            if (target != null && !skip_skill_update(battler, target)) //yeti
            {
                //target.onhit_skill_check(hit, target, distance); //Debug
                if (target.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 2,
                        Trigger = (int)Combat_Action_Triggers.Hit,
                        Skill_Ids = target.onhit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    target.skill_activated = false;
                }
                
            }
            // Calculate attack
            //Result = calculate_attack(battler, target, distance, hit, crt, weapon);
            Result = calculate_attack(battler, target, distance, weapon, stats);

            if (battler_2 != null)
                cause_damage(battler_1.id, battler_2.id, true);
            
            // Attack end skill activation (Adept) //Yeti
            if (!skip_skill_update(battler, target))
            {
                //battler.posthit_skill_check(target); //Debug
                if (battler.skill_activated)
                {
                    action_data.Add(new Combat_Action_Data
                    {
                        Battler_Index = 1,
                        Trigger = (int)Combat_Action_Triggers.Return,
                        Skill_Ids = battler.posthit_skill_ids(),
                        combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance)
                    });
                    // Turns skills activated at all back off so further checks can be accurate
                    battler.skill_activated = false;
                }
            }
            // Reset skills
            if (!skip_skill_update(battler, target))
            {
                battler.reset_skills();
                if (target != null)
                    target.reset_skills();
            }
        }

        protected virtual int attack_hit(int original_hit, int current_hit, bool hit_skill_changed)
        {
            return current_hit;
        }
        protected virtual int attack_dmg(int original_dmg, int current_dmg)
        {
            return current_dmg;
        }

        protected virtual Attack_Result calculate_attack(Game_Unit battler, Game_Unit target, int distance, int dmg, bool hit, bool crt,
            FEXNA_Library.Data_Weapon weapon)
        {
            return Combat.set_attack(battler, target, distance, dmg, hit, crt, weapon);
        }
        private Attack_Result calculate_attack(Game_Unit battler, Game_Unit target, int distance,
            FEXNA_Library.Data_Weapon weapon, Scripted_Combat_Stats stats)
        {
            return Combat.set_attack(battler, target, distance, weapon, stats);
        }
        protected virtual Attack_Result calculate_attack(Game_Unit battler, Destroyable_Object target, int distance, int dmg, bool hit, bool crt,
            FEXNA_Library.Data_Weapon weapon)
        {
            return Combat.set_attack(battler, target, distance, dmg, hit, crt, weapon);
        }

        public virtual void end_battle(Game_Unit battler_1, Combat_Map_Object battler_2, int distance, List<Combat_Action_Data> action_data)
        {
            action_data.Add(new Combat_Action_Data
            {
                Battler_Index = 0,
                Trigger = (int)Combat_Action_Triggers.End,
                combat_stats = Combat.combat_stats(battler_1.id, battler_2 == null ? null : (int?)battler_2.id, distance)
            });
        }
        internal void end_battle(Game_Unit battler_1, Combat_Map_Object battler_2, Scripted_Combat_Stats stats, List<Combat_Action_Data> action_data)
        {
            List<int?> list = new List<int?>();
            for (int i = 0; i < 4; i++)
                list.Add(stats.Stats_1.Count < i + 1 ? null : (int?)stats.Stats_1[i]);
            for (int i = 0; i < 4; i++)
                list.Add(stats.Stats_2.Count < i + 1 ? null : (int?)stats.Stats_2[i]);
            action_data.Add(new Combat_Action_Data
            {
                Battler_Index = 0,
                Trigger = (int)Combat_Action_Triggers.End,
                combat_stats = list
            });
        }

        protected bool skip_skill_update(Game_Unit battler_1, Game_Unit battler_2)
        {
            return (battler_1.skip_skill_update() || (battler_2 == null ? false : battler_2.skip_skill_update()));
        }

        public virtual bool is_successful_hit(FEXNA_Library.Data_Weapon weapon)
        {
            // If the attacker didn't hit themself, returns true when damage was successfully caused/the opponent was slain
            return !Result.backfire && (Result.dmg > 0 || Result.kill);

            if (!Result.backfire) //Debug
            {
                // Technically checks if attacks miss, since missed attacks do no damage
                return (Result.dmg > 0 || Result.kill); //Debug
                if (Attacker == 1 && Stats[1] > 0)
                {
                    return true;
                }
                else if (Attacker == 2 && Stats[5] > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void cause_damage(int battler_1_id)
        {
            cause_damage(Global.game_map.attackable_map_object(battler_1_id));
        }
        private void cause_damage(Combat_Map_Object battler_1) { }
        public void cause_damage(int battler_1_id, int battler_2_id)
        {
            cause_damage(battler_1_id, battler_2_id, false);
        }
        public void cause_damage(int battler_1_id, int battler_2_id, bool test)
        {
            if (Attacker == 1)
                cause_damage(Global.game_map.attackable_map_object(battler_1_id), Global.game_map.attackable_map_object(battler_2_id), test);
            else
                cause_damage(Global.game_map.attackable_map_object(battler_2_id), Global.game_map.attackable_map_object(battler_1_id), test);
        }
        private void cause_damage(Combat_Map_Object battler_1, Combat_Map_Object battler_2, bool test)
        {
            // If the attack didn't backfire, cause damage as normal then apply on-hit healing here
            if (!Result.backfire)
            {
                battler_2.combat_damage(Result.dmg, battler_1, Result.state_change, Result.backfire, test);
                battler_1.hp += Result.immediate_life_steal;
                //if (battler_1.is_unit()) //Debug
                //    ((Game_Unit)battler_1).actor.hp += Result.immediate_life_steal;
                //else
                //    ((Destroyable_Object)battler_1).hp += Result.immediate_life_steal;
            }
            // Else it backfired, and damage is caused
            else
                battler_1.combat_damage(Result.dmg - Result.immediate_life_steal, battler_2, Result.state_change, Result.backfire, test);
            // Then apply is delayed life gain, such as from Resire or Nosferatu
            if (Result.delayed_life_steal && !battler_1.is_dead)
            {
                battler_1.hp += Result.delayed_life_steal_amount;
                //if (battler_1.is_unit())
                //    (battler_1 as Game_Unit).actor.hp += Result.delayed_life_steal_amount;
                //else
                //    (battler_1 as Destroyable_Object).hp += Result.delayed_life_steal_amount;
            }
        }

        public void cause_status(int battler_1_id, int battler_2_id)
        {
            if (Attacker == 1)
                cause_status(Global.game_map.units[battler_1_id], Global.game_map.units[battler_2_id]);
            else
                cause_status(Global.game_map.units[battler_2_id], Global.game_map.units[battler_1_id]);
        }
        private void cause_status(Game_Unit battler_1, Game_Unit battler_2)
        {
            if (!Result.backfire)
            {
                battler_2.state_change(Result.state_change);
            }
            else
                battler_1.state_change(Result.state_change);
        }

        public Game_Unit hit_unit(int battler_1_id, int battler_2_id)
        {
            if (Attacker == 1)
                return hit_unit(Global.game_map.units[battler_1_id], Global.game_map.units[battler_2_id]);
            else
                return hit_unit(Global.game_map.units[battler_2_id], Global.game_map.units[battler_1_id]);
        }
        private Game_Unit hit_unit(Game_Unit battler_1, Game_Unit battler_2)
        {
            if (!Result.backfire)
                return battler_2;
            else
                return battler_1;
        }
    }
}
