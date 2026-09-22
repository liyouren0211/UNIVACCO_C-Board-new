using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Diagnostics;
using Basler.Pylon;
using System.Windows.Media.Media3D;

namespace UNIVACCO_UI
{
    public partial class LightCorrection : Form
    {
        UNIVACCO MainPage;
        string logString;

        bool firstFrameDisplayed = false;
        private float zoomFactor = 1.0f;
        private Point offset = new Point(0, 0);
        private Point panStartPoint;
        private bool isPanning = false;
        private volatile bool VedioCam_Flag = false;

        public LightCorrection(UNIVACCO _MainPage)
        {
            InitializeComponent();
            MainPage = _MainPage;

            // 系統log
            logString = "光源校正頁面開啟成功!";
            MainPage.WriteLog("System_LightCorrectionPage", logString);

            // 載入 O 版曝光時間
            textBox_ExposureTime.Text = MainPage.ExposureTime_OCT.ToString();

            // 綁定滑鼠操作
            pictureBox_SourceImg.MouseWheel += pictureBox_SourceImg_MouseWheel;
            pictureBox_SourceImg.MouseDown += pictureBox_SourceImg_MouseDown;
            pictureBox_SourceImg.MouseMove += pictureBox_SourceImg_MouseMove;
            pictureBox_SourceImg.MouseUp += pictureBox_SourceImg_MouseUp;
            pictureBox_SourceImg.Paint += pictureBox_SourceImg_Paint;

            SetInitialImageDisplay();
            if (pictureBox_SourceImg.Image != null)
            {
                pictureBox_SourceImg.Image.Dispose();
                pictureBox_SourceImg.Image = null;
            }
        }

        // ========= 相機串流開啟 =========
        private void StartVideoStream()
        {
            CleanupCameraEvents(); // 確保事件乾淨
            MainPage.camera.CameraImageEvent += Camera_LightCorrectionImageEvent;

            VedioCam_Flag = true;
            firstFrameDisplayed = false;

            // O版只需要綠光
            MainPage.ArduinoOpenGreenLight();

            // 開始串流
            MainPage.camera.KeepShot();

            Thread.Sleep(200); // 適度延遲，避免剛啟動黑屏
        }

        // ========= 相機串流關閉 =========
        private async Task StopVideoStream()
        {
            if (!VedioCam_Flag) return;
            VedioCam_Flag = false;

            // 用 await Task.Delay 取代 Thread.Sleep，不阻塞UI執行緒
            await Task.Delay(150);

            // 關燈
            MainPage.ArduinoCloseGreenLight();

            // 停止相機
            MainPage.camera.Stop();

            // 移除事件，避免重複綁定
            MainPage.camera.CameraImageEvent -= Camera_LightCorrectionImageEvent;

            // 清除畫面資源
            ClearPictureBox();

            await Task.Delay(50);
        }
        

        private async void button_VedioCam_Click(object sender, EventArgs e)
        {
            if (!VedioCam_Flag)
                StartVideoStream();
            else
                await StopVideoStream();
        }

        // ========= 曝光時間調整 =========

