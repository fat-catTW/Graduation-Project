# Feyndora App Database

## 專案說明
這是費曼學習 App 的資料庫，設計用於支持用戶管理、學習記錄、卡片系統、排行榜等核心功能。

## 資料庫表結構

---

## 資料庫表結構

### **Users 表**
- 儲存用戶的基本信息。
- **主要欄位**：
  - `user_id`: 自動遞增的用戶唯一 ID。
  - `username`: 用戶名，需唯一。
  - `email`: 電子郵件地址，需唯一。
  - `total_learning_points`: 用戶累積學習積分。
  - `account_created_at`: 用戶帳戶創建的時間戳。
  - `device_metadata`: JSON 格式儲存用戶設備相關信息。

---

### **TeacherCards 表**
- 管理教師卡片的相關數據。
- **主要欄位**：
  - `card_id`: 自動遞增的卡片唯一 ID。
  - `teacher_name`: 教師卡片名稱。
  - `rarity`: 卡片的稀有度（如 N、R、SR、SSR）。
  - `teacher_bio`: 教師的簡介。
  - `additional_attributes`: JSON 格式儲存卡片附加屬性。

---

### **TeacherCards_Users 表**
- 記錄用戶獲得的教師卡片。
- **主要欄位**：
  - `user_id`: 關聯到 `Users` 表的用戶 ID。
  - `card_id`: 關聯到 `TeacherCards` 表的卡片 ID。
  - `acquired_at`: 獲得卡片的時間戳。
  - `acquisition_metadata`: JSON 格式的獲取方式數據（如抽獎或任務獎勵）。

---

### **LearningSessionRecords 表**
- 儲存用戶的學習記錄。
- **主要欄位**：
  - `session_id`: 自動遞增的學習會話唯一 ID。
  - `user_id`: 關聯到 `Users` 表的用戶 ID。
  - `session_type`: 學習會話的類型（如「一對一」、「課堂模式」）。
  - `learning_points_earned`: 本次學習會話中獲得的積分。
  - `created_at`: 學習會話的創建時間戳。
  - `session_metadata`: JSON 格式的學習細節數據（如課程名稱、學習時長）。

---

### **InteractionDetails 表**
- 記錄學習過程中的互動信息。
- **主要欄位**：
  - `interaction_id`: 自動遞增的互動唯一 ID。
  - `session_id`: 關聯到 `LearningSessionRecords` 表的學習會話 ID。
  - `interaction_type`: 互動類型（如提問、回答）。
  - `content`: 互動的具體內容（如問題或答案）。
  - `created_at`: 互動創建的時間戳。
  - `interaction_metadata`: JSON 格式的附加數據（如反饋評分）。

---

### **LeaderboardRecords 表**
- 儲存用戶的排行榜數據。
- **主要欄位**：
  - `record_id`: 自動遞增的排行記錄唯一 ID。
  - `user_id`: 關聯到 `Users` 表的用戶 ID。
  - `ranking_type`: 排行榜的類型（如全球、每週）。
  - `total_points`: 用戶累積的積分總和。
  - `updated_at`: 排行榜記錄最後更新的時間戳。

---

### **LotteryTickets 表**
- 記錄用戶的抽獎券數據。
- **主要欄位**：
  - `ticket_id`: 自動遞增的抽獎券唯一 ID。
  - `user_id`: 關聯到 `Users` 表的用戶 ID。
  - `earned_at`: 抽獎券獲得的時間戳。
  - `is_used`: 抽獎券是否已被使用。

---

### **Notifications 表**
- 儲存用戶的通知數據。
- **主要欄位**：
  - `notification_id`: 自動遞增的通知唯一 ID。
  - `user_id`: 關聯到 `Users` 表的用戶 ID。
  - `content`: 通知的具體內容。
  - `notification_type`: 通知的類型（如積分更新或卡片獲得）。
  - `notification_metadata`: JSON 格式的附加數據（如觸發條件或詳情）。
  - `created_at`: 通知的創建時間戳。

---

### **DatabaseVersionControl 表**
- 追蹤資料庫的版本變更歷史。
- **主要欄位**：
  - `version_id`: 自動遞增的版本唯一 ID。
  - `version_number`: 資料庫版本號。
  - `description`: 版本的描述信息。
  - `applied_at`: 版本應用的時間戳。
  - `rollback_script`: 用於回滾版本的 SQL 腳本。

---

## 注意事項
1. 請確保 MySQL 版本大於等於 5.7，以支持 JSON 資料類型。
2. 每次資料庫結構變更需記錄在 `DatabaseVersionControl` 表中。
3. 資料庫設計遵循冪等性，所有變更皆可安全執行。

---


