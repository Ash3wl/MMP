using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DictionaryExtension;
using ListExtension;

namespace FEXNA_Library
{
    public class Map_Unit_Data
    {
        public Dictionary<Vector2, Data_Unit> Units = new Dictionary<Vector2, Data_Unit> ();
        public List<Data_Unit> Reinforcements = new List<Data_Unit>();

        #region Serialization
        public void write(BinaryWriter writer)
        {
            Units.write(writer);
            Reinforcements.write(writer);
        }

        public static Map_Unit_Data read(BinaryReader reader)
        {
            Map_Unit_Data unit_data = new Map_Unit_Data();
            unit_data.Units.read(reader);
            unit_data.Reinforcements.read(reader);
            return unit_data;
        }
        #endregion

        public override string ToString()
        {
            return string.Format("Map Unit Data: ", Units.Count, Reinforcements.Count);
        }
    }

    public struct Data_Unit
    {
        public string type;
        public string identifier;
        public string data;

        public Data_Unit(string type, string identifier, string data)
        {
            this.type = type;
            this.identifier = identifier;
            this.data = data;
        }

        #region Serialization
        public static Data_Unit read(BinaryReader reader)
        {
            return new Data_Unit(reader.ReadString(), reader.ReadString(), reader.ReadString());
        }

        public void write(BinaryWriter writer)
        {
            writer.Write(type);
            writer.Write(identifier);
            writer.Write(data);
        }
        #endregion

        public void reset(Data_Unit unit)
        {
            type = unit.type;
            identifier = unit.identifier;
            data = unit.data;
        }
    }
}
