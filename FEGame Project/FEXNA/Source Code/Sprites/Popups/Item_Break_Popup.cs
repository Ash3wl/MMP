﻿using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;

namespace FEXNA
{
    class Item_Break_Popup : Popup
    {
        protected readonly static char[] VOWELS = new char[] { 'A', 'E', 'I', 'O', 'U' };

        protected Item_Icon_Sprite Icon;
        protected FE_Text A_Name, Broke;

        #region Accessors
        public int width { get { return Width; } }
        #endregion

        public Item_Break_Popup() { }
        public Item_Break_Popup(int item_id, bool is_item)
        {
            initialize(item_id, is_item, true);
        }
        public Item_Break_Popup(int item_id, bool is_item, bool battle_scene)
        {
            initialize(item_id, is_item, battle_scene);
        }

        protected virtual void initialize(int item_id, bool is_item, bool battle_scene)
        {
            Timer_Max = 97;
            FEXNA_Library.Data_Equipment item;
            if (is_item)
                item = Global.data_items[item_id];
            else
                item = Global.data_weapons[item_id];
            // Item icon
            set_icon(item);
            // Text
            set_text(item, battle_scene);
            set_window(battle_scene);
        }

        protected void set_window(bool battle_scene)
        {
            if (battle_scene)
                texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Combat_Popup");
            else
            {
                if (Global.game_system.preparations && !Global.game_system.Preparation_Events_Ready)
                {
                    Window = new WindowPanel(Global.Content.Load<Texture2D>(
                        @"Graphics/Windowskins/Preparations_Item_Options_Window"));
                    Window.width = Width - 8;
                    Window.height = 24;
                    Window.offset = new Vector2(-4, -4);
                }
                else
                {
                    Window = new System_Color_Window();
                    Window.width = Width;
                    Window.height = 32;
                }
            }
        }

        protected void set_icon(FEXNA_Library.Data_Equipment item)
        {
            Icon = new Item_Icon_Sprite();
            Icon.texture = Global.Content.Load<Texture2D>(@"Graphics/Icons/" + item.Image_Name);
            Icon.index = item.Image_Index;
        }

        protected virtual void set_text(FEXNA_Library.Data_Equipment item, bool battle_scene)
        {
            int x = battle_scene ? 23 : 8;
            A_Name = new FE_Text();
            A_Name.loc = new Vector2(x, 8);
            A_Name.Font = "FE7_Text";
            A_Name.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/" +
                (battle_scene ? "FE7_Text_CombatBlue" : "FE7_Text_Blue"));
            A_Name.text = VOWELS.Contains(item.Name[0]) ? "An " : "A ";
            A_Name.text += item.Name;
            x += Font_Data.text_width(A_Name.text, "FE7_Text");
            Icon.loc = new Vector2(battle_scene ? 7 : 1 + x, 8);
            if (!battle_scene)
                x += 15;
            Broke = new FE_Text();
            Broke.loc = new Vector2(x, 8);
            Broke.Font = "FE7_Text";
            Broke.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Broke.text = " broke!";
            if (item.Name == "Mimi-chan")
            {
                A_Name.text = item.Name;
                Broke.text = "left!";
            }
            x += Font_Data.text_width(Broke.text, "FE7_Text");
            Width = x + 8 + (x % 8 != 0 ? (8 - x % 8) : 0);
        }

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            if (visible)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (Window != null)
                    Window.draw(sprite_batch, -(loc + draw_vector()));
                else
                    draw_panel(sprite_batch, Width);
                sprite_batch.End();

                draw_image(sprite_batch);
            }
        }

        protected virtual void draw_image(SpriteBatch sprite_batch)
        {
            Icon.draw(sprite_batch, -(loc + draw_vector()));

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            A_Name.draw(sprite_batch, -(loc + draw_vector()));
            Broke.draw(sprite_batch, -(loc + draw_vector()));
            sprite_batch.End();
        }
    }
}
