using ChatClientExample;
using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class TurnSwitchMessage : MessageHeader
{
    public bool IsMyTurn;

    public override NetworkMessageType Type => NetworkMessageType.SWITCH_TURN;

    public override void SerializeObject(ref DataStreamWriter writer)
    {
        base.SerializeObject(ref writer);
        if (IsMyTurn)
        {
            writer.WriteUInt(1);
            return;
        }
        writer.WriteUInt(0);
    }
    public override void DeserializeObject(ref DataStreamReader reader)
    {
        base.DeserializeObject(ref reader);
        IsMyTurn = reader.ReadUInt() == 1;
    }

}
