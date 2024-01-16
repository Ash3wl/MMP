using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA;
using FEXNA.Graphics.Text;

namespace FEXNA.Windows.UserInterface.Command
{
    class WorldmapUINode : TextUINode
    {
        private string NormalRank = "", HardRank = "";
        private FE_Text_Int Rank;

        internal WorldmapUINode(
                string helpLabel,
                FE_Text text,
                int width)
            : base(helpLabel, text, width)
        {

            Rank = new FE_Text_Int();
            Rank.draw_offset = new Vector2(width, 0);
            Rank.Font = "FE7_TextL";

            Rank.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
        }

        internal void set_rank(string normalRank, string hardRank)
        {
            NormalRank = normalRank;
            HardRank = hardRank;

            refresh_rank(Global.game_system.Difficulty_Mode);
        }

        internal void refresh_rank(Difficulty_Modes mode)
        {
            switch (mode)
            {
                case Difficulty_Modes.Normal:
                    Rank.text = NormalRank;
                    break;
                case Difficulty_Modes.Hard:
                    Rank.text = HardRank;
                    break;
            }
        }

        protected override void update_graphics(bool activeNode)
        {
            base.update_graphics(activeNode);
            Rank.update();
            refresh_rank(Global.game_system.Difficulty_Mode);
        }

        protected override void mouse_off_graphic()
        {
            base.mouse_off_graphic();
            Rank.tint = Color.White;
        }
        protected override void mouse_highlight_graphic()
        {
            base.mouse_highlight_graphic();
            Rank.tint = FEXNA.Config.MOUSE_OVER_ELEMENT_COLOR;
        }
        protected override void mouse_click_graphic()
        {
            base.mouse_click_graphic();
            Rank.tint = FEXNA.Config.MOUSE_PRESSED_ELEMENT_COLOR;
        }

        public override void Draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            base.Draw(sprite_batch, draw_offset);
            Rank.draw(sprite_batch, draw_offset - (loc + draw_vector()));
        }
    }
}
