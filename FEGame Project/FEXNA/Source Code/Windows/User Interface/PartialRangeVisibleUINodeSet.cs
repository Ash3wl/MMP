﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA_Library;

namespace FEXNA.Windows.UserInterface
{
    class PartialRangeVisibleUINodeSet<T> : UINodeSet<T> where T : UINode
    {
        internal PartialRangeVisibleUINodeSet(IEnumerable<T> set) : base(set) { }

        public void Update(bool input, IEnumerable<int> range,
            Vector2 draw_offset = default(Vector2))
        {
            if (input)
                update_input();
            foreach (int index in range)
                Nodes[index].Update(this, input, draw_offset);
        }

        public void Draw(SpriteBatch sprite_batch, IEnumerable<int> range,
            Vector2 draw_offset = default(Vector2))
        {
#if DEBUG
            // Draw node connections
            if (false)
            {
                draw_node_connections(sprite_batch, draw_offset);
            }
#endif
            foreach (int index in range)
                Nodes[index].Draw(sprite_batch, draw_offset);
        }
    }
}