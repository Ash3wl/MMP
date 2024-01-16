using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Windows;
using FEXNA.Windows.Map;
using FEXNA.Windows.UserInterface.Command;

namespace FEXNA
{
    enum Preparations_Choices { Pick_Units, Trade, Fortune, Check_Map, Save }
    enum Home_Base_Choices { Trade, Support, Talk, Codex, Options, Save }
    partial class Scene_Map
    {
        const string DEFAULT_HOME_BASE_BACKGROUND = "Camp";

        protected bool Changing_Formation = false;

        private Window_Setup Setup_Window;
        private Window_Setup_CheckMap CheckMap_Window;
        private Window_Prep_PickUnits Prep_PickUnits_Window;
        private Window_Prep_Items Prep_Items_Window;
        private Window_Prep_Trade Prep_Trade_Window;
        private Window_Base_Support Prep_Support_Window;
        private Window_Base_Convos Base_Convo_Window;
        private Window_Augury AuguryWindow;
        private Parchment_Confirm_Window Leave_Base_Confirm_Window;

        #region Accessors
        public bool changing_formation { get { return Changing_Formation; } }
        #endregion

        public void activate_preparations()
        {
            if (!Global.game_system.preparations)
            {
                Global.game_actors.heal_battalion();

                Global.game_system.preparations = true;
                Global.game_temp.menuing = true;

                Setup_Window = new Window_Setup(true);
                CheckMap_Window = new Window_Setup_CheckMap();
                start_preparations();
            }
        }

        public void resume_preparations()
        {
            if (Global.game_system.preparations)
            {
                Global.game_temp.menuing = true;

                Setup_Window = new Window_Setup(false);
                Setup_Window.black_screen();
                CheckMap_Window = new Window_Setup_CheckMap();
                start_preparations();
            }
        }

        protected void end_preparations()
        {
            Global.game_system.preparations = false;
            if (Global.game_system.home_base)
            {
                Global.battalion.leave_home_base();

                if (Constants.Support.BASE_COUNTS_AS_SEPARATE_CHAPTER)
                    Global.game_state.reset_support_data();
            }
            else
                Global.game_state.end_preparations();
            Global.game_system.home_base = false;
            Global.battalion.refresh_deployed();
        }

        public void activate_home_base()
        {
            activate_home_base(Global.game_system.home_base_background);
        }
        public void activate_home_base(string background)
        {
            if (!Global.game_system.preparations)
            {
                Global.game_actors.heal_battalion();

                Global.game_system.preparations = true;
                Global.game_system.home_base = true;
                Global.game_temp.menuing = true;
                if (!Global.content_exists(@"Graphics/Panoramas/" + background))
                    background = DEFAULT_HOME_BASE_BACKGROUND;
                Global.game_system.home_base_background = background;
                Global.battalion.enter_home_base();

                if (Constants.Support.BASE_COUNTS_AS_SEPARATE_CHAPTER)
                    Global.game_state.reset_support_data();

                Setup_Window = new Window_Home_Base();
                start_preparations();
            }
        }

        public void resume_home_base()
        {
            if (Global.game_system.home_base)
            {
                Global.game_temp.menuing = true;

                Setup_Window = new Window_Home_Base();
                Setup_Window.black_screen();
                start_preparations();
            }
        }

        public void resume_preparations_item_menu()
        {
            if (Global.game_system.preparations)
            {
                Global.game_temp.menuing = true;

                if (Global.game_system.home_base)
                {
                    Setup_Window = new Window_Home_Base();
                    Setup_Window.index = (int)Home_Base_Choices.Trade;
                }
                else
                {
                    Setup_Window = new Window_Setup(false);
                    Setup_Window.index = (int)Preparations_Choices.Trade;
                    CheckMap_Window = new Window_Setup_CheckMap();
                }
                Setup_Window.black_screen();
                Global.Audio.bgm_fade();
                Global.game_state.play_preparations_theme();
                Prep_Items_Window = new Window_Prep_Items(true);
                Setup_Window.active = false;
            }
        }

        private void start_preparations()
        {
            Global.game_map.move_range_visible = false;
            Global.Audio.bgm_fade();
            Global.game_state.play_preparations_theme();
            Global.game_system.Preparations_Actor_Id = Global.battalion.actors[0];
            Global.game_system.Preparation_Events_Ready = false;
        }

