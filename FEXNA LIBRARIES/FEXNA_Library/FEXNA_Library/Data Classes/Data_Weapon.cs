using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using ArrayExtension;

namespace FEXNA_Library
{
    //public enum Weapon_Types { None, Sword, Lance, Axe, Bow, Fire, Thunder, Wind, Light, Dark, Staff } //Debug
    public enum Weapon_Ranks { None, E, D, C, B, A, S }
    public enum Weapon_Traits { Thrown, Reaver, Brave, Cursed,
		Hits_All_in_Range, Ballista, Ignores_Pow, Drains_HP, Ignores_Def, Halves_HP }
    public enum Stave_Traits { Heals, Torch, Unlock, Repair, Barrier, Rescue, Warp }
    public enum Attack_Types { Physical, Magical, Magic_At_Range }
    public class Data_Weapon : Data_Equipment, IFEXNADataContent
    {
        internal static IWeaponTypeService WeaponTypeData { get; private set; }
        public static IWeaponTypeService weapon_type_data
        {
            get { return WeaponTypeData; }
            set
            {
                if (WeaponTypeData == null)
                    WeaponTypeData = value;
            }
        }

        public readonly static int[] WLVL_THRESHOLDS = new int[] { 0, 1, 31, 71, 121, 181, 251 };
        public readonly static string[] WLVL_LETTERS = new string[] { "-", "E", "D", "C", "B", "A", "S" };
        //public readonly static int[] ANIMA_TYPES = { (int)Weapon_Types.Fire, (int)Weapon_Types.Thunder, (int)Weapon_Types.Wind }; //Debug
        public readonly static int[] ANIMA_TYPES = { 5, 6, 7 };

        public int Mgt = 0;
        public int Hit = 0;
        public int Crt = 0;
        public int Wgt = 0;
        public int Min_Range = 1;
        public int Max_Range = 1;
        public bool Mag_Range = false;
        public bool No_Counter = false;
        public bool Long_Range = false;
        public int Main_Type = 1; //public Weapon_Types Main_Type = Weapon_Types.Sword; //Debug
        public int Scnd_Type = 0;  //public Weapon_Types Scnd_Type = Weapon_Types.None; //Debug
        public Weapon_Ranks Rank = Weapon_Ranks.E;
        public Attack_Types Attack_Type = Attack_Types.Physical;
        public int WExp = 1;
        public int Staff_Exp = 0;
        public bool[] Traits = new bool[] { false, false, false, false, false, false, false, false, false, false };
        public bool[] Staff_Traits = new bool[] { false, false, false, false, false, false, false };
        public int[] Effectiveness;

        #region Serialization
        public IFEXNADataContent Read_Content(ContentReader input)
        {
            return read(input);
        }
        public static Data_Weapon read(BinaryReader reader)
        {
            Data_Weapon result = new Data_Weapon();
            result.read_equipment(reader);
            
            result.Mgt = reader.ReadInt32();
            result.Hit = reader.ReadInt32();
            result.Crt = reader.ReadInt32();
            result.Wgt = reader.ReadInt32();
            result.Min_Range = reader.ReadInt32();
            result.Max_Range = reader.ReadInt32();
            result.Mag_Range = reader.ReadBoolean();
            result.No_Counter = reader.ReadBoolean();
            result.Long_Range = reader.ReadBoolean();
            result.Main_Type = reader.ReadInt32();
            result.Scnd_Type = reader.ReadInt32();
            result.Rank = (Weapon_Ranks)reader.ReadInt32();
            result.Attack_Type = (Attack_Types)reader.ReadInt32();
            result.WExp = reader.ReadInt32();
            result.Staff_Exp = reader.ReadInt32();
            result.Traits = result.Traits.read(reader);
            result.Staff_Traits = result.Staff_Traits.read(reader);
            result.Effectiveness = result.Effectiveness.read(reader);

            return result;
        }

        public void Write(BinaryWriter output)
        {
            base.write(output);

            output.Write(Mgt);
            output.Write(Hit);
            output.Write(Crt);
            output.Write(Wgt);
            output.Write(Min_Range);
            output.Write(Max_Range);
            output.Write(Mag_Range);
            output.Write(No_Counter);
            output.Write(Long_Range);
            output.Write(Main_Type);
            output.Write(Scnd_Type);
            output.Write((int)Rank);
            output.Write((int)Attack_Type);
            output.Write(WExp);
            output.Write(Staff_Exp);
            Traits.write(output);
            Staff_Traits.write(output);
            Effectiveness.write(output);

        }
        #endregion

        public override string ToString()
        {
            return ToString(0);
        }
        public override string ToString(int uses_left)
        {
            return String.Format("Weapon: {0}, Mgt {1}, Uses {2}",
                full_name(), Mgt, uses_left == 0 ? Uses.ToString() : string.Format("{0}/{1}", uses_left, Uses));
        }

