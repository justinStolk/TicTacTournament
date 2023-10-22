using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Collections;


namespace ChatClientExample
{
    public delegate void NetworkMessageHandler(object handler, NetworkConnection con, DataStreamReader stream);
    public delegate void ServerMessageHandler(object handler, NetworkConnection con, MessageHeader header);
    public delegate void ClientMessageHandler(object handler, MessageHeader header);
    public enum NetworkMessageType
    {
        HANDSHAKE,
        HANDSHAKE_RESPONSE,
        CHAT_MESSAGE,
        CHAT_QUIT,
        CREATE_ELEMENT,
        REMOVE_ELEMENT,
        PLACE_ELEMENT,
        NETWORK_SPAWN,
        END_TURN,
        PING,
        PONG,
        RPC
    }

    public static class NetworkMessageInfo
    {
        public static Dictionary<NetworkMessageType, System.Type> TypeMap = new Dictionary<NetworkMessageType, System.Type> {
            { NetworkMessageType.HANDSHAKE,                 typeof(HandshakeMessage) },
            { NetworkMessageType.HANDSHAKE_RESPONSE,        typeof(HandshakeResponseMessage) },
            { NetworkMessageType.CHAT_MESSAGE,              typeof(ChatMessage) },
        };
    }

    public class Server : MonoBehaviour
    {
        static Dictionary<NetworkMessageType, ServerMessageHandler> networkHeaderHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler>{
            { NetworkMessageType.HANDSHAKE, HandleClientHandshake },
            { NetworkMessageType.CHAT_MESSAGE, HandleClientMessage },
            { NetworkMessageType.END_TURN, HandleTurnEnd },
        };

        public NetworkDriver m_Driver;
        public NetworkPipeline m_Pipeline;
        private NativeList<NetworkConnection> m_Connections;
        private Dictionary<NetworkConnection, string> nameList = new Dictionary<NetworkConnection, string>();
        public ChatCanvas chat;

        private int currentPlayer = 0;

        void Start()
        {
            // Create Driver
            m_Driver = NetworkDriver.Create(new ReliableUtility.Parameters { WindowSize = 32 });
            m_Pipeline = m_Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));

            // Open listener on server port
            NetworkEndPoint endpoint = NetworkEndPoint.AnyIpv4;
            endpoint.Port = 1511;
            if (m_Driver.Bind(endpoint) != 0)
                Debug.Log("Failed to bind to port 1511");
            else
                m_Driver.Listen();
            m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);

            for (int i = 0; i < m_Connections.Length; i++)
            {
                TurnEndMessage message = new();
                message.IsMyTurn = i == currentPlayer;
                m_Driver.BeginSend(m_Connections[i], out var writer);
                message.SerializeObject(ref writer);
                m_Driver.EndSend(writer);
            }
        }
        // Write this immediately after creating the above Start calls, so you don't forget
        //  Or else you well get lingering thread sockets, and will have trouble starting new ones!
        void OnDestroy()
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
        void Update()
        {
            // This is a jobified system, so we need to tell it to handle all its outstanding tasks first
            m_Driver.ScheduleUpdate().Complete();
            // Clean up connections, remove stale ones
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    if (nameList.ContainsKey(m_Connections[i]))
                    {
                        chat.NewMessage($"{ nameList[m_Connections[i]]} has disconnected.", ChatCanvas.leaveColor);
                        nameList.Remove(m_Connections[i]);
                    }
                    m_Connections.RemoveAtSwapBack(i);
                    // This little trick means we can alter the contents of the list without breaking / skipping instances
                    --i;
                }
            }
            // Accept new connections
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default(NetworkConnection))
            {
                m_Connections.Add(c);
                if (m_Connections.Length == 2)
                {
                    uint nextId = NetworkManager.NextNetworkID;
                    // We can start the match
                }
                // Debug.Log("Accepted a connection");
            }
            DataStreamReader stream;
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                    continue;
                // Loop through available events
                NetworkEvent.Type cmd;
                while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        NetworkMessageType msgType = (NetworkMessageType)stream.ReadUShort();
                        MessageHeader header = (MessageHeader)System.Activator.CreateInstance(NetworkMessageInfo.TypeMap[msgType]);
                        Debug.Log(header);

                        header.DeserializeObject(ref stream);

                        // First UInt is always message type (this is our own first design choice)
                        if (networkHeaderHandlers.ContainsKey(msgType))
                        {
                            try
                            {
                                networkHeaderHandlers[msgType].Invoke(this, m_Connections[i], header);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogError(e);
                                Debug.LogError("Badly formatted message received...");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Unsupported message type received: { msgType} ", this);
                        }
                    }
                }
            }
        }
        // Static handler functions
        //  - Client handshake                  (DONE)
        //  - Client chat message               (DONE)
        //  - Client chat exit                  (DONE)
        static void HandleClientHandshake(object handler, NetworkConnection connection, MessageHeader header)
        {
            Server server = handler as Server;
            HandshakeMessage handshake = header as HandshakeMessage;

            HandshakeResponseMessage response = new HandshakeResponseMessage();
            response.Response = $"Welcome, {handshake.ClientName}!";
            server.chat.NewMessage(response.Response, ChatCanvas.joinColor);

            server.m_Driver.BeginSend(connection, out var writer);
            response.SerializeObject(ref writer);
            server.m_Driver.EndSend(writer);

        }
        static void HandleClientMessage(object handler, NetworkConnection connection, MessageHeader header)
        {
            Server server = handler as Server;
            ChatMessage chatMessage = header as ChatMessage;
            server.chat.NewMessage(chatMessage.Message, ChatCanvas.chatColor);

            foreach (NetworkConnection c in server.m_Connections)
            {
                server.m_Driver.BeginSend(c, out var writer);
                chatMessage.SerializeObject(ref writer);
                server.m_Driver.EndSend(writer);
            }
        }

        static void HandleTurnEnd(object handler, NetworkConnection connection, MessageHeader header)
        {
            Server serv = handler as Server;
            TurnEndMessage turnEndMessage = header as TurnEndMessage;
            for (int i = 0; i < serv.m_Connections.Length; i++)
            {
                serv.m_Driver.BeginSend(serv.m_Connections[i], out var writer);
                turnEndMessage.SerializeObject(ref writer);
                serv.m_Driver.EndSend(writer);
            }
        }

    }

}