        #region Update
        protected void update_preparations_menu_calls()
        {
            if (Global.game_system.preparations)
                if (Global.game_temp.map_menu_call)
                {
                    Global.game_map.clear_move_range();
                    Global.game_temp.menuing = true;
                    Global.game_temp.menu_call = false;
                    Global.game_temp.map_menu_call = false;
                    Global.game_system.play_se(System_Sounds.Unit_Select);
                    CheckMap_Window = new Window_Setup_CheckMap();
                    CheckMap_Window.active = true;
                    if (Changing_Formation)
                        CheckMap_Window.index = 1;
                    Changing_Formation = false;
                    Global.game_map.clear_move_range();
                    Global.game_map.move_range_visible = false;
                }
        }

        /// <summary>
        /// Updates preparations menus. Returns true if an active menu was processed, so no other menus should be updated.
        /// </summary>
        protected bool update_preparations()
        {
            if (Global.game_system.preparations)
            {
                // Move the redundant code from both branches out to methods //Yeti
                #region Home Base
                if (Global.game_system.home_base)
                {
                    if (Setup_Window != null)
                    {
                        Setup_Window.update();
                        if (Prep_Items_Window != null)
                        {
                            Prep_Items_Window.update();
                            if (Status_Window != null)
                            {
                                Status_Window.update();
                                update_status_menu();
                                Global.game_temp.menuing = true;
                                if (Status_Window == null)
                                    Prep_Items_Window.active = true;
                            }
                            else if (Prep_Trade_Window != null)
                            {
                                Prep_Trade_Window.update();
                                update_preptrade_menu();
                            }
                            else if (Supply_Window != null)
                            {
                                Supply_Window.update();
                                update_supply_menu();
                            }
                            else if (Shop_Window != null)
                            {
                                Shop_Window.update();
                                update_shop_menu();
                                if (Shop_Window == null)
                                    Prep_Items_Window.active = true;
                            }
                            else
                                update_prepitems_menu();
                        }
                        else if (Prep_Support_Window != null)
                        {
                            Prep_Support_Window.update();
                            if (Status_Window != null)
                            {
                                Status_Window.update();
                                update_status_menu();
                                Global.game_temp.menuing = true;
                                if (Status_Window == null)
                                    Prep_Support_Window.active = true;
                            }
                            else
                                update_base_support_menu();
                        }
                        else if (Base_Convo_Window != null)
                        {
                            Base_Convo_Window.update();
                            update_base_talk_menu();
                        }
                        else if (Options_Window != null)
                        {
                            if (Unit_Window != null)
                            {
                                if (Status_Window != null)
                                {
                                    Status_Window.update();
                                    update_status_menu();
                                }
                                else
                                {
                                    Unit_Window.update();
                                    update_unit_menu();
                                }
                            }
                            else
                            {
                                check_options();
                                if (Options_Window == null)
                                {
                                    Global.game_temp.menuing = true;
                                    Setup_Window.active = true;
                                }
                                return true;
                            }
                        }
                        else if (Map_Save_Confirm_Window != null)
                        {
                            Map_Save_Confirm_Window.update();
                            update_map_save();
                        }
                        else if (Leave_Base_Confirm_Window != null)
                        {
                            Leave_Base_Confirm_Window.update();
                            update_leave_base();
                        }
                        else
                            update_home_base_setup_menu();
                    }
                    return true;
                }
                #endregion
                #region Preparations
                else
                {
                    if (Unit_Window != null)
                    {
                        if (Status_Window != null)
                        {
                            Status_Window.update();
                            update_status_menu();
                            return true;
                        }
                        else
                        {
                            Unit_Window.update();
                            if (Unit_Window.closing)
                                Prep_PickUnits_Window.update();
                            update_unit_menu();
                            return true;
                        }
                    }
                    else if (AuguryWindow != null)
                    {
                        AuguryWindow.update();
                        update_augury();
                    }
                    if (check_options())
                    {
                        Global.game_temp.menuing = true;
                        return true;
                    }
                    if (CheckMap_Window != null)
                    {
                        CheckMap_Window.update();
                        if (Setup_Window == null)
                        {
                            if (Map_Save_Confirm_Window != null)
                            {
                                Map_Save_Confirm_Window.update();
                                update_map_save();
                            }
                            else
                                update_checkmap_menu();
                            return true;
                        }
                    }
                    if (Setup_Window != null)
                    {
                        Setup_Window.update();
                        {
                            if (Prep_PickUnits_Window != null)
                            {
                                Prep_PickUnits_Window.update();
                                if (Status_Window != null)
                                {
                                    Status_Window.update();
                                    update_status_menu();
                                    Global.game_temp.menuing = true;
                                    if (Status_Window == null)
                                        Prep_PickUnits_Window.active = true;
                                }
                                else
                                    update_preppickunits_menu();
                            }
                            else if (Prep_Items_Window != null)
                            {
                                Prep_Items_Window.update();
                                if (Status_Window != null)
                                {
                                    Status_Window.update();
                                    update_status_menu();
                                    Global.game_temp.menuing = true;
                                    if (Status_Window == null)
                                        Prep_Items_Window.active = true;
                                }
                                else if (Prep_Trade_Window != null)
                                {
                                    Prep_Trade_Window.update();
                                    update_preptrade_menu();
                                }
                                else if (Supply_Window != null)
                                {
                                    Supply_Window.update();
                                    update_supply_menu();
                                }
                                else if (Shop_Window != null)
                                {
                                    Shop_Window.update();
                                    update_shop_menu();
                                    if (Shop_Window == null)
                                        Prep_Items_Window.active = true;
                                }
                                else
                                    update_prepitems_menu();
                            }
                            else if (Map_Save_Confirm_Window != null)
                            {
                                Map_Save_Confirm_Window.update();
                                update_map_save();
                            }
                            else
                                update_setup_menu();
                        }
                        return true;
                    }
                }
                #endregion
            }
            return false;
        }

