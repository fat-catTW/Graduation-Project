using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

public class chatWithStudent : MonoBehaviour
{
  public TMP_Text studentSpeakText1;
  public GameObject studentSpeakPanel1;
  public TMP_Text studentSpeakText2;
  public GameObject studentSpeakPanel2;
  public TMP_Text studentSpeakText3;
  public GameObject studentSpeakPanel3;
  public TMP_Text teacherResponse;

  public GameObject settlementPanel;
  public Slider precisionSlider;
  public TMP_Text precisionPercentage;
  public Slider expressivenessSlider;
  public TMP_Text expressivenessPercentage;
  public Slider comprehensionSlider;
  public TMP_Text comprehensionPercentage;
  public Slider interactivitySlider;
  public TMP_Text interactivityPercentage;

  public GameObject settlementPanel1;

  public TMP_InputField testInput;
  public TMP_Text teacherTips;
  public fetchRenderToC2 fetcher; // 拖進 Inspector 或用 GetComponent 抓

  // 添加語音相關組件
  public AudioSource audioSource;  // 用於播放語音
  public GameObject loadingAnimation; // loading動畫
  private AudioManager audioManager;  // 音頻管理器
  private string apiTtsUrl = "https://feynman-server.onrender.com/text-to-speech";  // TTS API

