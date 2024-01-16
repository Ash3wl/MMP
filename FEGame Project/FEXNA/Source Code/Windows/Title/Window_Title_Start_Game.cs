using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Help;
using FEXNA.Windows.UserInterface;
using FEXNA_Library;

namespace FEXNA
{
    class Window_Title_Start_Game : Graphic_Object, Windows.ISelectionMenu
    {
        const int PANEL_WIDTH = 240;

        //private int Index, Page, Max_Page; //Debug
        private int Page, Max_Page;
        private int Move_File_Id, Move_Index, Move_Page;
        private bool Active = true, Moving = false, Copying = false, WaitingForIO;
        private bool Selecting_Style = false, Selecting_Difficulty = false;
        private List<Inputs> locked_inputs = new List<Inputs>();
        private Suspend_Info_Panel Suspend_Panel;
        private StartGame_Info_Panel[] Panels = new StartGame_Info_Panel[Config.SAVES_PER_PAGE];
        private Mode_Style_Info_Panel[] Style_Panels;
        private Difficulty_Info_Panel[] Difficulty_Panels;
        private UINodeSet<StartGame_Info_Panel> PanelNodes;
        private UINodeSet<Mode_Style_Info_Panel> StyleNodes;
        private UINodeSet<Difficulty_Info_Panel> DifficultyNodes;
        private Hand_Cursor Cursor, Move_Cursor;
        private Page_Arrow Left_Page_Arrow, Right_Page_Arrow;

        private Button_Description CancelButton;

        private int SelectedIndex = -1;
        private bool Canceled;

        #region Accessors
        public int file_id
        {
            get { return PanelNodes.ActiveNodeIndex + Page * Config.SAVES_PER_PAGE + 1; }
            set
            {
                int index;
                if (value == -1)
                {
                    index = 0;
                    Page = 0;
                }
                else
                {
                    index = (value - 1) % Config.SAVES_PER_PAGE;
                    Page = (value - 1) / Config.SAVES_PER_PAGE;
                }
                PanelNodes.set_active_node(PanelNodes[index]);

                for (int i = 0; i < Panels.Length; i++)
                    Panels[i].active = false;
                Panels[index].active = true;

                refresh_page();
                Cursor.force_loc(PanelNodes[index].loc);
            }
        }

        public bool active
        {
            set
            {
                Active = value;
                for (int i = 0; i < Panels.Length; i++)
                {
                    if (Active)
                        Panels[i].tint = new Color(255, 255, 255, 255);
                    else
                    {
                        int alpha = i == PanelNodes.ActiveNodeIndex ? 255 : 160;
                        Panels[i].tint = new Color(alpha, alpha, alpha, 255);
                    }
                }
            }
        }

        public int move_file_id { get { return Move_File_Id; } }
        public bool moving_file
        {
            get { return Moving; }
            set
            {
                Moving = value;
                Move_File_Id = file_id;
                Move_Index = PanelNodes.ActiveNodeIndex;
                Move_Page = Page;
                update_move_darken();
            }
        }
        public bool copying
        {
            get { return Copying; }
            set
            {
                Copying = value;
                moving_file = value;
            }
        }
        public bool waiting_for_io
        {
            get { return WaitingForIO; }
            set
            {
                WaitingForIO = value;
            }
        }

        public bool selecting_style
        {
            get { return Selecting_Style; }
            set
            {
                Selecting_Style = value;
                if (Selecting_Style)
                {
                    StyleNodes.set_active_node(StyleNodes[(int)Mode_Styles.Standard]);
                    for (int i = 0; i < Style_Panels.Length; i++)
                        Style_Panels[i].active = false;
                    StyleNodes.ActiveNode.active = true;
                }
            }
        }

        public bool selecting_difficulty
        {
            get { return Selecting_Difficulty; }
            set
            {
                Selecting_Difficulty = value;
                if (Selecting_Difficulty)
                {
                    DifficultyNodes.set_active_node(DifficultyNodes[(int)Difficulty_Modes.Normal]);
                    for (int i = 0; i < Difficulty_Panels.Length; i++)
                        Difficulty_Panels[i].active = false;
                    DifficultyNodes.ActiveNode.active = true;
                }
            }
        }