        #region Preparations
        protected void update_setup_menu()
        {
            if (Setup_Window.closed)
            {
                CheckMap_Window.active = true;
                if (Setup_Window.pressed_start)
                {
                    end_preparations();
                    Global.game_temp.menuing = false;
                    CheckMap_Window = null;
                }
                Setup_Window = null;
            }
            else if (Setup_Window.input_ready)
            {
                var input = Setup_Window.update_input();
                if (input != PrepSetupInputResults.None)
                {
                    switch (input)
                    {
                        case PrepSetupInputResults.PickUnits:
                            Prep_PickUnits_Window = new Window_Prep_PickUnits();
                            Setup_Window.active = false;
                            break;
                        case PrepSetupInputResults.Trade:
                            if (Global.game_system.SWITCHES[96])
                            {
                                Prep_Items_Window = new Window_Prep_Items();
                                Setup_Window.active = false;
                            }
                            else
                                Global.game_system.play_se(System_Sounds.Buzzer);
                            break;
                        case PrepSetupInputResults.Fortune:
                            //Yeti
                            if (Global.game_state.augury_event_exists())
                            {
                                Global.game_system.play_se(System_Sounds.Confirm);
                                AuguryWindow = new Window_Augury();
                                Setup_Window.active = false;
                            }
                            else
                                Global.game_system.play_se(System_Sounds.Buzzer);
                            break;
                        case PrepSetupInputResults.CheckMap:
                            Setup_Window.close();
                            CheckMap_Window.index = 0;
                            break;
                        case PrepSetupInputResults.Save:
                            //Yeti
                            //Global.game_system.play_se(System_Sounds.Confirm);
                            //Suspend_Filename = Config.MAP_SAVE_FILENAME;
                            //suspend();
                            open_map_save();
                            break;
                        case PrepSetupInputResults.StartMap:
                            Setup_Window.close(true);
                            break;
                    }
                }
            }
        }

