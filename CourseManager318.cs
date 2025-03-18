using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class CourseManager : MonoBehaviour
{
    [Header("課程 UI")]
    public Transform courseListParent; // ScrollView 的 Content
    public GameObject coursePrefab; // **完整的 RecordModule**
    public TMP_InputField searchInput; // 搜尋框
    public Button searchButton; // 搜尋按鈕
    public Button refreshButton; // 手動刷新按鈕

    [Header("課程圖標")]
    public Sprite pdfIconSprite;
    public Sprite textIconSprite;
    public Sprite videoIconSprite;
    public Sprite defaultIconSprite;

    private string baseUrl = "https://feyndora-api.onrender.com"; // Flask API 伺服器

    void Start()
    {
        searchButton.onClick.AddListener(() => StartCoroutine(SearchCourses()));
        refreshButton.onClick.AddListener(() => StartCoroutine(LoadCourses()));

        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener((value) => {
                if (string.IsNullOrEmpty(value.Trim()))
                {
                    StartCoroutine(LoadCourses());
                }
            });
        }

        StartCoroutine(LoadCourses());
    }

    // **🔹 獲取用戶課程**
    public IEnumerator LoadCourses()
    {
        int userID = PlayerPrefs.GetInt("UserID");

        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/courses/" + userID))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                CourseListResponse jsonResponse = JsonUtility.FromJson<CourseListResponse>("{\"courses\":" + request.downloadHandler.text + "}");
                UpdateCourseUI(jsonResponse.courses);
            }
            else
            {
                Debug.LogError("❌ 無法獲取課程：" + request.downloadHandler.text);
            }
        }
    }

    // **🔎 搜尋課程**
    IEnumerator SearchCourses()
    {
        string query = searchInput.text.Trim();
        
        // 如果搜索框為空，直接載入所有課程
        if (string.IsNullOrEmpty(query))
        {
            Debug.Log("🔍 搜索框為空，顯示所有課程");
            yield return StartCoroutine(LoadCourses());
            yield break;
        }

        Debug.Log($"🔍 開始搜索課程：{query}");
        int userID = PlayerPrefs.GetInt("UserID");
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/search_courses/" + userID + "?query=" + UnityWebRequest.EscapeURL(query)))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                CourseListResponse jsonResponse = JsonUtility.FromJson<CourseListResponse>("{\"courses\":" + request.downloadHandler.text + "}");
                UpdateCourseUI(jsonResponse.courses);
                Debug.Log($"✅ 搜索完成，找到 {jsonResponse.courses.Count} 個課程");
            }
            else
            {
                Debug.LogError("❌ 搜尋失敗：" + request.downloadHandler.text);
            }
        }
    }

    // **✅ 更新課程 UI**
    void UpdateCourseUI(List<Course> courses)
    {
        // **🧹 清空舊課程**
        ClearCourses();

        // **🔍 確保 VRLessonManager 存在**
        VRLessonManager vrLessonManager = Object.FindFirstObjectByType<VRLessonManager>();
        if (vrLessonManager == null)
        {
            Debug.LogError("❌ `VRLessonManager` 未找到，請確保 `VRLessonManager` 存在於場景中！");
            return; // **直接返回，避免進入迴圈後報錯**
        }

        foreach (Course course in courses)
        {
            GameObject courseItem = Instantiate(coursePrefab, courseListParent);
            courseItem.SetActive(true);

            Transform recordButton = courseItem.transform.Find("RecordButton");
            if (recordButton == null)
            {
                Debug.LogError("❌ 找不到 RecordButton，請確認 Prefab 結構！");
                continue;
            }

            // **設置課程名稱 & 創建時間**
            TMP_Text courseNameText = recordButton.Find("CourseName")?.GetComponent<TMP_Text>();
            TMP_Text createdAtText = recordButton.Find("CreatedAt")?.GetComponent<TMP_Text>();

            if (courseNameText != null) courseNameText.text = course.course_name;
            if (createdAtText != null) createdAtText.text = course.created_at;

            // **✅ 設定進度條**
            Slider progressBar = recordButton.Find("ProgressBar")?.GetComponent<Slider>();
            TMP_Text progressText = recordButton.Find("ProgressNum")?.GetComponent<TMP_Text>();

            if (progressBar != null)
            {
                progressBar.value = course.progress / 100f;
            }

            if (progressText != null)
            {
                progressText.text = course.progress.ToString("0") + "%";
            }

            // **🎨 設置對應的課程圖標**
            Image courseIcon = recordButton.Find("Image")?.GetComponent<Image>();
            if (courseIcon != null)
            {
                switch (course.file_type)
                {
                    case "pdf":
                        courseIcon.sprite = pdfIconSprite;
                        break;
                    case "text":
                        courseIcon.sprite = textIconSprite;
                        break;
                    case "video":
                        courseIcon.sprite = videoIconSprite;
                        break;
                    default:
                        courseIcon.sprite = defaultIconSprite;
                        break;
                }
            }

            // **確保按鈕存在**
            Button favoriteButton = courseItem.transform.Find("FavoriteButton")?.GetComponent<Button>();
            Button deleteButton = courseItem.transform.Find("DeleteButton")?.GetComponent<Button>();
            Button courseClickButton = recordButton.GetComponent<Button>(); // **點擊課程按鈕**

            if (favoriteButton != null)
            {
                favoriteButton.onClick.AddListener(() => StartCoroutine(ToggleFavorite(course.course_id)));
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => StartCoroutine(DeleteCourse(course.course_id, courseItem)));
            }

            // **📌 點擊課程按鈕來觸發 VRLessonManager**
            if (courseClickButton != null)
            {
                courseClickButton.onClick.AddListener(() =>
                    vrLessonManager.OnCourseClicked(course.course_id, course.course_name, course.created_at, course.progress)
                );
            }
            else
            {
                Debug.LogError("⚠️ 找不到課程的 RecordButton，請檢查 Prefab 結構！");
            }
        }
    }

    // **清除課程 UI**
    public void ClearCourses()
    {
        foreach (Transform child in courseListParent)
        {
            Destroy(child.gameObject);
        }
    }

    // **⭐ 切換收藏狀態**
    IEnumerator ToggleFavorite(int courseID)
    {
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(baseUrl + "/toggle_favorite/" + courseID, ""))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("⭐ 課程收藏狀態變更成功");
                StartCoroutine(LoadCourses());
            }
            else
            {
                Debug.LogError("❌ 收藏變更失敗：" + request.downloadHandler.text);
            }
        }
    }

    // **🗑️ 刪除課程**
    IEnumerator DeleteCourse(int courseID, GameObject courseItem)
    {
        using (UnityWebRequest request = UnityWebRequest.Delete(baseUrl + "/delete_course/" + courseID))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("🗑️ 課程刪除成功");
                Destroy(courseItem);
            }
            else
            {
                Debug.LogError("❌ 刪除失敗：" + request.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    public class Course
    {
        public int course_id;
        public string course_name;
        public string created_at;
        public float progress;
        public string file_type; // ✅ 加入 file_type
        public bool is_vr_ready; // ✅ 新增這個欄位
    }

    [System.Serializable]
    public class CourseListResponse
    {
        public List<Course> courses;
    }
}
