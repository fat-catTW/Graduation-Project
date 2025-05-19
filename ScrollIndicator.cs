using UnityEngine;
using UnityEngine.UI;

public class ScrollIndicator : MonoBehaviour
{
    public ScrollRect scrollRect; // Scroll View
    public Image[] dots; // 輪播指示器點點
    public Color activeColor = Color.white; // 當前頁面點點顏色
    public Color inactiveColor = Color.gray; // 其他點點顏色
    public int totalPages = 4; // 總頁數

    private void Update()
    {
        float scrollPos = scrollRect.horizontalNormalizedPosition; // 取得當前滾動位置
        int currentPage = Mathf.RoundToInt(scrollPos * (totalPages - 1)); // 計算當前頁面索引

        // 確保頁面索引在合理範圍內
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // 更新點點顏色
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].color = (i == currentPage) ? activeColor : inactiveColor;
        }
    }
}
