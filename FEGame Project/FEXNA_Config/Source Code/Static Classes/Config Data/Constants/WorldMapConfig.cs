using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FEXNA.Constants
{
    public class WorldMap
    {
        public const int WORLDMAP_CONTROLS_FADE_TIME = 20;
        public const int WORLDMAP_FADE_TIME = 60;
        public const int WORLDMAP_ZOOM_FADE_TIME = 60;
        public const int WORLDMAP_SCROLL_SPEED = 16;
        public const int WORLDMAP_EVENT_SCROLL_SPEED = 8;
        public const int WORLDMAP_MODE_SWITCH_DELAY = 10;

        public readonly static Vector2 WORLDMAP_EVENT_OFFSET = new Vector2(0, -24);
        public readonly static Vector2 WORLDMAP_MAP_OFFSET = new Vector2(0, 0);
        public readonly static Vector2 WORLDMAP_MINIMAP_SCALE = new Vector2(0.03f, 0.03f);
        public readonly static Vector2 WORLDMAP_MINIMAP_OFFSET = new Vector2(0, 0);
        public readonly static Vector2 WORLDMAP_MAP_SPRITE_OFFSET = new Vector2(16, 8);

        public readonly static List<string> HARD_MODE_BLOCKED = new List<string> { };

        public const string WORLDMAP_THEME = "A Hint of Things to Come";

        public const bool SEPARATE_CHAPTERS_INTO_ARCS = true;
        // the uther introduction arc, the foreign lords arc, the border defense arc, the Solomon's folly arc
        // then the isolated defense arc, prince of doom, narrow rescue, uncovering the truth, ongoing prophecy
        public readonly static List<string> GAME_ARCS = new List<string>
        {
            "Act 1", "Act 2", "Act 3", "Act 4"
        };

        public readonly static Dictionary<string, HashSet<string>> ARC_CHAPTERS =
            new Dictionary<string, HashSet<string>>
            {
                { "Act 1", new HashSet<string> { "P", "Ch2", "Ch5" } },
                { "Act 2", new HashSet<string> { "Ch1", "Ch1x", "Ch3", "Ch4", "Ch4x" } },
                { "Act 3", new HashSet<string> { "Ch6", "Ch7", "Ch7x", "Ch9" } },
                { "Act 4", new HashSet<string> { "Ch8", "Ch10", "Ch11", "Ch12" } },
            };
    }
}
