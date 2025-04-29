using UnityEngine;
using System;

public class TeacherManager : MonoBehaviour
{
    private const int SANTA_TEACHER_ID = 5;  // 聖誕老公公的ID改為5

    public static TeacherManager Instance; // 單例模式

    // 添加事件
    public event Action OnTeacherChanged;

    [System.Serializable]
    public class TeacherModel
    {
        public string teacherName;
        public GameObject teacherObject;  // 場景中已有的老師物件
        [Tooltip("只有聖誕老公公需要設置麋鹿物件")]
        public GameObject companionObject;  // 場景中已有的麋鹿物件
        [Tooltip("ElevenLabs的voice ID")]
        public string voiceId;  // 每個老師的 ElevenLabs voice ID
    }

    [Header("老師物件設置")]
    [Tooltip("確保陣列順序與teacher_card_id對應（第一個是ID=1，第二個是ID=2，依此類推）\n聖誕老公公請放在第五個位置（ID=5）")]
    public TeacherModel[] teacherModels;

    private GameObject currentTeacher;  // 当前显示的老师
    private GameObject currentCompanion;  // 当前显示的麋鹿
    private TeacherModel currentTeacherModel; // 當前的老師模型數據

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 初始化时隱藏所有老師和麋鹿
        HideAllTeachers();

        // 从 PersistentDataManager 获取当前老师 ID
        if (PersistentDataManager.Instance != null)
        {
            int teacherId = PersistentDataManager.Instance.TeacherCardId;
            Debug.Log($"[TeacherManager] 從 PersistentDataManager 獲取到的 teacherId: {teacherId}");
            
            // 檢查 teacherId 是否有效
            if (teacherId <= 0 || teacherId > teacherModels.Length)
            {
                Debug.LogError($"[TeacherManager] 無效的 teacherId: {teacherId}，使用默認值 1");
                teacherId = 1;  // 使用默認值
            }
            
            ShowTeacher(teacherId);
        }
        else
        {
            Debug.LogError("[TeacherManager] PersistentDataManager.Instance 為 null！");
        }
    }

    private void HideAllTeachers()
    {
        foreach (var teacher in teacherModels)
        {
            if (teacher.teacherObject != null)
                teacher.teacherObject.SetActive(false);
            if (teacher.companionObject != null)
                teacher.companionObject.SetActive(false);
        }
    }

    public void ShowTeacher(int teacherId)
    {
        // 先隱藏所有老師
        HideAllTeachers();

        // 确保 teacherId 在有效范围内
        if (teacherId <= 0 || teacherId > teacherModels.Length)
        {
            Debug.LogError($"[TeacherManager] 無效的老師ID: {teacherId}");
            return;
        }

        // 获取对应的老师（数组索引比 ID 小 1）
        TeacherModel teacher = teacherModels[teacherId - 1];
        currentTeacherModel = teacher; // 保存當前老師模型數據

        // 顯示選中的老師
        if (teacher.teacherObject != null)
        {
            teacher.teacherObject.SetActive(true);
            currentTeacher = teacher.teacherObject;
            Debug.Log($"[TeacherManager] 顯示老師: {teacher.teacherName}");

            // 如果是聖誕老公公，同時顯示麋鹿
            if (teacherId == SANTA_TEACHER_ID && teacher.companionObject != null)
            {
                teacher.companionObject.SetActive(true);
                currentCompanion = teacher.companionObject;
                Debug.Log("[TeacherManager] 顯示聖誕老公公的麋鹿夥伴");
            }

            // 觸發老師變更事件
            OnTeacherChanged?.Invoke();
        }
        else
        {
            Debug.LogError($"[TeacherManager] 老師 {teacher.teacherName} 的物件未設置！");
        }
    }

    // 獲取當前老師的 voice ID
    public string GetCurrentVoiceId()
    {
        if (currentTeacherModel != null && !string.IsNullOrEmpty(currentTeacherModel.voiceId))
        {
            return currentTeacherModel.voiceId;
        }
        Debug.LogWarning("[TeacherManager] 當前沒有設置voice ID，使用默認值");
        return "default";  // 如果沒有設置，返回默認值
    }

    // 可以添加其他方法来控制老师的动画、交互等
}
