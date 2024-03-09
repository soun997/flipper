using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public static Dictionary<int, PlayerManager> players = new Dictionary<int, PlayerManager>();

    public GameObject localPlayerPrefab;
    public GameObject playerPrefab;
    public GameObject readyPanel;
    public Text readyText;
    public Text playtimeText;
    public GameObject aim;
    public GameObject resultPanel;
    public List<GameObject> labels;

    public bool isReady;
    public int readyCount;
    public bool isGameStarted;

    [SerializeField]
    private float playtime;

    public void Awake()
    {
        // 싱글톤 객체 생성
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
        isReady = false;
        isGameStarted = false;
        readyPanel.SetActive(true);
        resultPanel.SetActive(false);
        playtimeText.gameObject.SetActive(false);
        aim.SetActive(false);
        playtime = 100f;
        readyCount = 0;
    }

    private void Update()
    {
        // 제한시간이 다 되었다면
        if (isGameStarted && playtime < 0)
        {
            isGameStarted = false;
            aim.SetActive(false);

            Dictionary<int, int> results = new Dictionary<int, int>();
            for (int i = 1; i <= readyCount; i++)
            {
                results.Add(i, 0);
            }

            foreach (ColorPanel panel in ColorPanelSpawner.instance.colorPanels)
            {
                if (panel.clientID != 0)
                    results[panel.clientID]++;
            }

            GetResult(results);
            resultPanel.SetActive(true);
        }
        if (isGameStarted)
        {
            playtime -= Time.deltaTime;
            playtimeText.text = "남은 시간: " + (int)playtime + "초";
        }
        
        // 플레이어가 Ready하지 않았을 때만
        if (!isReady)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlayerManager player;
                players.TryGetValue(Client.instance.myID, out player);
                player.isReady = true;
                isReady = true;
                ClientSend.PlayerReady();
            }
        }      
    }

    public void SpawnPlayer(int id, string username, Vector3 position, Quaternion rotation)
    {
        GameObject player;
        if (id == Client.instance.myID)
        {
            player = Instantiate(localPlayerPrefab, position, rotation);
            
        }
        else
        {
            player = Instantiate(playerPrefab, position, rotation);
        }

        InitPlayer(player.GetComponent<PlayerManager>(), id, username);
        player.transform.Find("Canvas").GetChild(0).GetComponent<Text>().text = username;
        players.Add(id, player.GetComponent<PlayerManager>());
    }

    public void GameStart()
    {
        StartCoroutine(GameStartCoroutine());
    }

    private void InitPlayer(PlayerManager player, int id, string username)
    {
        player.id = id;
        player.username = username;
        player.isReady = false;
    }

    public IEnumerator GameStartCoroutine()
    {
        readyText.text = "제한시간 안에 최대한 많은\n색판을 뒤집어라!";
        yield return new WaitForSeconds(3f);
        for (int i = 5; i >= 1; i--)
        {
            readyText.text = i + "";
            yield return new WaitForSeconds(1f);
        }
        readyText.text = "Start!";
        yield return new WaitForSeconds(1f);
        readyPanel.SetActive(false);
        playtimeText.gameObject.SetActive(true);
        isGameStarted = true;
        ColorPanelSpawner.instance.panels.SetActive(true);
        aim.SetActive(true);
    }

    // 결과창 순위표
    private void GetResult(Dictionary<int, int> results)
    {
        var sortedResults = results.OrderByDescending(x => x.Value);
        int rank = 1;
        int cnt = 0;

        foreach (var result in sortedResults)
        {
            labels[cnt].SetActive(true);
            labels[cnt].transform.GetChild(0).GetComponent<Text>().text = $"{rank}위";   // 순위
            labels[cnt].transform.GetChild(1).GetComponent<Text>().text = players[result.Key].username; // 플레이어 이름
            labels[cnt].transform.GetChild(2).GetComponent<Text>().text = $"{result.Value}";    // 뒤집은 색판 개수
            // 플레이어 색
            switch (result.Key)
            {
                case 1:
                    labels[cnt].transform.GetChild(3).GetComponent<Image>().color = Color.red;
                    break;
                case 2:
                    labels[cnt].transform.GetChild(3).GetComponent<Image>().color = Color.blue;
                    break;
                case 3:
                    labels[cnt].transform.GetChild(3).GetComponent<Image>().color = Color.yellow;
                    break;
                case 4:
                    labels[cnt].transform.GetChild(3).GetComponent<Image>().color = Color.green;
                    break;
            }
            rank++;
            cnt++;
        }
    }

    public void Quit()
    {
        Debug.Log("게임종료");
        Application.Quit();
    }
}
