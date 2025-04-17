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

        // 計算移動距離
        float deltaX = eventData.position.x - lastPointerPosition.x;
        lastPointerPosition = eventData.position;

        // 如果移動距離小於最小閾值，忽略這次移動
        if (Mathf.Abs(deltaX) < minMovementThreshold)
        {
            return;
        }

        // 計算目標位置
        targetPosX = startPosX + deltaX;

        // 限制只能往左滑動
        targetPosX = Mathf.Clamp(targetPosX, -buttonWidth / 2, 0);

        // 使用 SmoothDamp 平滑過渡到目標位置
        currentPosX = Mathf.SmoothDamp(currentPosX, targetPosX, ref velocityX, smoothTime);

        // 更新位置
        contentRect.anchoredPosition = new Vector2(currentPosX, 0);

        // 更新起始位置，為下一次移動做準備
        startPosX = currentPosX;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        float velocity = eventData.delta.x;

        // 只有當滑動距離足夠時才打開刪除按鈕
        if (Mathf.Abs(velocity) > swipeThreshold && velocity < 0)
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
