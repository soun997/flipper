using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class UIManager : MonoBehaviour
{
    public static UIManager instance;   // 싱글톤 객체

    public GameObject camera;
    public GameObject startMenu;
    public InputField usernameField;
    public InputField passwordField;
    public List<Toggle> maxPlayerToggles;
    public Text DebugText;

    public int maxPlayer;
    public bool isButtonClicked;
    private float timer;
    public bool isRegister;

    public System.Diagnostics.Stopwatch watch;

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
        maxPlayer = 2;
        maxPlayerToggles[0].isOn = true;
        timer = 0;
        isRegister = false;

        watch = new System.Diagnostics.Stopwatch();
    }
    
    private void Update()
    {
        if (isButtonClicked)
        {
            timer += Time.deltaTime;
            // 에러 핸들링이 불가능하다면 서버와 연결에 실패함을 의미
            if (timer > 3f)
            {
                Debug.LogError("Error: 서버가 동작하고 있지 않거나 방이 꽉 찼습니다.");
                DebugText.text = "Server is not running or Room is full";
                timer = 0f;
                isButtonClicked = false;
            }
        }     
    }

    public void OnConnectedToServer(bool isRegister)
    {
        // 텍스트필드를 다 채웠을 경우에만 로그인/회원가입 진행
        if (usernameField.text != "" && passwordField.text != "")
        {
            watch.Start();
            this.isRegister = isRegister;
            isButtonClicked = true;
            Client.instance.OnConnectedToServer();
        }
        else
        {
            DebugText.text = "Fill your TextField Area!";
        }
    }
    
    public void OnToggleValueChanges(int maxPlayer)
    {
        this.maxPlayer = maxPlayer;
    }
}
