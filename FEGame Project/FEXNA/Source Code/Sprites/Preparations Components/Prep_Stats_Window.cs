using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;

namespace FEXNA.Graphics.Preparations
{
    class Prep_Stats_Window : Graphic_Object
    {
        const float MAX_STAT = 30;
        const int STAT_BAR_WIDTH = 41;
        const int STAT_GAIN_TIME = 120;

        private int Timer = 0, Glow_Timer;
        protected System_Color_Window Stats_Window;
        protected FE_Text Stat_Labels_1, Stat_Labels_2;
        protected List<FE_Text> Stat_Values, Stat_Bonuses;
        protected List<Stat_Bar> Stat_Bars;

        private List<Spark> Swirls = new List<Spark>(), Arrows = new List<Spark>();
        private List<Stat_Up_Num> Stat_Gains = new List<Stat_Up_Num>();

        #region Accessors
        public bool is_ready { get { return Timer == 0; } }
        #endregion

        protected virtual int WIDTH()
        {
            return 144;
        }
        protected virtual int SPACING()
        {
            return 64;
        }
        protected virtual int HEIGHT()
        {
            return 80;
        }

        public Prep_Stats_Window(Game_Unit unit)
        {
            // Stats Window
            initialize_window();
            // Stat Labels
            Stat_Labels_1 = new FE_Text();
            Stat_Labels_1.loc = Stats_Window.loc + new Vector2(8, 8);
            Stat_Labels_1.Font = "FE7_Text";
            Stat_Labels_1.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
            Stat_Labels_1.text = "HP\nStr\nSkl\nSpd";
            Stat_Labels_1.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            Stat_Labels_2 = new FE_Text();
            Stat_Labels_2.loc = Stats_Window.loc + new Vector2(8 + SPACING(), 8);
            Stat_Labels_2.Font = "FE7_Text";
            Stat_Labels_2.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
            Stat_Labels_2.text = "Luck\nDef\nRes\nCon";
            Stat_Labels_2.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Stat Values
            Stat_Values = new List<FE_Text>();
            Stat_Bonuses = new List<FE_Text>();
            for (int i = 0; i < 8; i++)
            {
                Stat_Values.Add(new FE_Text_Int());
                Stat_Values[i].loc = Stats_Window.loc + new Vector2(48 + (i / 4) * SPACING(), 8 + (i % 4) * 16);
                Stat_Values[i].Font = "FE7_Text";
                Stat_Values[i].stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
                Stat_Bonuses.Add(new FE_Text());
                Stat_Bonuses[i].loc = Stats_Window.loc + new Vector2(48 + ((i / 4) * SPACING()), 8 + (i % 4) * 16);
                Stat_Bonuses[i].Font = "FE7_TextBonus";
                Stat_Bonuses[i].stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            }
            // Stat Bars
            Stat_Bars = new List<Stat_Bar>();
            for (int i = 0; i < 8; i++)
            {
                Stat_Bars.Add(new Stat_Bar());
                Stat_Bars[i].loc = Stats_Window.loc + new Vector2(24 + (i / 4) * SPACING(), 16 + (i % 4) * 16);
                Stat_Bars[i].offset = new Vector2(-2, 0);
                Stat_Bars[i].stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            }

            set_images(unit);
        }

        protected virtual void initialize_window()
        {
            Stats_Window = new System_Color_Window();
            Stats_Window.loc = new Vector2(0, 0);
            Stats_Window.width = WIDTH();
            Stats_Window.height = HEIGHT();
            Stats_Window.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
        }

        public void set_images(Game_Unit unit)
        {
            Game_Actor actor = unit.actor;
            // Stats
            Stat_Labels_1.text = "HP\n";
            switch (actor.power_type())
            {
                case Power_Types.Strength:
                    Stat_Labels_1.text += "Str";
                    break;
                case Power_Types.Magic:
                    Stat_Labels_1.text += "Mag";
                    break;
                default:
                    Stat_Labels_1.text += "Pow";
                    break;
            }
            Stat_Labels_1.text += "\nSkl\nSpd";
            // Stat Bars
            for (int i = 0; i < 8; i++)
            {
                int stat = 0;
                int bonus = 0;
                int cap = 0;
                switch (i)
                {
                    // Hp
                    case 0:
                        bonus = 0; //Debug
                        stat = unit.stat(Stat_Labels.Hp) - bonus;
                        cap = unit.stat_cap(Stat_Labels.Hp);
                        break;
                    // Pow
                    case 1:
                        stat = unit.stat(Stat_Labels.Pow) - unit.stat_bonus(Stat_Labels.Pow);
                        bonus = unit.pow_bonus_display; //unit.pow_bonus; //Yeti
                        cap = unit.stat_cap(Stat_Labels.Pow);
                        break;
                    // Skl
                    case 2:
                        bonus = unit.stat_bonus(Stat_Labels.Skl);
                        stat = unit.stat(Stat_Labels.Skl) - bonus;
                        cap = unit.stat_cap(Stat_Labels.Skl);
                        break;
                    // Spd
                    case 3:
                        stat = unit.stat(Stat_Labels.Spd) - unit.stat_bonus(Stat_Labels.Spd);
                        bonus = unit.spd_bonus_display;
                        cap = unit.stat_cap(Stat_Labels.Spd);
                        break;
                    // Lck
                    case 4:
                        bonus = unit.stat_bonus(Stat_Labels.Lck);
                        stat = unit.stat(Stat_Labels.Lck) - bonus;
                        cap = unit.stat_cap(Stat_Labels.Lck);
                        break;
                    // Def
                    case 5:
                        bonus = unit.stat_bonus(Stat_Labels.Def);
                        stat = unit.stat(Stat_Labels.Def) - bonus;
                        cap = unit.stat_cap(Stat_Labels.Def);
                        break;
                    // Res
                    case 6:
                        bonus = unit.stat_bonus(Stat_Labels.Res);
                        stat = unit.stat(Stat_Labels.Res) - bonus;
                        cap = unit.stat_cap(Stat_Labels.Res);
                        break;
                    // Con
                    case 7:
                        stat = actor.stat(Stat_Labels.Con);
                        bonus = Math.Min(unit.stat_bonus(Stat_Labels.Con), actor.get_cap(Stat_Labels.Con) - unit.stat(Stat_Labels.Con));
                        cap = unit.stat_cap(Stat_Labels.Con);
                        break;
                }

                Stat_Values[i].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_" + (actor.get_capped(i) ? "Green" : "Blue"));
                Stat_Values[i].text = stat.ToString();
                Stat_Bonuses[i].text = bonus == 0 ? "" : (bonus > 0 ? "+" : "") + bonus.ToString();
                Stat_Bonuses[i].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_" + (bonus > 0 ? "Green" : "Red"));

                float max_stat = i == 0 ? Constants.Actor.MAX_HP : MAX_STAT;
                int total_width = (int)Math.Round(Math.Min(max_stat, (stat + bonus)) * STAT_BAR_WIDTH / max_stat);
                stat = (int)Math.Round(stat * STAT_BAR_WIDTH / max_stat);
                bonus = total_width - stat;
                if (bonus < 0)
                    stat = total_width;
                Stat_Bars[i].fill_width = stat;
                Stat_Bars[i].bonus_width = bonus;
                Stat_Bars[i].bar_width = (int)Math.Round(cap * STAT_BAR_WIDTH / max_stat);
            }
        }

