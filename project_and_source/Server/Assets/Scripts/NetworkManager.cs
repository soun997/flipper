using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public GameObject playerPrefab;
    public List<string> connectedPlayers;
    public bool isGameStarted;
    public int maxPlayer;
    public int playerCount;
    public int readyCount;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("싱글톤 인스턴스가 존재하므로 오브젝트를 Destroy합니다.");
            Destroy(this);
        }
    }

    private void Start()
    {
        QualitySettings.vSyncCount = 0;     // 동기화를 위한 프레임 드랍 방지
        Application.targetFrameRate = 60;   // Server Logic이 초당 30 ticks 반복되므로 서버의 FrameRate를 제한 -> CPU 사용량 저하 목적

        isGameStarted = false;
        maxPlayer = 1;  // 초기값
        readyCount = 0;

        Server.Start(10, 9330);
    }

    private void FixedUpdate()
    {
        if (!isGameStarted && readyCount == maxPlayer)
        {
            isGameStarted = true;
            ServerSend.GameStart();
        }
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, new Vector3(0f, 0.5f, 0f), Quaternion.identity).GetComponent<Player>();
    }
}
