using UnityEngine;
using System;
using System.Collections;

public class ClientCommand : Attribute
{
    public byte byteCommand;
    public ClientCodes opCode;

    public ClientCommand(byte cmd)
    {
        byteCommand = cmd;
        opCode = (ClientCodes)cmd;
    }

    public ClientCommand(ClientCodes cmd)
    {
        byteCommand = (byte)cmd;
        opCode = cmd;
    }
}
