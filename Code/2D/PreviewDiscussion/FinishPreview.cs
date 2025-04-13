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
        Debug.Log("Activate VR for course: " + PlayerPrefs.GetInt("Course_ID"));
        StartCoroutine(MarkCourseVrReady(PlayerPrefs.GetInt("Course_ID")));
    }

    IEnumerator MarkCourseVrReady(int courseId)
    {
        string url = "https://feynman-server.onrender.com/activate_VR";
        Debug.Log("The course ID: " + courseId);
        string jsonData = JsonUtility.ToJson(new activateVR
        {
            action = "activate_VR",
            course_id = courseId
        });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
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
}


[System.Serializable]//�o�����O �i�ǦC�ƪ�
public class activateVR
{
    public string action;
    public int course_id;
}
