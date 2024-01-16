using Microsoft.Xna.Framework;

namespace FEXNA.Map
{
    public class Fow_View_Object
    {
        public Vector2 loc;
        protected int Vision;

        protected Fow_View_Object() { }
        public Fow_View_Object(Vector2 loc, int vision)
        {
            this.loc = loc;
            Vision = vision;
        }

        public virtual int vision()
        {
            return Vision;
        }
    }
}
