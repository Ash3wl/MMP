﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA.Windows.Command
{
    class Window_Item_Options : Window_Command
    {
        const int WIDTH = 48;
        List<int> Index_Redirect = new List<int>();
        
        public bool unequip;
        protected bool Can_Discard;

        #region Accessors
        public bool can_discard { get { return Can_Discard; } }

        public int redirect { get { return Index_Redirect[index]; } }
        #endregion

        public Window_Item_Options(Game_Unit unit, bool can_trade, Vector2 loc, int item_index, bool equipped)
        {
            List<string> strs = new List<string>();
            FEXNA_Library.Item_Data item_data = unit.actor.items[item_index];
            // Weapon
            if (item_data.is_weapon)
            {
                if (unit.actor.has_staves_only() || !Global.data_weapons[item_data.Id].is_staff())
                {
                    unequip = Constants.Actor.ALLOW_UNEQUIP && equipped;
                    if (unequip)
                        strs.Add("Unequip");
                    else
                        strs.Add("Equip");
                    Index_Redirect.Add(0);
                }
                if (Combat.can_use_item(unit, item_data.Id, item_data.is_weapon)) //Debug
                {
                    strs.Add("Use");
                    Index_Redirect.Add(1);
                }
            }
            // Item
            else
            {
                strs.Add("Use");
                Index_Redirect.Add(1);
            }
            if (can_trade)
            {
                strs.Add("Trade");
                Index_Redirect.Add(2);
            }
            strs.Add("Discard");
            Index_Redirect.Add(3);
            Can_Discard = item_data.to_equipment.Can_Sell;
            initialize(loc, WIDTH, strs);
            Window_Img.small = true;
            color_text(unit, item_data, strs);
        }

        protected void color_text(Game_Unit unit, FEXNA_Library.Item_Data item_data, List<string> strs)
        {
            for (int i = 0; i < num_items(); i++)
            {
                string color = "Green";
                // Equip
                if (Index_Redirect[i] == 0 &&
                    !unit.actor.is_equippable(Global.data_weapons[item_data.Id]))
                    color = "Grey";
                // Use
                else if (Index_Redirect[i] == 1)
                    if (Combat.can_use_item(unit, item_data.Id, item_data.is_weapon))
                        color = "White";
                    else
                        color = "Grey";
                // Discard
                else if (Index_Redirect[i] == 3 && !Can_Discard)
                    color = "Grey";

                Items[i].set_text_color(color);
            }
        }
    }
}
