using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class SettlementPanelController : MonoBehaviour
{
    public GameObject settlementPanel;  // UI上的SettlementPanel物件
    public Button finishButton;

    private string apiUrl = "https://feyndora-api.onrender.com/update_progress";

    private void Start()
    {
        settlementPanel.SetActive(false);  // 一開始先隱藏
        finishButton.onClick.AddListener(OnFinish);
    }

    /// <summary>
    /// 顯示結算面板
    /// </summary>
    public void ShowSettlementPanel()
    {
        settlementPanel.SetActive(true);
    }

    /// <summary>
    /// 確定離開並更新進度
    /// </summary>
    void OnFinish()
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
                        current_stage = stageData.current_stage,
                        is_vr_ready = 0  // 離開時設為 0
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
        public int is_vr_ready;
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
