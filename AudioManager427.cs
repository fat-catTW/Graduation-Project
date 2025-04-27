using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private string serverUrl = "https://feynman-server.onrender.com";
    private bool isRecording = false;
    private AudioClip recordedClip;

    // 音頻設置
    private const int SAMPLE_RATE = 44100;
    private const int CHANNELS = 1;  // 使用單聲道
    private const int RECORDING_LENGTH = 60;  // 最大錄音長度（秒）

    // 引用聊天組件
    private MonoBehaviour chatManager;

    // 添加事件
    public event Action OnRecordingStarted;
    public event Action OnRecordingStopped;

    void Start()
    {
        // 檢查麥克風權限
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("沒有找到麥克風設備！請檢查麥克風權限和連接。");
            return;
        }

        // 列出所有可用的麥克風設備
        Debug.Log("可用的麥克風設備：");
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"- {device}");
        }

        // 嘗試獲取聊天組件
        chatManager = GetComponent<chatWithProfessor>();
        if (chatManager == null)
        {
            chatManager = GetComponent<chatWithStudent>();
        }

        if (chatManager == null)
        {
            Debug.LogError("無法找到聊天組件！請確保 AudioManager 和聊天組件在同一個遊戲物件上。");
        }
    }

    // 開始錄音
    public void StartRecording()
    {
        if (!isRecording)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("沒有找到麥克風設備！");
                return;
            }

            isRecording = true;
            string deviceName = Microphone.devices[0];

            // 創建新的AudioClip
            recordedClip = Microphone.Start(deviceName, false, RECORDING_LENGTH, SAMPLE_RATE);
            if (recordedClip == null)
            {
                Debug.LogError("無法創建AudioClip！");
                isRecording = false;
                return;
            }

            Debug.Log($"開始錄音... 使用設備: {deviceName}");
            Debug.Log($"音頻設置 - 採樣率: {SAMPLE_RATE}Hz, 聲道數: {CHANNELS}, 最大長度: {RECORDING_LENGTH}秒");

            // 觸發錄音開始事件
            OnRecordingStarted?.Invoke();
        }
    }

    // 停止錄音並發送
    public void StopRecording()
    {
        if (isRecording)
        {
            Microphone.End(null);
            isRecording = false;
            Debug.Log("錄音結束");

            // 觸發錄音停止事件
            OnRecordingStopped?.Invoke();

            // 檢查錄音是否成功
            if (recordedClip != null && recordedClip.length > 0)
            {
                Debug.Log($"錄音長度: {recordedClip.length}秒");
                StartCoroutine(ProcessAudio());
            }
            else
            {
                Debug.LogError("錄音失敗或錄音時長為0！");
            }
        }
    }

    private IEnumerator ProcessAudio()
    {
        if (recordedClip == null || recordedClip.length == 0)
        {
            Debug.LogError("沒有有效的錄音數據！");
            yield break;
        }

        // 將AudioClip轉換為WAV格式
        byte[] wavData = AudioToWav(recordedClip);
        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogError("WAV轉換失敗！");
            yield break;
        }

        Debug.Log($"準備發送音頻數據，大小: {wavData.Length} bytes");

        // 首先發送到轉寫API
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post($"{serverUrl}/transcribe", form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                TranscriptionResponse response = JsonUtility.FromJson<TranscriptionResponse>(www.downloadHandler.text);
                string originalText = response.text;
                Debug.Log($"原始轉寫文本: {originalText}");

                // 處理重複字詞
                string cleanedText = CleanRepeatedWords(originalText);
                Debug.Log($"處理後的轉寫文本: {cleanedText}");

                // 使用聊天組件的SendMessage方法發送消息
                if (chatManager != null)
                {
                    if (chatManager is chatWithProfessor professor)
                    {
                        professor.testInput.text = cleanedText;
                        StartCoroutine(professor.SendMessage());
                    }
                    else if (chatManager is chatWithStudent student)
                    {
                        student.testInput.text = cleanedText;
                        StartCoroutine(student.SendMessage());
                    }
                }
                else
                {
                    Debug.LogError("chatManager未初始化！");
                }
            }
            else
            {
                Debug.LogError($"轉寫錯誤: {www.error}");
            }
        }
    }

    // 處理重複字詞的方法
    private string CleanRepeatedWords(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 分割文本為單詞
        string[] words = text.Split(new char[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> cleanedWords = new List<string>();
        string lastWord = "";

        foreach (string word in words)
        {
            // 如果當前單詞與上一個單詞不同，則添加到結果中
            if (word != lastWord)
            {
                cleanedWords.Add(word);
                lastWord = word;
            }
        }

        // 重新組合文本
        return string.Join(" ", cleanedWords);
    }

    // 將AudioClip轉換為WAV格式的位元組數組
    private byte[] AudioToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (Int16)(samples[i] * 32767);
        }

        // 計算檔案大小
        int headerSize = 44;
        int dataSize = intData.Length * 2;  // 16-bit = 2 bytes per sample
        int fileSize = headerSize + dataSize;

        byte[] header = new byte[headerSize];

        // RIFF 標識
        header[0] = (byte)'R'; header[1] = (byte)'I'; header[2] = (byte)'F'; header[3] = (byte)'F';

        // 檔案大小
        header[4] = (byte)(fileSize & 0xFF);
        header[5] = (byte)((fileSize >> 8) & 0xFF);
        header[6] = (byte)((fileSize >> 16) & 0xFF);
        header[7] = (byte)((fileSize >> 24) & 0xFF);

        // WAVE 標識
        header[8] = (byte)'W'; header[9] = (byte)'A'; header[10] = (byte)'V'; header[11] = (byte)'E';

        // fmt 子塊
        header[12] = (byte)'f'; header[13] = (byte)'m'; header[14] = (byte)'t'; header[15] = (byte)' ';
        header[16] = 16; // 子塊大小
        header[17] = 0; header[18] = 0; header[19] = 0;

        // 音頻格式 (PCM)
        header[20] = 1; // PCM = 1
        header[21] = 0;

        // 聲道數
        header[22] = (byte)clip.channels;
        header[23] = 0;

        // 採樣率 (44.1kHz)
        int sampleRate = 44100;
        header[24] = (byte)(sampleRate & 0xFF);
        header[25] = (byte)((sampleRate >> 8) & 0xFF);
        header[26] = (byte)((sampleRate >> 16) & 0xFF);
        header[27] = (byte)((sampleRate >> 24) & 0xFF);

        // 位元率 (採樣率 * 聲道數 * 2)
        int byteRate = sampleRate * clip.channels * 2;
        header[28] = (byte)(byteRate & 0xFF);
        header[29] = (byte)((byteRate >> 8) & 0xFF);
        header[30] = (byte)((byteRate >> 16) & 0xFF);
        header[31] = (byte)((byteRate >> 24) & 0xFF);

        // 區塊對齊 (聲道數 * 2)
        header[32] = (byte)(clip.channels * 2);
        header[33] = 0;

        // 位元深度
        header[34] = 16; // 16 bits
        header[35] = 0;

        // data 子塊
        header[36] = (byte)'d'; header[37] = (byte)'a'; header[38] = (byte)'t'; header[39] = (byte)'a';

        // 數據大小
        header[40] = (byte)(dataSize & 0xFF);
        header[41] = (byte)((dataSize >> 8) & 0xFF);
        header[42] = (byte)((dataSize >> 16) & 0xFF);
        header[43] = (byte)((dataSize >> 24) & 0xFF);

        // 合併檔案頭和音頻數據
        byte[] bytes = new byte[headerSize + dataSize];
        Buffer.BlockCopy(header, 0, bytes, 0, headerSize);

        // 將 Int16 數據轉換為字節數組
        byte[] soundData = new byte[dataSize];
        Buffer.BlockCopy(intData, 0, soundData, 0, dataSize);
        Buffer.BlockCopy(soundData, 0, bytes, headerSize, dataSize);

        Debug.Log($"生成的 WAV 檔案大小: {bytes.Length} bytes");
        Debug.Log($"音頻參數 - 採樣率: {sampleRate}Hz, 聲道數: {clip.channels}, 位元深度: 16-bit");

        return bytes;
    }
}
