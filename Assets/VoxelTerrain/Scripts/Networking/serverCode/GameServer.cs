using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnityGameServer
{
    public class GameServer<T> : ServerBase where T : GameServer<T>
    {
        public static T Instance { get; private set; }

        public GameServer(string[] args) : base(args)
        {
            Instance = (T)this;
        }
    }
}
