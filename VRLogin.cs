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

public class VRLogin : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public GameObject loginPanel;  // ✅ UI 登入面板
    public Button loginButton;     // ✅ 按鈕變數，程式碼中初始化

    private string flaskServerURL = "https://feyndora-api.onrender.com/login"; // 🔥 你的 Flask API 伺服器

    private void Start()
    {
        Debug.Log("🔹 VRLogin 初始化成功！");

        // ✅ 確保 UI 元件不為空
        if (emailInput == null || passwordInput == null || loginPanel == null)
        {
            Debug.LogError("❌ emailInput, passwordInput 或 loginPanel 未綁定！");
        }

        // ✅ 直接在程式碼內綁定按鈕事件
        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginButtonClick);
            Debug.Log("🔹 登入按鈕事件綁定成功！");
        }
        else
        {
            Debug.LogError("❌ loginButton 未綁定！");
        }
    }

    public void OnLoginButtonClick()
    {
        string email = emailInput.text.Trim();
        string password = passwordInput.text.Trim();

        Debug.Log($"🔹 嘗試登入 Email: {email}, Password: {new string('*', password.Length)}");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("❌ 請輸入帳號與密碼！");
            return;
        }

        StartCoroutine(LoginCoroutine(email, password));
    }

    IEnumerator LoginCoroutine(string email, string password)
    {
        // ✅ 建立 JSON 物件
        LoginRequest loginRequest = new LoginRequest { email = email, password = password };
        string jsonData = JsonUtility.ToJson(loginRequest);
        byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(jsonData);

        Debug.Log($"🔹 送出登入請求: {jsonData}");

        using (UnityWebRequest request = new UnityWebRequest(flaskServerURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ 登入失敗: {request.error}");
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log($"✅ 伺服器回應: {responseText}");

            try
            {
                LoginResponse loginData = JsonUtility.FromJson<LoginResponse>(responseText);
                
                if (loginData == null || loginData.user_id == 0)
                {
                    Debug.LogError("❌ JSON 解析失敗，請檢查 API 回應格式");
                    yield break;
                }

                Debug.Log($"✅ 登入成功！UserID: {loginData.user_id}, Username: {loginData.username}");

                // ✅ 存取登入資訊
                PlayerPrefs.SetInt("user_id", loginData.user_id);
                PlayerPrefs.SetString("username", loginData.username);
                PlayerPrefs.Save();

                // ✅ 隱藏登入面板
                if (loginPanel != null)
                {
                    loginPanel.SetActive(false);
                    Debug.Log("🔹 登入面板已隱藏！");
                }
                else
                {
                    Debug.LogWarning("⚠ loginPanel 為空，無法隱藏！");
                }

                // ✅ 進入 VR 課堂
                EnterClassroom();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ JSON 解析錯誤: {e.Message}");
                yield break;
            }
        }
    }

    private void EnterClassroom()
    {
        Debug.Log("🚀 進入 VR 課堂！");
        // 🔥 這裡你可以切換場景至 VR 課堂
        // UnityEngine.SceneManagement.SceneManager.LoadScene("VRClassroom");
    }
}
