using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance;  // Singleton 讓其他Manager可以叫

    [Header("AvatarManager (請在Inspector設定)")]
    public AvatarManager avatarManager;

    [Header("UI 元件")]
    public Image avatarImage;
    public TMP_Text usernameText;
    public TMP_Text emailText;
    public TMP_Text totalPointsText;
    public TMP_Text diamondsText;
    public TMP_Text coinsText;

    private void Awake()
    {
        Instance = this;
    }

    void OnEnable()  // 每次頁面開啟時自動刷新
    {
        RefreshProfile();  // ✅ 從PlayerPrefs直接拿最新資料
    }

    /// <summary>
    /// 讓外部 (APIManager/SettingsManager) 呼叫這個方法來刷新
    /// </summary>
    public void RefreshProfile()
    {
        LoadUserDataFromPrefs();
    }

    /// <summary>
    /// 讀取PlayerPrefs並更新UI
    /// </summary>
    public void LoadUserDataFromPrefs()
    {
        int avatarId = PlayerPrefs.GetInt("AvatarID", 1);
        string username = PlayerPrefs.GetString("Username", "未知用戶");
        string email = PlayerPrefs.GetString("UserEmail", "未綁定 Email");
        int totalPoints = PlayerPrefs.GetInt("TotalPoints", 0);
        int coins = PlayerPrefs.GetInt("Coins", 0);
        int diamonds = PlayerPrefs.GetInt("Diamonds", 0);

        avatarImage.sprite = avatarManager.GetAvatarSprite(avatarId);
        usernameText.text = username;
        emailText.text = email;
        totalPointsText.text = totalPoints.ToString();
        coinsText.text = coins.ToString();
        diamondsText.text = diamonds.ToString();
    }
}
