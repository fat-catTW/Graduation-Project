using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;
using System.Linq;

public class LevelSelector : MonoBehaviour
{
    [Header("UI 組件")]
    public GameObject levelButtonPrefab;
    public Transform levelGroup;
    public int levelCount = 6; // 关卡数量
    public float radius = 300f; // 半径
    public float moveDuration = 1f; // 移动持续时间
    public float yOffsetStep = 80f; // Y轴偏移步长

    public Button nextLevelButton;
    public Button previousLevelButton;
    public Toggle selectButton;

    private List<GameObject> levelButtons = new List<GameObject>();
    private List<float> yOffsets;
    private List<int> sortOrders;
    private float angleStep;
    private float currentAngle = 0f;

    private void Start()
    {
        Debug.Log("LevelSelector Start 方法被調用");
        Debug.Log($"LevelSelector GameObject 名稱: {gameObject.name}");
        Debug.Log($"LevelSelector GameObject 是否啟用: {gameObject.activeInHierarchy}");
        Debug.Log($"LevelSelector 組件是否為空: {this == null}");
        
        InitializeLayout();
        LoadUserCards();
        SetupButtons();
    }

    private void InitializeLayout()
    {
        Debug.Log("初始化卡片佈局...");
        // 计算每个按钮之间的角度
        angleStep = 360f / levelCount;

        // 自动计算对称的Y轴偏移量
        yOffsets = CalculateSymmetricYOffsets(levelCount);
        // 初始化排序顺序
        sortOrders = CalculateRenderingOrder(levelCount);
        
        // 重置當前角度
        currentAngle = 0f;
        
        Debug.Log($"初始化完成：angleStep={angleStep}, currentAngle={currentAngle}");
    }

