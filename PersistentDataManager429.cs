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
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 跨場景保留
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 儲存登入資料（從VRLogin傳來）
    /// </summary>
    public void SetUserData(int userId, string username)
    {
        UserId = userId;
        Username = username;
    }

    /// <summary>
    /// 儲存課程資料（從current_stage傳來）
    /// </summary>
    public void SetCourseData(int courseId, string courseName, string stage, float oneToOneProgress, float classroomProgress, int teacherCardId)
    {
        CurrentCourseId = courseId;
        CurrentCourseName = courseName;
        CurrentStage = stage;
        OneToOneProgress = oneToOneProgress;
        ClassroomProgress = classroomProgress;
        TeacherCardId = teacherCardId;
        Debug.Log($"[PersistentDataManager] 設置 TeacherCardId: {TeacherCardId}");
    }
}
