using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA.Windows.Map
{
    abstract class Map_Window_Base
    {
        const int BLACK_SCEEN_FADE_TIMER = 4;
        const int BLACK_SCREEN_HOLD_TIMER = 8;

        protected bool Closing = false;
        protected bool Visible = false;
        protected int Black_Screen_Fade_Timer = BLACK_SCEEN_FADE_TIMER;
        protected int Black_Screen_Hold_Timer = BLACK_SCREEN_HOLD_TIMER;
        protected int Black_Screen_Timer;
        protected int Map_Sprite_Frame = -1;

        protected Sprite Background;
        protected Sprite Black_Screen;
        protected Window_Help Help_Window;
        protected Hand_Cursor Cursor;

        #region Accessors
        public bool closing { get { return Closing; } }

        public bool closed { get { return Black_Screen_Timer <= 0 && Closing; } }

        public bool visible { get { return Visible; } }
        #endregion

        public Map_Window_Base()
        {
            set_black_screen_time();
        }

        protected virtual void set_black_screen_time()
        {
            Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
        }

        #region Update
        public virtual void update()
        {
            // Black Screen
            update_black_screen();
            // Inputs
            update_input();
            if (Background != null)
                Background.update();
        }

        protected virtual void update_input() { }

        protected virtual void close()
        {
            Closing = true;
            Black_Screen_Timer = Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2);
            if (Black_Screen != null)
                Black_Screen.visible = true;
        }

        protected void update_black_screen()
        {
            if (Black_Screen != null)
                Black_Screen.visible = Black_Screen_Timer > 0;
            if (Black_Screen_Timer > 0)
            {
                Black_Screen_Timer--;
                if (Black_Screen != null)
                {
                    if (Black_Screen_Timer > Black_Screen_Fade_Timer + (Black_Screen_Hold_Timer / 2))
                        Black_Screen.TintA = (byte)Math.Min(255,
                            (Black_Screen_Hold_Timer + (Black_Screen_Fade_Timer * 2) - Black_Screen_Timer) * (256 / Black_Screen_Fade_Timer));
                    else
                        Black_Screen.TintA = (byte)Math.Min(255,
                            Black_Screen_Timer * (256 / Black_Screen_Fade_Timer));
                }
                if (Black_Screen_Timer == Black_Screen_Fade_Timer + (Black_Screen_Hold_Timer / 2))
                {
                    black_screen_switch();
                }
            }
        }

        protected virtual void black_screen_switch()
        {
            Visible = !Visible;
        }
        #endregion

        #region Draw
        public virtual void draw(SpriteBatch sprite_batch)
        {
            if (Visible)
            {
                // Background
                draw_background(sprite_batch);

                draw_window(sprite_batch);
            }
            // Black Screen
            if (Black_Screen != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Black_Screen.draw(sprite_batch);
                sprite_batch.End();
            }
        }

        protected virtual void draw_background(SpriteBatch sprite_batch)
        {
            if (Background != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Background.draw(sprite_batch);
                sprite_batch.End();
            }
        }

        protected virtual void draw_window(SpriteBatch sprite_batch) { }
        #endregion
    }
}