        protected void update_checkmap_menu()
        {
            if (CheckMap_Window.closed)
            {
                if (CheckMap_Window.starting_map)
                {
                    end_preparations();
                }
                else
                    Global.game_map.move_range_visible = true;
                Global.game_temp.menuing = false;
                CheckMap_Window = null;
            }
            else if (CheckMap_Window.ready)
            {
                var input = CheckMap_Window.update_input();
                if (input != PrepCheckMapInputOptions.None)
                {
                    switch (input)
                    {
                        case PrepCheckMapInputOptions.ViewMap:
                            break;
                        case PrepCheckMapInputOptions.Formation:
                            Changing_Formation = true;
                            Global.game_map.view_deployments();
                            Global.game_map.highlight_test();
                            break;
                        case PrepCheckMapInputOptions.Options:
                            new_options_window();
                            break;
                        case PrepCheckMapInputOptions.Save:
                            //Yeti
                            //Suspend_Filename = Config.MAP_SAVE_FILENAME;
                            //suspend();
                            open_map_save();
                            break;
                    }
                }
                else if (CheckMap_Window.Canceled)
                {
                    Setup_Window = new Window_Setup(false);
                    CheckMap_Window.active = false;
                }
                else if (CheckMap_Window.is_selected())
                {
                    switch ((PrepCheckMapResults)(int)CheckMap_Window.selected_index())
                    {
                        case PrepCheckMapResults.StartChapter:
                            Global.game_system.play_se(System_Sounds.Confirm);
                            CheckMap_Window.close(true);
                            break;
                        case PrepCheckMapResults.Cancel:
                            Global.game_system.play_se(System_Sounds.Cancel);
                            Setup_Window = new Window_Setup(false);
                            CheckMap_Window.active = false;
                            break;
                    }
                }
            }
        }

        protected void update_preppickunits_menu()
        {
            if (Prep_PickUnits_Window.closed)
            {
                Setup_Window.active = true;
                if (Prep_PickUnits_Window.pressed_start)
                {
                    end_preparations();
                    Global.game_temp.menuing = false;
                    Setup_Window = null;
                    CheckMap_Window = null;
                }
                Prep_PickUnits_Window = null;
            }
            else if (Prep_PickUnits_Window.ready)
            {
                switch (Prep_PickUnits_Window.update_input())
                {
                    case PrepPickUnitsInputResults.None:
                        break;
                    case PrepPickUnitsInputResults.Closing:
                        Setup_Window.refresh_deployed_units(
                            Prep_PickUnits_Window.unit_changes());
                        Setup_Window.refresh();
                        break;
                    case PrepPickUnitsInputResults.UnitWindow:
                        Unit_Window = new Window_Unit();
                        Unit_Window.pickunits_window = Prep_PickUnits_Window;
                        break;
                    case PrepPickUnitsInputResults.Status:
                        Status_Window = new Window_Status(
                            Global.battalion.actors, Prep_PickUnits_Window.actor_id, true);
                        break;
                }
            }
        }

        protected void update_augury()
        {
            if (AuguryWindow.Event_Ended)
            {
                AuguryWindow = null;
                Setup_Window.active = true;
            }
            else if (AuguryWindow.start_event)
            {
                Global.game_state.activate_augury_event();
            }
        }
        #endregion

        #region Home Base
        protected void update_home_base_setup_menu()
        {
            if (Setup_Window.closed)
            {
                end_preparations();
                Global.game_temp.menuing = false;
                Setup_Window = null;
            }
            else if (Setup_Window.ready)
            {
                var selected_index = Setup_Window.selected_index;
                if (selected_index.IsSomething)
                {
                    switch ((Home_Base_Choices)(int)selected_index)
                    {
                        // Trade
                        case Home_Base_Choices.Trade:
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Prep_Items_Window = new Window_Prep_Items();
                            Setup_Window.active = false;
                            break;
                        // Support
                        case Home_Base_Choices.Support:
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Prep_Support_Window = new Window_Base_Support();
                            Setup_Window.active = false;
                            break;
                        // Talk
                        case Home_Base_Choices.Talk:
                            if (!(Setup_Window as Window_Home_Base).talk_events_exist)
                                Global.game_system.play_se(System_Sounds.Buzzer);
                            else
                            {
                                Global.game_system.play_se(System_Sounds.Confirm);
                                Base_Convo_Window = new Window_Base_Convos(new Vector2(48, Setup_Window.index * 16 + 32));
                                Setup_Window.active = false;
                            }
                            break;
                        // Codex
                        case Home_Base_Choices.Codex:
                            Global.game_system.play_se(System_Sounds.Buzzer);
                            break;
                        // Options
                        case Home_Base_Choices.Options:
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Options_Window = new Window_Options();
                            Setup_Window.active = false;
                            break;
                        // Save
                        case Home_Base_Choices.Save:
                            //Yeti
                            //Global.game_system.play_se(System_Sounds.Confirm);
                            //Suspend_Filename = Config.MAP_SAVE_FILENAME;
                            //suspend();
                            open_map_save();
                            break;
                    }
                }
                else if (Global.Input.triggered(Inputs.Start) ||
                    Setup_Window.start_ui_button_pressed)
                {
                    Leave_Base_Confirm_Window = new Parchment_Confirm_Window();
                    Leave_Base_Confirm_Window.set_text("Leave base?", new Vector2(8, 0));
                    Leave_Base_Confirm_Window.add_choice("Yes", new Vector2(16, 16));
                    Leave_Base_Confirm_Window.add_choice("No", new Vector2(56, 16));
                    Leave_Base_Confirm_Window.size = new Vector2(96, 48);
                    Leave_Base_Confirm_Window.loc = new Vector2(Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT) / 2 - Leave_Base_Confirm_Window.size / 2;
                }
            }
        }

