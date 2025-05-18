using UnityEngine;

public class YoutubeOpener : MonoBehaviour
{
    // 替換成你想跳轉的 YouTube 影片連結
    public string youtubeURL = "https://youtu.be/dQw4w9WgXcQ?si=958HQQaCaLjAmno8";

    public void OpenYoutube()
    {
        Application.OpenURL(youtubeURL);
    }
}
