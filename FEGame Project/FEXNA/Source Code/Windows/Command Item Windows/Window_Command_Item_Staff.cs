using System.Collections.Generic;
using Microsoft.Xna.Framework;
using FEXNA_Library;

namespace FEXNA.Windows.Command.Items
{
    class Window_Command_Item_Staff : Window_Command_Item_Attack
    {
        public Window_Command_Item_Staff(int unit_id, Vector2 loc)
            : base(unit_id, loc, "") { }

        protected override bool is_valid_item(List<Item_Data> items, int i)
        {
            var item_data = items[i];
            if (item_data.non_equipment || !item_data.is_weapon)
                return false;

            Data_Weapon weapon = item_data.to_weapon;
            if (unit.actor.is_equippable(weapon) && weapon.is_staff())
            {
                if (unit.allies_in_staff_range(new HashSet<Vector2> { unit.loc }, i)[0].Count > 0)
                    return true;
                else if (unit.enemies_in_staff_range(new HashSet<Vector2> { unit.loc }, i)[0].Count > 0)
                    return true;
                else if (unit.untargeted_staff_range(i)[1].Count > 0)
                    return true;
            }
            return false;
        }
    }
}
