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

    private string baseUrl = "http://127.0.0.1:8000";  // Flask 伺服器運行中

    void Start()
    {
        registerButton.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        loginButton.onClick.AddListener(() => StartCoroutine(LoginUser()));

        // 確保進入遊戲時只顯示登入/註冊頁面
        SigninPanel.SetActive(true);
        LoginPanel.SetActive(false);
        HomePagePanel.SetActive(false);
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

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/register", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("註冊成功！跳轉到登入頁");

                // **隱藏註冊頁，顯示登入頁**
                SigninPanel.SetActive(false);
                LoginPanel.SetActive(true);
            }
            else
            {
                Debug.LogError("註冊失敗：" + request.downloadHandler.text);
            }
        }
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

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/login", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("登入成功！跳轉到主頁");

                // **隱藏登入頁，顯示主頁**
                LoginPanel.SetActive(false);
                HomePagePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("登入失敗：" + request.downloadHandler.text);
            }
        }
    }
}
