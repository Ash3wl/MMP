// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
#if MONOMAC
using MonoMac.OpenAL;
#else
using OpenTK;
using OpenTK.Audio.OpenAL;
#endif

#if WINDOWS
namespace MonoGame.Framework.Audio
#else
namespace Microsoft.Xna.Framework.Audio
#endif
{
#if WINDOWS
    internal sealed class OpenALSoundController : IDisposable
#else
    internal sealed class OpenALSoundController : IDisposable
#endif
    {
        private static OpenALSoundController _instance = null;
        private IntPtr _device;
        private ContextHandle _context;
		//int outputSource;
		//int[] buffers;
        private AlcError _lastOpenALError;
        private int[] allSourcesArray;
        private const int MAX_NUMBER_OF_SOURCES = 32;
        private const double PREFERRED_MIX_RATE = 44100;
        private List<int> availableSourcesCollection;
        private List<OALSoundBuffer> inUseSourcesCollection;
        private List<OALSoundBuffer> playingSourcesCollection;
        private List<OALSoundBuffer> purgeMe;
        private bool _bSoundAvailable = false;
        private Exception _SoundInitException; // Here to bubble back up to the developer

        /// <summary>
        /// Sets up the hardware resources used by the controller.
        /// </summary>
		private OpenALSoundController ()
        {
            if (!OpenSoundController())
            {
                return;
            }
            // We have hardware here and it is ready
            _bSoundAvailable = true;


			allSourcesArray = new int[MAX_NUMBER_OF_SOURCES];
			AL.GenSources (allSourcesArray);

			availableSourcesCollection = new List<int> ();
			inUseSourcesCollection = new List<OALSoundBuffer> ();
			playingSourcesCollection = new List<OALSoundBuffer> ();
            purgeMe = new List<OALSoundBuffer> ();


			for (int x=0; x < MAX_NUMBER_OF_SOURCES; x++) {
				availableSourcesCollection.Add (allSourcesArray [x]);
			}
		}

        /// <summary>
        /// Open the sound device, sets up an audio context, and makes the new context
        /// the current context. Note that this method will stop the playback of
        /// music that was running prior to the game start. If any error occurs, then
        /// the state of the controller is reset.
        /// </summary>
        /// <returns>True if the sound controller was setup, and false if not.</returns>
        private bool OpenSoundController()
        {
#if MONOMAC || IOS
			alcMacOSXMixerOutputRate(PREFERRED_MIX_RATE);
#endif
            try
            {
                _device = Alc.OpenDevice(string.Empty);
            }
            catch (Exception ex)
            {
                _SoundInitException = ex;
                return (false);
            }
            if (CheckALError("Could not open AL device"))
            {
                return(false);
            }
            if (_device != IntPtr.Zero)
            {
                int[] attribute = new int[0];
                _context = Alc.CreateContext(_device, attribute);
                if (CheckALError("Could not open AL context"))
                {
                    CleanUpOpenAL();
                    return(false);
                }

                if (_context != ContextHandle.Zero)
                {
                    Alc.MakeContextCurrent(_context);
                    if (CheckALError("Could not make AL context current"))
                    {
                        CleanUpOpenAL();
                        return(false);
                    }
                    return (true);
                }
            }
            return (false);
        }

		public static OpenALSoundController GetInstance {
			get {
				if (_instance == null)
					_instance = new OpenALSoundController ();
				return _instance;
			}

		}

        /// <summary>
        /// Checks the error state of the OpenAL driver. If a value that is not AlcError.NoError
        /// is returned, then the operation message and the error code is output to the console.
        /// </summary>
        /// <param name="operation">the operation message</param>
        /// <returns>true if an error occurs, and false if not.</returns>
		public bool CheckALError (string operation)
		{
			_lastOpenALError = Alc.GetError (_device);

			if (_lastOpenALError == AlcError.NoError) {
				return(false);
			}

			string errorFmt = "OpenAL Error: {0}";
			Console.WriteLine (String.Format ("{0} - {1}",
							operation,
							//string.Format (errorFmt, Alc.GetString (_device, _lastOpenALError))));
							string.Format (errorFmt, _lastOpenALError)));
            return (true);
		}

        /// <summary>
        /// Destroys the AL context and closes the device, when they exist.
        /// </summary>
		private void CleanUpOpenAL ()
		{
			Alc.MakeContextCurrent (ContextHandle.Zero);
			if (_context != ContextHandle.Zero) {
				Alc.DestroyContext (_context);
				_context = ContextHandle.Zero;
			}
			if (_device != IntPtr.Zero) {
				Alc.CloseDevice (_device);
				_device = IntPtr.Zero;
            }
            _bSoundAvailable = false;
		}

		public void Dispose ()
        {
            if (_bSoundAvailable)
                CleanUpOpenAL();
		}

        /// <summary>
        /// Reserves the given sound buffer. If there are no available sources then false is
        /// returned, otherwise true will be returned and the sound buffer can be played. If
        /// the controller was not able to setup the hardware, then false will be returned.
        /// </summary>
        /// <param name="soundBuffer">The sound buffer you want to play</param>
        /// <returns>True if the buffer can be played, and false if not.</returns>
		public bool ReserveSource (OALSoundBuffer soundBuffer)
		{
            if (!CheckInitState())
            {
                return(false);
            }
			int sourceNumber;
			if (availableSourcesCollection.Count == 0) {

				soundBuffer.SourceId = 0;
				return false;
			}
			

			sourceNumber = availableSourcesCollection.First ();
            soundBuffer.SourceId = sourceNumber;

            //Debug
#if DEBUG
            int processed;
            int queued;

            AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersProcessed, out processed);
            AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersQueued, out queued);

            //81088928
            //81113264

            if (processed > 0 || queued > 0)
            {

            }
            if ((processed > 0 || queued > 0) && false)
            {
                AL.Source(soundBuffer.SourceId, ALSourcei.Buffer, 0);

                AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersProcessed, out processed);
                AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersQueued, out queued);

                if (processed > 0 || queued > 0)
                {
                    AL.Source(soundBuffer.SourceId, ALSourcei.Buffer, 0);
                }
            }

