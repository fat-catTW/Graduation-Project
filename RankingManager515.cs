using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class RankingManager : MonoBehaviour
{
    public static RankingManager Instance;

    [Header("AvatarManager")]
    public AvatarManager avatarManager;  // 這裡一定要拖到Inspector

    [Header("排行榜UI - 前10名")]
    public GameObject[] rankItems; // 0~9對應前10名，Inspector固定拖好

    [Header("用戶自己排名區")]
    public TMP_Text userRankText;    // 用戶自己的名次 (第X名)
    public TMP_Text userNameText;    // 用戶自己的名稱
    public TMP_Text userPointsText;  // 用戶自己的積分
    public Image userAvatarImage;    // 用戶自己的頭像

    [Header("日/週切換按鈕")]
    public Button dailyButton;
    public Button weeklyButton;

    private string baseUrl = "https://feyndora-api.onrender.com";  // Flask API
    private bool isWeekly = false;  // 預設是日排名
    private int currentUserId;
    private bool isLoggedOut = false;  // 新增：登出狀態標記

    // 新增：緩存機制
    private DailyRankingResponse cachedDailyRanking;
    private WeeklyRankingResponse cachedWeeklyRanking;
    private UserData cachedUserData;
    private float lastDailyFetchTime = 0f;
    private float lastWeeklyFetchTime = 0f;
    private float lastUserDataFetchTime = 0f;
    private const float CACHE_DURATION = 300f; // 緩存時間（秒），設為5分鐘
    private const float USER_DATA_CACHE_DURATION = 60f; // 用戶數據緩存時間（秒）

    // 新增：檢查緩存是否有效
    private bool IsCacheValid(float lastFetchTime, float duration)
    {
        return Time.time - lastFetchTime < duration;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 讓 RankingManager 在場景切換時不被刪除
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 每次RankingManager啟動時刷新資料
    private void OnEnable()
    {
        // 確保已經有登入用戶且不是登出狀態
        if (PlayerPrefs.HasKey("UserID") && !isLoggedOut)
        {
            currentUserId = PlayerPrefs.GetInt("UserID");
            // 呼叫一次FetchRanking()刷新資料
            FetchRanking();
        }
    }

    void Start()
    {
        currentUserId = PlayerPrefs.GetInt("UserID", 0);
        if (currentUserId == 0)
        {
            // 未登入時不顯示錯誤，直接 return
            return;
        }

        // 如果在Start中已經設定avatarManager則不用用FindObjectOfType
        if (avatarManager == null)
        {
            Debug.LogError("❌ avatarManager 尚未設定，請到 Inspector 把 AvatarManager 拖進來！");
            return;
        }

        // 修改按鈕監聽，添加立即切換功能
        dailyButton.onClick.AddListener(() => {
            isWeekly = false;
            // 如果有緩存，立即顯示
            if (cachedDailyRanking != null && IsCacheValid(lastDailyFetchTime, CACHE_DURATION))
            {
                UpdateRankingUI(cachedDailyRanking.rankings, cachedDailyRanking.userRank, cachedUserData);
            }
            // 無論如何都重新獲取數據
            FetchRanking();
        });

        weeklyButton.onClick.AddListener(() => {
            isWeekly = true;
            // 如果有緩存，立即顯示
            if (cachedWeeklyRanking != null && IsCacheValid(lastWeeklyFetchTime, CACHE_DURATION))
            {
                UpdateRankingUI(cachedWeeklyRanking.rankings, cachedWeeklyRanking.userRank, cachedUserData);
            }
            // 無論如何都重新獲取數據
            FetchRanking();
        });

        // 初次載入時獲取數據
        FetchRanking();
    }

    public void FetchRanking()
    {
        if (isLoggedOut || currentUserId <= 0)
        {
            Debug.Log("🔹 登出狀態或無效用戶ID，跳過排行榜刷新");
            return;
        }
        StartCoroutine(isWeekly ? FetchWeeklyRanking() : FetchDailyRanking());
    }

    IEnumerator FetchDailyRanking()
    {
        // 如果有有效緩存，直接使用
        if (cachedDailyRanking != null && IsCacheValid(lastDailyFetchTime, CACHE_DURATION))
        {
            Debug.Log("使用日排名緩存數據");
            if (cachedUserData != null && IsCacheValid(lastUserDataFetchTime, USER_DATA_CACHE_DURATION))
            {
                UpdateRankingUI(cachedDailyRanking.rankings, cachedDailyRanking.userRank, cachedUserData);
            }
            else
            {
                yield return StartCoroutine(FetchAndUpdateUserData(cachedDailyRanking.rankings, cachedDailyRanking.userRank));
            }
            yield break;
        }

        string url = $"{baseUrl}/daily_rankings?user_id={currentUserId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                cachedDailyRanking = JsonUtility.FromJson<DailyRankingResponse>(request.downloadHandler.text);
                lastDailyFetchTime = Time.time;
                yield return StartCoroutine(FetchAndUpdateUserData(cachedDailyRanking.rankings, cachedDailyRanking.userRank));
            }
            else
            {
                Debug.LogError($"❌ 取得日排名失敗：{request.error}");
            }
        }
    }

    IEnumerator FetchWeeklyRanking()
    {
        // 如果有有效緩存，直接使用
        if (cachedWeeklyRanking != null && IsCacheValid(lastWeeklyFetchTime, CACHE_DURATION))
        {
            Debug.Log("使用週排名緩存數據");
            if (cachedUserData != null && IsCacheValid(lastUserDataFetchTime, USER_DATA_CACHE_DURATION))
            {
                UpdateRankingUI(cachedWeeklyRanking.rankings, cachedWeeklyRanking.userRank, cachedUserData);
            }
            else
            {
                yield return StartCoroutine(FetchAndUpdateUserData(cachedWeeklyRanking.rankings, cachedWeeklyRanking.userRank));
            }
            yield break;
        }

        string url = $"{baseUrl}/weekly_rankings?user_id={currentUserId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                cachedWeeklyRanking = JsonUtility.FromJson<WeeklyRankingResponse>(request.downloadHandler.text);
                lastWeeklyFetchTime = Time.time;
                yield return StartCoroutine(FetchAndUpdateUserData(cachedWeeklyRanking.rankings, cachedWeeklyRanking.userRank));
            }
            else
            {
                Debug.LogError($"❌ 取得週排名失敗：{request.error}");
            }
        }
    }

    IEnumerator FetchAndUpdateUserData(List<RankData> top10, RankData userRankData)
    {
        // 如果有有效的用戶數據緩存，直接使用
        if (cachedUserData != null && IsCacheValid(lastUserDataFetchTime, USER_DATA_CACHE_DURATION))
        {
            UpdateRankingUI(top10, userRankData, cachedUserData);
            yield break;
        }

        string url = $"{baseUrl}/user/{currentUserId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                cachedUserData = JsonUtility.FromJson<UserData>(request.downloadHandler.text);
                lastUserDataFetchTime = Time.time;
                UpdateRankingUI(top10, userRankData, cachedUserData);
            }
            else
            {
                Debug.LogError("❌ 取得用戶資料失敗：" + request.error);
            }
        }
    }

    void UpdateRankingUI(List<RankData> top10, RankData userRankData, UserData latestUser)
    {
        // 清空前10名的欄位
        foreach (var item in rankItems)
        {
            ClearRankItem(item);
        }

        // 更新前10名
        for (int i = 0; i < top10.Count && i < rankItems.Length; i++)
        {
            UpdateRankItem(rankItems[i], top10[i]);
        }

        // 更新用戶自己的資料 (即使用戶未上榜也要顯示最新的名稱和頭像)
        if (latestUser != null)
        {
            userNameText.text = latestUser.username;
            userAvatarImage.sprite = avatarManager.GetAvatarSprite(latestUser.avatar_id);
        }

        if (userRankData != null)
        {
            userRankText.text = $"{userRankData.ranking}";
            userPointsText.text = $"{(isWeekly ? userRankData.weekly_points : userRankData.daily_points)} 分";
        }
        else
        {
            userRankText.text = "未上榜";
            userPointsText.text = "0 分";
        }
    }

    void ClearRankItem(GameObject item)
    {
        item.transform.Find("UsernameText").GetComponent<TMP_Text>().text = "----";
        item.transform.Find("PointsText").GetComponent<TMP_Text>().text = "0分";
        item.transform.Find("Avatar").GetComponent<Image>().sprite = avatarManager.GetAvatarSprite(1);
    }

    void UpdateRankItem(GameObject item, RankData data)
    {
        item.transform.Find("UsernameText").GetComponent<TMP_Text>().text = data.username;
        item.transform.Find("PointsText").GetComponent<TMP_Text>().text = $"{(isWeekly ? data.weekly_points : data.daily_points)} 分";
        item.transform.Find("Avatar").GetComponent<Image>().sprite = avatarManager.GetAvatarSprite(data.avatar_id);
    }

    // 修改：清除緩存的方法
    public void ClearAllUI()
    {
        isLoggedOut = true;  // 設置登出狀態
        currentUserId = 0;   // 重置用戶ID
        
        // 清除所有緩存
        cachedDailyRanking = null;
        cachedWeeklyRanking = null;
        cachedUserData = null;
        lastDailyFetchTime = 0f;
        lastWeeklyFetchTime = 0f;
        lastUserDataFetchTime = 0f;

        // 清空 UI
        foreach (var item in rankItems)
        {
            ClearRankItem(item);
        }

        userRankText.text = "";
        userNameText.text = "";
        userPointsText.text = "";
        userAvatarImage.sprite = avatarManager.GetAvatarSprite(1);
    }

    // 修改：重置登出狀態時也重置緩存
    public void ResetLogoutState()
    {
        isLoggedOut = false;
        currentUserId = PlayerPrefs.GetInt("UserID", 0);
        
        // 清除所有緩存，強制重新獲取數據
        cachedDailyRanking = null;
        cachedWeeklyRanking = null;
        cachedUserData = null;
        lastDailyFetchTime = 0f;
        lastWeeklyFetchTime = 0f;
        lastUserDataFetchTime = 0f;
        
        Debug.Log("✅ 重置排行榜登出狀態和緩存");
    }

    [System.Serializable]
    public class DailyRankingResponse
    {
        public string date;
        public List<RankData> rankings;
        public RankData userRank;
    }

    [System.Serializable]
    public class WeeklyRankingResponse
    {
        public List<RankData> rankings;
        public RankData userRank;
    }

    [System.Serializable]
    public class RankData
    {
        public int user_id;
        public string username;
        public int avatar_id;
        public int daily_points;
        public int weekly_points;
        public int ranking;
    }

    [System.Serializable]
    public class UserData
    {
        public int user_id;
        public string username;
        public int avatar_id;
    }
}
