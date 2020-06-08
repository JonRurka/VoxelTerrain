using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGameServer.Networking;

namespace UnityGameServer
{
    public class User : IUser
    {
        public string Name { get; private set; }
        public SocketUser Socket { get; private set; }

        public User(string name)
        {
            Name = name;
        }

        public virtual void SetSocket(SocketUser socket)
        {
            Socket = socket;
        }

        public virtual void Disconnected()
        {
            
        }

        public virtual void RequestedColumnGenerated(Column column, object Meta)
        {
            TransmitColumn(column, (bool)Meta);
        }

        public virtual void TransmitColumn(Column column, bool has_heightmap)
        {
            List<byte> sendThis = new List<byte>();
            sendThis.AddRange(BitConverter.GetBytes(column.Location.x));
            sendThis.AddRange(BitConverter.GetBytes(column.Location.y));
            sendThis.AddRange(BitConverter.GetBytes(column.Location.z));
            sendThis.Add((byte)column.Max_Mode);

            byte[] buff = new byte[column.SurfaceData.Length * 4];
            Buffer.BlockCopy(column.SurfaceData, 0, buff, 0, buff.Length);
            sendThis.AddRange(buff);


            if (column.Max_Mode >= LOD_Mode.ReducedDepth)
            {
                buff = new byte[column.surfaceBlocksCount * 4];
                Buffer.BlockCopy(column.surfaceBlocks, 0, buff, 0, buff.Length);

                sendThis.AddRange(BitConverter.GetBytes(buff.Length));
                sendThis.AddRange(buff);
            }

            Logger.Log("Sending chunk: " + DebugTimer.Elapsed());
            Socket.Send((byte)ClientCodes.ReceiveChunk, sendThis.ToArray());
        }
    }
}
