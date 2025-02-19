from flask import Flask, request, jsonify
import mysql.connector
from mysql.connector import Error
import bcrypt

app = Flask(__name__)

# MySQL 資料庫設定
db_config = {
    'host': '127.0.0.1',   
    'user': 'root',
    'password': 'my-secret-pw',
    'database': 'feyndora'
}

# 建立 MySQL 連線
def get_db_connection():
    try:
        conn = mysql.connector.connect(**db_config)
        return conn
    except Error as e:
        print(f"資料庫連接錯誤: {e}")
        return None

@app.route('/')
def index():
    return "Flask 伺服器運行中!"

# 註冊 API
@app.route('/register', methods=['POST'])
def register():
    data = request.json
    username = data.get('username')
    email = data.get('email')
    password = data.get('password')

    if not username or not email or not password:
        return jsonify({"error": "缺少必要欄位"}), 400

    conn = get_db_connection()
    if not conn:
        return jsonify({"error": "資料庫連接失敗"}), 500

    cursor = conn.cursor()

    # 檢查 Email 是否已存在
    cursor.execute("SELECT * FROM Users WHERE email = %s", (email,))
    if cursor.fetchone():
        return jsonify({"error": "該 Email 已被註冊"}), 400

    # 加密密碼
    hashed_password = bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt())

    # 插入新用戶，確保 total_learning_points 預設為 0，account_created_at 使用 NOW()
    query = """INSERT INTO Users (username, email, password, total_learning_points, account_created_at, diamonds, coins) 
               VALUES (%s, %s, %s, %s, NOW(), %s, %s)"""
    cursor.execute(query, (username, email, hashed_password.decode('utf-8'), 0))
    conn.commit()

    cursor.close()
    conn.close()
    return jsonify({"message": "註冊成功"}), 201

# 登入 API
@app.route('/login', methods=['POST'])
def login():
    data = request.json
    email = data.get('email')
    password = data.get('password')

    if not email or not password:
        return jsonify({"error": "缺少必要欄位"}), 400

    conn = get_db_connection()
    if not conn:
        return jsonify({"error": "資料庫連接失敗"}), 500

    cursor = conn.cursor(dictionary=True)
    cursor.execute("SELECT * FROM Users WHERE email = %s", (email,))
    user = cursor.fetchone()

    cursor.close()
    conn.close()

    # 確保使用 bcrypt 驗證密碼
    if not user or not bcrypt.checkpw(password.encode('utf-8'), user['password'].encode('utf-8')):
        return jsonify({"error": "帳號或密碼錯誤"}), 401

    return jsonify({
        "message": "登入成功",
        "user_id": user["user_id"],
        "username": user["username"],
        "total_learning_points": user["total_learning_points"],
        "account_created_at": user["account_created_at"],
        "dismonds": users["diamonds"], #傳回鑽石數量
        "coins": user["coins"] #傳回金幣數量
    }), 200

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)