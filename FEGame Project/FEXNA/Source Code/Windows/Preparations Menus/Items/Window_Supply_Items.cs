using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Windows;
using FEXNA.Windows.Command;
using FEXNA_Library;

namespace FEXNA
{
    class Window_Supply_Items : Stereoscopic_Graphic_Object, ISelectionMenu
    {
        const int ROWS = 8;
        const int WIDTH = 144;
        const int TYPE_CHANGE_TIME = 8;

        protected int Actor_Id;
        protected List<int> Index = new List<int>();
        protected List<int> Scroll = new List<int>();
        protected List<int> Index_Redirect = new List<int>();
        protected int Page_Change_Time = 0;
        protected bool Page_Change_Right;
        protected bool Active = false;
        protected float PageOffset;
        protected Window_Command_Supply CommandWindow;
        protected List<int> SupplyWeaponTypes;
        protected List<Weapon_Type_Icon> Type_Icons = new List<Weapon_Type_Icon>();
        protected Window_Help Help_Window;
        protected Page_Arrow Left_Arrow, Right_Arrow;
        protected bool Wants_Help_Open = false;
        protected bool Manual_Cursor_Draw = false;
        protected Maybe<float> Arrow_Stereo_Offset = new Maybe<float>(), Help_Stereo_Offset = new Maybe<float>();

        #region Accessors
        protected int type
        {
            get { return Global.game_system.Supply_Item_Type; }
            set { Global.game_system.Supply_Item_Type = value; }
        }

        protected Game_Actor actor { get { return Global.game_actors[Actor_Id]; } }

        protected int index
        {
            get { return Index[type]; }
            set { Index[type] = value; }
        }
        internal int scroll
        {
            get { return Scroll[type]; }
            private set { Scroll[type] = value; }
        }

        public int redirect { get { return Index_Redirect[index]; } }

        public bool active
        {
            set
            {
                Active = value;
                if (!Active)
                    Wants_Help_Open = false;
            }
        }

        public bool can_take { get { return Index_Redirect.Count > 0; } }

        public bool ready { get { return Page_Change_Time == 0; } }

        public Vector2 current_cursor_loc
        {
            get { return CommandWindow.current_cursor_loc; }
            set { CommandWindow.current_cursor_loc = value; }
        }

        public bool manual_cursor_draw { set { Manual_Cursor_Draw = value; } }

        public float arrow_stereoscopic { set { Arrow_Stereo_Offset = value; } }
        public float help_stereoscopic { set { Help_Stereo_Offset = value; } }
        #endregion

        public Window_Supply_Items(int actor_id, Vector2 loc)
        {
            Actor_Id = actor_id;
            get_supply_types();
            for (int i = 0; i < SupplyWeaponTypes.Count; i++)
            {
                Index.Add(0);
                Scroll.Add(0);
            }
            this.loc = loc;
            initialize_sprites();
        }

        protected void get_supply_types()
        {
            SupplyWeaponTypes = new List<int>();
            for (int i = 1; i < Global.weapon_types.Count; i++)
            {
                if (Global.weapon_types[i].DisplayedInStatus)
                    SupplyWeaponTypes.Add(i);
            }
            // Add none type for items and catch-all
            SupplyWeaponTypes.Add(0);
        }

        protected void initialize_sprites()
        {
            CommandWindow = new Window_Command_Supply(
                this.loc + new Vector2(0, 8), WIDTH, ROWS);
            CommandWindow.glow = true;
            CommandWindow.glow_width = WIDTH - 24;
            CommandWindow.manual_cursor_draw = true;
            // Weapon Type Icons
            for (int i = 0; i < SupplyWeaponTypes.Count; i++)
            {
                Type_Icons.Add(new Weapon_Type_Icon());
                Type_Icons[i].loc = new Vector2(i * 12 + 4, 0);
                Type_Icons[i].index = Global.weapon_types[SupplyWeaponTypes[i]].IconIndex;
            }
            // Arrows
            Left_Arrow = new Page_Arrow();
            Left_Arrow.loc = new Vector2(0, 0);
            Left_Arrow.ArrowClicked += Left_Arrow_ArrowClicked;
            Right_Arrow = new Page_Arrow();
            Right_Arrow.loc = new Vector2(144, 0);
            Right_Arrow.mirrored = true;
            Right_Arrow.ArrowClicked += Right_Arrow_ArrowClicked;

            refresh(true);
        }

