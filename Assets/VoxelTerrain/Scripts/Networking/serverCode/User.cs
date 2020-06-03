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

        }
    }
}