        public Data_Weapon() : this(11) { }
        public Data_Weapon(int effectiveness_count)
        {
            Effectiveness = new int[effectiveness_count];
            for (int i = 0; i < effectiveness_count; i++)
                Effectiveness[i] = 1;
        }
        public Data_Weapon(Data_Weapon weapon)
        {
            copy_traits(weapon);

            Mgt = weapon.Mgt;
            Hit = weapon.Hit;
            Crt = weapon.Crt;
            Wgt = weapon.Wgt;
            Min_Range = weapon.Min_Range;
            Max_Range = weapon.Max_Range;
            Mag_Range = weapon.Mag_Range;
            No_Counter = weapon.No_Counter;
            Long_Range = weapon.Long_Range;
            Main_Type = weapon.Main_Type;
            Scnd_Type = weapon.Scnd_Type;
            Rank = weapon.Rank;
            Attack_Type = weapon.Attack_Type;
            WExp = weapon.WExp;
            Staff_Exp = weapon.Staff_Exp;
            Traits = new bool[weapon.Traits.Length];
            Array.Copy(weapon.Traits, Traits, Traits.Length);
            Staff_Traits = new bool[weapon.Staff_Traits.Length];
            Array.Copy(weapon.Staff_Traits, Staff_Traits, Staff_Traits.Length);
            Effectiveness = new int[weapon.Effectiveness.Length];
            Array.Copy(weapon.Effectiveness, Effectiveness, Effectiveness.Length);
        }

        public override bool is_weapon { get { return true; } }

        public string type { get { return main_type().StatusHelpName; } }

        public string rank
        {
            get
            {
                if ((int)Rank == 0) return "Prf";
                return WLVL_LETTERS[(int)Rank];
            }
        }

        public bool is_staff()
        {
            return main_type().IsStaff;
        }

        public bool is_attack_staff()
        {
            return is_staff() && Attack_Type == Attack_Types.Magical;
        }

        public bool is_magic()
        {
            return main_type().IsMagic || scnd_type().IsMagic;
        }

        public bool is_always_magic()
        {
            return is_magic() && Attack_Type == Attack_Types.Magical;
        }

        public bool is_ranged_magic()
        {
            return is_magic() && Attack_Type == Attack_Types.Magic_At_Range;
        }

        public bool is_imbued()
        {
            return !main_type().IsMagic || scnd_type().IsMagic;
            //int num = PHYSICAL_TYPES; //Debug
            //return ((int)Main_Type <= num && (int)Scnd_Type > num);
        }

        public bool blocked_by_silence
        {
            get
            {
                return main_type().IsMagic || main_type().IsStaff || is_always_magic();
            }
        }

        public bool imbue_range_reduced_by_silence
        {
            get
            {
                if (blocked_by_silence)
                    return false;
                return is_ranged_magic();
            }
        }

        public WeaponType main_type()
        {
            if (WeaponTypeData != null)
                return WeaponTypeData.type(Main_Type);
            return null;
        }
        public WeaponType scnd_type()
        {
            if (WeaponTypeData != null)
                return WeaponTypeData.type(Scnd_Type);
            return null;
        }

        public int HitsPerAttack
        {
            get
            {
                if (Brave())
                    return 2;
                return 1;
            }
        }

        #region Traits
        public bool Thrown() { return Traits[(int)Weapon_Traits.Thrown]; }
        public bool Reaver() { return Traits[(int)Weapon_Traits.Reaver]; }
        public bool Brave() { return Traits[(int)Weapon_Traits.Brave]; }
        public bool Cursed() { return Traits[(int)Weapon_Traits.Cursed]; }
        public bool Hits_All_in_Range() { return Traits[(int)Weapon_Traits.Hits_All_in_Range]; }
        public bool Ballista() { return Traits[(int)Weapon_Traits.Ballista]; }
        public bool Ignores_Pow() { return Traits[(int)Weapon_Traits.Ignores_Pow]; }
        public bool Drains_HP() { return Traits[(int)Weapon_Traits.Drains_HP]; }
        public bool Ignores_Def() { return Traits[(int)Weapon_Traits.Ignores_Def]; }
        public bool Halves_HP() { return Traits[(int)Weapon_Traits.Halves_HP]; }
        #endregion

        #region Staff_Traits
        public bool Heals() { return Staff_Traits[(int)Stave_Traits.Heals]; }
        public bool Torch() { return Staff_Traits[(int)Stave_Traits.Torch]; }
        public bool Unlock() { return Staff_Traits[(int)Stave_Traits.Unlock]; }
        public bool Repair() { return Staff_Traits[(int)Stave_Traits.Repair]; }
        public bool Barrier() { return Staff_Traits[(int)Stave_Traits.Barrier]; }
        public bool Rescue() { return Staff_Traits[(int)Stave_Traits.Rescue]; }
        public bool Warp() { return Staff_Traits[(int)Stave_Traits.Warp]; }
        #endregion
    }

    public interface IWeaponTypeService
    {
        WeaponType type(int key);
    }
}
