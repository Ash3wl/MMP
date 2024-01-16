using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FEXNA.Graphics.Help;
using FEXNA.Graphics.Text;
using FEXNA.Windows.UserInterface.Command;
using FEXNA.Windows.UserInterface.Command.Config;

namespace FEXNA.Windows.Command
{
    enum Config_Options { Zoom, Fullscreen, Stereoscopic, Anaglyph, Metrics, Check_For_Updates, Rumble }
    enum ConfigTypes { Number, OnOffSwitch, Button, Input }
    class Window_Config_Options : Window_Command_Scrollbar
    {
        const int TIMER_MAX = 16;
        const int VALUE_OFFSET = 120;
        const int CONTROLS = 12;
        const int ROWS_AT_ONCE = 10;

        private bool Accepting = false;
        private bool Closing = false;
        private int Timer = TIMER_MAX;
        private int Non_Control_Options;
        private int Zoom, Stereoscopic_Level;
        private bool Fullscreen, Anaglyph, Metrics, Updates, Rumble;
        private KeyboardState Previous_Key_State;

        private FE_Text Version_Number;
        private Sprite Black_Screen;

        #region Accessors
        public bool accepting
        {
            get { return Accepting; }
            set
            {
                if (value && this.index == Non_Control_Options)
                {
                    Input.default_keys();
                    reset_controls();
                }
                else
                {
                    Accepting = value;
                    Greyed_Cursor = value;
                    if (this.index < Non_Control_Options + 1)
                        Items[this.index].set_text_color(Accepting ? "Green" : "White");
                    this.active = !value;
                }
            }
        }

        public bool is_option_enabled
        {
            get
            {
                if (this.index == (int)Config_Options.Anaglyph)
                    return Fullscreen && (Stereoscopic_Level > 0);
                return true;
            }
        }

        public bool is_ready { get { return Timer <= 0; } }

        public bool closed { get { return is_ready && Closing; } }

        public int non_control_options { get { return Non_Control_Options; } }

        public override float stereoscopic
        {
            set
            {
                base.stereoscopic = value;
                Version_Number.stereoscopic = value;
            }
        }
        #endregion

        public Window_Config_Options()
        {
            Rows = ROWS_AT_ONCE;

            List<string> strs = new List<string>();
            foreach (Config_Options option in Enum_Values.GetEnumValues(typeof(Config_Options)))
            {
                switch (option)
                {
                    case Config_Options.Zoom:
                        strs.Add("Zoom");
                        break;
                    case Config_Options.Fullscreen:
                        strs.Add("Fullscreen");
                        break;
                    case Config_Options.Stereoscopic:
                        strs.Add("Stereoscopic 3D");
                        break;
                    case Config_Options.Anaglyph:
                        strs.Add("  Red-Cyan (3D)");
                        break;
                    case Config_Options.Metrics:
                        if (!Global.metrics_allowed)
                            break;
                        strs.Add("Metrics");
                        break;
                    case Config_Options.Check_For_Updates:
                        strs.Add("Check for Updates");
                        break;
                    case Config_Options.Rumble:
                        strs.Add("Rumble");
                        break;
                    default:
#if DEBUG
                        throw new IndexOutOfRangeException(string.Format("There is no description text for the option\n\"{0}\" in Window_Config_Options.cs", option));
#endif
                        strs.Add("");
                        break;
                }
            }
            Non_Control_Options = strs.Count;
            strs.Add("Controls:");
            strs.AddRange(new List<string> { "Down", "Left", "Right", "Up",
                "A\nSelect/Confirm", "B\nCancel", "Y\nCursor Speed", "X\nEnemy Range",
                "L\nNext Unit", "R\nStatus", "Start\nSkip/Map", "Select\nMenu" });
            initialize(new Vector2(64, 16), 224, strs);
        }

        protected override void initialize(Vector2 loc, int width, List<string> strs)
        {
            this.loc = loc;
            Width = width;

            Grey_Cursor = new Hand_Cursor();
            Grey_Cursor.tint = new Color(192, 192, 192, 255);
            Grey_Cursor.draw_offset = new Vector2(-16, 0);

            set_items(strs);
            Black_Screen = new Sprite();
            Black_Screen.texture = Global.Content.Load<Texture2D>(@"Graphics/White_Square");
            Black_Screen.dest_rect = new Rectangle(0, 0, Config.WINDOW_WIDTH, Config.WINDOW_HEIGHT);
            Black_Screen.tint = new Color(0, 0, 0, 0);
            // Version Number
            Version_Number = new FE_Text();
            Version_Number.loc = new Vector2(8, Config.WINDOW_HEIGHT - 16);
            Version_Number.Font = "FE7_Text";
            Version_Number.texture = Global.Content.Load<Texture2D>(@"Graphics/Fonts/FE7_Text_White");
            Version_Number.text = "v " + Global.RUNNING_VERSION.ToString();
            update();
        }

