//這個版本涵蓋成就與任務，不影響成就Manager
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

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

    [Header("簽到 UI")]
    public GameObject SigninRewardPanel;     // 簽到面板，請在 Inspector 指派
    public Button claimRewardButton;         // 領取獎勵按鈕
    public Button comeBackTomorrowButton;    // 明天再來按鈕
    public Button closeSigninPanelButton;    // 離開（關閉簽到面板）按鈕

    [Header("簽到獎勵狀態 UI")]
    public Image[] dayImages;      // 每天獎勵的背景圖（長度 7）
    public Image[] gotImages;      // 已領取標記（長度 7）
    public Image[] lineImages;     // 當天高亮邊框（長度 7）

    private string baseUrl = "https://feyndora-api.onrender.com";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    [System.Obsolete]
    void Start()
    {
        // 載入頭貼（從 PlayerPrefs）
        avatarManager.UpdateHomePageAvatar(PlayerPrefs.GetInt("AvatarID", 1));

        registerButton.onClick.AddListener(() => StartCoroutine(RegisterUser()));
        loginButton.onClick.AddListener(() => StartCoroutine(LoginUser()));

        if (logoutButton != null)
            logoutButton.onClick.AddListener(Logout);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(() => StartCoroutine(RefreshAllData()));

        if (claimRewardButton != null)
            claimRewardButton.onClick.AddListener(() => StartCoroutine(ClaimSigninReward()));

        if (comeBackTomorrowButton != null)
            // 進入時先隱藏「明天再來」按鈕
            comeBackTomorrowButton.gameObject.SetActive(false);

        if (closeSigninPanelButton != null)
            closeSigninPanelButton.onClick.AddListener(CloseSigninPanel);

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

            // 登入後同步更新：用戶資料、進度與課程列表，以及檢查簽到狀態
            StartCoroutine(RefreshAllData());
        }
        else
        {
            SigninPanel.SetActive(true);
            LoginPanel.SetActive(false);
            HomePagePanel.SetActive(false);
        }
    }

    // 一次刷新：用戶資料、進度與課程列表，同時通知 ProfileManager 更新，再檢查簽到狀態
    public IEnumerator RefreshAllData()
    {
        yield return StartCoroutine(FetchUserData());
        yield return StartCoroutine(FetchCurrentStage());
        yield return StartCoroutine(courseManager.LoadCourses());

        // 呼叫 ProfileManager 更新最新數據
        ProfileManager.Instance?.RefreshProfile();

        // 檢查簽到狀態
        yield return StartCoroutine(CheckSigninStatus());

        // **🔹 新增：刷新每週任務**
        if (WeeklyTaskManager.Instance != null)
        {
            WeeklyTaskManager.Instance.RefreshTasks();
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

    [System.Obsolete]
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

                // 儲存新用戶資料到 PlayerPrefs
                PlayerPrefs.SetInt("UserID", jsonResponse.user_id);
                PlayerPrefs.SetString("Username", jsonResponse.username);
                PlayerPrefs.SetInt("Coins", jsonResponse.coins);
                PlayerPrefs.SetInt("Diamonds", jsonResponse.diamonds);
                PlayerPrefs.SetInt("AvatarID", jsonResponse.avatar_id);
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                // 新增：重新初始化成就資料（確保 AchievementManager 重新載入）
                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.ReinitializeAchievements();
                }

                // 初始化簽到紀錄（若尚未建立，後端會建立）
                yield return StartCoroutine(InitializeSigninRecord());
                // 刷新所有數據
                yield return StartCoroutine(RefreshAllData());
                // **🔹 新增：刷新每週任務**
                if(WeeklyTaskManager.Instance != null)
                {
                    WeeklyTaskManager.Instance.ReloadTasksOnLogin(jsonResponse.user_id);
                }


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
                PlayerPrefs.SetInt("TotalSigninDays", jsonResponse.total_signin_days);
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
        // 清除所有本地儲存
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        courseManager.ClearCourses();

        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.ClearAllUI();
        }
        else
        {
            Debug.LogWarning("RankingManager.Instance 為 null");
        }

        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.ClearProfileUI();
        }

        if (AchievementManager.Instance != null)
        {
            AchievementManager.Instance.ClearUserAchievementData();
        }
        if (WeeklyTaskManager.Instance != null)
        {
            WeeklyTaskManager.Instance.ClearUIOnLogout();
        }

        HomePagePanel.SetActive(false);
        ProfilePanel.SetActive(false);
        SettingsPanel.SetActive(false);

        SigninPanel.SetActive(true);
        LoginPanel.SetActive(true);
    }

    [System.Obsolete]
    public IEnumerator ClaimSigninReward()
    {
        int userID = PlayerPrefs.GetInt("UserID");
        using (UnityWebRequest request = UnityWebRequest.Post($"{baseUrl}/signin/claim/{userID}", ""))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var jsonResponse = JsonUtility.FromJson<SigninClaimResponse>(request.downloadHandler.text);

                Debug.Log("✅ 簽到領取獎勵成功");

                // ✅ **使用後端回傳的簽到日期**
                PlayerPrefs.SetString("LastSigninDate", jsonResponse.last_signin_date);
                PlayerPrefs.Save();

                // 更新 UI 為已領取狀態：
                if (claimRewardButton != null)
                    claimRewardButton.gameObject.SetActive(false);
                if (comeBackTomorrowButton != null)
                    comeBackTomorrowButton.gameObject.SetActive(true);

                UpdateSigninUIAfterClaim();

                // 刷新用戶資料（獎勵數值）
                yield return StartCoroutine(FetchUserData());
            }
            else
            {
                Debug.LogError("❌ 簽到領取獎勵失敗：" + request.downloadHandler.text);
            }
        }
    }

    // 新增：初始化簽到紀錄（若用戶從未簽到過）
    [System.Obsolete]
    IEnumerator InitializeSigninRecord()
    {
        int userID = PlayerPrefs.GetInt("UserID");
        using (UnityWebRequest request = UnityWebRequest.Post($"{baseUrl}/signin/init/{userID}", ""))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ 成功初始化簽到紀錄：" + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("❌ 簽到紀錄初始化失敗：" + request.downloadHandler.text);
            }
        }
    }

