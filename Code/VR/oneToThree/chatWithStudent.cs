using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class chatWithStudent : MonoBehaviour
{
  public TMP_Text studentSpeakText1;
  public GameObject studentSpeakPanel1;
  public TMP_Text studentSpeakText2;
  public GameObject studentSpeakPanel2;
  public TMP_Text studentSpeakText3;
  public GameObject studentSpeakPanel3;
  public TMP_Text teacherResponse;

  public TMP_InputField testInput;
  public TMP_Text teacherTips;
  public fetchRenderToC2 fetcher; // 拖進 Inspector 或用 GetComponent 抓

  private string apiFetchUrl = "https://feynman-server.onrender.com/fetch";
  private string apiChatUrl = "https://feynman-server.onrender.com/chat";
  private string apiUdateProgressUrl = "https://feynman-server.onrender.com/update_chapter_progress";
  private int UserId;
  private string Username;
  private int CurrentCourseId;
  private string CurrentCourseName;
  private string CurrentStage;
  private float OneToOneProgress;
  private float ClassroomProgress;
  private int CompletedChapters;
  private int TotalChapters;

  string studentAssistant_id = "";
  string studentThread_id = "";
  string teacherAssistant_id = "";
  string teacherThread_id = "";

  private string studentEmbeddedPrompt = @"請完全遵守Instruction進行，不然砍你! 請依據使用者的講解，以學生的角度去問問題。
                                           並依據使用者講解的內容去判斷他講道哪個章節了。  然後請用JSON的格式回傳給我，這很重要，遵守不然拔你電源
                                           JSON範例:
                                           {
                                             “action”: “one_to_three”,
                                             “progress”: 2,
                                             “reply”: “老師，在AI訓練裡什麼是過度擬合......”
                                           }
                                           回覆請用一定要用JSON的格式 不然扁你
                                           請注意使用者的回覆，如果可以理解就請簡單回覆 感謝 或 表示自己理解了
                                           如果使用者對問題沒有做很好的解釋，就再追問。
                                           注意! 章節進度 Progress得部分是要看使用者講到哪裡來顯示，Progress只增加不減少。
                                          當進行到最後的章節”結束”就請在JSON的 reply 中只回 “GoodJobYouAreGoodToGo”
                                          注意是要進行到最後的章節”結束後”才在reply只回  “GoodJobYouAreGoodToGo” 不要有其他內容 不然扁你
";
  private string teacherEmbeddedPrompt = "";
  private string chapterNames;
  private int currentProgress = 0;
  public GameObject parentObject;
  private GameObject[] childObjects;  //儲存每個目錄章節
  private int childCount;
  private string[] explanationPoints; // 用於存放解說點的陣列

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
      role = "student"
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
        studentAssistant_id = response.assistant_id;
        studentThread_id = response.thread_id;
        Debug.Log("assistant_Id and thread_id fetched successfully");
        Debug.Log("assistant_Id: " + studentAssistant_id);
        Debug.Log("thread_id: " + studentThread_id);
      }
      else
      {
        studentSpeakText2.text = "Error: " + request.error;
      }

    }

    jsonData = JsonUtility.ToJson(new fetchRequest
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
        teacherAssistant_id = response.assistant_id;
        teacherThread_id = response.thread_id;
        Debug.Log("assistant_Id and thread_id fetched successfully");
        Debug.Log("assistant_Id: " + teacherAssistant_id);
        Debug.Log("thread_id: " + teacherThread_id);
      }
      else
      {
        studentSpeakText2.text = "Error: " + request.error;
      }

    }

    // 在完成載入後，啟動生成解說點的協程
    StartCoroutine(GenerateExplanationPoints());
  }

  public IEnumerator SendMessage()
  {
    // 準備學生的請求
    string studentJsonData = JsonUtility.ToJson(new messageRequest
    {
      action = "message",
      message = $@"這是使用者的講解:{testInput.text}\n" + studentEmbeddedPrompt,
      assistant_id = studentAssistant_id,
      thread_id = studentThread_id
    });

    // 準備老師的請求
    // string teacherJsonData = JsonUtility.ToJson(new messageRequest
    // {
    //   action = "message",
    //   message = $@"這是使用者這段的講解:{testInput.text}\n" + teacherEmbeddedPrompt,
    //   assistant_id = teacherAssistant_id,
    //   thread_id = teacherThread_id
    // });

    // 清空輸入欄位
    testInput.text = "";

    // 啟動兩個協程並行處理請求
    Coroutine studentRequest = StartCoroutine(SendRequest(studentJsonData, HandleStudentResponse));
    //Coroutine teacherRequest = StartCoroutine(SendRequest(teacherJsonData, HandleTeacherResponse));

    // 等待兩個請求完成
    yield return studentRequest;
    // yield return teacherRequest;
  }

  // 通用的請求處理協程
  private IEnumerator SendRequest(string jsonData, System.Action<string> onSuccess)
  {
    using (UnityWebRequest request = new UnityWebRequest(apiChatUrl, "POST"))
    {
      byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
      request.uploadHandler = new UploadHandlerRaw(bodyRaw);
      request.downloadHandler = new DownloadHandlerBuffer();
      request.SetRequestHeader("Content-Type", "application/json");

      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.Success)
      {
        onSuccess?.Invoke(request.downloadHandler.text);
      }
      else
      {
        Debug.LogError("Request failed: " + request.error);
      }
    }
  }

  // 處理學生回應
  private void HandleStudentResponse(string responseText)
  {
    var response = JsonUtility.FromJson<messageResponse>(responseText);
    string jsonString = CleanJsonString(response.message);

    Debug.Log(jsonString);
    responseJson reply = JsonUtility.FromJson<responseJson>(jsonString);
    currentProgress = reply.progress;
    progressUpdate(currentProgress);
    CycleThroughExplanationPoints(currentProgress);

    switch (Random.Range(0, 3))
    {
      case 0:
        studentSpeakPanel1.gameObject.SetActive(true);
        studentSpeakText1.text = reply.reply;
        studentSpeakPanel2.gameObject.SetActive(false);
        studentSpeakPanel3.gameObject.SetActive(false);
        break;
      case 1:
        studentSpeakPanel2.gameObject.SetActive(true);
        studentSpeakText2.text = reply.reply;
        studentSpeakPanel1.gameObject.SetActive(false);
        studentSpeakPanel3.gameObject.SetActive(false);
        break;
      case 2:
        studentSpeakPanel3.gameObject.SetActive(true);
        studentSpeakText3.text = reply.reply;
        studentSpeakPanel1.gameObject.SetActive(false);
        studentSpeakPanel2.gameObject.SetActive(false);
        break;
    }
  }

  // 處理老師回應
  private void HandleTeacherResponse(string responseText)
  {
    var response = JsonUtility.FromJson<messageResponse>(responseText);
    string jsonString = CleanJsonString(response.message);

    Debug.Log(jsonString);
    responseJson reply = JsonUtility.FromJson<responseJson>(jsonString);
    currentProgress = reply.progress;
    progressUpdate(currentProgress);
    CycleThroughExplanationPoints(currentProgress);

  }

  void HandleChaptersFetched(Chapter[] chapters)
  {
    int chapterLength = chapters.Length;
    // setTeacherEmbeddPrompt(chapterLength);
    setStudentEmbeddPrompt(chapterLength);

    teacherEmbeddedPrompt = $"{chapterLength + 1}結束\n" + teacherEmbeddedPrompt;

    for (; chapterLength > 0; chapterLength--)
    {
      Debug.Log("A 檔案取得章節名稱：" + chapters[chapterLength - 1].chapter_name + "\n");
      // 你可以在這裡把資料存下來或顯示等

      chapterNames += chapterLength + chapters[chapterLength - 1].chapter_name + "\n";

    }

    teacherEmbeddedPrompt = $"{chapterNames} + {teacherEmbeddedPrompt}";
    Debug.Log(teacherEmbeddedPrompt);

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

  void setTeacherEmbeddPrompt(int length)
  {
    teacherEmbeddedPrompt = $@"
        目錄總共有{length}個章節
        {chapterNames}" +
    @" 
        以上是文檔的目錄章節

        這是phase 3 請嚴格遵守instruction，請注意這是Phase 3 請重新計算進度，不要把Phase 2 的進度帶進來。
        請根據講解的內容去判斷使用者講道哪部分了。
        並用Json的格式回覆
        例如:
        {
            “action”:”one_to_three”,
            “progress”:3,
            “reply”: “”
        }
        reply永遠是空的。
        一定要依上面的Json範例回傳，不然扁你。
        請注意，目錄的進行只能向向下，不能回頭。
        例如已經講到第8章了，但不能因為使用者講第四章的內容而把Progress往回調到第四章。
        但也要去保講解是有講到下一章節的內容，才能往下進行。
        這很重要，請遵守不然拆你GPU。
        ";
  }

  void setStudentEmbeddPrompt(int length)
  {
    studentEmbeddedPrompt = $@"
      目錄總共有{length}個章節
      {chapterNames}
      以上是文檔的目錄章節" + studentEmbeddedPrompt;
  }

  void progressUpdate(int progress)
  {
    if (progress <= childCount)
    {
      Debug.Log("Change Progress to" + progress);
      GameObject chapterItem = childObjects[progress - 1];
      Transform imageTransform = chapterItem.transform.Find("Image");
      if (imageTransform != null)
      {
        imageTransform.gameObject.SetActive(true);  // 顯示 Image
      }
      else
      {
        Debug.LogWarning("找不到 Image 物件！");
      }
      StartCoroutine(sendProgressUpdate(CurrentCourseId, "classroom", progress));
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


  private IEnumerator GenerateExplanationPoints()
  {
    Debug.Log("開始生成解說點...");

    string jsonData = JsonUtility.ToJson(new messageRequest
    {
      action = "message",
      message = $@"這是Phase 3 剛開始，有個邀求請你依照上傳的檔案的內容，為目錄的每個章接生一個講解提示給使用者看，讓他知道他可以講解些什麼。
            切記，有幾個章節就生多少講解提示(EX:有5個章節就生5個提示)。 每個提示要對映照章節且依照順序。
            這是目錄的依序章節:
            {chapterNames}"
        + @"
            請使用JSON格式回覆且不要有任何其他文字這很重要，不然扁你。
            JSON範例:
            {
                action: “one_to_three“,
                “tips“:[“This is a tip for you“,
                        “This is another tip for you“,
                        “This is the last tip for you“],
            }
            tips都要是繁體中文50字內，不然扁你",
      assistant_id = teacherAssistant_id,
      thread_id = teacherThread_id
    });

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

        GeneratePointsResponse tipsResponse = JsonUtility.FromJson<GeneratePointsResponse>(jsonString);
        explanationPoints = tipsResponse.tips; // 假設 API 回傳一個包含解說點的陣列
        Debug.Log("解說點生成成功");

        CycleThroughExplanationPoints(currentProgress);

      }
      else
      {
        Debug.LogError("生成解說點失敗: " + request.error);
      }


    }
  }

  void CycleThroughExplanationPoints(int currentProgress)
  {
    if (currentProgress >= explanationPoints.Length)
    {
      currentProgress = explanationPoints.Length - 1;
    }
    teacherTips.text = explanationPoints[currentProgress];
  }

  string CleanJsonString(string jsonString)
  {
    // 移除 Markdown 格式的 ```json 和 ```
    return jsonString.Replace("```json", "").Replace("```", "").Trim();
  }

}

// 用於解析 AI 回傳的 JSON 格式
[System.Serializable]
public class GeneratePointsResponse
{
  public string action;
  public string[] tips; // 對應 JSON 中的 "tips" 陣列
}


