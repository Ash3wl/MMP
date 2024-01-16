using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;
using FEXNA.Windows.UserInterface;
using FEXNA.Windows.UserInterface.Status;
using FEXNA_Library;

namespace FEXNA
{
    class Status_Page_2 : Status_Page
    {
        const int ACTOR_SKILLS = 4;
        const int WLVL_ROWS = 5;

        protected System_Color_Window Skills_Window, WLvls_Window;

        public Status_Page_2()
        {
            var nodes = new List<StatusUINode>();

            // Skills Window
            Skills_Window = new System_Color_Window();
            Skills_Window.loc = new Vector2(8, 80);
            Skills_Window.width = 144;
            Skills_Window.height = 112;
            Skills_Window.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;

            // WLvls Window
            WLvls_Window = new System_Color_Window();
            WLvls_Window.loc = new Vector2(168, 96);
            WLvls_Window.width = 144;
            WLvls_Window.height = 96;
            WLvls_Window.stereoscopic = Config.STATUS_RIGHT_WINDOW_DEPTH;
            
            // Skills
            for(int i = 0; i < ACTOR_SKILLS; i++)
            {
                int j = i;

                Vector2 loc = Skills_Window.loc +
                    new Vector2(8, 8 + 4 + i * Config.SKILL_ICON_SIZE);

                nodes.Add(new StatusSkillUINode(
                    string.Format("Skill{0}", i + 1),
                    (Game_Unit unit) =>
                    {
                        if (unit.actor.skills.Count <= j)
                            return new SkillState();
                        var skill = Global.data_skills[unit.actor.skills[j]];

                        float charge = -1f;
                        if (Game_Unit.MASTERIES.Contains(skill.Abstract))
                            charge = unit.mastery_charge_percent(skill.Abstract);
                        return new SkillState
                        {
                            Skill = skill,
                            Charge = charge
                        };
                    }));
                nodes.Last().loc = loc;
                nodes.Last().draw_offset = new Vector2(
                    0, -(Config.SKILL_ICON_SIZE - 16) / 2);
                nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            }
            // WLvls
            foreach (var weapon_type in Global.weapon_types)
            {
                if (!weapon_type.DisplayedInStatus)
                    continue;

                Vector2 loc = WLvls_Window.loc + new Vector2(
                    (weapon_type.StatusIndex / WLVL_ROWS) * 64 + 8,
                    (weapon_type.StatusIndex % WLVL_ROWS) * 16 + 8);

                nodes.Add(new StatusWLvlUINode(
                    weapon_type.StatusHelpName,
                    weapon_type,
                    (Game_Unit unit) =>
                    {
                        return new WLvlState
                        {
                            Rank = unit.actor.weapon_level_letter(weapon_type),
                            Progress = unit.actor.weapon_level_percent(weapon_type),
                            IsCapped = unit.actor.weapon_level_letter(weapon_type) ==
                                Data_Weapon.WLVL_LETTERS[Data_Weapon.WLVL_LETTERS.Length - 1]
                        };
                    }));
                nodes.Last().loc = loc;
                nodes.Last().stereoscopic = Config.STATUS_RIGHT_WINDOW_DEPTH;
            }

            StatusPageNodes = new UINodeSet<StatusUINode>(nodes);

            init_design();
        }

        public override void set_images(Game_Unit unit)
        {
            // Refresh UI nodes
            foreach (StatusUINode node in StatusPageNodes)
            {
                node.refresh(unit);
            }
        }

        public override void update()
        {
            base.update();
        }

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            // Skills Window
            Skills_Window.draw(sprite_batch, draw_offset);
            // WLvls Window
            WLvls_Window.draw(sprite_batch, draw_offset);

            foreach (var node in StatusPageNodes
                    .Where(x => x is StatusWLvlUINode))
                (node as StatusWLvlUINode).DrawGaugeBg(
                    sprite_batch, draw_offset);

            // Window Design //
            Window_Design.draw(sprite_batch, draw_offset);

            // Draw Window Contents //
            foreach (var node in StatusPageNodes)
                node.Draw(sprite_batch, draw_offset);
        }
    }
}
