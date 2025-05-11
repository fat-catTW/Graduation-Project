using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class LotteryManager : MonoBehaviour
{
    public static LotteryManager Instance { get; private set; }

    [Header("抽卡面板")]
    public GameObject lotteryPanel;          // 抽卡主面板
    public GameObject drawResultPanel;       // 抽卡結果面板

    [Header("資源顯示")]
    public TMP_Text coinsText;              // 金幣數量
    public TMP_Text diamondsText;           // 鑽石數量

    [Header("抽卡按鈕")]
    public Button normalDrawButton;          // 普通抽取按鈕 (500金幣)
    public Button premiumDrawButton;         // 高級抽取按鈕 (3鑽石)
    public Button closeResultButton;         // 關閉結果面板按鈕

    [Header("卡片圖片")]
    public Image resultCardImage;            // 抽卡結果顯示的圖片
    public Sprite cardBackSprite;           // 卡片背面圖片

    [Header("卡片圖片設置")]
    public Sprite goblinSprite;             // 哥布林
    public Sprite athenaSprite;             // 雅典娜
    public Sprite pirateSprite;             // 海盜
    public Sprite teacherSprite;            // 盛惟老師
    public Sprite santaSprite;              // 聖誕老公公
    public Sprite popeSprite;               // 教宗

    [Header("提示面板")]
    public GameObject insufficientFundsPanel;    // 餘額不足提示面板

    private Dictionary<string, Sprite> cardSprites;
    private Sprite drawnCardSprite;         // 暫存抽到的卡片圖片

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeCardSprites();
    }

    void InitializeCardSprites()
    {
        cardSprites = new Dictionary<string, Sprite>
        {
            { "哥布林", goblinSprite },
            { "雅典娜", athenaSprite },
            { "海盜", pirateSprite },
            { "盛惟老師", teacherSprite },
            { "聖誕老公公", santaSprite },
            { "教宗", popeSprite }
        };
    }

    void Start()
    {
        normalDrawButton.onClick.AddListener(() => StartCoroutine(OnNormalDraw()));
        premiumDrawButton.onClick.AddListener(() => StartCoroutine(OnPremiumDraw()));
        closeResultButton.onClick.AddListener(CloseResultPanel);

        drawResultPanel.SetActive(false);
        insufficientFundsPanel.SetActive(false);
        UpdateResourceDisplay();
    }

    void OnEnable()
    {
        UpdateResourceDisplay();
    }

    public void UpdateResourceDisplay()
    {
        int coins = PlayerPrefs.GetInt("Coins", 0);
        int diamonds = PlayerPrefs.GetInt("Diamonds", 0);
        coinsText.text = coins.ToString();
        diamondsText.text = diamonds.ToString();
    }

    private IEnumerator OnNormalDraw()
    {
        int currentCoins = PlayerPrefs.GetInt("Coins", 0);
        if (currentCoins < 500)
        {
            insufficientFundsPanel.SetActive(true);
            yield break;
        }

        normalDrawButton.interactable = false;
        yield return StartCoroutine(APIManager.Instance.DrawCard(false));
        normalDrawButton.interactable = true;
        
        UpdateResourceDisplay();
    }

    private IEnumerator OnPremiumDraw()
    {
        int currentDiamonds = PlayerPrefs.GetInt("Diamonds", 0);
        if (currentDiamonds < 3)
        {
            insufficientFundsPanel.SetActive(true);
            yield break;
        }

        premiumDrawButton.interactable = false;
        yield return StartCoroutine(APIManager.Instance.DrawCard(true));
        premiumDrawButton.interactable = true;
        
        UpdateResourceDisplay();
    }

    public void ShowDrawResult(string cardName, string rarity)
    {
        drawResultPanel.SetActive(true);

        if (cardSprites.TryGetValue(cardName, out Sprite cardSprite))
        {
            drawnCardSprite = cardSprite;  // 暫存抽到的卡片
            resultCardImage.sprite = cardBackSprite;  // 先顯示背面
        }

        PlayDrawAnimation();
    }

    private void PlayDrawAnimation()
    {
        // 重置卡片的位置、縮放和旋轉
        resultCardImage.rectTransform.localScale = Vector3.zero;
        resultCardImage.rectTransform.localRotation = Quaternion.identity;
        resultCardImage.rectTransform.anchoredPosition = new Vector2(0, -200); // 初始位置往下移一點

        // 創建動畫序列
        Sequence sequence = DOTween.Sequence();

        float rotationDuration = 0.6f;  // 旋轉動畫總時間
        float switchImageTime = rotationDuration * 0.5f;  // 在旋轉到一半時切換圖片

        // 卡片從下方彈出並旋轉，目標位置往上移一點
        sequence.Append(resultCardImage.rectTransform.DOScale(1f, 0.4f).SetEase(Ease.OutBack))  // 縮放動畫
                .Join(resultCardImage.rectTransform.DOAnchorPosY(50, 0.4f).SetEase(Ease.OutBack))  // 向上移動到 y=50 的位置
                .Join(resultCardImage.rectTransform.DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360));  // 旋轉一圈

        // 在旋轉到一半時切換到正面圖片
        sequence.InsertCallback(switchImageTime, () => {
            resultCardImage.sprite = drawnCardSprite;
        });
    }

    private void CloseResultPanel()
    {
        drawResultPanel.SetActive(false);
        resultCardImage.sprite = cardBackSprite;  // 重置為背面圖片
        
        // 重置卡片的位置、縮放和旋轉
        resultCardImage.rectTransform.localScale = Vector3.zero;
        resultCardImage.rectTransform.localRotation = Quaternion.identity;
        resultCardImage.rectTransform.anchoredPosition = new Vector2(0, -200);
    }
}
