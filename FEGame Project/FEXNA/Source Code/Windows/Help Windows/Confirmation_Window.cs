﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA_Library;

namespace FEXNA.Windows.UserInterface.Command
{
    class Window_Confirmation : Window_Help, ISelectionMenu
    {
        protected UICursor<TextUINode> Cursor;
        protected UINodeSet<TextUINode> Choices;
        protected bool Skip = false;
        protected bool Active = true;

        protected int SelectedIndex = -1;
        protected bool Canceled; 

        #region Accessors
        public int index
        {
            get { return Choices.ActiveNodeIndex; }
            set
            {
                Choices.set_active_node(Choices[value]);
                Cursor.update();
                Cursor.move_to_target_loc();
            }
        }

        public bool is_ready { get { return Help_String.Length == 0 && !Skip; } }

        public Vector2 current_cursor_loc
        {
            get { return Cursor.loc + loc + new Vector2(-8, 8); }
            set { Cursor.loc = value - (loc + new Vector2(-8, 8)); }
        }

        public bool active
        {
            get { return Active; }
            set { Active = value; }
        }

        protected virtual System_Sounds talk_sound { get { return System_Sounds.Talk_Boop; } }
        #endregion

        public Window_Confirmation()
        {
            initialize();
        }

        protected virtual void initialize()
        {
            texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Message_Window");
            Src_Rect = new Rectangle(0, 0, 0, 0);
            offset = new Vector2(-8, 3);
            Background = new Text_Box(48, 32);
            loc = new Vector2(48, 48);
        }

        public virtual void add_choice(string str, Vector2 loc)
        {
            List<TextUINode> choices = Choices == null ?
                new List<TextUINode>() : Choices.ToList();

            var text = new FE_Text();
            text.Font = "FE7_Convo";
            text.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Convo_Black");
            text.text = str;

            var node = new TextUINode("", text, text.text_width);
            node.loc = loc;
            choices.Add(node);

            Choices = new UINodeSet<TextUINode>(choices);
            Cursor = new UICursor<TextUINode>(Choices);
            Cursor.hide_when_using_mouse(false);
            // Resize if needed
            int width = Font_Data.text_width(str, "FE7_Convo");
            width = width + (width % 8 == 0 ? 0 : (8 - width % 8)) + 16 + (int)loc.X;
            if (width > Size.X)
                size = new Vector2(width, Size.Y);
            Cursor.move_to_target_loc();
        }

        protected override bool skip()
        {
            return Skip;
        }

        protected override int text_speed()
        {
            switch (Global.game_options.text_speed)
            {
                case 0:
                    return 9;
                case 1:
                    return 5;
                case 2:
                    return 2;
            }
            return 1;
        }

        protected override int characters_at_once()
        {
            return 1;
        }

        protected override void add_char(char text)
        {
            talk_boop();
            base.add_char(text);
        }

        protected virtual void talk_boop()
        {
            if (!Skip || Help_Text.text.Length == 0)
                Global.game_system.play_se(talk_sound);
        }

        public override void update()
        {
            if (Active)
                update_input();
            update_ui(Active && is_ready);
            base.update();
        }

        protected virtual void update_input()
        {
            if (Help_String.Length > 0 &&
                    (Global.Input.triggered(Inputs.A) ||
                    Global.Input.triggered(Inputs.B) ||
                    Global.Input.mouse_click(MouseButtons.Left) ||
                    Global.Input.gesture_triggered(TouchGestures.Tap)))
                Skip = true;
            if (Help_String.Length == 0 && Skip)
                Skip = false;
        }

        protected virtual void update_ui(bool input)
        {
            reset_selected();

            if (Choices != null)
            {
                Choices.Update(input, -(loc + draw_vector() + new Vector2(8, 8)));
                Cursor.update();

                if (input)
                {
                    var selected = Choices.consume_triggered(
                        Inputs.A, MouseButtons.Left, TouchGestures.Tap);
                    if (selected.IsSomething)
                        SelectedIndex = selected;

                    if (Global.Input.triggered(Inputs.B))
                        Canceled = true;
                }
            }
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

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            if (visible)
            {
                base.draw(sprite_batch, draw_offset);
                if (is_ready)
                {
                    sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    if (Choices != null)
                        Choices.Draw(sprite_batch, draw_offset - (loc + draw_vector() + new Vector2(8, 8)));
                    draw_cursor(sprite_batch, draw_offset);
                    sprite_batch.End();
                }
            }
        }

        protected virtual void draw_cursor(SpriteBatch sprite_batch, Vector2 draw_offset)
        {
            if (Active)
                Cursor.draw(sprite_batch, draw_offset - (loc + draw_vector() + new Vector2(-8, 8)));
        }
    }
}
