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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (fetcher != null)
        {
            fetcher.OnChaptersFetched += HandleChaptersFetched;
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

    // Update is called once per frame
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
                var response = JsonUtility.FromJson<messageResponse>(request.downloadHandler.text);
                string jsonString = CleanJsonString(response.message);

                Debug.Log(jsonString);
                responseJson reply = JsonUtility.FromJson<responseJson>(jsonString);

                blackBoardText.text = reply.reply;
                Debug.Log("The progress: " + reply.progress);

                progressUpdate(reply.progress);
            }
            else
            {
               
                blackBoardText.text = "Error: " + request.error;
            }
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
            // 你可以在這裡把資料存下來或顯示等

            embeddedPrompt = $"{chapterLength}" + chapters[chapterLength - 1].chapter_name + embeddedPrompt;
        }
        Debug.Log(embeddedPrompt);

        childCount = parentObject.transform.childCount;
        childObjects = new GameObject[childCount];

        for (int i = 0; i < childCount; i++)
        {
            childObjects[i] = parentObject.transform.GetChild(i).gameObject; // 存入子物件
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
            “action”: “one_to_one”,
            “progress”: 2,
            “reply”: “你的回答可以更精確......”
        }

        回覆請用一定要用JSON的格式 不然扁你
        每個reply請不要超出100字。
        請注意使用者的回答，如果回答錯誤就請他修正。 如果回答正確就給予鼓勵並前往下一題。
        目前目錄問題都完成了就進下一個目錄
        當進行到最後的章節”結束”就請在JSON的 reply 中只回 “GoodJobYouAreGoodToGo”
        注意是要進行到最後的章節”結束後”才在reply只回  “GoodJobYouAreGoodToGo” 不要有其他內容 不然扁你
        ";
    }

    void progressUpdate(int progress)
    {
        if(progress <= childCount)
        {
            Debug.Log("Change Color Progress " + progress);
            Image image = childObjects[progress - 1].GetComponent<Image>();
            image.color = new Color(0.5f, 0.8f, 0.2f, 1f);
            sendProgressUpdate(CurrentCourseId, "one_to_one", progress);
        }
    }

    IEnumerator sendProgressUpdate(int courseId, string chapterType, int orderIndex)
    {

        var payload = new
        {
            action = "update_chapter_progress",
            course_id = courseId,
            chapter_type = chapterType,
            order_index = orderIndex
        };
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest request = new UnityWebRequest(apiUdateProgressUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
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
        // 移除 Markdown 格式的 ```json 和 ```
        return jsonString.Replace("```json", "").Replace("```", "").Trim();
    }
}








[System.Serializable]//�o�����O �i�ǦC�ƪ�
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





