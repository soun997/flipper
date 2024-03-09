using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    /// <summary>Sends a packet to the server via TCP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendTCPData(Packet packet)
    {
        packet.WriteLength();
        Client.instance.tcp.SendData(packet);
    }

    /// <summary>Sends a packet to the server via UDP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.instance.udp.SendData(packet);
    }

    #region Packets

    /// <summary>서버가 보낸 Welcome 패킷을 받았음을 서버에게 알림</summary>
    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(Client.instance.myID);
            packet.Write(UIManager.instance.usernameField.text);
            packet.Write(UIManager.instance.passwordField.text);
            packet.Write(UIManager.instance.isRegister);

            SendTCPData(packet);
        }
    }

    public static void PlayerEnteredRoom()
    {
        using (Packet packet = new Packet((int)ClientPackets.playerEnter))
        {
            packet.Write(UIManager.instance.usernameField.text);
            packet.Write(UIManager.instance.passwordField.text);
            packet.Write(UIManager.instance.maxPlayer);

            SendTCPData(packet);
        }
    }

    public static void PlayerReady()
    {
        using (Packet packet = new Packet((int)ClientPackets.playerReady))
        {
            packet.Write(true);
            SendTCPData(packet);
        }
    }

    public static void PlayerMovement(bool[] inputs)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(inputs.Length);
            foreach (bool input in inputs)
            {
                packet.Write(input);
            }
            packet.Write(GameManager.players[Client.instance.myID].transform.rotation);

            SendUDPData(packet);
        }
    }

    public static void PlayerFlip(ColorPanel panel)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerFlip))
        {
            packet.Write(panel.panelID);

            SendTCPData(packet);
        }
    }
    #endregion
}
