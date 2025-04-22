using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

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

    [Header("重點回顧")]
    public TMP_Text reviewText;    // PartTwo 下的 reviewText

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
        public string[] good_points;      // 做得好的點
        public string[] improvement_points; // 需要加強的點
    }

    private void UpdateReviewUI(CourseReviewData data)
    {
        // 更新評分
        accuracySlider.value = data.accuracy_score;
        understandingSlider.value = data.understanding_score;
        expressionSlider.value = data.expression_score;
        interactionSlider.value = data.interaction_score;

        // 更新評分文字
        accuracyText.text = $"{data.accuracy_score}%";
        understandingText.text = $"{data.understanding_score}%";
        expressionText.text = $"{data.expression_score}%";
        interactionText.text = $"{data.interaction_score}%";

        // 更新評語
        studentFeedbackTexts[0].text = data.student1_feedback;
        studentFeedbackTexts[1].text = data.student2_feedback;
        studentFeedbackTexts[2].text = data.student3_feedback;

        // 更新積分
        pointsText.text = $"+{data.earned_points}";

        // 更新重點回顧
        StringBuilder reviewContent = new StringBuilder();

        // 添加做得好的點
        if (data.good_points != null && data.good_points.Length > 0)
        {
            reviewContent.AppendLine("<color=#4CAF50>✓ 做得好的點：</color>");
            foreach (var point in data.good_points)
            {
                reviewContent.AppendLine($"<color=#4CAF50>✓ {point}</color>");
            }
            reviewContent.AppendLine();
        }

        // 添加需要加強的點
        if (data.improvement_points != null && data.improvement_points.Length > 0)
        {
            reviewContent.AppendLine("<color=#FF9800>! 需要加強的點：</color>");
            foreach (var point in data.improvement_points)
            {
                reviewContent.AppendLine($"<color=#FF9800>! {point}</color>");
            }
        }

        // 更新文字內容
        reviewText.text = reviewContent.ToString();

        // 計算文字所需的高度
        float textHeight = reviewText.preferredHeight;

        // 獲取所有需要的組件
        RectTransform imageRect = reviewText.transform.parent.GetComponent<RectTransform>();
        RectTransform partTwoRect = imageRect.transform.parent.GetComponent<RectTransform>();
        LayoutElement partTwoLayout = partTwoRect.GetComponent<LayoutElement>();

        if (partTwoLayout == null)
        {
            partTwoLayout = partTwoRect.gameObject.AddComponent<LayoutElement>();
        }

        if (imageRect != null && partTwoRect != null)
        {
            // 設置固定的間距值
            float topPadding = 40f;      // Image 與 PartTwo 頂部的間距
            float bottomPadding = 40f;    // Image 與 PartTwo 底部的間距
            float textPadding = 60f;      // 文字與 Image 邊緣的間距
            float titleHeight = 60f;      // 標題預留的高度
            
            // 1. 計算 Image 需要的高度（加上標題空間）
            float imageHeight = titleHeight + textHeight + (textPadding * 2);
            
            // 2. 計算 PartTwo 需要的總高度
            float partTwoHeight = imageHeight + topPadding + bottomPadding;
            
            // 3. 設置 Image 的大小
            imageRect.sizeDelta = new Vector2(imageRect.sizeDelta.x, imageHeight);
            
            // 4. 使用 Layout Element 來控制 PartTwo 的高度
            partTwoLayout.preferredWidth = partTwoRect.sizeDelta.x;  // 保持原有寬度
            partTwoLayout.preferredHeight = partTwoHeight;
            
            // 5. 確保 Text 在 Image 內部正確位置（從標題下方開始）
            reviewText.rectTransform.anchoredPosition = new Vector2(
                reviewText.rectTransform.anchoredPosition.x,
                -(titleHeight + textPadding)  // 從標題下方開始
            );
        }
    }
}
