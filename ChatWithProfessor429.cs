using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;

public class chatWithProfessor : MonoBehaviour
{
    public TMP_Text blackBoardText;
    public TMP_InputField testInput;
    public fetchRenderToC fetcher; // 拖進 Inspector 或用 GetComponent 抓
    public AudioSource audioSource;  // 添加AudioSource組件引用
    public GameObject loadingAnimation; // 添加 Loading 動畫物件引用
    private AudioManager audioManager;  // 添加 AudioManager 引用

    [Header("Voice Settings")]
    private string currentVoice;  // 當前使用的聲音

    private string apiFetchUrl = "https://feynman-server.onrender.com/fetch";
    private string apiChatUrl = "https://feynman-server.onrender.com/chat";
    private string apiUdateProgressUrl = "https://feynman-server.onrender.com/update_chapter_progress";
    private string apiTtsUrl = "https://feynman-server.onrender.com/text-to-speech";
    private int UserId;
    private string Username;
    private int CurrentCourseId;
    private string CurrentCourseName;
    private string CurrentStage;
    private float OneToOneProgress;
    private float ClassroomProgress;
    private int CompletedChapters;
    private int TotalChapters;

    string assistant_id = "";
    string thread_id = "";
    private string embeddedPrompt = null;

    public GameObject parentObject;
    private GameObject[] childObjects;  //儲存每個目錄章節
    private int childCount;