        private CancellationTokenSource exposureCts = new CancellationTokenSource();
        private readonly object exposureLock = new object();
        private volatile bool isAdjustingExposure = false; // 新增：防止重複執行
                                                           // 優化後的曝光時間調整方法
        private async void button_ExposureTimeSave_Click(object sender, EventArgs e)
        {
            logString =
                           String.Format(
                               "按下曝光時間儲存按鈕");
            MainPage.WriteLog("System_LightCorrectionPage", logString);

            bool is_camerakeeping = false;
            //判斷相機是否正在串流需先暫停
            if (MainPage.camera.Graber)
            {
                is_camerakeeping = true;
                MainPage.camera.Stop();
            }

            if (MessageBox.Show("你確定要儲存？", "資訊", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (!int.TryParse(textBox_ExposureTime.Text, out int expTime))
                {
                    MessageBox.Show("請輸入有效數值！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox_ExposureTime.Text = MainPage.ExposureTime_OCT.ToString();
                    return;
                }
                MainPage.camera.ExposureTime(Convert.ToInt32(textBox_ExposureTime.Text));

                MainPage.IniWriteValue("Camera", "ExposureTime", textBox_ExposureTime.Text, "OCTParameters.ini");
                MainPage.ExposureTime_OCT = Convert.ToInt32(textBox_ExposureTime.Text);
                MainPage.ExposureTime = MainPage.ExposureTime_OCT;
                logString =
                String.Format(
                    "O版冷燙曝光時間調整完成" +
                    "目前曝光時間為:" + textBox_ExposureTime.Text);
                MainPage.WriteLog("System_LightCorrectionPage", logString);

                if (is_camerakeeping)
                {
                    MainPage.camera.KeepShot();
                }
            }
        }


        // ========= 相機影像事件 =========
        public void Camera_LightCorrectionImageEvent(Bitmap bmp)
        {
            if (bmp == null || !VedioCam_Flag || this.IsDisposed)
            {
                bmp?.Dispose();
                return;
            }

            if (pictureBox_SourceImg.InvokeRequired)
            {
                try
                {
                    // 改用 BeginInvoke，不等待UI執行緒，避免死鎖
                    pictureBox_SourceImg.BeginInvoke(new MethodInvoker(() =>
                    {
                        if (!this.IsDisposed && VedioCam_Flag)
                            UpdatePictureBoxSafely(bmp);
                        else
                            bmp?.Dispose();
                    }));
                }
                catch
                {
                    bmp?.Dispose();
                }
            }
            else
            {
                UpdatePictureBoxSafely(bmp);
            }
        }

        private void UpdatePictureBoxSafely(Bitmap bmp)
        {
            if (!VedioCam_Flag)
            {
                bmp?.Dispose();
                return;
            }

            try
            {
                SafeDisposeImage(pictureBox_SourceImg);
                pictureBox_SourceImg.Image = bmp;

                if (!firstFrameDisplayed)
                {
                    SetInitialImageDisplay();
                    firstFrameDisplayed = true;
                }
            }
            catch (Exception ex)
            {
                bmp?.Dispose();
                logString = $"更新圖像錯誤: {ex.Message}";
                MainPage.WriteLog("System_LightCorrectionPage", logString);
            }
        }

        // ========= 視窗關閉 =========
        private async void LightCorrection_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                await StopVideoStream();

                await Task.Delay(100);

                // 掛回主畫面事件並重啟串流
                MainPage.camera.CameraImageEvent -= MainPage.Camera_CameraImageEvent;
                MainPage.camera.CameraImageEvent += MainPage.Camera_CameraImageEvent;
                MainPage.camera.KeepShot();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"關閉曝光調整頁面時發生錯誤：{ex.Message}",
                    "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========= 工具函式 =========
        private void CleanupCameraEvents()
        {
            try { MainPage.camera.CameraImageEvent -= MainPage.Camera_CameraImageEvent; } catch { }
            try { MainPage.camera.CameraImageEvent -= Camera_LightCorrectionImageEvent; } catch { }
        }

        private void ClearPictureBox()
        {
            if (pictureBox_SourceImg.InvokeRequired)
            {
                pictureBox_SourceImg.Invoke(new MethodInvoker(() =>
                {
                    SafeDisposeImage(pictureBox_SourceImg);
                }));
            }
            else
            {
                SafeDisposeImage(pictureBox_SourceImg);
            }
        }

        private void SafeDisposeImage(PictureBox pictureBox)
        {
            if (pictureBox.Image != null)
            {
                var oldImage = pictureBox.Image;
                pictureBox.Image = null;
                oldImage.Dispose();
            }
        }

        // ========= 縮放 & 平移 =========
        private void SetInitialImageDisplay()
        {
            if (pictureBox_SourceImg.Image == null)
            {
                return;
            }            // 獲取圖片的尺寸與 PictureBox 的尺寸
            float imgWidth = pictureBox_SourceImg.Image.Width;
            float imgHeight = pictureBox_SourceImg.Image.Height;
            float boxWidth = pictureBox_SourceImg.Width;
            float boxHeight = pictureBox_SourceImg.Height;

            // 使用與滾輪事件中相同的最小縮放比例計算方法
            zoomFactor = Math.Max(
                (float)pictureBox_SourceImg.Width / pictureBox_SourceImg.Image.Width,
                (float)pictureBox_SourceImg.Height / pictureBox_SourceImg.Image.Height
            );

            // 計算初始的偏移量，讓圖片居中顯示
            offset.X = (int)((boxWidth - imgWidth * zoomFactor) / 2);
            offset.Y = (int)((boxHeight - imgHeight * zoomFactor) / 2);

            pictureBox_SourceImg.Invalidate(); // 重繪畫面
        }

        private void pictureBox_SourceImg_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox_SourceImg.Image == null) return; // 如果圖片為空，直接返回

            float oldZoomFactor = zoomFactor;
            float zoomIncrement = 0.1f;

            // 計算最小縮放比例，確保圖片能完全填滿 PictureBox
            float minZoomFactor = Math.Max(
                (float)pictureBox_SourceImg.Width / pictureBox_SourceImg.Image.Width,
                (float)pictureBox_SourceImg.Height / pictureBox_SourceImg.Image.Height
            );

            // 計算 PictureBox 中滑鼠指針對應的圖片座標
            float mouseX = (e.X - offset.X) / zoomFactor;
            float mouseY = (e.Y - offset.Y) / zoomFactor;

            // 更新縮放比例
            if (e.Delta > 0) // 滾輪向上：放大
            {
                zoomFactor += zoomIncrement;
            }
            else if (e.Delta < 0) // 滾輪向下：縮小
            {
                zoomFactor -= zoomIncrement;
            }

            // 限制縮放比例不能小於 minZoomFactor
            zoomFactor = Math.Max(zoomFactor, minZoomFactor);

            // 計算新的偏移量，保持縮放中心不變
            offset.X = (int)(e.X - mouseX * zoomFactor);
            offset.Y = (int)(e.Y - mouseY * zoomFactor);

            // 確保縮小時圖像不會有空白區域
            int imageWidth = (int)(pictureBox_SourceImg.Image.Width * zoomFactor);
            int imageHeight = (int)(pictureBox_SourceImg.Image.Height * zoomFactor);

            // 如果縮放後的圖像小於 PictureBox，則居中顯示
            if (imageWidth < pictureBox_SourceImg.Width)
            {
                offset.X = (pictureBox_SourceImg.Width - imageWidth) / 2;
            }
            else
            {
                // 確保左邊界不會出現空白
                offset.X = Math.Min(offset.X, 0);
                // 確保右邊界不會出現空白
                offset.X = Math.Max(offset.X, pictureBox_SourceImg.Width - imageWidth);
            }

            if (imageHeight < pictureBox_SourceImg.Height)
            {
                offset.Y = (pictureBox_SourceImg.Height - imageHeight) / 2;
            }
            else
            {
                // 確保上邊界不會出現空白
                offset.Y = Math.Min(offset.Y, 0);
                // 確保下邊界不會出現空白
                offset.Y = Math.Max(offset.Y, pictureBox_SourceImg.Height - imageHeight);
            }

            pictureBox_SourceImg.Invalidate(); // 使用 Refresh 代替 Invalidate，強制立即重繪
        }