public IEnumerator CheckSigninStatus()
{
    int userID = PlayerPrefs.GetInt("UserID");
    using (UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/signin/status/{userID}"))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var jsonResponse = JsonUtility.FromJson<SigninStatusResponse>(request.downloadHandler.text);

            // ✅ **從後端獲取台灣時間**
            string serverToday = jsonResponse.server_today;
            string lastSigninDate = jsonResponse.last_signin_date;

            PlayerPrefs.SetInt("CurrentSigninDay", jsonResponse.signin_day);
            PlayerPrefs.SetString("LastSigninDate", lastSigninDate);
            PlayerPrefs.Save();

            // ✅ **用 serverToday 來判斷是否已簽到**
            bool hasClaimedToday = (lastSigninDate == serverToday);

            if (!hasClaimedToday)
            {
                if (SigninRewardPanel != null)
                {
                    SigninRewardPanel.SetActive(true);
                    Debug.Log($"📅 今日可簽到：{jsonResponse.signin_day} 天, last_signin_date: {lastSigninDate}, 伺服器時間: {serverToday}");

                    UpdateSigninUI(jsonResponse.signin_day);

                    if (claimRewardButton != null)
                        claimRewardButton.gameObject.SetActive(true);
                    if (comeBackTomorrowButton != null)
                        comeBackTomorrowButton.gameObject.SetActive(false);
                }
            }
            else
            {
                // ✅ **用戶今天已經簽到，記錄 log**
                Debug.Log($"✅ 用戶今天已簽到，無需重複簽到 | last_signin_date: {lastSigninDate} | 伺服器時間: {serverToday}");
            }
        }
        else
        {
            Debug.LogError("❌ 檢查簽到狀態失敗：" + request.downloadHandler.text);
        }
    }
    yield return null;
}

    // 更新簽到狀態 UI 根據當前簽到天數
    void UpdateSigninUI(int currentSigninDay)
    {
        // 假設 dayImages, gotImages, lineImages 長度皆為 7
        for (int i = 0; i < 7; i++)
        {
            if (i == currentSigninDay - 1) // 今天的簽到（未領取）
            {
                if (lineImages != null && lineImages.Length > i && lineImages[i] != null)
                    lineImages[i].gameObject.SetActive(true); // 顯示高亮邊框
                if (gotImages != null && gotImages.Length > i && gotImages[i] != null)
                    gotImages[i].gameObject.SetActive(false); // 隱藏已領取標記
                
                if (dayImages != null && dayImages.Length > i && dayImages[i] != null)
                {
                    if (i == 6) // 第7天未領取前，使用 E2D7FF
                        dayImages[i].color = new Color(0.89f, 0.84f, 1f); // E2D7FF
                    else
                        dayImages[i].color = new Color(0.99f, 0.89f, 0.99f); // FEE9FF
                }
            }
            else if (i < currentSigninDay - 1) // 過去已簽到的
            {
                if (lineImages != null && lineImages.Length > i && lineImages[i] != null)
                    lineImages[i].gameObject.SetActive(false); // 隱藏高亮邊框
                if (gotImages != null && gotImages.Length > i && gotImages[i] != null)
                    gotImages[i].gameObject.SetActive(true); // 顯示已領取標記
                if (dayImages != null && dayImages.Length > i && dayImages[i] != null)
                    dayImages[i].color = new Color(0.97f, 0.97f, 0.97f); // F7F7F7（已領取）
            }
            else // 未來還未簽到的
            {
                if (lineImages != null && lineImages.Length > i && lineImages[i] != null)
                    lineImages[i].gameObject.SetActive(false);
                if (gotImages != null && gotImages.Length > i && gotImages[i] != null)
                    gotImages[i].gameObject.SetActive(false);
                
                if (dayImages != null && dayImages.Length > i && dayImages[i] != null)
                {
                    if (i == 6) // 第7天未來的底色也是 E2D7FF
                        dayImages[i].color = new Color(0.89f, 0.84f, 1f); // E2D7FF
                    else
                        dayImages[i].color = new Color(0.99f, 0.89f, 0.99f); // FEE9FF
                }
            }
        }
    }

    // 領取獎勵後更新簽到 UI 為已領取狀態
    void UpdateSigninUIAfterClaim()
    {
        // 當前已領取的天數 = 當前簽到天數 - 1
        int currentSigninDay = PlayerPrefs.GetInt("CurrentSigninDay", 1) - 1;
        if (currentSigninDay < 0) currentSigninDay = 0;
        if (currentSigninDay >= 7) currentSigninDay = 6;

        if (lineImages != null && lineImages.Length > currentSigninDay && lineImages[currentSigninDay] != null)
            lineImages[currentSigninDay].gameObject.SetActive(false); // 隱藏今天的高亮邊框
        if (gotImages != null && gotImages.Length > currentSigninDay && gotImages[currentSigninDay] != null)
            gotImages[currentSigninDay].gameObject.SetActive(true); // 顯示今天的已領取標記
        if (dayImages != null && dayImages.Length > currentSigninDay && dayImages[currentSigninDay] != null)
            dayImages[currentSigninDay].color = new Color(0.97f, 0.97f, 0.97f); // F7F7F7（已領取）
    }

    // 新增：離開簽到面板的函數，由 closeSigninPanelButton 呼叫
    public void CloseSigninPanel()
    {
        if (SigninRewardPanel != null)
            SigninRewardPanel.SetActive(false);
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
        public int total_signin_days;
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
        public int total_signin_days;
    }

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

    [System.Serializable]
    public class SigninStatusResponse
    {
        public int signin_day;
        public bool has_claimed_today;
        public int reward_coins;
        public int reward_diamonds;
        public string last_signin_date;
        public string server_today;
    }

    [System.Serializable]
    public class SigninClaimResponse
    {
        public string message;
        public int signin_day;
        public int coins_received;
        public int diamonds_received;
        public string last_signin_date;
    }
}
