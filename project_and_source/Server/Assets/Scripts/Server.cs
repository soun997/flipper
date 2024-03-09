using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int MaxPlayers { get; set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();  // 클라이언트 저장
    public delegate void PacketHandler(int fromClient, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener; // TCP Port Listening 기능
    private static UdpClient udpListener;   // UDP Port Listening 기능

    public static void Start(int maxPlayers, int port)
    {
        MaxPlayers = maxPlayers;    // 서버 최대 인원
        Port = port;    // 포트 번호

        Debug.Log("Starting server...");
        InitializeServerData(); // 최대인원 수 만큼 클라이언트 관리 자료구조에 Client 객체 할당 및 핸들러 연결

        tcpListener = new TcpListener(IPAddress.Parse("192.168.1.18"), Port);
        tcpListener.Start();
        // 클라이언트 접속 대기 -> 성공 시 콜백함수 호출(비동기)
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        udpListener = new UdpClient(Port);
        // UDP -> 메세지 대기(비동기)
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on {Port}.");

        // DB 관련 메소드
        DBManager.DBCreate();   // DB 생성
        DBManager.DBConnectionCheck();  // DB 연결 상태 체크
    }

    public static void Stop()
    {
        tcpListener.Stop();
        udpListener.Close();
    }

    // 클라이언트가 접속되었을 때 호출되는 콜백함수(from BeginAccepTcpClient)
    private static void TCPConnectCallback(IAsyncResult result)
    {
        TcpClient client = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Debug.Log($"Incoming connection from {client.Client.RemoteEndPoint}...");
        for (int i = 1; i <= MaxPlayers; i++)
        {
            // client의 tcp가 연결되지 않았다면
            if (clients[i].tcp.socket == null)
            {
                clients[i].tcp.Connect(client); // 연결해준다.
                return;
            }

        }
        // for loop이 완료될 때까지 계속 실행된다면, 서버가 꽉 찼다는 의미, 오류를 출력해줌
        Debug.Log($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
    }

    /// <summary>송신받은 UDP 패킷을 읽는다</summary>
    private static void UDPReceiveCallback(IAsyncResult result)
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (data.Length < 4)
            {
                return;
            }

            using (Packet packet = new Packet(data))
            {
                int clientId = packet.ReadInt();

                if (clientId == 0)
                {
                    return;
                }

                if (clients[clientId].udp.endPoint == null)
                {
                    // 새로운 연결이라면
                    clients[clientId].udp.Connect(clientEndPoint);  // 서버-클라이언트 연결 시도
                    return;
                }

                if (clients[clientId].udp.endPoint.ToString() == clientEndPoint.ToString())
                {
                    // 이미 연결된 상태라면
                    clients[clientId].udp.HandleData(packet);   // 데이터를 주고 받음
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error receiving UDP data: {e}");
        }
    }

    /// <summary>Sends a packet to the specified endpoint via UDP.</summary>
    /// <param name="clientEndPoint">The endpoint to send the packet to.</param>
    /// <param name="packet">The packet to send.</param>
    public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
    {
        try
        {
            if (clientEndPoint != null)
            {
                udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Error sending data to {clientEndPoint} via UDP: {e}");
        }
    }

    private static void InitializeServerData()
    {
        // clients 딕셔너리 채움, id는 1부터 순차적으로 부여
        for (int i = 1; i <= MaxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.playerEnter, ServerHandle.PlayerEnteredRoom},
                { (int)ClientPackets.playerReady, ServerHandle.PlayerReady },
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
                { (int)ClientPackets.playerFlip, ServerHandle.PlayerFilp },
            };
        Debug.Log("Initialized packets.");
    }
}
