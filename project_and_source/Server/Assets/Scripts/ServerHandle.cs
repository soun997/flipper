using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int fromClient, Packet packet)
    {
        int clientIdCheck = packet.ReadInt();
        string username = packet.ReadString();
        string password = packet.ReadString();
        bool isRegister = packet.ReadBool();
        Debug.Log($"username: {username} / password: {password}");

        Debug.Log($"{Server.clients[fromClient].tcp.socket.Client.RemoteEndPoint} connected succesfully and is now player {fromClient}");
        // Double Check -> 매개변수로 받은 클라이언트의 ID와 패킷 데이터에 저장된 클라이언트의 ID가 같은 지
        if (fromClient != clientIdCheck)
        {
            Debug.Log($"Player \"{username}\" (Id: {fromClient}) has assumed the wrong client ID ({clientIdCheck})!");
        }

        // 회원가입 시도라면
        if (isRegister)
        {
            // 회원가입 정보가 유효한지
            // 중복되는 아이디인지
            if (DBManager.DataBaseRead($"SELECT * FROM user WHERE username='{username}'").Count != 0)
            {
                ServerSend.Error(fromClient, "Duplicated ID");
                return;
            }

            DBManager.DataBaseChange($"INSERT INTO user(username, password, flipped_panel) VALUES('{username}', '{password}', '{0}')");
        }
        // 로그인 시도라면
        else
        {
            // 로그인 정보가 유효한지 검사
            // 존재하는 아이디인지
            if (DBManager.DataBaseRead($"SELECT * FROM user WHERE username='{username}'").Count == 0)
            {
                ServerSend.Error(fromClient, "Wrong ID");
                return;
            }
            // 아이디와 패스워드가 매칭되는지
            if (DBManager.DataBaseRead($"SELECT * FROM user WHERE username='{username}' AND password='{password}'").Count == 0)
            {
                ServerSend.Error(fromClient, "Wrong Password");
                return;
            }
            // 이미 접속해있는 플레이어인지
            if (NetworkManager.instance.connectedPlayers.Contains(username))
            {
                ServerSend.Error(fromClient, "This player is already in server");
                return;
            }
        }
        
        ServerSend.OnLoginOrRegisterSuccess(fromClient);  // 로그인 정보가 유효하다면 로그인 성공 패킷 보냄
    }

    public static void PlayerEnteredRoom(int fromClient, Packet packet)
    {
        string username = packet.ReadString();
        string password = packet.ReadString();
        int maxPlayer = packet.ReadInt();

        Server.clients[fromClient].SendIntoGame(username, password);  // 클라이언트(플레이어)를 게임에 연결
        NetworkManager.instance.connectedPlayers.Add(username);
        NetworkManager.instance.playerCount++;
        // 인원 수가 초기값이라면 maxPlayer값을 받아 넣어 줌
        if (NetworkManager.instance.maxPlayer == 1)
        {
            NetworkManager.instance.maxPlayer = maxPlayer;
        }
    }

    public static void PlayerReady(int fromClient, Packet packet)
    {
        bool isReady = packet.ReadBool();
        NetworkManager.instance.readyCount++;
        Server.clients[fromClient].player.isReady = true;   // 플레이어를 레디 상태로 전환

        ServerSend.PlayerReadyReceived();
    }

    public static void PlayerMovement(int fromClient, Packet packet)
    {
        bool[] inputs = new bool[packet.ReadInt()];
        for (int i = 0; i < inputs.Length; i++)
        {
            inputs[i] = packet.ReadBool();
        }
        Quaternion rotation = packet.ReadQuaternion();

        Server.clients[fromClient].player.SetInput(inputs, rotation);
    }

    public static void PlayerFilp(int fromClient, Packet packet)
    {
        int panelID = packet.ReadInt();

        foreach (ColorPanel panel in ColorPanelSpawner.instance.colorPanels)
        {
            if (panel.panelID.Equals(panelID))
            {
                panel.clientID = fromClient;
            }
        }

        ServerSend.PlayerFlipped(panelID, fromClient);
    }
}