        protected override void set_items(List<string> strs)
        {
            if (strs == null)
                return;

            base.set_items(strs);
            initialize_scrollbar();
            refresh_scroll_visibility();
            reset_all();
        }

        protected override void add_commands(List<string> strs)
        {
            var nodes = new List<CommandUINode>();
            for (int i = 0; i < strs.Count; i++)
            {
                ConfigTypes value;
                int options = Enum_Values.GetEnumCount(typeof(Config_Options));
                if (i < options)
                    switch ((Config_Options)i)
                    {
                        case Config_Options.Zoom:
                            value = ConfigTypes.Number;
                            break;
                        case Config_Options.Fullscreen:
                        case Config_Options.Stereoscopic:
                        case Config_Options.Anaglyph:
                        case Config_Options.Metrics:
                        case Config_Options.Check_For_Updates:
                        case Config_Options.Rumble:
                        default:
                            value = ConfigTypes.OnOffSwitch;
                            break;
                    }
                else
                {
                    if (i == options)
                        value = ConfigTypes.Button;
                    else
                        value = ConfigTypes.Input;
                }
                var node = item(new Tuple<string, ConfigTypes>(strs[i], value), i);
                nodes.Add(node);
            }

            set_nodes(nodes);
        }

        protected override CommandUINode item(object value, int i)
        {
            var str = (value as Tuple<string, ConfigTypes>).Item1;
            var type = (value as Tuple<string, ConfigTypes>).Item2;

            var text = new FE_Text();
            text.Font = "FE7_Text";
            text.texture = Global.Content.Load<Texture2D>(@"Graphics\Fonts\FE7_Text_White");
            text.text = str;

            CommandUINode node;
            switch (type)
            {
                case ConfigTypes.Number:
                    node = new NumberUINode("", str, this.column_width);
                    break;
                case ConfigTypes.OnOffSwitch:
                    node = new SwitchUINode("", str, this.column_width);
                    break;
                case ConfigTypes.Button:
                    string description = "";
                    if (i == Non_Control_Options)
                        description = "Reset to Default";
                    node = new ButtonUINode("", str, description, this.column_width);
                    break;
                case ConfigTypes.Input:
                default:
                    Inputs input;
                    switch (str.Split('\n')[0])
                    {
                        case "A":
                            input = Inputs.A;
                            break;
                        case "B":
                            input = Inputs.B;
                            break;
                        case "Y":
                            input = Inputs.Y;
                            break;
                        case "X":
                            input = Inputs.X;
                            break;
                        case "L":
                            input = Inputs.L;
                            break;
                        case "R":
                            input = Inputs.R;
                            break;
                        case "Start":
                            input = Inputs.Start;
                            break;
                        case "Select":
                            input = Inputs.Select;
                            break;

                        case "Down":
                            input = Inputs.Down;
                            break;
                        case "Left":
                            input = Inputs.Left;
                            break;
                        case "Right":
                            input = Inputs.Right;
                            break;
                        case "Up":
                            input = Inputs.Up;
                            break;

                        default:
                            input = Inputs.A;
                            break;
                    }
                    string label;
                    if (i < Non_Control_Options + 5)
                        label = str.Split('\n')[0];
                    else
                        label = str.Split('\n')[1];

                    node = new InputUINode("", input, label, this.column_width);
                    break;
            }
            node.loc = item_loc(i);
            return node;
        }

        public void close()
        {
            if (Closing)
                return;
            Timer = TIMER_MAX;
            Closing = true;
        }

        #region Update
        protected override void update_commands(bool input)
        {
            if (Timer > 0)
            {
                Timer--;
                Black_Screen.tint = new Color(0, 0, 0, (Closing ? Timer : (TIMER_MAX - Timer)) * 128 / TIMER_MAX);
            }
            base.update_commands(input);
        }

        protected override void update_movement(bool input)
        {
            int index = this.index;

            base.update_movement(input);
        }

        protected override Vector2 cursor_loc(int index = -1)
        {
            Vector2 loc = base.cursor_loc(index);
            return loc + new Vector2((Accepting ? VALUE_OFFSET : 0), 0);
        }