        protected void update_base_support_menu()
        {
            if (Prep_Support_Window.closed)
            {
                Prep_Support_Window = null;
                Setup_Window.active = true;
            }
            else if (Prep_Support_Window.ready)
            {
                switch (Prep_Support_Window.update_input())
                {
                    case PrepSupportInputResults.None:
                        break;
                    case PrepSupportInputResults.Closing:
                        break;
                    case PrepSupportInputResults.Status:
                        Status_Window = new Window_Status(
                            Global.battalion.actors, Prep_Support_Window.actor_id, true);
                        break;
                }
            }
        }

        protected void update_base_talk_menu()
        {
            if (Base_Convo_Window.event_ended)
            {
                Base_Convo_Window = null;
                Setup_Window.active = true;
            }
            else if (Base_Convo_Window.start_event)
            {
                Global.game_state.activate_base_event(Base_Convo_Window.index);
                (Setup_Window as Window_Home_Base).refresh_talk_ready();
            }
            else if (!Base_Convo_Window.event_selected)
            {
                if (Base_Convo_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Base_Convo_Window = null;
                    Setup_Window.active = true;
                }
                else if (Base_Convo_Window.is_selected())
                {
                    if (Base_Convo_Window.select_event())
                        Global.game_system.play_se(System_Sounds.Confirm);
                    else
                        Global.game_system.play_se(System_Sounds.Buzzer);
                }
            }
        }

        protected void update_leave_base()
        {
            if (Leave_Base_Confirm_Window.is_ready)
            {
                if (Leave_Base_Confirm_Window.is_canceled())
                {
                    Global.game_system.play_se(System_Sounds.Cancel);
                    Leave_Base_Confirm_Window = null;
                    return;
                }
                else if (Leave_Base_Confirm_Window.is_selected())
                {
                    Global.game_system.play_se(System_Sounds.Confirm);
                    switch (Leave_Base_Confirm_Window.index)
                    {
                        // Yes
                        case 0:
                            Global.game_system.play_se(System_Sounds.Confirm);
                            Setup_Window.close(true);
                            break;
                        // No
                        case 1:
                            break;
                    }
                    Leave_Base_Confirm_Window = null;
                }
            }
        }
        #endregion

        protected void update_prepitems_menu()
        {
            if (Prep_Items_Window.closed)
            {
                Prep_Items_Window = null;
                Setup_Window.active = true;
            }
            else if (Prep_Items_Window.gaining_stats && Prep_Items_Window.using_item)
            {
                if (Global.Input.triggered(Inputs.A) ||
                        Global.Input.triggered(Inputs.B) ||
                        Global.Input.mouse_click(MouseButtons.Left) ||
                        Global.Input.gesture_triggered(TouchGestures.Tap))
                    Prep_Items_Window.skip_stat_gain();
            }
            else if (Prep_Items_Window.ready)
            {
                switch(Prep_Items_Window.update_input())
                {
                    case PrepItemsInputResults.None:
                        break;
                    case PrepItemsInputResults.OpenTrade:
                        Prep_Trade_Window = new Window_Prep_Trade(
                            Prep_Items_Window.trading_actor_id, Prep_Items_Window.actor_id);
                        break;
                    case PrepItemsInputResults.Status:
                        Status_Window = new Window_Status(
                            Global.battalion.actors, Prep_Items_Window.actor_id, true);
                        break;
                    case PrepItemsInputResults.Supply:
                        Supply_Window = new Window_Supply(Prep_Items_Window.actor_id);
                        break;
                    case PrepItemsInputResults.Shop:
                        Global.game_temp.call_shop();
                        open_shop_menu();
                        break;
                    case PrepItemsInputResults.Closing:
                        Setup_Window.refresh();
                        break;
                }
            }
        }

