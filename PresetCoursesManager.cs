using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CourseImageMapping
{
    public string courseName;   // 課程名稱，必須與 CourseData 中的 courseName 相符
    public Sprite courseSprite; // 在 Inspector 指定的圖片
}

public class PresetCoursesManager : MonoBehaviour
{
    public static PresetCoursesManager Instance;

    [Header("推薦課程區")]
    public GameObject[] recommendedCourseObjects;
    // 推薦區的 ScrollRect（請拖入帶有 ScrollRect 組件的整個 ScrollView）
    public ScrollRect recommendedScrollRect;
    private Dictionary<string, Toggle> recommendedToggles = new Dictionary<string, Toggle>();

    [Header("收藏課程區")]
    public Transform savedCoursesContainer;
    public GameObject savedCoursePrefab;
    private Dictionary<string, GameObject> savedCourseObjects = new Dictionary<string, GameObject>();

    [Header("課程 Panels（手動拖入）")]
    public GameObject[] coursePages;  // 請手動拖入 CoursePage_1, CoursePage_2, CoursePage_3
    public GameObject courseListPage; // 課程列表頁

    [Header("搜尋功能")]
    public TMP_InputField searchInput;
    public Button searchButton;

    [Header("課程圖片對應")]
    public CourseImageMapping[] courseImageMappings;

    private int userId;
    private Dictionary<string, CourseData> allCourses = new Dictionary<string, CourseData>();

    // 用於防止重複呼叫
    private bool isUpdatingToggles = false;
    private HashSet<string> processingCourses = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 全域物件，不會隨場景切換而銷毀
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 初次進入時取得當前的 userId
        userId = PlayerPrefs.GetInt("UserID", 0);

        // 初始化推薦課程的 Toggle
        foreach (GameObject courseObj in recommendedCourseObjects)
        {
            CourseData courseData = courseObj.GetComponent<CourseData>();
            if (courseData == null) continue;

            string courseName = courseData.courseName;
            Toggle toggle = courseObj.GetComponentInChildren<Toggle>();

            if (toggle != null)
            {
                recommendedToggles[courseName] = toggle;
                toggle.onValueChanged.AddListener((isOn) => OnRecommendedToggleChanged(courseName, isOn));
            }

            allCourses[courseName] = courseData;
        }

        // 綁定搜尋按鈕及輸入框事件
        if (searchButton != null)
            searchButton.onClick.AddListener(SearchCourses);
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(delegate { SearchCourses(); });

