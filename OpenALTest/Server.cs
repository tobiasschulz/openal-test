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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

#endregion

namespace OpenALTest
{
	public class Server
	{
		public int Port { get; private set; }

		private Socket listening;

		public Server (int port)
		{
			Port = port;
		}

		public void Start ()
		{
			listening = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
			IPEndPoint endPoint = new IPEndPoint (IPAddress.Any, Port);
			listening.Bind (endPoint);
			listening.Listen (5);

			Console.WriteLine ("Listening on " + endPoint.Address + ":" + endPoint.Port + "...");
		}

		public void Loop ()
		{
			int count = 0;
			while (true) {
				Socket sock = listening.Accept ();
				Console.WriteLine ("Accept () is OK...");
				count++;
				Console.WriteLine ("Connection #" + count + " is established and awaiting data...");
				AudioHandler handler = new AudioHandler (new NetworkStream (sock));
			}
		}
	}
}
