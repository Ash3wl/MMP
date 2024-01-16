using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using FEXNA.Graphics.Text;
using FEXNA_Library;

namespace FEXNA.Windows.Command.Items
{
    class Window_Command_Item_Attack : Window_Command_Item
    {
        protected string Skill = "";

        #region Accessors
        public string skill { get { return Skill; } }
        #endregion

        public Window_Command_Item_Attack(int unit_id, Vector2 loc)
            : this(unit_id, loc, "") { }
        public Window_Command_Item_Attack(int unit_id, Vector2 loc, string skill)
        {
            WIDTH = 136;
            Unit_Id = unit_id;
            Skill = skill;
            initialize(loc, WIDTH, new List<string>());
        }

        protected override List<Item_Data> get_equipment()
        {
            return unit.items;
        }

        protected override bool is_valid_item(List<Item_Data> items, int i)
        {
            var item_data = items[i];
            if (item_data.non_equipment || !item_data.is_weapon)
                return false;

            Data_Weapon weapon = item_data.to_weapon;
            if (unit.actor.is_equippable(weapon) && !weapon.is_staff())
            {
                return unit.enemies_in_range(i, Skill)[0].Any();
            }
            return false;
        }

        protected override void equip_actor() { }

        protected override bool show_equipped()
        {
            return false;
        }
    }
}
