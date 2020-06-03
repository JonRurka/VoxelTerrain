using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//namespace UnityGameServer.Networking
//{
public class Data
{
    public Protocal Type;
    public byte command;
    public byte[] Buffer
    {
        get
        {
            return buffer;
        }

        set
        {
            buffer = value;
            Input = Encoding.UTF8.GetString(buffer);
        }
    }

    public string Input;

    private byte[] buffer;

    public Data(Protocal type, byte cmd, byte[] data)
    {
        Type = type;
        command = cmd;
        buffer = data;
        Input = Encoding.UTF8.GetString(Buffer);
    }
}
//}