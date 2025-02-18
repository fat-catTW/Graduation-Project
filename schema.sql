-- 用戶表
CREATE TABLE Users (
    user_id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    total_learning_points INT DEFAULT 0,
    account_created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    device_metadata JSON DEFAULT (JSON_OBJECT())
);

-- 教師卡片表
CREATE TABLE TeacherCards (
    card_id INT PRIMARY KEY AUTO_INCREMENT,
    teacher_name VARCHAR(100) NOT NULL,
    rarity VARCHAR(20) NOT NULL,
    teacher_bio TEXT,
    additional_attributes JSON
);

-- 用戶-卡片關聯表
CREATE TABLE TeacherCards_Users (
    user_id INT,
    card_id INT,
    acquired_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    acquisition_metadata JSON,
    PRIMARY KEY (user_id, card_id),
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (card_id) REFERENCES TeacherCards(card_id) ON DELETE CASCADE
);

-- 學習記錄表
CREATE TABLE LearningSessionRecords (
    session_id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    session_type VARCHAR(50) NOT NULL,
    learning_points_earned INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    session_metadata JSON,
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

-- 互動詳情表
CREATE TABLE InteractionDetails (
    interaction_id INT PRIMARY KEY AUTO_INCREMENT,
    session_id INT NOT NULL,
    interaction_type VARCHAR(50) NOT NULL,
    content TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    interaction_metadata JSON,
    FOREIGN KEY (session_id) REFERENCES LearningSessionRecords(session_id) ON DELETE CASCADE
);

-- 排行榜記錄表
CREATE TABLE LeaderboardRecords (
    record_id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    ranking_type VARCHAR(50) NOT NULL,
    total_points INT DEFAULT 0,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

-- 抽獎券表
CREATE TABLE LotteryTickets (
    ticket_id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    earned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_used BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

-- 通知表
CREATE TABLE Notifications (
    notification_id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    content TEXT NOT NULL,
    notification_type VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    notification_metadata JSON,
    FOREIGN KEY (user_id) REFERENCES Users(user_id) ON DELETE CASCADE
);

-- 數據庫版本控制表
CREATE TABLE DatabaseVersionControl (
    version_id INT PRIMARY KEY AUTO_INCREMENT,
    version_number VARCHAR(20) NOT NULL,
    description TEXT,
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    rollback_script TEXT
);

-- 添加索引
CREATE INDEX idx_users_username ON Users(username);
CREATE INDEX idx_users_email ON Users(email);
CREATE INDEX idx_teachercards_name_rarity ON TeacherCards(teacher_name, rarity);
CREATE INDEX idx_learning_sessions_user ON LearningSessionRecords(user_id);
CREATE INDEX idx_interactions_session ON InteractionDetails(session_id);
CREATE INDEX idx_leaderboard_user ON LeaderboardRecords(user_id);
CREATE INDEX idx_lottery_user ON LotteryTickets(user_id);
CREATE INDEX idx_notifications_user ON Notifications(user_id);

-- 修改互動詳情表
ALTER TABLE InteractionDetails 
ADD COLUMN interaction_depth ENUM('BASIC', 'INTERMEDIATE', 'ADVANCED') NOT NULL DEFAULT 'BASIC',
ADD COLUMN learning_mode ENUM('ONE_TO_ONE', 'ONE_TO_MANY') NOT NULL,
ADD COLUMN expected_answer_complexity ENUM('SIMPLE', 'DETAILED', 'COMPREHENSIVE') NOT NULL DEFAULT 'SIMPLE',
ADD COLUMN user_question TEXT,
ADD COLUMN user_answer TEXT,
ADD COLUMN ai_feedback TEXT,
ADD COLUMN correctness_level ENUM('CORRECT', 'PARTIALLY_CORRECT', 'INCORRECT'),
ADD COLUMN points_earned INT DEFAULT 0,
ADD COLUMN knowledge_point_id INT;

-- 創建學習材料表
CREATE TABLE LearningMaterials (
    material_id INT PRIMARY KEY AUTO_INCREMENT,
    material_type ENUM('PPT', 'PDF', 'DOCX', 'OTHER') NOT NULL,
    file_path VARCHAR(255) NOT NULL,
    material_name VARCHAR(255) NOT NULL,
    uploaded_by INT,
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    material_metadata JSON DEFAULT (JSON_OBJECT()),
    FOREIGN KEY (uploaded_by) REFERENCES Users(user_id) ON DELETE SET NULL
);

-- 創建知識點表
CREATE TABLE MaterialKnowledgePoints (
    knowledge_point_id INT PRIMARY KEY AUTO_INCREMENT,
    material_id INT NOT NULL,
    point_order INT NOT NULL,
    point_title VARCHAR(255) NOT NULL,
    point_description TEXT,
    difficulty_level ENUM('BASIC', 'INTERMEDIATE', 'ADVANCED') DEFAULT 'BASIC',
    knowledge_point_metadata JSON DEFAULT (JSON_OBJECT()),
    FOREIGN KEY (material_id) REFERENCES LearningMaterials(material_id) ON DELETE CASCADE
);

-- 為 InteractionDetails 添加外鍵
ALTER TABLE InteractionDetails 
ADD CONSTRAINT fk_interaction_knowledge_point 
FOREIGN KEY (knowledge_point_id) 
REFERENCES MaterialKnowledgePoints(knowledge_point_id);

-- 修改索引
CREATE INDEX idx_learning_materials_uploaded_by ON LearningMaterials(uploaded_by);
CREATE INDEX idx_knowledge_points_material ON MaterialKnowledgePoints(material_id);

-- 修改Users表
ALTER TABLE Users ADD COLUMN password VARCHAR(255) NOT NULL;
