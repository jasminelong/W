using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 在 Unity 中使用后台线程读取串口数据的示例脚本，包含详细日志，
/// 可防止在停止/退出时卡死。
/// Unityでバックグラウンドスレッドを使用してシリアルデータを読み取るサンプルスクリプト。
/// 詳細なログを含み、停止/終了時のフリーズを防ぎます。
///
/// 适用平台 / 対応プラットフォーム: Windows/macOS/Linux
/// API 兼容级别 / API互換性レベル: .NET Framework
/// </summary>
public class SerialReader : MonoBehaviour
{
    [Header("Serial Port Settings / シリアルポート設定")]
    [Tooltip("例: Windows: COM3, macOS: /dev/tty.usbserial-xxx")]
    [SerializeField] private string portName = "COM3"; // 串口端口 / シリアルポート
    [SerializeField] private int baudRate = 115200; // 波特率 / ボーレート

    [Header("Timeout & Thread Settings / タイムアウト & スレッド設定")]
    [SerializeField] private int readTimeoutMillis = 50; // 读取超时时间(ms) / 読み取りタイムアウト(ミリ秒)
    [Tooltip("チェックを入れると、Editor で Pause を押すとバックグラウンドスレッドも停止する（デバッグ用）。")]
    [SerializeField] private bool stopThreadOnEditorPause = true;

    // 串口对象 / シリアルポートオブジェクト
    private SerialPort serialPort;

    // 后台线程 / バックグラウンドスレッド
    private Thread serialThread;

    // 线程运行标记（volatile 或用 lock 来读写） / スレッド実行フラグ（volatile または lock を使用して読み書き）
    private volatile bool isRunning;

    // 用于保存最新的传感器数值，在后台线程和主线程之间共享
    // 最新のセンサーデータを保存し、バックグラウンドスレッドとメインスレッドで共有
    private float lastSensorValue;
    private readonly object lockObject = new object();

    //--------------------------------------------------------------------------
    // 1. Unity 生命周期方法 / Unityライフサイクルメソッド
    //--------------------------------------------------------------------------
    private void Start()
    {
        Debug.Log("<color=cyan>[SerialReader] Start()</color>");

        // 1) 初始化并尝试打开串口 / シリアルポートを初期化して開く
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = readTimeoutMillis;
            serialPort.Open();

            Debug.Log($"<color=green>Serial Port [{portName}] opened at {baudRate} baud.</color>");

            // 2) 标记线程可运行，并启动后台线程
            // スレッドを実行可能にし、バックグラウンドスレッドを開始
            isRunning = true;
            serialThread = new Thread(SerialReadLoop);
            serialThread.Start();

            Debug.Log("<color=lime>Serial thread started.</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>Failed to open {portName}: {e.Message}</color>");
        }
    }

    private void Update()
    {
        // 如果在 Editor 中并且想要在 Pause 时关线程，则进行检测
        // Editor で Pause が押されたときにスレッドを停止するオプション
#if UNITY_EDITOR
        if (stopThreadOnEditorPause && EditorApplication.isPaused && isRunning)
        {
            Debug.LogWarning("<color=yellow>Editor paused -> Stopping serial thread...</color>");
            StopSerialThread();
        }
#endif

        // 从共享变量里拿到最新的数值 / 共有変数から最新の値を取得
        float currentVal;
        lock (lockObject)
        {
            currentVal = lastSensorValue;
        }

        // 在主线程使用数据 (演示：打印到 Console)
        // メインスレッドでデータを使用（例: コンソール出力）
        Debug.Log($"[SerialReader] Current Sensor Value: {currentVal:F3}");
    }

    private void OnDisable()
    {
        Debug.Log("<color=magenta>[SerialReader] OnDisable()</color>");
        StopSerialThread();
    }

    private void OnDestroy()
    {
        Debug.Log("<color=magenta>[SerialReader] OnDestroy()</color>");
        StopSerialThread();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("<color=magenta>[SerialReader] OnApplicationQuit()</color>");
        StopSerialThread();
    }

    //--------------------------------------------------------------------------
    // 2. 串口读取后台线程 / シリアルポート読み取りバックグラウンドスレッド
    //--------------------------------------------------------------------------
    private void SerialReadLoop()
    {
        Debug.Log("<color=gray>SerialReadLoop() started</color>");

        while (isRunning)
        {
            Debug.Log("<color=gray>Thread loop tick</color>");

            try
            {
                // 读取一行数据（阻塞最多 readTimeoutMillis ms）
                // 1行のデータを読み取る（最大 readTimeoutMillis ミリ秒ブロック）
                string line = serialPort.ReadLine();

                Debug.Log($"<color=gray>Raw data received: '{line}'</color>");

                // 解析为浮点数 / 浮動小数点数に変換
                if (float.TryParse(line, out float parsed))
                {
                    lock (lockObject)
                    {
                        lastSensorValue = parsed;
                    }
                }
            }
            catch (TimeoutException)
            {
                // 超时抛出 TimeoutException，可忽略，继续下一轮循环
                // タイムアウト例外は無視して次のループへ
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=red>SerialPort Error: {ex.Message}</color>");
            }

            // 休眠几毫秒，防止死循环占满 CPU
            // 数ミリ秒スリープし、CPU使用率を抑える
            Thread.Sleep(5);
        }

        Debug.Log("<color=gray>SerialReadLoop() exit while(isRunning)</color>");
    }

    //--------------------------------------------------------------------------
    // 3. 安全停止线程 & 关闭串口 / スレッドの安全な停止 & シリアルポートのクローズ
    //--------------------------------------------------------------------------
    private void StopSerialThread()
    {
        Debug.Log("<color=orange>[SerialReader] StopSerialThread() called.</color>");

        if (!isRunning)
        {
            Debug.Log("<color=orange>[SerialReader] Already stopped, ignore.</color>");
            return;
        }

        isRunning = false;

        // 等待线程自己退出循环 / スレッドがループを抜けるのを待つ
        if (serialThread != null && serialThread.IsAlive)
        {
            Debug.Log("<color=orange>[SerialReader] Joining thread...</color>");
            serialThread.Join();
            Debug.Log("<color=orange>[SerialReader] Thread joined.</color>");
        }

        // 关闭并释放串口 / シリアルポートを閉じて解放
        if (serialPort != null)
        {
            if (serialPort.IsOpen)
            {
                Debug.Log("<color=orange>[SerialReader] Closing port...</color>");
                serialPort.Close();
            }
            serialPort.Dispose();
            serialPort = null;
        }

        Debug.Log("<color=orange>[SerialReader] Serial thread stopped, port closed.</color>");
    }
}
