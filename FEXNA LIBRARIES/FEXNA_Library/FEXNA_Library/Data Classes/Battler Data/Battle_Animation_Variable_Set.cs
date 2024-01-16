using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnumExtension;
using ListExtension;

namespace FEXNA_Library.Battler
{
    public class Battle_Animation_Variable_Set
    {
        public AttackAnims AnimDefinition = AttackAnims.None;
        public List<int> Animation = new List<int>();

        #region Serialization
        public static Battle_Animation_Variable_Set Read(BinaryReader input)
        {
            Battle_Animation_Variable_Set result = new Battle_Animation_Variable_Set();

            result.AnimDefinition = (AttackAnims)input.ReadInt16();
            result.Animation.read(input);

            return result;
        }

        public void Write(BinaryWriter output)
        {
            output.Write((short)AnimDefinition);
            Animation.write(output);
        }
        #endregion

        public Battle_Animation_Variable_Set() { }
        public Battle_Animation_Variable_Set(Battle_Animation_Variable_Set other)
        {
            AnimDefinition = other.AnimDefinition;
            Animation = new List<int>(other.Animation);
        }

        public override string ToString()
        {
            string key = "Default";
            if (AnimDefinition != AttackAnims.None)
            {
                List<string> flags = new List<string>();
                if (AnimDefinition.HasEnumFlag(AttackAnims.HitDefined))
                    flags.Add(AnimDefinition.HasEnumFlag(AttackAnims.Hit) ? "Hit" : "Miss");
                if (AnimDefinition.HasEnumFlag(AttackAnims.CritDefined))
                    flags.Add(AnimDefinition.HasEnumFlag(AttackAnims.Crit) ? "Crit" : "No-crit");
                if (AnimDefinition.HasEnumFlag(AttackAnims.DamageDefined))
                    flags.Add(AnimDefinition.HasEnumFlag(AttackAnims.CausedDamage) ? "Damage" : "No-dmg");
                if (AnimDefinition.HasEnumFlag(AttackAnims.SkillDefined))
                    flags.Add(AnimDefinition.HasEnumFlag(AttackAnims.SkillActive) ? "Skill" : "No-skill");
                key = string.Join(", ", flags);
            }

            return string.Format("{0}: {1}", key,
                !Animation.Any() ? "Nothing" : string.Join(", ", Animation));
        }

        internal bool test_attack_flag_match(AttackAnims anim,
            AttackAnims definition_flag, AttackAnims test_flag)
        {
            // If this animation doesn't care about this definition, return true
            if (!AnimDefinition.HasEnumFlag(definition_flag))
                return true;
            // Check if the animation data and the requested anim have the same flag
            return AnimDefinition.HasEnumFlag(test_flag) == anim.HasEnumFlag(test_flag);
        }

        internal bool compare(Battle_Animation_Variable_Set other)
        {
            if (AnimDefinition != other.AnimDefinition)
                return false;
            if (!Animation.compare(other.Animation))
                return false;

            return true;
        }
    }
}
