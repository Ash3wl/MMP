using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;

namespace FEXNA.Windows.UserInterface.Status
{
    class StatusPrimaryStatUINode : StatusStatUINode
    {
        const float MAX_STAT = 30;
        const int STAT_BAR_WIDTH = 41;

        private Func<Game_Unit, PrimaryStatState> StatFormula;
        private Func<Game_Unit, Color> LabelHueFormula;
        protected FE_Text Bonus;
        protected Stat_Bar Bar;
        protected Weapon_Triangle_Arrow PenaltyArrow;
        private Color LabelTint = Color.White;

        internal StatusPrimaryStatUINode(
            string helpLabel,
            string label,
            Func<Game_Unit, PrimaryStatState> statFormula,
            Func<Game_Unit, Color> labelHueFormula,
            int textOffset = 48)
                : base(helpLabel, label, null, textOffset)
        {
            StatFormula = statFormula;
            LabelHueFormula = labelHueFormula;

            Bonus = new FE_Text();
            Bonus.draw_offset = new Vector2(textOffset + 0, 0);
            Bonus.Font = "FE7_TextBonus";

            Bar = new Stat_Bar();
            Bar.draw_offset = new Vector2(16, 8);
            Bar.offset = new Vector2(-2, 0);

            PenaltyArrow = new Weapon_Triangle_Arrow();
            PenaltyArrow.draw_offset = new Vector2(16, 0) + new Vector2(-12, 1);

            Size = new Vector2(textOffset + 24, 16);
        }

        internal override void refresh(Game_Unit unit)
        {
            PrimaryStatState stat = StatFormula == null ?
                new PrimaryStatState() : stat = StatFormula(unit);

            if (stat.NullStat)
                Text.text = "--";
            else
                Text.text = stat.Stat.ToString();
            Text.texture = Global.Content.Load<Texture2D>(
                @"Graphics/Fonts/FE7_Text_" + (stat.IsCapped ? "Green" : "Blue"));

            Bonus.text = stat.Bonus == 0 ? "" : string.Format(
                "{0}{1}", stat.Bonus > 0 ? "+" : "", stat.Bonus);
            Bonus.texture = Global.Content.Load<Texture2D>(
                @"Graphics/Fonts/FE7_Text_" + (stat.Bonus > 0 ? "Green" : "Red"));

            PenaltyArrow.value = stat.Penalized ?
                WeaponTriangle.Disadvantage : WeaponTriangle.Nothing;

            refresh_bar(stat.Stat, stat.Bonus, stat.Cap);
            refresh_label_hue(unit);
        }

        private void refresh_bar(int stat, int bonus, int cap)
        {
            int total_width = (int)Math.Round(Math.Min(MAX_STAT, (stat + bonus)) * STAT_BAR_WIDTH / MAX_STAT);
            stat = (int)Math.Round(stat * STAT_BAR_WIDTH / MAX_STAT);
            bonus = total_width - stat;
            if (bonus < 0)
                stat = total_width;
            Bar.fill_width = stat;
            Bar.bonus_width = bonus;
            Bar.bar_width = (int)Math.Round(cap * STAT_BAR_WIDTH / MAX_STAT);
        }

        private void refresh_label_hue(Game_Unit unit)
        {
            if (LabelHueFormula != null)
                LabelTint = LabelHueFormula(unit);
        }

        private void apply_label_hue()
        {
            Label.tint = new Color(
                Label.tint.R * LabelTint.R / 255,
                Label.tint.G * LabelTint.G / 255,
                Label.tint.B * LabelTint.B / 255,
                Label.tint.A * LabelTint.A / 255);
        }

        protected override void update_graphics(bool activeNode)
        {
            base.update_graphics(activeNode);
            Bonus.update();
            Bar.update();
            PenaltyArrow.update();
        }

        protected override void mouse_off_graphic()
        {
            base.mouse_off_graphic();
            apply_label_hue();
            Bonus.tint = Color.White;
        }
        protected override void mouse_highlight_graphic()
        {
            base.mouse_highlight_graphic();
            apply_label_hue();
            Bonus.tint = Config.MOUSE_OVER_ELEMENT_COLOR;
        }
        protected override void mouse_click_graphic()
        {
            base.mouse_click_graphic();
            apply_label_hue();
            Bonus.tint = Config.MOUSE_PRESSED_ELEMENT_COLOR;
        }

        public override void Draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            Bar.draw_fill(sprite_batch, draw_offset - (loc + draw_vector()));
            base.Draw(sprite_batch, draw_offset);
            Bonus.draw(sprite_batch, draw_offset - (loc + draw_vector()));
            PenaltyArrow.draw(sprite_batch, draw_offset - (loc + draw_vector()));
        }

        public void DrawGaugeBg(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            Bar.draw_bg(sprite_batch, draw_offset - (loc + draw_vector()));
        }
    }

    struct PrimaryStatState
    {
        internal int Stat;
        internal int Bonus;
        internal int Cap;
        internal bool IsCapped;
        internal bool NullStat;
        internal bool Penalized;
    }
}
