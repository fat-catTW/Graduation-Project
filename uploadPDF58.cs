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

    public TMP_InputField classNameInputFieldPDF;
    public TMP_InputField classNameInputFieldPPT;
    public TMP_Text classNameText;
    public TMP_Text classDateText;
    public GameObject previewPagePanel;
    public GameObject updatePagePanel;

    public TMP_InputField uploadTextInputField;

    public GameObject loadingPagePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void triggerUploadText()
    {
        print("Starting a course with text uploaded.");
        StartCoroutine(UploadText());
    }


    public void triggerUploadPDF()
    {
        if (NativeFilePicker.IsFilePickerBusy())
            return;

        string[] allowedTypes = new string[]
        {
            "public.pdf",                          // PDF for iOS
            "com.adobe.pdf"                        // PDF for other platforms
        };

        NativeFilePicker.PickFile((path) =>
        {
            if (path != null)
            {
                Debug.Log("Selected PDF: " + path);
                StartCoroutine(UploadFile(path, "pdf", classNameInputFieldPDF.text));
                classNameInputFieldPDF.text = "";
            }
        }, allowedTypes);
    }

    public void triggerUploadPPT()
    {
        if (NativeFilePicker.IsFilePickerBusy())
            return;

        string[] allowedTypes = new string[]
        {
            "com.microsoft.powerpoint.ppt",           // PPT for iOS
            "org.openxmlformats.presentationml.presentation", // PPTX for iOS
            ".ppt",                                  // PPT for other platforms
            ".pptx"                                  // PPTX for other platforms
        };

        NativeFilePicker.PickFile((path) =>
        {
            if (path != null)
            {
                Debug.Log("Selected PPT: " + path);
                StartCoroutine(UploadFile(path, "ppt", classNameInputFieldPPT.text));
                classNameInputFieldPPT.text = "";
            }
        }, allowedTypes);
    }

    IEnumerator UploadFile(string filePath, string course_format, string class_name)
    {
        loadingPagePanel.SetActive(true);
        string fileName = Uri.EscapeDataString(Path.GetFileName(filePath));

        classNameText.text = class_name;
        classDateText.text = DateTime.Now.ToString("yyyy-MM-dd");

        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", fileData, fileName, "application/pdf");
        form.AddField("class_name", class_name);
        form.AddField("user_id", PlayerPrefs.GetInt("UserID"));
        form.AddField("course_type", 0);
        form.AddField("course_format", course_format);
        Debug.Log("User ID: " + PlayerPrefs.GetInt("UserID"));
        Debug.Log("Class Name: " + class_name);

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

                if (previewPagePanel != null)
                {
                    previewPagePanel.SetActive(true);
                    updatePagePanel.SetActive(false);
                    previewPagePanel.GetComponent<Chat>().InitPreviewPagePanel();
                }
            }
            else
            {
                Debug.LogError("Upload Failed: " + request.error);
            }
        }
    }

    IEnumerator UploadText()
    {
        loadingPagePanel.SetActive(true);

        classDateText.text = DateTime.Now.ToString("yyyy-MM-dd");

        WWWForm form = new WWWForm();
        form.AddField("class_name", "文字課程");
        form.AddField("user_id", PlayerPrefs.GetInt("UserID"));
        form.AddField("course_type", -1);
        form.AddField("course_format", "Text");
        form.AddField("course_context", uploadTextInputField.text);
        uploadTextInputField.text = "";
        Debug.Log("User ID: " + PlayerPrefs.GetInt("UserID"));
        Debug.Log("Class Name: " + "文字課程");

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

                if (previewPagePanel != null)
                {
                    previewPagePanel.SetActive(true);
                    updatePagePanel.SetActive(false);
                    previewPagePanel.GetComponent<Chat>().InitPreviewPagePanel();
                }
            }
            else
            {
                Debug.LogError("Upload Failed: " + request.error);
            }
        }
    }



}

[System.Serializable]//�o�����O �i�ǦC�ƪ�
public class newAssistantCreateResponse
{
    public string action;
    public int course_id;
    public string assistant_id_1;
    public string thread_id_1;
    public string assistant_id_2;
    public string thread_id_2;
}
