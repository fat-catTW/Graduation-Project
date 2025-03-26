using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class VRLessonManager : MonoBehaviour
{
    [Header("評分系統")]
    public Slider accuracySlider;
    public Slider understandingSlider;
    public Slider expressionSlider;
    public Slider interactionSlider;

    [Header("評分文字")]
    public TMP_Text accuracyText;
    public TMP_Text understandingText;
    public TMP_Text expressionText;
    public TMP_Text interactionText;

    [Header("評語區塊")]
    public TMP_Text teacherCommentText;
    public TMP_Text[] studentFeedbackTexts;
    public TMP_Text pointsText;

    [Header("額外 UI 元件")]
    public GameObject continuePanel;  // **繼續上課確認視窗**
    public GameObject reviewPage;     // **複習頁面**
    public TMP_Text reviewCourseName; // **複習頁面 - 課程名稱**
    public TMP_Text reviewCreatedAt;  // **複習頁面 - 課程創建時間**
    public Button confirmContinueButton; // **確認繼續按鈕**

    private string baseUrl = "https://feyndora-api.onrender.com"; // Flask API 伺服器
    private int selectedCourseId;  // **目前選中的課程 ID**
    private string selectedCourseName; // **目前選中的課程名稱**
    private string selectedCourseCreatedAt; // **目前選中的課程建立時間**

    void Start()
    {
        if (confirmContinueButton != null)
        {
            confirmContinueButton.onClick.AddListener(() => StartCoroutine(ConfirmContinueCourse()));
        }

        // 初始化所有滑块
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        // 设置滑块的最小值和最大值
        accuracySlider.minValue = 0;
        accuracySlider.maxValue = 100;
        accuracySlider.wholeNumbers = true;
        accuracySlider.interactable = false;  // 设置为不可交互

        understandingSlider.minValue = 0;
        understandingSlider.maxValue = 100;
        understandingSlider.wholeNumbers = true;
        understandingSlider.interactable = false;

        expressionSlider.minValue = 0;
        expressionSlider.maxValue = 100;
        expressionSlider.wholeNumbers = true;
        expressionSlider.interactable = false;

        interactionSlider.minValue = 0;
        interactionSlider.maxValue = 100;
        interactionSlider.wholeNumbers = true;
        interactionSlider.interactable = false;
    }

    // **🔹 當課程被點擊時**
    public void OnCourseClicked(int courseId, string courseName, string createdAt, float progress)
    {
        selectedCourseId = courseId;
        selectedCourseName = courseName;
        selectedCourseCreatedAt = createdAt;

        if (progress >= 100)
        {
            // **✅ 進度 100%，顯示複習頁**
            reviewCourseName.text = courseName;
            
            // 解析時間字串並格式化為 yyyy/MM/dd HH:mm:ss
            if (System.DateTime.TryParse(createdAt, out System.DateTime dateTime))
            {
                reviewCreatedAt.text = dateTime.ToString("yyyy/MM/dd HH:mm:ss");
            }
            else
            {
                reviewCreatedAt.text = createdAt;  // 如果解析失敗，顯示原始時間
            }
            
            reviewPage.SetActive(true);
            StartCoroutine(LoadCourseReview(courseId));  // 加载课程评价数据
        }
        else
        {
            // **✅ 進度未達 100%，顯示確認視窗**
            continuePanel.SetActive(true);
        }
    }

    // **🚀 確認繼續上課**
    IEnumerator ConfirmContinueCourse()
    {
        if (selectedCourseId == 0)
        {
            Debug.LogError("❌ 沒有選擇課程，無法繼續！");
            yield break;
        }

        string url = $"{baseUrl}/continue_course";
        string jsonData = $"{{\"course_id\": {selectedCourseId}}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ 成功通知後端，開始課程: {selectedCourseName}");
                continuePanel.SetActive(false);
            }
            else
            {
                Debug.LogError($"❌ 失敗: {request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator LoadCourseReview(int courseId)
    {
        string url = $"{baseUrl}/course_review/{courseId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"API Response: {responseText}");

                CourseReviewData reviewData = JsonUtility.FromJson<CourseReviewData>(responseText);
                UpdateReviewUI(reviewData);
            }
            else
            {
                Debug.LogError("无法载入课程回顾：" + request.error);
            }
        }
    }

    [System.Serializable]
    private class CourseReviewData
    {
        public int accuracy_score;
        public int understanding_score;
        public int expression_score;
        public int interaction_score;
        public string teacher_comment;
        public string student1_feedback;
        public string student2_feedback;
        public string student3_feedback;
        public int earned_points;
    }

    private void UpdateReviewUI(CourseReviewData data)
    {
        // 更新评分滑块和对应的文字（只显示数字）
        accuracySlider.value = data.accuracy_score;
        accuracyText.text = data.accuracy_score.ToString();

        understandingSlider.value = data.understanding_score;
        understandingText.text = data.understanding_score.ToString();

        expressionSlider.value = data.expression_score;
        expressionText.text = data.expression_score.ToString();

        interactionSlider.value = data.interaction_score;
        interactionText.text = data.interaction_score.ToString();

        // 更新導師評語
        if (teacherCommentText != null && !string.IsNullOrEmpty(data.teacher_comment))
        {
            teacherCommentText.text = data.teacher_comment;
        }

        // 更新AI同學回饋
        if (studentFeedbackTexts != null && studentFeedbackTexts.Length >= 3)
        {
            if (!string.IsNullOrEmpty(data.student1_feedback))
                studentFeedbackTexts[0].text = data.student1_feedback;
            if (!string.IsNullOrEmpty(data.student2_feedback))
                studentFeedbackTexts[1].text = data.student2_feedback;
            if (!string.IsNullOrEmpty(data.student3_feedback))
                studentFeedbackTexts[2].text = data.student3_feedback;
        }
        else
        {
            Debug.LogError("studentFeedbackTexts not properly set up");
        }

        // 更新学习积分
        pointsText.text = $"{data.earned_points}";
    }
}
