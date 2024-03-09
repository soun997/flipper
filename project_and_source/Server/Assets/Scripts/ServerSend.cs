using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    /// <summary>특정 클라이언트에게 데이터 보내기</summary>
    /// <param name="toClient">패킷을 보낼 클라이언트 ID</param>
    /// <param name="packet">패킷</param>
    private static void SendTCPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clients[toClient].tcp.SendData(packet);
    }

    /// <summary>Sends a packet to a client via UDP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clients[toClient].udp.SendData(packet);
    }


    /// <summary>모든 클라이언트에게 데이터 보내기</summary>
    /// <param name="packet">패킷</param>
    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(packet);
        }
    }

    /// <summary>특정 클라이언트를 제외하고 모두에게 데이터 보내기</summary>
    /// <param name="exceptClient">제외할 클라이언트 ID</param>
    /// <param name="packet">패킷</param>
    private static void SendTCPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.clients[i].tcp.SendData(packet);
            }
        }
    }

    /// <summary>Sends a packet to all clients via UDP.</summary>
    /// <param name="packet">The packet to send.</param>
    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(packet);
        }
    }
    /// <summary>Sends a packet to all clients except one via UDP.</summary>
    /// <param name="exceptClient">The client to NOT send the data to.</param>
    /// <param name="packet">The packet to send.</param>
    private static void SendUDPDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.clients[i].udp.SendData(packet);
            }
        }
    }

    #region Packets

    /// <summary>서버->클라이언트 Welcome 패킷 생성</summary>
    /// <param name="toClient">패킷을 보낼 클라이언트 ID</param>
    /// <param name="msg">클라이언트에게 보낼 Welcome 메세지</param>
    public static void Welcome(int toClient, string msg)
    {
        // 패킷을 하나 생성
        using (Packet packet = new Packet((int)ServerPackets.welcome))
        {
            // 패킷에 데이터 추가
            packet.Write(msg);
            packet.Write(toClient);

            SendTCPData(toClient, packet);
        }
    }

    public static void OnLoginOrRegisterSuccess(int toClient)
    {
        using (Packet packet = new Packet((int)ServerPackets.loginOrRegisterSuccess))
        {
            packet.Write(toClient);

            SendTCPData(toClient, packet);
        }
    }

    public static void PlayerReadyReceived() {
        using (Packet packet = new Packet((int)ServerPackets.playerReadyReceived))
        {
            packet.Write(NetworkManager.instance.readyCount);

            SendTCPDataToAll(packet);
        }
    }
    

    public static void SpawnPlayer(int toClient, Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            packet.Write(player.id);
            packet.Write(player.username);
            packet.Write(player.transform.position);
            packet.Write(player.transform.rotation);

            SendTCPData(toClient, packet);
        }
    }

    public static void PlayerDisconnected(int playerID)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            packet.Write(playerID);
            SendTCPDataToAll(packet);
        }
    }

    public static void GameStart()
    {
        using (Packet packet = new Packet((int)ServerPackets.gameStart))
        {
            Debug.Log("GameStart 패킷 보냄");
            packet.Write(true);
            SendTCPDataToAll(packet);
        }
    }

    public static void PlayerPosition(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerPosition))
        {
            packet.Write(player.id);
            packet.Write(player.transform.position);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerRotation(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerRotation))
        {
            packet.Write(player.id);
            packet.Write(player.transform.rotation);

            SendUDPDataToAll(player.id, packet);
        }
    }

    public static void PlayerFlipped(int panelID, int clientID)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerFlipped))
        {
            packet.Write(panelID);
            packet.Write(clientID);

            SendTCPDataToAll(packet);
        }
    }

    public static void Error(int toClient, string msg)
    {
        using (Packet packet = new Packet((int)ServerPackets.error))
        {
            packet.Write(msg);
            SendTCPData(toClient, packet);
        }
    }
    #endregion
}
