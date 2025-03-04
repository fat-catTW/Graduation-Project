using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class ExitPanelController : MonoBehaviour
{
    public GameObject exitPanel;  // UI上面的ExitPanel物件
    public Button confirmButton;
    public Button cancelButton;

    private string apiUrl = "https://feyndora-api.onrender.com/update_progress";

    private void Start()
    {
        exitPanel.SetActive(false);  // 一開始先隱藏

        confirmButton.onClick.AddListener(OnConfirmExit);
        cancelButton.onClick.AddListener(() => exitPanel.SetActive(false));  // 取消就關掉面板
    }

    public void ShowExitPanel()
    {
        exitPanel.SetActive(true);  // 顯示離開確認面板
    }

    void OnConfirmExit()
    {
        StartCoroutine(UpdateCourseProgress());  // 先更新進度再跳LoginScene
    }

    IEnumerator UpdateCourseProgress()
    {
        int userId = PersistentDataManager.Instance.UserId;
        int courseId = PersistentDataManager.Instance.CurrentCourseId;
        string currentStage = PersistentDataManager.Instance.CurrentStage;

        float progressOneToOne = PersistentDataManager.Instance.OneToOneProgress;
        float progressClassroom = PersistentDataManager.Instance.ClassroomProgress;

        float overallProgress = (progressOneToOne + progressClassroom) / 2f;

        // 根據現在是在哪個場景，更新對應的progress
        if (currentStage == "one_to_one")
        {
            progressOneToOne = CalculateCurrentStageProgress();
        }
        else if (currentStage == "classroom")
        {
            progressClassroom = CalculateCurrentStageProgress();
        }

        // 重新計算整體進度（50%+50%）
        overallProgress = (progressOneToOne + progressClassroom) / 2f;

        // 組成上傳資料
        var progressData = new ProgressUpdateData
        {
            course_id = courseId,
            progress = overallProgress,
            progress_one_to_one = progressOneToOne,
            progress_classroom = progressClassroom,
            current_stage = currentStage
        };

        string jsonData = JsonUtility.ToJson(progressData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 更新課程進度失敗: {request.error}");
            }
            else
            {
                Debug.Log("✅ 課程進度更新成功");
            }
        }

        // 更新完後，回到LoginScene
        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }

    /// <summary>
    /// 計算「當前這個場景」的進度 (這裡要根據你課程實際結構來)
    /// </summary>
    float CalculateCurrentStageProgress()
    {
        int completedChapters = PersistentDataManager.Instance.CompletedChapters;
        int totalChapters = PersistentDataManager.Instance.TotalChapters;

        if (totalChapters <= 0) return 0f;

        float progress = (completedChapters / (float)totalChapters) * 100f;

        // 保險起見，最多顯示99.9%避免誤判
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
}
