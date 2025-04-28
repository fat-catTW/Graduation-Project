using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance;

    // 用户数据
    public int UserId;
    public string Username;

    // 课程数据
    public int CurrentCourseId;
    public string CurrentCourseName;
    public string CurrentStage;
    public int TeacherCardId;  // 教师卡片ID

    // 进度数据
    public float OneToOneProgress;
    public float ClassroomProgress;

    // 章节数据
    public int CompletedChapters;
    public int TotalChapters;

    private void Awake()
    {
        Debug.Log("[PersistentDataManager] Awake 開始");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 跨場景保留
            Debug.Log("[PersistentDataManager] 創建新實例");
        }
        else
        {
            Debug.Log("[PersistentDataManager] 實例已存在，銷毀當前實例");
            Destroy(gameObject);
        }
        Debug.Log("[PersistentDataManager] Awake 結束");
    }

    /// <summary>
    /// 儲存登入資料（從VRLogin傳來）
    /// </summary>
    public void SetUserData(int userId, string username)
    {
        Debug.Log("[PersistentDataManager] SetUserData 開始");
        Debug.Log($"[PersistentDataManager] 設置 UserId: {userId}");
        Debug.Log($"[PersistentDataManager] 設置 Username: {username}");
        UserId = userId;
        Username = username;
        Debug.Log("[PersistentDataManager] SetUserData 結束");
    }

    /// <summary>
    /// 儲存課程資料（從current_stage傳來）
    /// </summary>
    public void SetCourseData(int courseId, string courseName, string stage, float oneToOneProgress, float classroomProgress, int teacherCardId)
    {
        Debug.Log("[PersistentDataManager] SetCourseData 開始");
        Debug.Log($"[PersistentDataManager] 設置 CourseId: {courseId}");
        Debug.Log($"[PersistentDataManager] 設置 CourseName: {courseName}");
        Debug.Log($"[PersistentDataManager] 設置 Stage: {stage}");
        Debug.Log($"[PersistentDataManager] 設置 OneToOneProgress: {oneToOneProgress}");
        Debug.Log($"[PersistentDataManager] 設置 ClassroomProgress: {classroomProgress}");
        Debug.Log($"[PersistentDataManager] 設置 TeacherCardId: {teacherCardId}");
        
        CurrentCourseId = courseId;
        CurrentCourseName = courseName;
        CurrentStage = stage;
        OneToOneProgress = oneToOneProgress;
        ClassroomProgress = classroomProgress;
        TeacherCardId = teacherCardId;
        
        Debug.Log($"[PersistentDataManager] 設置後的 TeacherCardId: {TeacherCardId}");
        Debug.Log("[PersistentDataManager] SetCourseData 結束");
    }
}