        public Mode_Styles SelectedStyle
        {
            get { return (Mode_Styles)2; }//(Mode_Styles)StyleNodes.ActiveNodeIndex; }
        }
        #endregion

        public Window_Title_Start_Game(int file_id)
        {
            if (file_id == -1)
            {
                Page = 0;
                Max_Page = 0;
            }
            else
            {
                Page = (file_id - 1) / Config.SAVES_PER_PAGE;
                Max_Page = Math.Min(Config.SAVE_PAGES,
                    ((Global.save_files_info.Keys.Max() - 1) / Config.SAVES_PER_PAGE) + 1);
            }

            initialize(file_id);

            if (file_id == -1)
            {
                PanelNodes.set_active_node(Panels[0]); //Debug
            }
            else
            {
                PanelNodes.set_active_node(Panels[(file_id - 1) % Config.SAVES_PER_PAGE]); //Debug
            }
            refresh_panel_locations();
        }

        private void initialize(int fileId)
        {
            Cursor = new Hand_Cursor();
            Cursor.draw_offset = new Vector2(-20, 0);
            Cursor.stereoscopic = Config.TITLE_MENU_DEPTH;
            Move_Cursor = new Hand_Cursor();
            Move_Cursor.draw_offset = new Vector2(-20, 0);
            Move_Cursor.tint = new Color(160, 160, 160, 255);
            Move_Cursor.stereoscopic = Config.TITLE_MENU_DEPTH;

            for (int i = 0; i < Panels.Length; i++)
            {
                Panels[i] = new StartGame_Info_Panel(Page * Config.SAVES_PER_PAGE + i + 1, PANEL_WIDTH);
                Panels[i].stereoscopic = Config.TITLE_MENU_DEPTH;
            }

            Style_Panels = new Mode_Style_Info_Panel[Enum_Values.GetEnumCount(typeof(Mode_Styles))];
            for (int i = 0; i < Style_Panels.Length; i++)
            {
                Style_Panels[i] = new Mode_Style_Info_Panel((Mode_Styles)i);
                Style_Panels[i].stereoscopic = Config.TITLE_MENU_DEPTH;
            }
            Style_Panels[0].active = true;

            Difficulty_Panels = new Difficulty_Info_Panel[Enum_Values.GetEnumCount(typeof(Difficulty_Modes))];
            for (int i = 0; i < Difficulty_Panels.Length; i++)
            {
                Difficulty_Panels[i] = new Difficulty_Info_Panel((Difficulty_Modes)i);
                Difficulty_Panels[i].stereoscopic = Config.TITLE_MENU_DEPTH;
            }
            Difficulty_Panels[0].active = true;

            refresh_panel_locations();
            PanelNodes = new UINodeSet<StartGame_Info_Panel>(Panels);
            PanelNodes.CursorMoveSound = System_Sounds.Menu_Move1;
            PanelNodes.WrapVerticalMove = true;

            StyleNodes = new UINodeSet<Mode_Style_Info_Panel>(Style_Panels);
            DifficultyNodes = new UINodeSet<Difficulty_Info_Panel>(Difficulty_Panels);

            // Page Arrows
            Left_Page_Arrow = new Page_Arrow();
            Left_Page_Arrow.loc = new Vector2(-16, 64);
            Left_Page_Arrow.stereoscopic = Config.TITLE_MENU_DEPTH - 1;
            Left_Page_Arrow.ArrowClicked += Left_Page_Arrow_ArrowClicked;
            Right_Page_Arrow = new Page_Arrow();
            Right_Page_Arrow.loc = new Vector2(PANEL_WIDTH - 16, 64);
            Right_Page_Arrow.mirrored = true;
            Right_Page_Arrow.stereoscopic = Config.TITLE_MENU_DEPTH - 1;
            Right_Page_Arrow.ArrowClicked += Right_Page_Arrow_ArrowClicked;

            create_cancel_button();
        }

