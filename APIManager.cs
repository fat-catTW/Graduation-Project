using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APIManager : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:8000";  // Flask 伺服器位址

    // 單例模式（確保只有一個API管理器實例）
    public static APIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 確保在場景切換時不會被銷毀
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 場景加載後立即請求用戶資料
        GetUserData(1);  // 這裡的 1 可以替換為你想要的用戶 ID
    }

    // 取得用戶資料的函式
    public void GetUserData(int userId)
    {
        StartCoroutine(GetUserRequest(userId));
    }

    IEnumerator GetUserRequest(int userId)
    {
        string url = $"{baseUrl}/get_user/{userId}";  // 組合 API URL
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("成功取得用戶資料：" + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("請求失敗：" + request.error);
            }
        }
    }
}