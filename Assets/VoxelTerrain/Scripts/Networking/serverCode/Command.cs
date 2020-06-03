using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameServer.Networking
{
    public class Command : Attribute
    {
        public byte command;

        public Command(byte command)
        {
            this.command = command;
        }

        public Command(ServerCodes code)
        {
            this.command = (byte)code;
        }
    }
}