        private void create_cancel_button()
        {
            CancelButton = Button_Description.button(Inputs.B,
                Config.WINDOW_WIDTH - 64);
            CancelButton.description = "Cancel";
            CancelButton.stereoscopic = Config.TITLE_MENU_DEPTH;
        }

        private void refresh_panel_locations()
        {
            int offset = 0;
            for (int i = 0; i < Panels.Length; i++)
            {
                Panels[i].loc = new Vector2(0, offset);
                offset += Panels[i].height - 8;
            }

            offset = -24;
            for (int i = 0; i < Style_Panels.Length; i++)
            {
                Style_Panels[i].loc = new Vector2(
                    (Mode_Style_Info_Panel.WIDTH - PANEL_WIDTH) / 2, offset);
                offset += Style_Panels[i].height + 8;
            }
            
            offset = 0;
            for (int i = 0; i < Difficulty_Panels.Length; i++)
            {
                Difficulty_Panels[i].loc = new Vector2(
                    (Difficulty_Info_Panel.WIDTH - PANEL_WIDTH) / 2, offset);
                offset += Difficulty_Panels[i].height + 8;
            }
        }

        public void refresh_page()
        {
            for (int i = 0; i < Panels.Length; i++)
                Panels[i].set_data(Page * Config.SAVES_PER_PAGE + i + 1);
            Left_Page_Arrow.visible = Page > 0;
            Right_Page_Arrow.visible = Page < Max_Page;

            refresh_panel_locations();
        }

        public void update()
        {
            update(Active);
        }
        public void update(bool input)
        {
            Cursor.update();
            Left_Page_Arrow.update();
            Right_Page_Arrow.update();

            if (input)
                update_input();
            update_ui(input);

            if (Moving)
                update_move_darken();
        }

        private void update_move_darken()
        {
            for (int i = 0; i < Panels.Length; i++)
            {
                if (!Moving ||
                        ((Page == Move_Page && i == Move_Index) ||
                        (i == PanelNodes.ActiveNodeIndex && !Global.save_files_info.ContainsKey(i + Page * Config.SAVES_PER_PAGE + 1))))
                    Panels[i].tint = new Color(255, 255, 255, 255);
                else
                    Panels[i].tint = new Color(160, 160, 160, 255);
            }
        }

        private void update_input()
        {
            if (Selecting_Style)
            {
                /*
                if (Global.Input.triggered(Inputs.Up))
                    move_up();
                if (Global.Input.triggered(Inputs.Down))
                    move_down();*/
            }
            else
            {
                Left_Page_Arrow.update_input(-this.loc);
                Right_Page_Arrow.update_input(-this.loc);

                /*
                if (Global.Input.repeated(Inputs.Up) && !locked_inputs.Contains(Inputs.Up))
                    move_up();
                if (Global.Input.repeated(Inputs.Down) && !locked_inputs.Contains(Inputs.Down))
                    move_down();*/

                if (!Global.Input.pressed(Inputs.Up) && !Global.Input.pressed(Inputs.Down))
                {
                    if (Page > 0 && (Global.Input.repeated(Inputs.Left) ||
                        Global.Input.gesture_triggered(TouchGestures.SwipeRight)))
                    {
                        Global.game_system.play_se(System_Sounds.Menu_Move2);
                        Page--;
                        refresh_page();
                    }
                    if (Page < Max_Page && (Global.Input.repeated(Inputs.Right) ||
                        Global.Input.gesture_triggered(TouchGestures.SwipeLeft)))
                    {
                        Global.game_system.play_se(System_Sounds.Menu_Move2);
                        Page++;
                        refresh_page();
                    }
                }
            }
            if (!Global.Input.pressed(Inputs.Up))
                locked_inputs.Remove(Inputs.Up);
            if (!Global.Input.pressed(Inputs.Down))
                locked_inputs.Remove(Inputs.Down);
        }

