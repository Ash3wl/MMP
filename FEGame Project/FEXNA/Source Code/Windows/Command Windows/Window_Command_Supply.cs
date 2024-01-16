using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FEXNA.Windows.UserInterface;
using FEXNA.Windows.UserInterface.Command;

namespace FEXNA.Windows.Command
{
    class Window_Command_Supply : Window_Command_Scrollbar
    {
        private List<Status_Item> ConvoyItems;
        internal float PageOffset;

        internal override int rows { get { return Rows; } }

        public Window_Command_Supply(Vector2 loc, int width, int rows) :
            base(loc, width, rows, null) { }

        protected override void set_default_offsets(int width)
        {
            base.set_default_offsets(width);
            this.text_offset = Vector2.Zero;
        }

        internal void refresh_items(List<Status_Item> items)
        {
            ConvoyItems = items;

            set_items(null);
        }

        protected override void add_commands(List<string> strs)
        {
            var nodes = new List<CommandUINode>();

            if (ConvoyItems == null)
            {
                set_nodes(nodes);
                return;
            }

            int count = ConvoyItems.Count;

            for (int i = 0; i < count; i++)
            {
                var item_node = item("", i);
                if (item_node != null)
                {
                    item_node.loc = item_loc(nodes.Count);
                    nodes.Add(item_node);
                }
            }

            set_nodes(nodes);
        }

        protected override CommandUINode item(object value, int i)
        {
            var text_node = new ItemUINode("", ConvoyItems[i], this.column_width - 8);
            text_node.loc = item_loc(i);
            return text_node;
        }

        protected override void draw_text(SpriteBatch sprite_batch)
        {
            Items.Draw(sprite_batch,
                -(loc + text_draw_vector() + new Vector2(PageOffset, 0)));
        }
    }
}