        protected override void update_input(bool input)
        {
            if (Accepting)
            {
                switch ((Config_Options)this.index)
                {
                    // Zoom
                    case Config_Options.Zoom:
                        if (Global.Input.triggered(Inputs.Right))
                        {
                            int zoom = (int)MathHelper.Clamp(Zoom + 1, Global.zoom_min, Global.zoom_max);
                            if (zoom != Zoom)
                            {
                                reset_zoom(zoom);
                                Global.game_system.play_se(System_Sounds.Menu_Move2);
                            }
                        }
                        else if (Global.Input.triggered(Inputs.Left))
                        {
                            int zoom = (int)MathHelper.Clamp(Zoom - 1, Global.zoom_min, Global.zoom_max);
                            if (zoom != Zoom)
                            {
                                reset_zoom(zoom);
                                Global.game_system.play_se(System_Sounds.Menu_Move2);
                            }
                        }
                        break;
                    // Fullscreen
                    case Config_Options.Fullscreen:
                        if (Global.Input.triggered(Inputs.Right) || Global.Input.triggered(Inputs.Left))
                        {
                            reset_fullscreen(!Fullscreen);
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                        }
                        break;
                    // Stereoscopic
                    case Config_Options.Stereoscopic:
                        if (Global.Input.repeated(Inputs.Right))
                        {
                            int stereoscopic = (int)MathHelper.Clamp(Stereoscopic_Level + 1, 0, Global.MAX_STEREOSCOPIC_LEVEL);
                            if (stereoscopic != Stereoscopic_Level)
                            {
                                reset_stereoscopic(stereoscopic);
                                Global.game_system.play_se(System_Sounds.Menu_Move2);
                            }
                        }
                        else if (Global.Input.repeated(Inputs.Left))
                        {
                            int stereoscopic = (int)MathHelper.Clamp(Stereoscopic_Level - 1, 0, Global.MAX_STEREOSCOPIC_LEVEL);
                            if (stereoscopic != Stereoscopic_Level)
                            {
                                reset_stereoscopic(stereoscopic);
                                Global.game_system.play_se(System_Sounds.Menu_Move2);
                            }
                        }
                        break;
                    // Anaglyph
                    case Config_Options.Anaglyph:
                        if (Global.Input.triggered(Inputs.Right) || Global.Input.triggered(Inputs.Left))
                        {
                            reset_anaglyph(!Anaglyph);
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                        }
                        break;
                    // Metrics
                    case Config_Options.Metrics:
                        if (Global.Input.triggered(Inputs.Right) || Global.Input.triggered(Inputs.Left))
                        {
                            reset_metrics(!Metrics);
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                        }
                        break;
                    // Check for Updates
                    case Config_Options.Check_For_Updates:
                        if (Global.Input.triggered(Inputs.Right) || Global.Input.triggered(Inputs.Left))
                        {
                            reset_updates(!Updates);
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                        }
                        break;
                    // Rumble
                    case Config_Options.Rumble:
                        if (Global.Input.triggered(Inputs.Right) || Global.Input.triggered(Inputs.Left))
                        {
                            reset_rumble(!Rumble);
                            Global.game_system.play_se(System_Sounds.Menu_Move2);
                        }
                        break;
                }
            }
        }
        #endregion

        #region Controls
        public void set()
        {

            switch (this.index)
            {
                // Zoom
                case (int)Config_Options.Zoom:
                    set_zoom();
                    break;
                // Fullscreen
                case (int)Config_Options.Fullscreen:
                    set_fullscreen();
                    break;
                // Stereoscopic
                case (int)Config_Options.Stereoscopic:
                    set_stereoscopic();
                    break;
                // Anaglyph
                case (int)Config_Options.Anaglyph:
                    set_anaglyph();
                    break;
                // Metrics
                case (int)Config_Options.Metrics:
                    set_metrics();
                    break;
                // Check for Updates
                case (int)Config_Options.Check_For_Updates:
                    set_updates();
                    break;
                // Rumble
                case (int)Config_Options.Rumble:
                    set_rumble();
                    break;
            }
        }

        public void reset()
        {
            switch (this.index)
            {
                // Zoom
                case (int)Config_Options.Zoom:
                    reset_zoom();
                    break;
                // Fullscreen
                case (int)Config_Options.Fullscreen:
                    reset_fullscreen();
                    break;
                // Stereoscopic
                case (int)Config_Options.Stereoscopic:
                    reset_stereoscopic();
                    break;
                // Anaglyph
                case (int)Config_Options.Anaglyph:
                    reset_anaglyph();
                    break;
                // Metrics
                case (int)Config_Options.Metrics:
                    reset_metrics();
                    break;
                // Check for Updates
                case (int)Config_Options.Check_For_Updates:
                    reset_updates();
                    break;
                // Rumble
                case (int)Config_Options.Rumble:
                    reset_rumble();
                    break;
            }
        }
        private void reset_all()
        {
            reset_zoom();
            reset_fullscreen();
            reset_stereoscopic();
            reset_anaglyph();
            if (Global.metrics_allowed)
                reset_metrics();
            reset_updates();
            reset_rumble();
            reset_controls();
        }

        // Zoom
        private void set_zoom()
        {
            Global.zoom = Zoom;
        }

