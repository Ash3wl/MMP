using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Preparations;
using FEXNA.Windows.Command;
using FEXNA.Windows.UserInterface.Command;
using FEXNA_Library;

namespace FEXNA
{
    enum PrepSupportInputResults { None, Status, Closing }

    class Window_Base_Support : Window_Prep_Items
    {
        const int SUPPORT_START_TIME = 8;

        protected int Healer_Id = -1, Battler_1_Id = -1, Battler_2_Id = -1;
        protected int temp_battler_1_id = -1, temp_battler_2_id = -1;
        private bool Starting_Support, Returning_From_Support;
        private int Support_Timer = 0;
        private Prep_Support_Stats_Window Support_Stats_Window;
        private Prep_Support_List_Window Support_List_Window;

        #region Accessors
        public override bool is_help_active { get { return Command_Window != null; } }

        public bool partner_selected { get { return Use_Confirm_Window != null; } }

        new public bool ready
        {
            get
            {
                return !Closing && Black_Screen_Timer <= 0 && !Starting_Support && !Returning_From_Support;
            }
        }
        #endregion

        public Window_Base_Support()
        {
            Choose_Unit_Window.width = 104;
            Choose_Unit_Window.loc -= new Vector2(16, 0);
            set_label("");
            label_visible(false);
        }

        protected override void returning_to_menu(bool returning_to_item_use)
        {
            if (returning_to_item_use)
            {
            }
        }

        protected override void set_unit_window()
        {
            UnitWindow = new Window_Prep_Support_Unit();
        }

        protected override void set_command_window() { }

        public override void refresh()
        {
            Global.game_map.add_actor_unit(Constants.Team.PLAYER_TEAM, Config.OFF_MAP, UnitWindow.actor_id, "");
            create_stats_window();
            Global.game_map.completely_remove_unit(Global.game_map.last_added_unit.id);

            Item_Header = new Pick_Units_Items_Header(UnitWindow.actor_id, 144);
            Item_Header.loc = (new Vector2(UnitWindow.loc.X + 4, ((Config.WINDOW_HEIGHT / 16) - 5) * 16 + 8)) - new Vector2(4, 36);
            Item_Header.stereoscopic = Config.PREPITEM_UNIT_DEPTH;
        }

        public override void refresh_trade()
        {
            /*Item_Window_1 = new Window_Command_Item_Preparations(
                Trading_Actor_Id, new Vector2(Unit_Window.loc.X + 4, ((Config.WINDOW_HEIGHT / 16) - (Config.NUM_ITEMS + 1)) * 16 + 8), true);
            Item_Window_1.darkened = true;
            Item_Window_1.stereoscopic = Config.PREPITEM_UNIT_DIMMED_DEPTH;
            Item_Window_2 = new Window_Command_Item_Preparations(
                Unit_Window.actor_id, new Vector2(Unit_Window.loc.X + 148, ((Config.WINDOW_HEIGHT / 16) - (Config.NUM_ITEMS + 1)) * 16 + 8), false);
            Item_Window_2.stereoscopic = Config.PREPITEM_UNIT_DEPTH;*/
        }

        protected override void create_stats_window()
        {
            Support_Stats_Window = new Prep_Support_Stats_Window(Global.game_map.last_added_unit);
            Support_Stats_Window.loc = new Vector2(12, Config.WINDOW_HEIGHT - 76);
            Support_List_Window = new Prep_Support_List_Window(Global.game_map.last_added_unit.actor);
            Support_List_Window.loc = new Vector2(Config.WINDOW_WIDTH - 164, Config.WINDOW_HEIGHT - 100);
        }

        public override void update()
        {
            base.update();
            if (Returning_From_Support)
            {
                Black_Screen.visible = true;
                if (!Global.game_state.support_active &&
                    !Global.scene.is_message_window_active && Support_Timer > 0)
                {
                    if (Support_Timer == SUPPORT_START_TIME)
                    {
                        refresh();
                        (UnitWindow as Window_Prep_Support_Unit).refresh_map_sprites();
                    }
                    Support_Timer--;
                    Black_Screen.tint = new Color(0f, 0f, 0f, Support_Timer * (1f / SUPPORT_START_TIME));
                    if (Support_Timer == 0)
                        Returning_From_Support = false;
                }
            }
            else if (Starting_Support)
            {
                Black_Screen.visible = true;
                if (Support_Timer > 0)
                {
                    Support_Timer--;
                    Black_Screen.tint = new Color(0f, 0f, 0f, (SUPPORT_START_TIME - Support_Timer) * (1f / SUPPORT_START_TIME));
                    if (Support_Timer == 0)
                        start_support();
                }
            }
        }

        protected override void update_unit_window()
        {
            UnitWindow.update(Active && ready && Command_Window == null);
        }
        protected override void update_command_window()
        {
            Command_Window.update(Active && ready && !partner_selected);
        }