#endif
			inUseSourcesCollection.Add (soundBuffer);

			availableSourcesCollection.Remove (sourceNumber);

			//sourceId = sourceNumber;
			return true;
		}

		public void RecycleSource (OALSoundBuffer soundBuffer)
		{
            if (!CheckInitState())
            {
                return;
            }

            //Debug
#if DEBUG
            int processed;
            int queued;

            AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersProcessed, out processed);
            AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersQueued, out queued);

            if (soundBuffer.SourceId == 81088928 || soundBuffer.SourceId == 81113264)
            {

            }
            if (processed > 0 || queued > 0)
            {
            }

#endif

			inUseSourcesCollection.Remove(soundBuffer);
            foreach(OALSoundBuffer buffer in playingSourcesCollection)
                if (buffer.SourceId == soundBuffer.SourceId)
                {

                }
			availableSourcesCollection.Add(soundBuffer.SourceId);
			soundBuffer.RecycleSoundBuffer();

            //Debug
            if (inUseSourcesCollection.Count + availableSourcesCollection.Count > adsf)
            {
                adsf = inUseSourcesCollection.Count + availableSourcesCollection.Count;
            }
		}
        int adsf = MAX_NUMBER_OF_SOURCES;

		public void PlaySound (OALSoundBuffer soundBuffer)
        {
            if (!CheckInitState())
            {
                return;
            }
            lock (playingSourcesCollection)
            {
#if DEBUG
                if (playingSourcesCollection.Contains(soundBuffer))
                    throw new ArgumentException("Trying to start playing a sound buffer that's already in the playing list (did you mean to resume?)");
#endif
                playingSourcesCollection.Add(soundBuffer);
            }
			AL.SourcePlay (soundBuffer.SourceId);
		}

		public void StopSound (OALSoundBuffer soundBuffer)
        {
            if (!CheckInitState())
            {
                return;
            }
			AL.SourceStop (soundBuffer.SourceId);

			AL.Source(soundBuffer.SourceId, ALSourcei.Buffer, 0);
            lock (playingSourcesCollection)
            {
                playingSourcesCollection.Remove(soundBuffer);
            }
			RecycleSource (soundBuffer);
		}

		public void PauseSound (OALSoundBuffer soundBuffer)
        {
            if (!CheckInitState())
            {
                return;
            }
			AL.SourcePause(soundBuffer.SourceId);
		}

        public void ResumeSound(OALSoundBuffer soundBuffer)
        {
            if (!CheckInitState())
            {
                return;
            }

            lock (playingSourcesCollection)
                if (!playingSourcesCollection.Contains(soundBuffer))
                {
#if DEBUG
                    throw new ArgumentException("Trying to resume a sound buffer that's not even playing");
#endif
                    return;
                }

            AL.SourcePlay(soundBuffer.SourceId);
        }

		public bool IsState (OALSoundBuffer soundBuffer, int state)
		{
            if (!CheckInitState())
            {
                return (false);
            }
			int sourceState;

			AL.GetSource (soundBuffer.SourceId, ALGetSourcei.SourceState, out sourceState);

			if (state == sourceState) {
				return true;
			}

			return false;
		}

        /// <summary>
        /// Checks if the AL controller was initialized properly. If there was an
        /// exception thrown during the OpenAL init, then that exception is thrown
        /// inside of NoAudioHardwareException.
        /// </summary>
        /// <returns>True if the controller was initialized, false if not.</returns>
        private bool CheckInitState()
        {
            if (!_bSoundAvailable)
            {
                if (_SoundInitException != null)
                {
                    Exception e = _SoundInitException;
                    _SoundInitException = null;
                    throw (new NoAudioHardwareException("No audio hardware available.", e));
                }
                return (false);
            }
            return (true);
        }

		public double SourceCurrentPosition (int sourceId)
		{
            if (!CheckInitState())
            {
                return(0.0);
            }
			int pos;
			AL.GetSource (sourceId, ALGetSourcei.SampleOffset, out pos);
			return pos;
		}

        /// <summary>
        /// Called repeatedly, this method cleans up the state of the management lists. This method
        /// will also lock on the playingSourcesCollection. Sources that are stopped will be recycled
        /// using the RecycleSource method.
		/// </summary>
