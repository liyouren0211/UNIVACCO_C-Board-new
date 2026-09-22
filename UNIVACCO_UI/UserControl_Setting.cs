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
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using NPOI;
using System.IO;
using System.IO.Ports;               //使用串口控件
using System.Threading;              //使用延时
using StandardOPage;
using static OpenCvSharp.LineIterator;

namespace UNIVACCO_UI
{
    public partial class UserControl_Setting : UserControl
    {
        UNIVACCO MainPage;
        string logString;
        LightCorrection LightCorrectionPage;
        public UserControl_Setting(UNIVACCO _MainPage)
        {
            InitializeComponent();
            MainPage = _MainPage;

            //系統log
            logString =
                    String.Format(
                        "設定頁面開啟成功!");
            MainPage.WriteLog("System_SettingPage", logString);

            textBox_ResultSavePathShow.Text = MainPage.ResultSavePath;
            textBox_SystemMessegeSavePathShow.Text = MainPage.SystemMessegeSavePath;
        }


        private void button_ResultSavePath_Click(object sender, EventArgs e)
        {
            //系統log
            logString =
                    String.Format(
                        "更改辨識結果路徑按鈕按下");
            MainPage.WriteLog("System_SettingPage", logString);

            FolderBrowserDialog path = new FolderBrowserDialog();
            path.SelectedPath = textBox_ResultSavePathShow.Text;
            path.ShowDialog();
            textBox_ResultSavePathShow.Text = path.SelectedPath;

            //系統log
            logString =
                    String.Format(
                        "辨識結果路徑變更");
            MainPage.WriteLog("System_SettingPage", logString);
        }

        private void button_SystemMessegeSavePath_Click(object sender, EventArgs e)
        {
            //系統log
            logString =
                    String.Format(
                        "更改系統訊息儲存路徑按鈕按下");
            MainPage.WriteLog("System_SettingPage", logString);

            FolderBrowserDialog path = new FolderBrowserDialog();
            path.SelectedPath = textBox_SystemMessegeSavePathShow.Text;
            path.ShowDialog();
            textBox_SystemMessegeSavePathShow.Text = path.SelectedPath;

            //系統log
            logString =
                    String.Format(
                        "系統訊息儲存路徑變更");
            MainPage.WriteLog("System_SettingPage", logString);
        }

        private void button_Save_Click(object sender, EventArgs e)
        {
            //系統log
            logString =
                    String.Format(
                        "儲存按鈕按下");
            MainPage.WriteLog("System_SettingPage", logString);

            if (MessageBox.Show("你確定要儲存？", "資訊", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                //更改路徑，同時更新ini檔
                MainPage.ResultSavePath = textBox_ResultSavePathShow.Text;
                MainPage.IniWriteValue("Setting", "ResultSavePath", MainPage.ResultSavePath, "OCTParameters.ini");
                MainPage.SystemMessegeSavePath = textBox_SystemMessegeSavePathShow.Text;
                MainPage.IniWriteValue("Setting", "SystemMessegeSavePath", MainPage.SystemMessegeSavePath, "OCTParameters.ini");
                //載入Log助手
                OCT_LogHelper.Init(MainPage.SystemMessegeSavePath);
                //系統log
                logString =
                        String.Format(
                            "儲存成功!");
                MainPage.WriteLog("System_SettingPage", logString);
            }
        }

        private void button_LightCorrection_Click(object sender, EventArgs e)
        {

        }

        private async void button_ValveOpen_Click(object sender, EventArgs e)
        {
            await MainPage.ArduinoOpenValve();
        }

        private async void button_ValveClose_Click(object sender, EventArgs e)
        {
            await MainPage.ArduinoCloseValve();
        }

        private void button_GreenLightOpen_Click_Click(object sender, EventArgs e)
        {
            MainPage.ArduinoOpenGreenLight();
        }

        private void button_GreenLightClose_Click_Click(object sender, EventArgs e)
        {
            MainPage.ArduinoCloseGreenLight();
        }



        private void button1_Click(object sender, EventArgs e)
        {
            if (int.TryParse(textBox1.Text, out int pixels))
            {
                if (pixels > 0)
                {
                    MainPage.arduino?.SpecificRightMoveAsync(pixels);
                }
                else
                {
                    MessageBox.Show("pixels必須大於 0", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("請輸入有效的pixels", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        
    }

        private async void button_check_left_limit_Click(object sender, EventArgs e)
        {
            try
            {
                string status = await MainPage.arduino.CheckLeftLimitAsync();

                if (status == "left_limit_istriggered")
                {
                    MessageBox.Show("左側極限開關：已觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (status == "left_limit_nottriggered")
                {
                    MessageBox.Show("左側極限開關：未觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"檢查左側極限失敗或收到非預期回應: {status}", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"檢查左側極限時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        private async void button_check_right_limit_Click(object sender, EventArgs e)
        {
            try
            {
                // 呼叫 Arduino 類別中的 CheckRightLimitAsync 方法
                // 並將回傳的狀態字串賦值給 status 變數
                string status = await MainPage.arduino.CheckRightLimitAsync();

                if (status == "right_limit_istriggered")
                {
                    MessageBox.Show("右側極限開關：已觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (status == "right_limit_nottriggered")
                {
                    MessageBox.Show("右側極限開關：未觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"檢查右側極限失敗或收到非預期回應: {status}", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"檢查右側極限時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void UserControl_Setting_Load(object sender, EventArgs e)
        {

        }

        private async void Gohome_button_Click(object sender, EventArgs e)
        {
            try
            {
                // 禁用按鈕，避免重複點擊
                Gohome_button.Enabled = false;

                string response = await MainPage.arduino.GoHomeAsync();

                if (response == "leftmove_finish")
                {
                    MessageBox.Show("馬達已成功回原點。", "動作完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"回原點失敗或收到非預期回應: {response}", "動作結果", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"回原點時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 無論成功或失敗，最後都恢復按鈕
                Gohome_button.Enabled = true;
            }
        }
    }
}
