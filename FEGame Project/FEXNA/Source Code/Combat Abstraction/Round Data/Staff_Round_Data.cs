using System.Collections.Generic;

namespace FEXNA
{
    class Staff_Round_Data : Combat_Round_Data
    {
        private Staff_Modes StaffMode;

        public Staff_Round_Data(
            Game_Unit battler_1, Game_Unit battler_2, int distance, Staff_Modes mode)
        {
            List<int?> ary = Combat.combat_stats(battler_1.id, battler_2 == null ? null : (int?)battler_2.id, distance);
            Stats = ary;
            StaffMode = mode;
        }

        protected override Attack_Result calculate_attack(Game_Unit battler, Game_Unit target, int distance, int dmg, bool hit, bool crt,
            FEXNA_Library.Data_Weapon weapon)
        {
            // Healing, Status Healing, Barrier, Positive Status
            if (StaffMode == Staff_Modes.Heal)
                return Combat.set_heal(battler, target, distance, weapon);
            // Status infliction
            else if (StaffMode == Staff_Modes.Status_Inflict)
                return Combat.set_status_staff(battler, target, distance, hit, weapon);
            // Flare
            else if (StaffMode == Staff_Modes.Torch)
                return Combat.set_torch(battler, weapon);
            else
                return new Attack_Result { state_change = new List<KeyValuePair<int, bool>>() }; // Additional results add on after here //Yeti
        }

        public override bool is_successful_hit(FEXNA_Library.Data_Weapon weapon)
        {
            if (weapon.Barrier())
                return true;
            // If the attacker didn't hit themself (how does this happen with staves),
            // returns true when damage or status effects happened
            return !Result.backfire && (Result.dmg != 0 || Result.state_change.Count > 0);
        }
    }
}