  private string apiFetchUrl = "https://feynman-server.onrender.com/fetch";
  private string apiChatUrl = "https://feynman-server.onrender.com/chat";
  private string apiUdateProgressUrl = "https://feynman-server.onrender.com/update_chapter_progress";
  private string apiUpdateScoreUrl = "https://feynman-server.onrender.com/update_score";
  private string apiUpdateCommentUrl = "https://feynman-server.onrender.com/update_comment";

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
                                             ""action"": ""one_to_three"",
                                             ""progress"": 1,
                                             ""reply"": ""老師，在AI訓練裡什麼是過度擬合......""
                                           }
                                           回覆請用一定要用JSON的格式 不然扁你
                                           請注意使用者的回覆，如果可以理解就請簡單回覆 感謝 或 表示自己理解了，不要再追問
                                           如果使用者對問題沒有做很好的解釋，就再追問。
                                           注意! 章節進度 Progress得部分是要看使用者講到哪裡來顯示，Progress只增加不減少。
                                           重要！Progress 必須從 1 開始，不能是 0！
                                           當進行到最後的章節""結束""就請在JSON的 reply 中只回 ""GoodJobYouAreGoodToGo""
                                           注意是要進行到最後的章節""結束後""才在reply只回  ""GoodJobYouAreGoodToGo"" 不要有其他內容 不然扁你";
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
    // 初始化音頻相關組件
    if (audioSource == null)
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
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
    }
    else
    {
        Debug.LogError("找不到 AudioManager 組件！請確保在同一個物件上添加 AudioManager 組件");
    }

    // 確保 loading 動畫一開始是隱藏的
    if (loadingAnimation != null)
    {
        loadingAnimation.SetActive(false);
    }

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
    Debug.Log($@"這是使用者的講解:{testInput.text}\n" + studentEmbeddedPrompt);

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
      
      // 如果 progress 為 0，設置為 1
      currentProgress = reply.progress <= 0 ? 1 : reply.progress;
      progressUpdate(currentProgress);
      CycleThroughExplanationPoints(currentProgress);

      int studentCase = Random.Range(0, 3);
      switch (studentCase)
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

      StartCoroutine(TextToSpeech(reply.reply, studentCase));
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
    

    

    for (int i = 0; i < chapterLength ; i++)
    {
      Debug.Log("A 檔案取得章節名稱：" + chapters[i].chapter_name + "\n");
      // 你可以在這裡把資料存下來或顯示等

      chapterNames += i+1 + chapters[i].chapter_name + "\n";

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

    // setTeacherEmbeddPrompt(chapterLength);
    setStudentEmbeddPrompt(chapterLength);

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
            ""action"":""one_to_three"",
            ""progress"":3,
            ""reply"": ""
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
      {chapterNames}
      {length + 1}結束
      目錄總共有{length + 1}個章節
      以上是文檔的目錄章節" + studentEmbeddedPrompt;
  }

  void progressUpdate(int progress)
  {
    // 檢查 progress 是否在有效範圍內
    if (progress <= 0 || progress > childCount)
    {
      Debug.LogWarning($"Progress 值 {progress} 超出有效範圍 (1-{childCount})");
      return;
    }

    Debug.Log($"Change Progress to {progress}");
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

    // 檢查是否達到最後一章
    if(progress >= childCount)
    {
      settlementPanel.gameObject.SetActive(true);
      StartCoroutine(Scoring());
      StartCoroutine(MakingComment());
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
                ""action"": ""one_to_three"",
                ""tips"":[""This is a tip for you"",""This is another tip for you"",""This is the last tip for you""],
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

  IEnumerator Scoring()
  {
    string scoringJsonData = JsonUtility.ToJson(new messageRequest
    {
      action = "message",
      message = @"現在課程結束了，參考剛剛使用者的上課與回覆問題的表現為其 準確度 表達力 理解度 互動力 這四項打 0 到 100的分數。 請用JSON的方式回覆給我不然砍你
                    JSON範例:
                    {
                      'action' = 'one_to_three',
                      'precision' = 90,
                      'expressiveness' = 75 ,
                      'comprehension' = 88,
                      'interactivity' = 81 
                    }
                    請只用JSON回覆! 請只用JSON回覆! 不要有任何其他文字!! 不然揍你!",
      assistant_id = studentAssistant_id,
      thread_id = studentThread_id
    });

    Coroutine scoringRequest = StartCoroutine(SendRequest(scoringJsonData, HandleScoringResponse));

    yield return scoringRequest;

  }

  // 處理學生回應
  private void HandleScoringResponse(string responseText)
  {
    var response = JsonUtility.FromJson<messageResponse>(responseText);
    string jsonString = CleanJsonString(response.message);

    Debug.Log("Scoring JSON:" + jsonString);

    Debug.Log(jsonString);
    Scoring scoring = JsonUtility.FromJson<Scoring>(jsonString);
    
    precisionSlider.value = scoring.precision;
    precisionPercentage.text = scoring.precision.ToString();
    expressivenessSlider.value = scoring.expressiveness;
    expressivenessPercentage.text = scoring.expressiveness.ToString();
    comprehensionSlider.value = scoring.comprehension;
    comprehensionPercentage.text = scoring.comprehension.ToString();
    interactivitySlider.value = scoring.interactivity;
    interactivityPercentage.text = scoring.interactivity.ToString();

    StartCoroutine(sendScoringUpdate(scoring.precision, scoring.expressiveness, scoring.comprehension, scoring.interactivity));
  }

  IEnumerator sendScoringUpdate(int precisionScore, int expressivenessScore, int comprehensionScore, int interactivityScore)
  {

    Debug.Log("Updating Scores to DB...");
    string jsonData = JsonUtility.ToJson(new updateScoreInDB
    {
      action = "update_score",
      course_id = CurrentCourseId,
      user_id = UserId,
      precision = precisionScore,
      expressiveness = expressivenessScore,
      comprehension = comprehensionScore,
      interactivity = interactivityScore
    });

    UnityWebRequest request = new UnityWebRequest(apiUpdateScoreUrl, "POST");
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

  IEnumerator MakingComment()
  {
    string commentJsonData = JsonUtility.ToJson(new messageRequest
    {
      action = "message",
      message = @"現在課程結束了，參考剛剛使用者的上課與回覆問題的表現，分別以老師、學生的角色給予評論，每個評論不要超過30個字。另外還要列出整體使用者好表現與可以改進的地方(各不超過5項)， 請用JSON的方式回覆給我不然砍你
                    JSON範例:
                    {
                      'action' = 'one_to_three',
                      'teacher_comment' = '今天的表現有點糟糕！哼哼！',
                      'student1_feedback' = '你把複雜的概念講得不太清楚！尤其是在解釋那個難懂的部分時。' ,
                      'student2_feedback' = '舉的例子很實用，不過可以多說明一下實際應用場景喔～親～。',
                      'student3_feedback' = '觀點還行啦！讓我想到這個理論在其他延伸應用。',
                      'good_points' = ['概念解釋清晰準確',
                                        '舉例生動有趣',
                                        '與同學互動熱絡',
                                        '邏輯思維清晰',
                                        '表達流暢自然'],
                      'improvement_points' = ['可以多分享實際應用場景',
                                        '建議控制節奏，不要說太快',
                                        '可以多使用視覺輔助工具',
                                        '建議增加互動練習時間',
                                        '可以多舉一些生活化的例子']
                    }
                    請只用JSON回覆! 請只用JSON回覆! 不要有任何其他文字!! 不然揍你!",
      assistant_id = studentAssistant_id,
      thread_id = studentThread_id
    });

    Coroutine commentRequest = StartCoroutine(SendRequest(commentJsonData, HandleCommentResponse));

    yield return commentRequest;
  }

  private void HandleCommentResponse(string responseText)
  {
    var response = JsonUtility.FromJson<messageResponse>(responseText);
    string jsonString = CleanJsonString(response.message);

    Debug.Log("Comment JSON:" + jsonString);

    Debug.Log(jsonString);
    Comment comment = JsonUtility.FromJson<Comment>(jsonString);
    
    

    StartCoroutine(sendCommentUpdate(comment.teacher_comment, comment.student1_feedback, comment.student2_feedback, comment.student3_feedback, comment.good_points, comment.improvement_points));
  }

  IEnumerator sendCommentUpdate(string teacher_comment, string student1_feedback, string student2_feedback, string student3_feedback, string[] good_points, string[] improvement_points)
  {

    Debug.Log("Updating Comment to DB...");

    string jsonData = $@"
    {{
      ""action"": ""update_score"",
      ""course_id"": {CurrentCourseId},
      ""user_id"": {UserId},
      ""teacher_comment"": ""{teacher_comment}"",
      ""student1_feedback"": ""{student1_feedback}"",
      ""student2_feedback"": ""{student2_feedback}"",
      ""student3_feedback"": ""{student3_feedback}"",
      ""good_points"": [{string.Join(",", good_points.Select(s => $"\"{s}\""))}],
      ""improvement_points"": [{string.Join(",", improvement_points.Select(s => $"\"{s}\""))}]
    }}";
    // string jsonData = JsonUtility.ToJson(new updateCommentInDB
    // {
    //   action = "update_score",
    //   course_id = CurrentCourseId,
    //   user_id = UserId,
    //   teacher_comment = teacher_comment,
    //   student1_feedback = student1_feedback,
    //   student2_feedback = student2_feedback,
    //   student3_feedback = student3_feedback,
    //   good_points = good_points,
    //   improvement_points = improvement_points
      
    // });

    UnityWebRequest request = new UnityWebRequest(apiUpdateScoreUrl, "POST");
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

  // 處理錄音停止事件
  private void HandleRecordingStopped()
  {
      Debug.Log("錄音停止，顯示 loading 動畫");
      if (loadingAnimation != null)
      {
          loadingAnimation.SetActive(true);
      }
  }

  // 添加文字轉語音功能
  private IEnumerator TextToSpeech(string text, int studentCase)
  {
      if (audioSource == null)
      {
          Debug.LogError("AudioSource is not assigned!");
          if (loadingAnimation != null)
          {
              loadingAnimation.SetActive(false);
          }
          yield break;
      }

      string voiceId;
      string studentName;
      switch (studentCase)
      {
          case 0:
              voiceId = "hkfHEbBvdQFNX4uWHqRF";
              studentName = "小嚕";
              break;
          case 1:
              voiceId = "fQj4gJSexpu8RDE2Ii5m";
              studentName = "小明";
              break;
          case 2:
              voiceId = "gU2KtIu9OZWy3KqiqNj6";
              studentName = "小美";
              break;
          default:
              voiceId = "hkfHEbBvdQFNX4uWHqRF";
              studentName = "小嚕";
              break;
      }

      Debug.Log($"使用 {studentName} 的語音 ID: {voiceId}");

      // 確保 text 不為空
      if (string.IsNullOrEmpty(text))
      {
          Debug.LogError($"{studentName} 的文本為空！");
          if (loadingAnimation != null)
          {
              loadingAnimation.SetActive(false);
          }
          yield break;
      }

      // 創建 TTS 請求
      var ttsRequest = new TtsRequest
      {
          text = text,
          voice_id = voiceId
      };

      string jsonData = JsonUtility.ToJson(ttsRequest);
      Debug.Log($"TTS 請求數據: {jsonData}");

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
                  Debug.Log($"{studentName} 的音頻接收成功，長度: {audioClip.length}秒");
                  audioSource.clip = audioClip;
                  audioSource.Play();
                  
                  if (loadingAnimation != null)
                  {
                      loadingAnimation.SetActive(false);
                  }
              }
          }
          else
          {
              Debug.LogError($"{studentName} 的 TTS 請求失敗: {request.error}");
              Debug.LogError($"請求數據: {jsonData}");
              if (loadingAnimation != null)
              {
                  loadingAnimation.SetActive(false);
              }
          }
      }
      catch (System.Exception e)
      {
          Debug.LogError($"{studentName} 的 TextToSpeech 錯誤: {e.Message}");
          if (loadingAnimation != null)
          {
              loadingAnimation.SetActive(false);
          }
      }
      finally
      {
          request.Dispose();
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
}


// 用於解析 AI 回傳的 JSON 格式
[System.Serializable]
public class GeneratePointsResponse
{
  public string action;
  public string[] tips; // 對應 JSON 中的 "tips" 陣列
}

[System.Serializable]
public class Scoring
{
  public string action;
  public int precision;
  public int expressiveness;
  public int comprehension;
  public int interactivity;
}

[System.Serializable]
public class updateScoreInDB
{
  public string action;
  public int course_id;
  public int user_id;
  public int precision;
  public int expressiveness;
  public int comprehension;
  public int interactivity;
}

[System.Serializable]
public class Comment
{
  public string action;
  public string teacher_comment;
  public string student1_feedback;
  public string student2_feedback;
  public string student3_feedback;
  public string[] good_points;
  public string[] improvement_points;
}

[System.Serializable]
public class updateCommentInDB
{
  public string action;
  public int course_id;
  public int user_id;
  public string teacher_comment;
  public string student1_feedback;
  public string student2_feedback;
  public string student3_feedback;
  public string[] good_points;
  public string[] improvement_points;
}
