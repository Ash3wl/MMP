using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;
using FEXNA.Graphics.Preparations;
using FEXNA.Graphics.Text;
using FEXNA.Windows.Command;
using FEXNA.Windows.Command.Items;

namespace FEXNA
{
    enum Supply_Command_Window { All, Give, Restock, Take }
    class Window_Supply : Windows.Map.Map_Window_Base
    {
        const int BLACK_SCEEN_FADE_TIMER = 8;
        const int BLACK_SCREEN_HOLD_TIMER = 4;
        protected int Unit_Id;
        protected bool Giving = false, Taking = false, Restocking = false;
        protected bool Is_Actor = false;
        protected bool Traded = false;
        protected Window_Command Command_Window;
        protected Window_Command_Item_Supply Item_Window;
        protected Window_Supply_Items Supply_Window;
        protected Window_Command_Item_Convoy_Take Item_Selection_Window;
        protected Miniface Convoy_Face;
        protected Sprite Face_Bg, Banner, Stock_Banner;
        protected Button_Description R_Button;
        protected FE_Text Convoy_Label, Convoy_Text, Stock_Label, Stock_Value, Stock_Slash, Stock_Max;

        protected Prep_Items_Help_Footer HelpFooter;

        #region Accessors
        protected Game_Unit unit { get { return Global.game_map.units[Unit_Id]; } }
        protected Game_Actor actor { get { return Global.game_actors[unit.actor.id]; } }

        public bool giving { get { return Giving; } }
        public bool taking { get { return Taking; } }
        public bool restocking { get { return Restocking; } }
        /// <summary>
        /// Performing an supply action: giving, taking, or restocking.
        /// </summary>
        public bool trading { get { return Giving || Taking || Restocking; } }
        public bool selecting_take { get { return Item_Selection_Window != null; } }

        public bool traded { get { return Traded; } }

        public bool ready { get { return Supply_Window.ready && !Closing && Black_Screen_Timer <= 0; } }

        public bool can_give
        {
            get
            {
                return actor.num_items > 0 &&
                    Global.game_battalions.active_convoy_data.Count <
                    Constants.Gameplay.CONVOY_SIZE;
            }
        }
        public bool can_take
        {
            get
            {
                return actor.num_items < Constants.Actor.NUM_ITEMS &&
                    Global.game_battalions.active_convoy_data.Count > 0;
            }
        }
        public bool can_restock { get { return actor.num_items > 0 && Global.game_battalions.active_convoy_data.Count > 0; } }

        public bool is_help_active { get { return Item_Window.is_help_active || Supply_Window.is_help_active ||
            (Item_Selection_Window != null && Item_Selection_Window.is_help_active); } }

        private bool restock_blocked { get { return Global.scene.is_worldmap_scene; } }
        #endregion

        public Window_Supply(int actor_id)
        {
            Is_Actor = true;
            Global.game_map.add_actor_unit(Constants.Team.PLAYER_TEAM, Config.OFF_MAP,
                actor_id, "");
            Unit_Id = Global.game_map.last_added_unit.id;
            initialize_sprites();
            update_black_screen();
        }
        public Window_Supply(Game_Unit unit)
        {
            Unit_Id = unit.id;
            initialize_sprites();
            update_black_screen();
        }

        protected override void set_black_screen_time()
        {
            Black_Screen_Fade_Timer = BLACK_SCEEN_FADE_TIMER;
            Black_Screen_Hold_Timer = BLACK_SCREEN_HOLD_TIMER;
            base.set_black_screen_time();
        }

