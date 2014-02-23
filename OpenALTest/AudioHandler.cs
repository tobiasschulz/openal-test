#region Copyright

/*
 * Copyright (c) 2013-2014 Tobias Schulz, Maximilian Reuter, Pascal Knodel,
 *                         Gerd Augsburg, Christina Erler, Daniel Warzel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

#endregion

#region Using
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

#endregion

namespace OpenALTest
{
	public class AudioHandler
	{
		public static readonly int Frequency = 44100;
		public static readonly int BufferSize = 4096;
		public static readonly int NumPlayBuffers = 5;
		public static readonly ALFormat Format = ALFormat.Mono16;
		public static readonly int SampleSize = 2;
		public static bool Running;
		private NetworkStream Stream;

		public AudioHandler (NetworkStream stream)
		{
			Stream = stream;
			try {
				string device = ChooseCaptureDevice ();

				Running = true;
				ThreadStart captureLoop = new ThreadStart (() => CaptureLoop (device));
				ThreadStart playLoop = new ThreadStart (PlayLoop);
				Thread captureThread = new Thread (captureLoop);
				Thread playThread = new Thread (playLoop);
				captureThread.Start ();
				playThread.Start ();
			}
			catch (Exception e) {
				Console.WriteLine (e);
				stream.Close ();
			}
		}

		public string ChooseCaptureDevice ()
		{
			IList<string> devices = AudioCapture.AvailableDevices;

			if (devices.Count > 0) {
				string best = AudioCapture.DefaultDevice;

				foreach (string device in devices) {
					if (device.Contains ("Monitor of")) {
						continue;
					}

					if (device.Contains ("Logitech")) {
						best = device;
					}
				}

				Console.WriteLine ("Microphones:");
				foreach (string device in devices) {
					Console.WriteLine ("  " + (device == best ? "[X]" : "[ ]") + " " + device);
				}

				return best;
			}
			else {
				return null;
			}
		}

		public void CaptureLoop (string device)
		{
			using (AudioContext context = new AudioContext ()) {
				Console.WriteLine ("Starting capture loop.");
				AudioCapture capture = new AudioCapture (device, Frequency, Format, BufferSize);
				capture.Start ();
				byte[] buffer = new byte[BufferSize];

				Console.WriteLine ("Started capture loop.");
				while (AudioHandler.Running) {
					int samples = capture.AvailableSamples;
					if (samples > 0) {
						if (samples > BufferSize / SampleSize)
							samples = BufferSize / SampleSize;
						//Console.WriteLine ("samples: " + samples);
						capture.ReadSamples (buffer, samples);
						Stream.Write (buffer, 0, samples * SampleSize);
					}
				}
				Console.WriteLine ("Finished capture loop.");
				AudioHandler.Running = false;
			}
		}

		public void PlayLoop ()
		{
			using (AudioContext context = new AudioContext ()) {
				int source = AL.GenSource ();
				byte[] buffer = new byte[BufferSize];
				int read;

				Console.WriteLine ("Starting play loop.");
				{
					int[] buffers = AL.GenBuffers (NumPlayBuffers);
					for (int i = 0; i < NumPlayBuffers; ++i) {
						read = Stream.Read (buffer, 0, BufferSize);
						AL.BufferData (buffers [i], Format, buffer, read, Frequency);
					}
					AL.SourceQueueBuffers (source, NumPlayBuffers, buffers);
					AL.SourcePlay (source);
				}

				Console.WriteLine ("Started play loop.");
				while (AudioHandler.Running) {
					int processed;
					AL.GetSource (source, ALGetSourcei.BuffersProcessed, out processed);
					if (processed <= 0) {
						continue;
					}

					//Console.WriteLine ("Processed: " + processed);
					while (processed-- > 0) {
						read = Stream.Read (buffer, 0, BufferSize);
						//Console.WriteLine ("read: " + read);
						int[] buf = new int[1];
						AL.SourceUnqueueBuffers (source, 1, buf);
						AL.BufferData (buf [0], Format, buffer, read, Frequency);
						AL.SourceQueueBuffers (source, 1, buf);
					}

					int state;
					AL.GetSource (source, ALGetSourcei.SourceState, out state);
					if ((ALSourceState)state != ALSourceState.Playing) {
						AL.SourcePlay (source);
					}

					// http://kcat.strangesoft.net/openal-tutorial.html
				}
				Console.WriteLine ("Finished play loop.");
				AudioHandler.Running = false;
			}
		}

		/*
			AL.GetError ();
			string deviceName = Alc.GetString (IntPtr.Zero, AlcGetString.CaptureDefaultDeviceSpecifier);
			Console.WriteLine ("device: " + deviceName);
			IntPtr device = Alc.CaptureOpenDevice (deviceName, SRATE, ALFormat.Mono16, SSIZE);
			if (Error (device))
				return;
			Console.WriteLine ("c");
			Alc.CaptureStart (device);
			if (Error (device))
				return;

			while (true) {
			byte[] buffer = new byte[SRATE];
				int sample = 0;
				Alc.GetInteger (device, AlcGetInteger.CaptureSamples, sizeof (int), out sample);
				if (Error (device))
					return;
				Console.WriteLine ("sample: " + sample.ToString ());
				Alc.CaptureSamples (device, buffer, sample);
				if (Error (device))
					return;
				SDL2.SDL.SDL_Delay (100);
			}
			Alc.CaptureStop (device);
			Alc.CaptureCloseDevice (device);
		*/
		/*
		ALCdevice *device = alcCaptureOpenDevice (NULL, SRATE, AL_FORMAT_STEREO16, SSIZE);
		if (alGetError () != AL_NO_ERROR) {
		return 0;
		}
		alcCaptureStart (device);

		while (true) {
		alcGetIntegerv (device, ALC_CAPTURE_SAMPLES, (ALCsizei)sizeof (ALint), &sample);
		alcCaptureSamples (device, (ALCvoid *)buffer, sample);

		// ... do something with the buffer
		}

		alcCaptureStop (device);
		alcCaptureCloseDevice (device);

		return 0;
		*/

		private static bool Error (IntPtr device)
		{
			AlcError err = Alc.GetError (device);
			if (err != AlcError.NoError) {
				Console.WriteLine ("Error: " + err.ToString ());
				return true;
			}
			else {
				return false;
			}
		}
	}
}
