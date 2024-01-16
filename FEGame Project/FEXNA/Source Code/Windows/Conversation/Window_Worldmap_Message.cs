using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA
{
    class Window_Worldmap_Message : Window_Message
    {
        const int BASE_X = 60;
        readonly static int SPACING = (Config.WINDOW_WIDTH - (BASE_X * 2)) / (Face_Sprite_Data.FACE_COUNT > -1 ? Face_Sprite_Data.FACE_COUNT - 1 : 1);
        const int LINES = 2;

        Sprite Text_Background;

        public Window_Worldmap_Message()
        {
            Message_Lines = LINES;
            Text_Speed = (int)Message_Speeds.Normal;
            reset(false, new Vector2(4, Config.WINDOW_HEIGHT - 48), 240, Message_Lines * 16 + 16);
        }

        public override void reset(bool active, Vector2 loc, int width, int height)
        {
            Active = active;
            Text = null;
            Text_End = false;
            if (Window_Img != null)
                return;
            base.reset(active, loc, width, height);
        }

        protected override void set_default_color()
        {
            Default_Text_Color = "White";
            Backlog_Default_Color = "White";
        }

        protected override void setup_images(int width, int height)
        {
            Text_Background = new Sprite();
            Text_Background.texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Worldmap_Text_Box");
            Text_Background.loc = new Vector2(0, Config.WINDOW_HEIGHT - ((LINES + 1) * 16));
            base.setup_images(width, height);
            Window_Img.window_visible = false;
            add_backlog('\n');
            reset_text_color();
            // Doesn't actually do anything, but the world map text is at the bottom middle
            Speaker = CG_VOICEOVER_SPEAKER;
        }

        protected override void test_width(ref int width, string copy_text2, int offset)
        {
            width = Config.WINDOW_WIDTH;
        }

        protected override void play_talk_sound()
        {
            Global.game_system.play_se(System_Sounds.Worldmap_Talk_Boop);
        }

        protected override float text_bubble_y_offset()
        {
            return 0;
        }

        protected override bool change_speaker(Regex r, string test_text)
        {
            return false;
        }

        protected override Vector2 face_location(int id)
        {
            //return new Vector2(BASE_X + ((id - 1) * SPACING), Config.WINDOW_HEIGHT - ((LINES + 1) * 16 - 4));
            return new Vector2(BASE_X + ((id - 1) * SPACING), loc.Y + 4 + Face_Sprite_Data.WORLD_MAP_Y_OFFSET);
        }

        protected override void begin_terminate_message()
        {
            //base.begin_terminate_message();
            Active = false;
        }

        protected override TextSkips text_skip_input()
        {
            if (Global.Input.triggered(Inputs.Start))
                return TextSkips.SkipEvent;
            return TextSkips.None;
        }

        protected override void skip_text(TextSkips skip)
        {
            if (skip == TextSkips.SkipEvent)
            {
                Event_Skip = true;
                begin_terminate_message();
            }
        }

        protected override bool end_wait_input()
        {
            return Global.Input.triggered(Inputs.B) ||
                Global.Input.triggered(Inputs.A) ||
                Global.Input.mouse_click(MouseButtons.Left) ||
                Global.Input.gesture_triggered(TouchGestures.Tap);
        }

        protected override void end_text()
        {
            begin_terminate_message();
        }

        protected override bool background_transition_over_text()
        {
            return false;
        }

        public override void draw_foreground(SpriteBatch sprite_batch)
        {
            if (Text_Background != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Text_Background.draw(sprite_batch);
                sprite_batch.End();
            }

            base.draw_foreground(sprite_batch);
        }
    }
}
