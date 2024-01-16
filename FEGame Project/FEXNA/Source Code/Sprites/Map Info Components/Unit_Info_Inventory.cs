﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA_Library;

namespace FEXNA
{
    class Unit_Info_Inventory : Stereoscopic_Graphic_Object
    {
        protected FE_Text Weapon_Name;
        protected List<Item_Icon_Sprite> Icons = new List<Item_Icon_Sprite>();

        public Unit_Info_Inventory()
        {
            Weapon_Name = new FE_Text();
            Weapon_Name.Font = "FE7_Text_Info";
            Weapon_Name.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Info");
        }

        protected virtual bool display_full_inventory(Game_Unit unit)
        {
            return !unit.is_active_player_team; //Multi
        }

        protected virtual bool scissor()
        {
            return false;
        }

        protected virtual bool equipped_first()
        {
            return false;
        }

        public void set_images(Game_Unit unit)
        {
            set_images(unit.actor, display_full_inventory(unit), unit.drops_item);
        }
        public void set_images(Game_Actor actor, bool full_inventory, bool drops_item)
        {
            Icons.Clear();
            if (full_inventory)
            {
                Weapon_Name.text = "";
                int equipped = -1;
                if (equipped_first() && actor.equipped != 0)
                {
                    equipped = actor.equipped - 1;
                    add_icon(actor, equipped, drops_item);
                }
                for (int i = 0; i < actor.items.Count; i++)
                {
                    if (i == equipped)
                        continue;
                    add_icon(actor, i, drops_item);
                }
            }
            else
            {
                if (actor.weapon == null)
                    Weapon_Name.text = "Unarmed";
                else
                {
                    Data_Weapon item = actor.weapon;
                    Weapon_Name.text = item.Name;
                    Icons.Add(new Item_Icon_Sprite());
                    if (Global.content_exists(@"Graphics/Icons/" + item.Image_Name))
                        Icons[Icons.Count - 1].texture = Global.Content.Load<Texture2D>(@"Graphics/Icons/" + item.Image_Name);
                    Icons[Icons.Count - 1].index = item.Image_Index;
                    Icons[Icons.Count - 1].loc = new Vector2(0, 0);
                    Icons[Icons.Count - 1].scissor = scissor();
                }
            }
        }

        protected void add_icon(Game_Actor actor, int i, bool drops_item)
        {
            Item_Data item_data = actor.items[i];
            if (item_data.Id > 0)
            {
                Data_Equipment item = item_data.to_equipment;
                Icons.Add(new Item_Icon_Sprite());
                if (Global.content_exists(@"Graphics/Icons/" + item.Image_Name))
                    Icons[Icons.Count - 1].texture = Global.Content.Load<Texture2D>(@"Graphics/Icons/" + item.Image_Name);
                Icons[Icons.Count - 1].index = item.Image_Index;
                Icons[Icons.Count - 1].loc = new Vector2((Icons.Count - 1) * 16, 0);
                Icons[Icons.Count - 1].flash_color = new Color(72, 232, 32, 255);
                Icons[Icons.Count - 1].flash_time_max = 120;
                //Icons[Icons.Count - 1].flash = (unit.drops_item && i == unit.actor.num_items - 1); //Debug
                Icons[Icons.Count - 1].flash = (drops_item && i == actor.num_items - 1);
                Icons[Icons.Count - 1].scissor = scissor();
            }
        }

        public void update()
        {
            foreach (Item_Icon_Sprite icon in Icons)
                icon.update();
        }

        public void draw(SpriteBatch sprite_batch)
        {
            draw(sprite_batch, Vector2.Zero);
        }
        public virtual void draw(SpriteBatch sprite_batch, Vector2 draw_offset)
        {
            draw(sprite_batch, Vector2.Zero, null);
        }
        public virtual void draw(SpriteBatch sprite_batch, Vector2 draw_offset, RasterizerState state)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, state);
            Weapon_Name.draw(sprite_batch, draw_offset - (loc + draw_vector() + new Vector2(16, 0) - offset));
            sprite_batch.End();
            foreach (Item_Icon_Sprite icon in Icons)
                icon.draw(sprite_batch, draw_offset - (loc + draw_vector() - offset));
        }
    }
}
