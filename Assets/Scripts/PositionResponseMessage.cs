using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class PositionResponseMessage : MessageHeader
    {
        public override NetworkMessageType Type => NetworkMessageType.RECEIVE_POSITION;
        public uint PositionX;
        public uint PositionY;
        public uint Value;


        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteUInt(PositionX);
            writer.WriteUInt(PositionY);
            writer.WriteUInt(Value);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            PositionX = reader.ReadUInt();
            PositionY = reader.ReadUInt();
            Value = reader.ReadUInt();
        }
    }
}
