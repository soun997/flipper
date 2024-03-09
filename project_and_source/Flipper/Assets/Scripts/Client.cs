using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;


public class Client : MonoBehaviour
{
    public static Client instance;  // 싱글톤 객체
    public static int dataBufferSize = 4096;

    public string ip = "192.168.1.18"; // 127.0.0.01은 loopback ip로 localhost의 아이피를 의미함
    public int port = 9330;
    public int myID = 0;
    public TCP tcp;
    public UDP udp;

    bool isConnected = false;
    private delegate void PacketHandler(Packet packet);
    private static Dictionary<int, PacketHandler> packetHandlers;


    public void Awake()
    {
        // 싱글톤 객체 생성
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);
        }
        else if (instance != this)
        {
            Debug.Log("싱글톤 인스턴스가 존재하므로 오브젝트를 Destroy합니다.");
            Destroy(this);
        }
    }

    private void Start()
    {
        
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
 
    /// <summary>서버와 연결 -> 서버로부터 Welcome 패킷을 받음으로써 확인</summary>
    public void OnConnectedToServer()
    {
        tcp = new TCP();
        udp = new UDP();

        InitializeClientData();

        isConnected = true;
        tcp.Connect();
    }

    /// <summary>연결 설정 단계, 패킷과 핸들러 매칭</summary>
    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.loginOrRegisterSuccess, ClientHandle.OnLoginOrRegisterSuccess },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
            { (int)ServerPackets.playerReadyReceived, ClientHandle.PlayerReadyReceived },
            { (int)ServerPackets.gameStart, ClientHandle.GameStart },
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition },
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation },
            { (int)ServerPackets.playerDisconnected, ClientHandle.PlayerDisconnected },
            { (int)ServerPackets.playerFlipped, ClientHandle.PlayerFlipped },
            { (int)ServerPackets.error, ClientHandle.Error },
        };
        Debug.Log("Initialized packets");
    }

    public void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected from server.");
        }
    }

    /// <summary>클라이언트 TCP</summary>
    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        /// <summary>서버와 연결 시도</summary>
        public void Connect()
        {
            // 소켓의 send, recv 버퍼 크기 지정해줌
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            // 서버ip, 9330, 콜백함수, 소켓 객체 -> 서버와 연결 시도   
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);   
        }

        /// <summary>연결 시도 결과</summary>
        /// <param name="result">비동기 연결 시도 결과</param>
        private void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);  // 비동기 연결시도 끝냄

            // 연결 안되어있다면 -> return
            if (!socket.Connected)
            {
                return;
            }

            // 연결되어 있다면 아래 스크립트 실행
            stream = socket.GetStream();

            receivedData = new Packet();    // Init

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);  // 소켓을 통해 서버로부터의 데이터 읽어옴
            //stream.Read(receiveBuffer, 0, dataBufferSize);
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (socket != null)
                {
                    //stream.Write(packet.ToArray(), 0, packet.Length());
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"TCP를 통해 서버에 데이터를 보내는 도중 오류가 발생했습니다. :  {e}");
            }
        }

        /// <summary>서버가 보낸 데이터를 받았을 때의 콜백</summary>
        /// <param name="result"></param>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);    // 비동기 읽기 시도 끝냄
                // 읽은 게 없다면, return
                if (byteLength <= 0)
                {
                    Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                // 보내고자 하는 데이터가 너무 클 경우 여러번 나누어 보낼 수도 있음
                // 패킷을 받는 족족 Reset하게 되면 데이터를 취합할 수 없음
                // HandleData의 리턴값을 이용하여 데이터를 모두 읽어서 Reset해도 되는 패킷인지 아닌지를 판단
                receivedData.Reset(HandleData(data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>receivedData를 사용할 수 있도록 handle</summary>
        /// <param name="data">receivedData</param>
        /// <returns>해당 패킷이 데이터의 마지막인지 여부</returns>
        private bool HandleData(byte[] data)
        {
            int packetLength = 0;

            receivedData.SetBytes(data);

            // 패킷 데이터의 처음은 패킷의 데이터 길이(int형 4바이트)
            // int형을 읽었을 때 packetLength가 0이하가 된다면, 패킷에 데이터 없음
            if (receivedData.UnreadLength() >= 4)
            {
                packetLength = receivedData.ReadInt();
                // 패킷에 데이터가 없다면
                if (packetLength <= 0)
                {
                    return true;    // receviedData Reset, 재사용 가능하도록
                }
            }

            // 패킷에 데이터를 모두 읽을 때까지
            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                byte[] packetBytes = receivedData.ReadBytes(packetLength);
                // MainThread에서 실행되어야할 함수
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetID = packet.ReadInt();    // 패킷 ID 받아옴
                        packetHandlers[packetID](packet);   // 해당 패킷 핸들러함수 실행
                    }
                });

                packetLength = 0;

                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (packetLength <= 1)
            {
                return true;
            }
            // 아직도 데이터를 모두 읽어오지 못했다면 -> 패킷 분할해서 보낸 것임
            return false;
        }

        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    /// <summary>클라이언트 UDP</summary>
    public class UDP
    {
        public UdpClient socket;    // UDP 클라이언트 생성
        public IPEndPoint endPoint; // endpoint 지정

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        /// <summary>UDP  사용을 위해 서버와 연결</summary>
        /// <param name="localPort">socket의 bind를 위한 port#</param>
        public void Connect(int localPort)
        {
            socket = new UdpClient(localPort);

            socket.Connect(endPoint);   // IPEndPoint
            socket.BeginReceive(ReceiveCallback, null); // 서버로부터의 입력 받음

            // 서버에게 UDP 패킷 전송
            using (Packet packet = new Packet())
            {
                SendData(packet);
            }
        }

        /// <summary>UDP를 이용한 데이터 전송</summary>
        /// <param name="packet">The packet to send.</param>
        public void SendData(Packet packet)
        {
            try
            {
                packet.InsertInt(instance.myID); // 패킷의 시작부분에 클라이언트의 ID 추가
                if (socket != null)
                {
                    socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Error sending data to server via UDP: {e}");
            }
        }

        /// <summary>Receives incoming UDP data.</summary>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                byte[] data = socket.EndReceive(result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                HandleData(data);
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="data">The recieved data.</param>
        private void HandleData(byte[] data)
        {
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                data = packet.ReadBytes(packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(data))
                {
                    int packetId = packet.ReadInt();
                    packetHandlers[packetId](packet); // Call appropriate method to handle the packet
                }
            });
        }

        /// <summary>Disconnects from the server and cleans up the UDP connection.</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

}
