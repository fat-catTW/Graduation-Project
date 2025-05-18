using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

public class SwipeableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private RectTransform deleteButtonRect;

    [Header("Settings")]
    [SerializeField] private float swipeThreshold = 50f;
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private float minMovementThreshold = 1f;  // 最小移動閾值
    [SerializeField] private float smoothTime = 0.1f;  // 平滑過渡時間

    private float startPosX;
    private float currentPosX;
    private float targetPosX;
    private float velocityX;
    private bool isDragging;
    private float contentWidth;
    private float buttonWidth;
    private Vector2 lastPointerPosition;

    private void Awake()
    {
        // 獲取內容寬度和按鈕寬度
        contentWidth = contentRect.rect.width;
        buttonWidth = deleteButtonRect.rect.width;

        // 初始化按鈕位置
        deleteButtonRect.anchoredPosition = new Vector2((contentWidth - buttonWidth) / 2, 0);

        // 確保內容在正確位置
        contentRect.anchoredPosition = Vector2.zero;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        startPosX = contentRect.anchoredPosition.x;
        lastPointerPosition = eventData.position;
        velocityX = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float deltaX = eventData.position.x - lastPointerPosition.x;
        lastPointerPosition = eventData.position;

        if (Mathf.Abs(deltaX) < minMovementThreshold)
        {
            return;
        }

        // 直接根據手指移動
        float newPosX = contentRect.anchoredPosition.x + deltaX;
        newPosX = Mathf.Clamp(newPosX, -buttonWidth / 2, 0);
        contentRect.anchoredPosition = new Vector2(newPosX, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        float endPosX = contentRect.anchoredPosition.x;

        // 只要滑動超過按鈕寬度的 1/4 就展開
        if (endPosX < -buttonWidth / 4)
        {
            OpenLeftSide();
        }
        else
        {
            Close();
        }
    }

    private void OpenLeftSide()
    {
        contentRect.DOAnchorPosX(-buttonWidth / 2, animationDuration);
    }

    private void Close()
    {
        contentRect.DOAnchorPosX(0, animationDuration);
    }

    // 刪除按鈕點擊事件
    public void OnDeleteButtonClick()
    {
        Debug.Log("Delete item");
        // 在這裡實現刪除邏輯
    }
}
