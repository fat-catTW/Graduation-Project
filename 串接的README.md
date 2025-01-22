# Flask 與 Unity 互動並互結 MySQL

本項目目標是使用 Flask 建立 API 伺服器，並與 Azure Data Studio 中的 MySQL 資料庫互動，結合 Unity 實現前端與後端的資料互動。

---

## 項目架構

- **後端 (Flask + MySQL)**
  - Flask 充當後端 API，處理 Unity 的請求，並從 MySQL 資料庫中讀取數據。  
- **資料庫 (MySQL on Azure Data Studio)**
  - 儲存用戶與學習相關的資料。  
- **前端 (Unity)**
  - Unity 使用 HTTP 請求與後端轉接，取得數據並顯示。

---

## 環境設定

### 1. 必備工具

請確保已安裝：
- Python 3.x (推薦 3.9 以上)
- MySQL Server (已在 Azure Data Studio 中設置)
- Unity (最新版本)
- Git (版本控制工具)

---

### 2. 安裝 Python 處理處理環境

1. 建立 Python 處理處理環境：
    ```bash
    python3 -m venv myproject-env
    source myproject-env/bin/activate
    ```

2. 安裝所需軟體套件：
    ```bash
    pip install flask mysql-connector-python python-dotenv
    ```

3. 將套件列出保存為 `requirements.txt`：
    ```bash
    pip freeze > requirements.txt
    ```

---

## 後端部分 (Flask API)

1. 建立 `app.py` 檔案

```python
from flask import Flask, jsonify
import mysql.connector

app = Flask(__name__)

# MySQL 連接配置
db_config = {
    'host': '127.0.0.1',
    'user': 'root',
    'password': 'my-secret-pw',
    'database': 'feyndora'
}

@app.route('/')
def index():
    return "Flask server is running!"

@app.route('/get_user/<int:user_id>', methods=['GET'])
def get_user(user_id):
    conn = mysql.connector.connect(**db_config)
    cursor = conn.cursor(dictionary=True)
    cursor.execute("SELECT * FROM Users WHERE user_id = %s", (user_id,))
    user = cursor.fetchone()
    cursor.close()
    conn.close()
    return jsonify(user) if user else jsonify({"message": "User not found"})

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)
```

2. 啟動 Flask API

```bash
python app.py
```

3. 在瀏覽器中檢查 API：

[http://127.0.0.1:8000/get_user/1](http://127.0.0.1:8000/get_user/1)

---

## 前端部分 (Unity)

1. 建立 APIManager.cs 檔案

```csharp
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class APIManager : MonoBehaviour
{
    private string baseUrl = "http://127.0.0.1:8000";

    public static APIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GetUserData(1);
    }

    public void GetUserData(int userId)
    {
        StartCoroutine(GetUserRequest(userId));
    }

    IEnumerator GetUserRequest(int userId)
    {
        string url = $"{baseUrl}/get_user/{userId}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("成功取得用戶資料：" + request.downloadHandler.text);
            else
                Debug.LogError("API 請求失敗：" + request.error);
        }
    }
}
```

---

## 上傳到 GitHub

1. 初始化 Git 專案

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/你的代碼他/你的專案.git
git push -u origin main
```

---
