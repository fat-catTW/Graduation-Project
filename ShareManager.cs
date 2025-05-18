
using UnityEngine;

public class ShareManager : MonoBehaviour
{
    public void ShareNow()
    {
        ShareBridge.Share("我現在都用『英文知識王』背單字，推薦你也試試：https://englishwidget.page.link/shareApp");
    }
}