        public void refresh(bool correctToScrollRange = false)
        {
            Global.game_battalions.sort_convoy(Global.battalion.convoy_id);
            switch (Constants.Gameplay.CONVOY_ITEMS_STACK)
            {
                case Convoy_Stack_Types.Full:
                    refresh_stacked_items();
                    break;
                case Convoy_Stack_Types.Use:
                    refresh_stacked_items(true);
                    break;
                case Convoy_Stack_Types.None:
                    refresh_items();
                    break;
            }

            CommandWindow.immediate_index =  this.index;
            this.index = CommandWindow.index;
            CommandWindow.scroll = this.scroll;
            CommandWindow.refresh_scroll(correctToScrollRange);

            // Weapon Type Icons
            for (int i = 0; i < Type_Icons.Count; i++)
            {
                int alpha = type == i ? 255 : 160;
                Type_Icons[i].tint = new Color(alpha, alpha, alpha, 255);
            }
            if (is_help_active)
            {
                if (Index_Redirect.Count == 0)
                {
                    close_help();
                    Wants_Help_Open = true;
                }
            }
            refresh_loc();
        }
        protected void refresh_items()
        {
            Index_Redirect.Clear();
            for (int i = 0; i < Global.game_battalions.active_convoy_data.Count; i++)
            {
                if (is_item_add_valid(
                        type, Global.game_battalions.active_convoy_data[i]))
                    Index_Redirect.Add(i);
            }

            List<Status_Item> items = new List<Status_Item>();
            for (int i = 0; i < Index_Redirect.Count; i++)
            {
                var item = new Status_Item();
                item.set_image(
                    actor, Global.game_battalions.active_convoy_data[Index_Redirect[i]]);
                //if (Global.game_battalions.active_convoy_data[Index_Redirect[i]].is_weapon && //Yeti
                //        !actor.is_equippable(Global.data_weapons[Global.game_battalions.active_convoy_data[Index_Redirect[i]].Id]))
                //    Items[Items.Count - 1].change_text_color("Grey");

                items.Add(item);
            }

            if (items.Count == 0)
                items.Add(new ConvoyItemNothing());

            CommandWindow.refresh_items(items);
        }
        protected void refresh_stacked_items(bool same_uses = false)
        {
            List<int> index_redirect = new List<int>();
            Index_Redirect.Clear();
            for (int i = 0; i < Global.game_battalions.active_convoy_data.Count; i++)
            {
                if (is_item_add_valid(
                        type, Global.game_battalions.active_convoy_data[i]))
                    index_redirect.Add(i);
            }

            var items = new List<Status_Item>();
            int item_count;
            // Goes through all item data and finds any the match
            Item_Data item_data;
            for (int i = 0; i < index_redirect.Count; i++)
            {
                if (same_uses)
                    item_data = Global.game_battalions.active_convoy_data[index_redirect[i]];
                else
                    item_data = new Item_Data(Global.game_battalions.active_convoy_data[index_redirect[i]].Type,
                        Global.game_battalions.active_convoy_data[index_redirect[i]].Id);
                Index_Redirect.Add(index_redirect[i]);
                item_count = 1;
                // Checks the following items after the current one, and adds one to the count for each with the same id
                while ((i + 1) < index_redirect.Count && item_data.Id ==
                    Global.game_battalions.active_convoy_data[index_redirect[i + 1]].Id &&
                        // If caring abotu uses, the uses must also match
                        (!same_uses || (item_data.Uses ==
                        Global.game_battalions.active_convoy_data[index_redirect[i + 1]].Uses)))
                    {
                        item_count++;
                        i++;
                    }

                Convoy_Item item_listing = new Convoy_Item();
                item_listing.set_image(actor, item_data, item_count);
                //// If the item is a weapon and can't be equipped, color it grey // This should already be handled though //Yeti
                //if (item_data.is_weapon && !actor.is_equippable(Global.data_weapons[item_data.Id]))
                //    item_listing.change_text_color("Grey");

                items.Add(item_listing);
            }

            if (items.Count == 0)
                items.Add(new ConvoyItemNothing());

            CommandWindow.refresh_items(items);
        }

