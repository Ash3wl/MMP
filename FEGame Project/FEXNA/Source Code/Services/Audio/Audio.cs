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
    public class Audio_Engine
    {
        public class Audio_Service : IAudioService
        {
            private Audio_Engine _audio;

            public Audio_Service()
            {
                _audio = new Audio_Engine();
            }

            public bool playing_map_theme()
            {
                return _audio.playing_map_theme;
            }

            public void update()
            {
                _audio.update();
            }

            public void post_update()
            {
                _audio.post_update();
            }

            public void set_pitch_global_var(string var_name, float value)
            {
                _audio.set_pitch_global_var(var_name, value);
            }

            public void stop()
            {
                _audio.stop();
            }

            #region BGM
            public void set_bgm_volume(float volume)
            {
                _audio.set_bgm_volume(volume);
            }

            public void play_map_bgm(string cue_name)
            {
                _audio.play_map_bgm(cue_name);
            }
            public void play_map_bgm(string cue_name, bool fade)
            {
                _audio.play_map_bgm(cue_name, fade);
            }
            public void play_map_bgm(string cue_name, bool fade, bool force_restart)
            {
                _audio.play_map_bgm(cue_name, fade, force_restart);
            }

            public void play_bgm(string cue_name)
            {
                _audio.play_bgm(cue_name);
            }
            public void play_bgm(string cue_name, bool fade)
            {
                _audio.play_bgm(cue_name, fade);
            }

            public bool is_playing(string cue_name)
            {
                return _audio.is_playing(cue_name);
            }

            public void stop_bgm()
            {
                _audio.stop_bgm();
            }

            public void bgm_fade()
            {
                _audio.bgm_fade();
            }
            public void bgm_fade(int time)
            {
                _audio.bgm_fade(time);
            }

            public void clear_map_theme()
            {
                _audio.clear_map_theme();
            }
            #endregion

            #region BGS
            public void set_bgs_volume(float volume)
            {
                _audio.set_bgs_volume(volume);
            }

            public void play_bgs(string cue_name)
            {
                _audio.play_bgs(cue_name);
            }

            public void stop_bgs()
            {
                _audio.stop_bgs();
            }
            #endregion

            #region SFX
            public void set_sfx_volume(float volume)
            {
                _audio.set_sfx_volume(volume);
            }

            public void play_se(string bank, string cue_name,
                Maybe<float> pitch = default(Maybe<float>),
                Maybe<int> channel = default(Maybe<int>))
            {
                _audio.play_se(bank, cue_name, pitch, channel);
            }
            public void play_system_se(string bank, string cue_name, bool priority, Maybe<float> pitch = default(Maybe<float>))
            {
                _audio.prepare_system_se(bank, cue_name, priority, pitch);
            }

            public bool playing_system_sound()
            {
                return _audio.playing_system_sound();
            }
            public void cancel_system_sound()
            {
                _audio.cancel_system_sound();
            }

            public void stop_sfx()
            {
                _audio.stop_sfx();
            }

            public void sfx_fade()
            {
                _audio.sfx_fade();
            }
            public void sfx_fade(int time)
            {
                _audio.sfx_fade(time);
            }
            #endregion

            #region ME
            public void play_me(string bank, string cue_name)
            {
                _audio.play_me(bank, cue_name);
            }

            public bool stop_me()
            {
                return _audio.stop_me();
            }
            public bool stop_me(bool bgm_stop)
            {
                return _audio.stop_me(bgm_stop);
            }
            #endregion
        }

        const int SIMULTANEOUS_SOUNDS = 26;
        private static Dictionary<string, int[]> LOOP_DATA = new Dictionary<string, int[]>();
        private static Dictionary<string, byte[]> SOUND_DATA = new Dictionary<string, byte[]>();

        private SoundEffectInstance Map_Theme, Music;
        //private List<SoundEffectInstance> Playing_Sounds = new List<SoundEffectInstance>();
        private ChannelSound[] Playing_Sounds = new ChannelSound[SIMULTANEOUS_SOUNDS];
        private SoundEffectInstance System_Sound;
        private SoundEffectInstance Background_Sound, Music_Effect;
        private bool ME_Pause, System_SFX_Priority = false;
        private int Music_Fade_Out_Time = 0, Music_Fade_In_Time = 0, Music_Fade_Timer = 0;
        private int Bgs_Fade_Out_Time = 0, Bgs_Fade_Timer = 0;
        private int Sound_Fade_Out_Time = 0, Sound_Fade_Timer = 0;
        private string New_Music = "", Playing_Name = "", Saved_Map_Theme_Name = "";
        private bool Playing_Map_Theme, New_Music_Map, New_Music_Fade;
        private float Music_Volume = 1, Sound_Volume = 1, Bgs_Volume = 1;
        private float Music_Fade_Volume = 1, Sound_Fade_Volume = 1, Bgs_Fade_Volume = 1;
        private Dictionary<string, float> PITCHES = new Dictionary<string, float>();
        private Sound_Name_Data New_System_Sound_Data;

        #region Accessors
        private bool music_muted { get { return Global.game_options.music_volume == 1; } }
        private bool sound_muted { get { return Global.game_options.sound_volume == 1; } }

        private bool music_fading_out { get { return Music_Fade_Out_Time > 0; } }
        private bool music_fading_in { get { return Music_Fade_In_Time > 0; } }

        private bool bgs_fading_out { get { return Bgs_Fade_Out_Time > 0; } }
        private bool sound_fading_out { get { return Sound_Fade_Out_Time > 0; } }

        private bool playing_map_theme { get { return Playing_Map_Theme; } }

        private bool too_many_active_sounds
        {
            get
            {
                return !Playing_Sounds.Any(x => x == null);
            }
        }
        #endregion

        private Audio_Engine() { }

        private void update()
        {
            if (Music != null)
            {
                if (!Music.IsLooped && Music.State == SoundState.Stopped)
                {
                    stop_bgm();
                }
            }
            if (Music_Effect != null)
            {
                if (Music_Effect.State == SoundState.Stopped)
                {
                    stop_me();
                }
            }
            else
                update_bgm_fade();
            update_sounds();
            update_bgs_fade();
        }

        private void post_update()
        {
            if (New_System_Sound_Data != null)
                play_system_se(New_System_Sound_Data.Bank, New_System_Sound_Data.Name, New_System_Sound_Data.Priority, New_System_Sound_Data.Pitch);
            New_System_Sound_Data = null;
        }

        protected void update_bgm_fade()
        {
            if (music_fading_out)
            {
                Music_Fade_Timer++;
                if (Music_Fade_Timer >= Music_Fade_Out_Time)
                {
                    cancel_music_fade_out();
                    // If waiting for the fade to finish to play a new bgm
                    if (New_Music != "")
                    {
                        // Play the bgm
                        if (New_Music_Map)
                            play_map_bgm(New_Music, New_Music_Fade);
                        else
                            play_bgm(New_Music, New_Music_Fade);
                        // Clear new bgm variables
                        clear_new_music();
                    }
                    else
                    {
                        if (Playing_Map_Theme)
                            pause_map_theme();
                        else
                            stop_bgm();
                        set_bgm_fade_volume(1);
                    }
                }
                else
                    //if (!music_muted)
                        set_bgm_fade_volume((1f * (Music_Fade_Out_Time - Music_Fade_Timer)) / Music_Fade_Out_Time);
            }
            else if (music_fading_in)
            {
                Music_Fade_Timer++;
                if (Music_Fade_Timer >= Music_Fade_In_Time)
                {
                    Music_Fade_Timer = 0;
                    Music_Fade_In_Time = 0;
                    //if (!music_muted)
                        set_bgm_fade_volume(1);
                }
                else
                    //if (!music_muted)
                        set_bgm_fade_volume((1f * Music_Fade_Timer) / Music_Fade_In_Time);
            }
        }

        protected void update_sounds()
        {
            for (int i = 0; i < Playing_Sounds.Length; )
            {
                if (Playing_Sounds[i] != null)
                    if (!Playing_Sounds[i].IsLooped && Playing_Sounds[i].State == SoundState.Stopped)
                    {
                        Playing_Sounds[i].Dispose();
                        pop_sound_ids(i);
                        continue;
                    }
                i++;
            }
            if (System_Sound != null)
            {
                if (!System_Sound.IsLooped && System_Sound.State == SoundState.Stopped)
                {
                    cancel_system_sound();
                }
            }
            /*int i = 0;
            while (i < Playing_Sounds.Count)
            {
                if (!Playing_Sounds[i].IsPlaying)
                    Playing_Sounds.RemoveAt(i);
                else
                    i++;
            }*/
            update_sfx_fade();
        }

        protected void update_bgs_fade()
        {
            if (bgs_fading_out)
            {
                Bgs_Fade_Timer++;
                if (Bgs_Fade_Timer >= Bgs_Fade_Out_Time)
                {
                    cancel_bgs_fade_out();
                    stop_bgs();
                    //if (!sound_muted)
                        set_bgs_fade_volume(1);
                }
                else
                    //if (!sound_muted)
                        set_bgs_fade_volume((1f * (Bgs_Fade_Out_Time - Bgs_Fade_Timer)) / Bgs_Fade_Out_Time);
            }
        }

        protected void update_sfx_fade()
        {
            if (sound_fading_out)
            {
                Sound_Fade_Timer++;
                if (Sound_Fade_Timer >= Sound_Fade_Out_Time)
                {
                    cancel_sound_fade_out();
                    stop_sfx();
                    //if (!sound_muted)
                        set_sfx_fade_volume(1);
                }
                else
                    //if (!sound_muted)
                        set_sfx_fade_volume((1f * (Sound_Fade_Out_Time - Sound_Fade_Timer)) / Sound_Fade_Out_Time);
            }
        }

        private void set_pitch_global_var(string var_name, float value)
        {
            PITCHES[var_name] = value;
        }

        private void stop()
        {
            stop_bgm();
            stop_bgs();
            stop_sfx();
        }

        #region BGM
        private void set_bgm_volume(float volume)
        {
            Music_Volume = MathHelper.Clamp(volume, 0, 1);
            set_bgm_fade_volume(Music_Fade_Volume);
        }
        private void set_bgm_fade_volume(float volume)
        {
            Music_Fade_Volume = volume;
            if (Music != null)
                Music.Volume = volume * Music_Volume;
            if (Map_Theme != null)
                Map_Theme.Volume = volume * Music_Volume;
            if (Music_Effect != null)
                Music_Effect.Volume = volume * Music_Volume;
        }

        private void play_map_bgm(string cue_name, bool fade = false, bool force_restart = false)
        {
            if (force_restart)
                clear_map_theme();
            play_bgm(cue_name, fade, true);
        }

        private void play_bgm(string cue_name)
        {
            play_bgm(cue_name, false);
        }
        private void play_bgm(string cue_name, bool fade)
        {
            play_bgm(cue_name, fade, false);
        }
        private void play_bgm(string cue_name, bool fade, bool map)
        {
            // If resuming a map theme
            if (map && !music_fading_out && cue_name == Saved_Map_Theme_Name && Map_Theme != null)
            {
                stop_bgm();
                Music = Map_Theme;
                Map_Theme = null;
                fade_new_music_in(fade);
                Music.Resume();
                Playing_Name = cue_name;
                Playing_Map_Theme = map;
            }
            // Else playing a new theme
            else
            {
                if (map && !music_fading_out)
                    clear_map_theme();

                try
                {
                    // If the same bgm that was already playing, just restore volume/cancel fade
                    if (is_playing(cue_name))
                    {
                        cancel_music_fade_out();
                        set_bgm_fade_volume(1);
                        if (map)
                        {
                            clear_map_theme();
                            Saved_Map_Theme_Name = cue_name;
                        }
                    }
                    // If fading out, wait for the fade to finish before playing
                    else if (music_fading_out)
                    {
                        New_Music = cue_name;
                        New_Music_Fade = fade;
                        New_Music_Map = map;
                    }
                    // Else play now
                    else
                    {
                        SoundEffectInstance instance = get_music(cue_name);
                        if (instance == null)
                            return;

                        instance.Volume = Music_Fade_Volume * Music_Volume;
                        play_bgm_cue(instance, fade);
                        Playing_Name = cue_name;
                        if (map)
                        {
                            clear_map_theme();
                            Saved_Map_Theme_Name = cue_name;
                        }
                        Playing_Map_Theme = map;
                    }
                }
                catch (FileNotFoundException e)
                {
#if DEBUG
                    Print.message("Tried to play nonexistant BGM: " + cue_name);
#endif
                }
                catch (ContentLoadException e)
                {
#if DEBUG
                    Print.message("Tried to play nonexistant BGM: " + cue_name);
#endif
                }
#if __ANDROID__
                catch (Java.IO.FileNotFoundException e)
                {
                }
#endif
            }
        }

        private static SoundEffectInstance get_music(string cue_name)
        {
			SoundEffect song = null;
            SoundEffectInstance music = null;
            int intro_start = 0, loop_start = -1, loop_length = -1;

            NVorbis.VorbisReader vorbis = null;
            try
            {
                try
				{
                    Stream cue_stream = TitleContainer.OpenStream(@"Content\Audio\BGM\" + cue_name + ".ogg");

                    
#if __ANDROID__
					MemoryStream stream = new MemoryStream();
					cue_stream.CopyTo(stream);
                    vorbis = new NVorbis.VorbisReader(stream, cue_name, true);
#else
                    vorbis = new NVorbis.VorbisReader(cue_stream, cue_name, true);
#endif
                    get_loop_data(vorbis, out intro_start, out loop_start, out loop_length);


                    // If the loop points are set past the end of the song, don't play
                    if (vorbis.TotalSamples < loop_start || vorbis.TotalSamples < loop_start + loop_length)
                    {
#if DEBUG
                        throw new IndexOutOfRangeException("Loop points are set past the end of the song");
#endif
#if __ANDROID__
                        cue_stream.Dispose();
                        vorbis.Dispose();
#else
                        vorbis.Dispose();
#endif
                        throw new FileNotFoundException();
                    }
#if __ANDROID__
					cue_stream.Dispose();
#endif
                    
                }
                catch (FileNotFoundException ex)
                {
                    throw;
                }
#if __ANDROID__
                catch (Java.IO.FileNotFoundException e)
                {
                    throw;
                }
#endif
            }
            // If loaded as an ogg failed, try loading as a SoundEffect
            catch (FileNotFoundException ex)
            {
                intro_start = 0;
                loop_start = -1;
                loop_length = -1;
                song = Global.Content.Load<SoundEffect>(@"Audio/" + cue_name);
            }

            // If the file is an ogg file and was found and initialized successfully
            if (vorbis != null)
            {
                music = get_vorbis_music(vorbis, cue_name,
                    intro_start, loop_start, loop_length);
            }
            else
            {
                music = get_effect_music(song, cue_name, intro_start, loop_start, loop_length);
            }

            if (music != null)
                music.IsLooped = true;
#if !__ANDROID__
            if (song != null)
				song.Dispose();
#endif
            return music;
        }
        private static SoundEffectInstance get_vorbis_music(
            NVorbis.VorbisReader vorbis, string cue_name,
            int intro_start, int loop_start, int loop_length)
        {
            SoundEffectInstance music;
#if __ANDROID__
            SoundEffect sound_effect = SoundEffectStreamed.FromVorbis(
                vorbis, intro_start, loop_start, loop_start + loop_length);
            music = sound_effect.CreateInstance();
            music.AlsoDisposeEffect();
#else
            if (loop_start != -1)
                music = new SoundEffectInstance(vorbis, intro_start, loop_start, loop_start + loop_length);
            else
                music = new SoundEffectInstance(vorbis, 0, -1, -1);
#endif

            return music;
        }
        private static SoundEffectInstance get_effect_music(
            SoundEffect song, string cue_name,
            int intro_start, int loop_start, int loop_length)
        {
            SoundEffectInstance music;
#if __ANDROID__
            if (song == null)
                return null;
            music = song.CreateInstance();
            music.AlsoDisposeEffect();
#else
                if (loop_start != -1)
                    music = new SoundEffectInstance(song, intro_start, loop_start, loop_start + loop_length);
                else
                    music = new SoundEffectInstance(song);
#endif

            return music;
        }

        private void clear_new_music()
        {
            New_Music = "";
            New_Music_Fade = false;
            New_Music_Map = false;
        }

        private bool is_playing(string cue_name)
        {
            if (Playing_Name == "")
                return false;
            return cue_name == Playing_Name;
        }

        private void play_bgm_cue(SoundEffectInstance instance, bool fade)
        {
            if (Playing_Map_Theme)
                pause_map_theme();
            stop_bgm();
            Music = instance;
            // If playing a music effect, wait for it to end before playing
            if (Music_Effect != null)
            {
                Music.Pause();
                ME_Pause = true;
            }
            else
                Music.Play();
            fade_new_music_in(fade);
        }

        private void fade_new_music_in(bool fade)
        {
            if (fade)
                fade_in();
            else if (!music_muted)
                set_bgm_fade_volume(1);
        }

        private void stop_bgm()
        {
            Playing_Name = "";
            if (Music != null)
            {
                Music.Stop();
                Music.Dispose();
            }
            Music = null;
            cancel_music_fade_out();
            set_bgm_fade_volume(1);
        }

        private void fade_in()
        {
            fade_in(30);
        }
        private void fade_in(int time)
        {
            if (time > 0)
            {
                set_bgm_fade_volume(0);
                Music_Fade_In_Time = time;
                if (!music_fading_out)
                    Music_Fade_Timer = 0;
            }
            else if (!music_muted)
                set_bgm_fade_volume(1);
        }

        private void bgm_fade()
        {
            bgm_fade(60);
        }
        private void bgm_fade(int time)
        {
            if (time > 0)
            {
                // If already in the middle of a fade out, clear the new music that would normally start playing, since this fade probably wanted to apply
                if (music_fading_out)
                    clear_new_music();
                else if (Music != null || Background_Sound != null)
                {
                    //if (!music_muted)
                    set_bgm_fade_volume(1);
                    Music_Fade_Out_Time = time;
                    Music_Fade_Timer = 0;
                    // Clears present fade in; if a fade in after the out is desired it needs to be called second
                    Music_Fade_In_Time = 0;
                }
            }
            bgs_fade(time);
        }

        private void cancel_music_fade_out()
        {
            Music_Fade_Out_Time = 0;
            Music_Fade_Timer = 0;
        }

        private void pause_map_theme()
        {
            if (Music != null)
            {
                if (Map_Theme != null)
                {
                    Map_Theme.Stop();
                    Map_Theme.Dispose();
                }
                Map_Theme = Music;
                Map_Theme.Pause();
                Music = null;
            }
        }

        private void clear_map_theme()
        {
            if (Map_Theme != null)
            {
                Map_Theme.Stop();
                Map_Theme.Dispose();
            }
            Map_Theme = null;
            Saved_Map_Theme_Name = null;
        }
        #endregion

        #region BGS
        private void set_bgs_volume(float volume)
        {
            Bgs_Volume = MathHelper.Clamp(volume, 0, 1);
            set_bgs_fade_volume(Bgs_Fade_Volume);
        }
        private void set_bgs_fade_volume(float volume)
        {
            Bgs_Fade_Volume = volume;
            if (Background_Sound != null)
                Background_Sound.Volume = volume * Bgs_Volume;
        }

        private void play_bgs(string cue_name)
        {
            stop_bgs();
            Background_Sound = get_music(cue_name); // maybe its own bank *folder //Debug
            if (Background_Sound == null)
                return;

            Background_Sound.Volume = Bgs_Fade_Volume * Bgs_Volume;
            Background_Sound.Play();
            //if (!sound_muted) //Debug
                set_bgs_fade_volume(1);
        }

        private void stop_bgs()
        {
            cancel_bgs_fade_out();
            if (Background_Sound != null)
            {
                Background_Sound.Stop();
                Background_Sound.Dispose();
            }
            Background_Sound = null;
        }

        private void bgs_fade(int time)
        {
            if (time > 0 && !bgs_fading_out)
            {
                //if (!sound_muted) //Debug
                    set_bgs_fade_volume(1);
                Bgs_Fade_Out_Time = time;
                Bgs_Fade_Timer = 0;
            }
        }

        private void cancel_bgs_fade_out()
        {
            Bgs_Fade_Out_Time = 0;
            Bgs_Fade_Timer = 0;
        }
        #endregion

        #region SFX
        private void set_sfx_volume(float volume)
        {
            Sound_Volume = MathHelper.Clamp(volume, 0, 1);
            set_sfx_fade_volume(Sound_Fade_Volume);
        }
        private void set_sfx_fade_volume(float volume)
        {
            Sound_Fade_Volume = volume;
            foreach (var sound in Playing_Sounds)
                if (sound != null)
                    sound.Instance.Volume = volume * Sound_Volume;
            if (System_Sound != null)
                System_Sound.Volume = volume * Sound_Volume;
        }

        private void play_se(string bank, string cue_name, Maybe<float> pitch, Maybe<int> channel)
        {
            // If too many sounds and not important enough
            if (too_many_active_sounds && false)
                return;
            SoundEffectGetter sound = get_sound(bank, cue_name);
            if (sound.Sound == null)
            {
                sound.Dispose();
                return;
            }
            SoundEffectInstance instance = sound_instance(sound, cue_name, pitch);

            add_new_playing_sound(instance, channel);

            /* //Debug
            // If too many sounds 
            if (too_many_active_sounds)
            {
                // And this sound is important enough to remove the oldest one
                if (true)
                {
                    if (!remove_oldest_playing_sound())
                        // This should never return false, but just in case
                        instance.Dispose();
                }
                else
                {
                    instance.Dispose();
                    return null;
                }
            }



            if (instance != null)
            {
                if (instance.State != SoundState.Initial)
                {
                    if (!add_new_playing_sound(instance))
                        // This should never return false, but just in case
                        instance.Dispose();
                }
                else
                    instance.Dispose();
            }*/

            sound.Dispose();
        }
        private void prepare_system_se(string bank, string cue_name, bool priority, Maybe<float> pitch = default(Maybe<float>))
        {
            New_System_Sound_Data = new Sound_Name_Data(bank, cue_name, priority, pitch);
        }
        private void play_system_se(string bank, string cue_name, bool priority, Maybe<float> pitch)
        {
            SoundEffectGetter sound = get_sound(bank, cue_name, true);
            if (sound.Sound == null)
            {
                sound.Dispose();
                return;
            }

            cancel_system_sound();
            System_Sound = sound_instance(sound, cue_name, pitch);
            System_Sound.Play();
            System_SFX_Priority = priority;
            if (System_Sound.State == SoundState.Initial)
            {
                System_Sound.Dispose();
                System_Sound = null;
                System_SFX_Priority = false;
            }
            sound.Dispose();
            return;
        }

        private SoundEffectInstance sound_instance(SoundEffectGetter sound, string cue_name, Maybe<float> pitch)
        {
            SoundEffectInstance instance = sound.Instance;
            instance.Volume = Sound_Fade_Volume * Sound_Volume;
            if (pitch.IsSomething)
                instance.Pitch = pitch;
            else if (PITCHES.ContainsKey(cue_name))
            {
                instance.Pitch = PITCHES[cue_name];
            }

            return instance;
        }

        private static SoundEffectGetter get_sound(string bank, string cue_name)
        {
            return get_sound(bank, cue_name, false);
        }
        private static SoundEffectGetter get_sound(string bank, string cue_name, bool looping)
        {
            string filename = @"Content\Audio\SE\" + bank + @"\" + cue_name + ".ogg";
            SoundEffect sound = null;
            SoundEffectInstance instance = null;
            bool ogg = false;

            try
            {
                sound_effect_from_ogg(out sound, filename);
                ogg = true;
            }
            catch (FileNotFoundException ex)
            {
#if WINDOWS || __ANDROID__
                return new SoundEffectGetter();
#endif
                sound = Global.Content.Load<SoundEffect>(@"Audio/" + filename);
            }
#if __ANDROID__
            catch (Java.IO.FileNotFoundException e)
            {
                return new SoundEffectGetter();
            }
#endif

            sound.Name = cue_name;
#if __ANDROID__
            instance = sound.CreateInstance();
            instance.AlsoDisposeEffect();
#else
            if (looping && LOOP_DATA.ContainsKey(filename) && LOOP_DATA[filename][1] != -1)
            {
                instance = new SoundEffectInstance(sound, LOOP_DATA[filename][0], LOOP_DATA[filename][1], LOOP_DATA[filename][1] + LOOP_DATA[filename][2]);
                instance.IsLooped = true;
            }
            else
                instance = new SoundEffectInstance(sound);
#endif
            return new SoundEffectGetter(ogg, sound, instance);
        }

        private bool playing_system_sound()
        {
            return (System_SFX_Priority && System_Sound != null && System_Sound.State != SoundState.Stopped) ||
                (New_System_Sound_Data != null && New_System_Sound_Data.Priority);
        }
        private void cancel_system_sound()
        {
            if (System_Sound != null)
            {
                System_Sound.Stop();
                System_Sound.Dispose();
                System_Sound = null;
                System_SFX_Priority = false;

                New_System_Sound_Data = null;
            }
        }

        private void stop_sfx()
        {
            for (int i = 0; i < Playing_Sounds.Length; i++)
            {
                if (Playing_Sounds[i] != null)
                {
                    Playing_Sounds[i].Stop();
                    Playing_Sounds[i].Dispose();
                    Playing_Sounds[i] = null;
                }
            }

            cancel_system_sound();
            if (Music_Effect != null)
            {
                Music_Effect.Stop();
                Music_Effect.Dispose();
                Music_Effect = null;
            }
            ME_Pause = false;

            set_sfx_fade_volume(1);
            cancel_sound_fade_out();
        }

        private void sfx_fade()
        {
            sfx_fade(60);
        }
        private void sfx_fade(int time)
        {
            if (time > 0 && !sound_fading_out)
            {
                //if (!sound_muted)
                    set_sfx_fade_volume(1);
                Sound_Fade_Out_Time = time;
                Sound_Fade_Timer = 0;
            }
            bgs_fade(time);
        }

        private void cancel_sound_fade_out()
        {
            Sound_Fade_Out_Time = 0;
            Sound_Fade_Timer = 0;
        }

        private bool add_new_playing_sound(SoundEffectInstance instance, Maybe<int> channel)
        {
            int actual_channel;

            // If the channel is defined
            if (channel.IsSomething)
            {
                if (channel >= Playing_Sounds.Length || channel < 0)
                    throw new ArgumentException();

                // If a sound is already on this channel
                if (Playing_Sounds[channel] != null)
                {
                    // If the sound on this channel is also a channel sound,
                    if (Playing_Sounds[channel].FixedChannel)
                    {
                        // Dispose the sound so it can be replaced
                        Playing_Sounds[channel].Stop();
                        Playing_Sounds[channel].Dispose();
                        Playing_Sounds[channel] = null;
                    }
                    else
                    {
                        // If there aren't empty channels to move the sound to instead
                        // Remove the oldest sound
                        if (too_many_active_sounds)
                            remove_oldest_playing_sound();
                        // Move all sounds up a channel if this one is still taken
                        if (Playing_Sounds[channel] != null)
                            push_sound_ids(channel);
                    }
                }

                actual_channel = channel;
            }
            // Else play on the first open channel
            else
            {
                actual_channel = -1;
                // Remove the oldest sound if there are too many
                if (too_many_active_sounds)
                    remove_oldest_playing_sound();
                // Find the first empty channel
                for (int i = 0; i < Playing_Sounds.Length; i++)
                    if (Playing_Sounds[i] == null)
                    {
                        actual_channel = i;
                        break;
                    }
            }

            // If we found a channel to play the sound on
            if (actual_channel >= 0)
            {
                instance.Play();
                // If calling play on the sound results in some error, dispose it
                if (instance.State == SoundState.Initial)
                    instance.Dispose();
                else
                {
                    Playing_Sounds[actual_channel] = new ChannelSound(
                        instance, channel.IsSomething);
                    return true;
                }
            }
            return false;
        }

        private bool remove_oldest_playing_sound()
        {
            for (int i = 0; i < Playing_Sounds.Length; i++)
            {
                if (!Playing_Sounds[i].FixedChannel)
                {
                    Playing_Sounds[i].Stop();
                    Playing_Sounds[i].Dispose();
                    pop_sound_ids(i);
                    return true;
                }
            }
            return false;
        }

        private void push_sound_ids(int i)
        {
            if (Playing_Sounds[i].FixedChannel)
                throw new ArgumentException();

            // First determine if there are any null channels between the last unfixed
            // channel and the end of the array
            int null_channel = Playing_Sounds.Length;
            int unfixed_channel = Playing_Sounds.Length;
            for (int j = Playing_Sounds.Length - 1; j >= i; j--)
            {
                if (Playing_Sounds[j] == null)
                {
                    null_channel = j;
                    break;

                }
            }
            for (int j = Playing_Sounds.Length - 1; j >= i; j--)
            {
                if (Playing_Sounds[j] != null && !Playing_Sounds[j].FixedChannel)
                {
                    unfixed_channel = j;
                    break;
                }
            }

            // If there are no available null channels
            if (null_channel == Playing_Sounds.Length || null_channel < unfixed_channel)
            {
                // The first fixed sound encountered will be disposed
                for (; i < Playing_Sounds.Length; i++)
                {
                    if (Playing_Sounds[i] != null && !Playing_Sounds[i].FixedChannel)
                    {
                        Playing_Sounds[i].Stop();
                        Playing_Sounds[i].Dispose();
                        Playing_Sounds[i] = null;
                        return;
                    }
                }
            }
            // Else move all sound ids that don't have their channel defined up
            else
            {
                var sound = Playing_Sounds[i];
                Playing_Sounds[i] = null;
                i++;
                for (; i < Playing_Sounds.Length; i++)
                {
                    if (Playing_Sounds[i] == null)
                    {
                        Playing_Sounds[i] = sound;
                        sound = null;
                        break;
                    }
                    else
                    {
                        if (Playing_Sounds[i].FixedChannel)
                            continue;
                        else
                        {
                            var temp = Playing_Sounds[i];
                            Playing_Sounds[i] = sound;
                            sound = temp;
                        }
                    }
                }
                // Should never hit this, but just in case
                if (sound != null)
                    sound.Dispose();
            }
        }
        private void pop_sound_ids(int i)
        {
            Playing_Sounds[i] = null;
            // Move all sound ids that don't have their channel defined down
            for (int j = i + 1; j < Playing_Sounds.Length; j++)
            {
                if (Playing_Sounds[j] != null && !Playing_Sounds[j].FixedChannel)
                {
                    Playing_Sounds[i] = Playing_Sounds[j];
                    i = j;
                    Playing_Sounds[i] = null;
                }
            }
        }
        #endregion

        #region ME
        private void play_me(string bank, string cue_name)
        {
            SoundEffectGetter sound = get_sound(bank, cue_name);
            if (sound.Sound == null)
            {
                sound.Dispose();
                return;
            }
            if (Music_Effect != null)
            {
                Music_Effect.Stop();
                Music_Effect.Dispose();
            }

            Music_Effect = sound.Instance;
            Music_Effect.Pitch = 0f;
            if (PITCHES.ContainsKey(cue_name))
            {
                Music_Effect.Pitch = PITCHES[cue_name];
            }

            set_bgm_fade_volume(1);
            Music_Effect.Play();
            if (Music != null && Music.State != SoundState.Paused)
            {
                if (Music != null)
                    Music.Pause();
                if (Map_Theme != null)
                    Map_Theme.Pause();
                ME_Pause = true;
            }

            sound.Dispose();
            return;
        }

        private bool stop_me()
        {
            return stop_me(false);
        }
        private bool stop_me(bool bgm_stop)
        {
            bool result = false;
            if (Music != null && ME_Pause)
            {
                if (bgm_stop)
                    stop_bgm();
                else
                {
                    //if (!music_muted)
                        set_bgm_fade_volume(1);
                    Music.Resume();
                    if (Music.State != SoundState.Playing)
                        Music.Play();
                    update_bgm_fade();
                }
            }
            if (Music_Effect != null)
            {
                Music_Effect.Stop();
                Music_Effect.Dispose();
                result = true;
            }
            Music_Effect = null;
            return result;
        }
        #endregion

        private static void sound_effect_from_ogg(out SoundEffect sound, string cue_name, int intro_start = 0, int loop_start = -1, int loop_length = -1)
        {
            sound_effect_from_ogg(out sound, cue_name, out intro_start, out loop_start, out loop_length);
        }
        private static void sound_effect_from_ogg(out SoundEffect sound, string cue_name, out int intro_start, out int loop_start, out int loop_length)
        {
            if (!LOOP_DATA.ContainsKey(cue_name))
            {
                try
                {
                    using (Stream cue_stream = TitleContainer.OpenStream(cue_name))
                    {
#if __ANDROID__
						MemoryStream stream = new MemoryStream();
						cue_stream.CopyTo(stream);
                        using (var vorbis = new NVorbis.VorbisReader(stream, cue_name, false))
#else
                        using (var vorbis = new NVorbis.VorbisReader(cue_stream, cue_name, false))
#endif
                        {
                            // Stores sound effect data, so it doesn't have to be reloaded repeatedly
                            if (!SOUND_DATA.ContainsKey(cue_name))
                                SOUND_DATA[cue_name] = get_ogg_pcm_data(vorbis);

                            get_loop_data(vorbis, out intro_start, out loop_start, out loop_length);

                            LOOP_DATA[cue_name] = new int[5];
                            LOOP_DATA[cue_name][0] = intro_start;
                            LOOP_DATA[cue_name][1] = loop_start;
                            LOOP_DATA[cue_name][2] = loop_length;
                            LOOP_DATA[cue_name][3] = vorbis.Channels;
                            LOOP_DATA[cue_name][4] = vorbis.SampleRate;
                        }
#if __ANDROID__
						stream.Dispose();
#endif
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw;
                }
#if __ANDROID__
                catch (Java.IO.FileNotFoundException e)
                {
                    throw;
                }
#endif
            }

            intro_start = LOOP_DATA[cue_name][0];
            loop_start = LOOP_DATA[cue_name][1];
            loop_length = LOOP_DATA[cue_name][2];
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Writes the decoded ogg data to a memorystream
                WriteWave(writer, LOOP_DATA[cue_name][3], LOOP_DATA[cue_name][4], SOUND_DATA[cue_name]);
                // Resets the stream's position back to 0 after writing
                stream.Position = 0;
#if __ANDROID__
                sound = SoundEffect.FromStream(
                    stream, intro_start, loop_start, loop_start + loop_length);
#else
                sound = SoundEffect.FromStream(stream);
#endif
                sound.Name = cue_name;
            }
        }

        private static byte[] get_ogg_pcm_data(NVorbis.VorbisReader vorbis)
        {
            return vorbis.SelectMany(x => x).ToArray();
        }

        private static void get_loop_data(NVorbis.VorbisReader reader, out int intro_start, out int loop_start, out int loop_length)
        {
            string[] comments = reader.Comments;
            get_loop_data(comments, out intro_start, out loop_start, out loop_length);
        }
        private static void get_loop_data(string[] comments, out int intro_start, out int loop_start, out int loop_length)
        {
            intro_start = 0;
            loop_start = -1;
            loop_length = -1;

            string[] str_ary;
            for (int i = 0; i < comments.Length; i++)
            {
                str_ary = comments[i].Split('\0');
                str_ary = str_ary[0].Split('=');
                switch (str_ary[0])
                {
                    case "INTROSTART":
                        intro_start = Convert.ToInt32(str_ary[1]);
                        break;
                    case "LOOPSTART":
                        loop_start = Convert.ToInt32(str_ary[1]);
                        break;
                    case "LOOPLENGTH":
                        loop_length = Convert.ToInt32(str_ary[1]);
                        break;
                }
            }
            if (loop_start == -1 || loop_length == -1)
            {
                loop_start = -1;
                loop_length = -1;
            }
        }

        private static void WriteWave(BinaryWriter writer, int channels, int rate, byte[] data)
        {
            writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
            writer.Write((int)(36 + data.Length));
            writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

            writer.Write(new char[4] { 'f', 'm', 't', ' ' });
            writer.Write((int)16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write((int)rate);
            writer.Write((int)(rate * ((16 * channels) / 8)));
            writer.Write((short)((16 * channels) / 8));
            writer.Write((short)16);

            writer.Write(new char[4] { 'd', 'a', 't', 'a' });
            writer.Write((int)data.Length);
            writer.Write(data);
        }
    }

    internal struct SoundEffectGetter : IDisposable
    {
        public bool Ogg;
        public SoundEffect Sound;
        public SoundEffectInstance Instance;

        public SoundEffectGetter(bool ogg, SoundEffect sound, SoundEffectInstance instance)
        {
            Ogg = ogg;
            Sound = sound;
            Instance = instance;
        }

        private bool dispose_sound
        {
            get
            {
#if __ANDROID__
                return false;
#endif
                return Ogg;
            }
        }

        public void Dispose()
        {
            if (Sound != null)
                if (dispose_sound)
                    Sound.Dispose();
        }
    }
}