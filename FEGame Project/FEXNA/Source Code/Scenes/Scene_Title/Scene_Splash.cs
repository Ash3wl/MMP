using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA
{
    public class Scene_Splash : Scene_Base
    {
        private int Timer = -(Config.SPLASH_INITIAL_BLACK_TIME + 1);
        private int Image_Index = 0;
        private Sprite Splash;

        public Scene_Splash()
        {
            Scene_Type = "Scene_Splash";
            Splash = new Sprite();
            set_splash_image();
            Global.game_map = null;
        }

        protected void set_splash_image()
        {
            Splash.texture = Global.Content.Load<Texture2D>(@"Graphics/Titles/" + Config.SPLASH_SCREENS[Image_Index]);
            Splash.opacity = 0;
        }

        public override void update()
        {
            if (update_soft_reset())
                return;
            Timer++; //Debug
            if (Timer <= Config.SPLASH_FADE_TIME)
                Splash.opacity = (255 * Timer) / Config.SPLASH_FADE_TIME;
            else if (Config.SPLASH_TIME - Timer <= Config.SPLASH_FADE_TIME)
                Splash.opacity = (255 * (Config.SPLASH_TIME - Timer)) / Config.SPLASH_FADE_TIME;

            if (Timer >= -(Config.SPLASH_INITIAL_BLACK_TIME - 10))
            {
                // Skip to title with start
                if (Global.Input.triggered(Inputs.Start))
                    Global.scene_change("Scene_Title");
                else if (Timer >= Config.SPLASH_TIME ||
                    Global.Input.triggered(Inputs.A) ||
                    Global.Input.any_mouse_triggered ||
                    Global.Input.gesture_triggered(TouchGestures.Tap))
                {
                    Image_Index++;
                    Timer = 0;
                    // Advance to title load if out of splash screens
                    if (Image_Index >= Config.SPLASH_SCREENS.Length)
                        Global.scene_change("Scene_Title_Load");
                    else
                        set_splash_image();
                }
            }

            base.update();
        }

        public override void draw(
            Microsoft.Xna.Framework.Graphics.SpriteBatch sprite_batch,
            Microsoft.Xna.Framework.Graphics.GraphicsDevice device,
            Microsoft.Xna.Framework.Graphics.RenderTarget2D[] render_targets)
        {
            base.draw(sprite_batch, device, render_targets);

            device.SetRenderTarget(render_targets[0]);
            device.Clear(Color.Transparent);

            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Splash.draw(sprite_batch);
            sprite_batch.End();
        }
    }
}