    public void LoadUserCards()
    {
        try
        {
            // 先清理現有卡片
            ClearCards();
            
            int userId = PlayerPrefs.GetInt("UserID");
            Debug.Log($"正在獲取用戶 {userId} 的卡片數據");
            
            APIManager.Instance.GetUserCards(userId, (cards) =>
            {
                // 創建一個長度為 levelCount 的數組來存儲卡片數據
                APIManager.UserCardData[] cardDataArray = new APIManager.UserCardData[levelCount];
                
                // 如果有卡片數據，將它們放入對應的位置
                if (cards != null && cards.Count > 0)
                {
                    Debug.Log($"獲取到卡片數據：{cards.Count} 張卡片");
                    foreach (var card in cards)
                    {
                        Debug.Log($"卡片信息：ID={card.card_id}, 名稱={card.name}, 是否選中={card.is_selected}");
                        int index = card.card_id - 1; // card_id 從 1 開始，轉換為 0 基索引
                        Debug.Log($"放置卡片 {card.name} 到位置 {index}");
                        if (index >= 0 && index < levelCount)
                        {
                            cardDataArray[index] = card;
                        }
                    }
                }
                else
                {
                    Debug.Log("用戶目前沒有任何卡片，將顯示所有卡片的背面");
                }
                
                // 創建並設置卡片按鈕
                for (int i = 0; i < levelCount; i++)
                {
                    GameObject levelButton = Instantiate(levelButtonPrefab, levelGroup);
                    levelButton.name = $"Level_{i}";
                    
                    LevelButton cardButton = levelButton.GetComponent<LevelButton>();
                    if (cardButton == null)
                    {
                        Debug.LogError($"LevelButton 組件未找到於 {levelButton.name}");
                        continue;
                    }
                    
                    cardButton.RegisterLevelSelector(this);
                    
                    // 設置卡片數據（如果該位置有數據則使用數據，否則傳 null）
                    var cardData = cardDataArray[i];
                    Debug.Log($"位置 {i} 的卡片數據：{(cardData != null ? cardData.name : "未擁有")}");
                    cardButton.SetupCard(cardData);
                    
                    levelButtons.Add(levelButton);
                }
                
                // 更新按鈕位置
                UpdateButtonPositions();
                
                // 更新 Toggle 按鈕狀態
                UpdateToggleButtonState();
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load user cards: {e.Message}");
        }
    }

    private void SetupButtons()
    {
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(() => RotateButtons(1));

        if (previousLevelButton != null)
            previousLevelButton.onClick.AddListener(() => RotateButtons(-1));

        if (selectButton != null)
            selectButton.onValueChanged.AddListener(OnSelectButtonValueChanged);
    }

    private void OnSelectButtonValueChanged(bool isOn)
    {
        // 添加檢查，確保 sortOrders 列表不為空且有足夠的元素
        if (sortOrders == null || sortOrders.Count == 0 || levelButtons.Count == 0)
        {
            Debug.LogWarning("sortOrders 列表為空或 levelButtons 列表為空，無法處理選擇事件");
            return;
        }

        int bottomIndex = sortOrders.IndexOf(sortOrders.Count / 2);
        if (bottomIndex < 0 || bottomIndex >= levelButtons.Count)
        {
            Debug.LogWarning($"bottomIndex {bottomIndex} 超出範圍，無法處理選擇事件");
            return;
        }

        var currentCard = levelButtons[bottomIndex].GetComponent<LevelButton>();
        if (currentCard == null)
        {
            Debug.LogWarning("無法獲取當前卡片的 LevelButton 組件");
            return;
        }

        try
        {
            if (isOn)
            {
                // 獲取當前卡片數據
                var cardData = currentCard.GetCardData();
                if (cardData == null) return;

                // 如果已經選中，不做任何操作
                if (cardData.is_selected) return;

                // 取消其他卡片的選中狀態
                foreach (var button in levelButtons)
                {
                    if (button == null) continue;
                    LevelButton levelButton = button.GetComponent<LevelButton>();
                    if (levelButton != null && levelButton != currentCard)
                    {
                        levelButton.UpdateSelectedState(false);
                    }
                }

                // 更新當前卡片的選中狀態並播放動畫
                currentCard.UpdateSelectedState(true);
                currentCard.PlaySelectEffect();

                // 調用API選擇卡片
                StartCoroutine(APIManager.Instance.SelectTeacherCard(cardData.card_id, (response) =>
                {
                    if (response == null || !response.success)
                    {
                        // 如果API調用失敗，恢復之前的狀態
                        currentCard.UpdateSelectedState(false);
                        selectButton.isOn = false;
                    }
                    else
                    {
                        // API 調用成功，通知 ProfileManager 更新卡片
                        if (ProfileManager.Instance != null)
                        {
                            ProfileManager.Instance.UpdateTeacherCard();
                        }
                    }
                }));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to select teacher card: {e.Message}");
            selectButton.isOn = false;
        }
    }

    public void ClearCards()
    {
        Debug.Log("開始清理卡片數據...");
        
        // 停止所有動畫
        DOTween.Kill(levelGroup);
        
        // 銷毀所有卡片按鈕
        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        
        // 清空列表
        levelButtons.Clear();
        
        // 重置角度和排序順序
        currentAngle = 0f;
        
        // 初始化或重置 sortOrders 和 yOffsets
        if (sortOrders == null)
        {
            sortOrders = new List<int>();
        }
        else
        {
            sortOrders.Clear();
        }
        
        if (yOffsets == null)
        {
            yOffsets = new List<float>();
        }
        else
        {
            yOffsets.Clear();
        }
        
        // 重新初始化佈局
        InitializeLayout();
        
        // 重置按鈕狀態，但不觸發事件
        if (selectButton != null)
        {
            selectButton.onValueChanged.RemoveListener(OnSelectButtonValueChanged);
            selectButton.isOn = false;
            selectButton.interactable = false;
            selectButton.onValueChanged.AddListener(OnSelectButtonValueChanged);
        }
        
        Debug.Log("卡片數據清理完成");
    }

    public void ReloadCards()
    {
        Debug.Log("重新加載卡片數據...");
        ClearCards();
        LoadUserCards();
    }

    void UpdateButtonPositions()
    {
        if (levelButtons == null || levelButtons.Count == 0) return;
        
        for (int i = 0; i < levelButtons.Count; i++)
        {
            if (levelButtons[i] == null) continue;
            
            float angle = (360f / levelButtons.Count) * i;
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;

            // 获取计算后的Y轴偏移
            float yOffset = i < yOffsets.Count ? yOffsets[i] : 0;

            // 设置按钮位置
            RectTransform rectTransform = levelButtons[i].GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition3D = new Vector3(x, yOffset, z);
            }

            // 设置Canvas的sortingOrder
            Canvas canvas = levelButtons[i].GetComponent<Canvas>();
            if (canvas != null && i < sortOrders.Count)
            {
                canvas.sortingOrder = sortOrders[i];
            }

            // 设置透明度和缩放值
            SetButtonAppearance(levelButtons[i], i < sortOrders.Count ? sortOrders[i] : 0);
        }

        // 打印最上层和最下层按钮的名称
        PrintTopButtonName();
        PrintBottomButtonName();
    }
    void RotateButtons(int direction)
    {
        // 只禁用被點擊的按鈕
        if (direction > 0)
        {
            nextLevelButton.interactable = false;
        }
        else
        {
            previousLevelButton.interactable = false;
        }

        // 檢查是否有卡片可以旋轉
        if (levelButtons == null || levelButtons.Count == 0 || sortOrders == null || sortOrders.Count == 0)
        {
            Debug.LogWarning("沒有卡片可以旋轉");
            return;
        }

        List<Vector3> targetPositions = new List<Vector3>();
        List<float> targetYOffsets = new List<float>();
        List<int> newSortOrders = new List<int>();

        // 計算目標位置、Y軸偏移和順序
        for (int i = 0; i < levelButtons.Count; i++)
        {
            int targetIndex = (i + direction + levelCount) % levelCount;
            if (targetIndex < 0 || targetIndex >= levelCount)
            {
                Debug.LogWarning($"無效的目標索引: {targetIndex}");
                continue;
            }

            float angle = currentAngle + angleStep * targetIndex;
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            
            // 確保 yOffsets 有足夠的元素
            float yOffset = targetIndex < yOffsets.Count ? yOffsets[targetIndex] : 0;
            targetPositions.Add(new Vector3(x, yOffset, z));
            targetYOffsets.Add(yOffset);
            
            // 確保 sortOrders 有足夠的元素
            int sortOrder = targetIndex < sortOrders.Count ? sortOrders[targetIndex] : 0;
            newSortOrders.Add(sortOrder);
        }

        // 更新排序順序
        sortOrders = newSortOrders;
        for (int i = 0; i < levelButtons.Count; i++)
        {
            if (i >= levelButtons.Count || i >= sortOrders.Count) continue;
            
            Canvas canvas = levelButtons[i].GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = sortOrders[i];
                // 更新透明度和縮放值
                SetButtonAppearance(levelButtons[i], sortOrders[i]);
            }
        }

        // 平滑移動並更新Y軸偏移和排序順序
        for (int i = 0; i < levelButtons.Count; i++)
        {
            if (i >= levelButtons.Count || i >= targetPositions.Count) continue;
            
            RectTransform rectTransform = levelButtons[i].GetComponent<RectTransform>();
            if (rectTransform == null) continue;
            
            int index = i;
            // 平滑移動位置
            rectTransform.DOAnchorPos3D(targetPositions[i], moveDuration).OnComplete(() =>
            {
                // 更新Y軸偏移
                if (index < yOffsets.Count)
                {
                    yOffsets[index] = targetYOffsets[index];
                }
                if (index == levelButtons.Count - 1)
                {
                    Debug.Log("完成移動");
                    // 只重新啟用之前被禁用的按鈕
                    if (direction > 0)
                    {
                        nextLevelButton.interactable = true;
                    }
                    else
                    {
                        previousLevelButton.interactable = true;
                    }

                    // 更新 Toggle 按鈕狀態
                    UpdateToggleButtonState();
                }
            });
        }

        // 更新角度
        currentAngle += angleStep * direction;
    }
    void PrintTopButtonName()
    {
        int topIndex = sortOrders.IndexOf(0); // 假设排序顺序0是最上层
        if (topIndex >= 0 && topIndex < levelButtons.Count)
        {
            string topButtonName = levelButtons[topIndex].name;
            Debug.Log("最上层按钮的名称: " + topButtonName);
        }
    }
    void PrintBottomButtonName()
    {
        int bottomIndex = sortOrders.IndexOf(sortOrders.Count / 2);
        if (bottomIndex >= 0 && bottomIndex < levelButtons.Count)
        {
            string bottomButtonName = levelButtons[bottomIndex].name;
            Debug.Log("最下层按钮的名称: " + bottomButtonName);
        }
    }
    List<float> CalculateSymmetricYOffsets(int count)
    {
        List<float> offsets = new List<float>();
        float step = yOffsetStep;
        int half = count / 2;
        float y = 0;
        for (int i = 0; i < count; i++)
        {
            if (i != 0)
            {
                y = i * step;
                if (i > half)
                {
                    y = (count - i) * step;
                }
            }
            offsets.Add(y);
        }
        return offsets;
    }
    List<int> CalculateRenderingOrder(int count)
    {
        List<int> order = new List<int>();
        int middle = count / 2;
        int number = 0;
        for (int i = 0; i < count; i++)
        {
            if (i <= middle)
            {
                number = middle - i;
            }
            else
            {
                number = order[count - i];
            }
            order.Add(number);
        }
        return order;
    }
    void SetButtonAppearance(GameObject button, int sortOrder)
    {
        LevelButton levelButton = button.GetComponent<LevelButton>();
        float scale = Mathf.Lerp(0.8f, 1f, (float)sortOrder / (levelCount / 2));
        
        // 設置透明度
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            // 如果是未擁有的卡片（顯示背面），則保持完全不透明
            if (!levelButton.IsOwned())
            {
                canvasGroup.alpha = 1f;
            }
            else
            {
                // 已擁有的卡片才使用漸變透明度
                canvasGroup.alpha = Mathf.Lerp(0.3f, 1f, (float)sortOrder / (levelCount / 2));
            }
        }

        // 設置縮放值
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = new Vector3(scale, scale, scale);
        }
    }
    public int GetCurrentBottomIndex()
    {
        int bottomIndex = sortOrders.IndexOf(sortOrders.Count / 2);
        if (bottomIndex >= 0 && bottomIndex < levelButtons.Count)
        {
            return int.Parse(levelButtons[bottomIndex].name);
        }
        return -1; // 返回-1表示无效索引
    }

    // 處理卡片選中事件
    public void OnCardSelected(LevelButton selectedButton, bool isSelected)
    {
        Debug.Log($"卡片 {selectedButton.GetCardId()} 被{(isSelected ? "選中" : "取消選中")}");
        
        // 如果是選中狀態，取消其他卡片的選中狀態
        if (isSelected)
        {
            foreach (var buttonObj in levelButtons)
            {
                LevelButton button = buttonObj.GetComponent<LevelButton>();
                if (button != selectedButton && button != null)
                {
                    button.SetToggleState(false, false);
                }
            }
        }
    }

    private void UpdateToggleButtonState()
    {
        int bottomIndex = sortOrders.IndexOf(sortOrders.Count / 2);
        if (bottomIndex >= 0 && bottomIndex < levelButtons.Count)
        {
            LevelButton currentCard = levelButtons[bottomIndex].GetComponent<LevelButton>();
            
            // 更新 Toggle 按鈕狀態
            if (selectButton != null)
            {
                // 如果卡片未擁有，禁用按鈕
                if (!currentCard.IsOwned())
                {
                    selectButton.interactable = false;
                    selectButton.isOn = false;
                }
                else
                {
                    APIManager.UserCardData cardData = currentCard.GetCardData();
                    if (cardData != null)
                    {
                        // 如果卡片已被選中，禁用按鈕但保持選中狀態
                        if (cardData.is_selected)
                        {
                            selectButton.interactable = false;
                            selectButton.isOn = true;
                        }
                        else
                        {
                            // 如果卡片未被選中，啟用按鈕
                            selectButton.interactable = true;
                            selectButton.isOn = false;
                        }
                    }
                }
            }
        }
    }
}
