using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance;

    // 用戶基本資料
    public int UserId;
    public string Username;

    // 課程資料
    public int CurrentCourseId;
    public string CurrentCourseName;
    public string CurrentStage;

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
            DontDestroyOnLoad(gameObject);  // 跨場景保存
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 存放登入資訊（從VRLogin來）
    /// </summary>
    public void SetUserData(int userId, string username)
    {
        UserId = userId;
        Username = username;
    }

    /// <summary>
    /// 存放課程資料（從current_stage來）
    /// </summary>
    public void SetCourseData(int courseId, string courseName, string stage, float oneToOneProgress, float classroomProgress)
    {
        CurrentCourseId = courseId;
        CurrentCourseName = courseName;
        CurrentStage = stage;
        OneToOneProgress = oneToOneProgress;
        ClassroomProgress = classroomProgress;
    }
}
