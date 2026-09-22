using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Concurrent;

namespace new_UNIVACCO
{
    public class Arduino : IDisposable
    {
        /*
            相機解析度10um/pixel
            螺桿導程4mm
            馬達200pulse/rev
            1pulse = 40um
            =>1pixel = 4pulse
        */
        private double pixelperpulse = 2;
        private readonly SerialPort serialPort;
        private bool isConnected;

        // 用於處理多個命令回應的隊列
        private readonly ConcurrentQueue<TaskCompletionSource<string>> pendingResponses = new ConcurrentQueue<TaskCompletionSource<string>>();

        // 用於緩衝不完整的序列埠資料，因為 Arduino 的回應是逐行發送的
        private StringBuilder serialBuffer = new StringBuilder();

        public Arduino(string portName = "COM6", int baudRate = 115200, double pixelperpulse = 2)
        {
            this.pixelperpulse = pixelperpulse;
            serialPort = new SerialPort(portName, baudRate)
            {
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 1000, // 讀取逾時
                WriteTimeout = 1000 // 寫入逾時
            };
            // 訂閱 DataReceived 事件，當序列埠收到資料時觸發
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                    isConnected = true;
                    Debug.WriteLine($"已連接到 {serialPort.PortName}");
                    await Task.Delay(2000); // 等待 Arduino 初始化完成
                }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"連接失敗: {ex.Message}");
                isConnected = false;
                throw; // 重新拋出異常，讓呼叫端處理連接失敗的情況
            }
        }

        public void Disconnect()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    // 在關閉埠口前取消訂閱事件，避免在 Dispose 後還觸發事件
                    serialPort.DataReceived -= SerialPort_DataReceived;
                    serialPort.Close();
                    isConnected = false;
                    Debug.WriteLine("已斷開連接");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"斷開連接錯誤: {ex.Message}");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // 讀取序列埠中所有可用的資料
                string data = serialPort.ReadExisting();
                serialBuffer.Append(data); // 將收到的資料追加到緩衝區

                // 檢查緩衝區中是否有完整的行 (以換行符 '\n' 結束)
                while (serialBuffer.ToString().Contains("\n"))
                {
                    int newlineIndex = serialBuffer.ToString().IndexOf('\n');
                    // 提取完整的訊息行，並去除前後的空白字元
                    string completeMessage = serialBuffer.ToString(0, newlineIndex).Trim();
                    // 從緩衝區移除已處理的訊息及其後續的換行符
                    serialBuffer.Remove(0, newlineIndex + 1);

                    Debug.WriteLine($"收到 Arduino 訊息: {completeMessage}");

                    // 嘗試從隊列中取出最老的 TaskCompletionSource
                    if (pendingResponses.TryDequeue(out TaskCompletionSource<string> tcs))
                    {
                        // 設定 TaskCompletionSource 的結果，這會解除 SendCommandAsync 中的 await 狀態
                        tcs.SetResult(completeMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"序列埠資料接收錯誤: {ex.Message}");
            }
        }

        private async Task<string> SendCommandAsync(string command, int timeoutMs = 5000)
        {
            if (!isConnected || !serialPort.IsOpen)
            {
                throw new InvalidOperationException("未連接到 Arduino。");
            }

            // 創建一個 TaskCompletionSource 來等待回應。
            // RunContinuationsAsynchronously 確保 SetResult 不會在 DataReceived 處理器所在的執行緒上阻塞。
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            pendingResponses.Enqueue(tcs); // 將其加入隊列，表示我們正在等待一個回應

            try
            {
                // 清空序列埠輸入緩衝區，確保我們只處理當前命令的回應
                serialPort.DiscardInBuffer();
                serialPort.WriteLine(command); // 發送命令給 Arduino
                Debug.WriteLine($"發送指令: {command}");

                // 等待回應，並設置一個超時任務
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask); // 等待命令完成或超時

                if (completedTask == timeoutTask)
                {
                    // 如果是超時任務先完成，表示命令逾時
                    // 嘗試將 tcs 標記為取消或異常狀態，以避免它在未來被完成
                    if (tcs.Task.Status == TaskStatus.WaitingForActivation)
                    {
                        tcs.TrySetCanceled();
                    }
                    throw new TimeoutException($"命令 '{command}' 超時，未收到 Arduino 回應。");
                }

                // 如果 tcs.Task 先完成，則表示收到了回應，回傳結果
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"發送指令錯誤: {ex.Message}");
                // 如果在發送或等待過程中發生其他錯誤，也確保 tcs 被標記為錯誤狀態
                if (tcs.Task.Status == TaskStatus.WaitingForActivation)
                {
                    tcs.TrySetException(ex);
                }
                throw; // 重新拋出異常
            }
        }

        public async Task<string> GoHomeAsync()
        {
            return await SendCommandAsync("jog_left_start", 10000);
        }

        public async Task<string> SpecificLeftMoveAsync(int pixels)
        {
            int pulses = Convert.ToInt16(Math.Round(pixels / pixelperpulse));
            if (pulses <= 0)
            {
                throw new ArgumentException("脈衝數必須大於 0。");
            }
            return await SendCommandAsync($"jog_left_{pulses}");
        }

        public async Task<string> SpecificRightMoveAsync(int pixels)
        {
            int pulses = Convert.ToInt16(Math.Round(pixels / pixelperpulse));
            if (pulses <= 0)
            {
                throw new ArgumentException("脈衝數必須大於 0。");
            }
            return await SendCommandAsync($"jog_right_{pulses}");
        }

        public async Task<string> CheckLeftLimitAsync()
        {
            return await SendCommandAsync("check_left_limit");
        }

        public async Task<string> CheckRightLimitAsync()
        {
            return await SendCommandAsync("check_right_limit");
        }

        public void Dispose()
        {
            Disconnect();
            serialPort?.Dispose(); // 確保序列埠物件被正確釋放
        }

        public async Task<string> OpenGreenLightAsync()
        {
            // Arduino 會回傳 "light_on_ok"
            return await SendCommandAsync("light_on");
        }

        public async Task<string> CloseGreenLightAsync()
        {
            // Arduino 會回傳 "light_off_ok"
            return await SendCommandAsync("light_off");
        }
        public async Task<string> OpenValveAsync()
        {
            // Arduino 會回傳 "valve_on_ok"
            return await SendCommandAsync("valve_on");
        }

        public async Task<string> CloseValveAsync()
        {
            // Arduino 會回傳 "valve_off_ok"
            return await SendCommandAsync("valve_off");
        }


    }
}