from flask import Flask, request, jsonify
import mysql.connector
from mysql.connector import Error

app = Flask(__name__)

# Azure MySQL 連接設定
db_config = {
    'host': '127.0.0.1',   
    'user': 'root',
    'password': 'my-secret-pw',
    'database': 'feyndora'
}

# 建立資料庫連線的函式
def get_db_connection():
    try:
        conn = mysql.connector.connect(**db_config)
        return conn
    except Error as e:
        print(f"Error connecting to MySQL: {e}")
        return None

@app.route('/')
def index():
    return "Flask server is running!"

@app.route('/get_user/<int:user_id>', methods=['GET'])
def get_user(user_id):
    try:
        conn = get_db_connection()
        if not conn:
            return jsonify({"error": "Database connection failed"}), 500

        cursor = conn.cursor(dictionary=True)
        query = "SELECT * FROM Users WHERE user_id = %s"
        cursor.execute(query, (user_id,))
        user = cursor.fetchone()

        cursor.close()
        conn.close()

        if user:
            return jsonify(user)
        else:
            return jsonify({"message": "User not found"}), 404

    except Error as e:
        return jsonify({"error": str(e)}), 500
    finally:
        if 'conn' in locals() and conn.is_connected():
            cursor.close()
            conn.close()

if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=8000)