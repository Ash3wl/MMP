using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;

namespace FEXNA
{
    class Weapon_Level_Popup : Popup
    {
        Weapon_Type_Icon Icon;
        FE_Text Text;

        public Weapon_Level_Popup(int weapon)
        {
            initialize(weapon, true);
        }
        public Weapon_Level_Popup(int weapon, bool battle_scene)
        {
            initialize(weapon, battle_scene);
        }

        protected void initialize(int weapon, bool battle_scene)
        {
            Timer_Max = 97;
            if (battle_scene)
                texture = Global.Content.Load<Texture2D>(@"Graphics/Windowskins/Combat_Popup");
            else
            {
                Window = new System_Color_Window();
                Window.width = 136;
                Window.height = 32;
            }
            // Weapon type icon
            Icon = new Weapon_Type_Icon();
            Icon.index = weapon;
            Icon.loc = new Vector2(8, 8);
            // Text
            Text = new FE_Text();
            Text.loc = new Vector2(24, 8);
            Text.Font = "FE7_Text";
            Text.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Text.text = "Weapon Level increased.";
        }

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            if (visible)
            {
                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                if (Window != null)
                    Window.draw(sprite_batch, -(loc + draw_vector()));
                else
                    draw_panel(sprite_batch, 136);
                Icon.draw(sprite_batch, -(loc + draw_vector()));
                Text.draw(sprite_batch, -(loc + draw_vector()));
                sprite_batch.End();
            }
        }
    }
}
