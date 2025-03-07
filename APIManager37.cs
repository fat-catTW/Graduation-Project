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
        // 載入頭貼
        avatarManager.UpdateHomePageAvatar(PlayerPrefs.GetInt("AvatarID", 1));

        registerButton.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        loginButton.onClick.AddListener(() => StartCoroutine(LoginUser()));

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(Logout);
        }

        // 修改：刷新按鈕按下時，呼叫 RefreshAllData 來同時更新用戶資料、進度與課程列表
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(() => StartCoroutine(RefreshAllData()));
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

            // 登入後同步更新：用戶資料、進度與課程列表
            StartCoroutine(RefreshAllData());
        }
        else
        {
            SigninPanel.SetActive(true);
            LoginPanel.SetActive(false);
            HomePagePanel.SetActive(false);
        }
    }

    // 新增：RefreshAllData，一次刷新用戶資料、進度與課程列表
    public IEnumerator RefreshAllData()
    {
        yield return StartCoroutine(FetchUserData());
        yield return StartCoroutine(FetchCurrentStage());
        yield return StartCoroutine(courseManager.LoadCourses());

        // 🌟通知ProfilePageManager
        ProfileManager.Instance?.RefreshProfile();
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
                PlayerPrefs.SetInt("AvatarID", jsonResponse.avatar_id); // 存avatar_id
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                // 刷新用戶資料、進度與課程列表
                StartCoroutine(RefreshAllData());

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
                PlayerPrefs.SetInt("AvatarID", jsonResponse.avatar_id);
                PlayerPrefs.SetInt("TotalPoints", jsonResponse.total_learning_points);
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                avatarManager.UpdateHomePageAvatar(jsonResponse.avatar_id);
            }
            else
            {
                Debug.LogError("❌ 獲取數據失敗：" + request.downloadHandler.text);
            }
        }
    }

    // 登入後、或手動刷新時，呼叫此API取得最新current_stage和progress
    IEnumerator FetchCurrentStage()
    {
        int userID = PlayerPrefs.GetInt("UserID");
        string url = baseUrl + "/current_stage/" + userID;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                CurrentStageResponse response = JsonUtility.FromJson<CurrentStageResponse>(request.downloadHandler.text);
                // 將取得的課程資訊存入PlayerPrefs
                if (response.hasReadyCourse)
                {
                    PlayerPrefs.SetInt("current_course_id", response.course_id);
                    PlayerPrefs.SetString("current_course_name", response.course_name);
                    PlayerPrefs.SetString("current_stage", response.current_stage);
                    PlayerPrefs.Save();
                    Debug.Log($"取得current_stage成功：{response.current_stage}, 進度: {response.progress}%");
                }
                else
                {
                    Debug.LogWarning("目前沒有ready的課程");
                }
            }
            else
            {
                Debug.LogError("❌ 取得current_stage失敗：" + request.downloadHandler.text);
            }
        }
    }

    public void Logout()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        courseManager.ClearCourses();

        // 使用 Singleton 直接清除 Ranking UI
        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.ClearAllUI();
        }
        else
        {
            Debug.LogWarning("RankingManager.Instance 為 null");
        }

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
        public int avatar_id;
        public int total_learning_points;
    }

    [System.Serializable]
    public class UserDataResponse
    {
        public int user_id;
        public string username;
        public string email;
        public int coins;
        public int diamonds;
        public int avatar_id;
        public int total_learning_points;
    }

    // CurrentStageResponse 結構，與最新的app.py接口對應
    [System.Serializable]
    public class CurrentStageResponse
    {
        public bool hasReadyCourse;
        public int course_id;
        public string course_name;
        public string current_stage;
        public float progress;
        public float progress_one_to_one;
        public float progress_classroom;
    }
}