        /// <summary>
        /// Returns true if the item matches the supply type given
        /// </summary>
        /// <param name="item_data">Index of supply type to check against</param>
        /// <param name="item_data">Item data to test</param>
        private bool is_item_add_valid(int typeIndex, Item_Data item_data)
        {
            var supply_type = Global.weapon_types[SupplyWeaponTypes[typeIndex]];

            bool is_weapon = item_data.is_weapon;
            WeaponType weapon_type =
                !is_weapon ? Global.weapon_types[0] : item_data.to_weapon.main_type();

            if (supply_type == Global.weapon_types[0])
                return !is_weapon || !weapon_type.DisplayedInStatus;
            else
                return supply_type == weapon_type;
        }

        protected Vector2 cursor_loc()
        {
            return new Vector2(-4, index * 16 - scroll * 16 + 16);
        }

        public void jump_to(Item_Data item_data)
        {
            // Jump to the correct page
            if (!item_data.is_weapon)
                type = SupplyWeaponTypes.IndexOf(0);
            else
            {
                if (Global.data_weapons[item_data.Id].main_type().DisplayedInStatus)
                    type = SupplyWeaponTypes.IndexOf(
                        (int)Global.data_weapons[item_data.Id].main_type().Key);
                else
                    type = SupplyWeaponTypes.IndexOf(0);
            }
            refresh(true);
            // Jump to the actual item
            for(int i = 0; i < Index_Redirect.Count; i++)
            {
                if (item_data.same_item(Global.game_battalions.active_convoy_data[Index_Redirect[i]]))
                    if (Constants.Gameplay.CONVOY_ITEMS_STACK == Convoy_Stack_Types.Full ||
                        item_data.Uses == Global.game_battalions.active_convoy_data[Index_Redirect[i]].Uses)
                    {
                        this.index = i;
                        CommandWindow.immediate_index = this.index;
                        CommandWindow.refresh_scroll();

                        refresh_loc();
                        break;
                    }
            }
        }

        public Maybe<int> selected_index()
        {
            return CommandWindow.selected_index();
        }

        public bool getting_help()
        {
            return CommandWindow.getting_help();
        }

        public bool is_selected()
        {
            return CommandWindow.is_selected();
        }

        public bool is_canceled()
        {
            return CommandWindow.is_canceled() ||
                (is_help_active && CommandWindow.getting_help());
        }

        public void reset_selected() { }

        private void Left_Arrow_ArrowClicked(object sender, EventArgs e)
        {
            Global.Audio.play_se("System Sounds", "Menu_Move2");
            move_left();
        }
        private void Right_Arrow_ArrowClicked(object sender, EventArgs e)
        {
            Global.Audio.play_se("System Sounds", "Menu_Move2");
            move_right();
        }

        #region Update
        public void update()
        {
            CommandWindow.update(Active && ready);
            bool moved = this.index != CommandWindow.index;
            this.index = CommandWindow.index;
            this.scroll = CommandWindow.scroll;

            if (moved)
                refresh_loc();

            if (Active && ready)
                update_input();
            if (!ready)
                update_page_change();
            if (is_help_active)
                Help_Window.update();
            Left_Arrow.update();
            Right_Arrow.update();
        }

        protected void update_input()
        {
            Left_Arrow.update_input(-(this.loc + arrow_draw_vector()));
            Right_Arrow.update_input(-(this.loc + arrow_draw_vector()));

            // Change page
            if (Global.Input.repeated(Inputs.Left) ||
                Global.Input.gesture_triggered(TouchGestures.SwipeRight))
            {
                Global.Audio.play_se("System Sounds", "Menu_Move2");
                move_left();
            }
            else if (Global.Input.repeated(Inputs.Right) ||
                Global.Input.gesture_triggered(TouchGestures.SwipeLeft))
            {
                Global.Audio.play_se("System Sounds", "Menu_Move2");
                move_right();
            }
        }

