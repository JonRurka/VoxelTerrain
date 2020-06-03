using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace UnityGameServer.Networking
{
    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadMessage(this Stream stream)
        {
            //Logger.Log("ReadMessage 1");
            ushort bytesRead = 0;
            ushort headerRead = 0;
            byte[] buffer = new byte[2];

            if (stream == null)
                return null;

            while (headerRead < 2 && (bytesRead = (ushort)await stream.ReadAsync(buffer, headerRead, 2 - headerRead).ConfigureAwait(false)) > 0)
            {
                headerRead += bytesRead;
            }
            //Logger.Log("ReadMessage 2");
            if (headerRead < 2)
            {
                return null;
            }
            //Logger.Log("ReadMessage 3");

            ushort bytesRemaining = BitConverter.ToUInt16(buffer, 0);
            byte[] data = new byte[bytesRemaining];

            while (bytesRemaining > 0 && (bytesRead = (ushort)await stream.ReadAsync(data, data.Length - bytesRemaining, bytesRemaining)) != 0)
            {
                bytesRemaining -= bytesRead;
            }
            //Logger.Log("ReadMessage 4");
            if (bytesRemaining != 0)
            {
                return null;
            }
            //Logger.Log("ReadMessage 5");
            return data;
        }

        public static Task SendMessasge(this Stream stream, byte[] data)
        {
            if (stream == null || data == null)
                return null;
            return Task.WhenAll(stream.WriteAsync(BitConverter.GetBytes((ushort)data.Length + 2), 0, 2),
                                stream.WriteAsync(data, 0, data.Length));
        }
    }
}