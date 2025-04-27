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
        StartCoroutine(UpdateCourseProgress());
    }

    IEnumerator UpdateCourseProgress()
    {
        int userId = PersistentDataManager.Instance.UserId;
        int courseId = PersistentDataManager.Instance.CurrentCourseId;
        string currentStage = PersistentDataManager.Instance.CurrentStage;

        int completedChapters = PersistentDataManager.Instance.CompletedChapters;
        int totalChapters = PersistentDataManager.Instance.TotalChapters;

        float progressOneToOne = PersistentDataManager.Instance.OneToOneProgress;
        float progressClassroom = PersistentDataManager.Instance.ClassroomProgress;

        // ✔️ 進度改用ProgressCalculator計算
        if (currentStage == "one_to_one")
        {
            progressOneToOne = ProgressCalculator.CalculateOneToOneProgress(completedChapters, totalChapters);
        }
        else if (currentStage == "classroom")
        {
            progressClassroom = ProgressCalculator.CalculateClassroomProgress(completedChapters, totalChapters);
        }

        // ✔️ 整體進度
        float overallProgress = ProgressCalculator.CalculateOverallProgress(progressOneToOne, progressClassroom);

        // ✅ 組成上傳資料
        var progressData = new ProgressUpdateData
        {
            course_id = courseId,
            progress = overallProgress,
            progress_one_to_one = progressOneToOne,
            progress_classroom = progressClassroom,
            current_stage = currentStage
        };

        string jsonData = JsonUtility.ToJson(progressData);
        Debug.Log($"準備更新課程進度，同時將 is_vr_ready 設為 0: {jsonData}");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 更新課程進度和 VR 狀態失敗: {request.error}");
            }
            else
            {
                Debug.Log("✅ 課程進度和 VR 狀態更新成功");
            }
        }

        // ✅ 更新完進度，回LoginScene
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
}
