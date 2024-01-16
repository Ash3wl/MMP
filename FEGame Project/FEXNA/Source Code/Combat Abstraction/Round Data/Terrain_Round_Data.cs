using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FEXNA
{
    class Terrain_Round_Data : Combat_Round_Data
    {
        public Terrain_Round_Data(Game_Unit battler_1, Destroyable_Object battler_2, int distance)
        {
            List<int?> ary = Combat.combat_stats(battler_1.id, battler_2.id, distance);
            Stats = ary;
        }

        public void set_attack(Game_Unit battler_1, Destroyable_Object battler_2, int distance, List<Combat_Action_Data> action_data)
        {
            Game_Unit battler = battler_1;
            Destroyable_Object target = battler_2;
            FEXNA_Library.Data_Weapon weapon = battler.actor.weapon;
            // Hit test
            //bool[] hit_success = new bool[] { true, false }; //Debug
            //if (Stats[Attacker == 1 ? 0 : 4] != null) // Why even bother to roll RNs for this //Debug
            //    hit_success = Combat.test_hit(battler, target, distance);

            //bool hit = hit_success[0]; //Debug
            //bool crt = hit_success[1];
            List<int?> combat_stats = Combat.combat_stats(battler_1.id, battler_2.id, distance);
            int dmg = (int)combat_stats[1];
            bool hit = true;
            bool crt = false;
            battler.hit_skill_update();
            // Calculate attack
            Result = calculate_attack(battler, target, distance, dmg, hit, crt, weapon);
            // Reset skills
            battler.reset_skills();
        }
    }
}
