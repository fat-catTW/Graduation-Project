using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class SettlementPanelController : MonoBehaviour
{
    public GameObject settlementPanel;  // UI上的SettlementPanel物件
    public Button finishButton;

    private string finishCourseUrl = "https://feyndora-api.onrender.com/finish_course";
    private string updateProgressUrl = "https://feyndora-api.onrender.com/update_progress";

    private void Start()
    {
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
        StartCoroutine(FinishCourseAndUpdate());
    }

    IEnumerator FinishCourseAndUpdate()
    {
        int userId = PersistentDataManager.Instance.UserId;
        int courseId = PersistentDataManager.Instance.CurrentCourseId;
        
        // 先呼叫 finish_course API 強制完成所有章節
        var finishData = new FinishCourseData
        {
            course_id = courseId
        };

        string jsonData = JsonUtility.ToJson(finishData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest finishRequest = new UnityWebRequest(finishCourseUrl, "POST"))
        {
            finishRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            finishRequest.downloadHandler = new DownloadHandlerBuffer();
            finishRequest.SetRequestHeader("Content-Type", "application/json");

            yield return finishRequest.SendWebRequest();

            if (finishRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 強制完成章節失敗: {finishRequest.error}");
                yield break;
            }
            else
            {
                Debug.Log("✅ 章節強制完成成功");
            }
        }

        // 然後更新課程進度
        using (UnityWebRequest request = UnityWebRequest.Get($"https://feyndora-api.onrender.com/current_stage/{userId}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                CurrentStageData stageData = JsonUtility.FromJson<CurrentStageData>(jsonResponse);

                if (stageData.hasReadyCourse)
                {
                    var updateData = new ProgressUpdateData
                    {
                        course_id = courseId,
                        progress = 100f,  // 強制設為 100%
                        progress_one_to_one = 100f,  // 強制設為 100%
                        progress_classroom = 100f,  // 強制設為 100%
                        current_stage = "completed",  // 強制設為 completed
                        is_vr_ready = 0  // 離開時設為 0
                    };

                    jsonData = JsonUtility.ToJson(updateData);
                    bodyRaw = Encoding.UTF8.GetBytes(jsonData);

                    using (UnityWebRequest updateRequest = new UnityWebRequest(updateProgressUrl, "POST"))
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
    class FinishCourseData
    {
        public int course_id;
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