    void Start()
    {
        Debug.Log("[chatWithProfessor] Start 開始");
        if (fetcher != null)
        {
            fetcher.OnChaptersFetched += HandleChaptersFetched;
            Debug.Log("[chatWithProfessor] 訂閱 fetcher.OnChaptersFetched 事件");
        }

        // 初始化語音設置
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("[chatWithProfessor] 創建新的 AudioSource 組件");
            }
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
        }

        // 獲取 AudioManager 並訂閱事件
        audioManager = GetComponent<AudioManager>();
        if (audioManager != null)
        {
            audioManager.OnRecordingStopped += HandleRecordingStopped;
            Debug.Log("[chatWithProfessor] 訂閱 audioManager.OnRecordingStopped 事件");
        }
        else
        {
            Debug.LogError("[chatWithProfessor] 找不到 AudioManager 組件！");
        }

        // 確保 loading 動畫一開始是隱藏的
        if (loadingAnimation != null)
        {
            loadingAnimation.SetActive(false);
        }

        // 獲取當前老師的 voice ID
        if (TeacherManager.Instance != null)
        {
            currentVoice = TeacherManager.Instance.GetCurrentVoiceId();
            Debug.Log($"[chatWithProfessor] 使用老師語音ID: {currentVoice}");
            
            // 訂閱 TeacherManager 的事件
            TeacherManager.Instance.OnTeacherChanged += UpdateVoiceId;
            Debug.Log("[chatWithProfessor] 訂閱 TeacherManager.OnTeacherChanged 事件");
        }
        else
        {
            Debug.LogError("[chatWithProfessor] 找不到 TeacherManager！");
            currentVoice = "default";
        }

        if (PersistentDataManager.Instance != null)
        {
            UserId = PersistentDataManager.Instance.UserId;
            Username = PersistentDataManager.Instance.Username;
            CurrentCourseId = PersistentDataManager.Instance.CurrentCourseId;
            CurrentCourseName = PersistentDataManager.Instance.CurrentCourseName;
            CurrentStage = PersistentDataManager.Instance.CurrentStage;
            OneToOneProgress = PersistentDataManager.Instance.OneToOneProgress;
            ClassroomProgress = PersistentDataManager.Instance.ClassroomProgress;
            CompletedChapters = PersistentDataManager.Instance.CompletedChapters;
            TotalChapters = PersistentDataManager.Instance.TotalChapters;

            Debug.Log($"[chatWithProfessor] 從 PersistentDataManager 獲取資料:");
            Debug.Log($"[chatWithProfessor] - UserId: {UserId}");
            Debug.Log($"[chatWithProfessor] - Username: {Username}");
            Debug.Log($"[chatWithProfessor] - CurrentCourseId: {CurrentCourseId}");
            Debug.Log($"[chatWithProfessor] - CurrentCourseName: {CurrentCourseName}");
            Debug.Log($"[chatWithProfessor] - CurrentStage: {CurrentStage}");
            Debug.Log($"[chatWithProfessor] - OneToOneProgress: {OneToOneProgress}");
            Debug.Log($"[chatWithProfessor] - ClassroomProgress: {ClassroomProgress}");
            Debug.Log($"[chatWithProfessor] - CompletedChapters: {CompletedChapters}");
            Debug.Log($"[chatWithProfessor] - TotalChapters: {TotalChapters}");

            StartCoroutine(getChatGPTIDs());
        }
        else
        {
            Debug.LogError("[chatWithProfessor] PersistentDataManager.Instance is null! 確保 PersistentDataManager 存在於場景中。");
        }
        Debug.Log("[chatWithProfessor] Start 結束");
    }

    void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame) // 檢測 Enter 鍵
        {
            Debug.Log("Sending a Message.");
            StartCoroutine(SendMessage());
        }
    }

    IEnumerator getChatGPTIDs()
    {
        Debug.Log("開始從資料庫載入資料...");

        string jsonData = JsonUtility.ToJson(new fetchRequest
        {
            action = "fetch_assistant_and_thread",
            course_id = CurrentCourseId,
            role = "teacher"
        });

        using (UnityWebRequest request = new UnityWebRequest(apiFetchUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<fetchResponse>(request.downloadHandler.text);
                assistant_id = response.assistant_id;
                thread_id = response.thread_id;
                Debug.Log("assistant_Id and thread_id fetched successfully");
                Debug.Log("assistant_Id: " + assistant_id);
                Debug.Log("thread_id: " + thread_id);
            }
            else
            {
                blackBoardText.text = "Error: " + request.error;
            }
        }
    }

    public IEnumerator SendMessage()
    {
        string jsonData = JsonUtility.ToJson(new messageRequest
        {
            action = "message",
            message = $"這是使用者的回答:{testInput.text}\n" + embeddedPrompt,
            assistant_id = assistant_id,
            thread_id = thread_id
        });
        Debug.Log($"這是使用者的回答:{testInput.text} \n" + embeddedPrompt);
        testInput.text = "";

        using (UnityWebRequest request = new UnityWebRequest(apiChatUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<messageResponse>(request.downloadHandler.text);
                    string jsonString = CleanJsonString(response.message);
                    Debug.Log($"原始 JSON 字符串: {jsonString}");

                    // 嘗試解析 JSON
                    responseJson reply = JsonUtility.FromJson<responseJson>(jsonString);
                    if (reply != null)
                    {
                        blackBoardText.text = reply.reply;
                        Debug.Log($"老師回覆內容: {reply.reply}");
                        Debug.Log($"目前進度: {reply.progress}");

                        // 收到回應後，立即進行文字轉語音
                        StartCoroutine(TextToSpeech(reply.reply));

                        progressUpdate(reply.progress);
                    }
                    else
                    {
                        Debug.LogError("無法解析 JSON 回應");
                        blackBoardText.text = "無法解析伺服器回應";
                        // 發生錯誤時隱藏 loading
                        if (loadingAnimation != null)
                        {
                            loadingAnimation.SetActive(false);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"JSON 解析錯誤: {e.Message}");
                    Debug.LogError($"原始回應: {request.downloadHandler.text}");
                    blackBoardText.text = "解析回應時發生錯誤";
                    // 發生錯誤時隱藏 loading
                    if (loadingAnimation != null)
                    {
                        loadingAnimation.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogError($"請求失敗: {request.error}");
                blackBoardText.text = "Error: " + request.error;
                // 發生錯誤時隱藏 loading
                if (loadingAnimation != null)
                {
                    loadingAnimation.SetActive(false);
                }
            }
        }
    }

    private IEnumerator TextToSpeech(string text)
    {
        Debug.Log("[chatWithProfessor] TextToSpeech 開始");
        Debug.Log($"[chatWithProfessor] 要轉換的文字: {text}");
        Debug.Log($"[chatWithProfessor] 使用的 voiceId: {currentVoice}");

        if (audioSource == null)
        {
            Debug.LogError("[chatWithProfessor] AudioSource is not assigned!");
            if (loadingAnimation != null)
            {
                loadingAnimation.SetActive(false);
            }
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new TtsRequest
        {
            text = text,
            voice_id = currentVoice
        });
        Debug.Log($"[chatWithProfessor] 準備發送TTS請求: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(apiTtsUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(apiTtsUrl, AudioType.MPEG);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        try
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                if (audioClip != null)
                {
                    Debug.Log($"[chatWithProfessor] 音頻接收成功，長度: {audioClip.length}秒");
                    audioSource.clip = audioClip;
                    audioSource.Play();

                    // 在成功接收並播放語音後才隱藏 loading
                    if (loadingAnimation != null)
                    {
                        loadingAnimation.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogError("[chatWithProfessor] 接收到的音頻為空");
                    if (loadingAnimation != null)
                    {
                        loadingAnimation.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogError($"[chatWithProfessor] TTS請求失敗: {request.error}");
                Debug.LogError($"[chatWithProfessor] 錯誤詳情: {request.downloadHandler.text}");
                if (loadingAnimation != null)
                {
                    loadingAnimation.SetActive(false);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[chatWithProfessor] TextToSpeech 錯誤: {e.Message}");
            Debug.LogError($"[chatWithProfessor] 錯誤堆疊: {e.StackTrace}");
            if (loadingAnimation != null)
            {
                loadingAnimation.SetActive(false);
            }
        }
        finally
        {
            request.Dispose();
        }
        Debug.Log("[chatWithProfessor] TextToSpeech 結束");
    }

    void HandleChaptersFetched(Chapter[] chapters)
    {
        int chapterLength = chapters.Length;
        setEmbeddPrompt(chapterLength + 1);

        embeddedPrompt = $"{chapterLength + 1}結束\n" + embeddedPrompt;

        for (; chapterLength > 0; chapterLength--)
        {
            Debug.Log("A 檔案取得章節名稱：" + chapters[chapterLength - 1].chapter_name + "\n");
            embeddedPrompt = $"{chapterLength}" + chapters[chapterLength - 1].chapter_name + embeddedPrompt;
        }
        Debug.Log(embeddedPrompt);

        childCount = parentObject.transform.childCount;
        childObjects = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            childObjects[i] = parentObject.transform.GetChild(i).gameObject;
        }

        foreach (GameObject child in childObjects)
        {
            Debug.Log("子物件名稱: " + child.name);
        }
    }

    void setEmbeddPrompt(int length)
    {
        embeddedPrompt = $@"
        目錄總共有{length}個章節" +
        @"
        這是上傳內容的目錄。
        請完全遵守Instruction進行 Phrase2，依序每個目錄要問兩個問題。

        回覆請用一定要用JSON的格式 不然扁你
        Json要包含兩個Field 第一個是 progress 用數字標明目前輔導到哪個目錄主題 ， 第二個是reply也就是你的回覆。
        範例Json:
        {
            ""action"": ""one_to_one"",
            ""progress"": 2,
            ""reply"": ""你的回答可以更精確......""
        }

        回覆請用一定要用JSON的格式 不然扁你
        每個reply請不要超出100字。
        請注意使用者的回答，如果回答錯誤就請他修正。 如果回答正確就給予鼓勵並前往下一題。
        目前目錄問題都完成了就進下一個目錄
        當進行到最後的章節""結束""就請在JSON的 reply 中只回 ""GoodJobYouAreGoodToGo""
        注意是要進行到最後的章節""結束後""才在reply只回  ""GoodJobYouAreGoodToGo"" 不要有其他內容 不然扁你
        ";
    }

    void progressUpdate(int progress)
    {
        if (progress <= childCount)
        {
            Debug.Log("Change Progress to " + progress);
            GameObject chapterItem = childObjects[progress - 1];

            Transform imageTransform = chapterItem.transform.Find("Image");
            if (imageTransform != null)
            {
                imageTransform.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("找不到 Image 物件！");
            }

            StartCoroutine(sendProgressUpdate(CurrentCourseId, "one_to_one", progress));
        }
    }

    IEnumerator sendProgressUpdate(int courseId, string chapterType, int orderIndex)
    {
        Debug.Log("Updating Progress to DB...");
        string jsonData = JsonUtility.ToJson(new updateProgressInDB
        {
            action = "update_chapter_progress",
            course_id = courseId,
            chapter_type = chapterType,
            order_index = orderIndex
        });

        UnityWebRequest request = new UnityWebRequest(apiUdateProgressUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Progress updated successfully!");
        else
            Debug.LogError("Failed to update progress: " + request.error);
    }

    string CleanJsonString(string jsonString)
    {
        // 如果字符串包含 JSON 代碼塊，提取其中的內容
        if (jsonString.Contains("```json"))
        {
            int startIndex = jsonString.IndexOf("```json") + 7;
            int endIndex = jsonString.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                jsonString = jsonString.Substring(startIndex, endIndex - startIndex);
            }
        }

        // 移除所有換行符和多餘的空格
        jsonString = jsonString.Replace("\n", "").Replace("\r", "").Trim();

        Debug.Log($"清理後的 JSON: {jsonString}");
        return jsonString;
    }

    // 處理錄音停止事件
    private void HandleRecordingStopped()
    {
        Debug.Log("錄音停止，顯示 loading 動畫");
        if (loadingAnimation != null)
        {
            loadingAnimation.SetActive(true);
        }
    }

    // 在 OnDestroy 中取消訂閱事件
    private void OnDestroy()
    {
        if (audioManager != null)
        {
            audioManager.OnRecordingStopped -= HandleRecordingStopped;
        }
    }

    // 更新 voice ID 的方法
    private void UpdateVoiceId()
    {
        Debug.Log("[chatWithProfessor] UpdateVoiceId 被調用");
        if (TeacherManager.Instance != null)
        {
            string newVoiceId = TeacherManager.Instance.GetCurrentVoiceId();
            Debug.Log($"[chatWithProfessor] 從 TeacherManager 獲取到的 voiceId: {newVoiceId}");
            currentVoice = newVoiceId;
            Debug.Log($"[chatWithProfessor] 更新後的 currentVoice: {currentVoice}");
        }
        else
        {
            Debug.LogError("[chatWithProfessor] TeacherManager.Instance 為 null");
        }
    }
}

[System.Serializable]
public class fetchRequest
{
    public string action;
    public int course_id;
    public string role;
}

[System.Serializable]
public class fetchResponse
{
    public string action;
    public string assistant_id;
    public string thread_id;
}

[System.Serializable]
public class messageResponse
{
    public string action;
    public string message;
}

[System.Serializable]
public class messageRequest
{
    public string action;
    public string assistant_id;
    public string thread_id;
    public string message;
}

[System.Serializable]
public class responseJson
{
    public string action;
    public int progress;
    public string reply;
}

[System.Serializable]
public class updateProgressInDB
{
    public string action;
    public int course_id;
    public string chapter_type;
    public int order_index;
}

[System.Serializable]
public class TtsRequest
{
    public string text;
    public string voice_id;
}