        protected void initialize_sprites()
        {
            // Black Screen
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Black_Screen.tint = new Color(0, 0, 0, 255);
            // Background
            Background = new Menu_Background();
            Background.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Preparation_Background");
            (Background as Menu_Background).vel = new Vector2(0, -1 / 3f);
            (Background as Menu_Background).tile = new Vector2(1, 2);
            Background.stereoscopic = Config.MAPMENU_BG_DEPTH;
            // Command Window
            create_command_window(Supply_Command_Window.All);
            // Supply Window
            Supply_Window = new Window_Supply_Items(
                actor.id, new Vector2(Config.WINDOW_WIDTH - 152, 24));
            Supply_Window.manual_cursor_draw = true;
            Supply_Window.stereoscopic = Config.CONVOY_SUPPLY_DEPTH;
            Supply_Window.arrow_stereoscopic = Config.CONVOY_ARROWS_DEPTH;
            Supply_Window.help_stereoscopic = Config.CONVOY_HELP_DEPTH;
            // //Yeti
            Banner = new Sprite();
            Banner.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Banner.loc = new Vector2(0, 8);
            Banner.draw_offset = new Vector2(Config.WINDOW_WIDTH / 2, 4);
            Banner.offset = new Vector2(16 / 2, 0);
            Banner.scale = new Vector2(
                (Config.WINDOW_WIDTH + Math.Abs(Config.CONVOY_BANNER_DEPTH) * 4) / 16f,
                40 / 16f);
            Banner.tint = new Color(0, 0, 0, 128);
            Banner.stereoscopic = Config.CONVOY_BANNER_DEPTH;

            Face_Bg = new Sprite();
            Face_Bg.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Face_Bg.loc = Banner.loc + new Vector2(8, -8);
            Face_Bg.src_rect = new Rectangle(136, 136, 48, 64);
            Face_Bg.stereoscopic = Config.CONVOY_ICON_DEPTH;
            Convoy_Face = new Miniface();
            Convoy_Face.loc = Face_Bg.loc + new Vector2(24, 8);
            if (Global.battalion.convoy_id > 0)
                Convoy_Face.set_actor(Global.game_actors[Global.battalion.convoy_id].face_name);
            else
                Convoy_Face.set_actor("Convoy");
            Convoy_Face.stereoscopic = Config.CONVOY_ICON_DEPTH;

            Stock_Banner = new Sprite();
            Stock_Banner.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            Stock_Banner.loc = new Vector2(232, 8);
            Stock_Banner.src_rect = new Rectangle(0, 64, 88, 24);
            Stock_Banner.offset = new Vector2(0, 2);
            Stock_Banner.stereoscopic = Config.CONVOY_STOCK_DEPTH;
            Convoy_Label = new FE_Text();
            Convoy_Label.loc = Face_Bg.loc + new Vector2(8, 40);
            Convoy_Label.Font = "FE7_Text";
            Convoy_Label.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Convoy_Label.text = "Convoy";
            Convoy_Label.stereoscopic = Config.CONVOY_ICON_DEPTH;
            Convoy_Text = new FE_Text();
            Convoy_Text.loc = Face_Bg.loc + new Vector2(48, 16);
            Convoy_Text.Font = "FE7_Text";
            Convoy_Text.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Convoy_Text.text = "What'll you do?";
            Convoy_Text.one_at_a_time = true;
            Convoy_Text.stereoscopic = Config.CONVOY_ICON_DEPTH;

            Stock_Label = new FE_Text();
            Stock_Label.loc = Stock_Banner.loc + new Vector2(8, 0);
            Stock_Label.Font = "FE7_Text";
            Stock_Label.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Stock_Label.text = "Stock";
            Stock_Label.stereoscopic = Config.CONVOY_STOCK_DEPTH;
            Stock_Value = new FE_Text_Int();
            Stock_Value.loc = Stock_Banner.loc + new Vector2(56, 0);
            Stock_Value.Font = "FE7_Text";
            Stock_Value.stereoscopic = Config.CONVOY_STOCK_DEPTH;
            Stock_Slash = new FE_Text();
            Stock_Slash.loc = Stock_Banner.loc + new Vector2(56, 0);
            Stock_Slash.Font = "FE7_Text";
            Stock_Slash.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Stock_Slash.text = "/";
            Stock_Slash.stereoscopic = Config.CONVOY_STOCK_DEPTH;
            Stock_Max = new FE_Text_Int();
            Stock_Max.loc = Stock_Banner.loc + new Vector2(88, 0);
            Stock_Max.Font = "FE7_Text";
            Stock_Max.stereoscopic = Config.CONVOY_STOCK_DEPTH;

            HelpFooter = new Prep_Items_Help_Footer();
            HelpFooter.loc = new Vector2(0, Config.WINDOW_HEIGHT - 18);
            HelpFooter.stereoscopic = Config.CONVOY_INPUTHELP_DEPTH + 1;

            refresh_input_help();

            refresh();
        }