#if ANDROID
		byte stream_update_frame = 0;
		List<byte> STREAM_UPDATE_FRAMES = new List<byte> { 0, 3 };
		const byte STREAM_UPDATE_TIME = 5;
#endif
		public void Update ()
        {
            if (!_bSoundAvailable)
            {
                //OK to ignore this here because the game can run without sound.
                return;
            }
            purgeMe.Clear();

			ALSourceState state;
            lock (playingSourcesCollection) {

				for (int i=playingSourcesCollection.Count-1; i >= 0; i--)
				//foreach (var soundBuffer in playingSourcesCollection) {
				{
					var soundBuffer = playingSourcesCollection [i];

					int processed;
					int queued;
					
					AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersProcessed, out processed);
					AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersQueued, out queued);

					state = AL.GetSourceState(soundBuffer.SourceId);

					// If the stream is still active, read some more data
					if (soundBuffer.actively_streaming)
					{
#if ANDROID
						if (STREAM_UPDATE_FRAMES.Contains(stream_update_frame))
						{
#endif
							state = AL.GetSourceState(soundBuffer.SourceId);
							soundBuffer.dispose_played_buffers(processed);
							soundBuffer.read_ogg_stream();
							// Then after reading data, if either there are two or less buffers remaining to play or the stream is done, queue more buffers
							if (queued - processed <= 2 || !soundBuffer.actively_streaming)
							{
								soundBuffer.queue_stream_buffers();
								if (state == ALSourceState.Stopped)
                                    ResumeSound(soundBuffer);
								AL.GetSource(soundBuffer.SourceId, ALGetSourcei.BuffersQueued, out queued);
							}
#if ANDROID
						}
#endif
                    }
                    //else if ((soundBuffer.streamed_buffer && soundBuffer.Streamed_Loop) && queued > 1) //Debug
					else if ((soundBuffer.streamed_buffer && soundBuffer.Streamed_Loop) && queued >= 1)
					{
						if (queued - processed <= 1)
						{
							if (soundBuffer.streamed_buffer)
								soundBuffer.dispose_played_buffers(processed);
							else
								AL.SourceUnqueueBuffer(soundBuffer.SourceId);
							AL.Source(soundBuffer.SourceId, ALSourceb.Looping, true);
							state = AL.GetSourceState(soundBuffer.SourceId);
							if (state == ALSourceState.Stopped)
                                ResumeSound(soundBuffer);
						}
						else if (soundBuffer.streamed_buffer && processed > 0)
						{
							soundBuffer.dispose_played_buffers(processed);
						}
					}
					else
					{
						if (state == ALSourceState.Stopped || (state == ALSourceState.Playing && processed == 0 && queued == 0))
						{
							//AL.Source(soundBuffer.SourceId, ALSourcei.Buffer, 0);
							purgeMe.Add(soundBuffer);
							//playingSourcesCollection.RemoveAt (i);
							//Console.WriteLine ("to be recycled: " + soundBuffer.SourceId);
						}
					}

				}

				/*for (int i=playingSourcesCollection.Count-1; i >= 0; i--) {
                    var soundBuffer = playingSourcesCollection [i];
                    state = AL.GetSourceState (soundBuffer.SourceId);
                    if (state == ALSourceState.Stopped) {
                        purgeMe.Add (soundBuffer);
                        playingSourcesCollection.RemoveAt (i);
                    }
				}*/
            }
            for (int i = 0; i < purgeMe.Count; i++)
                for (int j = i + 1; j < purgeMe.Count; j++)
                    if (purgeMe[i] == purgeMe[j])
                    {

                    }
            foreach (var soundBuffer in purgeMe)
            {
                StopSound(soundBuffer);

                //AL.Source(soundBuffer.SourceId, ALSourcei.Buffer, 0); //Debug

                //playingSourcesCollection.Remove(soundBuffer); //Debug
                //RecycleSource(soundBuffer);
            }
			
#if ANDROID
			stream_update_frame = (byte)((stream_update_frame + 1) % STREAM_UPDATE_TIME);
#endif
		}

#if MONOMAC || IOS
		public const string OpenALLibrary = "/System/Library/Frameworks/OpenAL.framework/OpenAL";

		[DllImport(OpenALLibrary, EntryPoint = "alcMacOSXMixerOutputRate")]
		static extern void alcMacOSXMixerOutputRate (double rate); // caution
#endif

	}
}

