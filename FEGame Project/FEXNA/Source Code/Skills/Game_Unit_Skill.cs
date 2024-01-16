using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using FEXNA_Library;
using ArrayExtension;
using FEXNAStringExtension;
using FEXNAWeaponExtension;
using Vector2Extension;

namespace FEXNA
{
    partial class Game_Unit
    {
        readonly static List<string> ACTIVATION_SKILLS = new List<string> { "DETER", "ADEPT", "FRENZY", "CANCEL", "LETHAL", "BASTION", "THAUM", "ASTRAAA" };
        readonly static List<string> ANY_ATTACK_TYPE_ACTIVATION = new List<string> { "DETER", "CANCEL", "BASTION", "THAUM" };
        public readonly static List<string> MASTERIES = new List<string> { "ASTRA", "LUNA", "SOL", "SPRLDVE", "NOVA", "FLARE", "ZITI", "WOLFRAM" };
        public readonly static List<string> HEALING_MASTERIES = new List<string> { "SOL" };
        const int MASTERY_MAX_CHARGE = 100;
        const float MASTERY_RATE_NEW_TURN = 0f;
        private const int MAG_EFFECT = 20;
        public const float MASTERY_RATE_BATTLE_END = 1f;

        #region Serialization
        public void skills_write(BinaryWriter writer)
        {
            writer.Write(Swoop_Activated);
            writer.Write(Trample_Activated);
            Trample_Loc.write(writer);
            Called_Masteries.write(writer);
            Mastery_Gauges.write(writer);
        }

        public void skills_read(BinaryReader reader)
        {
            Swoop_Activated = reader.ReadBoolean();
            Trample_Activated = reader.ReadBoolean();
            Trample_Loc = Trample_Loc.read(reader);
            Called_Masteries = Called_Masteries.read(reader);
            Mastery_Gauges = Mastery_Gauges.read(reader);
        }
        #endregion

        #region Accessors
        public bool skill_activated
        {
            get { return actor.skill_activated; }
            set { actor.skill_activated = value; }
        }

        public bool skip_skill_effect
        {
            get
            {
                if (actor.astra_activated && actor.astra_count != Game_Actor.ASTRA_HITS - 1) return true;
                return false;
            }
        }

        private void end_battle_skills()
        {
            // Overwritten by Vendetta and Swoop //Yeti
            // Skills: Swoop
            Swoop_Activated = false;
            Swoop_Attacked = false;
            // Skills: Trample
            Trample_Activated = false;
            // Skills: Masteries
            for (int i = 0; i < MASTERIES.Count; i++)
            {
                if (mastery_called(MASTERIES[i]))
                    Mastery_Gauges[i] = 0;
            }
            reset_masteries();
        }
        #endregion


        private int process_number(string str)
        {
            return process_number(str, false);
        }

        private int process_number(string str, bool allow_unit)
        {
            if (str.substring(0, 8) == "PlayerId")
                return Constants.Team.PLAYER_TEAM;
            if (str.substring(0, 7) == "EnemyId")
                return Constants.Team.ENEMY_TEAM;
            if (str.substring(0, 9) == "CitizenId")
                return Constants.Team.CITIZEN_TEAM;
            if (str.substring(0, 10) == "IntruderId")
                return Constants.Team.INTRUDER_TEAM;
            if (str.substring(0, 22) == "First_Saved_Deployment")
            {
                return Global.battalion.deployed[0];
            }
            if (str.substring(0, 20) == "Last_Battalion_Actor")
            {
                return Global.battalion.actors[Global.battalion.actors.Count - 1];
            }
            if (str.substring(0, 13) == "Visitor_Actor")
                return Global.game_state.event_caller_unit.actor.id;

            var weapon_type = Global.weapon_types.FirstOrDefault(x => x.EventName == str);
            if (weapon_type != null)
                return Global.weapon_types.IndexOf(weapon_type);
            //if (str == "None") //Debug
            //    return (int)FEXNA_Library.Weapon_Types.None;
            //else if (FEXNA_Library.Data_Weapon.WEAPON_TYPE_NAMES.Contains(str))
            //    return FEXNA_Library.Data_Weapon.WEAPON_TYPE_NAMES.ToList().IndexOf(str);

            /*if (allow_unit)
                return process_unit_id(str);
            else*/
            return Convert.ToInt32(str);
        }

        #region Skill Setup
        public void prehit_def_skill_check()
        {
            prehit_def_skill_check(null);
        }
        public void prehit_def_skill_check(Game_Unit target)
        {
            bastion_prehit_def_skill_check(target);
        }

        public void prehit_skill_check()
        {
            prehit_skill_check(null);
        }
        public void prehit_skill_check(Game_Unit target)
        {
            prehit_skill_check(target, null);
        }
        public void prehit_skill_check(Game_Unit target, int? distance)
        {
            astra_hit_skill_check(target, distance);
            astraaa_prehit_skill_check(target, distance);
            //sprldve_prehit_skill_check(target, distance);
        }

        public void hit_skill_check(bool is_hit, bool is_crt)
        {
            hit_skill_check(is_hit, is_crt, null);
        }
        public void hit_skill_check(bool is_hit, bool is_crt, Game_Unit target)
        {
            hit_skill_check(is_hit, is_crt, target, null);
        }
        public void hit_skill_check(bool is_hit, bool is_crt, Game_Unit target, int? distance)
        {
            //lethal_hit_skill_check(is_hit, is_crt, target, distance);
            //cancel_hit_skill_check(is_hit, is_crt, target, distance);

            //astra_hit_skill_check(is_hit, is_crt, target, distance);
            //lethal_hit_skill_check(is_hit, is_crt, target, distance); //Yeti
            //luna_hit_skill_check(is_hit, is_crt, target, distance);
            //pierce_hit_skill_check(is_hit, is_crt, target, distance); //Yeti
            //sol_hit_skill_check(is_hit, is_crt, target, distance);
            //thaum_hit_skill_check(is_hit, is_crt, target, distance); //Yeti
            //nova_hit_skill_check(is_hit, is_crt, target, distance); //Yeti
            //flare_hit_skill_check(is_hit, is_crt, target, distance); //Yeti
            //cancel_hit_skill_check(is_hit, is_crt, target, distance); //Yeti
        }

        public void onhit_skill_check(bool is_hit)
        {
            onhit_skill_check(is_hit, null);
        }
        public void onhit_skill_check(bool is_hit, Game_Unit target)
        {
            onhit_skill_check(is_hit, target, null);
        }
        public void onhit_skill_check(bool is_hit, Game_Unit target, int? distance)
        {
            deter_hit_skill_check(is_hit, target, distance);
            
        }

        public void posthit_skill_check()
        {
            posthit_skill_check(null);
        }
        public void posthit_skill_check(Game_Unit target)
        {
            adept_posthit_skill_check(target);
            //i don't think you need any astraaa posthit stuff?
            frenzy_posthit_skill_check(target);
            //vandynerf_posthit_skill_check(target);
        }

        public void hit_skill_update()
        {
            astra_hit_skill_update();
            adept_hit_skill_update();
            astraaa_hit_skill_update();
            frenzy_hit_skill_update();
        }

        public List<string> hit_skill_ids()
        {
            List<string> result = new List<string>();
            if (actor.astra_activated) result.Add("ASTRA");
            if (actor.luna_activated) result.Add("LUNA");
            if (actor.ziti_activated) result.Add("ZITI");
            if (actor.sol_activated) result.Add("SOL");
            if (actor.bastion_activated) result.Add("BASTION");
            if (actor.sprldve_activated) result.Add("SPRLDVE");
            if (actor.astraaaActivated) result.Add("ASTRAAA");
            return result;
        }

        public List<string> onhit_skill_ids()
        {
            List<string> result = new List<string>();
            if (actor.deter_activated) result.Add("DETER");
            return result;
        }

        public List<string> posthit_skill_ids()
        {
            List<string> result = new List<string>();
            if (actor.adeptActivated)
                result.Add("ADEPT");
            if (actor.frenzy_activated)
                result.Add("FRENZY");
            return result;
        }

        public void reset_skills()
        {
            actor.reset_skills();
        }

        public bool ignore_hud()
        {
            if (astra_skip_skill_update()) return true;
            return false;
        }

        public bool skip_skill_update()
        {
            if (astra_skip_skill_update())
                return true;
            if (adept_skip_skill_update())
                return true;
            if (frenzy_skip_skill_update())
                return true;
            return false;
        }

        public int? shown_skill_rate(Game_Unit target)
        {
            if (target != null ? !nihil(target) : true)
                foreach (string skill in ACTIVATION_SKILLS)
                    if (actor.has_skill(skill))
                    {
                        if (!is_correct_attack_type() && !ANY_ATTACK_TYPE_ACTIVATION.Contains(skill))
                            continue;
                        //if (skill == "SPRLDVE" && target != null && target.actor.move_type == (int)MovementTypes.Flying)
                        //    continue;
                        if (MASTERIES.Contains(skill))
                            return Mastery_Gauges[MASTERIES.IndexOf(skill)];
                        else
                            return skill_rate(skill);
                    }
            return null;
        }

        private int skill_rate(string skill)
        {
            switch (skill)
            {
                // Masteries that charge
                case "ASTRA":
                    return stat(Stat_Labels.Spd);
                case "LUNA":
                    return stat(Stat_Labels.Skl);
                case "ZITI":
                    return 10;
                case "WOLFRAM":
                    return 0;
                case "SOL":
                    return stat(Stat_Labels.Lck) * 2;
                case "SPRLDVE":
                    return 35;
                    //return (stat(Stat_Labels.Skl) + stat(Stat_Labels.Spd));
                // In battle activation skills
                case "BASTION":
                    return 50;
                case "DETER":
                    return stat(Stat_Labels.Skl) / 2;
                case "ADEPT":
                    return stat(Stat_Labels.Spd) / 2 + actor.weapon.Crt + (actor.has_skill("MULCIBER") ? 15 : 0);
                case "FRENZY":
                    return stat(Stat_Labels.Pow) / 2;
                case "ASTRAAA":
                    return stat(Stat_Labels.Spd) / 2;
            }
            return 0;
        }

