using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public string message;
    public int user_id;
    public string username;
}

[Serializable]
public class CurrentStageResponse
{
    public bool hasReadyCourse;
    public int course_id;
    public string course_name;
    public string current_stage;
    public float progress;
    public float progress_one_to_one;
    public float progress_classroom;
    public int teacher_card_id;
}

public class VRLogin : MonoBehaviour
{
    [Header("登入介面")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public GameObject loginPanel;
    public Button loginButton;

    [Header("開始上課介面")]
    public GameObject startCoursePanel;
    public TMP_Text startCourseUsernameText;
    public TMP_Text startCourseNameText;
    public Button startCourseButton;

    [Header("繼續上課介面")]
    public GameObject continueCoursePanel;
    public TMP_Text continueCourseUsernameText;
    public TMP_Text continueCourseNameText;
    public Button continueCourseButton;

    [Header("沒有課程介面")]
    public GameObject noClassPanel;
    public TMP_Text noClassUsernameText;
    public Button noClassConfirmButton;

    private string flaskBaseUrl = "https://feyndora-api.onrender.com";

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClick);
        startCourseButton.onClick.AddListener(OnStartCourse);
        continueCourseButton.onClick.AddListener(OnContinueCourse);
        noClassConfirmButton.onClick.AddListener(OnNoClassConfirmClicked);

        ShowPanel(loginPanel);
    }

    void ShowPanel(GameObject panelToShow)
    {
        loginPanel.SetActive(false);
        startCoursePanel.SetActive(false);
        continueCoursePanel.SetActive(false);
        noClassPanel.SetActive(false);

        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    public void OnLoginButtonClick()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("❌ 請輸入帳號與密碼！");
            return;
        }

        StartCoroutine(LoginCoroutine(email, password));
    }

    IEnumerator LoginCoroutine(string email, string password)
    {
        LoginRequest loginRequest = new LoginRequest { email = email, password = password };
        string jsonData = JsonUtility.ToJson(loginRequest);

        using (UnityWebRequest request = new UnityWebRequest($"{flaskBaseUrl}/login", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 登入失敗: {request.error}");
                yield break;
            }

            LoginResponse loginData = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            if (loginData == null || loginData.user_id == 0)
            {
                Debug.LogError("❌ 登入成功但解析失敗！");
                yield break;
            }

            // 存到PersistentDataManager
            PersistentDataManager.Instance.SetUserData(loginData.user_id, loginData.username);

            Debug.Log($"✅ 登入成功！用戶：{loginData.username}");

            ShowPanel(null);  // 隱藏登入面板

            StartCoroutine(FetchCurrentStage(loginData.user_id, loginData.username));
        }
    }

    IEnumerator FetchCurrentStage(int user_id, string username)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{flaskBaseUrl}/current_stage/{user_id}"))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 取得current_stage失敗: {request.error}");
                yield break;
            }

            CurrentStageResponse stageData = JsonUtility.FromJson<CurrentStageResponse>(request.downloadHandler.text);
            Debug.Log($"從 API 獲取到的 teacher_card_id: {stageData.teacher_card_id}");

            if (stageData.hasReadyCourse)
            {
                PersistentDataManager.Instance.SetCourseData(
                    stageData.course_id,
                    stageData.course_name,
                    stageData.current_stage,
                    stageData.progress_one_to_one,
                    stageData.progress_classroom,
                    stageData.teacher_card_id
                );
                Debug.Log($"設置到 PersistentDataManager 的 TeacherCardId: {PersistentDataManager.Instance.TeacherCardId}");

                if (stageData.progress == 0)
                {
                    ShowStartCoursePanel(username, stageData.course_name);
                }
                else
                {
                    ShowContinueCoursePanel(username, stageData.course_name);
                }
            }
            else
            {
                ShowNoCourseMessage(username);
            }
        }
    }

    void ShowStartCoursePanel(string username, string courseName)
    {
        ShowPanel(startCoursePanel);
        startCourseUsernameText.text = $"你好！{username}";
        startCourseNameText.text = $"您即將進入課程\n【{courseName}】";
    }

    void ShowContinueCoursePanel(string username, string courseName)
    {
        ShowPanel(continueCoursePanel);
        continueCourseUsernameText.text = $"你好！{username}";
        continueCourseNameText.text = $"您將接續上次的課程\n【{courseName}】";
    }

    void ShowNoCourseMessage(string username)
    {
        ShowPanel(noClassPanel);
        noClassUsernameText.text = $"你好！{username}";
    }

    public void OnStartCourse()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("OneToOne");
    }

    public void OnContinueCourse()
    {
        if (PersistentDataManager.Instance.CurrentStage == "one_to_one")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OneToOne");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("OneToThree");
        }
    }

    public void OnNoClassConfirmClicked()
    {
        StartCoroutine(FetchCurrentStage(
            PersistentDataManager.Instance.UserId,
            PersistentDataManager.Instance.Username
        ));
    }
}
