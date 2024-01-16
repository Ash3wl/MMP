using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;

namespace FEXNA
{
    class Status_Support_Bonuses : Stereoscopic_Graphic_Object
    {
        protected FE_Text Bond_Label, Bond_Name;
        protected FE_Text Bonus_Labels_1, Bonus_Labels_2;
        protected List<FE_Text_Int> Bonus_Values = new List<FE_Text_Int>();

        #region Accessors
        public override float stereoscopic
        {
            set
            {
                base.stereoscopic = value;
                Bond_Label.stereoscopic = value;
                Bond_Name.stereoscopic = value;
                Bonus_Labels_1.stereoscopic = value;
                Bonus_Labels_2.stereoscopic = value;
                foreach (FE_Text_Int bonus in Bonus_Values)
                    bonus.stereoscopic = value;
            }
        }
        #endregion

        public Status_Support_Bonuses(int bond_offset = 0)
        {
            // Bond Label
            Bond_Label = new FE_Text();
            Bond_Label.loc = new Vector2(12, bond_offset);
            Bond_Label.Font = "FE7_Text";
            Bond_Label.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
            Bond_Label.text = "Bond";
            Bond_Label.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Bond Name
            Bond_Name = new FE_Text();
            Bond_Name.loc = new Vector2(64, bond_offset);
            Bond_Name.Font = "FE7_Text";
            Bond_Name.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Bond_Name.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Bonus Labels
            Bonus_Labels_1 = new FE_Text();
            Bonus_Labels_1.loc = new Vector2(4, 16);
            Bonus_Labels_1.Font = "FE7_Text";
            Bonus_Labels_1.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
            Bonus_Labels_1.text = "Atk\nHit\nCrit";
            Bonus_Labels_1.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            Bonus_Labels_2 = new FE_Text();
            Bonus_Labels_2.loc = new Vector2(60, 16);
            Bonus_Labels_2.Font = "FE7_Text";
            Bonus_Labels_2.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
            Bonus_Labels_2.text = "Def\nAvoid\nDodge";
            Bonus_Labels_2.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Bonus Values
            for (int i = 0; i < 6; i++)
            {
                Bonus_Values.Add(new FE_Text_Int());
                Bonus_Values[i].loc = new Vector2((i / 3) * 56 + 52, (i % 3) * 16 + 16);
                Bonus_Values[i].Font = "FE7_Text";
                Bonus_Values[i].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue");
                Bonus_Values[i].stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            }
        }

        public void set_images(Game_Unit unit)
        {
            Game_Actor actor = unit.actor;
            // Bond
            if (unit.actor.bond > 0)
                Bond_Name.text = Global.game_actors[unit.actor.bond].name; //Yeti
            else
                Bond_Name.text = "-----";
            Bond_Name.offset.X = Font_Data.text_width(Bond_Name.text) / 2;
            // Bonuses
            for (int i = 0; i < 6; i++)
            {
                int bonus = 0;
                switch (i)
                {
                    case 0:
                        bonus = unit.support_bonus(Combat_Stat_Labels.Dmg, true);
                        break;
                    case 1:
                        bonus = unit.support_bonus(Combat_Stat_Labels.Hit, true);
                        break;
                    case 2:
                        bonus = unit.support_bonus(Combat_Stat_Labels.Crt, true);
                        break;
                    case 3:
                        bonus = unit.support_bonus(Combat_Stat_Labels.Def, true);
                        break;
                    case 4:
                        bonus = unit.support_bonus(Combat_Stat_Labels.Avo, true);
                        break;
                    case 5:
                        bonus = unit.support_bonus(Combat_Stat_Labels.Dod, true);
                        break;
                }
                Bonus_Values[i].text = bonus.ToString();
            }
        }

        public void update()
        {
            Bond_Label.update();
            Bond_Name.update();
            Bonus_Labels_1.update();
            Bonus_Labels_2.update();
            foreach (FE_Text_Int bonus in Bonus_Values)
                bonus.update();
        }

        public void draw(SpriteBatch sprite_batch, Vector2 draw_offset)
        {
            Bond_Label.draw(sprite_batch, draw_offset - this.loc);
            Bond_Name.draw(sprite_batch, draw_offset - this.loc);
            Bonus_Labels_1.draw(sprite_batch, draw_offset - this.loc);
            Bonus_Labels_2.draw(sprite_batch, draw_offset - this.loc);
            foreach (FE_Text bonus in Bonus_Values)
                bonus.draw(sprite_batch, draw_offset - this.loc);
        }
    }
}
