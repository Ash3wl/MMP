using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Graphics.Windows;
using FEXNA.Windows.UserInterface;
using FEXNA.Windows.UserInterface.Status;
using FEXNA_Library;

namespace FEXNA
{
    class Status_Page_1 : Status_Page
    {
        protected System_Color_Window Stats_Window, Items_Window;
        protected StatusStatUINode PowNode;

        public Status_Page_1()
        {
            var nodes = new List<StatusUINode>();

            // Stats Window
            Stats_Window = new System_Color_Window();
            Stats_Window.loc = new Vector2(8, 80);
            Stats_Window.width = 144;
            Stats_Window.height = 112;
            Stats_Window.stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Stats
            for (int i = 0; i < 6; i++)
            {
                string help_label;
                string label;

                var stat_label = (Stat_Labels)i + 1;
                Func<Game_Unit, PrimaryStatState> stat_formula = (Game_Unit unit) =>
                {
                    return new PrimaryStatState
                    {
                        Stat = unit.stat(stat_label) - unit.stat_bonus(stat_label),
                        Bonus = unit.stat_bonus_display(stat_label),
                        Cap = unit.stat_cap(stat_label),
                        IsCapped = unit.actor.get_capped(stat_label),
                        Penalized = unit.is_weighed_stat(stat_label)
                    };
                };

                Func<Game_Unit, Color> label_color = null;
                if (Window_Status.show_stat_averages(stat_label))
                {
                    label_color = (Game_Unit unit) =>
                    {
                        if (!Constants.Actor.ONLY_PC_AVERAGES || unit.is_player_team)
                        {
                            float stat_quality = unit.actor.stat_quality(stat_label);
                            if (unit.actor.get_capped(stat_label))
                                stat_quality = Math.Max(0, stat_quality);
                            int r = 255 - (int)MathHelper.Clamp((stat_quality * 1.25f * 255), 0, 255);
                            int g = (int)MathHelper.Clamp(255 + (stat_quality * 255), 0, 255);
                            return new Color(r, g, 255);

                            /*//Yeti
                            int avg = (int)Math.Round(unit.actor.stat_avg_comparison(stat_label));
                            Text.text += (avg >= 0 ? "+" : "-") + Math.Abs(avg);
                            Text.draw_offset = new Vector2(16, 0);*/
                        }
                        return Color.White;
                    };
                }
                switch (i)
                {
                    // Str
                    case 0:
                    default:
                        help_label = "Pow";
                        label = "Str";
                        break;
                    // Skl
                    case 1:
                        help_label = "Skl";
                        label = "Skl";
                        break;
                    // Spd
                    case 2:
                        help_label = "Spd";
                        label = "Spd";
                        break;
                    // Lck
                    case 3:
                        help_label = "Lck";
                        label = "Luck";
                        break;
                    // Def
                    case 4:
                        help_label = "Def";
                        label = "Def";
                        break;
                    // Res
                    case 5:
                        help_label = "Res";
                        label = "Res";
                        break;
                }
                
                Vector2 loc = Stats_Window.loc + new Vector2(8, i * 16 + 8);

                nodes.Add(new StatusPrimaryStatUINode(
                    help_label, label, stat_formula, label_color, 40));
                nodes.Last().loc = loc;
                nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;

                if (stat_label == Stat_Labels.Pow)
                    PowNode = nodes.Last() as StatusStatUINode;
            }

            // Move
            nodes.Add(new StatusPrimaryStatUINode(
                "Move",
                "Move",
                (Game_Unit unit) =>
                {
                    if (unit.immobile)
                        return new PrimaryStatState
                        {
                            Stat = 0,
                            Bonus = 0,
                            Cap = unit.stat_cap(Stat_Labels.Mov),
                            NullStat = true,
                        };
                    return new PrimaryStatState
                    {
                        Stat = unit.base_mov,
                        Bonus = unit.mov - unit.base_mov,
                        Cap = unit.stat_cap(Stat_Labels.Mov),
                    };
                }, null, 40));
            nodes.Last().loc = Stats_Window.loc + new Vector2(72, 0 * 16 + 8);
            nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Con
            nodes.Add(new StatusPrimaryStatUINode(
                "Con",
                "Con",
                (Game_Unit unit) =>
                {
                    return new PrimaryStatState
                    {
                        Stat = unit.actor.stat(Stat_Labels.Con),
                        Bonus = Math.Min(unit.stat_bonus(Stat_Labels.Con),
                            unit.actor.get_cap(Stat_Labels.Con) -
                                unit.stat(Stat_Labels.Con)),
                        Cap = unit.stat_cap(Stat_Labels.Con),
                        IsCapped = unit.actor.get_capped(Stat_Labels.Con)
                    };
                }, null, 40));
            nodes.Last().loc = Stats_Window.loc + new Vector2(72, 1 * 16 + 8);
            nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Aid
            nodes.Add(new StatusAidUINode(
                "Aid",
                "Aid",
                (Game_Unit unit) =>
                {
                    return unit.aid().ToString();
                },
                (Game_Unit unit) =>
                {
                    if (unit.actor.actor_class.Class_Types.Contains(ClassTypes.FDragon))
                        return 3;
                    else if (unit.actor.actor_class.Class_Types.Contains(ClassTypes.Flier))
                        return 2;
                    else if (unit.actor.actor_class.Class_Types.Contains(ClassTypes.Cavalry))
                        return 1;
                    else
                        return 0;
                }, 40));
            nodes.Last().loc = Stats_Window.loc + new Vector2(72, 2 * 16 + 8);
            nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Trv
            nodes.Add(new StatusTravelerUINode(
                "Trv",
                "Trv",
                (Game_Unit unit) =>
                {
                    if (unit.is_rescued)
                        return Global.game_map.units[unit.rescued].actor.name;
                    else if (unit.is_rescuing)
                        return Global.game_map.units[unit.rescuing].actor.name;
                    else if (unit.actor.has_skill("PURT"))
                        return "Bread";
                    return "---";
                },
                (Game_Unit unit) =>
                {
                    if (!unit.is_rescuing)
                        return 0;
                    return Global.game_map.units[unit.rescuing].team;
                }, 24));
            nodes.Last().loc = Stats_Window.loc + new Vector2(72, 3 * 16 + 8);
            nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Type
            nodes.Add(new StatusClassTypesUINode(
                "Type",
                "Type",
                (Game_Unit unit) =>
                {
                    return unit.actor.class_types;
                }, 24));
            nodes.Last().loc = Stats_Window.loc + new Vector2(72, 4 * 16 + 8);
            nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;
            // Rating
            nodes.Add(new StatusLabeledTextUINode(
                "Rating",
                "Rating",
                (Game_Unit unit) =>
                {
                    return unit.rating().ToString();
                }, 32));
            nodes.Last().loc = Stats_Window.loc + new Vector2(72, 5 * 16 + 8);
            nodes.Last().Size = new Vector2(64, 16);
            nodes.Last().stereoscopic = Config.STATUS_LEFT_WINDOW_DEPTH;

            // Items Window
            Items_Window = new System_Color_Window();
            Items_Window.loc = new Vector2(168, 80);
            Items_Window.width = 144;
            Items_Window.height = Constants.Actor.NUM_ITEMS * 16 + 16;
            Items_Window.stereoscopic = Config.STATUS_RIGHT_WINDOW_DEPTH;
            // Items
            for (int i = 0; i < Constants.Actor.NUM_ITEMS; i++)
            {
                int j = i;

                Vector2 loc = Items_Window.loc + new Vector2(8, i * 16 + 8);

                nodes.Add(new StatusItemUINode(
                    string.Format("Item{0}", i + 1),
                    (Game_Unit unit) =>
                    {
                        return new ItemState
                        {
                            Item = unit.actor.items[j],
                            Drops = unit.drops_item && j == unit.actor.num_items - 1,
                            Equipped = unit.actor.equipped - 1 == j
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
            Game_Actor actor = unit.actor;
            // Stats
            switch (actor.power_type())
            {
                case Power_Types.Strength:
                    PowNode.set_label("Str");
                    break;
                case Power_Types.Magic:
                    PowNode.set_label("Mag");
                    break;
                default:
                    PowNode.set_label("Pow");
                    break;
            }
            // Aid
            //Stat_Values[7].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue"); // Green if capped //Yeti
            // Mov
            //Stat_Values[8].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Blue"); // Green if capped //Yeti
            
            // Refresh UI nodes
            foreach (StatusUINode node in StatusPageNodes)
            {
                node.refresh(unit);
            }
        }

        public override void update()
        {
            base.update();
            // Stats Window
            Stats_Window.update();
            // Item Window
            Items_Window.update();
        }

        public override void draw(SpriteBatch sprite_batch, Vector2 draw_offset = default(Vector2))
        {
            // Stats Window
            Stats_Window.draw(sprite_batch, draw_offset);

            foreach (var node in StatusPageNodes
                    .Where(x => x is StatusPrimaryStatUINode))
                (node as StatusPrimaryStatUINode).DrawGaugeBg(
                    sprite_batch, draw_offset);

            // Item Window
            Items_Window.draw(sprite_batch, draw_offset);
            // Window Design //
            Window_Design.draw(sprite_batch, draw_offset);

            // Draw Window Contents //
            foreach (var node in StatusPageNodes)
                node.Draw(sprite_batch, draw_offset);
        }
    }
}