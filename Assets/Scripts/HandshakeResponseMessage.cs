using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class HandshakeResponseMessage : MessageHeader
    {
        public override NetworkMessageType Type => NetworkMessageType.HANDSHAKE_RESPONSE;
        public string Response;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteFixedString128(new Unity.Collections.FixedString128Bytes(Response));
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            Response = reader.ReadFixedString128().ToString();
        }

    }

}
