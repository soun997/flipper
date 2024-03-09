using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientHandle : MonoBehaviour
{
    /// <summary>서버로부터 Welcome 패킷을 받았을 때의 처리</summary>
    /// <param name="packet">패킷</param>
    public static void Welcome(Packet packet)
    {
        string msg = packet.ReadString();
        int myID = packet.ReadInt();

        Debug.Log($"Message from server: {msg}");

        Client.instance.myID = myID;
        ClientSend.WelcomeReceived();
        // 플레이어 이동 처리를 위한 UDP 연결
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);   
    }

    public static void OnLoginOrRegisterSuccess(Packet packet)
    {
        UIManager.instance.watch.Stop();
        Debug.Log($"총 소요시간: {UIManager.instance.watch.ElapsedMilliseconds}ms");
        SceneManager.sceneLoaded += LoadedSceneEvent;
        SceneManager.LoadScene("Room");
             
    }
    
    public static void LoadedSceneEvent(Scene scene, LoadSceneMode mode)
    {
        ClientSend.PlayerEnteredRoom();
    }


    public static void PlayerDisconnected(Packet packet)
    {
        int id = packet.ReadInt();

        if (GameManager.players.ContainsKey(id))
        {
            Destroy(GameManager.players[id].gameObject);   // player 오브젝트 Destroy
            GameManager.players.Remove(id); // player Dictionary에서 지움
        }   
    }

    public static void PlayerReadyReceived(Packet packet)
    {
        GameManager.instance.readyCount = packet.ReadInt();
    }

    public static void GameStart(Packet packet)
    {
        Debug.Log("GameStart 패킷 받음");
        GameManager.instance.GameStart();      
        Debug.Log("게임시작");
    }

    /// <summary>서버로부터 SpawnPlayer 패킷을 받았을 때의 처리</summary>
    /// <param name="packet"></param>
    public static void SpawnPlayer(Packet packet)
    {
        Debug.Log("Spawn 패킷 받음!");
        int id = packet.ReadInt();
        string username = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();

        GameManager.instance.SpawnPlayer(id, username, position, rotation);
    }

    /// <summary>서버로부터 PlayerPosition 패킷을 받았을 때의 처리</summary>
    /// <param name="packet"></param>
    public static void PlayerPosition(Packet packet)
    {
        int id = packet.ReadInt();
        Vector3 position = packet.ReadVector3();

        try
        {
            GameManager.players[id].transform.position = position;
        }
        catch (Exception e)
        {
            Debug.Log("아직 생성 안됨");
        }
    }

    /// <summary>서버로부터 PlayerRotation 패킷을 받았을 때의 처리</summary>
    /// <param name="packet"></param>
    public static void PlayerRotation(Packet packet)
    {
        int id = packet.ReadInt();
        Quaternion rotation = packet.ReadQuaternion();

        try
        {
            GameManager.players[id].transform.rotation = rotation;
        }
        catch (Exception e)
        {
            Debug.Log("아직 생성 안됨");
        }
    }

    public static void PlayerFlipped(Packet packet)
    {
        int panelID = packet.ReadInt();
        int clientID = packet.ReadInt();

        foreach (ColorPanel panel in ColorPanelSpawner.instance.colorPanels)
        {
            if (panel.panelID.Equals(panelID))
            {
                panel.clientID = clientID;
                ColorPanelSpawner.instance.ChangePanelColor(panelID, clientID);
            }
        }
    }

    public static void Error(Packet packet)
    {
        string msg = packet.ReadString();

        Debug.LogError($"서버로부터의 에러 메세지: {msg}");
        UIManager.instance.isButtonClicked = false;
        UIManager.instance.DebugText.text = msg;
        Client.instance.Disconnect();
    }
}
