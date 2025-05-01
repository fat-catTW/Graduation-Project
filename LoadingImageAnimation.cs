using UnityEngine;
using DG.Tweening;

public class LoadingImageAnimation : MonoBehaviour 
{
    private RectTransform rectTransform;
    private Sequence bounceSequence;
    
    [Header("动画设置")]
    [SerializeField] private float bounceHeight = 40f;  // 跳跃高度
    [SerializeField] private float upDuration = 0.3f;   // 上升时间
    [SerializeField] private float downDuration = 0.4f; // 下落时间

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        StartBounceAnimation();
    }

    void StartBounceAnimation()
    {
        // 如果已经有动画在播放，先停止它
        if (bounceSequence != null)
        {
            bounceSequence.Kill();
        }

        float originalY = rectTransform.anchoredPosition.y;

        // 创建新的动画序列
        bounceSequence = DOTween.Sequence();

        // 添加弹跳动画
        // 快速上升
        bounceSequence.Append(rectTransform.DOAnchorPosY(originalY + bounceHeight, upDuration)
            .SetEase(Ease.OutQuad));  // 使用 OutQuad 实现简单的快速上升
            
        // 自然下落
        bounceSequence.Append(rectTransform.DOAnchorPosY(originalY, downDuration)
            .SetEase(Ease.InQuad));  // 使用 InQuad 实现简单的下落

        // 设置循环
        bounceSequence.SetLoops(-1);
    }

    void OnDestroy()
    {
        // 确保在销毁时停止动画
        if (bounceSequence != null)
        {
            bounceSequence.Kill();
        }
    }
} 
