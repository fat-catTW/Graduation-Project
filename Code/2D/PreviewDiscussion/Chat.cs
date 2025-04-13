using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using TMPro;
using UnityEngine.Networking;

public class Chat : MonoBehaviour
{
    private string apiUrl = "https://feynman-server.onrender.com/chat";
    public TMP_InputField inputField;
    public GameObject playerMessagePrefab;
    public GameObject feyndoraMessagePrefab;
    public GameObject Content;

    public GameObject ToCSecondMenu_1;
    public GameObject ToCTabPrefab;


    private string assistantID;
    private string threadID;
    private int course_id;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assistantID = PlayerPrefs.GetString("Assistant1_ID");
        threadID = PlayerPrefs.GetString("Thread1_ID");
        course_id = PlayerPrefs.GetInt("Course_ID");

        GameObject welcomeMessage1 = Instantiate(feyndoraMessagePrefab, Content.transform);
        welcomeMessage1.GetComponent<Message>().MessageText.text = "嗨! 這裡是預習空間";
        GameObject welcomeMessage2 = Instantiate(feyndoraMessagePrefab, Content.transform);
        welcomeMessage2.GetComponent<Message>().MessageText.text = "預習過程中遇到問題，歡迎向我提問";

        Debug.Log("StartMakingToC");
        //StartCoroutine(GenerateContent("根據我上傳的檔案把它做解析，生成目錄並生成給我時請用的JSON的格式。。"));

        StartCoroutine(GenerateContent(@"根據我上傳檔案的內容把自行把它做解析，自行理解內容然後生成目錄，生成給我時請用的JSON的格式不然揍你。 模板為:
                    {
                        ""action"": ""get_chapters"",
                        ""chapters"": [
                            {""title"": ""什麼是人工智慧""},
                            {""title"": ""AI 歷史簡介""},
                            {""title"": ""AI 應用""},
                            {""title"": ""AI 未來展望""}...
                        ]
                    }
                    每個title的文字不要超過16個，不然拔你電源
                    不要生成其他文字，生成目錄Json給我就好，每個title內容的字不要超過60字。 我只要Json目錄，只有一這次要請你生目錄，後面就請依據instruction進行"));

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void playerSendMessage()
    {
        string message = inputField.text;
        GameObject newMessage = Instantiate(playerMessagePrefab, Content.transform);
        newMessage.GetComponent<Message>().MessageText.text = message;
        StartCoroutine(SendMessageToChatGPT($@"現在請完全遵守Instruction進行 Phrase1
        這是使用者的回答{message}"));
    }

    public IEnumerator SendMessageToChatGPT(string message)
    {

        string jsonData = JsonUtility.ToJson(new messageRequest
        {
            action = "message",
            message = message,
            assistant_id = assistantID,
            thread_id = threadID
        });


        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");


            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {

                var response = JsonUtility.FromJson<messageResponse>(request.downloadHandler.text);
                GameObject newMessage = Instantiate(feyndoraMessagePrefab, Content.transform);// ��ܦ^��
                newMessage.GetComponent<Message>().MessageText.text = response.message;
            }
            else
            {

                GameObject newMessage = Instantiate(feyndoraMessagePrefab, Content.transform);// ��ܦ^��
                newMessage.GetComponent<Message>().MessageText.text = "Error: " + request.error;
            }
        }
    }

    public IEnumerator GenerateContent(string message)
    {

        string jsonData = JsonUtility.ToJson(new messageRequest
        {
            action = "message",
            message = message,
            assistant_id = assistantID,
            thread_id = threadID
        });


        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
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
                TableOfContents ToC = JsonUtility.FromJson<TableOfContents>(jsonString);



                foreach (Chapter chapter in ToC.chapters)
                {
                    Debug.Log(chapter.title);
                    GameObject newTab1 = Instantiate(ToCTabPrefab, ToCSecondMenu_1.transform);
                    newTab1.GetComponent<NewTap>().TabText.text = chapter.title;
                }


                StartCoroutine(ToCToDB(ToC));
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    public IEnumerator ToCToDB(TableOfContents ToC)
    {
        ToC.action = "upload_ToC";


        string jsonData = JsonUtility.ToJson(ToC);
        string modifiedJson = "{\"course_id\":" + course_id + "," + jsonData.Substring(1);

        Debug.Log("上傳 JSON: " + modifiedJson); // 方便 Debug，看看 JSON 是否正確

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(modifiedJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // **錯誤處理**
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("目錄上傳成功: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("目錄上傳失敗: " + request.error);
            }
        }

    }

    string CleanJsonString(string jsonString)
    {
        // 移除 Markdown 格式的 ```json 和 ```
        return jsonString.Replace("```json", "").Replace("```", "").Trim();
    }
}




[System.Serializable]
public class messageRequest
{
    public string action;
    public string assistant_id;
    public string thread_id;
    public string message;
}

[System.Serializable]//�o�����O �i�ǦC�ƪ�
public class messageResponse
{
    public string action;
    public string message;
}

[System.Serializable]  // 讓 JsonUtility 能解析這個類別
public class Chapter
{
    public string title;
}

[System.Serializable]
public class TableOfContents
{
    public string action;
    public Chapter[] chapters;
}

[System.Serializable]  // 用來封裝 chapters 陣列，避免 Unity 無法直接轉換陣列的問題
public class ChapterArrayWrapper
{
    public Chapter[] chapters;
}



// 根據我上傳的檔案把它做解析，生成目錄，並以 JSON 格式回傳給我，然後很重要! 請不要任何其他文字。請務必確保輸出是 JSON，格式如下：
//         {
//             ""action"": ""get_chapters"",
//             ""chapters"": [
//                 {""title"": ""什麼是人工智慧""},
//                 {""title"": ""AI 歷史簡介""},
//                 {""title"": ""AI 應用""},
//                 {""title"": ""AI 未來展望""}
//             ]
//         }
//         不要輸出其他文字，只需要 JSON 結果，請務必確保格式正確。