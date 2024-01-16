using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using ListExtension;

namespace FEXNA_Library
{
    public class Data_Generic_Actor : IFEXNADataContent
    {
        public string Name;
        public string MiniFaceName = "";
        public string Description = "";
        public List<int> BaseStats = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 };
        public List<int> Growths = new List<int> { 0, 0, 0, 0, 0, 0, 0 };
        public List<int> Skills = new List<int>();

        #region Serialization
        public IFEXNADataContent Read_Content(ContentReader input)
        {
            Data_Generic_Actor result = new Data_Generic_Actor();

            result.Name = input.ReadString();
            result.MiniFaceName = input.ReadString();
            result.Description = input.ReadString();
            result.BaseStats.read(input);
            result.Growths.read(input);
            result.Skills.read(input);

            return result;
        }

        public void Write(BinaryWriter output)
        {
            output.Write(Name);
            output.Write(MiniFaceName);
            output.Write(Description);
            BaseStats.write(output);
            Growths.write(output);
            Skills.write(output);
        }
        #endregion

        public Data_Generic_Actor() { }
        public Data_Generic_Actor(Data_Generic_Actor other)
        {
            Name = other.Name;
            MiniFaceName = other.MiniFaceName;
            Description = other.Description;
            BaseStats = new List<int>(other.BaseStats);
            Growths = new List<int>(other.Growths);
            Skills = new List<int>(other.Skills);
        }

        public override string ToString()
        {
            return string.Format("Generic_Actor: {0}", Name);
        }
    }
}
