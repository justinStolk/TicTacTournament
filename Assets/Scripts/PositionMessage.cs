using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{

    public class PositionMessage : MessageHeader
    {
        public override NetworkMessageType Type => NetworkMessageType.CHOOSE_POSITION;
        public uint PositionX;
        public uint PositionY;


        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteUInt(PositionX);
            writer.WriteUInt(PositionY);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            PositionX = reader.ReadUInt();
            PositionY = reader.ReadUInt();
        }
    }
}
