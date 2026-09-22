using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using NPOI;
using System.IO;
using System.Drawing.Imaging;
using peak;
using IniParser;
using IniParser.Model;
using System.Data.SQLite;
using System.IO.Ports;               //使用串口控件
using System.Threading;
using System.Diagnostics;              //使用延时
using StandardOPage;
using new_UNIVACCO;

namespace UNIVACCO_UI
{
    public partial class UNIVACCO : Form
    {
        //管理員密碼
        public string AdministratorPassword;

        //LED使用次數
        public int LED_Used_Counter;
        public int GLED_Used_Counter;

        //相機變數
        public baslerCamcs camera;

        //Arduino
        public Arduino arduino;

        //各頁面變數生成
        public UserControl_Setting SettingPage;
        public UserControl_Main StandardOPage;
        public PictureBox switchbox; //轉換相機圖像用
        public int ExposureTime_A;
        public int Hot_ExposureTime_B;
        public int Cold_ExposureTime_B;
        public int ExposureTime_OCT;
        public int ExposureTime;        
        //系統LOG文字儲存
        string logString;

        //IPC IO DLL呼叫
        [DllImport("winio64.dll")]

        public static extern bool InitializeWinIo();

        [DllImport("winio64.dll")]

        public static extern void ShutdownWinIo();

        [DllImport("winio64.dll")]

        public static extern bool GetPortVal(IntPtr wPortAddr, out int pdwPortVal, byte bSize);

        [DllImport("winio64.dll")]
        public static extern bool SetPortVal(uint wPortAddr, IntPtr dwPortVal, byte bSize);
        //系統檔案路徑
        public string ResultSavePath;
        public string SystemMessegeSavePath;
        //光源校正頁面switchbox
        public PictureBox lightcorrection_switchbox;
        public UNIVACCO()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); // 設置應用程序圖示icon
            ResultSavePath = ReadIniFile("Setting", "ResultSavePath", "OCTParameters.ini");    //結果儲存路徑
            SystemMessegeSavePath = ReadIniFile("Setting", "SystemMessegeSavePath", "OCTParameters.ini");    //Log儲存路徑
            AdministratorPassword = ReadIniFile("Setting", "AdministratorPassword", "OCTParameters.ini");    //頁面密碼
            ExposureTime_OCT = int.Parse(ReadIniFile("Camera", "ExposureTime", "OCTParameters.ini"));    //O版曝光時間
            GLED_Used_Counter = Convert.ToInt32(ReadIniFile("Setting", "GLED_Used_Counter", "OCTParameters.ini")); //綠光使用次數
            SettingPage = new UserControl_Setting(this);
            panel_StandardPage.Controls.Clear();
            panel_StandardPage.Controls.Add(SettingPage);

            //啟動log
            logString =
                    String.Format(
                        "切面自動分析辨識系統設備啟動!");
            WriteLog("System", logString);

            //IPC IO初始化
            InitializeWinIo();
            logString =String.Format("IPC IO初始化成功!");
            WriteLog("System", logString);
            _ = ConnectArduinoAsync();     //連接硬體

