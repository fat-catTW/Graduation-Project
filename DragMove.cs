using UnityEngine;
using UnityEngine.EventSystems;

public class DragMove : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RectTransform whitePanel;  // 可拖動的目標
    public float moveDistance = 500f;
    private bool isMoved = false;

    // 新增：儲存原始位置
    private Vector2 originalPosition;

    void Start()
    {
        // 進入場景時，先記錄 whitePanel 原本的 anchoredPosition
        if (whitePanel != null)
            originalPosition = whitePanel.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (whitePanel == null) return;

        // 往上拖動（y > 20）
        if (eventData.delta.y > 20 && !isMoved)
        {
            whitePanel.anchoredPosition += new Vector2(0, moveDistance);
            isMoved = true;
        }
        // 往下拖動（y < -20）
        else if (eventData.delta.y < -20 && isMoved)
        {
            whitePanel.anchoredPosition -= new Vector2(0, moveDistance);
            isMoved = false;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 結束拖動時，如果還有要做的事可以寫在這裡
    }

    // 當此物件或其父物件被設為 inactive 時，會呼叫 OnDisable
    void OnDisable()
    {
        // 只要頁面被關閉或這個物件被停用，就自動把白板位置重置回原點
        if (whitePanel != null)
            whitePanel.anchoredPosition = originalPosition;
        isMoved = false;
    }
}
