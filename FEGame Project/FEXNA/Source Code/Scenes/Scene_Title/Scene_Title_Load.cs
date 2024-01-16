using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA
{
    class Scene_Title_Load : Scene_Base
    {
        const int SWORD_BASE_Y = -44;
        const int SWORD_Y_MOVE = -272;

        int Action = 0;
        protected int Timer = 0;
        protected Sprite Sword;
        protected Sprite FE_Logo;
        protected Sprite Flash;

        public Scene_Title_Load()
        {
            initialize();
            Global.storage_selection_requested = true;
            Global.load_config = true;
            Global.game_map = null;
        }

        protected virtual void initialize()
        {
            Scene_Type = "Scene_Title_Load";
            Sword = new Sprite();
            Sword.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/title_PRECIOUS");
            Sword.loc = new Vector2(Config.WINDOW_WIDTH / 2, 62);
            //Sword.loc = new Vector2(Config.WINDOW_WIDTH / 2, SWORD_BASE_Y + SWORD_Y_MOVE);
            Sword.tint = new Color(0, 0, 0, 0);
            Sword.offset = new Vector2(Sword.texture.Width / 2, 96);
            Sword.stereoscopic = Config.TITLE_SWORD_DEPTH;
            FE_Logo = new Sprite();
            FE_Logo.texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/title_MARCS");
            FE_Logo.loc = new Vector2(0, 30);//160 - FE_Logo.texture.Width / 2, 96 - FE_Logo.texture.Height);
            //FE_Logo.loc = new Vector2(44 + 52, 56 - 8 + 8);
            FE_Logo.tint = new Color(0, 0, 0, 0);
            FE_Logo.stereoscopic = Config.TITLE_LOGO_DEPTH;
            Flash = new Sprite();
            Flash.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Flash.tint = new Color(0, 0, 0, 0);
            Flash.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Global.Audio.play_bgm("Chapter Transition Humming");
        }

        public override void update()
        {
            if (update_soft_reset())
                return;
            base.update();
            if (Global.Input.triggered(Inputs.Start) ||
                    Global.Input.triggered(Inputs.A) ||
                    Global.Input.any_mouse_triggered ||
                    Global.Input.gesture_triggered(TouchGestures.Tap))
                Global.scene_change("Scene_Title");
            else
            {
                switch (Action)
                {
                    // Pause
                    case 0:
                        Timer++;
                        if (Timer > 60)
                        {
                            Action++;
                            Timer = 0;
                        }
                        break;
                    // FE Logo Appears
                    case 1:
                        FE_Logo.loc.Y--;
                        Sword.loc.Y++;
                        int opacity = Math.Min(FE_Logo.tint.A + 32, 255);
                        FE_Logo.tint = new Color(opacity, opacity, opacity, opacity);
                        Sword.tint = new Color(opacity, opacity, opacity, opacity);
                        if (opacity >= 255)
                            Action++;
                        break;
                    // Pause again
                    case 2:
                        Timer++;
                        if (Timer > 60)
                        {
                            Action++;
                            Timer = 0;
                        }
                        break;
                    // Slide logo
                    case 3:
                        FE_Logo.loc.Y--;
                        Sword.loc.Y++;
                        if (FE_Logo.loc.Y < 20)
                            Action = 6;
                        break;
                    // Pause again
                    case 4:
                        Timer++;
                        if (Timer > 6)
                        {
                            Action++;
                            Timer = 0;
                        }
                        break;
                    // Sword moves down
                    case 5:
                        Sword.loc.Y += 16;
                        FE_Logo.loc.Y -= 16;
                        if (Sword.loc.Y >= SWORD_BASE_Y)
                            Action++;
                        break;
                    // Screen flash
                    case 6:
                        switch (Timer)
                        {
                            case 0:
                                Flash.tint = new Color(255, 255, 255, 255);
                                break;
                            case 2:
                                Flash.tint = new Color(0, 0, 0, 255);
                                Global.scene_change("Scene_Title");
                                break;
                        }
                        Timer++;
                        break;
                }
            }
        }

        public override void draw(SpriteBatch sprite_batch, GraphicsDevice device, RenderTarget2D[] render_targets)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Sword.draw(sprite_batch);
            FE_Logo.draw(sprite_batch);
            Flash.draw(sprite_batch);
            sprite_batch.End();
            base.draw(sprite_batch, device, render_targets);
        }
    }
}
