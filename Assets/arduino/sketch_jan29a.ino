#include <M5StickCPlus.h>  // M5StickC Plus Library / M5StickC Plus 库

// Global Variables / 全局变量
const float expFilterSmoothingFactor = 0.05;  // Exponential smoothing factor / 指数平滑滤波因子
const float scaleFactor = 4095;  // Normalization factor / 归一化因子
float sensorVal = 0;
float displayVal = 0;
int previousBarWidth = 0;
SemaphoreHandle_t xMutex;  // Mutex for thread safety / 线程安全的互斥锁

// Sensor Reading Task / 传感器读取任务
void readSensorTask(void *pvParameters) {
    while (true) {
        int sensorValueInt = analogRead(33);  // Read sensor value / 读取传感器值
        
        // Low-pass filter processing / 低通滤波处理
        float newVal = sensorVal * (1.0 - expFilterSmoothingFactor) + (float)sensorValueInt * expFilterSmoothingFactor;

        // Ensure thread safety using mutex / 使用互斥锁保证数据同步
        if (xSemaphoreTake(xMutex, portMAX_DELAY) == pdTRUE) {
            sensorVal = newVal;
            xSemaphoreGive(xMutex);
        }

        delay(1);  // Sampling interval (1ms) / 采样间隔（1ms）
    }
}

// Display & Serial Output Task / 显示任务 + 串口数据发送任务
void displayGraphTask(void *pvParameters) {
    while (true) {
        // Get the current sensorVal / 获取当前 sensorVal 值
        if (xSemaphoreTake(xMutex, portMAX_DELAY) == pdTRUE) {
            displayVal = sensorVal;
            xSemaphoreGive(xMutex);
        }

        // Calculate bar graph width / 计算条形图长度
        int barWidth = map(displayVal, 0, 4095, 0, 240);

        // Erase previous bar graph if new width is smaller / 擦除旧条形图（避免重叠）
        if (barWidth < previousBarWidth) {
            M5.Lcd.fillRect(barWidth, 60, previousBarWidth - barWidth, 20, BLACK);
        }

        // Draw new bar graph / 绘制新条形图
        M5.Lcd.fillRect(0, 60, barWidth, 20, GREEN);

        // Update previous width / 更新上一次的宽度
        previousBarWidth = barWidth;

        // Display sensor value on LCD screen / 在 LCD 屏幕上显示数值
        M5.Lcd.setCursor(10, 100);
        M5.Lcd.printf("%4.3f", displayVal / scaleFactor);

        // Send sensor value via Serial / **发送数据到串口**
       // Serial.printf("%4.3f\n", displayVal / scaleFactor);
        Serial.printf("%4.3f\r\n", displayVal / scaleFactor);


        delay(10);  // Display & serial output interval (10ms) / 显示 & 串口发送间隔（10ms）
    }
}

void setup() {
    M5.begin();
    Serial.begin(115200);  // Initialize Serial Communication / 串口初始化
    pinMode(33, INPUT);  // Set GPIO 33 as input / 设置 GPIO 33 为输入模式

    // Initialize sensor value / 初始值
    sensorVal = (float)analogRead(33);

    // Create Mutex / 创建互斥锁
    xMutex = xSemaphoreCreateMutex();
    if (xMutex == NULL) {
        Serial.println("Mutex creation failed.");
        while (1);
    }

    // Initialize LCD display / 初始化 LCD 显示
    M5.Lcd.setRotation(1);
    M5.Lcd.fillScreen(BLACK);
    M5.Lcd.setTextColor(WHITE, BLACK);
    M5.Lcd.setTextSize(1);
    M5.Lcd.setCursor(10, 20);  
    M5.Lcd.printf("Sensor Visualization & Serial Output");
    M5.Lcd.setCursor(10, 40);  
    M5.Lcd.printf("SmoothFactor: %4.2f", expFilterSmoothingFactor);
    M5.Lcd.setTextSize(3);

    // **Create FreeRTOS Tasks** / **创建 FreeRTOS 任务**
    xTaskCreate(readSensorTask, "Read Sensor Task", 1024, NULL, 1, NULL);
    xTaskCreate(displayGraphTask, "Display Graph Task", 2048, NULL, 1, NULL);
}

void loop() {
    // Leave main loop empty, all processes are handled by FreeRTOS tasks / 主循环为空，所有处理都由 FreeRTOS 任务执行
}