        protected void update_page_change()
        {
            if (Page_Change_Time != TYPE_CHANGE_TIME)
            {
                if (Page_Change_Time == TYPE_CHANGE_TIME / 2)
                {
                    PageOffset = -PageOffset;
                    int num = SupplyWeaponTypes.Count;
                    type = (type + (Page_Change_Right ? 1 : -1) + num) % num;
                    refresh(true);
                    if (!is_help_active && Wants_Help_Open && Index_Redirect.Count > 0)
                    {
                        open_help();
                        Wants_Help_Open = false;
                    }
                }
                else
                    PageOffset += (Page_Change_Right ? 1 : -1) * (WIDTH / (int)Math.Pow(2,
                        Page_Change_Time > TYPE_CHANGE_TIME / 2 ?
                        Page_Change_Time - TYPE_CHANGE_TIME / 2 :
                        TYPE_CHANGE_TIME / 2 - Page_Change_Time));
            }
            Page_Change_Time--;
            if (Page_Change_Time == 0)
                PageOffset = 0;
            CommandWindow.PageOffset = -PageOffset;
        }
        #endregion

        #region Movement
        protected void move_left()
        {
            Page_Change_Time = TYPE_CHANGE_TIME;
            Page_Change_Right = false;
        }
        protected void move_right()
        {
            Page_Change_Time = TYPE_CHANGE_TIME;
            Page_Change_Right = true;
        }

        protected void refresh_loc()
        {
            if (is_help_active)
            {
                Help_Window.set_item(Global.game_battalions.active_convoy_data[redirect], actor);
                update_help_loc();
            }
        }
        #endregion

        #region Help
        public bool is_help_active { get { return Help_Window != null; } }

        public void open_help()
        {
            if (Index_Redirect.Count == 0)
            {
                Global.game_system.play_se(System_Sounds.Buzzer);
            }
            else
            {
                Help_Window = new Window_Help();
                Help_Window.set_screen_bottom_adjustment(-16);
                Help_Window.set_item(Global.game_battalions.active_convoy_data[redirect], actor);
                Help_Window.loc = loc + new Vector2(0, 8 + (index - scroll) * 16);
                if (Help_Stereo_Offset.IsSomething)
                Help_Window.stereoscopic = Help_Stereo_Offset; //Debug
                update_help_loc();
                Global.game_system.play_se(System_Sounds.Help_Open);
            }
        }

        public virtual void close_help()
        {
            Help_Window = null;
            Global.game_system.play_se(System_Sounds.Help_Close);
        }

        protected virtual void update_help_loc()
        {
            Help_Window.set_loc(loc + new Vector2(0, 16 + (index - scroll) * 16));
        }
        #endregion

        protected Vector2 arrow_draw_vector()
        {
            return draw_offset + graphic_draw_offset(Arrow_Stereo_Offset);
        }

        #region Draw
        public void draw(SpriteBatch sprite_batch)
        {
            Vector2 loc = this.loc + draw_vector();
            // Window
            CommandWindow.draw(sprite_batch);

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            for (int i = Type_Icons.Count - 1; i >= 0; i--)
                if (i != type)
                    Type_Icons[i].draw(sprite_batch, -loc);
            Type_Icons[type].draw(sprite_batch, -loc);
            Left_Arrow.draw(sprite_batch, -(this.loc + arrow_draw_vector()));
            Right_Arrow.draw(sprite_batch, -(this.loc + arrow_draw_vector()));
            if (!Manual_Cursor_Draw)
                draw_cursor(sprite_batch);
            sprite_batch.End();
        }

        public void draw_help(SpriteBatch spriteBatch)
        {
            if (is_help_active)
                Help_Window.draw(spriteBatch);
        }

        public void draw_cursor(SpriteBatch sprite_batch)
        {
            if (Active)
                CommandWindow.draw_cursor(sprite_batch);
        }
        #endregion
    }
}
