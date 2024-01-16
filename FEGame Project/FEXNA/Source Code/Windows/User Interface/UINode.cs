using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EnumExtension;

namespace FEXNA.Windows.UserInterface
{
    abstract class UINode : Stereoscopic_Graphic_Object
    {
        internal Vector2 Size;
        private bool LeftMouseDown, RightMouseDown;
        private HashSet<Inputs> Triggers = new HashSet<Inputs>();
        private HashSet<MouseButtons> MouseTriggers = new HashSet<MouseButtons>();
        private HashSet<TouchGestures> TouchTriggers = new HashSet<TouchGestures>();

        internal Vector2 CenterPoint { get { return loc + Size / 2; } }
        internal virtual bool Enabled { get { return true; } }

        protected abstract IEnumerable<Inputs> ValidInputs { get; }
        protected abstract bool RightClickActive { get; }

        public override string ToString()
        {
            return string.Format("UINode: {0}", this.loc);
        }

        internal void Update(bool input,
            Vector2 draw_offset = default(Vector2))
        {
            Update((UINodeSet<UINode>)null, input ? ControlSet.All : ControlSet.None, draw_offset);
        }
        internal void Update<T>(UINodeSet<T> nodes, bool input,
            Vector2 draw_offset = default(Vector2)) where T : UINode
        {
            Update(nodes, input ? ControlSet.All : ControlSet.None, draw_offset);
        }
        internal void Update<T>(UINodeSet<T> nodes, ControlSet input,
            Vector2 draw_offset = default(Vector2)) where T : UINode
        {
            // If wrong type
            if (!(this is T))
                return;
            // If not in node set
            if (nodes != null && !nodes.Contains(this as T))
                return;

            mouse_off_graphic();
            if (Enabled)
            {
                if (Input.IsControllingOnscreenMouse)
                {
                    UpdateMouse<T>(nodes, input, draw_offset);
                }
                else if (Input.ControlScheme == ControlSchemes.Touch)
                {
                    UpdateTouch<T>(nodes, input, draw_offset);
                }
                else if (is_active_node<T>(nodes))
                {
                    UpdateButtons<T>(input);
                }
            }

            bool active_node = is_active_node<T>(nodes);
            update_graphics(active_node);
        }

        private bool is_active_node<T>(UINodeSet<T> nodes) where T : UINode
        {
            return nodes == null || nodes.ActiveNode == this;
        }

        private void UpdateMouse<T>(
            UINodeSet<T> nodes,
            ControlSet input,
            Vector2 draw_offset) where T : UINode
        {
            if (input.HasEnumFlag(ControlSet.MouseButtons))
            {
                bool input_used = update_mouse_input(
                    ref LeftMouseDown, MouseButtons.Left, draw_offset);
                if (RightClickActive && !input_used)
                    update_mouse_input(
                        ref RightMouseDown, MouseButtons.Right, draw_offset);
            }

            if (input.HasEnumFlag(ControlSet.MouseMove))
            {
                if (OnScreenBounds(draw_offset).Contains(
                    (int)Global.Input.mousePosition.X,
                    (int)Global.Input.mousePosition.Y))
                {
                    if (nodes != null)
                        nodes.set_active_node(this as T);

                    if (LeftMouseDown || RightMouseDown)
                        mouse_click_graphic();
                    else
                        mouse_highlight_graphic();
                }
            }
        }

        private void UpdateTouch<T>(
            UINodeSet<T> nodes,
            ControlSet input,
            Vector2 draw_offset) where T : UINode
        {
            if (input.HasEnumFlag(ControlSet.Touch))
            {
                if (Global.Input.gesture_rectangle(
                    TouchGestures.Tap, OnScreenBounds(draw_offset)))
                {
                    if (nodes != null)
                        nodes.set_active_node(this as T);
                    TouchTriggers.Add(TouchGestures.Tap);
                }
                else if (Global.Input.gesture_rectangle(
                    TouchGestures.LongPress, OnScreenBounds(draw_offset)))
                {
                    if (nodes != null)
                        nodes.set_active_node(this as T);
                    TouchTriggers.Add(TouchGestures.LongPress);
                }
                else if (Global.Input.touch_rectangle(
                    Services.Input.InputStates.Pressed,
                    OnScreenBounds(draw_offset),
                    false))
                {
                    if (nodes != null)
                        nodes.set_active_node(this as T);
                    mouse_click_graphic();
                }
            }
        }

        private void UpdateButtons<T>(ControlSet input) where T : UINode
        {
            if (input.HasEnumFlag(ControlSet.Buttons))
                foreach (Inputs key in this.ValidInputs)
                    if (Global.Input.triggered(key))
                    {
                        Triggers.Add(key);
                    }
        }

        private bool update_mouse_input(ref bool mouseDown, MouseButtons button,
            Vector2 draw_offset)
        {
            bool input_used = false;
            if (Global.Input.mouse_down_rectangle(
                button, OnScreenBounds(draw_offset), false))
            {
                input_used = true;
                mouseDown = true;
            }
            bool released = !Global.Input.mouse_pressed(button, false);

            if (mouseDown && released)
            {
                mouseDown = false;
                input_used = false;

                if (OnScreenBounds(draw_offset).Contains(
                    (int)Global.Input.mousePosition.X,
                    (int)Global.Input.mousePosition.Y))
                {
                    // Consume the input of this button
                    if (!Global.Input.consume_input(button))
                    {
                        MouseTriggers.Add(button);
                        input_used = true;
                    }
                }
            }

            return input_used;
        }

        protected abstract void update_graphics(bool activeNode);

        public bool consume_trigger(MouseButtons button)
        {
            bool result = MouseTriggers.Contains(button);
            MouseTriggers.Remove(button);
            return result;
        }
        public bool consume_trigger(Inputs input)
        {
#if DEBUG
            if (!ValidInputs.Contains(input))
                throw new ArgumentException(string.Format(
                    "Tried to test a UINode for input with \"Inputs.{0}\", a key it isn't set to process",
                    input.ToString()));
#endif
            bool result = Triggers.Contains(input);
            Triggers.Remove(input);
            return result;
        }
        public bool consume_trigger(TouchGestures gesture)
        {
            bool result = TouchTriggers.Contains(gesture);
            TouchTriggers.Remove(gesture);
            return result;
        }

        internal void clear_triggers()
        {
            Triggers.Clear();
            MouseTriggers.Clear();
        }

        private Rectangle OnScreenBounds(Vector2 draw_offset)
        {
            Vector2 loc = this.loc + this.draw_offset - draw_offset;
            return new Rectangle((int)loc.X, (int)loc.Y,
                (int)Size.X, (int)Size.Y);
        }

        protected abstract void mouse_off_graphic();
        protected abstract void mouse_highlight_graphic();
        protected abstract void mouse_click_graphic();

        public abstract void Draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2));
    }
}
