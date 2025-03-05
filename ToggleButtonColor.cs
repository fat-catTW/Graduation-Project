using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToggleButtonColor : MonoBehaviour
{
    [Header("按鈕參考")]
    public Button dayButton;   // 日按鈕
    public Button monthButton; // 月按鈕

    [Header("顏色設定")]
    public Color activeButtonColor = new Color(0.5f, 0, 0.5f); // 紫色，啟用狀態
    public Color inactiveButtonColor = Color.white;           // 白色，非啟用狀態

    public Color activeTextColor = Color.white;   // 啟用狀態文字顏色
    public Color inactiveTextColor = Color.black;   // 非啟用狀態文字顏色

    private void Start()
    {
        // 初始狀態：日按鈕為啟用，月按鈕為非啟用
        SetButtonAndTextColors(dayButton, activeButtonColor, activeTextColor);
        SetButtonAndTextColors(monthButton, inactiveButtonColor, inactiveTextColor);

        // 為按鈕設定點擊事件
        dayButton.onClick.AddListener(OnDayButtonClicked);
        monthButton.onClick.AddListener(OnMonthButtonClicked);
    }

    void OnDayButtonClicked()
    {
        SetButtonAndTextColors(dayButton, activeButtonColor, activeTextColor);
        SetButtonAndTextColors(monthButton, inactiveButtonColor, inactiveTextColor);
        // 其他切換邏輯在這裡處理…
    }

    void OnMonthButtonClicked()
    {
        SetButtonAndTextColors(monthButton, activeButtonColor, activeTextColor);
        SetButtonAndTextColors(dayButton, inactiveButtonColor, inactiveTextColor);
        // 其他切換邏輯在這裡處理…
    }

    // 此方法同時更新按鈕的顏色和其子物件中的TMP_Text顏色
    void SetButtonAndTextColors(Button button, Color buttonColor, Color textColor)
    {
        // 更新按鈕背景顏色 (ColorBlock)
        ColorBlock colors = button.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonColor;
        colors.pressedColor = buttonColor * 0.9f; // 略暗表示按下狀態
        colors.selectedColor = buttonColor;
        button.colors = colors;

        // 找到按鈕中的TMP_Text並更新文字顏色
        TMP_Text textComponent = button.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.color = textColor;
        }
    }
}
