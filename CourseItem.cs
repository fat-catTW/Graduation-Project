using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CourseItem : MonoBehaviour
{
    public TMP_Text courseNameText;  // 課程名稱
    public TMP_Text createdAtText;   // 課程建立時間
    public Slider progressSlider;    // 進度條
    private int courseId;
    private float courseProgress;
    private VRLessonManager vrLessonManager;

    // **🔹 設定課程資訊**
    public void Setup(int id, string name, string createdAt, float progress, VRLessonManager lessonManager)
    {
        courseId = id;
        courseNameText.text = name;
        createdAtText.text = string.IsNullOrEmpty(createdAt) ? "未知時間" : createdAt; // ✅ 防止 `null`
        courseProgress = progress;
        
        if (progressSlider != null)
        {
            progressSlider.value = progress / 100f;
        }
        else
        {
            Debug.LogError("⚠️ `progressSlider` 未設定，請確認 Prefab 裡有這個 UI 元件！");
        }

        vrLessonManager = lessonManager;
    }

    // **🔹 點擊課程時觸發**
    public void OnCourseClick()
    {
        if (vrLessonManager != null)
        {
            vrLessonManager.OnCourseClicked(courseId, courseNameText.text, createdAtText.text, courseProgress);
        }
        else
        {
            Debug.LogError("❌ VRLessonManager 未設置，請確保它被正確傳入！");
        }
    }
}
