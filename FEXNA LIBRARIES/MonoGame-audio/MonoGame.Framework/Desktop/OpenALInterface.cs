using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if WINDOWS
namespace MonoGame.Framework.Audio
#else
namespace Microsoft.Xna.Framework.Audio
#endif
{
    public class OpenALInterface
    {
        private static OpenALSoundController soundControllerInstance = null;

        public static void create_sound_controller()
        {
            soundControllerInstance = OpenALSoundController.GetInstance;
        }

        public static void update()
        {
            if (soundControllerInstance != null)
                soundControllerInstance.Update();
        }
    }
}