        private void reset_zoom()
        {
            reset_zoom(Global.zoom);
        }
        private void reset_zoom(int value)
        {
            Zoom = value;
            (Items[(int)Config_Options.Zoom] as NumberUINode).set_value(Zoom);
        }

        // Fullscreen
        private void set_fullscreen()
        {
            Global.fullscreen = Fullscreen;
        }

        private void reset_fullscreen()
        {
            reset_fullscreen(Global.fullscreen);
        }
        private void reset_fullscreen(bool value)
        {
            Fullscreen = value;
            (Items[(int)Config_Options.Fullscreen] as SwitchUINode).set_switch(Fullscreen);

            reset_anaglyph();
        }

        // Stereoscopic 3D
        private void set_stereoscopic()
        {
            Global.stereoscopic_level = Stereoscopic_Level;
        }

        private void reset_stereoscopic()
        {
            reset_stereoscopic(Global.stereoscopic_level);
        }
        private void reset_stereoscopic(int value)
        {
            Stereoscopic_Level = value;
            bool stereoscopy = Stereoscopic_Level > 0;
            (Items[(int)Config_Options.Stereoscopic] as SwitchUINode).set_switch(
                stereoscopy, stereoscopy ? Stereoscopic_Level.ToString() : "");

            reset_anaglyph();
        }

        // Anaglyph
        private void set_anaglyph()
        {
            Global.anaglyph = Anaglyph;
        }

        private void reset_anaglyph()
        {
            reset_anaglyph(Global.anaglyph);
        }
        private void reset_anaglyph(bool value)
        {
            if (!Fullscreen || Stereoscopic_Level == 0)
            {
                (Items[(int)Config_Options.Anaglyph] as SwitchUINode).locked = true;
            }
            else
            {
                (Items[(int)Config_Options.Anaglyph] as SwitchUINode).locked = false;
            }

            Anaglyph = value;
            (Items[(int)Config_Options.Anaglyph] as SwitchUINode).set_switch(
                Stereoscopic_Level > 0 && (!Fullscreen || Anaglyph));
        }

        // Metrics
        private void set_metrics()
        {
            Global.metrics = Metrics ? Metrics_Settings.On : Metrics_Settings.Off;
        }

        private void reset_metrics()
        {
            reset_metrics(Global.metrics == Metrics_Settings.On);
        }
        private void reset_metrics(bool value)
        {
            Metrics = value;
            (Items[(int)Config_Options.Metrics] as SwitchUINode).set_switch(Metrics);
        }

        // Check for Updates
        private void set_updates()
        {
            Global.updates_active = Updates;
        }

        private void reset_updates()
        {
            reset_updates(Global.updates_active);
        }
        private void reset_updates(bool value)
        {
            Updates = value;
            (Items[(int)Config_Options.Check_For_Updates] as SwitchUINode).set_switch(Updates);
        }

        // Rumble
        private void set_rumble()
        {
            Global.rumble = Rumble;
        }

        private void reset_rumble()
        {
            reset_rumble(Global.rumble);
        }
        private void reset_rumble(bool value)
        {
            Rumble = value;
            (Items[(int)Config_Options.Rumble] as SwitchUINode).set_switch(Rumble);
        }

        // Controls
        private void reset_controls()
        {
            // handled automatically now by the control refreshing itself? //Debug
            //for (int i = 0; i < CONTROLS; i++) //Debug
                //(Values[i + Non_Control_Options + 1] as FE_Text).text = Input.key_name(i);
            //    (Values[i + Non_Control_Options + 1] as Keyboard_Icon).letter = Input.key_name(i);
        }

        public void start_key_config(KeyboardState key_state)
        {
            Previous_Key_State = key_state;
        }

        public void update_key_config(KeyboardState key_state)
        {
            KeyboardState state = new KeyboardState(Keys.OemAuto, Keys.Attn, Keys.Zoom);
            if (!Input.REMAPPABLE_KEYS.Keys.Intersect(Previous_Key_State.GetPressedKeys()).Any())
            {
                if (key_state.IsKeyDown(Keys.Escape))
                    accepting = false;
                else
                {
                    foreach (Keys key in Input.REMAPPABLE_KEYS.Keys.Intersect(key_state.GetPressedKeys()))
                        if (Input.remap_key(this.index - (Non_Control_Options + 1), key))
                        {
                            accepting = false;
                            reset_controls();
                            break;
                        }
                }
            }
            Previous_Key_State = key_state;
        }
        #endregion

        public override void draw(SpriteBatch sprite_batch)
        {
            sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Black_Screen.draw(sprite_batch);
            sprite_batch.End();

            if (is_ready)
            {
                base.draw(sprite_batch);

                sprite_batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Version_Number.draw(sprite_batch);
                sprite_batch.End();
            }
        }
    }
}