        protected void refresh_input_help()
        {
            /*R_Button = new Sprite();
            R_Button.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen");
            R_Button.loc = new Vector2(280, 176);
            R_Button.src_rect = new Rectangle(104, 120, 40, 16);
            R_Button.stereoscopic = Config.CONVOY_INPUTHELP_DEPTH;*/
            R_Button = Button_Description.button(Inputs.R,
                Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Screen"), new Rectangle(126, 122, 24, 16));
            R_Button.loc = new Vector2(276, 176);
            R_Button.offset = new Vector2(0, -2);
            R_Button.stereoscopic = Config.CONVOY_INPUTHELP_DEPTH;
        }

        protected void create_command_window(Supply_Command_Window type)
        {
            List<string> commands;
            switch (type)
            {
                case Supply_Command_Window.Give:
                    commands = new List<string> { "Give" };
                    break;
                case Supply_Command_Window.Take:
                    commands = new List<string> { "Take" };
                    break;
                case Supply_Command_Window.Restock:
                    commands = new List<string> { "Restock" };
                    break;
                default:
                    if (this.restock_blocked)
                        commands = new List<string> { "Give", "Take" };
                    else
                        commands = new List<string> { "Give", "Restock", "Take" };
                    break;
            }
            int width = 56;
            int i = Math.Max(0, (int)type - 1);
            Vector2 loc = new Vector2(
                (64 + 36) + ((i % 2) * (width - 16)),
                (36 - 2) + ((i / 2) * 16));
            //Vector2 loc = new Vector2( //Debug
            //    (64 + 36),
            //    (36 - 2));
            Command_Window = new Window_Command(loc, width, commands);
            Command_Window.set_columns(this.restock_blocked ? 1 : 2);
            Command_Window.glow_width = width - 8;
            Command_Window.glow = true;
            Command_Window.bar_offset = new Vector2(-8, 0);
            Command_Window.text_offset = new Vector2(0, -4);
            Command_Window.size_offset = new Vector2(-8, -8);
            Command_Window.greyed_cursor = type != Supply_Command_Window.All;
            Command_Window.active = type == Supply_Command_Window.All;
            Command_Window.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Preparations_Item_Options_Window");
            Command_Window.immediate_index = 0;
            Command_Window.color_override = 0;
            Command_Window.stereoscopic = Config.CONVOY_WINDOW_DEPTH;
        }

        protected void refresh()
        {
            // Item Window
            refresh_item_window();

            Supply_Window.refresh();
            if (Item_Selection_Window != null)
                refresh_select_take();

            // //Yeti
            bool convoy_full = Global.game_battalions.active_convoy_data.Count >=
                Constants.Gameplay.CONVOY_SIZE;
            Stock_Value.text = Global.game_battalions.active_convoy_data.Count.ToString();
            Stock_Value.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_" + (convoy_full ? "Green" : "Blue"));
            Stock_Max.text = Constants.Gameplay.CONVOY_SIZE.ToString();
            Stock_Max.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_" + (convoy_full ? "Green" : "Blue"));
        }
        protected void refresh_item_window()
        {
            int index = Item_Window == null ? 0 : Item_Window.index;
            Item_Window = new Window_Command_Item_Supply(
                unit.actor.id, new Vector2(
                    8, Config.WINDOW_HEIGHT - (Constants.Actor.NUM_ITEMS + 2) * 16),
                Restocking);
            Item_Window.glow = true;
            if (Item_Window.num_items() > 0)
                Item_Window.immediate_index = index;
            Item_Window.active = Giving || Restocking;
            Item_Window.manual_cursor_draw = true;
            Item_Window.manual_help_draw = true;
            Item_Window.stereoscopic = Config.CONVOY_INVENTORY_DEPTH;
            Item_Window.help_stereoscopic = Config.CONVOY_HELP_DEPTH;

            Command_Window.set_text_color(0, can_give ? "White" : "Grey");
            if (this.restock_blocked)
            {
                Command_Window.set_text_color(1, can_take ? "White" : "Grey");
            }
            else
            {
                Command_Window.set_text_color(1, can_restock ? "White" : "Grey");
                Command_Window.set_text_color(2, can_take ? "White" : "Grey");
            }
        }

        protected void refresh_select_take()
        {
            List<FEXNA_Library.Item_Data> items = new List<FEXNA_Library.Item_Data>();
            int index = Supply_Window.redirect;
            int i = 0;
            while ((index + i) < Global.game_battalions.active_convoy_data.Count &&
                Global.game_battalions.active_convoy_data[index + i].Type ==
                     Global.game_battalions.active_convoy_data[index].Type &&
                Global.game_battalions.active_convoy_data[index + i].Id ==
                     Global.game_battalions.active_convoy_data[index].Id)
            {
                items.Add(Global.game_battalions.active_convoy_data[index + i]);
                i++;
            }
            Item_Selection_Window.set_item_data(items);
        }

        #region Update
        public override void update()
        {
            Command_Window.update(!trading && ready);

            int item_index = Item_Window.index;
            Item_Window.update(Giving || Restocking);
            if (item_index != Item_Window.index)
            {
                item_window_index_changed();
            }

            int supply_index = Supply_Window.can_take ? Supply_Window.redirect : -1;
            Supply_Window.update();
            if (supply_index != (Supply_Window.can_take ? Supply_Window.redirect : -1))
            {
                supply_window_index_changed();
            }

            if (Item_Selection_Window != null)
                Item_Selection_Window.update();
            Convoy_Text.update();
            HelpFooter.update();
            base.update();
            if (Input.ControlSchemeSwitched)
                refresh_input_help();
        }

        private void item_window_index_changed()
        {
            HelpFooter.refresh(this.unit, this.unit.items[Item_Window.index]);
        }

        private void supply_window_index_changed()
        {
            if (!Supply_Window.can_take)
                HelpFooter.refresh(this.unit, null);
            else
                HelpFooter.refresh(
                    this.unit, Global.battalion.convoy_item(Supply_Window.redirect));
        }

        new internal void update_input()
        {
            if (!trading)
            {
                if (Command_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    close();
                }
                else if (Command_Window.is_selected())
                {
                    trade();
                }
                else if (Command_Window.getting_help()) { } //Yeti
            }
            else if (Giving || Restocking)
                update_unit_inventory();
            else
                update_taking();
        }

        private void update_unit_inventory()
        {
            if (is_help_active)
            {
                if (Item_Window.is_canceled())
                    close_help();
            }
            else if (giving)
            {
                if (Item_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    cancel_trading();
                }
                else if (Item_Window.is_selected())
                    give();
                else if (Item_Window.getting_help())
                    open_help();
            }
            else if (restocking)
            {
                if (Item_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    cancel_trading();
                }
                else if (Item_Window.is_selected())
                    restock();
                else if (Item_Window.getting_help())
                    open_help();
            }
        }

        private void update_taking()
        {
            if (selecting_take)
            {
                if (is_help_active)
                {
                    if (Item_Selection_Window.is_canceled())
                        close_help();
                }
                else
                {
                    if (Item_Selection_Window.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        cancel_selecting_take();
                    }
                    else if (Item_Selection_Window.is_selected())
                        take();
                    else if (Item_Selection_Window.getting_help())
                        open_help();
                }
            }
            else if (taking)
            {
                if (is_help_active)
                {
                    if (Supply_Window.is_canceled())
                        close_help();
                }
                else
                {
                    if (Supply_Window.is_canceled())
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        cancel_trading();
                    }
                    else if (Supply_Window.is_selected())
                    {
                        if (Constants.Gameplay.CONVOY_ITEMS_STACK == Convoy_Stack_Types.Full)
                            select_take();
                        else
                            take();
                    }
                    else if (Global.Input.triggered(Inputs.X) &&
                        Constants.Gameplay.CONVOY_ITEMS_STACK == Convoy_Stack_Types.Full)
                    {
                        take();
                    }
                    else if (Supply_Window.getting_help())
                    {
                        open_help();
                    }
                }
            }
        }

