using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;

namespace FEXNA
{
    class Game_Updated_Banner : Sprite
    {
        const int FADE_IN_TIME = 16;
        const int SCROLL_SPEED = -1;
        const int HEIGHT = 20;
        readonly static string SPACES_GAP = "            ";

        private int FadeInTimer;
        private int ScissorHeight;
        private int TextWidth, TextPosition;
        private FE_Text Text;
        private static RasterizerState ScissorState = new RasterizerState { ScissorTestEnable = true };

        public override float stereoscopic
        {
            set
            {
                base.stereoscopic = value;
                Text.stereoscopic = value;
            }
        }

        public Game_Updated_Banner(bool open_already_opened)
        {
            texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            loc = new Vector2(0, Config.WINDOW_HEIGHT - 32);
            tint = new Color(0, 0, 0, 128);
            scale = new Vector2(Config.WINDOW_WIDTH, HEIGHT) / 16f;

            FadeInTimer = open_already_opened ? FADE_IN_TIME : 0;
            ScissorHeight = open_already_opened ? HEIGHT : 0;

            Text = new FE_Text();
            Text.draw_offset = new Vector2(0, (HEIGHT - 16) / 2);
            Text.Font = "FE7_Convo";
            // Set message
            Text.text = "New version is available!";
            Text.text += SPACES_GAP;
            Text.text += string.Format("Download v{0} ({1}-{2}-{3}) at ",
                Global.UpdateVersion, Global.UpdateDate.Year, Global.UpdateDate.Month, Global.UpdateDate.Day);
            Text.text_colors[0] = "FE7_Convo_White";
            Text.text_colors[Text.text.Length] = "FE7_Convo_Blue";
            Text.text += Global.UpdateUri;
            Text.text += SPACES_GAP;

            TextWidth = Text.text_width;
            TextWidth = TextWidth + (TextWidth % 8 == 0 ? 0 : (8 - TextWidth % 8));
            TextPosition = (SCROLL_SPEED < 0 ? -1 : 1) * (TextWidth - 80);
        }

        public override void update()
        {
            base.update();
            if (FadeInTimer < FADE_IN_TIME)
            {
                FadeInTimer++;
                ScissorHeight = (int)(HEIGHT * (FadeInTimer / (float)FADE_IN_TIME));
            }
            TextPosition += SCROLL_SPEED;
            while (Math.Abs(TextPosition) >= TextWidth)
                TextPosition += (SCROLL_SPEED < 0 ? 1 : -1) * TextWidth;
        }

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            Rectangle scissor_rect = new Rectangle((int)loc.X, (int)loc.Y + (HEIGHT / 2) - (ScissorHeight / 2), Config.WINDOW_WIDTH, ScissorHeight);

            sprite_batch.GraphicsDevice.ScissorRectangle = scissor_rect;
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, ScissorState);
            base.draw(sprite_batch, texture, draw_offset);
            sprite_batch.End();

            sprite_batch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, null, null, ScissorState);
            for (int x = 0; (x - 1) * TextWidth < Config.WINDOW_WIDTH; x++)
                Text.draw_multicolored(sprite_batch, -(loc + new Vector2(TextPosition + x * TextWidth, 0)));
            sprite_batch.End();
        }
    }
}
