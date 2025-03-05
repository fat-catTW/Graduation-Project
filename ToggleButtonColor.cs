using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonColor : MonoBehaviour
{
    [Header("按鈕參考")]
    public Button dayButton;   // 日按鈕
    public Button monthButton; // 月按鈕

    [Header("顏色設定")]
    public Color activeColor = new Color(0.5f, 0, 0.5f); // 紫色（啟用狀態）
    public Color inactiveColor = Color.white;           // 白色（非啟用狀態）

    private void Start()
    {
        // 初始狀態：日為啟用（紫色），月為非啟用（白色）
        SetButtonColors(dayButton, activeColor);
        SetButtonColors(monthButton, inactiveColor);

        // 為按鈕設定點擊事件
        dayButton.onClick.AddListener(OnDayButtonClicked);
        monthButton.onClick.AddListener(OnMonthButtonClicked);
    }

    void OnDayButtonClicked()
    {
        // 當點擊「日」按鈕時，將日設為啟用，月設為非啟用
        SetButtonColors(dayButton, activeColor);
        SetButtonColors(monthButton, inactiveColor);
        // 如有其他切換邏輯，可在這裡加入
    }

    void OnMonthButtonClicked()
    {
        // 當點擊「月」按鈕時，將月設為啟用，日設為非啟用
        SetButtonColors(monthButton, activeColor);
        SetButtonColors(dayButton, inactiveColor);
        // 如有其他切換邏輯，可在這裡加入
    }

    // 設定按鈕的 ColorBlock
    void SetButtonColors(Button button, Color color)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.pressedColor = color * 0.9f;  // 略深一點，表示按下狀態
        colors.selectedColor = color;
        button.colors = colors;
    }
}
