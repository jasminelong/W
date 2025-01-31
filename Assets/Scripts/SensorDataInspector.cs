/*using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SensorDataInspector : MonoBehaviour
{
    public string portName = "COM3";  // 修改为实际串口 (Windows: COMx, macOS/Linux: /dev/ttyUSBx)
    public int baudRate = 115200;

    [SerializeField, Range(0f, 1f)]
    private float sensorValue = 0f; // 直接在 Inspector 中显示传感器数值

    private SerialPort serialPort;
    private Thread serialThread;
    private bool running = true;
    private const float smoothingFactor = 0.05f; // 低通滤波因子

    void Start()
    {
        // 初始化串口通信
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 50;
        Debug.Log("串口 ");
        try
        {
            serialPort.Open();
            serialThread = new Thread(ReadSerial);
            serialThread.Start();
            Debug.Log("串口打开 ");
        }
        catch (System.Exception e)
        {
            Debug.LogError("串口打开失败: " + e.Message);
        }
    }

    void ReadSerial()
    {
        while (running)
        {
            try
            {
                string data = serialPort.ReadLine();
                Debug.Log("串口打开11111 ");
                if (float.TryParse(data, out float value))
                {
                    Debug.Log("串口打开122222 ");
                    // 应用低通滤波
                    sensorValue = sensorValue * (1.0f - smoothingFactor) + value * smoothingFactor;
                    // 在控制台打印获取到的数据
                    Debug.Log("Received Sensor Value: " + sensorValue);
                }
            }
            catch (System.TimeoutException) { }
        }
    }

    void Update()
    {
        // 只在 Inspector 界面中更新数据
    }

    void OnApplicationQuit()
    {
        running = false;
        serialThread?.Join();
        if (serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
*/