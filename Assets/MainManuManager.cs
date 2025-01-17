using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void GoToHome()
    {
        UnityEngine.Debug.Log("Click HomeButton");
    }

    public void GoToRanking()
    {
        UnityEngine.Debug.Log("Click RankButton");
    }

    public void OpenAddMenu()
    {
        UnityEngine.Debug.Log("Click AddButton");
    }

    public void GoToCourse()
    {
        UnityEngine.Debug.Log("Click CourseButton");
    }

    public void GoToLottery()
    {
        UnityEngine.Debug.Log("Click LotteryButton");
    }

    public void GoToProfile()
    {
        UnityEngine.Debug.Log("Click ProfileButton");
        SceneManager.LoadScene("ProfilePageScene");
    }
}