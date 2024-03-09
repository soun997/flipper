using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public static int dataBufferSize = 4096;
    public Player player;
    public int id;
    public TCP tcp;
    public UDP udp;

    public Client(int clientID)
    {
        id = clientID;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    /// <summary>새로운 플레이어와 다른 플레이어 간의 연결/summary>
    /// <param name="playerName"></param>
    public void SendIntoGame(string playerName, string password)
    {
        player = NetworkManager.instance.InstantiatePlayer();
        player.Initialize(id, playerName, password);

        // 플레이어 id에 따른 생성 위치
        switch (player.id)
        {
            case 1:
                player.transform.position = new Vector3(-15f, 0.5f, 0);
                player.transform.rotation = Quaternion.EulerAngles(0f, 90f, 0f);
                break;
            case 2:
                player.transform.position = new Vector3(15f, 0.5f, 0);
                player.transform.rotation = Quaternion.EulerAngles(0f, -90f, 0f);
                break;
            case 3:
                player.transform.position = new Vector3(-15f, 0.5f, 15f);
                player.transform.rotation = Quaternion.EulerAngles(0f, 180f, 0f);
                break;
            case 4:
                player.transform.position = new Vector3(15f, 0.5f, -15f);
                player.transform.rotation = Quaternion.EulerAngles(0f, -180f, 0f);
                break;

        }

        foreach (Client client in Server.clients.Values)
        {
            if (!(client.player == null))
            {
                Debug.Log(client.player.username);
            }
        }

        // 새로운 플레이어와 다른 플레이어 연결
        foreach (Client client in Server.clients.Values)
        {
            if (client.player != null)
            {
                if (client.id != id)
                {
                    Debug.Log("다른 플레이어 생성!");
                    Debug.Log($"{client.id} / {client.player.username}");
                    ServerSend.SpawnPlayer(id, client.player);
                }
            }
        }

        // 다른 플레이어와 새로운 플레이어 연결
        foreach (Client client in Server.clients.Values)
        {
            if (client.player != null)
            {
                Debug.Log("내 플레이어 생성!");
                ServerSend.SpawnPlayer(client.id, player);
            }
        }
    }


    // 클라이언트가 연결 해제
    private void Disconnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected");

        // 플레이어가 방에 들어와있는 상태에서 접속을 끊었을 때
        if (player != null)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                UnityEngine.Object.Destroy(player.gameObject);
                player = null;
            });

            NetworkManager.instance.connectedPlayers.Remove(player.username);
            NetworkManager.instance.playerCount--;  // 접속 중인 플레이어 수 감소

            // 플레이어가 Ready 상태였다면
            if (player.isReady)
            {
                NetworkManager.instance.readyCount--;
            }

            // 서버에 아무 플레이어도 남아있지 않다면 -> maxPlayer값 초기화
            if (NetworkManager.instance.playerCount == 0)
            {
                NetworkManager.instance.maxPlayer = 1;
                NetworkManager.instance.isGameStarted = false;
                NetworkManager.instance.connectedPlayers.Clear();
                
                foreach (ColorPanel panel in ColorPanelSpawner.instance.colorPanels)
                {
                    panel.clientID = 0;
                }
            }
        }

        tcp.Disconnect();
        udp.Disconnect();

        ServerSend.PlayerDisconnected(id);
    }

    /// <summary>클라이언트 UDP</summary>
    public class TCP
    {
        public TcpClient socket;
        private readonly int id;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public TCP(int _id)
        {
            id = _id;
        }

        public void Connect(TcpClient _socket)
        {
            socket = _socket;
            // 읽어오는 버퍼와 받아오는 버퍼의 크기를 지정해줌(4096바이트)

            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            // GetStream(): TCP 네트워크 스트림 리턴, 네트워크 스트림을 이용해 데이터 송수신
            stream = socket.GetStream();

            receivedData = new Packet();    // receivedData에는 패킷을 저장
            // 패킷을 데이터를 받아오는 버퍼에 byte 배열을 해당 크기만큼 할당해줌
            receiveBuffer = new byte[dataBufferSize];

            // 읽기 시작
            // params: 읽어 온 것을 저장할 버퍼, 오프셋, 버퍼 사이즈, 콜백함수, 상태
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            //stream.Read(receiveBuffer, 0, dataBufferSize);

            ServerSend.Welcome(id, "Welcome to the server!");
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
                Debug.Log($"플레이어 {id}에게 데이터를 보내는 도중 오류가 발생했습니다. : {e}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);    // receiveBuffer의 값을 data에 복사

                receivedData.Reset(HandleData(data));

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);    // 다시 데이터 들어오기를 기다림
            }
            catch (Exception e)
            {
                Console.Write($"Error receiving TCP data: {e}");
                Server.clients[id].Disconnect();
            }
        }

        /// <summary>연속적인 데이터 여부 판별</summary>
        /// <param name="data"></param>
        /// <returns>해당 패킷이 데이터의 마지막인지 여부</returns>
        private bool HandleData(byte[] data)
        {
            int packetLength = 0;

            receivedData.SetBytes(data);

            // 패킷의 처음 데이터는 패킷의 데이터 길이(int형 4바이트)
            // int형을 읽었을 때 packetLength가 0이하가 된다면, 마지막  데이터임 -> true
            if (receivedData.UnreadLength() >= 4)
            {
                packetLength = receivedData.ReadInt();
                if (packetLength <= 0)
                {
                    return true;
                }
            }

            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                byte[] packetBytes = receivedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetID = packet.ReadInt();
                        Server.packetHandlers[packetID](id, packet);
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
            return false;
        }

        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public IPEndPoint endPoint;

        private int id;

        public UDP(int _id)
        {
            id = _id;
        }

        /// <summary>서버 UDP</summary>
        /// <param name="_endPoint">서버의 IPEndPoint -> 클라이언트와 연결</param>
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        /// <summary>UDP를 이용한 데이터 전송</summary>
        /// <param name="packet">The packet to send.</param>
        public void SendData(Packet packet)
        {
            Server.SendUDPData(endPoint, packet);
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="packetData">The packet containing the recieved data.</param>
        public void HandleData(Packet packetData)
        {
            int packetLength = packetData.ReadInt();
            byte[] packetBytes = packetData.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    Server.packetHandlers[packetId](id, packet); // Call appropriate method to handle the packet
                }
            });
        }

        /// <summary>Cleans up the UDP connection.</summary>
        public void Disconnect()
        {
            endPoint = null;
        }
    }
}