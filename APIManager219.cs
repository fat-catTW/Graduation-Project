using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    [Header("註冊 UI 元件")]
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public Button registerButton;

    [Header("登入 UI 元件")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;
    public Button loginButton;

    [Header("頁面 Panel")]
    public GameObject SigninPanel;   // 註冊頁
    public GameObject LoginPanel;    // 登入頁
    public GameObject HomePagePanel; // 主頁 (登入成功後顯示)

    [Header("主頁 UI")]
    public TMP_Text welcomeText;  // 用於顯示 "你好, {username}" 的 UI 元件

    private string baseUrl = "http://127.0.0.1:8000";  // Flask 伺服器運行中

    void Start()
    {
        registerButton.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        loginButton.onClick.AddListener(() => StartCoroutine(LoginUser()));

        // 進入遊戲時顯示適當的頁面
        ShowCorrectPanel();
    }

    void ShowCorrectPanel()
    {
        if (PlayerPrefs.HasKey("UserID"))
        {
            // 已經登入過，直接進入主頁
            SigninPanel.SetActive(false);
            LoginPanel.SetActive(false);
            HomePagePanel.SetActive(true);

            // 顯示用戶名稱
            string username = PlayerPrefs.GetString("Username");
            welcomeText.text = "你好, " + username;
        }
        else
        {
            // 預設顯示登入頁
            SigninPanel.SetActive(true);
            LoginPanel.SetActive(false);
            HomePagePanel.SetActive(false);
        }
    }

    IEnumerator RegisterUser()
    {
        string username = registerUsernameInput.text;
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("所有欄位皆需填寫");
            yield break;
        }

        string jsonData = $"{{\"username\": \"{username}\", \"email\": \"{email}\", \"password\": \"{password}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/register", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("註冊成功！跳轉到登入頁");
                SigninPanel.SetActive(false);
                LoginPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("註冊失敗：" + request.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    public class LoginResponse
    {
        public int user_id;
        public string username;
    }

    IEnumerator LoginUser()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("所有欄位皆需填寫");
            yield break;
        }

        string jsonData = $"{{\"email\": \"{email}\", \"password\": \"{password}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("登入成功！跳轉到主頁");

                // 解析 JSON
                LoginResponse jsonResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                // 存入 PlayerPrefs
                PlayerPrefs.SetInt("UserID", jsonResponse.user_id);
                PlayerPrefs.SetString("Username", jsonResponse.username);
                PlayerPrefs.Save();

                // 更新主頁的用戶名稱
                welcomeText.text = "你好, " + jsonResponse.username;

                LoginPanel.SetActive(false);
                HomePagePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("登入失敗：" + request.downloadHandler.text);
            }
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteKey("UserID");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();

        Debug.Log("已登出，返回登入頁面");
        welcomeText.text = "";  // 清空用戶名稱
        SigninPanel.SetActive(true);
        LoginPanel.SetActive(false);
        HomePagePanel.SetActive(false);
    }
}
