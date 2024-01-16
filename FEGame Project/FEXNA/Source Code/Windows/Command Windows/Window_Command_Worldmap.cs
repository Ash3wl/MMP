﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Graphics.Text;
using FEXNA.Windows.UserInterface.Command;

namespace FEXNA.Windows.Command
{
    class Window_Command_Worldmap : Window_Command_Scrollbar
    {
        protected List<FE_Text> Ranks, Hard_Ranks;

        #region Accessors
        public override float stereoscopic
        {
            set
            {
                base.stereoscopic = value;
                foreach (var rank in Ranks)
                    rank.stereoscopic = value;
                foreach (var rank in Hard_Ranks)
                    rank.stereoscopic = value;
            }
        }
        #endregion

        public Window_Command_Worldmap(
            Vector2 loc, int width, int rows, List<string> strs)
            : base(loc, width, rows, strs) { }

        protected override void set_default_offsets(int width)
        {
            this.text_offset = new Vector2(12, 0);
            this.glow_width = width + 16 - (24 + (int)(Text_Offset.X * 2));
            Bar_Offset = new Vector2(-4, 0);
        }

        protected override void initialize(
            Vector2 loc, int width, List<string> strs)
        {
            Ranks = new List<FE_Text>();
            Hard_Ranks = new List<FE_Text>();
            for (int i = 0; i < strs.Count; i++)
            {
                Ranks.Add(new FE_Text_Int());
                Ranks[Ranks.Count - 1].Font = "FE7_TextL";
                Ranks[Ranks.Count - 1].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");

                Hard_Ranks.Add(new FE_Text_Int());
                Hard_Ranks[Hard_Ranks.Count - 1].Font = "FE7_TextL";
                Hard_Ranks[Hard_Ranks.Count - 1].texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_Yellow");
            }
            base.initialize(loc, width, strs);
        }

        protected override CommandUINode item(object value, int i)
        {
            var text = new FE_Text();
            text.Font = "FE7_Text";
            text.texture = Global.Content.Load<Texture2D>(@"Graphics\Fonts\FE7_Text_White");
            text.text = value as string;
            var text_node = new WorldmapUINode("", text, this.column_width);
            text_node.loc = item_loc(i);
            return text_node;
        }

        protected override void refresh_layout()
        {
            base.refresh_layout();
            if (Ranks != null)
                for (int i = 0; i < num_items(); i++)
                {
                    Vector2 rank_offset = new Vector2(
                        (i % Columns) * Width + (Width - (Text_Offset.X + 8)),
                        8 + (i / Columns) * 16);

                    Ranks[i].loc = loc + rank_offset;
                    Hard_Ranks[i].loc = loc + rank_offset;
                }
        }

        public void refresh_ranks(List<string> ranks, List<string> hard_ranks)
        {
            for (int i = 0; i < ranks.Count; i++)
            {
                (Items[i] as WorldmapUINode).set_rank(ranks[i], hard_ranks[i]);
            }
        }
    }
}