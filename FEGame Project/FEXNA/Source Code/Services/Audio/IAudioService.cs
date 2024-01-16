using FEXNA_Library;

namespace FEXNA.Services.Audio
{
    public interface IAudioService
    {
        bool playing_map_theme();

        void update();

        void post_update();

        void set_pitch_global_var(string var_name, float value);

        void stop();

        #region BGM
        void set_bgm_volume(float volume);

        void play_map_bgm(string cue_name);
        void play_map_bgm(string cue_name, bool fade);
        void play_map_bgm(string cue_name, bool fade, bool force_restart);

        void play_bgm(string cue_name);
        void play_bgm(string cue_name, bool fade);

        bool is_playing(string cue_name);

        void stop_bgm();

        void bgm_fade();
        void bgm_fade(int time);

        void clear_map_theme();
        #endregion

        #region BGS
        void set_bgs_volume(float volume);

        void play_bgs(string cue_name);

        void stop_bgs();
        #endregion

        #region SFX
        void set_sfx_volume(float volume);

        void play_se(string bank, string cue_name,
            Maybe<float> pitch = default(Maybe<float>),
            Maybe<int> channel = default(Maybe<int>));
        void play_system_se(string bank, string cue_name, bool priority, Maybe<float> pitch);

        bool playing_system_sound();
        void cancel_system_sound();

        void stop_sfx();

        void sfx_fade();
        void sfx_fade(int time);
        #endregion

        #region ME
        void play_me(string bank, string cue_name);

        bool stop_me();
        bool stop_me(bool bgm_stop);
        #endregion
    }
}
