using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LevelButton : MonoBehaviour
{
    private LevelSelector levelSelector;
    private Image buttonImage;    // 主要的按鈕圖片組件
    
    [Header("UI 組件")]
    [SerializeField] private Image cardImage;      // 卡片正面圖片
    [SerializeField] private Image cardBackImage;  // 卡片背面圖片

    [Header("卡片圖片資源")]
    public Sprite goblinSprite;             // 哥布林
    public Sprite athenaSprite;             // 雅典娜
    public Sprite pirateSprite;             // 海盜
    public Sprite teacherSprite;            // 盛惟老師
    public Sprite santaSprite;              // 聖誕老公公
    public Sprite popeSprite;               // 教宗
    public Sprite cardBackSprite;           // 卡片背面圖片

    private int cardId;
    private bool isOwned;
    private APIManager.UserCardData cardData;

    private void Awake()
    {
        // 獲取或創建必要的組件
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = gameObject.AddComponent<Image>();
            Debug.Log("創建主按鈕 Image 組件");
        }

        // 檢查或創建卡片正面的 Image
        if (cardImage == null)
        {
            GameObject cardFront = new GameObject("CardImage");
            cardFront.transform.SetParent(transform, false);
            cardImage = cardFront.AddComponent<Image>();
            cardImage.raycastTarget = false;  // 不接收點擊事件
            
            // 設置 RectTransform
            RectTransform cardFrontRect = cardFront.GetComponent<RectTransform>();
            cardFrontRect.anchorMin = Vector2.zero;
            cardFrontRect.anchorMax = Vector2.one;
            cardFrontRect.offsetMin = Vector2.zero;
            cardFrontRect.offsetMax = Vector2.zero;
        }

        // 檢查或創建卡片背面的 Image
        if (cardBackImage == null)
        {
            GameObject cardBack = new GameObject("CardBackImage");
            cardBack.transform.SetParent(transform, false);
            cardBackImage = cardBack.AddComponent<Image>();
            cardBackImage.raycastTarget = false;  // 不接收點擊事件
            cardBackImage.sprite = cardBackSprite;
            
            // 設置 RectTransform
            RectTransform cardBackRect = cardBack.GetComponent<RectTransform>();
            cardBackRect.anchorMin = Vector2.zero;
            cardBackRect.anchorMax = Vector2.one;
            cardBackRect.offsetMin = Vector2.zero;
            cardBackRect.offsetMax = Vector2.zero;
        }

        // 檢查所有圖片資源是否已設置
        if (goblinSprite == null) Debug.LogError("哥布林圖片未設置");
        if (athenaSprite == null) Debug.LogError("雅典娜圖片未設置");
        if (pirateSprite == null) Debug.LogError("海盜圖片未設置");
        if (teacherSprite == null) Debug.LogError("盛惟老師圖片未設置");
        if (santaSprite == null) Debug.LogError("聖誕老公公圖片未設置");
        if (popeSprite == null) Debug.LogError("教宗圖片未設置");
        if (cardBackSprite == null) Debug.LogError("卡片背面圖片未設置");
    }

    public void SetupCard(APIManager.UserCardData cardData)
    {
        this.cardData = cardData;
        if (cardData != null)
        {
            this.cardId = cardData.card_id;
            this.isOwned = true;
            Debug.Log($"設置卡片：ID={cardData.card_id}, 名稱={cardData.name}, 是否選中={cardData.is_selected}");

            // 根據卡片名稱獲取對應的 Sprite
            Sprite cardSprite = null;
            switch (cardData.name)
            {
                case "哥布林":
                    cardSprite = goblinSprite;
                    Debug.Log($"使用哥布林圖片: {(goblinSprite != null ? "已找到" : "未找到")}");
                    break;
                case "雅典娜":
                    cardSprite = athenaSprite;
                    Debug.Log($"使用雅典娜圖片: {(athenaSprite != null ? "已找到" : "未找到")}");
                    break;
                case "海盜":
                    cardSprite = pirateSprite;
                    Debug.Log($"使用海盜圖片: {(pirateSprite != null ? "已找到" : "未找到")}");
                    break;
                case "盛惟老師":
                    cardSprite = teacherSprite;
                    Debug.Log($"使用盛惟老師圖片: {(teacherSprite != null ? "已找到" : "未找到")}");
                    break;
                case "聖誕老公公":
                    cardSprite = santaSprite;
                    Debug.Log($"使用聖誕老公公圖片: {(santaSprite != null ? "已找到" : "未找到")}");
                    break;
                case "教宗":
                    cardSprite = popeSprite;
                    Debug.Log($"使用教宗圖片: {(popeSprite != null ? "已找到" : "未找到")}");
                    break;
                default:
                    Debug.LogError($"未知的卡片名稱：{cardData.name}");
                    break;
            }

            if (cardSprite == null)
            {
                Debug.LogError($"卡片 {cardData.name} 的圖片資源為空");
                cardImage.gameObject.SetActive(false);
                cardBackImage.gameObject.SetActive(true);
                cardBackImage.sprite = cardBackSprite;
            }
            else
            {
                cardImage.sprite = cardSprite;
                cardImage.gameObject.SetActive(true);
                cardBackImage.gameObject.SetActive(false);
                
                Debug.Log($"卡片 {cardData.name} 設置完成");
            }
        }
        else
        {
            this.cardId = -1;
            this.isOwned = false;
            cardImage.gameObject.SetActive(false);
            cardBackImage.gameObject.SetActive(true);
            if (cardBackImage.sprite == null)
            {
                cardBackImage.sprite = cardBackSprite;
            }
            Debug.Log("設置未擁有的卡片背面");
        }
    }

    public void RegisterLevelSelector(LevelSelector levelSelector)
    {
        this.levelSelector = levelSelector;
    }

    // 播放选中特效
    public void PlaySelectEffect()
    {
        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        
        // 添加跳動動畫
        sequence.Append(transform.DOScale(1.2f, 0.2f))  // 放大
               .Append(transform.DOScale(1f, 0.2f));    // 縮小
    }

    public int GetCardId() => cardId;
    public bool IsOwned() => isOwned;

    public void SetToggleState(bool isOn, bool shouldNotify = true)
    {
        if (shouldNotify && levelSelector != null)
        {
            levelSelector.OnCardSelected(this, isOn);
        }
    }

    public APIManager.UserCardData GetCardData()
    {
        return cardData;
    }

    public void UpdateSelectedState(bool isSelected)
    {
        if (cardData != null)
        {
            // 只有在卡片從未選中變為選中時才播放動畫
            if (!cardData.is_selected && isSelected)
            {
                PlaySelectEffect();
            }
            cardData.is_selected = isSelected;
        }
    }
}
