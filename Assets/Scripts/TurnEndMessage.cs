using ChatClientExample;
using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

public class TurnEndMessage : MessageHeader
{
    public bool IsMyTurn;

    public override NetworkMessageType Type => NetworkMessageType.END_TURN;

    public override void SerializeObject(ref DataStreamWriter writer)
    {
        base.SerializeObject(ref writer);
    }
    public override void DeserializeObject(ref DataStreamReader reader)
    {
        base.DeserializeObject(ref reader); 
    }

}
