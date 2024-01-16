﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FEXNA
{
    class Miniface : Sprite
    {
        protected Color[] Palette;
        private bool Has_Palette;
        protected Texture2D Bg_Texture, Flag_Texture;
        protected Rectangle Bg_Rect, Flag_Rect;

        public Miniface()
        {
            initialize_palette();
            offset.X = Face_Sprite_Data.MINI_FACE_SIZE.X / 2;
        }

        protected void initialize_palette()
        {
            Palette = new Color[Palette_Handler.PALETTE_SIZE];
        }

        protected void refresh_palette(string filename)
        {
            //Has_Palette = Global.face_palette_data.ContainsKey(filename); //Debug
            Has_Palette = Global.face_palette_data.ContainsKey(filename) && Global.face_palette_data[filename].Length > 0;
            if (Has_Palette)
            {
                for (int i = 0; i < Global.face_palette_data[filename].Length; i++)
                    Palette[i] = Global.face_palette_data[filename][i];
            }
        }

        public void set_actor(string actor_name)
        {
            reset();
            if (!Global.content_exists(@"Graphics/Faces/" + actor_name))
            {
                if (Global.content_exists(@"Graphics/Faces/" + actor_name.Split(Constants.Actor.ACTOR_NAME_DELIMITER)[0]))
                    actor_name = actor_name.Split(Constants.Actor.ACTOR_NAME_DELIMITER)[0];
                else
                    return;
            }

            texture = Global.Content.Load<Texture2D>(@"Graphics/Faces/" + actor_name);
            Src_Rect = new Rectangle(0, texture.Height - (int)Face_Sprite_Data.MINI_FACE_SIZE.Y,
                (int)Face_Sprite_Data.MINI_FACE_SIZE.X, (int)Face_Sprite_Data.MINI_FACE_SIZE.Y);
            refresh_palette(actor_name);
        }
        public void set_actor(Game_Actor actor)
        {
            reset();
            if (actor != null)
            {
                string actor_name = actor.face_name;
                if (!Global.content_exists(@"Graphics/Faces/" + actor_name))
                    if (Global.content_exists(@"Graphics/Faces/" + actor_name.Split(Constants.Actor.ACTOR_NAME_DELIMITER)[0]))
                        actor_name = actor_name.Split(Constants.Actor.ACTOR_NAME_DELIMITER)[0];

                if (Global.content_exists(@"Graphics/Faces/" + actor_name))
                {
                    texture = Global.Content.Load<Texture2D>(@"Graphics/Faces/" + actor_name);
                    Src_Rect = new Rectangle(0, texture.Height - (int)Face_Sprite_Data.MINI_FACE_SIZE.Y,
                        (int)Face_Sprite_Data.MINI_FACE_SIZE.X, (int)Face_Sprite_Data.MINI_FACE_SIZE.Y);
                    refresh_palette(actor_name);
                }

                // Generic background/flags
                if (actor.generic_face)
                {
                    Mirrored = false;
                    Bg_Texture = Global.Content.Load<Texture2D>(@"Graphics/Faces/Countries/Default_Miniface");
                    Bg_Rect = new Rectangle(32 * actor.tier, 0, 32, 32);
                    Flag_Texture = flag_texture(actor);
                    Flag_Rect = new Rectangle(32 * actor.build, 0, 32, 32);
                }
            }
        }

        protected void reset()
        {
            texture = null;
            Bg_Texture = null;
            Flag_Texture = null;
            Mirrored = true;
        }

        protected Texture2D flag_texture(Game_Actor actor)
        {
            string flag = actor.flag_name;
            //if (Face_Sprite_Data.FACE_COUNTRY_RENAME.ContainsKey(country)) //Debug
            //    country = Face_Sprite_Data.FACE_COUNTRY_RENAME[country];
            if (Global.content_exists(@"Graphics/Faces/Countries/" + flag))
                return Global.Content.Load<Texture2D>(@"Graphics/Faces/Countries/" + flag);
#if DEBUG
            else
                return Global.Content.Load<Texture2D>(@"Graphics/Faces/Countries/Bern Flags");
#endif
            return null;
        }

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            Vector2 offset = this.offset;
            // Bg
            if (Bg_Texture != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                sprite_batch.Draw(Bg_Texture, loc + draw_vector() - draw_offset,
                    Bg_Rect, tint, angle, offset, scale,
                    mirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Z);
                sprite_batch.End();
            }
            // Face
            if (Has_Palette)
            {
                Effect effect = Global.effect_shader();
                if (effect != null)
                {
                    Texture2D palette_texture = Global.palette_pool.get_palette();
                    palette_texture.SetData<Color>(Palette);
#if __ANDROID__
                    // There has to be a way to do this for both
                    effect.Parameters["Palette"].SetValue(palette_texture);
#else
                    sprite_batch.GraphicsDevice.Textures[2] = palette_texture;
#endif
                    sprite_batch.GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
                    effect.CurrentTechnique = effect.Techniques["Palette1"];
                    effect.Parameters["color_shift"].SetValue(new Vector4(0, 0, 0, 0));
                    effect.Parameters["opacity"].SetValue(1f);
                }
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, effect);
            }
            else
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

            base.draw(sprite_batch, draw_offset);

            sprite_batch.End();
#if __ANDROID__
            // There has to be a way to do this for both
            if (Global.effect_shader() != null)
                Global.effect_shader().Parameters["Palette"].SetValue((Texture2D)null);
#else
            sprite_batch.GraphicsDevice.Textures[2] = null;
#endif
            // Flags
            if (Flag_Texture != null)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                sprite_batch.Draw(Flag_Texture, loc + draw_vector() - draw_offset,
                    Flag_Rect, tint, angle, offset, scale,
                    mirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None, Z);
                sprite_batch.End();
            }
        }
    }
}
