using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using System;
using System.IO;
using System.Collections.Generic;

public class rubyChat : MonoBehaviour
{
    public TMP_Text blackBoardText;
    public TMP_InputField testInput;
    public AudioSource audioSource;  // 添加AudioSource組件引用
    
    [Header("Voice Settings")]
    [SerializeField] private string currentVoice = "voice1";  // 當前使用的聲音
    [SerializeField] private TMP_Dropdown voiceDropdown;      // 聲音選擇下拉選單

    private string apiFetchUrl = "https://feynman-server.onrender.com/fetch";
    private string apiChatUrl = "https://feynman-server.onrender.com/chat";
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

    public string assistant_id = "";
    public string thread_id = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

        // 確保有AudioSource組件
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
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
            message = testInput.text,
            assistant_id = assistant_id,
            thread_id = thread_id
        });
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
                blackBoardText.text = response.message;
                Debug.Log(response.message);
                
                // 收到回應後，立即進行文字轉語音
                StartCoroutine(TextToSpeech(response.message));
            }
            else
            {
                blackBoardText.text = "Error: " + request.error;
            }
        }
    }

    private IEnumerator TextToSpeech(string text)
    {
        string jsonData = JsonUtility.ToJson(new TtsRequest
        {
            text = text,
            voice_id = currentVoice
        });

        using (UnityWebRequest request = new UnityWebRequest(apiTtsUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(apiTtsUrl, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"Text-to-speech error: {request.error}");
            }
        }
    }
}


[System.Serializable]
public class TtsRequest
{
    public string text;
    public string voice_id;
}