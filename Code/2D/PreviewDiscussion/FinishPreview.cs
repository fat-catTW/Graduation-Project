using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using TMPro;


public class FinishPreview : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void FinishPreviewCourse()
    {
        StartCoroutine(MarkCourseVrReady(PlayerPrefs.GetInt("Course_ID")));
    }

    IEnumerator MarkCourseVrReady(int courseId)
    {
        string url = "http://your-server.com/activate_VR";

        var payload = new { 
            action = "activate_VR",
            course_id = courseId 
            };
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Course marked as VR Ready!");
        else
            Debug.LogError("Failed to mark course: " + request.error);
    }
}