        public void trade()
        {
            Supply_Command_Window selected_option;
            if (Command_Window.selected_index() == 0)
                selected_option = Supply_Command_Window.Give;
            else if (Command_Window.selected_index() == 1)
            {
                if (this.restock_blocked)
                    selected_option = Supply_Command_Window.Take;
                else
                    selected_option = Supply_Command_Window.Restock;
            }
            else
                selected_option = Supply_Command_Window.Take;

            switch (selected_option)
            {
                case (Supply_Command_Window.Give):
                    if (can_give)
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Item_Window.active = true;
                        Item_Window.current_cursor_loc = Command_Window.current_cursor_loc;
                        create_command_window(Supply_Command_Window.Give);
                        Giving = true;
                        item_window_index_changed();
                    }
                    else
                        Global.game_system.play_se(System_Sounds.Buzzer);
                    break;
                case (Supply_Command_Window.Take):
                    if (can_take)
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Supply_Window.active = true;
                        Supply_Window.current_cursor_loc =
                            Command_Window.current_cursor_loc +
                            new Vector2(0, 16 * Supply_Window.scroll);
                        create_command_window(Supply_Command_Window.Take);
                        //Supply_Window.refresh_cursor_loc(false); //Debug
                        Taking = true;
                        supply_window_index_changed();
                    }
                    else
                        Global.game_system.play_se(System_Sounds.Buzzer);
                    break;
                case (Supply_Command_Window.Restock):
                    if (can_restock)
                    {
                        Global.game_system.play_se(System_Sounds.Confirm);
                        Restocking = true;
                        refresh_item_window();
                        Item_Window.active = true;
                        Item_Window.current_cursor_loc = Command_Window.current_cursor_loc;
                        create_command_window(Supply_Command_Window.Restock);
                        item_window_index_changed();
                    }
                    else
                        Global.game_system.play_se(System_Sounds.Buzzer);
                    break;
            }
        }

        public void cancel_trading()
        {
            create_command_window(Supply_Command_Window.All);
            var command = Restocking ? Supply_Command_Window.Restock :
                (Giving ? Supply_Command_Window.Give : Supply_Command_Window.Take);
            Command_Window.index = (int)command - 1;
            if (Taking)
                Command_Window.current_cursor_loc =
                    Supply_Window.current_cursor_loc -
                    new Vector2(0, 16 * Supply_Window.scroll);
            else
                Command_Window.current_cursor_loc = Item_Window.current_cursor_loc;

            Item_Window.active = false;
            Supply_Window.active = false;
            Giving = false;
            Taking = false;
            Restocking = false;
            refresh_item_window();
            HelpFooter.refresh(this.unit, null);
        }

        public void select_take()
        {
            if (Supply_Window.can_take)
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                Item_Selection_Window = new Window_Command_Item_Convoy_Take(Unit_Id, Supply_Window.loc + new Vector2(8, 20));
                Item_Selection_Window.stereoscopic = Config.CONVOY_SELECTION_DEPTH;
                refresh_select_take();
                Supply_Window.active = false;
            }
            else
                Global.game_system.play_se(System_Sounds.Buzzer);
        }

        public void cancel_selecting_take()
        {
            Item_Selection_Window = null;
            Supply_Window.active = true;
        }

        public void give()
        {
            FEXNA_Library.Item_Data item_data = actor.items[Item_Window.index];
            Global.game_battalions.add_item_to_convoy(actor.items[Item_Window.index]);
            actor.discard_item(Item_Window.index);
            if (!can_give)
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                cancel_trading();
            }
            else
                Global.game_system.play_se(System_Sounds.Confirm);
            Traded = true;
            refresh();
            // Add jumping to the correct page and probably jumping to the correct line for the item here //Debug?
            Supply_Window.jump_to(item_data);
        }

        public void take()
        {
            if (Item_Selection_Window != null)
            {
                actor.gain_item(
                    Global.game_battalions.remove_item_from_convoy(Global.battalion.convoy_id, Supply_Window.redirect + Item_Selection_Window.index));
                if (Item_Selection_Window.item_count == 1 || !can_take)
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    cancel_selecting_take();
                    if (!can_take)
                        cancel_trading();
                }
                else
                    Global.game_system.play_se(System_Sounds.Confirm);
                Traded = true;
                refresh();
            }
            else
            {
                if (Supply_Window.can_take)
                {
                    actor.gain_item(
                        Global.game_battalions.remove_item_from_convoy(
                        Global.battalion.convoy_id, Supply_Window.redirect));
                    if (!can_take)
                    {
                        Global.game_system.play_se(System_Sounds.Cancel);
                        cancel_trading();
                    }
                    else
                        Global.game_system.play_se(System_Sounds.Confirm);
                    Traded = true;
                    refresh();
                }
                else
                    Global.game_system.play_se(System_Sounds.Buzzer);
            }
        }

        public void restock()
        {
            if (actor.restock(Item_Window.index))
            {
                if (!can_restock)
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    cancel_trading();
                }
                else
                    Global.game_system.play_se(System_Sounds.Confirm);
                Traded = true;
                refresh();
            }
            else
                Global.game_system.play_se(System_Sounds.Buzzer);
        }

        new public void close()
        {
            Closing = true;
            Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
            if (Black_Screen != null)
                Black_Screen.visible = true;
            if (Is_Actor)
                Global.game_map.remove_unit(Unit_Id);
        }
        #endregion

        #region Help
        public void open_help()
        {
            if (Giving || Restocking)
            {
                Item_Window.open_help();
            }
            else if (Taking)
            {
                if (Item_Selection_Window != null)
                    Item_Selection_Window.open_help();
                else
                    Supply_Window.open_help();
            }
            else { }
        }

        public virtual void close_help()
        {
            if (Giving || Restocking)
            {
                Item_Window.close_help();
            }
            else if (Taking)
            {
                if (Item_Selection_Window != null)
                    Item_Selection_Window.close_help();
                else
                    Supply_Window.close_help();
            }
            else { }
        }
        #endregion

        #region Draw
        protected override void draw_window(SpriteBatch sprite_batch)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Banner.draw(sprite_batch);
            Convoy_Text.draw(sprite_batch);
            Face_Bg.draw(sprite_batch);
            Convoy_Label.draw(sprite_batch);
            sprite_batch.End();
            Convoy_Face.draw(sprite_batch);
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Stock_Banner.draw(sprite_batch);
            Stock_Label.draw(sprite_batch);
            Stock_Max.draw(sprite_batch);
            Stock_Slash.draw(sprite_batch);
            Stock_Value.draw(sprite_batch);
            sprite_batch.End();

            if (Taking)
            {
                Item_Window.draw(sprite_batch);
                Supply_Window.draw(sprite_batch);

                Command_Window.draw(sprite_batch);
            }
            else
            {
                Supply_Window.draw(sprite_batch);
                Item_Window.draw(sprite_batch);

                Command_Window.draw(sprite_batch);
            }
            if (Item_Selection_Window != null)
                Item_Selection_Window.draw(sprite_batch);

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            HelpFooter.draw(sprite_batch);
            sprite_batch.End();

            Item_Window.draw_help(sprite_batch);
            Supply_Window.draw_help(sprite_batch);

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            //Command_Window.draw(sprite_batch); //Debug
            Supply_Window.draw_cursor(sprite_batch);
            Item_Window.draw_cursor(sprite_batch);
            R_Button.Draw(sprite_batch);
            // Labels
            // Data
            sprite_batch.End();
        }
        #endregion
    }
}
