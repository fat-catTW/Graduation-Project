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
        if (string.IsNullOrEmpty(query))
        {
            yield return StartCoroutine(LoadCourses());
            yield break;
        }

        int userID = PlayerPrefs.GetInt("UserID");
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/search_courses/" + userID + "?query=" + UnityWebRequest.EscapeURL(query)))
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
                Debug.LogError("❌ 搜尋失敗：" + request.downloadHandler.text);
            }
        }
    }

    // **✅ 更新課程 UI**
    void UpdateCourseUI(List<Course> courses)
    {
        // **🧹 清空舊課程**
        ClearCourses();

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
            recordButton.Find("CourseName").GetComponent<TMP_Text>().text = course.course_name;
            recordButton.Find("CreatedAt").GetComponent<TMP_Text>().text = course.created_at;

            // **✅ 修正進度條**
            Transform progressBarContainer = recordButton.Find("ProgressBar");
            TMP_Text progressText = recordButton.Find("ProgressNum")?.GetComponent<TMP_Text>();

            if (progressBarContainer != null)
            {
                Slider progressBar = progressBarContainer.GetComponent<Slider>();
                if (progressBar != null)
                {
                    progressBar.value = course.progress / 100f;  // ✅ 設定 Slider
                }
                else
                {
                    Debug.LogError("⚠ `ProgressBar` 存在，但不是 `Slider`，請檢查 Prefab 設置！");
                }
            }
            else
            {
                Debug.LogError("⚠️ `ProgressBar` 找不到，請檢查 Unity 層級結構！");
            }

            if (progressText != null)
            {
                progressText.text = course.progress.ToString("0") + "%";  // ✅ 設定百分比
            }
            else
            {
                Debug.LogError("⚠️ `ProgressNum` 找不到，請檢查 Unity 層級結構！");
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
            else
            {
                Debug.LogError("⚠️ `Image` 找不到，請檢查 Unity 層級結構！");
            }

            // **確保按鈕存在**
            Button favoriteButton = courseItem.transform.Find("FavoriteButton")?.GetComponent<Button>();
            Button deleteButton = courseItem.transform.Find("DeleteButton")?.GetComponent<Button>();

            if (favoriteButton != null)
            {
                favoriteButton.onClick.AddListener(() => StartCoroutine(ToggleFavorite(course.course_id)));
            }
            else
            {
                Debug.LogError("⚠️ 找不到 FavoriteButton，請檢查 Unity 層級結構！");
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => StartCoroutine(DeleteCourse(course.course_id, courseItem)));
            }
            else
            {
                Debug.LogError("⚠️ 找不到 DeleteButton，請檢查 Unity 層級結構！");
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
    }

    [System.Serializable]
    public class CourseListResponse
    {
        public List<Course> courses;
    }
}
