using UnityEngine;

public class PageManager : MonoBehaviour
{
    public GameObject[] panels;  // 所有頁面的Panel

    public void ShowPage(GameObject pageToShow)
    {
        foreach (var panel in panels)
        {
            panel.SetActive(panel == pageToShow);
        }
    }
}
