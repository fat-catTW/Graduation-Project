using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LayoutComponent : MonoBehaviour
{
    private RectTransform rectTransform;
    private GameObject secondMenu;
    private Toggle firstToggle;

    private void Awake()
    {
        firstToggle = GetComponent<Toggle>();
        rectTransform = GetComponent<RectTransform>();
        secondMenu = rectTransform.GetChild(1).gameObject;
    }

    private void Start()
    {
        firstToggle.onValueChanged.AddListener((canOpen) => OpenSecondMenu(canOpen));
    }

    public void OpenSecondMenu(bool canOpen)
    {
        secondMenu.gameObject.SetActive(canOpen);
        StartCoroutine(UpdateLayout(rectTransform));
    }

    IEnumerator UpdateLayout(RectTransform rect)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        yield return new WaitForEndOfFrame();
    }
}