            //檢查相機連線狀態
            try
            {
                camera = new baslerCamcs();
                BaslerCamera();
            }
            catch
            {
                button_CamConnect.BackgroundImage = Properties.Resources.ConnectError;

                //相機連線失敗log
                logString =
                        String.Format(
                            "相機連線失敗");
                WriteLog("System", logString);
            }
            StandardOPage = new UserControl_Main(this);
            //載入Log助手
            OCT_LogHelper.Init(SystemMessegeSavePath);

        }
        private async Task ConnectArduinoAsync()
        {
            try
            {
                arduino = new Arduino(pixelperpulse: 2);
                await arduino.ConnectAsync();
                button_OffsetColdTransfer.Enabled = true;   //O版頁面按鈕啟動
                button_MotorConnect.BackgroundImage = Properties.Resources.motor_connect;
            }
            catch (Exception ex)
            {
                button_MotorConnect.BackgroundImage = Properties.Resources.motor_unconnect;
            }
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var screen = Screen.PrimaryScreen;

            // 取得目前的螢幕解析度
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // 取得 DPI 縮放比例
            float scaleFactor = this.DeviceDpi / 96f;

            // 只有在 24 吋 1920×1080 螢幕時補高度
            if (screenWidth == 1920 && screenHeight == 1080)    //1920*1080螢幕強制
            {
                this.ClientSize = new System.Drawing.Size(
                    (int)(this.Width * scaleFactor)-10,
                    (int)(this.Height * scaleFactor) + 15 // 增加 10px 來補足被裁掉的部分
                );
            }
            if (screenWidth == 2560 && screenHeight == 1440)    //2560*1440螢幕強制
            {
                this.ClientSize = new System.Drawing.Size(
                    (int)(this.Width * scaleFactor),
                    (int)(this.Height * scaleFactor) + 25 // 增加 25px 來補足被裁掉的部分
                );
            }
        }
        public void BaslerCamera()
        {
            camera.CameraImageEvent += Camera_CameraImageEvent;
            if (camera.CameraNumber > 0)
            {
                camera.CameraInit();
                //camera.ExposureTime(ExposureTime);
                button_CamConnect.BackgroundImage = Properties.Resources.ConnectSuccese;
                

                //相機連線成功log
                logString =
                        String.Format(
                            "相機連線成功");
                WriteLog("System", logString);
            }
            else
            {
                button_CamConnect.BackgroundImage = Properties.Resources.ConnectError;

                //相機連線失敗log
                logString =
                        String.Format(
                            "相機連線失敗");
                WriteLog("System", logString);
            }
        }

        public async Task ArduinoOpenGreenLight()
        {
            arduino.OpenGreenLightAsync();
        }                                                                               
        public async Task ArduinoCloseGreenLight()
        {
            arduino.CloseGreenLightAsync();
        }

        public async Task ArduinoOpenValve()
        {
            await arduino.OpenValveAsync();
        }

        public async Task ArduinoCloseValve()
        {
            await arduino.CloseValveAsync();
        }


        public void WriteLog(string errorType, string message)
        {
            try
            {
                // 使用 Path.Combine 結合路徑
                string dirName = Path.Combine(SystemMessegeSavePath, "Log");
                string fileName = Path.Combine(dirName, $"{DateTime.Now:yyyyMMdd}.txt");

                // 檢查並創建目錄（如果不存在）
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                // 使用 StreamWriter 寫入日誌，避免多餘的 File.Create 操作
                using (StreamWriter sw = new StreamWriter(fileName, true, Encoding.UTF8)) // UTF-8 編碼
                {
                    sw.WriteLine("{0} {1}: {2}", DateTime.Now.ToLongTimeString(), errorType, message);
                }
            }
            catch (Exception ex)
            {
                // 日誌寫入失敗時拋出清晰的錯誤
                throw new IOException($"寫入日誌失敗，錯誤訊息: {ex.Message}", ex);
            }
        }
        //-------------------用來讀寫ini檔案------------------------------
        [DllImport("kernel32")]
        private static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string sectionName, string key, string defaultValue, byte[] returnBuffer, int size, string filePath);
        //-------------------------------------------------------------------
        /// 寫入INI檔案
        /// </summary>
        /// <param Name="Section">專案名稱(如 [TypeName] )</param>
        /// <param Name="Key">鍵</param>
        /// <param Name="Value">值</param>
        public string ReadIniFile(string section, string key, string relativeFilePath)
        {
            // 將路徑結合為完整路徑
            if (relativeFilePath.StartsWith("/") || relativeFilePath.StartsWith("\\"))
            {
                relativeFilePath = relativeFilePath.Substring(1);
            }
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeFilePath);

            // 檢查文件是否存在
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"找不到指定的 INI 文件: {fullPath}");
            }

            try
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(fullPath, Encoding.UTF8); // 指定 UTF-8 編碼讀取
                return data[section][key] ?? string.Empty; // 返回值，若不存在則返回空字串
            }
            catch (Exception ex)
            {
                throw new Exception($"讀取 INI 文件失敗: {fullPath}", ex);
            }
        }
        public void IniWriteValue(string section, string key, string value, string relativeFilePath)
        {
            // 將路徑結合為完整路徑
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeFilePath);

            try
            {
                var parser = new FileIniDataParser();
                IniData data;

                // 如果文件存在，則讀取現有內容；否則創建新內容
                if (File.Exists(fullPath))
                {
                    data = parser.ReadFile(fullPath, Encoding.UTF8);
                }
                else
                {
                    data = new IniData();
                }

                // 寫入新的鍵值對
                data[section][key] = value;

                // 使用 UTF-8 保存
                parser.WriteFile(fullPath, data, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new Exception($"寫入 INI 文件失敗: {fullPath}", ex);
            }
        }
        //------------------------相機拍照觸發事件--------------------------
        public void Camera_CameraImageEvent(Bitmap bmp)
        {
            if (pictureBox_stream.InvokeRequired)
            {
                pictureBox_stream.BeginInvoke(new MethodInvoker(() =>
                {
                    var oldImage = pictureBox_stream.Image;
                    pictureBox_stream.Image = bmp;
                    oldImage?.Dispose();
                }));
            }
            else
            {
                var oldImage = pictureBox_stream.Image;
                pictureBox_stream.Image = bmp;
                oldImage?.Dispose();
            }
        }

        //---------------------------視窗按鈕切換----------------------------------
        private void button_Setting_Click(object sender, EventArgs e)
        {
            panel_StandardPage.Controls.Clear();
            panel_StandardPage.Controls.Add(SettingPage);

            //頁面切換log
            logString =
                    String.Format(
                        "切換至設定頁面");
            WriteLog("System", logString);
        }
        private void button_CamConnect_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("您確定要重新連線?", "資訊", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    camera.DestroyCamera();
                    camera = new baslerCamcs();
                    camera.CameraImageEvent += Camera_CameraImageEvent;
                    if (camera.CameraNumber > 0)
                    {
                        camera.CameraInit();
                        button_CamConnect.BackgroundImage = Properties.Resources.ConnectSuccese;
                        MessageBox.Show("連線成功", "連線資訊", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        //相機連線log
                        logString =
                                String.Format(
                                    "相機連線成功");
                        WriteLog("System", logString);
                    }
                    else
                    {
                        button_CamConnect.BackgroundImage = Properties.Resources.ConnectError;
                        MessageBox.Show("連線失敗", "連線資訊", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        //相機連線log
                        logString =
                                String.Format(
                                    "相機連線失敗");
                        WriteLog("System", logString);
                    }                   
                }
                catch
                {
                    button_CamConnect.BackgroundImage = Properties.Resources.ConnectError;

                    //相機連線失敗log
                    logString =
                            String.Format(
                                "相機連線失敗");
                    WriteLog("System", logString);
                    MessageBox.Show("連線失敗", "連線資訊", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        /// <summary>
        /// 將控制項的寬，高，左邊距，頂邊距和字體大小暫存到tag屬性中
        /// </summary>
        /// <param Name="cons">遞歸控制項中的控制項</param>
        private void SetTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                    SetTag(con);
            }
        }     
        private void UNIVACCO_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
        private void UNIVACCO_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // 儲存 LED 使用次數
                IniWriteValue("Setting", "GLED_Used_Counter", GLED_Used_Counter.ToString(), "OCTParameters.ini");

                // 關閉相機資源
                if (camera != null)
                {
                    try
                    {
                        camera.Stop();  // 如果有 KeepShot 模式
                        camera.CameraImageEvent -= Camera_CameraImageEvent; // 移除事件
                        camera.DestroyCamera();  // 假設你已實作該釋放方法
                        camera = null;
                    }
                    catch (Exception ex)
                    {
                        WriteLog("Camera", $"相機釋放時發生錯誤: {ex.Message}");
                    }
                }

                // 釋放 Arduino
                if (arduino != null)
                {
                    try
                    {
                        arduino.Disconnect();
                        arduino.Dispose();
                        arduino = null;
                    }
                    catch (Exception ex)
                    {
                        WriteLog("Arduino", $"Arduino 釋放時發生錯誤: {ex.Message}");
                    }
                }

                // 關閉 IO DLL
                ShutdownWinIo();
            }
            catch (Exception ex)
            {
                WriteLog("System", $"FormClosing 發生錯誤: {ex.Message}");
            }
        }

        private void UNIVACCO_Load(object sender, EventArgs e)
        {
        }
        private void UNIVACCO_Resize(object sender, EventArgs e)
        {

        }
        private void UNIVACCO_Shown(object sender, EventArgs e)
        {
            
        }
        //--------------------------------------------------------------
        /// <summary>
        /// 獲取本機MAC地址
        /// </summary>
        /// <returns>本機MAC地址</returns>
        public static string GetMacAddress()
        {
            try
            {
                string strMac = string.Empty;
                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                {
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        strMac = mo["MacAddress"].ToString();
                    }
                }
                moc = null;
                mc = null;
                return strMac;
            }
            catch
            {
                return "unknown";
            }
        }
        private async void button_StandardOPage_Click(object sender, EventArgs e)
        {
            try
            {
                camera.KeepShot();
                // 1. 馬達回原點 (非同步啟動，不等待其完成)
                // 將 await 放在這裡，可以讓 GoHomeAsync 啟動後，程式碼立即往下執行
                Task<string> goHomeTask = arduino.GoHomeAsync();

                // 2. 載入頁面並更新 UI
                ExposureTime = ExposureTime_OCT;
                camera.ExposureTime(ExposureTime_OCT);
                this.BackColor = SystemColors.GradientInactiveCaption;
                panel_StandardPage.Controls.Clear();
                panel_StandardPage.Controls.Add(StandardOPage);

                // 3. 確保 "開始分析" 按鈕先被禁用，直到馬達回原點完成
                StandardOPage.button_Start.Enabled = false; // 初始禁用

                // 4. 等待馬達回原點完成
                string response = await goHomeTask; // 在這裡等待 GoHomeAsync 的結果

                // 5. 根據回原點結果啟用或顯示訊息
                if (response == "leftmove_finish")
                {
                    StandardOPage.button_Start.Enabled = true; // 回原點成功後，啟用 "開始分析" 按鈕
                    // 您可以選擇在這裡不顯示 MessageBox，因為用戶體驗已改善
                    // 或顯示一個輕量級的通知 (例如狀態列訊息)
                    // MessageBox.Show("馬達已成功回原點。", "動作完成", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                }
                else
                {
                    // 回原點失敗，不啟用 "開始分析" 按鈕
                    StandardOPage.button_Start.Enabled = false;
                    MessageBox.Show($"回原點失敗或收到非預期回應: {response}", "動作結果", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // 捕獲任何潛在的錯誤
                MessageBox.Show($"回原點時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // 確保錯誤發生時 "開始分析" 按鈕也是禁用的
                if (StandardOPage != null)
                {
                    StandardOPage.button_Start.Enabled = false;
                }
            }
        }

        private async void button_MotorConnect_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("您確定要重新連線?", "資訊", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    if (arduino != null)
                    {
                        arduino.Disconnect();
                        arduino.Dispose();
                        arduino = null;
                    }

                    arduino = new Arduino(pixelperpulse: 2);
                    await arduino.ConnectAsync();

                    button_MotorConnect.BackgroundImage = Properties.Resources.motor_connect;
                    button_OffsetColdTransfer.Enabled = true;  // ← 加這行
                    MessageBox.Show("馬達連線成功", "連線資訊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    logString = String.Format("馬達重新連線成功");
                    WriteLog("System", logString);
                }
                catch (Exception ex)
                {
                    arduino = null;
                    button_MotorConnect.BackgroundImage = Properties.Resources.motor_unconnect;
                    button_OffsetColdTransfer.Enabled = false;  // ← 加這行
                    MessageBox.Show($"馬達連線失敗: {ex.Message}", "連線資訊", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    logString = String.Format("馬達重新連線失敗");
                    WriteLog("System", logString);
                }
            }
        }
    }
}
