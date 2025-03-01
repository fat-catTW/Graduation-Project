using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance;

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
    public GameObject ProfilePanel;
    public GameObject SettingsPanel;

    [Header("主頁 UI")]
    public TMP_Text welcomeText;
    public TMP_Text coinsText;
    public TMP_Text diamondsText;

    [Header("課程管理")]
    public CourseManager courseManager;

    [Header("頭貼管理")]
    public AvatarManager avatarManager;

    private string baseUrl = "https://feyndora-api.onrender.com";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        avatarManager.UpdateHomePageAvatar(PlayerPrefs.GetInt("AvatarID", 1)); // ✅ 開始時先載入頭貼

        registerButton.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        loginButton.onClick.AddListener(() => StartCoroutine(LoginUser()));

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(Logout);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(() =>
            {
                StartCoroutine(FetchUserData());
                StartCoroutine(courseManager.LoadCourses());
            });
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

            StartCoroutine(FetchUserData());
            StartCoroutine(courseManager.LoadCourses());
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

                var jsonResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                PlayerPrefs.SetInt("UserID", jsonResponse.user_id);
                PlayerPrefs.SetString("Username", jsonResponse.username);
                PlayerPrefs.SetInt("Coins", jsonResponse.coins);
                PlayerPrefs.SetInt("Diamonds", jsonResponse.diamonds);
                PlayerPrefs.SetInt("AvatarID", jsonResponse.avatar_id); // ✅ 存avatar_id
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                StartCoroutine(FetchUserData());
                StartCoroutine(courseManager.LoadCourses());

                avatarManager.UpdateHomePageAvatar(jsonResponse.avatar_id); // ✅ 馬上刷新頭像

                LoginPanel.SetActive(false);
                HomePagePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("登入失敗：" + request.downloadHandler.text);
            }
        }
    }

    public IEnumerator FetchUserData()
    {
        int userID = PlayerPrefs.GetInt("UserID");

        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/user/" + userID))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var jsonResponse = JsonUtility.FromJson<UserDataResponse>(request.downloadHandler.text);

                coinsText.text = jsonResponse.coins.ToString();
                diamondsText.text = jsonResponse.diamonds.ToString();

                PlayerPrefs.SetString("Username", jsonResponse.username);
                PlayerPrefs.SetString("UserEmail", jsonResponse.email);
                PlayerPrefs.SetInt("Coins", jsonResponse.coins);
                PlayerPrefs.SetInt("Diamonds", jsonResponse.diamonds);
                PlayerPrefs.SetInt("AvatarID", jsonResponse.avatar_id); // ✅ 這裡也更新
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                avatarManager.UpdateHomePageAvatar(jsonResponse.avatar_id); // ✅ 頭像也更新
            }
            else
            {
                Debug.LogError("❌ 獲取數據失敗：" + request.downloadHandler.text);
            }
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        courseManager.ClearCourses();

        HomePagePanel.SetActive(false);
        ProfilePanel.SetActive(false);
        SettingsPanel.SetActive(false);

        SigninPanel.SetActive(true);
        LoginPanel.SetActive(true);
    }

    [System.Serializable]
    public class LoginResponse
    {
        public int user_id;
        public string username;
        public string email;
        public int coins;
        public int diamonds;
        public int avatar_id; // ✅ 這裡加了
    }

    [System.Serializable]
    public class UserDataResponse
    {
        public int user_id;
        public string username;
        public string email;
        public int coins;
        public int diamonds;
        public int avatar_id; // ✅ 這裡也加了
    }
}
