using System;
using System.Collections.Generic;
using System.IO;
using ArrayExtension;
using FEXNAVersionExtension;

namespace FEXNA
{
    enum Animation_Modes { Full, Player_Only, Map, Solo }
    enum Message_Speeds { Slow, Normal, Fast, Max }
    enum Hp_Gauge_Modes { Basic, Advanced, Injured, Off }
    enum Options
    {
        Animation_Mode, Game_Speed, Text_Speed, Combat_Window, Unit_Window, Enemy_Window, Terrain_Window, Objective_Window,
        Grid, Range_Preview, Hp_Gauges, Controller, Subtitle_Help, Autocursor, Auto_Turn_End, Music_Volume, Sound_Volume, Window_Color
    }
    public class Game_Options
    {
        const int DATA_COUNT = 18;
        public byte[] Data = new byte[DATA_COUNT];

        #region Serialization
        public void write(BinaryWriter writer)
        {
            Data.write(writer);
        }

        public static Game_Options read(BinaryReader reader)
        {
            Game_Options result = new Game_Options();
            byte[] data = result.Data.read(reader);

            if (Global.LOADED_VERSION.older_than(0, 5, 0, 9))
            {
                data[(int)Options.Grid] = (byte)(data[(int)Options.Grid] == 0 ? 8 : 0);
                data[(int)Options.Music_Volume] = (byte)(data[(int)Options.Music_Volume] == 0 ? 100 : 0);
                data[(int)Options.Sound_Volume] = (byte)(data[(int)Options.Sound_Volume] == 0 ? 100 : 0);
            }

            if (Global.ignore_options_load)
                result.Data = Global.game_options.Data;
            else
                result.Data = data;
            Global.ignore_options_load = false;
            if (result.Data.Length != DATA_COUNT)
            {
                result.reset_options();
                throw new EndOfStreamException("Options Data does not contain the correct amount of entries");
            }
            return result;
        }

        public void post_read()
        {
            update_music_volum();
            update_sound_volume();
        }
        #endregion

        #region Accessors
        public byte animation_mode { get { return Data[(int)Options.Animation_Mode]; } set { Data[(int)Options.Animation_Mode] = value; } }
        public byte game_speed { get { return Data[(int)Options.Game_Speed]; } set { Data[(int)Options.Game_Speed] = value; } }
        public byte text_speed { get { return Data[(int)Options.Text_Speed]; } set { Data[(int)Options.Text_Speed] = value; } }
        // Unused //Yeti
        public byte combat_window { get { return Data[(int)Options.Combat_Window]; } set { Data[(int)Options.Combat_Window] = value; } }
        public byte unit_window { get { return Data[(int)Options.Unit_Window]; } set { Data[(int)Options.Unit_Window] = value; } }
        public byte enemy_window { get { return Data[(int)Options.Enemy_Window]; } set { Data[(int)Options.Enemy_Window] = value; } }
        public byte terrain_window { get { return Data[(int)Options.Terrain_Window]; } set { Data[(int)Options.Terrain_Window] = value; } }
        public byte objective_window { get { return Data[(int)Options.Objective_Window]; } set { Data[(int)Options.Objective_Window] = value; } }
        public byte grid { get { return Data[(int)Options.Grid]; } set { Data[(int)Options.Grid] = value; } }
        public byte range_preview { get { return Data[(int)Options.Range_Preview]; } set { Data[(int)Options.Range_Preview] = value; } }
        public byte hp_gauges { get { return Data[(int)Options.Hp_Gauges]; } set { Data[(int)Options.Hp_Gauges] = value; } }
        public byte controller { get { return Data[(int)Options.Controller]; } set { Data[(int)Options.Controller] = value; } }
        // Unused
        public byte subtitle_help { get { return Data[(int)Options.Subtitle_Help]; } set { Data[(int)Options.Subtitle_Help] = value; } }
        public byte autocursor { get { return Data[(int)Options.Autocursor]; } set { Data[(int)Options.Autocursor] = value; } }
        public byte auto_turn_end { get { return Data[(int)Options.Auto_Turn_End]; } set { Data[(int)Options.Auto_Turn_End] = value; } }
        public byte music_volume
        {
            get { return Data[(int)Options.Music_Volume]; }
            set
            {
                Data[(int)Options.Music_Volume] = value;
                update_music_volum();
            }
        }
        public byte sound_volume
        {
            get { return Data[(int)Options.Sound_Volume]; }
            set
            {
                Data[(int)Options.Sound_Volume] = value;
                update_sound_volume();
            }
        }
        public byte window_color { get { return Data[(int)Options.Window_Color]; } set { Data[(int)Options.Window_Color] = value; } }
        #endregion

        public Game_Options()
        {
            reset_options();
        }

        public void reset_options()
        {
            Data = new byte[DATA_COUNT];
            animation_mode = (int)Animation_Modes.Full;
            game_speed = 0;
            text_speed = (int)Message_Speeds.Fast;
            combat_window = 0;
            unit_window = 0;
            enemy_window = 1;
            terrain_window = 0;
            objective_window = 0;
            grid = 8;
            range_preview = 0;
            hp_gauges = (int)Hp_Gauge_Modes.Injured;
            controller = 0;
            subtitle_help = 1;
            autocursor = 0;
            auto_turn_end = 0;
            Data[(int)Options.Music_Volume] = 50;
            Data[(int)Options.Sound_Volume] = 100;
            //music_on = 0; //Yeti
            //sound_on = 0;
            window_color = 0;
        }

        public void update_music_volum()
        {
            Global.Audio.set_bgm_volume(music_volume / 100f);
        }

        public void update_sound_volume()
        {
            Global.Audio.set_bgs_volume(sound_volume / 100f);
            Global.Audio.set_sfx_volume(sound_volume / 100f);
        }
    }
}
