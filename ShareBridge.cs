
using System.Runtime.InteropServices;
using UnityEngine;

public class ShareBridge : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShareText(string message);
#endif

    public static void Share(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass pluginClass = new AndroidJavaClass("com.yourcompany.plugin.Share");
            pluginClass.CallStatic("shareText", activity, message);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        ShareText(message);
#else
        Debug.Log("Sharing is only available on mobile devices.");
#endif
    }
}
