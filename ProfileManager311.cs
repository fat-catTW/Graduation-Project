using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance;  // Singleton

    [Header("AvatarManager (請在Inspector設定)")]
    public AvatarManager avatarManager;

    [Header("用戶資料 UI")]
    public Image avatarImage;
    public TMP_Text usernameText;
    public TMP_Text emailText;
    public TMP_Text totalPointsText;
    public TMP_Text diamondsText;
    public TMP_Text coinsText;
    public TMP_Text coursesCountText;  // 顯示用戶課程數量

    [Header("分數統計 - 長條圖 (Bar Chart)")]
    public Image[] barImages;  // 7 個 Bar (代表最近 7 天)
    public float baseHeight = 50f;  // 125分對應50高度
    public float heightPerPoint = 50f / 125f;  // 每 1 分的高度
    public float maxBarHeight = 240f;  // 長條最大高度

    private string baseUrl = "https://feyndora-api.onrender.com";

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LoadUserDataFromPrefs();  // ✅ 只讀取本地資料，避免多餘 API 呼叫
    }

    /// <summary>
    /// 讓 APIManager 呼叫這個方法來刷新 Profile 頁面
    /// </summary>
    public void RefreshProfile()
    {
        if (!gameObject.activeInHierarchy) return; // 🔹 避免 Coroutine 啟動失敗
        StartCoroutine(RefreshAll());
    }

    /// <summary>
    /// 讀取 PlayerPrefs 的資料來更新 UI
    /// </summary>
    public void LoadUserDataFromPrefs()
    {
        int avatarId = PlayerPrefs.GetInt("AvatarID", 1);
        string username = PlayerPrefs.GetString("Username", "未知用戶");
        string email = PlayerPrefs.GetString("UserEmail", "未綁定 Email");
        int totalPoints = PlayerPrefs.GetInt("TotalPoints", 0);
        int coins = PlayerPrefs.GetInt("Coins", 0);
        int diamonds = PlayerPrefs.GetInt("Diamonds", 0);

        avatarImage.sprite = avatarManager.GetAvatarSprite(avatarId);
        usernameText.text = username;
        emailText.text = email;
        totalPointsText.text = totalPoints.ToString();
        coinsText.text = coins.ToString();
        diamondsText.text = diamonds.ToString();
    }

    /// <summary>
    /// 一次刷新：用戶資料、課程數量、以及最近 7 天每日積分（用來更新長條圖）
    /// </summary>
    IEnumerator RefreshAll()
    {
        yield return StartCoroutine(GetUserProfile());
        yield return StartCoroutine(GetCoursesCount());
        yield return StartCoroutine(GetWeeklyPoints());
    }

    /// <summary>
    /// 從 /user/{user_id} 取得用戶資料並更新 UI
    /// </summary>
    IEnumerator GetUserProfile()
    {
        int userID = PlayerPrefs.GetInt("UserID", 0);
        if (userID == 0)
        {
            Debug.LogError("❌ 未登入，無法取得用戶資料");
            yield break;
        }

        string url = $"{baseUrl}/user/{userID}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                UserData user = JsonUtility.FromJson<UserData>(req.downloadHandler.text);

                // 更新 UI 元件
                avatarImage.sprite = avatarManager.GetAvatarSprite(user.avatar_id);
                usernameText.text = user.username;
                emailText.text = user.email;
                totalPointsText.text = user.total_learning_points.ToString();
                coinsText.text = user.coins.ToString();
                diamondsText.text = user.diamonds.ToString();

                // 更新本地資料
                PlayerPrefs.SetString("Username", user.username);
                PlayerPrefs.SetString("UserEmail", user.email);
                PlayerPrefs.SetInt("TotalPoints", user.total_learning_points);
                PlayerPrefs.SetInt("Coins", user.coins);
                PlayerPrefs.SetInt("Diamonds", user.diamonds);
                PlayerPrefs.SetInt("AvatarID", user.avatar_id);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError("❌ 取得用戶資料失敗: " + req.error);
            }
        }
    }

    /// <summary>
    /// 從 /courses_count/{user_id} 取得用戶的課程數量並更新 UI
    /// </summary>
    IEnumerator GetCoursesCount()
    {
        int userID = PlayerPrefs.GetInt("UserID", 0);
        if (userID == 0)
        {
            yield break;
        }

        string url = $"{baseUrl}/courses_count/{userID}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                CoursesCountResponse resp = JsonUtility.FromJson<CoursesCountResponse>(req.downloadHandler.text);
                if (coursesCountText != null)
                {
                    coursesCountText.text = resp.courses_count.ToString();
                }
            }
            else
            {
                Debug.LogError("❌ 取得課程數量失敗: " + req.error);
            }
        }
    }

    /// <summary>
    /// 從 /weekly_points/{user_id} 取得最近 7 天的每日積分，更新長條圖
    /// </summary>
    IEnumerator GetWeeklyPoints()
    {
        int userID = PlayerPrefs.GetInt("UserID", 0);
        if (userID == 0) yield break;

        string url = $"{baseUrl}/weekly_points/{userID}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                WeeklyPointsResponse resp = JsonUtility.FromJson<WeeklyPointsResponse>(req.downloadHandler.text);

                // 確保取得 7 天的數據，不足者補 0
                List<int> points = resp.weekly_points;
                while (points.Count < 7)
                {
                    points.Add(0);
                }

                // 設定高度換算公式 (50 高度 = 125 分數)
                float heightPerPoint = 50f / 125f;  // 1 分數的高度
                float maxBarHeight = 240f;  // 長條最大高度

                // 更新每個長條的高度
                for (int i = 0; i < barImages.Length && i < points.Count; i++)
                {
                    float newHeight = points[i] * heightPerPoint;  // 計算新高度
                    newHeight = Mathf.Min(newHeight, maxBarHeight);  // 限制最大高度為 240

                    RectTransform rt = barImages[i].rectTransform;
                    rt.sizeDelta = new Vector2(rt.sizeDelta.x, newHeight);

                    Debug.Log($"📊 Bar {i}: Points = {points[i]}, NewHeight = {newHeight}");
                }
            }
            else
            {
                Debug.LogError("❌ 取得最近7天分數失敗: " + req.error);
            }
        }
    }

    // --- 資料結構 ---
    [System.Serializable]
    public class UserData
    {
        public int user_id;
        public string username;
        public string email;
        public int total_learning_points;
        public int coins;
        public int diamonds;
        public int avatar_id;
    }

    [System.Serializable]
    public class CoursesCountResponse
    {
        public int courses_count;
    }

    [System.Serializable]
    public class WeeklyPointsResponse
    {
        public List<int> weekly_points;
    }
}
