using UnityEngine;
using UnityEngine.SceneManagement;

public class ProfilePageManager : MonoBehaviour
{
    public void BackToHome()
    {
        UnityEngine.Debug.Log("Click BackButton");
        SceneManager.LoadScene("Scenes/HomePageScene");
    }
}