        LoadSavedCourses();
    }

    /// <summary>
    /// 登入時呼叫，重置內部資料並重新載入收藏課程
    /// </summary>
    public void ReinitializeCourses()
    {
        Debug.Log("🔄 [PresetCoursesManager] 重新初始化課程");

        // 更新 userId 以符合新登入的使用者
        userId = PlayerPrefs.GetInt("UserID", 0);

        // 清除舊的 UI 與內部資料（包括重置滑動位置）
        ClearUI();
        StopAllCoroutines();

        if (!gameObject.activeSelf)
        {
            Debug.Log("🔄 [PresetCoursesManager] 物件為 inactive，重新啟用...");
            gameObject.SetActive(true);
            StartCoroutine(WaitAndLoadSavedCourses());
        }
        else
        {
            LoadSavedCourses();
        }
    }

    private IEnumerator WaitAndLoadSavedCourses()
    {
        // 等待一個 frame 確保 GameObject 完全激活
        yield return null;
        LoadSavedCourses();
    }

    /// <summary>
    /// 當推薦區的收藏 Toggle 改變時
    /// </summary>
    private void OnRecommendedToggleChanged(string courseName, bool isOn)
    {
        if (processingCourses.Contains(courseName) || isUpdatingToggles)
        {
            Debug.Log($"⚠️ 跳過重複處理課程：{courseName}");
            return;
        }

        processingCourses.Add(courseName);
        try
        {
            if (isOn)
            {
                SaveCourse(courseName);
            }
            else
            {
                if (!savedCourseObjects.ContainsKey(courseName))
                {
                    Debug.LogWarning($"⚠️ 課程 {courseName} 已經被移除，忽略重複請求");
                    return;
                }

                RemoveSavedCourse(courseName);
                UpdateSavedToggle(courseName, false);
            }
        }
        finally
        {
            processingCourses.Remove(courseName);
        }
    }

    /// <summary>
    /// 當收藏區的收藏 Toggle 改變時
    /// </summary>
    private void OnSavedToggleChanged(string courseName, bool isOn)
    {
        if (processingCourses.Contains(courseName) || isUpdatingToggles)
        {
            Debug.Log($"⚠️ 跳過重複處理課程：{courseName}");
            return;
        }

        processingCourses.Add(courseName);
        try
        {
            if (!isOn)
            {
                if (!savedCourseObjects.ContainsKey(courseName))
                {
                    Debug.LogWarning($"⚠️ 課程 {courseName} 已經被移除，忽略重複請求");
                    return;
                }

                RemoveSavedCourse(courseName);
                UpdateRecommendedToggle(courseName, false);
            }
        }
        finally
        {
            processingCourses.Remove(courseName);
        }
    }

    /// <summary>
    /// 更新推薦區 Toggle 的狀態
    /// </summary>
    private void UpdateRecommendedToggle(string courseName, bool isOn)
    {
        if (!recommendedToggles.ContainsKey(courseName))
            return;

        isUpdatingToggles = true;
        try
        {
            recommendedToggles[courseName].isOn = isOn;
        }
        finally
        {
            isUpdatingToggles = false;
        }
    }

    /// <summary>
    /// 更新收藏區 Toggle 的狀態
    /// </summary>
    private void UpdateSavedToggle(string courseName, bool isOn)
    {
        if (!savedCourseObjects.ContainsKey(courseName))
            return;

        Toggle savedToggle = savedCourseObjects[courseName].GetComponentInChildren<Toggle>();
        if (savedToggle == null)
            return;

        isUpdatingToggles = true;
        try
        {
            savedToggle.isOn = isOn;
        }
        finally
        {
            isUpdatingToggles = false;
        }
    }

    /// <summary>
    /// 讀取用戶收藏的課程
    /// </summary>
    public void LoadSavedCourses()
    {
        StartCoroutine(APIManager.Instance.GetSavedCourses(userId, (savedCourses) =>
        {
            isUpdatingToggles = true;
            try
            {
                foreach (string courseName in savedCourses)
                {
                    if (!savedCourseObjects.ContainsKey(courseName))
                    {
                        AddSavedCourse(courseName);
                    }
                    if (recommendedToggles.ContainsKey(courseName))
                        recommendedToggles[courseName].isOn = true;
                }
            }
            finally
            {
                isUpdatingToggles = false;
            }
        }));
    }

    /// <summary>
    /// 收藏課程
    /// </summary>
    private void SaveCourse(string courseName)
    {
        if (!savedCourseObjects.ContainsKey(courseName))
        {
            AddSavedCourse(courseName);
        }
        StartCoroutine(APIManager.Instance.SaveCourse(userId, courseName));
    }

    /// <summary>
    /// 新增收藏區的課程 UI
    /// </summary>
    private void AddSavedCourse(string courseName)
    {
        GameObject savedCourse = Instantiate(savedCoursePrefab, savedCoursesContainer);
        CourseData courseData = savedCourse.GetComponent<CourseData>();

        if (allCourses.ContainsKey(courseName))
        {
            CourseData originalData = allCourses[courseName];
            courseData.courseId = originalData.courseId;
            courseData.courseName = originalData.courseName; // 複製推薦區的名稱
            courseData.category = originalData.category;     // 複製推薦區的類別
            courseData.courseImage = originalData.courseImage; // 複製推薦區的圖片（預設值）

            if (courseData.courseId >= 1 && courseData.courseId <= coursePages.Length)
            {
                courseData.coursePage = coursePages[courseData.courseId - 1];
            }
        }

        // 更新顯示課程名稱的文字（假設使用第一個 TMP_Text 顯示）
        TMP_Text courseText = savedCourse.GetComponentInChildren<TMP_Text>();
        if (courseText != null)
        {
            courseText.text = courseData != null ? courseData.courseName : "未知課程";
        }

        // 更新顯示類別的文字（假設在 prefab 中有名為 "CategoryText" 的子物件）
        TMP_Text categoryText = savedCourse.transform.Find("CategoryText")?.GetComponent<TMP_Text>();
        if (categoryText != null)
        {
            categoryText.text = courseData != null ? courseData.category : "未知類別";
        }

        // 【僅修改圖片更新部分】更新圖片：先嘗試使用 Inspector 指定的映射圖片，否則用原本的圖片
        UnityEngine.UI.Image courseImageUI = savedCourse.transform.Find("CourseImage")?.GetComponent<UnityEngine.UI.Image>();
        if (courseImageUI != null)
        {
            Sprite finalSprite = courseData.courseImage; // 預設使用推薦區的圖片
            if (courseImageMappings != null)
            {
                foreach (var mapping in courseImageMappings)
                {
                    if (mapping.courseName.Equals(courseData.courseName))
                    {
                        finalSprite = mapping.courseSprite;
                        break;
                    }
                }
            }
            courseImageUI.sprite = finalSprite;
        }

        Toggle toggle = savedCourse.GetComponentInChildren<Toggle>();
        if (toggle != null)
        {
            toggle.isOn = true;
            toggle.onValueChanged.AddListener((isOn) => OnSavedToggleChanged(courseName, isOn));
        }

        Button goButton = savedCourse.GetComponentInChildren<Button>();
        if (goButton != null)
        {
            goButton.onClick.AddListener(courseData.OpenCourseDetail);
        }

        savedCourseObjects[courseName] = savedCourse;
    }

    /// <summary>
    /// 移除收藏課程
    /// </summary>
    private void RemoveSavedCourse(string courseName)
    {
        if (savedCourseObjects.ContainsKey(courseName))
        {
            Destroy(savedCourseObjects[courseName]);
            savedCourseObjects.Remove(courseName);
        }
        StartCoroutine(APIManager.Instance.RemoveSavedCourse(userId, courseName));
    }

    /// <summary>
    /// 顯示指定的課程頁面
    /// </summary>
    public void ShowCoursePage(GameObject targetPage)
    {
        if (targetPage.activeSelf)
        {
            Debug.Log($"⚠️ 課程頁面 {targetPage.name} 已經處於開啟狀態，跳過重複切換");
            return;
        }

        Debug.Log($"🎯 切換到課程頁面：{targetPage.name}");
        foreach (GameObject panel in coursePages)
        {
            if (panel != null)
                panel.SetActive(false);
        }
        targetPage.SetActive(true);
    }

    /// <summary>
    /// 返回課程列表頁
    /// </summary>
    public void ReturnToCourseList()
    {
        Debug.Log("↩️ 返回課程列表");
        foreach (GameObject panel in coursePages)
        {
            if (panel != null)
                panel.SetActive(false);
        }
        courseListPage.SetActive(true);
    }

    /// <summary>
    /// 搜尋課程（依照名稱或分類匹配）
    /// </summary>
    public void SearchCourses()
    {
        string keyword = searchInput.text.ToLower().Trim();
        Debug.Log($"🔍 搜尋課程關鍵字：{keyword}");

        if (string.IsNullOrEmpty(keyword))
        {
            foreach (GameObject courseObj in recommendedCourseObjects)
            {
                courseObj.SetActive(true);
            }
            Debug.Log("🔄 搜尋欄位為空，顯示所有課程");
            return;
        }

        bool hasMatch = false;
        foreach (GameObject courseObj in recommendedCourseObjects)
        {
            CourseData courseData = courseObj.GetComponent<CourseData>();
            if (courseData == null)
                continue;

            string name = courseData.courseName.ToLower();
            string category = courseData.category.ToLower();
            bool match = name.Contains(keyword) || category.Contains(keyword);
            courseObj.SetActive(match);
            if (match)
                hasMatch = true;
        }

        if (!hasMatch)
        {
            Debug.LogWarning($"⚠️ 沒有找到匹配的課程：{keyword}");
        }
    }

    /// <summary>
    /// 登出時清除 UI 與內部資料（呼叫此方法確保狀態重置）
    /// </summary>
    public void ClearUI()
    {
        Debug.Log("🔄 [PresetCoursesManager] 正在清除 UI（登出）...");

        // 刪除收藏課程的所有 UI 物件
        if (savedCoursesContainer != null)
        {
            foreach (Transform child in savedCoursesContainer)
            {
                Destroy(child.gameObject);
            }
        }
        savedCourseObjects.Clear();

        // 重置推薦區 Toggle 的狀態
        foreach (var kv in recommendedToggles)
        {
            kv.Value.isOn = false;
        }

        // 重置推薦區的 ScrollRect 滑動位置（回到最左邊）
        if (recommendedScrollRect != null)
        {
            recommendedScrollRect.horizontalNormalizedPosition = 0;
        }

        // 清空其他內部狀態
        processingCourses.Clear();
        isUpdatingToggles = false;

        Debug.Log("✅ [PresetCoursesManager] UI 已清除完畢！");
    }
}