        public void update()
        {
            if (Timer > 0)
            {
                Glow_Timer++;
                Timer--;
                if (Timer == 0)
                    cancel_stats_gain();
                else
                {
                    foreach (Spark arrow in Arrows)
                        ((Stat_Change_Arrow)arrow).update(Glow_Timer);
                    foreach (Stat_Up_Num stat_up in Stat_Gains)
                        stat_up.update(Glow_Timer);
                    foreach (Spark swirl in Swirls)
                        swirl.update();
                }
            }

            // Stats Window
            Stats_Window.update();
            foreach (Stat_Bar bar in Stat_Bars)
                bar.update();
            Stat_Labels_1.update();
            Stat_Labels_2.update();
            foreach (FE_Text stat in Stat_Values)
                stat.update();
        }

        public void gain_stats(Dictionary<FEXNA_Library.Boosts, int> boosts)
        {
            Glow_Timer = 0;
            Timer = STAT_GAIN_TIME;
            Arrows.Clear();
            Stat_Gains.Clear();
            Swirls.Clear();

            foreach(KeyValuePair<FEXNA_Library.Boosts, int> pair in boosts)
            {
                Vector2 loc;
                if (pair.Key == FEXNA_Library.Boosts.Con)
                    loc = new Vector2(16 + ((((int)Stat_Labels.Con) / 4) * SPACING()), (((int)Stat_Labels.Con) % 4) * 16);
                else
                    loc = new Vector2(16 + ((((int)pair.Key) / 4) * SPACING()), ((((int)pair.Key) % 4) * 16));

                Stat_Gains.Add(new Quick_Stat_Up_Num(new List<Texture2D> {
                    Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Stat2"),
                    Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Stat1") }));
                Stat_Gains[Stat_Gains.Count - 1].value = pair.Value;
                Stat_Gains[Stat_Gains.Count - 1].loc = loc + new Vector2(40, 23);
                Arrows.Add(new Stat_Change_Arrow());
                Arrows[Arrows.Count - 1].texture = Global.Content.Load<Texture2D>(@"Graphics/Pictures/" + Stat_Change_Arrow.FILENAME);
                Arrows[Arrows.Count - 1].loc = loc + new Vector2(32, 1);
                ((Stat_Change_Arrow)Arrows[Arrows.Count - 1]).update(0);
                Swirls.Add(new Stat_Up_Spark());
                Swirls[Swirls.Count - 1].loc = loc + new Vector2(-5, -7);
                Swirls[Swirls.Count - 1].update();
            }
        }

        public void cancel_stats_gain()
        {
            Timer = 0;
            Arrows.Clear();
            Stat_Gains.Clear();
            Swirls.Clear();
        }

        public void draw(SpriteBatch sprite_batch)
        {
            // Stats Window
            Stats_Window.draw(sprite_batch, -loc);
            // Draw Window Contents //
            // Stats Window
            foreach (Stat_Bar bar in Stat_Bars)
                bar.draw(sprite_batch, -loc);
            Stat_Labels_1.draw(sprite_batch, -loc);
            Stat_Labels_2.draw(sprite_batch, -loc);
            foreach (FE_Text stat in Stat_Values)
                stat.draw(sprite_batch, -loc);
            foreach (FE_Text bonus in Stat_Bonuses)
                bonus.draw(sprite_batch, -loc);

            if (!is_ready)
            {
                foreach (Spark arrow in Arrows)
                    arrow.draw(sprite_batch, -loc);
                foreach (Stat_Up_Num stat_up in Stat_Gains)
                    stat_up.draw(sprite_batch, -loc);
                foreach (Spark swirl in Swirls)
                    swirl.draw(sprite_batch, -loc);
            }
        }
    }
}
