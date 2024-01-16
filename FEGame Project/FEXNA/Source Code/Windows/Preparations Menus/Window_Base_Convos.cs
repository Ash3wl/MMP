using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Windows.Command;
using FEXNA_Library;

namespace FEXNA.Windows
{
    class Window_Base_Convos : Graphic_Object, ISelectionMenu
    {
        const int FADE_TIME = 16;

        private bool Event_Selected = false;
        private bool Event_Started = false;
        private int Black_Screen_Timer = 0;
        private Window_Command Command_Window;
        private List<Sprite> Priority_Stars;
        private Sprite Black_Screen;

        #region Accessors
        public bool event_selected { get { return Event_Selected; } }

        public bool start_event { get { return Event_Started && Black_Screen_Timer == 0; } }

        public bool event_ending { get { return Event_Started && Black_Screen_Timer > 0; } }

        public bool event_ended { get { return Event_Started && !Event_Selected; } }

        public int index { get { return Command_Window.index; } }
        #endregion

        public Window_Base_Convos(Vector2 loc)
        {
            this.loc = loc;
            initialize_sprites();
        }

        private void initialize_sprites()
        {
            List<string> event_names = Global.game_state.base_event_names();
            Command_Window = new Window_Command(loc, 200, event_names);
            Command_Window.text_offset = new Vector2(16, 0);
            Command_Window.stereoscopic = Config.PREPMAIN_TALK_DEPTH;

            Texture2D star_texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/Ranking_Star");
            Priority_Stars = new List<Sprite>();
            List<int> priorities = Global.game_state.base_event_priorities();
            for (int i = 0; i < priorities.Count; i++)
            {
                bool ready = true;
                if (!Config.BASE_EVENT_ACTIVATED_INVISIBLE)
                    if (!Global.game_state.base_event_ready(i))
                    {
                        Command_Window.set_text_color(i, "Grey");
                        ready = false;
                    }
                for (int j = 0; j < priorities[i]; j++)
                {
                    Priority_Stars.Add(new Sprite(star_texture));
                    Priority_Stars[Priority_Stars.Count - 1].draw_offset = new Vector2(128 + j * 16, i * 16 + 8);
                    Priority_Stars[Priority_Stars.Count - 1].stereoscopic = Config.PREPMAIN_TALK_DEPTH;
                    if (!ready)
                        Priority_Stars[Priority_Stars.Count - 1].tint = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }

        public void update()
        {
            Command_Window.update(!Global.game_system.is_interpreter_running);
            Command_Window.loc = loc;
            if (Event_Selected && Event_Started && Black_Screen_Timer == 0)
                Black_Screen_Timer = FADE_TIME;

            if (!Global.game_system.is_interpreter_running && Event_Selected)
            {
                if (Event_Started)
                {
                    Black_Screen_Timer--;
                    if (Black_Screen_Timer == 0)
                        Event_Selected = false;
                    Black_Screen.tint = new Color(0, 0, 0, Black_Screen_Timer * (256 / FADE_TIME));
                }
                else
                {
                    Black_Screen_Timer--;
                    if (Black_Screen_Timer == 0)
                        Event_Started = true;
                    Black_Screen.tint = new Color(0, 0, 0, Math.Min(255, 256 - Black_Screen_Timer * (256 / FADE_TIME)));
                }
            }
        }

        public Maybe<int> selected_index()
        {
            return Command_Window.selected_index();
        }

        public bool is_selected()
        {
            return Command_Window.is_selected();
        }

        public bool is_canceled()
        {
            return Command_Window.is_canceled();
        }

        public void reset_selected() { }

        public bool select_event()
        {
            if (!Global.game_state.base_event_ready(Command_Window.index))
                return false;
            else
            {
                Event_Selected = true;
                Black_Screen = new Sprite();
                Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
                Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
                Black_Screen.tint = new Color(0, 0, 0, 0);
                Black_Screen_Timer = FADE_TIME;
                return true;
            }
        }

        public void draw(SpriteBatch sprite_batch)
        {
            if (!Event_Started)
            {
                Command_Window.draw(sprite_batch);

                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                foreach (Sprite star in Priority_Stars)
                    star.draw(sprite_batch, -loc);
                sprite_batch.End();
            }

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            if (Black_Screen != null)
                Black_Screen.draw(sprite_batch);
            sprite_batch.End();
        }
    }
}
