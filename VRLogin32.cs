using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System;

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
}

public class VRLogin : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public GameObject loginPanel;

    public GameObject startCoursePanel;
    public GameObject continueCoursePanel;

    public Button loginButton;
    public Button startCourseButton;
    public Button continueCourseButton;

    // Inspector 直接綁定這4個Text
    public TMP_Text startCourseUsernameText;
    public TMP_Text startCourseNameText;
    public TMP_Text continueCourseUsernameText;
    public TMP_Text continueCourseNameText;

    private string flaskBaseUrl = "https://feyndora-api.onrender.com";

    private void Start()
    {
        loginButton.onClick.AddListener(OnLoginButtonClick);
        startCourseButton.onClick.AddListener(OnStartCourse);
        continueCourseButton.onClick.AddListener(OnContinueCourse);

        ShowPanel(loginPanel);  // 初始進來先顯示登入畫面
    }

    private void ShowPanel(GameObject panelToShow)
    {
        loginPanel.SetActive(false);
        startCoursePanel.SetActive(false);
        continueCoursePanel.SetActive(false);

        if (panelToShow != null) panelToShow.SetActive(true);
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

            PlayerPrefs.SetInt("user_id", loginData.user_id);
            PlayerPrefs.SetString("username", loginData.username);

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

            if (stageData.hasReadyCourse)
            {
                PlayerPrefs.SetInt("current_course_id", stageData.course_id);
                PlayerPrefs.SetString("current_course_name", stageData.course_name);
                PlayerPrefs.SetString("current_stage", stageData.current_stage);

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
                Debug.LogWarning($"⚠️ {username} 目前沒有ready的課程，請先到App預習。");
                ShowNoCourseMessage(username);
            }
        }
    }

    void ShowStartCoursePanel(string username, string courseName)
    {
        ShowPanel(startCoursePanel);

        if (startCourseUsernameText != null) startCourseUsernameText.text = $"你好！{username}";
        if (startCourseNameText != null) startCourseNameText.text = $"您即將進入課程\n【{courseName}】";
    }

    void ShowContinueCoursePanel(string username, string courseName)
    {
        ShowPanel(continueCoursePanel);

        if (continueCourseUsernameText != null) continueCourseUsernameText.text = $"你好！{username}";
        if (continueCourseNameText != null) continueCourseNameText.text = $"您將接續上次的課程\n【{courseName}】";
    }

    void ShowNoCourseMessage(string username)
    {
        Debug.LogWarning($"⚠️ {username} 目前沒有ready的課程，請先到App預習。");
        ShowPanel(loginPanel);  // 退回到登入面板或你想要的引導UI
    }

    public void OnStartCourse()
    {
        Debug.Log("🚀 開始上課，進入一對一導師場景");
        UnityEngine.SceneManagement.SceneManager.LoadScene("OneToOneScene");
    }

    public void OnContinueCourse()
    {
        string currentStage = PlayerPrefs.GetString("current_stage", "one_to_one");

        if (currentStage == "one_to_one")
        {
            Debug.Log("🚀 繼續上一對一課程");
            UnityEngine.SceneManagement.SceneManager.LoadScene("OneToOneScene");
        }
        else if (currentStage == "classroom")
        {
            Debug.Log("🚀 繼續上一對多課程");
            UnityEngine.SceneManagement.SceneManager.LoadScene("OneToThreeScene");
        }
        else
        {
            Debug.LogError($"❌ 不明的current_stage: {currentStage}");
        }
    }
}