        protected void update_preptrade_menu()
        {
            if (Prep_Trade_Window.closed)
            {
                Prep_Trade_Window = null;
                Prep_Items_Window.active = true;
            }
            else
            {
                if (Prep_Trade_Window.ready)
                {
                    if (Prep_Trade_Window.is_help_active)
                    {
                        if (Prep_Trade_Window.is_canceled())
                            Prep_Trade_Window.close_help();
                    }
                    else
                    {
                        if (Prep_Trade_Window.getting_help())
                            Prep_Trade_Window.open_help();
                        else if (Prep_Trade_Window.is_canceled())
                        {
                            if (Prep_Trade_Window.mode > 0)
                            {
                                Global.game_system.play_se(System_Sounds.Cancel);
                                Prep_Trade_Window.cancel();
                            }
                            else
                            {
                                Global.game_system.play_se(System_Sounds.Cancel);
                                Prep_Trade_Window.staff_fix();
                                Prep_Trade_Window.close();
                                Prep_Items_Window.refresh_trade();
                            }
                            return;
                        }
                        else if (Prep_Trade_Window.is_selected())
                        {
                            Prep_Trade_Window.enter();
                        }
                    }
                }
            }
        }
        #endregion

        protected void clear_prep_menus()
        {
            Setup_Window = null;
            CheckMap_Window = null;
            Prep_PickUnits_Window = null;
            Prep_Items_Window = null;
            Prep_Trade_Window = null;
        }

        #region Draw
        protected void draw_preparations(SpriteBatch sprite_batch)
        {
            if (CheckMap_Window != null || Setup_Window != null)
            {
                if (CheckMap_Window != null)
                {
                    if (Setup_Window == null || (!Setup_Window.visible && !Setup_Window.pressed_start))
                        CheckMap_Window.draw(sprite_batch);
                    else if (Config.PREP_MAP_BACKGROUND)
                        CheckMap_Window.draw_map_darken(sprite_batch);
                }

                if (Setup_Window != null)
                {
                    if (Global.game_system.home_base)
                    {
                        if ((Prep_Items_Window == null || !Prep_Items_Window.visible) &&
                                (Prep_Support_Window == null || !Prep_Support_Window.visible))
                            Setup_Window.draw(sprite_batch);

                        if (Prep_Items_Window != null)
                        {
                            if ((Prep_Trade_Window == null || !Prep_Trade_Window.visible) &&
                                    (Supply_Window == null || !Supply_Window.visible))
                                Prep_Items_Window.draw(sprite_batch);

                            if (Prep_Trade_Window != null)
                                Prep_Trade_Window.draw(sprite_batch);
                            if (Supply_Window != null)
                                Supply_Window.draw(sprite_batch);
                        }

                        if (Prep_Support_Window != null)
                            Prep_Support_Window.draw(sprite_batch);
                        if (Base_Convo_Window != null)
                            Base_Convo_Window.draw(sprite_batch);
                        if (Leave_Base_Confirm_Window != null)
                            Leave_Base_Confirm_Window.draw(sprite_batch);
                    }
                    else
                    {
                        if ((Prep_PickUnits_Window == null || (!Prep_PickUnits_Window.visible && !Prep_PickUnits_Window.pressed_start)) &&
                                (Prep_Items_Window == null || !Prep_Items_Window.visible))
                            Setup_Window.draw(sprite_batch);

                        if (Prep_PickUnits_Window != null)
                            Prep_PickUnits_Window.draw(sprite_batch);
                        if (Prep_Items_Window != null)
                        {
                            if ((Prep_Trade_Window == null || !Prep_Trade_Window.visible) &&
                                    (Supply_Window == null || !Supply_Window.visible))
                                Prep_Items_Window.draw(sprite_batch);

                            if (Prep_Trade_Window != null)
                                Prep_Trade_Window.draw(sprite_batch);
                            if (Supply_Window != null)
                                Supply_Window.draw(sprite_batch);
                        }
                        if (AuguryWindow != null)
                            AuguryWindow.draw(sprite_batch);
                    }
                }
            }
        }
        #endregion
    }
}