        public int? skill_animation_val()
        {
            // Skills: Astra
            if (actor.astra_activated && (actor.astra_count == Game_Actor.ASTRA_HITS - 1 || actor.astra_missed))
                return Global.skill_from_abstract("ASTRA").Animation_Id;
            // Skills: Luna
            if (actor.luna_activated)
                return Global.skill_from_abstract("LUNA").Animation_Id;
            if (actor.ziti_activated)
                return Global.skill_from_abstract("ZITI").Animation_Id;
            // Skills: Sol
            if (actor.sol_activated)
                return Global.skill_from_abstract("SOL").Animation_Id;
            // Skills: Bastion
            if (actor.bastion_activated)
                return Global.skill_from_abstract("BASTION").Animation_Id;
            // Skills: Spiral Dive
            if (actor.sprldve_activated)
                return Global.skill_from_abstract("LUNA").Animation_Id;
                //return Global.skill_from_abstract("SPRLDVE").Animation_Id;
            // Skills: Determination
            if (actor.deter_activated)
                return Global.skill_from_abstract("DETER").Animation_Id;
            // Skills: Adept
            if (actor.adeptActivated && actor.class_id != 91)
                return Global.skill_from_abstract("ADEPT").Animation_Id;
            if (actor.astraaaActivated)
                return Global.skill_from_abstract("ASTRAAA").Animation_Id;
            // Skills: Frenzy
            if (actor.frenzy_activated)
                return Global.skill_from_abstract("FRENZY").Animation_Id;
            return null;
        }

        public void skill_map_effect()
        {
            int? id = skill_map_effect_id();
            if (id != null && Global.scene.is_strict_map_scene)
            {
                ((Scene_Map)Global.scene).set_map_effect(Loc, 2, (int)id);
            }
        }

        private int? skill_map_effect_id()
        {
            // Skills: Astra
            if (actor.astra_activated && (actor.astra_count == Game_Actor.ASTRA_HITS - 1 || actor.astra_missed))
                return Global.skill_from_abstract("ASTRA").Map_Anim_Id;
            // Skills: Luna
            if (actor.luna_activated)
                return Global.skill_from_abstract("LUNA").Map_Anim_Id;
            if (actor.ziti_activated)
                return Global.skill_from_abstract("ZITI").Map_Anim_Id;
            // Skills: Sol
            if (actor.sol_activated)
                return Global.skill_from_abstract("SOL").Map_Anim_Id;
            // Skills: Bastion
            if (actor.bastion_activated)
                return Global.skill_from_abstract("BASTION").Map_Anim_Id;
            // Skills: Spiral Dive
            if (actor.sprldve_activated)
                return Global.skill_from_abstract("LUNA").Map_Anim_Id;
            // Skills: Determination
            if (actor.deter_activated)
                return Global.skill_from_abstract("DETER").Map_Anim_Id;
            // Skills: Adept
            if (actor.adeptActivated)
                return Global.skill_from_abstract("ADEPT").Map_Anim_Id;
            if (actor.astraaaActivated)
                return Global.skill_from_abstract("ASTRAAA").Map_Anim_Id;
            // Skills: Frenzy
            if (actor.frenzy_activated)
                return Global.skill_from_abstract("FRENZY").Map_Anim_Id;
            return null;
        }
        #endregion

        #region Battle Stat Affecting
        internal Maybe<int> atk_pow_skill(Data_Weapon weapon, bool magic)
        {
            // Skills: Knife
            if (actor.has_skill("KNIFE"))
                if (!weapon.is_magic() && !magic && weapon.main_type().Name == "Sword")
                {
                    int base_pow = stat(Stat_Labels.Pow) - stat_bonus(Stat_Labels.Pow);
                    int base_skl = stat(Stat_Labels.Skl) - stat_bonus(Stat_Labels.Skl);
                    return Math.Min(actor.get_cap(Stat_Labels.Pow), (base_pow + base_skl) / 2) + stat_bonus(Stat_Labels.Pow);
                }
            // Skills: Crossbow
            if (actor.has_skill("CROSSBOW"))
                if (!magic && weapon.main_type().Name == "Bow" && !weapon.Ballista())
                    return 0;
            return new Maybe<int>();
        }

        internal Maybe<int> atk_spd_skill(int spd, Data_Weapon weapon, bool magic)
        {
            // Skills: Crossbow
            if (!magic && weapon.main_type().Name == "Bow" && !weapon.Ballista() && actor.has_skill("CROSSBOW"))
                return (stat(Stat_Labels.Pow) + spd) / 2;

            if (actor.has_skill("WILTED"))
                return (stat(Stat_Labels.Spd) - 4);

            return new Maybe<int>();
        }

        internal void dmg_skill(
            ref int skill_dmg, ref int weapon_dmg, ref int actor_dmg, ref int support_dmg,
            Data_Weapon weapon, bool magic, int? distance)
        {
            int target_def = 0;
            dmg_skill(ref skill_dmg, ref weapon_dmg, ref actor_dmg, ref support_dmg,
                ref target_def, weapon, null, null, WeaponTriangle.Nothing,
                magic, distance);
        }
        internal void dmg_skill(
            ref int skill_dmg, ref int weapon_dmg, ref int actor_dmg, ref int support_dmg,
            ref int target_def, Data_Weapon weapon,
            Game_Unit target, Data_Weapon target_weapon, WeaponTriangle tri, bool magic,
            int? distance, float effectiveness = 1)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return;

            // Skills: Knife
            if (actor.has_skill("KNIFE"))
                if (!weapon.is_magic() && !magic &&
                    weapon.main_type().Name == "Sword")
                {
                    //skill_dmg = (weapon_dmg + skill_dmg + 1) / 2;
                    weapon_dmg -= 3;
                    skill_dmg -= (int)(3 * (effectiveness - 1));
                    //skill_dmg -= weapon_dmg;
                }
            if (actor.has_skill("OLD_KNIFE"))
                if (!magic && weapon.main_type().Name == "Sword")
                {
                    skill_dmg = (weapon_dmg + skill_dmg + 1) / 2;
                    weapon_dmg = (weapon_dmg + 1) / 2;
                    skill_dmg -= weapon_dmg;
                }
            // Skills: Crossbow
            if (actor.has_skill("CROSSBOW")) // maybe move this to atk_pow //Debug
                if (!magic && weapon.main_type().Name == "Bow" && !weapon.Ballista())
                    //actor_dmg += (int)(weapon.Mgt * (1 + actor.tier / 2.0f)); //Debug
                    actor_dmg += (int)(weapon_dmg * (1 + actor.tier / 2.0f));
            // Skills: Siege Mastery
            if (actor.has_skill("SIEGEMST"))
                if (!magic && weapon.main_type().Name == "Bow" && weapon.Ballista())
                {
                    weapon_dmg += 5;
                    skill_dmg -= (int)(5 * (effectiveness - 1));
                }
            // Skills: Not Tomebreaker
            if (target != null && !nihil(target)) // This and all the others don't have to check if target is null, because nihil() already does? //Yeti
                if (tri != WeaponTriangle.Disadvantage)
                    if (actor.has_skill("TOMEBREAK") && target_weapon != null)
                    {
                        if (!weapon.is_staff() && weapon.is_always_magic() &&
                                weapon.main_type().IsMagic && target_weapon.main_type().IsMagic)
                            skill_dmg += 5;
                    }
            // Skills: Colossus
            if (target != null && !nihil(target))
                if (actor.has_skill("COLOSSUS"))
                    actor_dmg += (int)MathHelper.Clamp(stat(Stat_Labels.Con) - target.stat(Stat_Labels.Con), 0, 5);
            // Skills: Commando
            if (target != null && !nihil(target))
                if (actor.has_skill("CMNDO"))
                    skill_dmg += terrain_def_bonus();
            // Skills: Mulciber's Steel
            if (actor.has_skill("MULCIBER"))
                actor_dmg += 5;
            // Activation skills
            // Skills: Bastion
            if (target != null)
                if (target.actor.bastion_activated)
                    target_def += Math.Max(0, (skill_dmg + weapon_dmg + actor_dmg + support_dmg) - target_def);
            // Skills: Luna
            if (target != null && !nihil(target))
                if (actor.luna_activated || actor.has_skill("SURE"))
                {
                    weapon_dmg *= 2;
                    skill_dmg += target.stat(Stat_Labels.Res) / 2;
                }
            if(!nihil(target))
                if (actor.ziti_activated)
                {
                    weapon_dmg += weapon.Mgt;
                }
            // Skills: Spiral Dive
            if (!nihil(target))
                if (actor.sprldve_activated)
                {
                    skill_dmg += stat(Stat_Labels.Spd);
                }
            if (actor.has_skill("BFH"))
                actor_dmg += stat(Stat_Labels.Def);
            if (actor.has_skill("INOX"))
                skill_dmg += stat(Stat_Labels.Spd) / 2;
            if(actor.has_skill("DEFIANCE"))
                actor_dmg += (actor.maxhp - actor.hp) / 2;
            if(actor.has_skill("HIATUSING"))
            {
                actor_dmg = 0;
                skill_dmg = 0;
                weapon_dmg = 0;
            }
        }

        internal void dmg_staff_skill(ref int n, ref int weapon_dmg, ref int actor_dmg, Game_Unit target, bool magic, int? distance)
        {
            // Skills: Faith
            if (actor.has_skill("FAITH"))
                n += 5 * actor.tier;
        }

        public int base_hit_skl(Data_Weapon weapon, bool magic)
        {
            // Skills: Crossbow
            if (!magic && weapon.main_type().Name == "Bow" && !weapon.Ballista() && actor.has_skill("OLD_CROSSBOW"))
                return stat(Stat_Labels.Pow) + stat(Stat_Labels.Skl);
            return stat(Stat_Labels.Skl) * 2;
        }

