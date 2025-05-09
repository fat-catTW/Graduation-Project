using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class ExitPanelController : MonoBehaviour
{
    public GameObject exitPanel;  // UI上的ExitPanel物件
    public Button confirmButton;
    public Button cancelButton;

    private string apiUrl = "https://feyndora-api.onrender.com/update_progress";  // 修正API網址
    private string getProgressUrl = "https://feyndora-api.onrender.com/get_latest_progress/";

    private void Start()
    {
        exitPanel.SetActive(false);  // 一開始先隱藏

        confirmButton.onClick.AddListener(OnConfirmExit);
        cancelButton.onClick.AddListener(() => exitPanel.SetActive(false));  // 取消就關掉面板
    }

    /// <summary>
    /// 顯示離開確認面板
    /// </summary>
    public void ShowExitPanel()
    {
        exitPanel.SetActive(true);
    }

    /// <summary>
    /// 確認離開並更新進度
    /// </summary>
    void OnConfirmExit()
    {
        StartCoroutine(FetchLatestProgressAndUpdate());
    }

    IEnumerator FetchLatestProgressAndUpdate()
    {
        int userId = PersistentDataManager.Instance.UserId;
        int courseId = PersistentDataManager.Instance.CurrentCourseId;
        
        // 使用 current_stage API 取得最新進度
        using (UnityWebRequest request = UnityWebRequest.Get($"https://feyndora-api.onrender.com/current_stage/{userId}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                CurrentStageData stageData = JsonUtility.FromJson<CurrentStageData>(jsonResponse);

                if (stageData.hasReadyCourse)
                {
                    // 直接使用計算好的進度來更新
                    var updateData = new ProgressUpdateData
                    {
                        course_id = courseId,
                        progress = stageData.progress,
                        progress_one_to_one = stageData.progress_one_to_one,
                        progress_classroom = stageData.progress_classroom,
                        current_stage = stageData.current_stage
                    };

                    string jsonData = JsonUtility.ToJson(updateData);
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

                    using (UnityWebRequest updateRequest = new UnityWebRequest(apiUrl, "POST"))
                    {
                        updateRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                        updateRequest.downloadHandler = new DownloadHandlerBuffer();
                        updateRequest.SetRequestHeader("Content-Type", "application/json");

                        yield return updateRequest.SendWebRequest();

                        if (updateRequest.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogError($"❌ 更新課程進度和 VR 狀態失敗: {updateRequest.error}");
                        }
                        else
                        {
                            Debug.Log("✅ 課程進度和 VR 狀態更新成功");
                        }
                    }
                }
            }
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }

    /// <summary>
    /// 舊的進度計算保留但不再用（你可以刪掉）
    /// </summary>
    [System.Obsolete]
    float CalculateCurrentStageProgress()
    {
        int completedChapters = PersistentDataManager.Instance.CompletedChapters;
        int totalChapters = PersistentDataManager.Instance.TotalChapters;

        if (totalChapters <= 0) return 0f;
        float progress = (completedChapters / (float)totalChapters) * 100f;
        return Mathf.Min(progress, 99.9f);
    }

    /// <summary>
    /// 上傳進度用的資料結構
    /// </summary>
    [System.Serializable]
    class ProgressUpdateData
    {
        public int course_id;
        public float progress;
        public float progress_one_to_one;
        public float progress_classroom;
        public string current_stage;
    }

    [System.Serializable]
    class CurrentStageData
    {
        public bool hasReadyCourse;
        public int course_id;
        public string course_name;
        public string current_stage;
        public float progress;
        public float progress_one_to_one;
        public float progress_classroom;
        public int teacher_card_id;
    }
}
