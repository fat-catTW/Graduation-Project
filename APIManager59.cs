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

    [Header("老師頁面 Panel")]
    public GameObject TeacherPagePanel;
    public Image teacherRedDotImage;    // 第一個紅點
    public Image teacherRedDotImage2;   // 第二個紅點

    [Header("開場動畫/引導 UI")]
    public GameObject openingPanel;
    public GameObject openingIntroPanel;
    public GameObject introPagePanel;

    private string baseUrl = "https://feyndora-api.onrender.com";

    private const string RED_DOT_STATE_KEY = "TeacherRedDotState";

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 加載紅點狀態
        LoadRedDotState();
    }

    private void LoadRedDotState()
    {
        bool redDotState = PlayerPrefs.GetInt(RED_DOT_STATE_KEY, 0) == 1;
        if (teacherRedDotImage != null)
            teacherRedDotImage.gameObject.SetActive(redDotState);
        if (teacherRedDotImage2 != null)
            teacherRedDotImage2.gameObject.SetActive(redDotState);
    }

    private void SaveRedDotState(bool state)
    {
        PlayerPrefs.SetInt(RED_DOT_STATE_KEY, state ? 1 : 0);
        PlayerPrefs.Save();
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

        // 啟動流程
        if (PlayerPrefs.HasKey("UserID"))
        {
            StartCoroutine(ShowOpeningThenHomePage());
        }
        else
        {
            // 如果沒有用戶登入，播放完整動畫序列
            StartCoroutine(ShowOpeningIntroSequence());
        }
    }

    private IEnumerator ShowOpeningThenHomePage()
    {
        // 隱藏其他面板
        if (SigninPanel != null) SigninPanel.SetActive(false);
        if (LoginPanel != null) LoginPanel.SetActive(false);
        if (HomePagePanel != null) HomePagePanel.SetActive(false);
        if (openingIntroPanel != null) openingIntroPanel.SetActive(false);
        if (introPagePanel != null) introPagePanel.SetActive(false);

        // 顯示 OpeningPanel
        if (openingPanel != null) openingPanel.SetActive(true);

        // 預先加載 ProfilePagePanel 但放在螢幕外
        if (ProfilePanel != null)
        {
            ProfilePanel.SetActive(true);
            RectTransform rectTransform = ProfilePanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 將面板移到螢幕外
                rectTransform.anchoredPosition = new Vector2(2000f, 0f);
            }
        }

        // 等待資料加載完成
        yield return StartCoroutine(RefreshAllData());

        // 資料加載完畢後直接切換到主頁
        if (openingPanel != null) openingPanel.SetActive(false);
        if (HomePagePanel != null) HomePagePanel.SetActive(true);

        // 將 ProfilePagePanel 移回原位但保持隱藏
        if (ProfilePanel != null)
        {
            RectTransform rectTransform = ProfilePanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
            ProfilePanel.SetActive(false);
        }
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
            // 如果沒有用戶登入，播放完整動畫序列
            StartCoroutine(ShowOpeningIntroSequence());
        }
    }

    public IEnumerator RefreshAllData()
    {
        yield return StartCoroutine(FetchUserData());
        yield return StartCoroutine(GetLatestCourseProgress((response) => {
            if (response != null && response.hasCourse)
            {
                // 更新本地儲存的課程資訊
                PlayerPrefs.SetInt("current_course_id", response.course_id);
                PlayerPrefs.SetString("current_course_name", response.course_name);
                PlayerPrefs.SetString("current_stage", response.current_stage);
                PlayerPrefs.Save();

                Debug.Log($"取得最新課程進度成功：{response.course_name}, 進度: {response.progress}%");
            }
            else
            {
                Debug.LogWarning("目前沒有課程記錄");
                // 清空本地儲存的課程資訊
                PlayerPrefs.DeleteKey("current_course_id");
                PlayerPrefs.DeleteKey("current_course_name");
                PlayerPrefs.DeleteKey("current_stage");
                PlayerPrefs.Save();
            }
        }));
        yield return StartCoroutine(courseManager.LoadCourses());

        // 呼叫 ProfileManager 更新最新數據
        if (ProfileManager.Instance != null)
        {
            ProfileManager.Instance.RefreshProfile();
        }

        // 檢查簽到狀態
        yield return StartCoroutine(CheckSigninStatus());

        // 刷新每週任務
        if (WeeklyTaskManager.Instance != null)
        {
            WeeklyTaskManager.Instance.RefreshTasks();
        }

        // 刷新排行榜
        if (RankingManager.Instance != null)
        {
            RankingManager.Instance.ResetLogoutState();  // 重置登出狀態
            RankingManager.Instance.FetchRanking();
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
                PlayerPrefs.SetInt("TotalPoints", jsonResponse.total_learning_points);
                PlayerPrefs.SetInt("TotalSigninDays", jsonResponse.total_signin_days);
                PlayerPrefs.Save();

                welcomeText.text = "你好, " + jsonResponse.username;

                // 先激活 TeacherPagePanel 但移到視窗外
                if (TeacherPagePanel != null)
                {
                    TeacherPagePanel.SetActive(true);
                    RectTransform teacherRectTransform = TeacherPagePanel.GetComponent<RectTransform>();
                    if (teacherRectTransform != null)
                    {
                        // 將面板移到螢幕外
                        teacherRectTransform.anchoredPosition = new Vector2(2000f, 0f);
                    }

                    // 確保 LevelSelector 被正確初始化
                    var levelSelector = FindFirstObjectByType<LevelSelector>();
                    if (levelSelector != null)
                    {
                        levelSelector.ClearCards();
                        levelSelector.LoadUserCards();
                        Debug.Log("✅ 登入時已初始化 TeacherPagePanel 的卡片");
                    }
                    else
                    {
                        Debug.LogError("❌ 找不到 LevelSelector 組件");
                    }
                }

                // 新增：重新初始化成就資料
                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.ReinitializeAchievements();
                }
                if (PresetCoursesManager.Instance != null)
                {
                    PresetCoursesManager.Instance.ReinitializeCourses();
                }
                if (WeeklyTaskManager.Instance != null)
                {
                    WeeklyTaskManager.Instance.ReloadTasksOnLogin(jsonResponse.user_id);
                }

                // 初始化簽到紀錄
                yield return StartCoroutine(InitializeSigninRecord());
                // 刷新所有數據
                yield return StartCoroutine(RefreshAllData());

                // 新增：刷新排行榜
                if (RankingManager.Instance != null)
                {
                    RankingManager.Instance.FetchRanking();
                }

                // 新增：更新 ProfileManager 中的老師卡片
                if (ProfileManager.Instance != null)
                {
                    ProfileManager.Instance.UpdateTeacherCard();
                    Debug.Log("已更新 ProfileManager 中的老師卡片");
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

    IEnumerator GetLatestCourseProgress(System.Action<LatestCourseResponse> callback)
    {
        int userID = PlayerPrefs.GetInt("UserID");
        string url = $"{baseUrl}/latest_course/{userID}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LatestCourseResponse response = JsonUtility.FromJson<LatestCourseResponse>(request.downloadHandler.text);
                if (response != null)
                {
                    Debug.Log($"成功獲取最新課程：{response.course_name}, 進度: {response.progress}%");
                    callback?.Invoke(response);
                }
                else
                {
                    Debug.LogWarning("未找到課程記錄");
                    callback?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"❌ 取得最新課程進度失敗：{request.downloadHandler.text}");
                callback?.Invoke(null);
            }
        }
    }

    public void Logout()
    {
        Debug.Log("開始登出流程...");
        // 隱藏所有主頁面
        if (HomePagePanel != null) HomePagePanel.SetActive(false);
        if (ProfilePanel != null) ProfilePanel.SetActive(false);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
        if (SigninPanel != null) SigninPanel.SetActive(false);
        if (LoginPanel != null) LoginPanel.SetActive(false);
        if (openingPanel != null) openingPanel.SetActive(false);
        if (openingIntroPanel != null) openingIntroPanel.SetActive(false);
        if (introPagePanel != null) introPagePanel.SetActive(false);
        // 通知所有 Manager 清理
        if (ProfileManager.Instance != null) ProfileManager.Instance.ClearProfileUI();
        if (RankingManager.Instance != null) RankingManager.Instance.ClearAllUI();
        if (AchievementManager.Instance != null) AchievementManager.Instance.ClearUserAchievementData();
        if (WeeklyTaskManager.Instance != null) WeeklyTaskManager.Instance.ClearUIOnLogout();
        if (PresetCoursesManager.Instance != null) PresetCoursesManager.Instance.ClearUI();
        courseManager.ClearCourses();
        // 開始顯示登出動畫流程
        StartCoroutine(LogoutShowPanels());
        Debug.Log("登出流程完成");
    }

    private IEnumerator LogoutShowPanels()
    {
        // 初始化狀態：確保其他面板都是隱藏的
        if (SigninPanel != null) SigninPanel.SetActive(false);
        if (LoginPanel != null) LoginPanel.SetActive(false);

        // 開始動畫序列
        if (openingPanel != null)
        {
            openingPanel.SetActive(true);
            yield return new WaitForSeconds(1.0f);  // 從 0.5f 改為 1.0f
            openingPanel.SetActive(false);
        }

        if (openingIntroPanel != null)
        {
            openingIntroPanel.SetActive(true);
            yield return new WaitForSeconds(1.5f);  // 從 1.0f 改為 1.5f
            openingIntroPanel.SetActive(false);
        }

        if (introPagePanel != null)
        {
            introPagePanel.SetActive(true);
        }

        // 清空 PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }

    [System.Obsolete]
    public IEnumerator ClaimSigninReward()
    {
        int userID = PlayerPrefs.GetInt("UserID");
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm($"{baseUrl}/signin/claim/{userID}", ""))
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

                // 新增：更新任務進度
                if (WeeklyTaskManager.Instance != null)
                {
                    WeeklyTaskManager.Instance.RefreshTasks();
                    Debug.Log("✅ 簽到後已更新任務進度");
                }
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
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm($"{baseUrl}/signin/init/{userID}", ""))
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

                // 如果是新的一週，重置 UI
                if (jsonResponse.is_new_week)
                {
                    Debug.Log("🆕 檢測到新的一週開始，重置簽到 UI");
                    ResetSigninUI();
                }

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

    // ✅ 1️⃣ 獲取收藏課程（回傳收藏的課程名稱）
    public IEnumerator GetSavedCourses(int userId, System.Action<List<string>> callback)
    {
        string url = $"{baseUrl}/saved_courses/{userId}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SavedCoursesResponse response = JsonUtility.FromJson<SavedCoursesResponse>(request.downloadHandler.text);
                callback(response.saved_courses); // 回傳收藏課程列表
            }
            else
            {
                Debug.LogError("❌ 獲取收藏課程失敗：" + request.downloadHandler.text);
            }
        }
    }

    // ✅ 2️⃣ 收藏課程（傳遞課程名稱）
    public IEnumerator SaveCourse(int userId, string courseName)
    {
        string jsonData = $"{{\"user_id\": {userId}, \"course_name\": \"{courseName}\"}}";

        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/save_course", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ 收藏課程失敗：" + request.downloadHandler.text);
            }
            else
            {
                Debug.Log($"✅ 成功收藏課程: {courseName}");
            }
        }
    }

    // ✅ 3️⃣ 取消收藏課程（傳遞課程名稱）
    public IEnumerator RemoveSavedCourse(int userId, string courseName)
    {
        string jsonData = $"{{\"user_id\": {userId}, \"course_name\": \"{courseName}\"}}";

        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/remove_course", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ 取消收藏課程失敗：" + request.downloadHandler.text);
            }
            else
            {
                Debug.Log($"✅ 成功取消收藏課程: {courseName}");
            }
        }
    }

    [System.Serializable]
    public class SavedCoursesResponse
    {
        public List<string> saved_courses; // 這裡的類型改成 string，因為後端存的是課程名稱
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
    public class LatestCourseResponse
    {
        public bool hasCourse;
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
        public int weekly_streak;
        public bool is_new_week;
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

    // 新增：重置簽到 UI 的方法
    private void ResetSigninUI()
    {
        // 重置所有簽到狀態
        for (int i = 0; i < 7; i++)
        {
            if (lineImages != null && lineImages.Length > i && lineImages[i] != null)
                lineImages[i].gameObject.SetActive(false); // 隱藏所有高亮邊框

            if (gotImages != null && gotImages.Length > i && gotImages[i] != null)
                gotImages[i].gameObject.SetActive(false); // 隱藏所有已領取標記

            if (dayImages != null && dayImages.Length > i && dayImages[i] != null)
            {
                if (i == 6) // 第7天的特殊顏色
                    dayImages[i].color = new Color(0.89f, 0.84f, 1f); // E2D7FF
                else // 其他天的顏色
                    dayImages[i].color = new Color(0.99f, 0.89f, 0.99f); // FEE9FF
            }
        }

        // 重置按鈕狀態
        if (claimRewardButton != null)
            claimRewardButton.gameObject.SetActive(true);
        if (comeBackTomorrowButton != null)
            comeBackTomorrowButton.gameObject.SetActive(false);

        Debug.Log("✅ 簽到 UI 已重置為新的一週");
    }

    [System.Serializable]
    public class DrawCardResponse
    {
        public bool success;
        public int card_id;
        public string card_name;
        public string rarity;
        public int remaining_coins;
        public int remaining_diamonds;
        public bool is_new_teacher_card;
    }

    public IEnumerator DrawCard(bool isPremium)
    {
        int userID = PlayerPrefs.GetInt("UserID");
        string url = $"{baseUrl}/draw_card/{userID}?type={(isPremium ? "premium" : "normal")}";

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, ""))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<DrawCardResponse>(request.downloadHandler.text);

                // 更新用戶資源
                PlayerPrefs.SetInt("Coins", response.remaining_coins);
                PlayerPrefs.SetInt("Diamonds", response.remaining_diamonds);

                // 更新UI顯示
                coinsText.text = response.remaining_coins.ToString();
                diamondsText.text = response.remaining_diamonds.ToString();

                // 如果是新獲得的老師卡片
                if (response.is_new_teacher_card)
                {
                    // 顯示紅點並保存狀態
                    if (teacherRedDotImage != null)
                        teacherRedDotImage.gameObject.SetActive(true);
                    if (teacherRedDotImage2 != null)
                        teacherRedDotImage2.gameObject.SetActive(true);

                    SaveRedDotState(true);

                    // 確保 TeacherPagePanel 是激活的
                    if (TeacherPagePanel != null)
                    {
                        TeacherPagePanel.SetActive(true);
                        CanvasGroup canvasGroup = TeacherPagePanel.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                        {
                            canvasGroup = TeacherPagePanel.AddComponent<CanvasGroup>();
                        }
                        canvasGroup.alpha = 0f;
                        canvasGroup.blocksRaycasts = false;
                        canvasGroup.interactable = false;
                    }

                    // 在背景更新 TeacherPagePanel 的資料
                    var levelSelector = FindFirstObjectByType<LevelSelector>();
                    if (levelSelector != null)
                    {
                        // 先清理現有卡片
                        levelSelector.ClearCards();
                        // 重新加載卡片
                        levelSelector.LoadUserCards();
                        Debug.Log("✅ 獲得新卡片，已在背景更新老師頁面");
                    }
                    else
                    {
                        Debug.LogError("❌ 更新卡片時找不到 LevelSelector 組件");
                    }
                }

                // 顯示抽卡結果
                LotteryManager.Instance.ShowDrawResult(response.card_name, response.rarity);
            }
            else
            {
                Debug.LogError("抽卡失敗：" + request.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    public class UserCardData
    {
        public int card_id;
        public string name;
        public string rarity;
        public bool is_selected;
    }

    [System.Serializable]
    private class UserCardsResponse
    {
        public List<UserCardData> cards;
    }

    [System.Serializable]
    private class SelectCardRequest
    {
        public int user_id;
        public int card_id;
    }

    [System.Serializable]
    public class SelectCardResponse
    {
        public string message;
        public int selected_card_id;
        public bool success;
    }

    public void GetUserCards(int userId, System.Action<List<UserCardData>> callback)
    {
        StartCoroutine(GetUserCardsCoroutine(userId, callback));
    }

    private IEnumerator GetUserCardsCoroutine(int userId, System.Action<List<UserCardData>> callback)
    {
        string url = $"{baseUrl}/user_cards/{userId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                UserCardsResponse response = JsonUtility.FromJson<UserCardsResponse>(json);
                callback?.Invoke(response.cards);
            }
            else
            {
                Debug.LogError($"獲取用戶卡片失敗：{request.error}");
                callback?.Invoke(null);
            }
        }
    }

    public IEnumerator SelectTeacherCard(int cardId, System.Action<SelectCardResponse> callback)
    {
        int userId = PlayerPrefs.GetInt("UserID");
        string jsonData = JsonUtility.ToJson(new SelectCardRequest { user_id = userId, card_id = cardId });
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/select_teacher_card", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<SelectCardResponse>(request.downloadHandler.text);
                response.success = true;
                Debug.Log($"✅ 成功選擇卡片 {response.selected_card_id}：{response.message}");
                callback?.Invoke(response);
            }
            else
            {
                Debug.LogError($"❌ 選擇卡片失敗：{request.downloadHandler.text}");
                callback?.Invoke(new SelectCardResponse { success = false });
            }
        }
    }

    public void ShowTeacherPage()
    {
        if (TeacherPagePanel != null)
        {
            // 先设置面板为激活状态
            TeacherPagePanel.SetActive(true);
            
            // 將面板移回原位
            RectTransform teacherRectTransform = TeacherPagePanel.GetComponent<RectTransform>();
            if (teacherRectTransform != null)
            {
                teacherRectTransform.anchoredPosition = Vector2.zero;
            }
            
            // 获取 LevelSelector 组件
            var levelSelector = TeacherPagePanel.GetComponentInChildren<LevelSelector>();
            if (levelSelector == null)
            {
                Debug.LogError("❌ TeacherPagePanel 中找不到 LevelSelector 组件，请检查预制体设置");
                return;
            }

            // 开启协程来处理卡片初始化和显示
            StartCoroutine(InitializeAndShowCards(levelSelector));

            // 隐藏红点并保存状态
            if (teacherRedDotImage != null)
                teacherRedDotImage.gameObject.SetActive(false);
            if (teacherRedDotImage2 != null)
                teacherRedDotImage2.gameObject.SetActive(false);

            SaveRedDotState(false);
        }
    }

    private IEnumerator InitializeAndShowCards(LevelSelector levelSelector)
    {
        // 清理现有卡片
        levelSelector.ClearCards();

        // 等待一帧，确保清理完成
        yield return null;

        // 加载卡片数据
        levelSelector.LoadUserCards();

        // 等待一帧，确保卡片加载完成
        yield return null;

        // 等待额外的一帧，确保所有卡片位置都已经正确计算
        yield return null;

        // 显示面板
        TeacherPagePanel.SetActive(true);

        Debug.Log("✅ 显示老师页面，已完成卡片初始化和显示");
    }

    public void HideTeacherPage()
    {
        if (TeacherPagePanel != null)
        {
            TeacherPagePanel.SetActive(false);
            Debug.Log("隱藏老師頁面面板");
        }
    }

    private IEnumerator ShowOpeningIntroSequence()
    {
        // 初始化狀態：確保其他面板都是隱藏的
        if (SigninPanel != null) SigninPanel.SetActive(false);
        if (LoginPanel != null) LoginPanel.SetActive(false);
        if (HomePagePanel != null) HomePagePanel.SetActive(false);

        // 開始動畫序列
        if (openingPanel != null)
        {
            openingPanel.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            openingPanel.SetActive(false);
        }

        if (openingIntroPanel != null)
        {
            openingIntroPanel.SetActive(true);
            yield return new WaitForSeconds(1.0f);
            openingIntroPanel.SetActive(false);
        }

        if (introPagePanel != null)
        {
            introPagePanel.SetActive(true);
        }
    }
}