        new internal PrepSupportInputResults update_input()
        {
            // Confirming activating a support
            if (this.partner_selected)
            {
                if (Use_Confirm_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    cancel_partner_selection();
                }
                else if (Use_Confirm_Window.is_selected())
                {
                    if (accept_support())
                        Global.game_system.play_se(System_Sounds.Confirm);
                    else
                        Global.game_system.play_se(System_Sounds.Cancel);
                }
            }
            #region Unit Selected
            // Viewing list of support partners
            else if (this.unit_selected)
            {
                return update_selected_input();
            }
            #endregion
            // Viewing unit list
            else
            {
                return update_base_input();
            }
            return PrepSupportInputResults.None;
        }

        new private PrepSupportInputResults update_selected_input()
        {
            if (Command_Window.is_canceled())
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                cancel_unit_selection();
                return PrepSupportInputResults.None;
            }
            else if (Command_Window.is_selected())
            {
                if (select_partner())
                    Global.game_system.play_se(System_Sounds.Confirm);
                else
                    Global.game_system.play_se(System_Sounds.Buzzer);
                return PrepSupportInputResults.None;
            }
            else if (Command_Window.getting_help())
            {
            }

            return PrepSupportInputResults.None;
        }

        private PrepSupportInputResults update_base_input()
        {
            // Close this window
            if (Global.Input.triggered(Inputs.B))
            {
                Global.game_system.play_se(System_Sounds.Cancel);
                close();
                return PrepSupportInputResults.Closing;
            }

            // Select unit
            var selected_index = UnitWindow.consume_triggered(
                Inputs.A, MouseButtons.Left, TouchGestures.Tap);
            if (selected_index.IsSomething)
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                select_unit();
                return PrepSupportInputResults.None;
            }

            // Status screen
            var status_index = UnitWindow.consume_triggered(
                Inputs.R, MouseButtons.Right, TouchGestures.LongPress);
            if (status_index.IsSomething)
            {
                Global.game_system.play_se(System_Sounds.Confirm);
                active = false;
                return PrepSupportInputResults.Status;
            }

            return PrepSupportInputResults.None;
        }

        #region Controls
        public override bool select_unit()
        {
            if (this.actor.support_candidates().Any())
            {
                Unit_Selected = true;
                UnitWindow.active = false;
                UnitWindow.stereoscopic = Config.PREPITEM_BATTALION_DIMMED_DEPTH;

                Command_Window = new Window_Command_Support(
                    this.actor.id, new Vector2(Config.WINDOW_WIDTH - 152, 16));
                return true;
            }
            else
                return false;
        }

        public bool select_partner()
        {
            if (is_support_partner_ready())
            {
                initialize_confirm_window();
                return true;
            }
            return false;
        }

        private bool is_support_partner_ready()
        {
            if (!Global.battalion.actors.Contains(this.actor.support_candidates()[Command_Window.index]))
                return false;
#if DEBUG
            if (Global.Input.pressed(Inputs.Start) && !this.actor.is_support_maxed(false, this.actor.support_candidates()[Command_Window.index]) &&
                !this.actor.is_support_level_maxed(this.actor.support_candidates()[Command_Window.index]))
            {
                Global.Audio.play_se("System Sounds", "Buzzer");
                return true;
            }
#endif
            return this.actor.is_support_ready(this.actor.support_candidates()[Command_Window.index]);
        }

        private void initialize_confirm_window()
        {
            Use_Confirm_Window = new Preparations_Confirm_Window();
            Use_Confirm_Window.set_text(string.Format("Speak to {0}?",
                Global.game_actors[this.actor.support_candidates()[Command_Window.index]].name));
            Use_Confirm_Window.add_choice("Yes", new Vector2(16, 12));
            Use_Confirm_Window.add_choice("No", new Vector2(64, 12));
            Use_Confirm_Window.size = new Vector2(112, 40);
            Use_Confirm_Window.loc = new Vector2(32, 24);
            Use_Confirm_Window.index = 1;

            Command_Window.active = false;
        }

        public override void cancel_unit_selection()
        {
            Unit_Selected = false;
            UnitWindow.active = true;
            UnitWindow.stereoscopic = Config.PREPITEM_BATTALION_DEPTH;

            Command_Window = null;
        }

        public void cancel_partner_selection()
        {
            Use_Confirm_Window = null;
            Command_Window.active = true;
        }

        public bool accept_support()
        {
            if (Use_Confirm_Window.index == 1)
            {
                cancel_partner_selection();
                return false;
            }
            else
            {
                Starting_Support = true;
                Support_Timer = SUPPORT_START_TIME;
                return true;
            }
        }

        private void start_support()
        {
            Global.game_state.call_support(UnitWindow.actor_id, this.actor.support_candidates()[Command_Window.index]);

            Starting_Support = false;
            Returning_From_Support = true;
            Support_Timer = SUPPORT_START_TIME;

            cancel_partner_selection();
            cancel_unit_selection();
        }
        #endregion

        protected override void draw_window(SpriteBatch sprite_batch)
        {
            base.draw_window(sprite_batch);
        }

        protected override void draw_stats_window(SpriteBatch sprite_batch)
        {
            if (Support_Stats_Window != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Support_Stats_Window.draw(sprite_batch);
                sprite_batch.End();
            }
            if (Support_List_Window != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Support_List_Window.draw(sprite_batch);
                sprite_batch.End();
            }
        }

        protected override bool labels_visible()
        {
            return false;
        }
    }
}