        protected virtual void update_ui(bool input)
        {
            reset_selected();

            int index = PanelNodes.ActiveNodeIndex;
            PanelNodes.Update(input && !Selecting_Difficulty && !Selecting_Style, -this.loc);
            if (index != PanelNodes.ActiveNodeIndex)
            {
                Panels[index].active = false;
                PanelNodes.ActiveNode.active = true;
                refresh_panel_locations();
            }

            index = StyleNodes.ActiveNodeIndex;
            StyleNodes.Update(input && !Selecting_Difficulty && Selecting_Style, -this.loc);
            if (index != StyleNodes.ActiveNodeIndex)
            {
                Style_Panels[index].active = false;
                StyleNodes.ActiveNode.active = true;
            }

            index = DifficultyNodes.ActiveNodeIndex;
            DifficultyNodes.Update(input && Selecting_Difficulty, -this.loc);
            if (index != DifficultyNodes.ActiveNodeIndex)
            {
                Difficulty_Panels[index].active = false;
                DifficultyNodes.ActiveNode.active = true;
            }

            CancelButton.Update(true);

            if (input)
            {
                if (Cursor.target_loc != PanelNodes.ActiveNode.loc)
                    Cursor.set_loc(PanelNodes.ActiveNode.loc);

                if (Selecting_Difficulty)
                {
                    var difficulty_index = DifficultyNodes.consume_triggered(
                        Inputs.A, MouseButtons.Left, TouchGestures.Tap);
                    if (difficulty_index.IsSomething)
                    {
                        SelectedIndex = difficulty_index;
                        DifficultyNodes.set_active_node(DifficultyNodes[SelectedIndex]);
                    }
                }
                else if (Selecting_Style)
                {
                    var style_index = StyleNodes.consume_triggered(
                        Inputs.A, MouseButtons.Left, TouchGestures.Tap);
                    if (style_index.IsSomething)
                    {
                        SelectedIndex = style_index;
                        StyleNodes.set_active_node(StyleNodes[SelectedIndex]);
                    }
                }
                else
                {
                    var file_index = PanelNodes.consume_triggered(
                        Inputs.A, MouseButtons.Left, TouchGestures.Tap);
                    if (file_index.IsSomething)
                    {
                        SelectedIndex = file_index;
                    }
                }

                if (Global.Input.triggered(Inputs.B) ||
                        CancelButton.consume_trigger(MouseButtons.Left) ||
                        CancelButton.consume_trigger(TouchGestures.Tap))
                    Canceled = true;
            }
            if (!Canceled &&
                    (CancelButton.consume_trigger(MouseButtons.Left) ||
                    CancelButton.consume_trigger(TouchGestures.Tap)))
                Canceled = true;
        }

        public Maybe<int> selected_index()
        {
            if (SelectedIndex < 0)
                return Maybe<int>.Nothing;
            return SelectedIndex;
        }

        public bool is_selected()
        {
            return SelectedIndex >= 0;
        }

        public bool is_canceled()
        {
            return Canceled;
        }

        public void reset_selected()
        {
            SelectedIndex = -1;
            Canceled = false;
        }

        private void Left_Page_Arrow_ArrowClicked(object sender, EventArgs e)
        {
            if (Page > 0)
            {
                Global.game_system.play_se(System_Sounds.Menu_Move2);
                Page--;
                refresh_page();
            }
        }

        private void Right_Page_Arrow_ArrowClicked(object sender, EventArgs e)
        {
            if (Page < Max_Page)
            {
                Global.game_system.play_se(System_Sounds.Menu_Move2);
                Page++;
                refresh_page();
            }
        }

        internal void preview_suspend()
        {
            int save_id = Page * Config.SAVES_PER_PAGE + PanelNodes.ActiveNodeIndex + 1;
            if (Global.suspend_files_info != null && Global.suspend_files_info.ContainsKey(save_id))
            {
                Suspend_Panel = new Suspend_Info_Panel(
                    false, Global.suspend_files_info[save_id]);
                Suspend_Panel.loc = new Vector2(56, 16 + PanelNodes.ActiveNodeIndex * 24);
                Suspend_Panel.stereoscopic = Config.TITLE_MENU_DEPTH;
            }
        }

