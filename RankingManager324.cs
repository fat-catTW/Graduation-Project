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
        // 確保已經有登入用戶
        if (PlayerPrefs.HasKey("UserID"))
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
            Debug.LogError("❌ UserID未設定");
            return;
        }

        // 如果在Start中已經設定avatarManager則不用用FindObjectOfType
        if (avatarManager == null)
        {
            Debug.LogError("❌ avatarManager 尚未設定，請到 Inspector 把 AvatarManager 拖進來！");
            return;
        }

        dailyButton.onClick.AddListener(() => { isWeekly = false; FetchRanking(); });
        weeklyButton.onClick.AddListener(() => { isWeekly = true; FetchRanking(); });

        // 初次載入也呼叫一次
        FetchRanking();
    }

    public void FetchRanking()
    {
        StartCoroutine(isWeekly ? FetchWeeklyRanking() : FetchDailyRanking());
    }

    IEnumerator FetchDailyRanking()
    {
        string url = $"{baseUrl}/daily_rankings?user_id={currentUserId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                DailyRankingResponse response = JsonUtility.FromJson<DailyRankingResponse>(request.downloadHandler.text);
                // 呼叫FetchAndUpdateUserData來同時取得最新的用戶資料
                StartCoroutine(FetchAndUpdateUserData(response.rankings, response.userRank));
            }
            else
            {
                Debug.LogError($"❌ 取得日排名失敗：{request.error}");
            }
        }
    }

    IEnumerator FetchWeeklyRanking()
    {
        string url = $"{baseUrl}/weekly_rankings?user_id={currentUserId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                WeeklyRankingResponse response = JsonUtility.FromJson<WeeklyRankingResponse>(request.downloadHandler.text);
                StartCoroutine(FetchAndUpdateUserData(response.rankings, response.userRank));
            }
            else
            {
                Debug.LogError($"❌ 取得週排名失敗：{request.error}");
            }
        }
    }

    IEnumerator FetchAndUpdateUserData(List<RankData> top10, RankData userRankData)
    {
        string url = $"{baseUrl}/user/{currentUserId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            UserData latestUser = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                latestUser = JsonUtility.FromJson<UserData>(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("❌ 取得用戶資料失敗：" + request.error);
            }

            UpdateRankingUI(top10, userRankData, latestUser);
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

    public void ClearAllUI()
    {
        // 清空前10名的所有 UI 項目
        foreach (var item in rankItems)
        {
            ClearRankItem(item);
        }

        // 清空用戶自己的排名區
        userRankText.text = "";
        userNameText.text = "";
        userPointsText.text = "";
        userAvatarImage.sprite = avatarManager.GetAvatarSprite(1); // 預設頭像
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