        internal void hit_skill(ref int n, ref int weapon_hit, ref int actor_hit,
            Data_Weapon weapon, Game_Unit target, bool magic, int? distance)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return;
            // Skills: Hit +X
            foreach (int skill_id in actor.all_skills)
            {
                double str_test;
                string name = Global.data_skills[skill_id].Abstract;
                if (name.substring(0, 3) == "HIT" && double.TryParse(name.substring(3, name.Length - 3), out str_test))
                    n += Convert.ToInt32(name.Substring(3, name.Length - 3));
            }
            // Skills: Knife
            if (actor.has_skill("KNIFE"))
                if (!weapon.is_magic() && !magic &&
                        weapon.main_type().Name == "Sword")
                    weapon_hit += 10;
            // Skills: Rage
            if (!nihil(target))
                if (actor.has_skill("RAGE"))
                {
                    var stats = new BattlerStats(Id);
                    int crt = stats.base_crt() + class_crt_bonus;
                    actor_hit += crt;
                }
            // Skills: Kamaitachi
            if (actor.has_skill("KAMAI"))
            {
                if (!magic && weapon.Max_Range == 1 && distance >= 2)
                    weapon_hit -= 20;
                else if (weapon.Thrown() && distance >= 2)
                    weapon_hit += 5;
            }
            // Skills: Entropic Shield
            if (target != null)
                // This reduces the attacker's hit, not the defender's avoid, so the one with the skill is the target
                if (target.actor.has_skill("ENTROP"))
                    if (!magic)
                        if (!target.nihil(this))
                            n -= 25;
            // Skills: Cyclone
            if (target != null)
                // This reduces the attacker's hit, not the defender's avoid, so the one with the skill is the target
                if (distance >= 2)
                    if (target.actor.has_skill("CYCLONE"))
                        if (!target.nihil(this))
                            n -= 25;
            // Skills: Commando
            if (target != null && !nihil(target))
                if (actor.has_skill("CMNDO"))
                    n += terrain_avo_bonus();
            // Skills: Deus's Guidance
            if (actor.has_skill("SET"))
                actor_hit += 30;
            // Skills: Prestige
            if (Global.scene.is_map_scene && !nihil(target) && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(3))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("PSTG") && !is_attackable_team(unit))
                    {
                        n += 10;
                        break;
                    }
                }
            // Skills: Dreaded
            if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(3))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("DREAD") && is_attackable_team(unit))
                        if (!unit.nihil(this))
                        {
                            n -= 10;
                            break;
                        }
                }
            if (actor.has_skill("HALLUC"))
                weapon_hit /= 2;
            if (Global.scene.is_map_scene && !nihil(target) && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(3))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("CHARI") && !is_attackable_team(unit))
                    {
                        n += 10;
                        break;
                    }
                }
            if (target != null && (target.actor.has_skill("JUGGER") || actor.has_skill("SPEEDMETAL") || target.actor.has_skill("SPEEDMETAL")))
                n += 255;
            if (actor.has_skill("CONFUSED"))
                n -= 500;
            if (actor.has_skill("SURE"))
                n += 255;
            if (actor.has_skill("HIATUSING"))
                n = -500;
        }

        internal void crt_skill(ref int n, ref int weapon_crt, ref int actor_crt,
            Data_Weapon weapon, Game_Unit target, bool magic, int? distance)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return;
            // Skills: Critical +X
            actor_crt += class_crt_bonus;
            // Skills: Prestige
            if (Global.scene.is_map_scene && !nihil(target) && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(3))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("PSTG") && !is_attackable_team(unit))
                    {
                        n += 10;
                        break;
                    }
                }
            // Skills: Dreaded
            if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(3))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("DREAD") && is_attackable_team(unit))
                        if (!unit.nihil(this))
                        {
                            n -= 10;
                            break;
                        }
                }
            // Activation Skills
            if(!nihil(target))
                if (actor.ziti_activated)
                {
                    n += 255;
                }
            if(actor.has_skill("VANDY2"))
            {
                n += (int)(actor.vandy_stacks * 2.5f);
            }
            if (actor.has_skill("MULCIBER"))
                n += 15;
            if (actor.has_skill("ADEPT") && !actor.has_skill("FATHEROFBOOBS"))
                n -= 255;
            if (actor.has_skill("FATHEROFBOOBS2") && actor.hp < actor.maxhp / 2)
                n += 510;//overcome adept nerf and then add 255 on that lol
        }

        internal void avo_skill(ref int n, ref int actor_avo, ref int terr_avo,
            Data_Weapon weapon, Game_Unit target, WeaponTriangle tri)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return;
            // Skills: Avo +X
            foreach (int skill_id in actor.all_skills)
            {
                double str_test;
                string name = Global.data_skills[skill_id].Abstract;
                if (name.substring(0, 3) == "AVO" &&
                        double.TryParse(name.substring(3, name.Length - 3), out str_test))
                    n += Convert.ToInt32(name.Substring(3, name.Length - 3));
            }
            // Skills: Evasiveness
            if (actor.has_skill("EVASIVE"))
                if (tri != WeaponTriangle.Disadvantage)
                    actor_avo += 10;
            // Skills: Shield Mastery
            if (!nihil(target))
                if (actor.has_skill("SHIELD"))
                    if (weapon != null)
                    {
                        WeaponType type = weapon.main_type();
                        actor_avo += actor.get_weapon_level(type) * 5;
                    }
            // Skills: Rage
            if (!nihil(target))
                if (actor.has_skill("RAGE"))
                {
                    var stats = new BattlerStats(Id);
                    int crt = stats.base_crt() + class_crt_bonus;
                    actor_avo += crt / 2;
                }
            // Skills: Slow
            if (actor.has_skill("SLOW"))
                actor_avo -= 10;
            // Skills: Set's Litany
            if (actor.has_skill("SET"))
                actor_avo += 30;
            if (actor.has_skill("VANDY2"))
                actor_avo += 20;
            if (actor.has_skill("TENA"))
            {
                if (actor.hp <= actor.maxhp / 2)
                    actor_avo += actor.maxhp - actor.hp;
            }
            if (target != null && actor.has_skill("MIRACLE") && 10 >= actor.hp && Global.game_system.roll_rng(stat(Stat_Labels.Skl) * 3))
                actor_avo = 9999;
            if (Global.scene.is_map_scene && !nihil(target) && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(3))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("CHARI") && !is_attackable_team(unit))
                    {
                        n += 10;
                        break;
                    }
                }
            if (actor.has_skill("HIDDEN") && !Global.game_state.is_player_turn)
                actor_avo = 9999;
        }

        internal void base_avo_skill(ref int spd, ref int lck)
        {
            // Skills: Sleep
            if (actor.has_skill("SLEEP"))
            {
                spd = 0;
            }
        }

        public int dodge_skill()
        {
            int n = 0;
            // Skills: Dodge +X
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 5) == "DODGE"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(5, name.Length - 5), out str_test))
                        n += Convert.ToInt32(name.Substring(5, name.Length - 5));
                }
            // Skills: Set's Litany
            if (actor.has_skill("MOT"))
                n += 15;
            return n;
        }

        private int class_crt_bonus
        {
            get
            {
                int n = 0;
                // Skills: Crit +X
                foreach (int skill_id in actor.all_skills)
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (name.substring(0, 4) == "CRIT" && double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                return n;
            }
        }

        internal int dmg_target_skill(Game_Unit target, Data_Weapon weapon, int? distance, int dmg)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return dmg;
            if (!weapon.is_staff())
            {
                // Activation Skills
                // Skills: Astra
                if (actor.astra_activated && actor.astra_count > 0)
                {
                    dmg = Math.Max(dmg, 1);
                }
                // Skills: Determination
                bool magic_attack = weapon == null ? false : check_magic_attack(weapon, (int)distance);
                if (target.actor.deter_activated)
                    target.actor.deter_counter = !magic_attack && distance == 1;
                if (target != null && target.actor.has_skill("JUGGER"))
                {
                    dmg /= 2;
                }
                if (target != null && target.actor.has_skill("DRAGONSKIN"))
                {
                    dmg /= 4;
                }
                if (target != null && actor.has_skill("RIGHTMARC") && dmg < 20)
                    dmg = 20;
                if (target != null && target.actor.has_skill("GUTS") && target.actor.hp > 1 && dmg >= target.actor.hp)
                    dmg = target.actor.hp - 1;
                if (target != null && target.actor.has_skill("SHIELDING") && dmg >= target.actor.hp)
                    dmg = target.actor.hp - 1;
                if (target != null && target.actor.has_skill("HIATUSING") || actor.has_skill("HIATUSING"))
                    dmg = 0;
                if (actor.astraaaActivated)
                    dmg = (int)(dmg * 4.5f);
            }
            return dmg;
        }

        internal int hit_target_skill(Game_Unit target, Data_Weapon weapon, int? distance, int hit)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return hit;
            if (!weapon.is_staff())
            {
                // Skills: Astra
                if (actor.astra_activated && (actor.astra_count != -1 && actor.astra_count < Game_Actor.ASTRA_HITS - 1) && !actor.astra_missed)
                    return Math.Max(100, hit);

                if(actor.has_skill("RELIABLE") || (target != null && target.actor.has_skill("RELIABLE"))){
                    if(hit > 80)
                        hit = 100;
                    else if(hit < 20)
                        hit = 0;
                }
            }
            return hit;
        }

        internal int crt_target_skill(Game_Unit target, Data_Weapon weapon, int? distance, int crt)
        {
            if (target != null && target.actor.has_skill("CONFUSION"))
                return crt;
            if (!weapon.is_staff())
            {
                // Skills: Trample
                if (Trample_Activated)
                    return 0;
                // Skills: Fortune
                if (target.actor.has_skill("FORT"))
                    if (!target.nihil(this))
                        return 0;
                // Skills: Aegis
                if (target.actor.has_skill("AEGIS"))
                    if (!target.nihil(this))
                        crt /= 2;
                // Skills: Frenzy
                if (actor.frenzy_activated)
                    crt *= 2;

                if (actor.has_skill("RELIABLE") || (target != null && target.actor.has_skill("RELIABLE")))
                {
                    if (crt > 40)
                        crt = 100;
                    else if (crt < 10)
                        crt = 0;
                }
            }
            return crt;
        }

        // Deals with added effects for skills, largely life steal
        public void skill_effects(ref int dmg, Game_Unit target, ref Attack_Result result)
        {
            // this seems to be called twice while setting up battles, making permanent changes here doubled
            // for example, reducing the enemy stats
            // look into it? //Yeti

            // Activation skills
            // Skills: Nosferatu
            if (!nihil(target))
                if (actor.has_skill("NOSF"))
                    result.immediate_life_steal += dmg / 2;
            if (actor.has_skill("FIZZB") && dmg >= target.hp)
                result.immediate_life_steal += actor.maxhp / 2;
            // Activation skills
            // Skills: Sol
            if (actor.sol_activated)
                result.immediate_life_steal += dmg;
            // Skills: Determination
            // Determination needs to not activate if the attack is already backfiring //Debug
            // Also decide how this interacts with silencer
            // Also have to redo the code that calls this around checking for backfire before and after this method, lol
            // Probably just add a new 'riposte' property and use that instead of backfire
            if (target.actor.deter_activated)
            {
                if (target.actor.deter_counter)
                {
                    result.backfire = true;
                    dmg /= 2;
                }
                else
                    dmg = 0;
            }
        }

        private bool is_double_disabled()
        {
            // Skills: Slow
            if (actor.has_skill("SLOW"))
                return true;
            return is_doubling_blocked();
        }

        public bool is_doubling_blocked()
        {
            // Skills: Swoop
            if (Swoop_Activated)
                return true;
            // Skills: Trample
            if (Trample_Activated)
                return true;
            return false;
        }

        public bool is_brave_blocked()
        {
            // Skills: Trample
            if (Trample_Activated)
                return true;
            return false;
        }

        public bool no_counter_skill()
        {
            // Skills: Trample
            if (Trample_Activated)
                return true;
            return false;
        }

        public int weapon_triangle_mult_skill(Game_Unit target, Data_Weapon weapon, Data_Weapon target_weapon, int distance)
        {
            int n = -1;
            // Skills: Smite
            if (target != null && !nihil(target))
                if (actor.has_skill("SMITE") && target_weapon != null && !weapon.is_staff() &&
                        (target_weapon.main_type().Name == "Dark" || target_weapon.scnd_type().Name == "Dark"))
                    n = 2;
            return n;
        }
        #endregion

        #region Nihil
        /// <summary>
        /// Tests if the target cancels this unit's skills with Nihil.
        /// Returns true if the target has Nihil and this unit doesn't.
        /// Also always returns true in the arena.
        /// </summary>
        /// <param name="target">Opposing unit.</param>
        public bool nihil(Game_Unit target)
        {
            if (target == null)
                return false;
            if (Global.game_system.In_Arena && !Global.scene.is_test_battle)
                return true;
            return (target.actor.has_skill("NIHIL") && !actor.has_skill("NIHIL"));
        }
        #endregion

        #region Masteries
        private bool[] Called_Masteries = new bool[MASTERIES.Count];
        private int[] Mastery_Gauges = new int[MASTERIES.Count];

        public bool[] called_masteries { get { return Called_Masteries; } }

        public void call_mastery(string skill)
        {
            if (!MASTERIES.Contains(skill))
                return;
            Called_Masteries[MASTERIES.IndexOf(skill)] = true;
        }

        public void activate_masteries()
        {
            foreach(string skill in MASTERIES)
                if (mastery_called(skill))
                    switch (skill)
                    {
                        case "ASTRA":
                            actor.activate_astra();
                            break;
                        case "LUNA":
                            actor.activate_luna();
                            break;
                        case "ZITI":
                            actor.activate_ziti();
                            break;
                        case "SOL":
                            actor.activate_sol();
                            break;
                        case "SPRLDVE":
                            actor.activate_sprldve();
                            break;
                        case "ASTRAAA":
                            actor.activate_astraaa();
                            break;
                        case "NOVA":
                            break;
                        case "FLARE":
                            break;
                    }
        }

        public void mastery_hit_confirm(bool hit)
        {
            // Skills: Astra
            if (actor.astra_activated)
                actor.astra_hit_confirm(hit);
        }

        private bool mastery_called(string skill)
        {
            if (!MASTERIES.Contains(skill))
                return false;
            return Called_Masteries[MASTERIES.IndexOf(skill)];
        }

        public void charge_masteries(float mult)
        {
            for(int i = 0; i < MASTERIES.Count; i++)
                if (actor.has_skill(MASTERIES[i]))
                    Mastery_Gauges[i] = Math.Min(MASTERY_MAX_CHARGE, Mastery_Gauges[i] + (int)(skill_rate(MASTERIES[i]) * mult));
        }
        public void charge_masteries_set(int amt)
        {
            for (int i = 0; i < MASTERIES.Count; i++)
                if (actor.has_skill(MASTERIES[i]))
                    Mastery_Gauges[i] = Math.Min(MASTERY_MAX_CHARGE, Mastery_Gauges[i] + amt);
        }

        public IEnumerable<string> ready_masteries()
        {
            foreach (string mastery in MASTERIES)
                if (is_mastery_ready(mastery))
                    yield return mastery;
        }

        public bool is_mastery_ready(string skill)
        {
            if (!MASTERIES.Contains(skill))
                return false;
            return Mastery_Gauges[MASTERIES.IndexOf(skill)] >= MASTERY_MAX_CHARGE;
        }

        public float mastery_charge_percent(string skill)
        {
            if (!MASTERIES.Contains(skill))
                return 0;
            return Math.Min(1, Mastery_Gauges[MASTERIES.IndexOf(skill)] / (float)MASTERY_MAX_CHARGE);
        }

        public void reset_masteries()
        {
            for (int i = 0; i < MASTERIES.Count; i++)
            {
                Called_Masteries[i] = false;
            }
        }

        public void uncharge_masteries(string skill)
        {
            if (!MASTERIES.Contains(skill))
                return;
            Mastery_Gauges[MASTERIES.IndexOf(skill)] = 0;
            return;
        }

        public string has_any_mastery()
        {
            foreach (string skill in MASTERIES)
                if (actor.has_skill(skill))
                    return skill;
            return null;
        }
        public float any_mastery_charge_percent()
        {
            foreach (string skill in MASTERIES)
                if (actor.has_skill(skill))
                    return mastery_charge_percent(skill);
            return 0;
        }
        public float highest_mastery_charge_percent()
        {
            float result = 0;
            foreach (string skill in MASTERIES)
                if (actor.has_skill(skill))
                    result = Math.Max(result, mastery_charge_percent(skill));
            return result;
        }

        public bool valid_mastery_target(string skill, Game_Unit target, int? distance)
        {
            return valid_mastery_target(skill, target, distance, actor.weapon_id);
        }
        public bool valid_mastery_target(string skill, Game_Unit target, int? distance, int weapon_id)
        {
            if (!ACTIVATION_SKILLS.Contains(skill) && !MASTERIES.Contains(skill))
                return true;
            bool magic_attack = weapon_id <= 0 ? false : check_magic_attack(Global.data_weapons[weapon_id], (int)distance);
            // If opponent has Nihil or using the wrong attack type and this skill doesn't allow that
            if (nihil(target) || !(ANY_ATTACK_TYPE_ACTIVATION.Contains(skill) || is_correct_attack_type(magic_attack)))
                return false;
            switch (skill)
            {
                /*case "ASTRA":
                case "LUNA":
                case "SOL":
                case "THAUM":
                case "NOVA":
                case "FLARE":
                case "CANCEL":
                case "BASTION":
                case "DETER":
                    return true;*/
                //case "SPRLDVE":
                //    return distance == 1 && (target == null || (target.actor.move_type != (int)MovementTypes.Flying));
                case "ADEPT":
                case "ASTRAAA":
                case "FRENZY":
                    return target != null && !target.is_dead;
                default:
                    return true;
            }
            return false;
        }

        public static bool mastery_blocked_through_walls(string skill)
        {
            switch (skill)
            {
                case "ASTRA":
                case "LUNA":
                case "ZITI":
                case "SOL":
                case "SPRLDVE":
                case "NOVA":
                case "FLARE":
                    return true;
            }
            return true;
        }
        #endregion

        #region Activation Skills
        // Astra
        /*prev attempt
        private void astra_hit_skill_check(bool is_hit, bool is_crt, Game_Unit target, int? distance)
        {
            if (actor.has_skill("ASTRA") && is_hit && !actor.astra_activated)
            {
                if (valid_mastery_target("ASTRA", target, distance))
                {
                    int rate = skill_rate("ASTRA");
                    if (Global.game_system.roll_rng(rate) || true)
                    {
                        actor.activate_astra();
                        //actor.astra_use();
                        actor.astra_hit_confirm(true);
                    }
                }
            }
        }

        private void astra_hit_skill_update()
        {
            if (actor.astra_activated)
                actor.astra_use();
        }

        private bool astra_continue_attacking()
        {
            return actor.astra_count > 0;
        }

        private bool astra_skip_skill_update()
        {
            return actor.astra_count > 0;
        }
        */
        private void astra_hit_skill_check(Game_Unit target, int? distance)
        {
            astra_hit_skill_check(true, true, target, distance);
        }
        private void astra_hit_skill_check(bool is_hit, bool is_crt, Game_Unit target, int? distance)
        {
            if (actor.has_skill("ASTRA") && is_hit && !actor.astra_activated)
            {
                if (valid_mastery_target("ASTRA", target, distance))
                {
                    int rate = skill_rate("ASTRA");
                    if (true)//Global.game_system.roll_rng(rate))
                    {
                        actor.activate_astra();
                    }
                }
            }
        }

        private void astra_hit_skill_update()
        {
            actor.astra_use();
        }

        private bool astra_continue_attacking()
        {
            return actor.astra_count > 0;
        }

        private bool astra_skip_skill_update()
        {
            return actor.astra_count > 0;
        }
        // Luna
        private void luna_hit_skill_check(bool is_hit, bool is_crt, Game_Unit target, int? distance)
        {
            if (actor.has_skill("LUNA") && is_hit)
            {
                if (valid_mastery_target("LUNA", target, distance))
                {
                    int rate = skill_rate("LUNA");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_luna();
                    }
                }
            }
        }
        private void ziti_hit_skill_check(bool is_hit, bool is_crt, Game_Unit target, int? distance)
        {
            if (actor.has_skill("ZITI") && is_hit)
            {
                if (valid_mastery_target("ZITI", target, distance))
                {
                    int rate = skill_rate("ZITI");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_ziti();
                    }
                }
            }
        }

        // Sol
        private void sol_hit_skill_check(bool is_hit, bool is_crt, Game_Unit target, int? distance)
        {
            if (actor.has_skill("SOL") && is_hit)
            {
                if (valid_mastery_target("SOL", target, distance))
                {
                    int rate = skill_rate("SOL");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_sol();
                    }
                }
            }
        }

        // Bastion
        private void bastion_prehit_def_skill_check(Game_Unit target)
        {
            if (actor.has_skill("BASTION") && target!=null && !target.actor.has_skill("CONFUSION") && is_attackable_team(target))
            {
                if (valid_mastery_target("BASTION", target, 1))
                {
                    int rate = skill_rate("BASTION");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_bastion();
                    }
                }
            }
        }

        // Spiral Dive
        private void sprldve_prehit_skill_check(Game_Unit target, int? distance)
        {
            if (actor.has_skill("SPRLDVE"))
            {
                if (valid_mastery_target("SPRLDVE", target, distance))
                {
                    int rate = skill_rate("SPRLDVE");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_sprldve();
                    }
                }
            }
        }

        // Determination
        private void deter_hit_skill_check(bool is_hit, Game_Unit target, int? distance)
        {
            if (actor.has_skill("DETER") && is_hit)
            {
                if (valid_mastery_target("DETER", target, distance))
                {
                    int rate = skill_rate("DETER");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_deter();
                    }
                }
            }
        }

        // Adept
        private void adept_posthit_skill_check(Game_Unit target)
        {
            if (actor.has_skill("ADEPT") && !actor.adeptActivated && target != null && !target.actor.has_skill("CONFUSION"))
            {
                if (valid_mastery_target("ADEPT", target, 1))
                {
                    int rate = skill_rate("ADEPT");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_adept();
                    }
                }
            }
        }

        private void vandynerf_posthit_skill_check(Game_Unit target)
        {
            //vandynerf
            if (actor.has_skill("VANDYNERF") && target != null)
            {
                target.set_stat_bonus(FEXNA_Library.Buffs.Pow, -1);
                target.set_stat_bonus(FEXNA_Library.Buffs.Spd, -3);
            }
        }

        private void adept_hit_skill_update()
        {
            actor.adept_use();
        }

        // Spiral Dive
        private void astraaa_prehit_skill_check(Game_Unit target, int? distance)
        {
            if (actor.has_skill("ASTRAAA"))
            {
                if (valid_mastery_target("ASTRAAA", target, distance))
                {
                    int rate = skill_rate("ASTRAAA");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_astraaa();
                    }
                }
            }
        }

        private void astraaa_hit_skill_update()
        {
            actor.astraaa_use();
        }

        private bool adept_skip_skill_update()
        {
            return actor.adeptCount > 0;
        }

        // Frenzy
        private void frenzy_posthit_skill_check(Game_Unit target)
        {
            if (actor.has_skill("FRENZY") && !actor.frenzy_activated)
            {
                if (valid_mastery_target("FRENZY", target, 1))
                {
                    int rate = skill_rate("FRENZY");
                    if (Global.game_system.roll_rng(rate))
                    {
                        actor.activate_frenzy();
                    }
                }
            }
        }

        private void frenzy_hit_skill_update()
        {
            actor.frenzy_use();
        }

        private bool frenzy_skip_skill_update()
        {
            return actor.frenzy_count > 0;
        }
        #endregion

        #region Command Skills
        public void cover_ally(int ally_id)
        {
            // Skills: Savior
            set_rescue(FEXNA.State.Rescue_Modes.Cover, Id, ally_id);
            Global.player.loc = Global.game_map.units[ally_id].loc;
        }

        // Skills: Dash
        #region Dash
        internal bool DashActivated { get; private set; }
        private Vector2 DashPrevLoc;
        private int DashTempMoved;
        public void activate_dash()
        {
            DashActivated = true;
            DashPrevLoc = Prev_Loc;
            DashTempMoved = Temp_Moved;
            Temp_Moved = Moved_So_Far;

            Global.game_map.remove_updated_move_range(Id);
            update_move_range();

            open_move_range();
        }

        public void cancel_dash()
        {
            DashActivated = false;
            Prev_Loc = DashPrevLoc;
            Temp_Moved = DashTempMoved;

            Global.game_map.remove_updated_move_range(Id);
        }
        #endregion

        readonly static Dictionary<int, Vector2> COMMAND_DIRS = new Dictionary<int, Vector2> {
            { 2, new Vector2(0, 1) }, { 4, new Vector2(-1, 0) }, { 6, new Vector2(1, 0) }, { 8, new Vector2(0, -1) } };

        public int combat_distance_override()
        {
            // Skills: Swoop
            if (Swoop_Activated)
                return 1;
            return -1;
        }

        #region Swoop
        const int MIN_SWOOP_RANGE = 2;// 1; // increased from 1 because it allowed swooping at point blank //Yeti
        const int MAX_SWOOP_RANGE = 3;
        private bool Swoop_Activated;
        private bool Swoop_Attacked;
        public bool swoop_activated
        {
            get { return Swoop_Activated; }
            set { Swoop_Activated = value; }
        }

        public List<int>[] enemies_in_swoop_range()
        {
            //magic_attack = true; //Debug
            HashSet<Vector2> move_range = new HashSet<Vector2> { Loc };
            List<int> useable_weapons = useable_swoop_weapons();

            List<int> targets = new List<int>();
            List<int> weapons = new List<int>();
            if (useable_weapons.Count == 0)
                return new List<int>[] { targets, weapons };

            List<int>[] result;
            // Gets targets in range
            int min_range = MIN_SWOOP_RANGE;
            int max_range = MAX_SWOOP_RANGE;
            result = check_range(min_range, max_range, move_range, true, useable_weapons[0], "SWOOP");
            targets = result[0];
            targets = targets.Distinct().ToList(); //ListOrEquals
            // Removes targets on behind walls
            HashSet<Vector2> range = swoop_range();
            int i = 0;
            while (i < targets.Count)
            {
                Game_Unit target = Global.game_map.units[targets[i]];
                if (!range.Contains(target.loc) || target.nihil(this))
                    targets.RemoveAt(i);
                else
                    i++;
            }

            foreach (int index in useable_weapons)
                weapons.Add(actor.items[index].Id);
            return new List<int>[] { targets, weapons };
        }

        private List<int> useable_swoop_weapons()
        {
            List<int> useable_weapons = actor.useable_weapons();
            int i = 0;
            while (i < useable_weapons.Count)
            {
                if (min_range(useable_weapons[i], "SWOOP") > 1 || Global.data_weapons[items[useable_weapons[i]].Id].is_siege())
                    useable_weapons.RemoveAt(i);
                else
                    i++;
            }
            return useable_weapons;
        }

        public HashSet<Vector2> swoop_range()
        {
            HashSet<Vector2> range = Pathfinding.get_range_around(new HashSet<Vector2> { Loc }, MAX_SWOOP_RANGE, MIN_SWOOP_RANGE, true);
            return range;
        }
        #endregion

        #region Old Swoop
        const int OLD_SWOOP_RANGE = 5;
        private bool Old_Swoop_Activated;
        private bool Old_Swoop_Attacked;
        public bool old_swoop_activated
        {
            get { return Old_Swoop_Activated; }
            set { Old_Swoop_Activated = value; }
        }

        public List<int>[] enemies_in_old_swoop_range()
        {
            return enemies_in_old_swoop_range(0);
        }
        public List<int>[] enemies_in_old_swoop_range(int facing)
        {
            magic_attack = true; //This should be forcing magic attack to false based on some if statements...?
            HashSet<Vector2> move_range = new HashSet<Vector2> { Loc };
            List<int> useable_weapons = actor.useable_melee_weapons();

            List<int> targets = new List<int>();
            List<int> weapons = new List<int>();
            if (useable_weapons.Count == 0)
                return new List<int>[] { targets, weapons };

            List<int>[] result;
            // Gets targets in range
            int min_range = 1;
            int max_range = OLD_SWOOP_RANGE;
            result = check_range(min_range, max_range, move_range, true, 0, "OLDSWOOP");
            targets = result[0];
            targets = targets.Distinct().ToList(); //ListOrEquals
            // Removes targets on diagonals/behind walls
            HashSet<Vector2> range = old_swoop_range(facing);
            int i = 0;
            while (i < targets.Count)
            {
                Game_Unit target = Global.game_map.units[targets[i]];
                if (!range.Contains(target.loc) || target.nihil(this))
                    targets.RemoveAt(i);
                else
                    i++;
            }

            foreach (int index in useable_weapons)
                weapons.Add(actor.items[index].Id);
            return new List<int>[] { targets, weapons };
        }

        public HashSet<Vector2> old_swoop_range()
        {
            return old_swoop_range(0);
        }
        public HashSet<Vector2> old_swoop_range(int facing)
        {
            HashSet<Vector2> range = new HashSet<Vector2>();
            foreach (KeyValuePair<int, Vector2> dir in COMMAND_DIRS)
            {
                if (facing != 0)
                    if (dir.Key != facing)
                        continue;
                for (int i = 1; i <= OLD_SWOOP_RANGE; i++)
                    if (Pathfinding.passable(this, Loc + dir.Value * i))
                        range.Add(Loc + dir.Value * i);
                    else
                        break;
            }
            return range;
        }
        #endregion

        #region Multishot
        private bool Multishot_Activated;
        private bool Multishot_Attacked;
        public bool multishot_activated
        {
            get { return Multishot_Activated; }
            set { Multishot_Activated = value; }
        }
        #endregion

        #region Trample
        private bool Trample_Activated;
        private Vector2 Trample_Loc;
        public bool trample_activated
        {
            get { return Trample_Activated; }
            set { Trample_Activated = value; }
        }
        public Vector2 trample_loc
        {
            get { return Trample_Loc; }
        }

        public List<int>[] enemies_in_trample_range()
        {
            return enemies_in_trample_range(0);
        }
        public List<int>[] enemies_in_trample_range(int facing)
        {
            magic_attack = true; // wait why //Yeti //This should be forcing magic attack to false based on some if statements...?
            HashSet<Vector2> move_range = new HashSet<Vector2> { Loc };
            List<int> useable_weapons = actor.useable_melee_weapons();

            List<int> targets = new List<int>();
            List<int> weapons = new List<int>();
            if (useable_weapons.Count == 0)
                return new List<int>[] { targets, weapons };

            // Gets targets in range
            List<int>[] result = check_range(1, 1, move_range, true, useable_weapons.First(), "TRAMPLE");
            targets = result[0];
            targets = targets.Distinct().ToList(); //ListOrEquals
            // Removes targets that don't have a clear space behind them
            HashSet<Vector2> range = trample_range(facing);
            int i = 0;
            while (i < targets.Count)
            {
                Game_Unit target = Global.game_map.units[targets[i]];
                if (!range.Contains(target.loc) || target.nihil(this))
                    targets.RemoveAt(i);
                else
                    i++;
            }

            foreach (int index in useable_weapons)
                weapons.Add(actor.items[index].Id);
            return new List<int>[] { targets, weapons };
        }

        public HashSet<Vector2> trample_range()
        {
            return trample_range(0);
        }
        public HashSet<Vector2> trample_range(int facing)
        {
            HashSet<Vector2> range = new HashSet<Vector2>();
            foreach (KeyValuePair<int, Vector2> dir in COMMAND_DIRS)
            {
                if (facing != 0)
                    if (dir.Key != facing)
                        continue;
                if (!Global.game_map.is_off_map(Loc + dir.Value * 2) &&
                    Pathfinding.passable(this, Loc + dir.Value * 1) && (Pathfinding.passable(this, Loc + dir.Value * 2) &&
                    !Global.game_map.is_blocked(Loc + dir.Value * 2, Id, false)) &&
                    (!Global.game_map.fow || Global.game_map.fow_visibility[Team].Contains(Loc + dir.Value * 2)))
                    range.Add(Loc + dir.Value * 1);
            }
            return range;
        }

        public HashSet<Vector2> trample_move_range()
        {
            return trample_move_range(0);
        }
        public HashSet<Vector2> trample_move_range(int facing)
        {
            HashSet<Vector2> range = new HashSet<Vector2>();
            foreach (KeyValuePair<int, Vector2> dir in COMMAND_DIRS)
            {
                if (facing != 0)
                    if (dir.Key != facing)
                        continue;
                if (!Global.game_map.is_off_map(Loc + dir.Value * 2) &&
                    Pathfinding.passable(this, Loc + dir.Value * 1) && (Pathfinding.passable(this, Loc + dir.Value * 2) &&
                    !Global.game_map.is_blocked(Loc + dir.Value * 2, Id, false)) &&
                    (!Global.game_map.fow || Global.game_map.fow_visibility[Team].Contains(Loc + dir.Value * 2)))
                    range.Add(Loc + dir.Value * 2);
            }
            return range;
        }

        public void set_trample_loc()
        {
            Trample_Loc = Loc + COMMAND_DIRS[facing] * 2;
        }

        public void trample_move()
        {
            if (Trample_Activated)
            {
                Move_Loc = Loc + COMMAND_DIRS[facing] * 2;
                Move_Route.Add(COMMAND_DIRS[facing]);
                Move_Route.Add(COMMAND_DIRS[facing]);
                Temp_Moved += 2;
                Move_Timer = 0;
            }
        }
        #endregion

        #region Sacrifice
        public int sacrifice_heal_amount(Game_Unit target)
        {
            return Math.Min(
                Math.Min(target.actor.maxhp - target.actor.hp, 2 * actor.stat(Stat_Labels.Lck)),
                actor.hp - 1);
        }
        #endregion

        private bool skill_cancel_move()
        {
            // Skills: Dash
            if (DashActivated)
            {
                DashActivated = false;
                Prev_Loc = DashPrevLoc;
                Temp_Moved = DashTempMoved;

                if (Input.ControlScheme == ControlSchemes.Buttons)
                {
                    Global.player.force_loc(Loc);
                    Global.player.instant_move = true;
                }

                Global.game_map.remove_updated_move_range(Id);
                update_move_range(Prev_Loc, Prev_Loc);

                Global.game_map.clear_move_range();
                Global.game_map.show_move_range(Id);
                Global.game_map.show_attack_range(Id);

                Global.game_system.Menu_Canto &= ~Canto_Records.Dash;

                Global.game_map.move_range_visible = false;
                Global.game_temp.menu_call = true;
                Global.game_temp.unit_menu_call = true;

                return true;
            }
            return false;
        }

        private void skill_wait()
        {
            // Skills: Dash
            if (DashActivated)
            {
                Temp_Moved += DashTempMoved;

                DashActivated = false;
            }
        }
        #endregion

        #region Base Stat Bonuses
        public int stat_bonus_display(Stat_Labels stat)
        {
            int n = stat_bonus(stat);
            switch (stat)
            {
                case Stat_Labels.Pow:
                    return pow_bonus_display;
                case Stat_Labels.Spd:
                    return spd_bonus_display;
            }
            return n;
        }

        public int pow_bonus_display
        {
            get
            {
                int n = stat_bonus(Stat_Labels.Pow);
                // Skills: Knife
                if (actor.has_skill("KNIFE"))
                    if (actor.weapon != null && actor.weapon.main_type().Name == "Sword" && !actor.weapon.is_magic())
                    {
                        int base_pow = stat(Stat_Labels.Pow) - stat_bonus(Stat_Labels.Pow);
                        int base_skl = stat(Stat_Labels.Skl) - stat_bonus(Stat_Labels.Skl);
                        n += Math.Min(actor.get_cap(Stat_Labels.Pow), (base_pow + base_skl) / 2) - base_pow;
                    }
                return n;
            }
        }
        private int pow_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Pow+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "POW+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Pow))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                // Skills: Guts
                //if (actor.has_skill("GUTS"))
                //    n += ((actor.maxhp - actor.hp) / 10);
                // Skills: Focus
                if (actor.has_skill("FOCUS"))
                    if (Temp_Moved <= 1 && !(Global.game_system.home_base || Global.game_system.In_Arena || Global.scene.is_test_battle))
                        n += 2;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("YURI") && !is_attackable_team(unit))
                        {
                            n += 2;
                            switch (ActorId)
                            {
                                case 2:
                                case 5:
                                case 16:
                                    n += 6;
                                    break;
                                default: break;
                            }
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("INSPIRE") && !is_attackable_team(unit))
                        {
                            n += 4;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("CHARI") && !is_attackable_team(unit))
                        {
                            n += 2;
                            break;
                        }
                    }
                if (actor.has_skill("ROLL"))
                    n += actor.attack_stacks * 2;
                n += actor.vandy_stacks;
                if (actor.has_skill("EST"))
                    n += actor.level;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("ANNOYING"))
                        {
                            n -= 7;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc) && actor.has_skill("CHARIT"))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (!is_attackable_team(unit))
                        {
                            n += 1;
                        }
                    }
                if (actor.has_skill("LONER"))
                {
                    int bnous = 4;
                    if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                        foreach (int id in units_in_range(2))
                        {
                            Game_Unit unit = Global.game_map.units[id];
                            if (!is_attackable_team(unit))
                            {
                                bnous -= 2;
                            }
                        }
                    n += Math.Max(bnous, 0);
                }
                if (actor.has_skill("ANGLE"))// && actor.hp < actor.maxhp / 10)
                    n += 8;
                if (actor.has_skill("OMEGATRUMP"))
                    n += actor.stat(Stat_Labels.Pow) * 7;
                if (actor.has_skill("PIGTAILS"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 3://ash
                        case 6://elise
                        case 7://av
                        case 9://purt
                        case 11://set
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("FLUFFS"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 2://reisen
                        case 3://ash
                        case 6://elise
                        case 9://purt
                        case 10://wolfram
                        case 15://mofo
                        case 17://bob
                            n += MAG_EFFECT * 2;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT;
                            break;
                    }
                if (actor.has_skill("BARE"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 3://ash
                        case 5://char
                        case 6://elise
                        case 7://av
                        case 9://purt
                        case 10://wolfram
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 11://set
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("THICK"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 8://olivia
                        case 6://elise
                        case 12://hiro
                        case 14://gertrude
                        case 15://mofo
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 3://ash
                        case 4://ciraxis
                        case 5://char
                        case 9://purt
                        case 11://set
                        case 13://???
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(3))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (ActorId == 34 && unit.actor.has_ring)
                        {
                            n -= 35;
                            break;
                        }
                    }
                return n;
            }
        }

        private int skl_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Skl+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "SKL+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Skl))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                // Skills: Guts
                //if (actor.has_skill("GUTS"))
                //    n += ((actor.maxhp - actor.hp) / 10);
                if (actor.has_skill("EST"))
                    n += actor.level;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("ANNOYING"))
                        {
                            n -= 7;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc) && actor.has_skill("CHARIT"))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (!is_attackable_team(unit))
                        {
                            n += 1;
                        }
                    }
                if (actor.has_skill("LONER"))
                {
                    int bnous = 4;
                    if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                        foreach (int id in units_in_range(2))
                        {
                            Game_Unit unit = Global.game_map.units[id];
                            if (!is_attackable_team(unit))
                            {
                                bnous -= 2;
                            }
                        }
                    n += Math.Max(bnous, 0);
                }
                if (actor.has_skill("ANGLE"))// && actor.hp < actor.maxhp / 10)
                    n += 8;
                if (actor.has_skill("OMEGATRUMP"))
                    n += actor.stat(Stat_Labels.Skl) * 7;
                if (actor.has_skill("PIGTAILS"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 3://ash
                        case 6://elise
                        case 7://av
                        case 9://purt
                        case 11://set
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT * 2;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT;
                            break;
                    }
                if (actor.has_skill("FLUFFS"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 2://reisen
                        case 3://ash
                        case 6://elise
                        case 9://purt
                        case 10://wolfram
                        case 15://mofo
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("LOOSE"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 2://reisen
                        case 3://ash
                        case 6://elise
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(3))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (ActorId == 34 && unit.actor.has_ring)
                        {
                            n -= 40;
                            break;
                        }
                    }
                return n;
            }
        }

        public int spd_bonus_display
        {
            get
            {
                int n = stat_bonus(Stat_Labels.Spd);
                // Skills: Crossbow
                if (actor.has_skill("CROSSBOW"))
                    if (actor.weapon != null && actor.weapon.main_type().Name == "Bow" && !actor.weapon.Ballista())
                    {
                        int base_pow = stat(Stat_Labels.Pow) - stat_bonus(Stat_Labels.Pow);
                        int base_spd = stat(Stat_Labels.Spd) - stat_bonus(Stat_Labels.Spd);
                        //n += Math.Min(spd_cap(), (base_pow + base_spd) / 2) - base_spd; //Debug
                        n += (base_pow + base_spd) / 2 - base_spd;
                    }
                return n;
            }
        }
        private int spd_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Spd+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "SPD+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Spd))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                // Skills: El's Passage
                if (actor.has_skill("EL"))
                    n += 5;
                //if (actor.has_skill("GUTS"))
                //    n += ((actor.maxhp - actor.hp) / 10);
                if (actor.has_skill("ROLL"))
                    n += actor.attack_stacks * 2;
                if (actor.has_skill("EST"))
                    n += actor.level;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("ANNOYING"))
                        {
                            n -= 7;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc) && actor.has_skill("CHARIT"))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (!is_attackable_team(unit))
                        {
                            n += 1;
                        }
                    }
                if (actor.has_skill("LONER"))
                {
                    int bnous = 4;
                    if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                        foreach (int id in units_in_range(2))
                        {
                            Game_Unit unit = Global.game_map.units[id];
                            if (!is_attackable_team(unit))
                            {
                                bnous -= 2;
                            }
                        }
                    n += Math.Max(bnous, 0);
                }
                if (actor.has_skill("ANGLE"))// && actor.hp < actor.maxhp / 10)
                    n += 8;
                if (actor.has_skill("PIGTAILS"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 3://ash
                        case 6://elise
                        case 7://av
                        case 9://purt
                        case 11://set
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("MANES"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 4://ciraxis
                        case 5://char
                        case 6://elise
                        case 14://gertrude
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 8://olivia
                        case 13://???
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("LOOSE"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 2://reisen
                        case 3://ash
                        case 6://elise
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT * 2;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT;
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(3))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (ActorId == 34 && unit.actor.has_ring)
                        {
                            n -= 38;
                            break;
                        }
                    }
                return n;
            }
        }

        private int lck_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Lck+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "LCK+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Lck))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                if (actor.has_skill("EST"))
                    n += actor.level;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("ANNOYING"))
                        {
                            n -= 7;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc) && actor.has_skill("CHARIT"))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (!is_attackable_team(unit))
                        {
                            n += 1;
                        }
                    }
                if (actor.has_skill("LONER"))
                {
                    int bnous = 4;
                    if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                        foreach (int id in units_in_range(2))
                        {
                            Game_Unit unit = Global.game_map.units[id];
                            if (!is_attackable_team(unit))
                            {
                                bnous -= 2;
                            }
                        }
                    n += Math.Max(bnous, 0);
                }
                if (actor.has_skill("ANGLE"))// && actor.hp < actor.maxhp / 10)
                    n += 8;
                if (actor.has_skill("BARE"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 3://ash
                        case 5://char
                        case 6://elise
                        case 7://av
                        case 9://purt
                        case 10://wolfram
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT * 2;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 11://set
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT;
                            break;
                    }
                if (actor.has_skill("THICK"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 8://olivia
                        case 6://elise
                        case 12://hiro
                        case 14://gertrude
                        case 15://mofo
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 3://ash
                        case 4://ciraxis
                        case 5://char
                        case 9://purt
                        case 11://set
                        case 13://???
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(3))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (ActorId == 34 && unit.actor.has_ring)
                        {
                            n -= 15;
                            break;
                        }
                    }
                return n;
            }
        }

        private int def_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Def+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "DEF+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Def))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                // Skills: Focus
                if (actor.has_skill("FOCUS"))
                    if (Temp_Moved <= 1 && !(Global.game_system.home_base || Global.game_system.In_Arena || Global.scene.is_test_battle))
                        n += 2;
                // Skills: Mot's Mercy
                if (actor.has_skill("MOT"))
                    n += 10;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("YURI") && !is_attackable_team(unit))
                        {
                            n += 6;
                            switch (ActorId)
                            {
                                case 2:
                                case 5:
                                case 16:
                                    n += 2;
                                    break;
                                default: break;
                            }
                            break;
                        }
                    }
                if (actor.has_skill("SHIELDED") && Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(50))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("SHIELDING"))
                        {
                            n += 50;
                            break;
                        }
                        if (n > 40)
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("INSPIRE") && !is_attackable_team(unit))
                        {
                            n += 4;
                            break;
                        }
                    }
                if (actor.has_skill("SALAMI"))
                {
                    n += Math.Max(55 - (5 * Global.game_system.chapter_turn), 0);
                }
                if (actor.has_skill("EST"))
                    n += actor.level;
                if (actor.has_skill("NUMB"))
                    n -= 2;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("ANNOYING"))
                        {
                            n -= 7;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc) && actor.has_skill("CHARIT"))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (!is_attackable_team(unit))
                        {
                            n += 1;
                        }
                    }
                if (actor.has_skill("LONER"))
                {
                    int bnous = 4;
                    if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                        foreach (int id in units_in_range(2))
                        {
                            Game_Unit unit = Global.game_map.units[id];
                            if (!is_attackable_team(unit))
                            {
                                bnous -= 2;
                            }
                        }
                    n += Math.Max(bnous, 0);
                }
                if (actor.has_skill("ANGLE"))// && actor.hp < actor.maxhp / 10)
                    n += 8;
                if (actor.has_skill("HIATUSING"))
                    n += 999;
                if (actor.has_skill("MANES"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 4://ciraxis
                        case 5://char
                        case 6://elise
                        case 14://gertrude
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 8://olivia
                        case 13://???
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("THICK"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 8://olivia
                        case 6://elise
                        case 12://hiro
                        case 14://gertrude
                        case 15://mofo
                        case 17://bob
                            n += MAG_EFFECT * 2;
                            break;
                        case 3://ash
                        case 4://ciraxis
                        case 5://char
                        case 9://purt
                        case 11://set
                        case 13://???
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT;
                            break;
                    }
                if (actor.has_skill("LOOSE"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 2://reisen
                        case 3://ash
                        case 6://elise
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(3))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (ActorId == 34 && unit.actor.has_ring)
                        {
                            n -= 40;
                            break;
                        }
                    }
                return n;
            }
        }

        private int res_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Res+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "RES+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Res))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                // Skills: Mot's Mercy
                if (actor.has_skill("MOT"))
                    n += 10;
                if (actor.has_skill("SALAMI"))
                {
                    n += Math.Max(55 - (5 * Global.game_system.chapter_turn), 0);
                }
                if (actor.has_skill("EST"))
                    n += actor.level;
                if (actor.has_skill("NUMB"))
                    n -= 2;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("YURI") && !is_attackable_team(unit))
                        {
                            n += 6;
                            switch (ActorId)
                            {
                                case 2:
                                case 5:
                                case 16:
                                    n += 2;
                                    break;
                                default: break;
                            }
                            break;
                        }
                    }
                if (actor.has_skill("SHIELDED") && Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(50))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("SHIELDING"))
                        {
                            n += 50;
                            break;
                        }
                        if (n > 40)
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("INSPIRE") && !is_attackable_team(unit))
                        {
                            n += 4;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(1))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (unit.actor.has_skill("ANNOYING"))
                        {
                            n -= 7;
                            break;
                        }
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc) && actor.has_skill("CHARIT"))
                    foreach (int id in units_in_range(2))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (!is_attackable_team(unit))
                        {
                            n += 1;
                        }
                    }
                if (actor.has_skill("LONER"))
                {
                    int bnous = 4;
                    if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                        foreach (int id in units_in_range(2))
                        {
                            Game_Unit unit = Global.game_map.units[id];
                            if (!is_attackable_team(unit))
                            {
                                bnous -= 2;
                            }
                        }
                    n += Math.Max(bnous, 0);
                }
                if (actor.has_skill("ANGLE"))// && actor.hp < actor.maxhp / 10)
                    n += 8;
                if (actor.has_skill("HIATUSING"))
                    n += 999;
                if (actor.has_skill("FLUFFS"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 2://reisen
                        case 3://ash
                        case 6://elise
                        case 9://purt
                        case 10://wolfram
                        case 15://mofo
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (actor.has_skill("MANES"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 4://ciraxis
                        case 5://char
                        case 6://elise
                        case 14://gertrude
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT * 2;
                            break;
                        case 8://olivia
                        case 13://???
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT;
                            break;
                    }
                if (actor.has_skill("BARE"))
                    switch (ActorId)
                    {
                        case 1://marc
                        case 3://ash
                        case 5://char
                        case 6://elise
                        case 7://av
                        case 9://purt
                        case 10://wolfram
                        case 15://mofo
                        case 16://sepour
                        case 17://bob
                            n += MAG_EFFECT;
                            break;
                        case 4://ciraxis
                        case 8://olivia
                        case 11://set
                        case 12://hiro
                        case 13://???
                        case 14://gertrude
                            n += 0;
                            break;
                        default:
                            n += MAG_EFFECT / 2;
                            break;
                    }
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(3))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (ActorId == 34 && unit.actor.has_ring)
                        {
                            n -= 40;
                            break;
                        }
                    }
                return n;
            }
        }

        private int con_bonus_skill
        {
            get
            {
                int n = 0;
                // Skills: Con+
                foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "CON+"))
                {
                    double str_test;
                    string name = Global.data_skills[skill_id].Abstract;
                    if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                        n += Convert.ToInt32(name.Substring(4, name.Length - 4));
                }
                // Skills: Affinity+
                // Con doesn't have a growth rate so is this necessary //Yeti
                if (actor.affin != Affinities.None && Constants.Support.AFFINITY_GROWTHS[actor.affin][0].Contains(Stat_Labels.Con))
                    foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 6) == "AFFIN+"))
                    {
                        double str_test;
                        string name = Global.data_skills[skill_id].Abstract;
                        if (double.TryParse(name.substring(6, name.Length - 6), out str_test))
                            n += Convert.ToInt32(name.Substring(6, name.Length - 6));
                    }
                return n;
            }
        }
        #endregion

        #region Unit Stat Affecting
        private bool weighted_by_ally_skill()
        {
            // Skills: Savior
            if (actor.has_skill("SAVIOR"))
                return true;
            return false;
        }

        private bool is_weighted_skl_override
        {
            get
            {
                // Skills: Drunk
                if (actor.has_skill("DRUNK"))
                    return true;
                return false;
            }
        }
        private bool is_weighted_spd_override
        {
            get
            {
                // Skills: Drunk
                if (actor.has_skill("DRUNK"))
                    return true;
                return false;
            }
        }
        #endregion

        #region Movement/Range Affecting
        private bool canto_skill()
        {
            // Skills: Canto
            if (actor.has_skill("CANTO"))
                return true;
            // Skills: Flight
            if (actor.has_skill("FLIGHT"))// && !is_weighted_by_ally) //Yeti
                return true;
            return false;
        }
        private bool attack_canto_skill()
        {
            // Skills: Strafing
            if (actor.has_skill("STRAFING"))
                return true;
            // Skills: Trample
            if (Trample_Activated && has_canto())
                return true;
            return false;
        }

        private bool cover_skill()
        {
            // Skills: Savior
            if (actor.has_skill("SAVIOR"))
                return true;
            return false;
        }

        private int vision_skill(int base_vision, int vision_bonus)
        {
            // Skills: Drunk
            if (actor.has_skill("DRUNK"))
                return 1;
            return base_vision + vision_bonus;
        }

        internal bool vision_penalized()
        {
            // Skills: Drunk
            if (actor.has_skill("DRUNK"))
                return true;

            return false;
        }

        private bool siege_skill()
        {
            // Skills: Siege Training
            if (actor.has_skill("SIEGE"))
                return true;
            // Skills: Siege Mastery
            if (actor.has_skill("SIEGEMST"))
                return true;
            return false;
        }

        private bool can_dance_skill()
        {
            // Skills: Play
            if (actor.has_skill("PLAY"))
                return true;
            // Skills: Dance
            if (actor.has_skill("DANCE"))
                return true;
            return false;
        }

        private string dance_name_skill()
        {
            // Skills: Play
            if (actor.has_skill("PLAY"))
                return "Play";
            // Skills: Dance
            if (actor.has_skill("DANCE"))
                return "Dance";
            return "";
        }

        public bool can_pass_enemies()
        {
            // Skills: Silent Movement
            if (actor.has_skill("SILENT") && !Global.game_map.fow)
                return true;
            return false;
        }

        public bool aoe_exp_buff_skill()
        {
            if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(1))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("EXPVOICE") && !is_attackable_team(unit))
                    {
                        return true;
                    }
                }
            return false;
        }

        private bool can_rescue_skill()
        {
            // Skills: Drunk
            if (actor.has_skill("DRUNK"))
                return false;
            if (actor.has_skill("HIATUSING"))
                return false;
            return true;
        }

        private int mov_plus_skill()
        {
            int n = 0;
            // Skills: Mov+
            foreach (int skill_id in actor.all_skills.Where(x => Global.data_skills[x].Abstract.substring(0, 4) == "MOV+"))
            {
                double str_test;
                string name = Global.data_skills[skill_id].Abstract;
                if (double.TryParse(name.substring(4, name.Length - 4), out str_test))
                    n += Convert.ToInt32(name.Substring(4, name.Length - 4));
            }
            //if (actor.has_skill("CANTOFORCE"))
            //    n -= cantoforce_used;
            // Skills: Flight
            if (actor.has_skill("FLIGHT") && !is_weighted_by_ally)
                n += 2;
            // Skills: Celerity
            if (actor.has_skill("CELERITY"))
                n += 2;
            // Skills: El's Passage
            if (actor.has_skill("EL"))
                n += 2;
            if (actor.has_skill("SHIELDING"))
                n -= 6;
            if(actor.has_skill("LAZY") && !actor.has_skill("CELERITY"))
            {
                n -= actor.mov - 1;
                if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                    foreach (int id in units_in_range(5))
                    {
                        Game_Unit unit = Global.game_map.units[id];
                        if (is_attackable_team(unit))
                        {
                            n = 0;
                            break;
                        }
                    }
            }
            if (actor.has_skill("SALAMI") && Global.game_system.SWITCHES[69])
                n -= 3;
            // Skills: Slow
            if (actor.has_skill("SLOW"))
            {
                int slow_move_limit = 3;
                int current_move_score =
                    base_mov + actor.mov_plus + Stat_Bonuses[(int)Buffs.Mov] + n;
                n += -Math.Max(0, current_move_score - slow_move_limit);
                //n += -(int)MathHelper.Clamp((base_mov) - 4, 0, 2);
                //n += -(int)MathHelper.Clamp((base_mov + n) - 4, 0, 2); //Debug
            }
            if (actor.has_skill("HIATUSING"))
                n = -999;
#if DEBUG
            if (Global.scene.scene_type != "Scene_Map_Unit_Editor")
                if (!Global.game_system.is_interpreter_running)
                    if (is_ally && actor.mov > 0)
                        if (INFINITE_MOVE_ALLOWED)
                            return 100;
#endif
            return n;
        }

