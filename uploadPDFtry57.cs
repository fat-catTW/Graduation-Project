using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using TMPro;

public class uploadPDF : MonoBehaviour
{
    private string apiUrl = "https://feynman-server.onrender.com";

    public TMP_InputField classNameInputField;
    public TMP_Text classNameText;
    public TMP_Text classDateText;
    public GameObject previewDiscussionPanel;
    public GameObject updatePagePanel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        StartCoroutine(CopyDemoFilesToDocuments());
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenFileBrowser()
    {
        if (NativeFilePicker.IsFilePickerBusy())
            return;

        // 這裡指定支援的檔案類型（PDF、PPT、PPTX）
        string[] allowedTypes = new string[]
        {
            "com.adobe.pdf",                          // PDF
            "com.microsoft.powerpoint.ppt",           // PPT
            "org.openxmlformats.presentationml.presentation" // PPTX
        };

        NativeFilePicker.PickFile((path) =>
        {
            if (path != null)
            {
                Debug.Log("Selected file: " + path);
                StartCoroutine(UploadPDF(path));
            }
        }, allowedTypes);
    }

    IEnumerator UploadPDF(string filePath)
    {
        string fileName = Uri.EscapeDataString(Path.GetFileName(filePath));

        classNameText.text = classNameInputField.text;
        classDateText.text = DateTime.Now.ToString("yyyy-MM-dd");

        byte[] fileData = File.ReadAllBytes(filePath);  
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, "application/pdf");
        form.AddField("class_name", classNameInputField.text);
        form.AddField("user_id", PlayerPrefs.GetInt("UserID"));
        form.AddField("course_type", 0);
        Debug.Log("User ID: " + PlayerPrefs.GetInt("UserID"));
        Debug.Log("Class Name: " + classNameInputField.text);

        using (UnityWebRequest request = UnityWebRequest.Post(apiUrl + "/create", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<newAssistantCreateResponse>(request.downloadHandler.text);
                PlayerPrefs.SetInt("Course_ID", response.course_id);
                PlayerPrefs.SetString("Assistant1_ID", response.assistant_id_1);
                PlayerPrefs.SetString("Assistant2_ID", response.assistant_id_2);
                PlayerPrefs.SetString("Thread1_ID", response.thread_id_1);
                PlayerPrefs.SetString("Thread2_ID", response.thread_id_2);
                Debug.Log("Upload Success: " + request.downloadHandler.text);
                Debug.Log("Assistant ID: " + response.assistant_id_1);
                Debug.Log("Thread ID: " + response.thread_id_1);
                Debug.Log("Assistant ID: " + response.assistant_id_2);
                Debug.Log("Thread ID: " + response.thread_id_2);

                if (previewDiscussionPanel != null)
                {
                    previewDiscussionPanel.SetActive(true);
                    updatePagePanel.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("Upload Failed: " + request.error);
            }
        }
    }

    IEnumerator CopyDemoFilesToDocuments()
    {
        string[] demoFiles = { "追求最佳而非最低的員工流動率.pdf" }; // 你的 demo 檔案名稱
        foreach (string fileName in demoFiles)
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, fileName);
            string destPath = Path.Combine(Application.persistentDataPath, fileName);

            string uri = "file://" + sourcePath;
            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(uri);
            yield return www.SendWebRequest();
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                if (!File.Exists(destPath)) // 避免覆蓋用戶自己存的檔案
                    File.WriteAllBytes(destPath, www.downloadHandler.data);
            }
        }
        Debug.Log("Demo files copied to Documents!");
    }
}

[System.Serializable]//oO �i�ǦC�ƪ�
public class newAssistantCreateResponse
{
    public string action;
    public int course_id;
    public string assistant_id_1;
    public string thread_id_1;
    public string assistant_id_2;
    public string thread_id_2;
}
