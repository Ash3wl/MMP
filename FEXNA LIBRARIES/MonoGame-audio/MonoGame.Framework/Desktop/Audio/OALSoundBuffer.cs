// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
#if !MONOMAC
using OpenTK.Audio.OpenAL;
#else
using MonoMac.OpenAL;
#endif
#if DEBUG
using System.Diagnostics;
#endif

#if WINDOWS
namespace MonoGame.Framework.Audio
#else
namespace Microsoft.Xna.Framework.Audio
#endif
{
	internal class OALSoundBuffer : IDisposable
	{
		int openALDataBuffer, loopOpenALDataBuffer;
        List<byte[]> Stream_Data_Arrays, Stream_Loop_Data_Arrays, Streamed_Loop_Data_Arrays;
        List<int> streamBuffers;
		ALFormat openALFormat, loopOpenALFormat;
		int dataSize, loopDataSize;
		int sampleRate, loopSampleRate;
        int Channels;
		private int _sourceId;
        private bool hasIntroLoop = false;
        private NVorbis.VorbisReader Vorbis_Reader;
        private IEnumerator<byte[]> Vorbis_Enumerator;
        private int Blank_Length, Intro_Length, Loop_Length, Streamed_Length;
        public bool Streamed_Loop = false;

		public OALSoundBuffer ()
		{
            initialize(false);
		}
        public OALSoundBuffer(bool intro_loop)
        {
            initialize(intro_loop);
        }

//#if DEBUG //Debug
        internal string Name { get; private set; }
        public override string ToString()
        {
            return "OALSoundBuffer: " + Name;
        }
//#endif

        private void initialize(bool intro_loop)
        {
            ALError alError;

            alError = AL.GetError();
            AL.GenBuffers(1, out openALDataBuffer);
            alError = AL.GetError();
            if (alError != ALError.NoError)
            {
                Console.WriteLine("Failed to generate OpenAL data buffer: ", AL.GetErrorString(alError));
            }
            if (intro_loop)
            {
                AL.GenBuffers(1, out loopOpenALDataBuffer);
                alError = AL.GetError();
                if (alError != ALError.NoError)
                {
                    Console.WriteLine("Failed to generate OpenAL data buffer: ", AL.GetErrorString(alError));
                }
                hasIntroLoop = true;
                Streamed_Loop = true;
            }
        }

		public int OpenALDataBuffer {
			get {
				return openALDataBuffer;
			}
		}
        public int LoopOpenALDataBuffer
        {
            get
            {
                return loopOpenALDataBuffer;
            }
        }

		public double Duration {
			get;
			set;
		}
		public double LoopDuration {
			get;
			set;
		}

        public bool streamed_buffer { get { return streamBuffers != null; } }
        public bool actively_streaming { get { return Vorbis_Reader != null; } }

        public void dispose_played_buffers(int count)
        {
            if (count > streamBuffers.Count)
                count = streamBuffers.Count;
            for (int i = 0; i < count; i++)
            {
                AL.SourceUnqueueBuffer(SourceId);
                streamBuffers.RemoveAt(0);
            }
        }

        List<int> chunk_counts = new List<int>();
        List<int> lengths = new List<int>();
        public void queue_stream_buffers()
        {
            // Get the length of the buffer to add
            int length = 0;
            foreach (byte[] ary in Stream_Data_Arrays)
                length += ary.Length;
            foreach (byte[] ary in Stream_Loop_Data_Arrays)
                length += ary.Length;
            byte[] data = new byte[length];
#if DEBUG
            Debug.Assert(length > 0);
#endif
            // Go through intro data
            int offset = 0;
            foreach (byte[] ary in Stream_Data_Arrays)
            {
                Array.Copy(ary, 0, data, offset, ary.Length);
                offset += ary.Length;
                // If there is no intro loop, all of the data is in this list so copy from here to the list of already streamed data
                if (!hasIntroLoop)
                    Streamed_Loop_Data_Arrays.Add(ary);
            }
            chunk_counts.Add(Stream_Data_Arrays.Count);
            lengths.Add(length);
            Stream_Data_Arrays.Clear();
            // Go through loop data
            foreach (byte[] ary in Stream_Loop_Data_Arrays)
            {
                Array.Copy(ary, 0, data, offset, ary.Length);
                offset += ary.Length;
                // We only get here if there is an intro loop, so this is what should be copied to already streamed data
                Streamed_Loop_Data_Arrays.Add(ary);
            }
            Stream_Loop_Data_Arrays.Clear();

            // Create a buffer for the data
            int stream_buffer;

            ALError alError;

            alError = AL.GetError();
            AL.GenBuffers(1, out stream_buffer);
            alError = AL.GetError();
            if (alError != ALError.NoError)
            {
                Console.WriteLine("Failed to generate OpenAL data buffer: ", AL.GetErrorString(alError));
            }

            AL.BufferData(stream_buffer, openALFormat, data, data.Length, this.sampleRate);
            streamBuffers.Add(stream_buffer);
            AL.SourceQueueBuffer(SourceId, stream_buffer);
            buffers_added++;
            // If we've finished streaming, create the loop stream
            if (!actively_streaming && Streamed_Loop)
            {
                queue_loop_buffer();
            }
        }
        public int buffers_added = 0;

        private void queue_loop_buffer()
        {
            // Get the length of the buffer to add
            int length = 0;
            foreach (byte[] ary in Streamed_Loop_Data_Arrays)
                length += ary.Length;
            byte[] data = new byte[length];
#if DEBUG
            Debug.Assert(length > 0);
#endif
            // Go through data
            int offset = 0;
            foreach (byte[] ary in Streamed_Loop_Data_Arrays)
            {
                Array.Copy(ary, 0, data, offset, ary.Length);
                offset += ary.Length;
            }
            Streamed_Loop_Data_Arrays.Clear();

            AL.BufferData(openALDataBuffer, openALFormat, data, data.Length, this.sampleRate);
            AL.SourceQueueBuffer(SourceId, openALDataBuffer);
        }

        public bool HasIntroLoop { get { return hasIntroLoop; } }

#if DEBUG
        public void BindDataBuffer(string name, byte[] dataBuffer, ALFormat format, int size, int sampleRate)
        {
            Name = name;
#else
        public void BindDataBuffer(byte[] dataBuffer, ALFormat format, int size, int sampleRate)
        {
#endif
            openALFormat = format;
			dataSize = size;
            this.sampleRate = sampleRate;
            AL.BufferData(openALDataBuffer, openALFormat, dataBuffer, dataSize, this.sampleRate);

			int bits;

            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Bits, out bits);
            ALError alError = AL.GetError();
            if (alError != ALError.NoError)
            {
                Console.WriteLine("Failed to get buffer bits: {0}, format={1}, size={2}, sampleRate={3}", AL.GetErrorString(alError), format, size, sampleRate);
                Duration = -1;
            }
            else
            {
                AL.GetBuffer(openALDataBuffer, ALGetBufferi.Channels, out Channels);
#if DEBUG
                Channels = Math.Max(1, Channels);
#endif

                alError = AL.GetError();
                if (alError != ALError.NoError)
                {
                    Console.WriteLine("Failed to get buffer bits: {0}, format={1}, size={2}, sampleRate={3}", AL.GetErrorString(alError), format, size, sampleRate);
                    Duration = -1;
                }
                else
                {
                    Duration = (float)(size / ((bits / 8) * Channels)) / (float)sampleRate;
                }
            }
            //Console.WriteLine("Duration: " + Duration + " / size: " + size + " bits: " + bits + " Channels: " + Channels + " rate: " + sampleRate);
		}
        public void BindDataBuffer(byte[] dataBuffer, ALFormat format, int size, int sampleRate, int intro_start, int loop_start, int loop_end
#if DEBUG
            , string name)
        {
            Name = name;
#else
        ) {
#endif
            openALFormat = format;
            dataSize = size;
            this.sampleRate = sampleRate;

            // Gets duration from the whole sound
            // I have no idea if setting data to the buffer here and then the same one below will cause a memory leak or not
            AL.BufferData(openALDataBuffer, openALFormat, dataBuffer, dataSize, this.sampleRate);

            int bits;

            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Bits, out bits);
            ALError alError = AL.GetError();
            if (alError != ALError.NoError)
            {
                Console.WriteLine("Failed to get buffer bits: {0}, format={1}, size={2}, sampleRate={3}", AL.GetErrorString(alError), format, size, sampleRate);
                Duration = -1;
                return;
            }
            else
            {
                AL.GetBuffer(openALDataBuffer, ALGetBufferi.Channels, out Channels);

                alError = AL.GetError();
                if (alError != ALError.NoError)
                {
                    Console.WriteLine("Failed to get buffer bits: {0}, format={1}, size={2}, sampleRate={3}", AL.GetErrorString(alError), format, size, sampleRate);
                    Duration = -1;
                    return;
                }
                else
                {
                    Duration = (float)(size / ((bits / 8) * Channels)) / (float)sampleRate;
                }
            }

            //int second_length = ((sampleRate * ((bits / 8) * Channels)));

            // Before, this used floats for positions in seconds before; now it uses ints for samples
            int blank_length = (intro_start) * ((bits / 8) * Channels);//((int)((intro_start) * second_length / (2 * Channels))) * (2 * Channels);
            int intro_length = (loop_start - intro_start) * ((bits / 8) * Channels);//((int)((loop_start - intro_start) * second_length / (2 * Channels))) * (2 * Channels);
            int loop_length = (loop_end - loop_start) * ((bits / 8) * Channels);//((int)((loop_end - loop_start) * second_length / (2 * Channels))) * (2 * Channels);

            byte[] intro_data_buffer = new byte[intro_length];
            Array.Copy(dataBuffer, blank_length, intro_data_buffer,
                0, Math.Min(dataBuffer.Length - blank_length, intro_data_buffer.Length));
            byte[] loop_data_buffer = new byte[loop_length];
            Array.Copy(dataBuffer, blank_length + intro_length, loop_data_buffer,
                0, Math.Min(dataBuffer.Length - (blank_length + intro_length), loop_data_buffer.Length));

            AL.BufferData(openALDataBuffer, openALFormat, intro_data_buffer, intro_data_buffer.Length, this.sampleRate);
            alError = AL.GetError();
            AL.BufferData(loopOpenALDataBuffer, openALFormat, loop_data_buffer, loop_data_buffer.Length, this.sampleRate);
            alError = AL.GetError();

            //Console.WriteLine("Duration: " + Duration + " / size: " + size + " bits: " + bits + " Channels: " + Channels + " rate: " + sampleRate);
        }
        public void BindLoopDataBuffer(byte[] dataBuffer, ALFormat format, int size, int sampleRate)
        {
            loopOpenALFormat = format;
            loopDataSize = size;
            this.loopSampleRate = sampleRate;
            AL.BufferData(loopOpenALDataBuffer, loopOpenALFormat, dataBuffer, loopDataSize, this.loopSampleRate);

            int bits;

            AL.GetBuffer(openALDataBuffer, ALGetBufferi.Bits, out bits);
            ALError alError = AL.GetError();
            if (alError != ALError.NoError)
            {
                Console.WriteLine("Failed to get buffer bits: {0}, format={1}, size={2}, sampleRate={3}", AL.GetErrorString(alError), format, size, sampleRate);
                Duration = -1;
            }
            else
            {
                AL.GetBuffer(openALDataBuffer, ALGetBufferi.Channels, out Channels);

                alError = AL.GetError();
                if (alError != ALError.NoError)
                {
                    Console.WriteLine("Failed to get buffer bits: {0}, format={1}, size={2}, sampleRate={3}", AL.GetErrorString(alError), format, size, sampleRate);
                    Duration = -1;
                }
                else
                {
                    Duration = (float)(size / ((bits / 8) * Channels)) / (float)sampleRate;
                }
            }
            //Console.WriteLine("LoopDuration: " + LoopDuration + " / size: " + size + " bits: " + bits + " Channels: " + Channels + " rate: " + sampleRate);
        }


        public void BindDataBuffer(NVorbis.VorbisReader vorbis, int intro_start, int loop_start, int loop_end)
        {
//#if DEBUG //Debug
            Name = vorbis.Name;
//#endif
            Vorbis_Reader = vorbis;
            Vorbis_Enumerator = vorbis.GetEnumerator();
            if (vorbis.Channels > 2)
                throw new ArgumentOutOfRangeException(
                    string.Format("Only mono and stereo sounds are supported; \"{0}\" has {1} sound channels", Name, vorbis.Channels));
            openALFormat = vorbis.Channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
            this.sampleRate = vorbis.SampleRate;

            int bits = 16;
            Channels = vorbis.Channels;
            Blank_Length = (intro_start) * ((bits / 8) * Channels);
            // If the loop start has a value, the audio needs handled so that the into and looping sections are separate
            hasIntroLoop = loop_start != -1;
            if (hasIntroLoop)
            {
                Streamed_Loop = true;
                Intro_Length = (loop_start - intro_start) * ((bits / 8) * Channels);
                Loop_Length = (loop_end - loop_start) * ((bits / 8) * Channels);
            }
            else
            {
                //Intro_Length = ((int)(decoder.Samples * (bits / 8) * Channels)) - Blank_Length; //Debug
                //Loop_Length = 0;

                Intro_Length = 0;
                Loop_Length = ((int)(vorbis.TotalSamples * (bits / 8) * Channels)) - Blank_Length;
            }

            streamBuffers = new List<int>();
            Stream_Data_Arrays = new List<byte[]>();
            Stream_Loop_Data_Arrays = new List<byte[]>();
            Streamed_Loop_Data_Arrays = new List<byte[]>();

            read_ogg_stream();
        }


        public void read_ogg_stream()
        {
            int length = 0;
            bool end_of_stream = false;
            List<byte[]> data = new List<byte[]>();
            // Reads data from the stream
            // If no data has been read yet, read in a minimum starting length (16 seconds?)
            // Otherwise read in 6 frames worth of music each loop (1 frame on android)

            while (!end_of_stream &&
                ((Streamed_Length < Blank_Length && length < Blank_Length) ||
#if ANDROID
                // Android streams at least 1/59th of a second of music each frame, to keep the audio seemless without bogging the CPU
				initial_stream_length_not_read(length) || ((44100 * 2 * Channels) / 59) > length))
#else
                // PC streams at least 1/10th of a second of music each frame
                initial_stream_length_not_read(length) || ((44100 * 2 * Channels) / 10) > length))
