using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA
{
    class Page_Arrow : Spark
    {
        public readonly static string FILENAME = "Page_Arrows";

        public Page_Arrow()
        {
            Loop = true;
            Timer_Maxes = new int[] { 8, 8, 8, 8, 8, 8 };
            Frames = new Vector2(6, 1);
            texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/" + FILENAME);
        }

        internal void twirling_update()
        {
            for (int i = 0; i < 6; i++)
                update();
        }

        internal EventHandler ArrowClicked;

        public void update_input(Vector2 draw_offset = default(Vector2))
        {
            if (Input.ControlScheme == ControlSchemes.Buttons || !visible)
                return;

            Vector2 loc = (this.loc + this.draw_offset) - draw_offset;
            Rectangle arrow_rect = new Rectangle(
                (int)loc.X, (int)loc.Y, src_rect.Width, src_rect.Height);

            tint = Color.White;

            // Mouse triggered
            if (Global.Input.mouse_clicked_rectangle(MouseButtons.Left,
                    arrow_rect, loc, this.offset, this.angle, mirrored))
            {
                if (ArrowClicked != null)
                    ArrowClicked(this, new EventArgs());
            }
            // Tapped
            else if (Global.Input.gesture_rectangle(TouchGestures.Tap,
                arrow_rect, loc, this.offset, this.angle, mirrored))
            {
                if (ArrowClicked != null)
                    ArrowClicked(this, new EventArgs());
            }
            else if (Global.Input.mouse_in_rectangle(
                    arrow_rect, loc, this.offset, this.angle, mirrored) ||
                Global.Input.touch_rectangle(
                    Services.Input.InputStates.Pressed,
                    arrow_rect, loc, this.offset, this.angle, mirrored))
            {
                tint = new Color(0.6f, 0.7f, 0.8f, 1f);
            }
        }
    }
}
