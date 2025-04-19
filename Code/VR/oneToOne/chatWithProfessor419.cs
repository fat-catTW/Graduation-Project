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

    [Header("Voice Settings")]
    [SerializeField] private string currentVoice = "voice1";  // 當前使用的聲音
    [SerializeField] private TMP_Dropdown voiceDropdown;      // 聲音選擇下拉選單

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
        if (fetcher != null)
        {
            fetcher.OnChaptersFetched += HandleChaptersFetched;
        }

        // 初始化語音設置
        if (audioSource == null)
        {
            // 先檢查是否已經有 AudioSource 組件
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                // 如果沒有，則添加新的組件
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            // 設置 AudioSource 屬性
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 音頻
            audioSource.volume = 1f;
        }

        // 設置聲音下拉選單
        if (voiceDropdown != null)
        {
            voiceDropdown.ClearOptions();
            voiceDropdown.AddOptions(new List<string> { 
                "Voice 1 (預設)",
                "Voice 2 (Rachel)",
                "Voice 3 (Josh)"
            });
            voiceDropdown.onValueChanged.AddListener(OnVoiceChanged);
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
            
            StartCoroutine(getChatGPTIDs());
        }
        else
        {
            Debug.LogError("PersistentDataManager.Instance is null! 確保 PersistentDataManager 存在於場景中。");
        }
    }

    private void OnVoiceChanged(int index)
    {
        switch (index)
        {
            case 0:
                currentVoice = "voice1";
                break;
            case 1:
                currentVoice = "voice2";
                break;
            case 2:
                currentVoice = "voice3";
                break;
        }
        Debug.Log($"切换语音ID为: {currentVoice}");
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
                        Debug.Log("The progress: " + reply.progress);

                        // 收到回應後，立即進行文字轉語音
                        StartCoroutine(TextToSpeech(reply.reply));
                        
                        progressUpdate(reply.progress);
                    }
                    else
                    {
                        Debug.LogError("無法解析 JSON 回應");
                        blackBoardText.text = "無法解析伺服器回應";
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"JSON 解析錯誤: {e.Message}");
                    Debug.LogError($"原始回應: {request.downloadHandler.text}");
                    blackBoardText.text = "解析回應時發生錯誤";
                }
            }
            else
            {
                blackBoardText.text = "Error: " + request.error;
            }
        }
    }

    private IEnumerator TextToSpeech(string text)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is not assigned!");
            yield break;
        }

        string jsonData = JsonUtility.ToJson(new TtsRequest
        {
            text = text,
            voice_id = currentVoice
        });
        Debug.Log($"準備發送請求: {jsonData}");

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
                    Debug.Log($"音頻接收成功，長度: {audioClip.length}秒");
                    audioSource.clip = audioClip;
                    audioSource.Play();
                }
                else
                {
                    Debug.LogError("接收到的音頻為空");
                }
            }
            else
            {
                Debug.LogError($"請求失敗: {request.error}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TextToSpeech 錯誤: {e.Message}");
        }
        finally
        {
            request.Dispose();
        }
    }

    void HandleChaptersFetched(Chapter[] chapters)
    {
        int chapterLength = chapters.Length;
        setEmbeddPrompt(chapterLength + 1);

        embeddedPrompt = $"{chapterLength + 1}結束\n" + embeddedPrompt;

        for(; chapterLength > 0 ; chapterLength--)
        {
            Debug.Log("A 檔案取得章節名稱：" + chapters[chapterLength - 1].chapter_name +"\n");
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
        embeddedPrompt =$@"
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
        if(progress <= childCount)
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
        return jsonString.Replace("```json", "").Replace("```", "").Trim();
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
