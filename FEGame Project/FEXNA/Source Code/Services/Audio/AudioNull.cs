using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
#if MONOGAME
using Microsoft.Xna.Framework.Audio;
#else
using MonoGame.Framework.Audio;
#endif
using Microsoft.Xna.Framework.Content;
using FEXNA_Library;

namespace FEXNA.Services.Audio
{
    public class AudioNull
    {
        public class AudioNullService : IAudioService
        {
            private AudioNull _audio;

            public AudioNullService()
            {
                _audio = new AudioNull();
            }

            public bool playing_map_theme() { return false; }

            public void update() { }

            public void post_update() { }

            public void set_pitch_global_var(string var_name, float value) { }

            public void stop() { }

            #region BGM
            public void set_bgm_volume(float volume) { }

            public void play_map_bgm(string cue_name) { }
            public void play_map_bgm(string cue_name, bool fade) { }
            public void play_map_bgm(string cue_name, bool fade, bool force_restart) { }

            public void play_bgm(string cue_name) { }
            public void play_bgm(string cue_name, bool fade) { }

            public bool is_playing(string cue_name) { return false; }

            public void stop_bgm() { }

            public void bgm_fade() { }
            public void bgm_fade(int time) { }

            public void clear_map_theme() { }
            #endregion

            #region BGS
            public void set_bgs_volume(float volume) { }

            public void play_bgs(string cue_name) { }

            public void stop_bgs() { }
            #endregion

            #region SFX
            public void set_sfx_volume(float volume) { }

            public void play_se(string bank, string cue_name, Maybe<float> pitch, Maybe<int> channel) { }
            public void play_system_se(string bank, string cue_name, bool priority, Maybe<float> pitch = default(Maybe<float>)) { }

            public bool playing_system_sound() { return false; }
            public void cancel_system_sound() { }

            public void stop_sfx() { }

            public void sfx_fade() { }
            public void sfx_fade(int time) { }
            #endregion

            #region ME
            public void play_me(string bank, string cue_name) { }

            public bool stop_me() { return false; }
            public bool stop_me(bool bgm_stop) { return false; }
            #endregion
        }
    }
}