        internal void preview_checkpoint()
        {
            int save_id = Page * Config.SAVES_PER_PAGE + PanelNodes.ActiveNodeIndex + 1;
            if (Global.checkpoint_files_info != null && Global.checkpoint_files_info.ContainsKey(save_id))
            {
                Suspend_Panel = new Suspend_Info_Panel(
                    false, Global.checkpoint_files_info[save_id]);
                Suspend_Panel.loc = new Vector2(56, 16 + PanelNodes.ActiveNodeIndex * 24);
                Suspend_Panel.stereoscopic = Config.TITLE_MENU_DEPTH;
            }
        }

        internal void close_preview()
        {
            Suspend_Panel = null;
        }

        #region Movement
        /* //Debug
        private void move_down()
        {
            Global.game_system.play_se(System_Sounds.Menu_Move1);

            if (Selecting_Difficulty)
            {
                Difficulty_Panels[Difficulty_Index].active = false;
                Difficulty_Index = (Difficulty_Index + 1) % Difficulty_Panels.Length;
                Difficulty_Panels[Difficulty_Index].active = true;
            }
            else if (Selecting_Style)
            {
                Style_Panels[Style_Index].active = false;
                Style_Index = (Style_Index + 1) % Style_Panels.Length;
                Style_Panels[Style_Index].active = true;
            }
            else
            {
                Panels[Index].active = false;
                Index = (Index + 1) % Config.SAVES_PER_PAGE;
                Panels[Index].active = true;
                Cursor.set_loc(new Vector2(0, Index * 24));

                if (Index == Config.SAVES_PER_PAGE - 1)
                    locked_inputs.Add(Inputs.Down);
            }

            refresh_panel_locations();
        }
        private void move_up()
        {
            Global.game_system.play_se(System_Sounds.Menu_Move1);

            if (Selecting_Difficulty)
            {
                Difficulty_Panels[Difficulty_Index].active = false;
                Difficulty_Index = (Difficulty_Index + Difficulty_Panels.Length - 1) % Difficulty_Panels.Length;
                Difficulty_Panels[Difficulty_Index].active = true;
            }
            else if (Selecting_Style)
            {
                Style_Panels[Style_Index].active = false;
                Style_Index = (Style_Index + Style_Panels.Length - 1) % Style_Panels.Length;
                Style_Panels[Style_Index].active = true;
            }
            else
            {
                Panels[Index].active = false;
                Index = (Index - 1 + Config.SAVES_PER_PAGE) % Config.SAVES_PER_PAGE;
                Panels[Index].active = true;
                Cursor.set_loc(new Vector2(0, Index * 24));

                if (Index == 0)
                    locked_inputs.Add(Inputs.Up);
            }

            refresh_panel_locations();
        }

        private void move_to(int index)
        {
            Global.game_system.play_se(System_Sounds.Menu_Move1);

            Panels[Index].active = false;
            Index = index;
            Panels[Index].active = true;
            Cursor.set_loc(new Vector2(0, Index * 24));

            refresh_panel_locations();
        }*/
        #endregion

        public void draw(SpriteBatch sprite_batch)
        {
            if (Selecting_Difficulty)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                for (int i = 0; i < Difficulty_Panels.Length; i++)
                {
                    Difficulty_Panels[i].Draw(sprite_batch, -loc);
                }
                sprite_batch.End();
            }
            else if (Selecting_Style)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                for (int i = 0; i < Style_Panels.Length; i++)
                {
                    Style_Panels[i].Draw(sprite_batch, -loc);
                }
                sprite_batch.End();
            }
            else
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                for (int i = 0; i < Panels.Length; i++)
                {
                    Panels[i].Draw(sprite_batch, -loc);
                    if (Move_Index == i && Move_Page == Page && Moving)
                        Move_Cursor.draw(sprite_batch, -(loc + Panels[i].loc));
                }
                Left_Page_Arrow.draw(sprite_batch, -loc);
                Right_Page_Arrow.draw(sprite_batch, -loc);
                if (Active)
                    Cursor.draw(sprite_batch, -loc);
                sprite_batch.End();

                if (Suspend_Panel != null)
                    Suspend_Panel.Draw(sprite_batch);
            }

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (Input.ControlScheme != ControlSchemes.Buttons)
                CancelButton.Draw(sprite_batch);
            sprite_batch.End();
        }
    }
}
