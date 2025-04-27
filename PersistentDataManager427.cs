using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance;

    // 用戶資料
    public int UserId;
    public string Username;

    // 課程資料
    public int CurrentCourseId;
    public string CurrentCourseName;
    public string CurrentStage;
    public int TeacherCardId;  // 新增：老師卡片ID

    // 進度資料
    public float OneToOneProgress;
    public float ClassroomProgress;

    // 章節統計
    public int CompletedChapters;
    public int TotalChapters;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 不銷毀物件
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 儲存登入資料（從VRLogin場景）
    /// </summary>
    public void SetUserData(int userId, string username)
    {
        UserId = userId;
        Username = username;
    }

    /// <summary>
    /// 儲存課程資料（從current_stage場景）
    /// </summary>
    public void SetCourseData(int courseId, string courseName, string stage, float oneToOneProgress, float classroomProgress, int teacherCardId)
    {
        CurrentCourseId = courseId;
        CurrentCourseName = courseName;
        CurrentStage = stage;
        OneToOneProgress = oneToOneProgress;
        ClassroomProgress = classroomProgress;
        TeacherCardId = teacherCardId;  // 新增：設置老師卡片ID
    }
}
