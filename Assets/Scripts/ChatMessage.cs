using System.Collections;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine;

namespace ChatClientExample
{
    public class ChatMessage : MessageHeader
    {
        public override NetworkMessageType Type => NetworkMessageType.CHAT_MESSAGE;
        public string Message;

        public override void SerializeObject(ref DataStreamWriter writer)
        {
            base.SerializeObject(ref writer);
            writer.WriteFixedString128(Message);
        }

        public override void DeserializeObject(ref DataStreamReader reader)
        {
            base.DeserializeObject(ref reader);
            Message = reader.ReadFixedString128().ToString();
        }

    }

}
