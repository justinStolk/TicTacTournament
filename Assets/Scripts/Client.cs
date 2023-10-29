using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.Events;

namespace ChatClientExample
{
    public class Client : MonoBehaviour
    {
        public ChatCanvas chat;
        public string ClientName = "Justin";
        public LocalGameManager GameManager;

        private NetworkDriver networkDriver;
        private NetworkConnection connection;
        private bool done;

        static Dictionary<NetworkMessageType, ClientMessageHandler> clientHeaderHandlers = new()
        {
            { NetworkMessageType.HANDSHAKE_RESPONSE, HandleHandshakeResponse },
            { NetworkMessageType.CHAT_MESSAGE, HandleChatMessage },
            { NetworkMessageType.SWITCH_TURN, HandleTurnSwitchMessage },
            { NetworkMessageType.RECEIVE_POSITION, HandlePositionResponseMessage }
        };
        // Start is called before the first frame update
        void Start()
        {
            networkDriver = NetworkDriver.Create();
            connection = default(NetworkConnection);
            var endpoint = NetworkEndPoint.LoopbackIpv4;
            endpoint.Port = 1511;
            connection = networkDriver.Connect(endpoint);
        }

        // Update is called once per frame
        void Update()
        {
            networkDriver.ScheduleUpdate().Complete();
            if (!connection.IsCreated)
            {
                if (!done)
                {
                    Debug.Log("Something went wrong while trying to connect!");
                    return;
                }
            }

            DataStreamReader stream;
            NetworkEvent.Type cmd;

            while ((cmd = connection.PopEvent(networkDriver, out stream)) != NetworkEvent.Type.Empty)
            {

                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("We are now connected to the server");

                    networkDriver.BeginSend(connection, out var writer);

                    HandshakeMessage message = new HandshakeMessage();
                    message.ClientName = ClientName;
                    message.SerializeObject(ref writer);

                    networkDriver.EndSend(writer);
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    //uint value = stream.ReadUInt();
                    //Debug.Log("Got the value = " + value + " back from the server");
                    //Done = true;
                    //connection.Disconnect(networkDriver);
                    //connection = default(NetworkConnection);

                    NetworkMessageType msgType = (NetworkMessageType)stream.ReadUShort();
                    MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
                    header.DeserializeObject(ref stream);

                    if (clientHeaderHandlers.ContainsKey(msgType))
                    {
                        try
                        {
                            clientHeaderHandlers[msgType].Invoke(this, header);
                        }
                        catch
                        {
                            Debug.LogError("Badly formatted message received...");
                        }
                    }

                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    connection = default(NetworkConnection);
                }
            }


        }

        public void SendNetworkMessage(MessageHeader header)
        {
            networkDriver.BeginSend(connection, out var writer);
            header.SerializeObject(ref writer);
            networkDriver.EndSend(writer);
        }

        public void SendIntendedPosition(uint x, uint y)
        {
            PositionMessage message = new PositionMessage();
            message.PositionX = x;
            message.PositionY = y;
            SendNetworkMessage(message);
        }

        public void SendMessageOnServer(string message)
        {
            ChatMessage chatMessage = new ChatMessage();
            chatMessage.Message = message;
            SendNetworkMessage(chatMessage);
        }
        static void HandleHandshakeResponse(object handler, MessageHeader header)
        {
            Client client = handler as Client;
            HandshakeResponseMessage response = header as HandshakeResponseMessage;

            client.chat.NewMessage(response.Response, ChatCanvas.joinColor);

        }
        static void HandleChatMessage(object handler, MessageHeader header)
        {
            Client client = handler as Client;
            ChatMessage chatMessage = header as ChatMessage;

            client.chat.NewMessage(chatMessage.Message, ChatCanvas.chatColor);
        }
        static void HandlePositionResponseMessage(object handler, MessageHeader header)
        {
            Client client = handler as Client;
            PositionResponseMessage message = header as PositionResponseMessage;

            client.GameManager.ReceiveInput(message.PositionX, message.PositionY, message.Value);
        }
        
        static void HandleTurnSwitchMessage(object handler, MessageHeader header)
        {
            Client client = handler as Client;
            TurnSwitchMessage switchMessage = header as TurnSwitchMessage;

            Debug.Log(switchMessage.IsMyTurn);
            string message = switchMessage.IsMyTurn ? "It is now your turn" : "It is your opponents turn";
            client.chat.NewMessage(message, ChatCanvas.joinColor);
        }

        private void OnDestroy()
        {
            networkDriver.Dispose();
        }
    }
}
