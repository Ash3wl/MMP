using System;
using System.Collections.Generic;

namespace FEXNA
{
    class Arena_Combat_Round_Data : Combat_Round_Data
    {
        internal const int MINIMUM_HIT = 5;
        internal const int MINIMUM_DMG = 1;

        internal Arena_Combat_Round_Data(Game_Unit battler_1, Game_Unit battler_2, int distance)
        {
            List<int?> ary = Combat.combat_stats(battler_1.id, battler_2 == null ? null : (int?)battler_2.id, distance);
            ary[0] = attack_hit((int)ary[0], (int)ary[0], false);
            ary[1] = attack_dmg((int)ary[1], (int)ary[1]);
            ary[4] = attack_hit((int)ary[4], (int)ary[4], false);
            ary[5] = attack_dmg((int)ary[5], (int)ary[5]);
            Stats = ary;
        }

        protected override int attack_hit(int original_hit, int current_hit, bool hit_skill_changed)
        {
            if (original_hit > 0 && current_hit <= 0)
                return 0;
            return Math.Max(current_hit, MINIMUM_HIT);
        }
        protected override int attack_dmg(int original_dmg, int current_dmg)
        {
            if (original_dmg > 0 && current_dmg <= 0)
                return 0;
            return Math.Max(current_dmg, MINIMUM_DMG);
        }

        public override void end_battle(Game_Unit battler_1, Combat_Map_Object battler_2, int distance, List<Combat_Action_Data> action_data)
        {
            action_data.Add(new Combat_Action_Data
            {
                Battler_Index = 0,
                Trigger = (int)Combat_Action_Triggers.End
            });
            List<int?> ary = Combat.combat_stats(battler_1.id, battler_2 == null ? null : (int?)battler_2.id, distance);
            ary[0] = attack_hit((int)ary[0], (int)ary[0], false);
            ary[1] = attack_dmg((int)ary[1], (int)ary[1]);
            ary[4] = attack_hit((int)ary[4], (int)ary[4], false);
            ary[5] = attack_dmg((int)ary[5], (int)ary[5]);
            action_data[action_data.Count - 1].Hit1 = ary[0];
            action_data[action_data.Count - 1].Dmg1 = ary[1];
            action_data[action_data.Count - 1].Crt1 = ary[2];
            action_data[action_data.Count - 1].Skl1 = ary[3];
            action_data[action_data.Count - 1].Hit2 = ary[4];
            action_data[action_data.Count - 1].Dmg2 = ary[5];
            action_data[action_data.Count - 1].Crt2 = ary[6];
            action_data[action_data.Count - 1].Skl2 = ary[7];
        }
    }
}
