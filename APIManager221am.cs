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

    [Header("登出按鈕")]
    public Button logoutButton;

    [Header("手動刷新按鈕")]
    public Button refreshButton;

    [Header("頁面 Panel")]
    public GameObject SigninPanel;
    public GameObject LoginPanel;
    public GameObject HomePagePanel;
    public GameObject ProfilePanel;  // 個人檔案頁面
    public GameObject SettingsPanel; // 設定頁面

    [Header("主頁 UI")]
    public TMP_Text welcomeText;
    public TMP_Text coinsText;
    public TMP_Text diamondsText;

    private string baseUrl = "https://feyndora-api.onrender.com"; 

    void Start()
    {
        registerButton.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        loginButton.onClick.AddListener(() => StartCoroutine(LoginUser()));

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(Logout);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(() => StartCoroutine(FetchUserData()));
        }

        ShowCorrectPanel();
    }

    void ShowCorrectPanel()
    {
        if (PlayerPrefs.HasKey("UserID"))
        {
            SigninPanel.SetActive(false);
            LoginPanel.SetActive(false);
            HomePagePanel.SetActive(true);

            welcomeText.text = "你好, " + PlayerPrefs.GetString("Username");

            // **🚀 每次進入主頁時都重新從伺服器獲取最新數據**
            StartCoroutine(FetchUserData());
        }
        else
        {
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
        public int coins;
        public int diamonds;
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

                LoginResponse jsonResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                PlayerPrefs.SetInt("UserID", jsonResponse.user_id);
                PlayerPrefs.SetString("Username", jsonResponse.username);
                PlayerPrefs.SetInt("Coins", jsonResponse.coins);
                PlayerPrefs.SetInt("Diamonds", jsonResponse.diamonds);
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                // **🚀 立即刷新 UI**
                StartCoroutine(FetchUserData());

                LoginPanel.SetActive(false);
                HomePagePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("登入失敗：" + request.downloadHandler.text);
            }
        }
    }

    IEnumerator FetchUserData()
    {
        int userID = PlayerPrefs.GetInt("UserID");

        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/user/" + userID))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LoginResponse jsonResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                // **更新 UI**
                coinsText.text = jsonResponse.coins.ToString();
                diamondsText.text = jsonResponse.diamonds.ToString();

                // **同步 PlayerPrefs**
                PlayerPrefs.SetInt("Coins", jsonResponse.coins);
                PlayerPrefs.SetInt("Diamonds", jsonResponse.diamonds);
                PlayerPrefs.Save();

                Debug.Log("🎉 成功刷新數據：" + jsonResponse.coins + " 金幣, " + jsonResponse.diamonds + " 鑽石");
            }
            else
            {
                Debug.LogError("❌ 獲取數據失敗：" + request.downloadHandler.text);
            }
        }
    }
    public void Logout()
    {   
    // **清除本地儲存的用戶數據**
    PlayerPrefs.DeleteKey("UserID");
    PlayerPrefs.DeleteKey("Username");
    PlayerPrefs.DeleteKey("Coins");
    PlayerPrefs.DeleteKey("Diamonds");
    PlayerPrefs.Save(); 

    Debug.Log("✅ 已登出，返回登入頁面");

    // **關閉所有與登入後相關的頁面**
    HomePagePanel.SetActive(false);
    ProfilePanel.SetActive(false);
    SettingsPanel.SetActive(false);

    // **確保回到註冊頁**
    SigninPanel.SetActive(true);
    LoginPanel.SetActive(false);
    }
}
