using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using StandardOPage;
using Emgu.CV.Structure;
using System.Threading;

namespace UNIVACCO_UI
{
    public partial class UserControl_TempStandard : UserControl
    {
        UNIVACCO MainPage;
        UserControl_Main StandardOPage;
        List<Mat> meshp_areas;
        List<double> meshp_dectectpixels;
        List<double> autotune_thresholds = new List<double>();
        private Mat SourceImage0, SourceImage1, SourceImage2;
        public UserControl_TempStandard(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            InitializeComponent();
            StandardOPage = _StandardOPage;
            MainPage = _MainPage;
            ClearUI();
            UpdateUI_GetAllStandards();
        }
        public async Task CameraTrigger_ForAutoTune()
        {
            int movebias;
            double rotationAngle;
            try
            {
                
                MainPage.ArduinoOpenGreenLight();
                await Task.Delay(1000);
                //首次拍攝，計算移動誤差與斜率
                MainPage.camera.CaptureOneFrame();
                Thread.Sleep(400);//Delay久一點避免影像來不及傳來PC      
                SourceImage0 = new Bitmap(StandardOPage.pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                CvInvoke.Imwrite("SourceImage_first.bmp", SourceImage0);
                var result = StandardOPage.calc_motor_movebias(SourceImage0.Clone());
                movebias = result.movebias;
                rotationAngle = result.rotationAngle;
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "PictureBox_SourceImg為Null", ex.ToString());
                MessageBox.Show("計算馬達移動偏差與斜率失敗", "系統視窗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            try
            {
                //馬達移動
                await MainPage.arduino.SpecificRightMoveAsync(movebias);
                await Task.Delay(200);
                //拍攝網點區1
                MainPage.camera.CaptureOneFrame();
                Thread.Sleep(400);
                SourceImage1 = new Bitmap(StandardOPage.pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                CvInvoke.Imwrite("SourceImage_O1.bmp", SourceImage1);

                //馬達移動
                await MainPage.arduino.SpecificRightMoveAsync(2350);
                await Task.Delay(200);
                //拍攝網點區2
                MainPage.camera.CaptureOneFrame();
                Thread.Sleep(400);
                MainPage.ArduinoCloseGreenLight();
                await MainPage.arduino.GoHomeAsync();                    //等待回到原點
                SourceImage2 = new Bitmap(StandardOPage.pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                CvInvoke.Imwrite("SourceImage_O2.bmp", SourceImage2);
                //影像處理網點區
                var task_meshp = await StandardOPage.Autotune(SourceImage1, SourceImage2, StandardOPage.CurrentPrintType_Params);
                UpdateUI_Autotune(task_meshp.Value.meshp_areas, task_meshp.Value.meshp_dectectpixels, task_meshp.Value.autotune_thresholds);
                button_Save.Enabled = true;
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "PictureBox_SourceImg為Null", ex.ToString());
                MessageBox.Show("請檢查待測物是否放置妥當，並確認曝光時間設定是否適當。", "系統視窗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        #region UI元件
        private async void button_Start_Click(object sender, EventArgs e)
        {
            if (button_Save.Enabled)
            {
                if (MessageBox.Show("剛剛的結果尚未儲存，確定要繼續嗎?", "資訊", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
                //button_Save.Enabled = false;
            }
            ClearUI();
            //自動校正
            await CameraTrigger_ForAutoTune();

            //var result = CameraTrigger_ForAutoTune();
            //if (result == null)
            //{
            //    MessageBox.Show("馬達移動錯誤", "系統視窗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}
     
        }
        private void button_Save_Click(object sender, EventArgs e)
        {
            if (textBox_Savename.Text == "")
            {
                MessageBox.Show("儲存名稱未填寫");
                return;
            }
            bool is_insertsuccessful = false;
            string message;
            (is_insertsuccessful,message) = StandardOPage.MeshP_Param.InsertOneData("TempMeshP",
                new OCT_ParameterMeshP
                {
                    Name = textBox_Savename.Text,
                    Area10 = autotune_thresholds[0],
                    Area20 = autotune_thresholds[1],
                    Area30 = autotune_thresholds[2],
                    Area40 = autotune_thresholds[3],
                    Area60 = autotune_thresholds[4],
                    Area70 = autotune_thresholds[5],
                    Area80 = autotune_thresholds[6],
                    Area90 = autotune_thresholds[7]
                });
            //新增資料庫失敗錯誤機制
            if (!is_insertsuccessful)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, $"{message}");
                MessageBox.Show($"{message}", "系統視窗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            UpdateUI_GetAllStandards();
            button_Save.Enabled = false;
            ClearUI();
        }
        private void radioButton_PrintType_changed(object sender, EventArgs e)
        {
            if (radioButton_LabPrint.Checked)
            {
                StandardOPage.CurrentPrintType_Params = StandardOPage.LabPrint_Params;
                StandardOPage.radioButton_LabPrint.Checked = true;
                StandardOPage.radioButton_OffsetPrint.Checked = false;
            }
            else
            {
                StandardOPage.CurrentPrintType_Params = StandardOPage.OffsetPrint_Params;
                StandardOPage.radioButton_LabPrint.Checked = false;
                StandardOPage.radioButton_OffsetPrint.Checked = true;
            }
        }
        #endregion

        #region UI狀態更新
        private void UpdateUI(Action uiAction)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(uiAction);
            }
            else
            {
                uiAction();
            }
        }
        private void UpdateUI_Autotune(List<Mat> meshp_areas, List<double> meshp_dectectpixels, List<double> autotune_thresholds)
        {
            // 將參數賦值給類別成員變數
            this.meshp_areas = meshp_areas;
            this.meshp_dectectpixels = meshp_dectectpixels;
            this.autotune_thresholds = autotune_thresholds;

            UpdateUI(() => pictureBox_MeshP10.Image = meshp_areas[0].ToBitmap());
            UpdateUI(() => pictureBox_MeshP20.Image = meshp_areas[1].ToBitmap());
            UpdateUI(() => pictureBox_MeshP30.Image = meshp_areas[2].ToBitmap());
            UpdateUI(() => pictureBox_MeshP40.Image = meshp_areas[3].ToBitmap());
            UpdateUI(() => pictureBox_MeshP60.Image = meshp_areas[4].ToBitmap());
            UpdateUI(() => pictureBox_MeshP70.Image = meshp_areas[5].ToBitmap());
            UpdateUI(() => pictureBox_MeshP80.Image = meshp_areas[6].ToBitmap());
            UpdateUI(() => pictureBox_MeshP90.Image = meshp_areas[7].ToBitmap());
            UpdateUI(() => label_MeshP_Result10.Text = meshp_dectectpixels[0].ToString());
            UpdateUI(() => label_MeshP_Result20.Text = meshp_dectectpixels[1].ToString());
            UpdateUI(() => label_MeshP_Result30.Text = meshp_dectectpixels[2].ToString());
            UpdateUI(() => label_MeshP_Result40.Text = meshp_dectectpixels[3].ToString());
            UpdateUI(() => label_MeshP_Result60.Text = meshp_dectectpixels[4].ToString());
            UpdateUI(() => label_MeshP_Result70.Text = meshp_dectectpixels[5].ToString());
            UpdateUI(() => label_MeshP_Result80.Text = meshp_dectectpixels[6].ToString());
            UpdateUI(() => label_MeshP_Result90.Text = meshp_dectectpixels[7].ToString());
            UpdateUI(() => label_MeshP_Threshold10.Text = autotune_thresholds[0].ToString());
            UpdateUI(() => label_MeshP_Threshold20.Text = autotune_thresholds[1].ToString());
            UpdateUI(() => label_MeshP_Threshold30.Text = autotune_thresholds[2].ToString());
            UpdateUI(() => label_MeshP_Threshold40.Text = autotune_thresholds[3].ToString());
            UpdateUI(() => label_MeshP_Threshold60.Text = autotune_thresholds[4].ToString());
            UpdateUI(() => label_MeshP_Threshold70.Text = autotune_thresholds[5].ToString());
            UpdateUI(() => label_MeshP_Threshold80.Text = autotune_thresholds[6].ToString());
            UpdateUI(() => label_MeshP_Threshold90.Text = autotune_thresholds[7].ToString());

        }
        private void ClearUI()
        {
            // 🔹 清除所有 PictureBox 的影像
            PictureBox[] pictureBoxes =
            {
                pictureBox_MeshP10,
                pictureBox_MeshP20,
                pictureBox_MeshP30,
                pictureBox_MeshP40,
                pictureBox_MeshP60,
                pictureBox_MeshP70,
                pictureBox_MeshP80,
                pictureBox_MeshP90
            };

            foreach (var pictureBox in pictureBoxes)
            {
                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose();
                    pictureBox.Image = null;
                }
            }

            // 🔹 清除所有 Label 文字
            Label[] labels =
            {
                label_MeshP_Result10,
                label_MeshP_Result20,
                label_MeshP_Result30,
                label_MeshP_Result40,
                label_MeshP_Result60,
                label_MeshP_Result70,
                label_MeshP_Result80,
                label_MeshP_Result90,
                label_MeshP_Threshold10,
                label_MeshP_Threshold20,
                label_MeshP_Threshold30,
                label_MeshP_Threshold40,
                label_MeshP_Threshold60,
                label_MeshP_Threshold70,
                label_MeshP_Threshold80,
                label_MeshP_Threshold90,
            };

            foreach (var label in labels)
            {
                label.Text = string.Empty;
            }
        }
        private void UpdateUI_GetAllStandards()
        {
            List<OCT_ParameterMeshP> temp_standards = StandardOPage.MeshP_Param.GetAllData("TempMeshP"); // 取得資料
            DataGridView1.Rows.Clear(); // 先清空表格，避免重複顯示
            foreach (OCT_ParameterMeshP temp_standard in temp_standards)
            {
                DataGridView1.Rows.Add(
                    temp_standard.Name,                    
                    (int)Math.Round(temp_standard.Area10),
                    (int)Math.Round(temp_standard.Area20),
                    (int)Math.Round(temp_standard.Area30),
                    (int)Math.Round(temp_standard.Area40),
                    (int)Math.Round(temp_standard.Area60),
                    (int)Math.Round(temp_standard.Area70),
                    (int)Math.Round(temp_standard.Area80),
                    (int)Math.Round(temp_standard.Area90)
                );
            }
        }
        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //認定標題欄為"刪除"時才跳出
            if (DataGridView1.Columns[e.ColumnIndex].HeaderText == "刪除" && e.RowIndex >= 0)
            {
                var confirm = MessageBox.Show("確定要刪除這筆資料嗎？", "確認", MessageBoxButtons.YesNo);
                if (confirm == DialogResult.Yes)
                {
                    //取出第一欄的name
                    string name = DataGridView1.Rows[e.RowIndex].Cells[0].Value?.ToString();
                    //刪除資料
                    try 
                    {
                        StandardOPage.MeshP_Param.DeleteOneData("TempMeshP", name);
                    }
                    catch(Exception ex)
                    {
                        OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "新增臨時標準錯誤", ex.ToString());
                        MessageBox.Show("刪除臨時標準錯誤，請聯絡工程人員", "系統視窗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    //因為UpdateUI_GetAllStandards內有刪除表格動作，但現在還在表格的觸發狀態中，要安排在事件後才執行
                    //重新抓資料顯示
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        UpdateUI_GetAllStandards();
                    }));
                }
            }
        }
        #endregion


    }
}