        private void pictureBox_SourceImg_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = true;
                panStartPoint = e.Location;
            }
        }

        private void pictureBox_SourceImg_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && pictureBox_SourceImg.Image != null)
            {
                // 計算圖片縮放後的寬高
                float scaledWidth = pictureBox_SourceImg.Image.Width * zoomFactor;
                float scaledHeight = pictureBox_SourceImg.Image.Height * zoomFactor;

                // 更新偏移量
                offset.X += e.X - panStartPoint.X;
                offset.Y += e.Y - panStartPoint.Y;

                // 計算邊界範圍
                int maxX = 0; // 圖片左邊界
                int minX = (int)(pictureBox_SourceImg.Width - scaledWidth); // 圖片右邊界
                int maxY = 0; // 圖片上邊界
                int minY = (int)(pictureBox_SourceImg.Height - scaledHeight); // 圖片下邊界

                // 限制 offset 不超出邊界
                if (offset.X > maxX) offset.X = maxX;
                if (offset.X < minX) offset.X = minX;
                if (offset.Y > maxY) offset.Y = maxY;
                if (offset.Y < minY) offset.Y = minY;

                // 更新滑鼠起點
                panStartPoint = e.Location;

                // 重繪畫面
                pictureBox_SourceImg.Invalidate();
            }
        }

        private void pictureBox_SourceImg_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = false;
            }
        }

        private void pictureBox_SourceImg_Paint(object sender, PaintEventArgs e)
        {
            // 確保圖片已經加載
            if (pictureBox_SourceImg.Image == null) return;

            // 創建 Graphics 物件來繪製圖形
            Graphics g = e.Graphics;

            // 先繪製縮放和平移後的圖片
            g.DrawImage(pictureBox_SourceImg.Image,
                new Rectangle(offset.X, offset.Y,
                (int)(pictureBox_SourceImg.Image.Width * zoomFactor),
                (int)(pictureBox_SourceImg.Image.Height * zoomFactor)));
        }

        private void label_ExposureTime_Click(object sender, EventArgs e)
        {

        }

        private void textBox_ExposureTime_TextChanged(object sender, EventArgs e)
        {

        }

        private void button_Correction_Click(object sender, EventArgs e)
        {

        }

        private void LightCorrection_Load(object sender, EventArgs e)
        {

        }

        private void Motor_moves_right_button_Click(object sender, EventArgs e)
        {
            if (int.TryParse(Pixels_setting_textBox.Text, out int pixels))
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
                    MessageBox.Show("左側極限開關：已觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (status == "left_limit_nottriggered")
                    MessageBox.Show("左側極限開關：未觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show($"檢查左側極限失敗或收到非預期回應: {status}", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                string status = await MainPage.arduino.CheckRightLimitAsync();

                if (status == "right_limit_istriggered")
                    MessageBox.Show("右側極限開關：已觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else if (status == "right_limit_nottriggered")
                    MessageBox.Show("右側極限開關：未觸發。", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show($"檢查右側極限失敗或收到非預期回應: {status}", "限位檢查", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"檢查右側極限時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
