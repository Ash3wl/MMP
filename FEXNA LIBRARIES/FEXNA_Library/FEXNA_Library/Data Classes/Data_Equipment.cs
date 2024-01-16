using System;
using System.Collections.Generic;
using System.IO;
using ListExtension;

namespace FEXNA_Library
{
    public abstract class Data_Equipment
    {
        public int Id = 0;
        public string Name = "";
        public string Full_Name = "";
        public string Description = "";
        public string Quick_Desc = "";
        public string Image_Name = "";
        public int Image_Index = 0;
        public int Uses = 1;
        public int Cost = 1;
        public List<int> Prf_Character = new List<int>();
        public List<int> Prf_Class = new List<int>();
        public List<int> Prf_Type = new List<int>();
        public List<int> Skills = new List<int>();
        public List<int> Status_Inflict = new List<int>();
        public List<int> Status_Remove = new List<int>();
        public bool Can_Sell = true;

        #region Serialization
        protected void read_equipment(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            Name = reader.ReadString();
            Full_Name = reader.ReadString();
            Description = reader.ReadString();
            Quick_Desc = reader.ReadString();
            Image_Name = reader.ReadString();
            Image_Index = reader.ReadInt32();
            Uses = reader.ReadInt32();
            Cost = reader.ReadInt32();
            Prf_Character.read(reader);
            Prf_Class.read(reader);
            Prf_Type.read(reader);
            Skills.read(reader);
            Status_Inflict.read(reader);
            Status_Remove.read(reader);
            Can_Sell = reader.ReadBoolean();
        }

        public virtual void write(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Name);
            writer.Write(Full_Name);
            writer.Write(Description);
            writer.Write(Quick_Desc);
            writer.Write(Image_Name);
            writer.Write(Image_Index);
            writer.Write(Uses);
            writer.Write(Cost);
            Prf_Character.write(writer);
            Prf_Class.write(writer);
            Prf_Type.write(writer);
            Skills.write(writer);
            Status_Inflict.write(writer);
            Status_Remove.write(writer);
            writer.Write(Can_Sell);
        }
        #endregion

        public abstract string ToString(int uses_left);

        protected void copy_traits(Data_Equipment equipment)
        {
            Id = equipment.Id;
            Name = equipment.Name;
            Full_Name = equipment.Full_Name;
            Description = equipment.Description;
            Quick_Desc = equipment.Quick_Desc;
            Image_Name = equipment.Image_Name;
            Image_Index = equipment.Image_Index;
            Uses = equipment.Uses;
            Cost = equipment.Cost;
            Prf_Character = new List<int>(equipment.Prf_Character);
            Prf_Class = new List<int>(equipment.Prf_Class);
            Prf_Type = new List<int>(equipment.Prf_Type);
            Skills = new List<int>(equipment.Skills);
            Status_Inflict = new List<int>(equipment.Status_Inflict);
            Status_Remove = new List<int>(equipment.Status_Remove);
            Can_Sell = equipment.Can_Sell;
        }

        public string full_name()
        {
            if (Full_Name.Length > 0)
                return Full_Name;
            return Name;
        }

        public int full_price()
        {
            return Cost * Math.Max(1, Uses);
        }

        public virtual bool is_weapon { get { return false; } }

        public bool is_prf { get { return Prf_Character.Count > 0 || Prf_Class.Count > 0 || Prf_Type.Count > 0; } }

        public bool infinite_uses { get { return Uses == -1; } }
    }
}
