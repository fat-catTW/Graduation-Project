using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;

public class WeeklyTaskManager : MonoBehaviour
{
    public static WeeklyTaskManager Instance; // Singleton

    [Header("🔔 主頁 UI")]
    public GameObject bellRedDot;
    public GameObject mainPageRedDot;

    [Header("🔔 主頁 任務相關")]
    public GameObject[] mainPageTaskButtons;
    public Slider[] mainPageProgressBars;
    public TextMeshProUGUI[] mainPageProgressTexts;
    public GameObject[] mainPageGotImages;

    [Header("👤 個人頁 任務相關")]
    public GameObject[] profilePageTaskButtons;
    public Slider[] profilePageProgressBars;
    public TextMeshProUGUI[] profilePageProgressTexts;
    public GameObject[] profilePageGotImages;
    public GameObject profilePageRedDot;

    private string baseUrl = "https://feyndora-api.onrender.com";
    private int userId;
    private bool isLoggedOut = false; // **新增變數，避免登出後繼續發送請求**
    private bool[] canClaimReward = { false, false, false };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        userId = PlayerPrefs.GetInt("UserID", 0);
        if (userId > 0)
        {
            Debug.Log($"[WeeklyTaskManager] UserID: {userId}");
            StartCoroutine(GetWeeklyTasks());
        }
    }

    /// <summary>
    /// 重新登入後更新任務，並重新抓取資料
    /// </summary>
    public void ReloadTasksOnLogin(int newUserId)
    {
        userId = newUserId;
        PlayerPrefs.SetInt("UserID", newUserId);
        isLoggedOut = false; // **取消登出狀態**
        Debug.Log($"🔄 [WeeklyTaskManager] 用戶重新登入，更新每週任務... 新 UserID: {newUserId}");

        if (userId > 0)
        {
            StartCoroutine(GetWeeklyTasks());
        }
    }

    /// <summary>
    /// 登出時清除 UI，不發送 API 請求
    /// </summary>
    public void ClearUIOnLogout()
    {
        Debug.Log("🚪 [WeeklyTaskManager] 登出，清除 UI...");
        isLoggedOut = true; // **防止登出後還執行 API**
        userId = 0; // **重設 userId**
        PlayerPrefs.DeleteKey("UserID"); // **確保用戶 ID 被移除**

        for (int i = 0; i < mainPageProgressBars.Length; i++)
        {
            mainPageProgressBars[i].value = 0;
            profilePageProgressBars[i].value = 0;
            mainPageGotImages[i].SetActive(false);
            profilePageGotImages[i].SetActive(false);
            canClaimReward[i] = false;
            mainPageProgressTexts[i].text = "0%";
            profilePageProgressTexts[i].text = "0%";

            mainPageTaskButtons[i].GetComponent<Button>().interactable = false;
            profilePageTaskButtons[i].GetComponent<Button>().interactable = false;
        }

        bellRedDot.SetActive(false);
        if (mainPageRedDot != null) mainPageRedDot.SetActive(false);
        if (profilePageRedDot != null) profilePageRedDot.SetActive(false);
    }

    /// <summary>
    /// 刷新任務，確保不在登出狀態
    /// </summary>
    public void RefreshTasks()
    {
        if (isLoggedOut || userId <= 0)
        {
            Debug.Log("🔹 登出狀態，跳過每週任務刷新");
            return;
        }

        Debug.Log("🔄 [WeeklyTaskManager] 正在刷新每週任務...");
        StartCoroutine(GetWeeklyTasks());
    }

    IEnumerator GetWeeklyTasks()
    {
        if (isLoggedOut || userId <= 0)
        {
            Debug.Log("🔹 已登出或 userId 無效，跳過 API 請求");
            yield break;
        }

        string url = $"{baseUrl}/weekly_tasks/{userId}";
        Debug.Log($"📡 [WeeklyTaskManager] 正在獲取每週任務: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"✅ [WeeklyTaskManager] API 響應: {responseText}");

                try
                {
                    JObject jsonResponse = JObject.Parse(responseText);
                    JArray tasks = (JArray)jsonResponse["tasks"];
                    Debug.Log($"✅ 解析成功！獲取 {tasks.Count} 個任務");

                    bool hasClaimableTask = false;
                    for (int j = 0; j < canClaimReward.Length; j++)
                    {
                        canClaimReward[j] = false;
                    }

                    foreach (JToken task in tasks)
                    {
                        int taskId = task["task_id"].Value<int>();
                        int index = taskId - 1;
                        int progress = task["progress"].Value<int>();
                        int target = task["target"].Value<int>();
                        bool isClaimed = task["is_claimed"].ToObject<int>() == 1;

                        float progressRatio = (float)progress / target;
                        int progressPercentage = Mathf.RoundToInt(progressRatio * 100);
                        progressPercentage = Mathf.Min(100, progressPercentage);

                        Debug.Log($"📊 任務 {taskId}: {progress}/{target} | {progressPercentage}% | 已領取: {isClaimed}");

                        if (index < mainPageProgressBars.Length)
                        {
                            mainPageProgressBars[index].value = progressRatio;
                            mainPageProgressTexts[index].text = $"{progressPercentage}%";
                            mainPageGotImages[index].SetActive(isClaimed);
                            mainPageTaskButtons[index].GetComponent<Button>().interactable = !isClaimed;
                        }

                        if (index < profilePageProgressBars.Length)
                        {
                            profilePageProgressBars[index].value = progressRatio;
                            profilePageProgressTexts[index].text = $"{progressPercentage}%";
                            profilePageGotImages[index].SetActive(isClaimed);
                            profilePageTaskButtons[index].GetComponent<Button>().interactable = !isClaimed;
                        }

                        canClaimReward[index] = (progress >= target) && !isClaimed;
                        if (canClaimReward[index])
                        {
                            hasClaimableTask = true;
                        }
                    }

                    bellRedDot.SetActive(hasClaimableTask);
                    if (mainPageRedDot != null) mainPageRedDot.SetActive(hasClaimableTask);
                    if (profilePageRedDot != null) profilePageRedDot.SetActive(hasClaimableTask);
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    Debug.LogError($"❌ JSON 解析錯誤: {e.Message}");
                    Debug.LogError($"📢 API 回應內容: {responseText}");
                }
            }
            else
            {
                Debug.LogError($"❌ API 請求失敗: {request.result}");
                Debug.LogError($"🔴 HTTP 錯誤: {request.responseCode}");
                Debug.LogError($"📢 伺服器回應: {request.downloadHandler.text}");
            }
        }
    }
     // 領取任務獎勵
    public void ClaimTaskReward(int taskId)
    {
        Debug.Log($"🎁 [WeeklyTaskManager] 嘗試領取任務 {taskId + 1} 的獎勵...");
        if (taskId < 0 || taskId >= canClaimReward.Length)
        {
            Debug.LogError($"❌ 無效的 taskId: {taskId}");
            return;
        }
        if (!canClaimReward[taskId])
        {
            Debug.LogWarning($"❌ 任務 {taskId + 1} 無法領取（可能尚未達標或已領取）");
            return;
        }
        StartCoroutine(ClaimWeeklyTask(taskId));
    }

    IEnumerator ClaimWeeklyTask(int taskId)
    {
        int apiTaskId = taskId + 1; // API 的 task_id 從 1 開始
        string url = $"{baseUrl}/claim_weekly_task";
        string jsonData = $"{{\"user_id\": {userId}, \"task_id\": {apiTaskId}}}";

        Debug.Log($"📡 [WeeklyTaskManager] 發送領取任務 {apiTaskId} 的請求: {url}");
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"✅ [WeeklyTaskManager] API 響應: {responseText}");

                try
                {
                    JObject jsonResponse = JObject.Parse(responseText);
                    int rewardCoins = jsonResponse["reward_coins"]?.Value<int>() ?? 0;

                    // 取得當前金幣數量
                    int currentCoins = PlayerPrefs.GetInt("Coins", 0);
                    int updatedCoins = currentCoins + rewardCoins;

                    // **更新 PlayerPrefs**
                    PlayerPrefs.SetInt("Coins", updatedCoins);
                    PlayerPrefs.Save();

                    // **即時更新 Profile 頁的金幣顯示**
                    if (ProfileManager.Instance != null)
                    {
                        ProfileManager.Instance.coinsText.text = updatedCoins.ToString();
                        Debug.Log($"💰 [WeeklyTaskManager] 更新個人頁金幣：{updatedCoins}");
                    }

                    // 觸發用戶資料更新
                    StartCoroutine(APIManager.Instance.FetchUserData());

                    // 領取獎勵後刷新任務狀態
                    StartCoroutine(GetWeeklyTasks());
                }
                catch (Newtonsoft.Json.JsonReaderException e)
                {
                    Debug.LogError($"❌ JSON 解析錯誤: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"❌ 領取獎勵失敗: {request.downloadHandler.text}");
            }
        }
    }
}
