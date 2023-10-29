using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Utilities;
using Unity.Collections;
using UnityEngine.UI;

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
        CHOOSE_POSITION,
        RECEIVE_POSITION,
        SWITCH_TURN,
        PING,
        PONG,
        RPC
    }

    public enum MatchResult
    {
        WIN,
        DRAW,
        Undefined
    }

    public static class NetworkMessageInfo
    {
        public static Dictionary<NetworkMessageType, System.Type> TypeMap = new Dictionary<NetworkMessageType, System.Type> {
            { NetworkMessageType.HANDSHAKE,                 typeof(HandshakeMessage) },
            { NetworkMessageType.HANDSHAKE_RESPONSE,        typeof(HandshakeResponseMessage) },
            { NetworkMessageType.SWITCH_TURN,               typeof(TurnSwitchMessage) },
            { NetworkMessageType.CHAT_MESSAGE,              typeof(ChatMessage) },
            { NetworkMessageType.CHOOSE_POSITION,           typeof(PositionMessage) },
            { NetworkMessageType.RECEIVE_POSITION,          typeof(PositionResponseMessage) }
        };
    }

    public class Server : MonoBehaviour
    {
        static Dictionary<NetworkMessageType, ServerMessageHandler> networkHeaderHandlers = new Dictionary<NetworkMessageType, ServerMessageHandler>{
            { NetworkMessageType.HANDSHAKE, HandleClientHandshake },
            { NetworkMessageType.CHAT_MESSAGE, HandleClientMessage },
            //{ NetworkMessageType.SWITCH_TURN, HandleTurnSwitch },
            { NetworkMessageType.CHOOSE_POSITION, HandlePositionMessage }
        };

        public NetworkDriver m_Driver;
        public NetworkPipeline m_Pipeline;
        private NativeList<NetworkConnection> m_Connections;
        private Dictionary<NetworkConnection, string> nameList = new Dictionary<NetworkConnection, string>();
        public ChatCanvas chat;
        public Text serverBuildDebugText;

        private int currentPlayer = 0;
        private uint[,] field = new uint[3, 3];

        private void Awake()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int z = 0; z < 3; z++)
                {
                    field[i, z] = 0;
                }
            }
        }
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
                    for (int i = 0; i < m_Connections.Length; i++)
                    {
                        TurnSwitchMessage message = new();
                        message.IsMyTurn = i == currentPlayer;

                        m_Driver.BeginSend(m_Connections[i], out var writer);
                        message.SerializeObject(ref writer);
                        m_Driver.EndSend(writer);
                    }
                    //uint nextId = NetworkManager.NextNetworkID;
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

        static void HandlePositionMessage(object handler, NetworkConnection connection, MessageHeader header)
        {
            Server serv = handler as Server;
            PositionMessage positionMessage = header as PositionMessage;
            if(connection != serv.m_Connections[serv.currentPlayer])
            {             
                ChatMessage failMessage = new ChatMessage();
                failMessage.Message = "It isn't your turn yet!";
                serv.m_Driver.BeginSend(connection, out var writer);
                failMessage.SerializeObject(ref writer);
                serv.m_Driver.EndSend(writer);
                return;
            }
            uint x = positionMessage.PositionX;
            uint y = positionMessage.PositionY;

            if(serv.field[x,y] != 0)
            {
                serv.chat.NewMessage("Attempting to play on a field that already has been played on!", Color.red);

                ChatMessage failMessage = new ChatMessage();
                failMessage.Message = "Invalid field selected! Please try a free one!";
                serv.m_Driver.BeginSend(connection, out var writer);
                failMessage.SerializeObject(ref writer);
                serv.m_Driver.EndSend(writer);
                return;
            }
            //serv.serverBuildDebugText.text = $"Received positions [{x},{y}] for player {serv.currentPlayer + 1}";

            serv.field[x, y] = (uint)serv.currentPlayer + 1;

            PositionResponseMessage response = new PositionResponseMessage();

            response.PositionX = positionMessage.PositionX;
            response.PositionY = positionMessage.PositionY;
            response.Value = (uint)serv.currentPlayer + 1;

            foreach(NetworkConnection con in serv.m_Connections)
            {
                serv.m_Driver.BeginSend(con, out var writer);
                response.SerializeObject(ref writer);
                serv.m_Driver.EndSend(writer);
            }

            uint result = serv.EvaluateWinStatus();

            if (result == 5)
            {
                serv.serverBuildDebugText.text = "Conclusion reached! Player " + serv.currentPlayer+1 + " won!";

                ChatMessage winMessage = new ChatMessage();
                winMessage.Message = $"Player {serv.currentPlayer + 1} has won!";
                foreach (NetworkConnection c in serv.m_Connections)
                {
                    serv.m_Driver.BeginSend(c, out var writer);
                    winMessage.SerializeObject(ref writer);
                    serv.m_Driver.EndSend(writer);
                }


            }
            if(result == 1)
            {
                serv.serverBuildDebugText.text = "Conclusion reached! It's a draw!";

                ChatMessage drawMessage = new ChatMessage();
                drawMessage.Message = "It's a draw!";
                foreach (NetworkConnection c in serv.m_Connections)
                {
                    serv.m_Driver.BeginSend(c, out var writer);
                    drawMessage.SerializeObject(ref writer);
                    serv.m_Driver.EndSend(writer);
                }
            }
            if(result == 0)
            {
                serv.serverBuildDebugText.text = "No conclusion reached yet, should move on to next move";


                serv.currentPlayer = (serv.currentPlayer + 1) % 2;

                for (int i = 0; i < serv.m_Connections.Length; i++)
                {

                    TurnSwitchMessage message = new TurnSwitchMessage();
                    message.IsMyTurn = serv.currentPlayer == i;

                    serv.m_Driver.BeginSend(serv.m_Connections[i], out var turnWriter);
                    message.SerializeObject(ref turnWriter);
                    serv.m_Driver.EndSend(turnWriter);

                }
            }         
        }

        uint EvaluateWinStatus()
        {
            for (int z = 0; z < 3; z++)
            {
                uint a = field[0, z];
                uint b = field[1, z];
                uint c = field[2, z];

                if (a != 0 && a == b && b == c)
                {
                    return 5;
                }
            }
            for (int v = 0; v < 3; v++)
            {
                uint a = field[v, 0];
                uint b = field[v, 1];
                uint c = field[v, 2];

                if (a != 0 && a == b && b == c)
                {
                    return 5;
                }
            }

            uint d = field[0, 0];
            uint e = field[1, 1];
            uint f = field[2, 2];

            if (d != 0 && d == e && e == f)
            {
                return 5;
            }

            uint g = field[0, 2];
            uint h = field[1, 1];
            uint i = field[2, 0];

            if (g != 0 && g == h && h == i)
            {
                return 5;
            }

            for (int n = 0; n < 3; n++)
            {
                for (int m = 0; m < 3; m++)
                {
                    if(field[n, m] == 0)
                    {
                        return 0;
                    }
                }
            }

            // This is a draw situation
            return 1;
        }

    }

}