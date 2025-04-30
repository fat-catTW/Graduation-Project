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

        // 1. 获取最新进度和stage
        string currentStageUrl = $"https://feyndora-api.onrender.com/current_stage/{userId}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(currentStageUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"获取课程进度失败: {request.error}");
                yield break;
            }

            var response = JsonUtility.FromJson<CurrentStageResponse>(request.downloadHandler.text);
            
            if (!response.hasReadyCourse)
            {
                Debug.LogError("没有找到进行中的课程");
                yield break;
            }

            // 2. 准备上传数据
            var updateData = new ProgressUpdateData
            {
                course_id = courseId,
                progress = response.progress,
                progress_one_to_one = response.progress_one_to_one,
                progress_classroom = response.progress_classroom,
                current_stage = response.current_stage
            };

            string jsonData = JsonUtility.ToJson(updateData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

            // 3. 更新进度到数据库
            using (UnityWebRequest updateRequest = new UnityWebRequest(apiUrl, "POST"))
            {
                updateRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                updateRequest.downloadHandler = new DownloadHandlerBuffer();
                updateRequest.SetRequestHeader("Content-Type", "application/json");

                yield return updateRequest.SendWebRequest();

                if (updateRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"更新课程进度失败: {updateRequest.error}");
                }
                else
                {
                    Debug.Log($"课程进度更新成功 - 总体进度: {response.progress}%, 一对一: {response.progress_one_to_one}%, 课堂: {response.progress_classroom}%, 阶段: {response.current_stage}");
                }
            }
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
    }

    [System.Serializable]
    class CurrentStageResponse
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
