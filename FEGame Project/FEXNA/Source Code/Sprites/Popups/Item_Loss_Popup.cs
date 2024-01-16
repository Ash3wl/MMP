using System.Linq;
using Microsoft.Xna.Framework;

namespace FEXNA
{
    class Item_Loss_Popup : Item_Gain_Popup
    {
        public Item_Loss_Popup(int item_id, bool is_item, int time)
        {
            initialize(item_id, is_item, false);
            Timer_Max = time;
            loc = new Vector2((Config.WINDOW_WIDTH - Width) / 2, 80);
        }

        public override void update()
        {
            if (Timer > 0 && this.skip_input)
                Timer = Timer_Max;
            base.update();
        }

        protected override string got_text()
        {
            return "";
        }

        protected override string aname_text(string item_name)
        {
            return string.Format("A{0} {1}", VOWELS.Contains(item_name[0]) ? "n" : "", item_name);
        }

        protected override string broke_text()
        {
            return "was pilfered.";
        }
    }
}