#endif
            {
                end_of_stream = !Vorbis_Enumerator.MoveNext();

                data.Add(new byte[Vorbis_Enumerator.Current.Length]);
                Array.Copy(Vorbis_Enumerator.Current, data[data.Count - 1], Vorbis_Enumerator.Current.Length);
                length += Vorbis_Enumerator.Current.Length;
            }


            byte[] intro_data_buffer = null, loop_data_buffer = null;

            int index = 0;
            int copy_index = 0, copy_length = 0;
            // If intro isn't done yet
            if (Streamed_Length < (Blank_Length + Intro_Length))
            {
                // If still needing to skip over initial blank section
                if (Streamed_Length < Blank_Length)
                {
                    // Create buffer array and copy data to it
                    intro_data_buffer = new byte[Math.Min(length - Blank_Length, Intro_Length - (Streamed_Length - Blank_Length))];
                    while (index < length && Streamed_Length < Blank_Length + Intro_Length)
                    {
                        // If we're still in the blank section before the intro, jump ahead
                        if (Streamed_Length + data[0].Length < Blank_Length)
                        {
                            length -= data[0].Length;
                            Streamed_Length += data[0].Length;
                            data.RemoveAt(0);
                        }
                        else
                        {
                            if (Streamed_Length < Blank_Length)
                                copy_index = Blank_Length - Streamed_Length;
                            copy_length = Math.Min(intro_data_buffer.Length - index, data[0].Length - copy_index);
                            Array.Copy(data[0], copy_index, intro_data_buffer, index, copy_length);
                            if (Streamed_Length < Blank_Length)
                            {
                                length -= Blank_Length - Streamed_Length;
                                Streamed_Length = Blank_Length + copy_length;
                            }
                            else
                                Streamed_Length += copy_length;
                            if (copy_index + copy_length >= data[0].Length)
                            {
                                data.RemoveAt(0);
                                copy_index = 0;
                            }
                            else
                                copy_index += copy_length;
                            index += copy_length;
                        }
                    }
                }
                // Else just copying to the intro normally
                else
                {
                    // Create buffer array and copy data to it
                    intro_data_buffer = new byte[Math.Min(length, Intro_Length - (Streamed_Length - Blank_Length))];
                    while (index < length && Streamed_Length < Blank_Length + Intro_Length)
                    {
                        copy_length = Math.Min(intro_data_buffer.Length - index, data[0].Length - copy_index);
                        Array.Copy(data[0], copy_index, intro_data_buffer, index, copy_length);
                        Streamed_Length += copy_length;
                        if (copy_index + copy_length >= data[0].Length)
                        {
                            data.RemoveAt(0);
                            copy_index = 0;
                        }
                        else
                            copy_index += copy_length;
                        index += copy_length;
                    }
                }
            }
            // Copy whatever data is remaining into the loop buffer
            if (index < length && Streamed_Length < (Blank_Length + Intro_Length + Loop_Length))
            {
                length -= index;
                index = 0;
                // Create buffer array and copy data to it
                loop_data_buffer = new byte[Math.Min(length - index, Loop_Length - (Streamed_Length - (Blank_Length + Intro_Length)))];
                while (index < length && Streamed_Length < (Blank_Length + Intro_Length + Loop_Length))
                {
                    copy_length = Math.Min(loop_data_buffer.Length - index, data[0].Length - copy_index);
                    Array.Copy(data[0], copy_index, loop_data_buffer, index, copy_length);
                    Streamed_Length += copy_length;
                    data.RemoveAt(0);
                    copy_index = 0;
                    index += copy_length;
                }
            }

            if (intro_data_buffer != null)
                Stream_Data_Arrays.Add(intro_data_buffer);
            if (loop_data_buffer != null)
                Stream_Loop_Data_Arrays.Add(loop_data_buffer);

            // If the stream is finished, clean it up
            if (end_of_stream || Streamed_Length >= (Blank_Length + Intro_Length + Loop_Length))
            {
                if (Vorbis_Reader != null)
                {
                    Vorbis_Reader.Dispose();
                    Vorbis_Reader = null;
                    Vorbis_Enumerator = null;
                }
            }
        }

        private bool initial_stream_length_not_read(int length)
        {
#if ANDROID
            // Reads 1 seconds of music up front, to prime the stream
            return Streamed_Length == 0 && (length - Blank_Length) < (44100 * 2 * Channels) * 1;
#else
            // Reads half a second of music up front, to prime the stream
            return Streamed_Length == 0 && (length - Blank_Length) < (44100 * 2 * Channels) / 2;
#endif
        }

		public void Dispose ()
		{
            CleanUpBuffer();
            if (Vorbis_Reader != null)
            {
                Vorbis_Reader.Dispose();
                Vorbis_Reader = null;
                Vorbis_Enumerator = null;
            }
		}

		public void CleanUpBuffer ()
		{
			if (AL.IsBuffer (openALDataBuffer)) {
				AL.DeleteBuffers (1, ref openALDataBuffer);
                openALDataBuffer = -1;
			}
			if (AL.IsBuffer (loopOpenALDataBuffer)) {
                AL.DeleteBuffers(1, ref loopOpenALDataBuffer);
                loopOpenALDataBuffer = -1;
			}
            int buffer;
            if (streamBuffers != null)
            {
                foreach (int stream_buffer in streamBuffers)
                {
                    buffer = stream_buffer;
                    AL.DeleteBuffers(1, ref buffer);
                }
                streamBuffers.Clear();
                streamBuffers = null;
            }
		}

		public int SourceId
		{
			get {
				return _sourceId;
			}

			set {
				_sourceId = value;
				if (Reserved != null)
					Reserved(this, EventArgs.Empty);

			}
		}

		public void RecycleSoundBuffer()
		{
			if (Recycled != null)
				Recycled(this, EventArgs.Empty);
		}

		#region Events
		public event EventHandler<EventArgs> Reserved;
		public event EventHandler<EventArgs> Recycled;
		#endregion
	}
}

