using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using Unity.VisualScripting;

public class LevelSelector : MonoBehaviour
{
    public GameObject levelButtonPrefab; // 关卡按钮的Prefab

    public Transform levelGroup;
    public int levelCount = 6; // 关卡数量
    public float radius = 300f; // 半径
    public float moveDuration = 1f; // 移动持续时间
    public float yOffsetStep = 80f; // Y轴偏移步长

    public Button nextLevelButton;
    public Button previousLevelButton;

    private List<GameObject> levelButtons = new List<GameObject>();
    private List<float> yOffsets;
    private List<int> sortOrders;
    private float angleStep;
    private float currentAngle = 0f;
    void Start()
    {
        // 计算每个按钮之间的角度
        angleStep = 360f / levelCount;

        // 自动计算对称的Y轴偏移量
        yOffsets = CalculateSymmetricYOffsets(levelCount);
        // 初始化排序顺序
        sortOrders = CalculateRenderingOrder(levelCount);

        // 创建关卡按钮并设置它们的位置
        for (int i = 0; i < levelCount; i++)
        {
            GameObject levelButton = Instantiate(levelButtonPrefab, levelGroup);
            levelButton.gameObject.name = i.ToString();
            levelButton.GetComponentInChildren<Text>().text = i.ToString();
            levelButton.GetComponent<LevelButton>().RegisterLevelSelector(this);
            levelButtons.Add(levelButton);
        }

        UpdateButtonPositions();

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
        }

        if (previousLevelButton != null)
        {
            previousLevelButton.onClick.AddListener(OnPreviousLevelButtonClicked); // 添加上一关按钮的监听器
        }
    }
    void UpdateButtonPositions()
    {
        for (int i = 0; i < levelButtons.Count; i++)
        {
            float angle = (360f / levelButtons.Count) * i;
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;

            // 获取计算后的Y轴偏移
            float yOffset = yOffsets[i];

            // 设置按钮位置
            RectTransform rectTransform = levelButtons[i].GetComponent<RectTransform>();
            rectTransform.anchoredPosition3D = new Vector3(x, yOffset, z);

            // 设置Canvas的sortingOrder
            Canvas canvas = levelButtons[i].GetComponent<Canvas>();
            canvas.sortingOrder = sortOrders[i];

            // 设置透明度和缩放值
            SetButtonAppearance(levelButtons[i], sortOrders[i]);
        }

        // 打印最上层和最下层按钮的名称
        PrintTopButtonName();
        PrintBottomButtonName();
    }
    void OnNextLevelButtonClicked()
    {
        RotateButtons(-1);
    }
    void OnPreviousLevelButtonClicked()
    {
        RotateButtons(1);
    }
    Coroutine moveCoroutine;
    public void MoveLevelToBottom(int index)
    {
        int bottomIndex = sortOrders.IndexOf(sortOrders.Count / 2);

        int currentIndex = int.Parse(levelButtons[bottomIndex].name);

        bool isCounterclockwise = true;
        Debug.Log("当前显示 " + currentIndex);
        int movesRequired = index - currentIndex;
        if (movesRequired < 0)
        {
            isCounterclockwise = false;
            movesRequired = -movesRequired;
        }
        Debug.Log("需要偏移" + movesRequired);
        if (moveCoroutine != null)
        {
            return;
        }
        moveCoroutine = StartCoroutine(MoveButtonCoroutine(movesRequired, isCounterclockwise));
    }
    IEnumerator MoveButtonCoroutine(int movesRequired, bool isCounterclockwise)
    {
        int t = 0;
        int diction = isCounterclockwise ? -1 : 1;
        while (movesRequired > t)
        {
            RotateButtons(diction);
            Debug.Log("移动一次");

            yield return new WaitForSeconds(moveDuration);
            t++;
        }
        moveCoroutine = null;
    }
    void RotateButtons(int direction)
    {
        nextLevelButton.interactable = false;
        previousLevelButton.interactable = false;

        List<Vector3> targetPositions = new List<Vector3>();
        List<float> targetYOffsets = new List<float>();
        List<int> newSortOrders = new List<int>();

        // 计算目标位置、Y轴偏移和顺序
        for (int i = 0; i < levelButtons.Count; i++)
        {
            int targetIndex = (i + direction + levelCount) % levelCount;
            float angle = currentAngle + angleStep * targetIndex;
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float yOffset = yOffsets[targetIndex];

            targetPositions.Add(new Vector3(x, yOffset, z));
            targetYOffsets.Add(yOffset);
            newSortOrders.Add(sortOrders[targetIndex]);
        }

        // 更新排序顺序
        sortOrders = newSortOrders;
        for (int i = 0; i < levelButtons.Count; i++)
        {
            Canvas canvas = levelButtons[i].GetComponent<Canvas>();
            canvas.sortingOrder = sortOrders[i];
            // 更新透明度和缩放值
            SetButtonAppearance(levelButtons[i], sortOrders[i]);
        }

        // 平滑移动并更新Y轴偏移和排序顺序
        for (int i = 0; i < levelButtons.Count; i++)
        {
            RectTransform rectTransform = levelButtons[i].GetComponent<RectTransform>();
            int index = i;

            // 平滑移动位置
            rectTransform.DOAnchorPos3D(targetPositions[i], moveDuration).OnComplete(() =>
            {
                // 更新Y轴偏移
                if (index < yOffsets.Count)
                {
                    yOffsets[index] = targetYOffsets[index];
                }
                if (index == levelButtons.Count - 1)
                {
                    Debug.Log("完成移动");
                    // 重新启用按钮
                    nextLevelButton.interactable = true;
                    previousLevelButton.interactable = true;

                    // 打印最上层和最下层按钮的名称
                    PrintTopButtonName();
                    PrintBottomButtonName();
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
        string bottomButtonName = levelButtons[bottomIndex].name;
        Debug.Log("最下层按钮的名称: " + bottomButtonName);
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
        float alpha = Mathf.Lerp(0.3f, 1f, (float)sortOrder / (levelCount / 2));
        float scale = Mathf.Lerp(0.8f, 1f, (float)sortOrder / (levelCount / 2));

        // 设置透明度
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        // 设置缩放值
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = new Vector3(scale, scale, scale);
        }
    }
    public int GetCurrentBottomIndex()
    {
        int bottomIndex = sortOrders.IndexOf(sortOrders.Count / 2);
        return int.Parse(levelButtons[bottomIndex].name);
    }
}