#if DEBUG
        //Cheat codes
        internal static bool INFINITE_MOVE_ALLOWED = false;
        static bool ANY_TERRAIN_PASSABLE = false;
#endif

        private int min_range_skill(Data_Weapon weapon, int min_range)
        {
            // Skills: Point Blank Shot
            if (actor.has_skill("PNTBLNK"))
                if (weapon.main_type().Name == "Bow" && min_range == 2)
                    return 1;
            if (actor.has_skill("HIATUSING"))
                return 0;
            return min_range;
        }

        private int max_range_skill(Data_Weapon weapon, int max_range)
        {
            // Skills: Longbow
            if (actor.has_skill("LONGBOW") && Global.game_system.chapter_turn % 2 == 0)
                //if (weapon.main_type().Name == "Bow" && !weapon.Ballista())
                if(weapon.is_imbued() || weapon.is_magic())
                    return max_range + 1;
            if (actor.has_skill("REACH"))
                if (weapon.type == "Axe" && weapon.Max_Range < 2)
                    return 2;
            // Skills: Knife
            if (actor.has_skill("KNIFE"))
                if (!weapon.is_magic() && weapon.main_type().Name == "Sword" && max_range == 1)
                    return 2;
            // Skills: Kamaitachi
            if (actor.has_skill("KAMAI"))
                if (!weapon.is_magic() && weapon.Max_Range == 1)
                    return 2;
            if (actor.has_skill("SPEEDMETAL") && weapon.Max_Range == 1)
                return 3;
            if (actor.has_skill("HIATUSING"))
                return 0;
            return max_range;
        }

        private int Anticipation_Equip = -1;
        public void target_unit(Game_Unit attacker, Data_Weapon weapon1, int distance)
        {
            // Skills: Anticipation
            Anticipation_Equip = actor.equipped;
            if (actor.has_skill("ANTICI"))
                if (!weapon1.No_Counter && !can_counter(attacker, weapon1, distance))
                    for (int i = 0; i < Constants.Actor.NUM_ITEMS; i++)
                        if (can_counter(attacker, weapon1, distance, i))
                        {
                            actor.equip(i + 1);
                            break;
                        }
        }

        public void accept_targeting()
        {
            // Skills Anticipation
            if (actor.has_skill("ANTICI") && Anticipation_Equip != -1)
                actor.organize_items();
            Anticipation_Equip = -1;
        }

        public void cancel_targeted()
        {
            // Skills: Anticipation
            actor.equip(Anticipation_Equip);
            Anticipation_Equip = -1;
            //if (actor.has_skill("ROLL"))
            //    actor.attack_stacks--;
        }

        #region MOVE COST MODS
        readonly static Dictionary<string, Dictionary<int, Tuple<bool, int>>> MOVE_COST_MODS =
            new Dictionary<string, Dictionary<int, Tuple<bool, int>>> {
                { "FRSTMV", new Dictionary<int, Tuple<bool, int>> {
                    { 12, new Tuple<bool, int>(false, -1) } // Forest
                }},
                { "NMDMV", new Dictionary<int, Tuple<bool, int>> {
                    { 12, new Tuple<bool, int>(false, -1) }, // Forest
                    { 15, new Tuple<bool, int>(false, -1) }, // Desert
                    { 16, new Tuple<bool, int>(true, 5) }, // River
                    { 17, new Tuple<bool, int>(false, -1) } // Hill
                }},
                { "SEAMV", new Dictionary<int, Tuple<bool, int>> {
                    { 16, new Tuple<bool, int>(true, 2) }, // River
                    { 21, new Tuple<bool, int>(true, 2) }, // Sea
                    { 22, new Tuple<bool, int>(true, 3) }, // Lake
                    { 60, new Tuple<bool, int>(true, 3) } // Water
                }},
                { "MNTNMV", new Dictionary<int, Tuple<bool, int>> {
                    { 17, new Tuple<bool, int>(false, -1) }, // Hill
                    { 18, new Tuple<bool, int>(true, 4) } // Peak
                }},
                { "PHANT", new Dictionary<int, Tuple<bool, int>> {
                    { 26, new Tuple<bool, int>(false, -1) }, // wall
                    { 27, new Tuple<bool, int>(true, -1) } // weak wall...?
                }},
        };
        #endregion
        public int move_cost_skill(Vector2 target_loc, int cost)
        {
#if DEBUG
            if (is_ally)
                if (!Global.game_system.is_interpreter_running)
                    if (ANY_TERRAIN_PASSABLE)
                        return 1;
#endif
            int tag = Global.game_map.terrain_tag(target_loc);
            // Skills: Movement Cost Skills
            foreach (string skill in MOVE_COST_MODS.Keys)
            {
                if (actor.has_skill(skill))
                    if (MOVE_COST_MODS[skill].ContainsKey(tag))
                    {
                        // If overwriting the cost
                        if (MOVE_COST_MODS[skill][tag].Item1)
                            cost = (cost == -1 ? MOVE_COST_MODS[skill][tag].Item2 : Math.Min(cost, MOVE_COST_MODS[skill][tag].Item2));
                            //cost = (int)MathHelper.Clamp(cost, MOVE_COST_MODS[skill][tag].Value, 255); //Debug
                        // Else modifying the existing cost
                        else
                            cost = (cost == -1 ? -1 : Math.Max(1, cost + MOVE_COST_MODS[skill][tag].Item2));
                    }
            }
            // Skills: Guerilla
            if (actor.has_skill("GUER") && cost > 0)
                return 1;
            return cost;
        }

        private bool can_visit_skill()
        {
            // Skills: Drunk
            if (actor.has_skill("DRUNK"))
                return false;
            // Skills: Silenced
            if (actor.has_skill("SILENCED"))
                return false;
            return true;
        }

        private bool can_talk_skill()
        {
            // Skills: Silenced
            if (actor.has_skill("SILENCED"))
                return false;
            return true;
        }

        private bool can_support_skill()
        {
            // Skills: Drunk
            if (actor.has_skill("DRUNK"))
                return false;
            // Skills: Silenced
            if (actor.has_skill("SILENCED"))
                return false;
            return true;
        }

        private bool door_open_skill()
        {
            // Skills: Pick
            if (actor.has_skill("PICK"))
                return true;
            return false;
        }

        private Maybe<int> door_key_skill()
        {
            // Skills: Pick
            if (actor.has_skill("PICK"))
                return -1;
            return new Maybe<int>();
        }

        private bool chest_open_skill()
        {
            // Skills: Pick
            if (actor.has_skill("PICK"))
                return true;
            return false;
        }

        private Maybe<int> chest_key_skill()
        {
            // Skills: Pick
            if (actor.has_skill("PICK"))
                return -1;
            return new Maybe<int>();
        }
        #endregion

        #region Graphics
        readonly static Dictionary<string, Color> SKILL_AURA_COLORS = new Dictionary<string, Color>{
            { "DREAD", new Color(40, 16, 48, 104) },
            { "OBSTRUCT", new Color(40, 16, 48, 104) },
            { "PSTG", new Color(72, 56, 16, 64) },
            {"RING", new Color(80, 40, 16, 32)}
        };
        public readonly static List<Color> AURA_COLOR_ORDER = new List<Color>{
            SKILL_AURA_COLORS["DREAD"],
            SKILL_AURA_COLORS["PSTG"],
            SKILL_AURA_COLORS["RING"],
            SKILL_AURA_COLORS["OBSTRUCT"]
        };

        public bool has_aura()
        {
            // Skills: Dreaded
            if (actor.has_skill("DREAD"))
                return true;
            // Skills: Prestige
            if (actor.has_skill("PSTG") || actor.has_skill("OBSTRUCT"))
                return true;

            if (actor.has_ring && Global.game_map.is_actor_deployed(34))
                return true;

            return false;
        }

        public Color aura_color()
        {
            // Skills: Dreaded
            if (actor.has_skill("DREAD"))
                return SKILL_AURA_COLORS["DREAD"];
            // Skills: Prestige
            if (actor.has_skill("PSTG"))
                return SKILL_AURA_COLORS["PSTG"];
            if (actor.has_ring)
                return SKILL_AURA_COLORS["RING"];

            return Color.Transparent;
        }

        public int aura_radius()
        {
            // Skills: Dreaded
            if (actor.has_skill("DREAD") || actor.has_skill("OBSTRUCT"))
                return 3;
            // Skills: Prestige
            if (actor.has_skill("PSTG"))
                return 3;
            if (actor.has_ring)
                return 3;

            return 1;
        }
        #endregion

        public int skill_new_turn_heal_amount()
        {
            int n = 0;
            if (actor.has_skill("SEPOUR"))
                n += actor.maxhp;
            // Skills: Renewal (Mastery)
            if (actor.has_skill("OLD_RENEWAL"))
                n += (actor.maxhp * 25) / 100; //5; //Debug
            // Skills: Renewal
            if (actor.has_skill("RENEW"))
                n += 5;//(int)(actor.maxhp * 0.15f);
            // Skills: Provision
            if (Global.scene.is_map_scene && !Global.game_map.is_off_map(Loc))
                foreach (int id in units_in_range(1))
                {
                    Game_Unit unit = Global.game_map.units[id];
                    if (unit.actor.has_skill("PROVISION") && !is_attackable_team(unit))
                    {
                        n += 10;
                        break;
                    }
                }
            return n;
        }

        public bool has_skill_healing()
        {
            return skill_new_turn_heal_amount() != 0;
        }

        public bool counters_first(Game_Unit target)
        {
            // Skills: Vantage
            if (target != null && !nihil(target))
                if (actor.has_skill("VANT") && !target.overrides_ambush_counter())
                    return true;
            return false;
        }

        public bool overrides_ambush_counter()
        {
            // Skills: Vantage
            if (actor.has_skill("VANT"))
                return true;
            return false;
        }
    }
}
