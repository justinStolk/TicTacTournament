using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class HandshakeMessage : MessageHeader
    {
        public override NetworkMessageType Type => NetworkMessageType.HANDSHAKE;
        public string ClientName; 

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteFixedString128(new Unity.Collections.FixedString128Bytes(ClientName));
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            ClientName = reader.ReadFixedString128().ToString();
        }

    }
}
