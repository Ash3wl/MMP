using System;
#if MONOGAME
using Microsoft.Xna.Framework.Audio;
#else
using MonoGame.Framework.Audio;
#endif

namespace FEXNA.Services.Audio
{
    class ChannelSound : IDisposable
    {
        internal SoundEffectInstance Instance { get; private set; }
        internal bool FixedChannel { get; private set; }

        internal SoundState State { get { return Instance.State; } }
        internal bool IsLooped { get { return Instance.IsLooped; } }

        internal ChannelSound(SoundEffectInstance sound, bool fixedChannel)
        {
            Instance = sound;
            FixedChannel = fixedChannel;
        }

        public override string ToString()
        {
            return string.Format("Channel Sound: {0}, {1}", Instance,
                FixedChannel ? "Fixed" : "Unfixed");
        }

        internal void Stop()
        {
            Instance.Stop();
        }

        public void Dispose()
        {
            Instance.Dispose();
        }
    }
}
