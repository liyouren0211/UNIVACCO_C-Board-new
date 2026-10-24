using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace UNIVACCO_UI
{
    /// <summary>
    /// AOI 即時預覽視窗。
    /// 視窗內只顯示影像。
    ///
    /// 操作方式：
    /// 1. 滑鼠滾輪：以滑鼠位置為中心放大 / 縮小。
    /// 2. 滑鼠左鍵拖曳：移動畫面。
    /// 3. 滑鼠左鍵雙擊：恢復 Fit 顯示。
    ///
    /// 調整 Threshold 造成影像更新時，如果影像尺寸沒有改變，
    /// 會保留目前的縮放倍率與拖曳位置，方便持續觀察同一個細節。
    /// </summary>
    public class Form_LivePreview : Form
    {
        private readonly UserControl_Main standardOPage;
        private PictureBox pictureBoxPreview;

        private Bitmap currentImage;

        // 目前顯示倍率。
        private float zoom = 1.0f;

        // 影像左上角在 PictureBox 裡的位置。
        private PointF imageOffset = PointF.Empty;

        // 拖曳相關狀態。
        private bool isDragging = false;
        private Point lastMousePoint;

        // 縮放上下限。
        private const float MinZoom = 0.05f;
        private const float MaxZoom = 20.0f;
        private const float ZoomStep = 1.15f;

        public Form_LivePreview(UserControl_Main page)
        {
            standardOPage = page;
            BuildUI();
        }

        private void BuildUI()
        {
            Text = "AOI 即時預覽";
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(1000, 760);
            MinimumSize = new Size(500, 400);
            MaximizeBox = true;
            BackColor = Color.Black;
            KeyPreview = true;

            pictureBoxPreview = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Normal,
                TabStop = true
            };

            pictureBoxPreview.Paint += PictureBoxPreview_Paint;
            pictureBoxPreview.MouseWheel += PictureBoxPreview_MouseWheel;
            pictureBoxPreview.MouseDown += PictureBoxPreview_MouseDown;
            pictureBoxPreview.MouseMove += PictureBoxPreview_MouseMove;
            pictureBoxPreview.MouseUp += PictureBoxPreview_MouseUp;
            pictureBoxPreview.MouseDoubleClick += PictureBoxPreview_MouseDoubleClick;
            pictureBoxPreview.MouseEnter += PictureBoxPreview_MouseEnter;
            pictureBoxPreview.Resize += PictureBoxPreview_Resize;

            Controls.Add(pictureBoxPreview);

            FormClosed += Form_LivePreview_FormClosed;

            // 只控制視窗位置，不改任何預覽影像或門檻計算。
            Shown += delegate { ArrangeOnRightOfOwner(); };
        }

        /// <summary>
        /// 將預覽視窗安排在工程設定視窗右方。
        /// 依目前螢幕工作區縮放預覽大小，避免覆蓋左側參數。
        /// </summary>
        private void ArrangeOnRightOfOwner()
        {
            Form setting = Owner;
            Screen monitor = setting != null ? Screen.FromControl(setting) : Screen.FromControl(this);
            Rectangle area = monitor.WorkingArea;
            int gap = 8;
            int leftEdge = setting != null ? Math.Max(area.Left, setting.Right) : area.Left + area.Width / 2;
            int availableWidth = area.Right - leftEdge - gap;

            // 螢幕空間不足時，先縮小工程窗（僅改視窗大小，不改內容）。
            if (setting != null && availableWidth < 480)
            {
                int targetSettingWidth = Math.Max(700, area.Width - 480 - gap);
                if (setting.Width > targetSettingWidth)
                {
                    setting.Width = targetSettingWidth;
                    setting.Location = new Point(area.Left, area.Top);
                    leftEdge = setting.Right;
                    availableWidth = area.Right - leftEdge - gap;
                }
            }

            if (availableWidth >= 400)
            {
                WindowState = FormWindowState.Normal;
                // MinimumSize=500x400（含視窗邊框），優先安排左右並排。
                int width = Math.Max(500, Math.Min(1000, availableWidth));
                Size = new Size(width, Math.Min(800, area.Height));
                Location = new Point(Math.Max(area.Left, area.Right - width), area.Top);
            }
            else
            {
                // 極窄的螢幕無法同時放下兩個可用視窗，優先確保預覽可見。
                Location = new Point(Math.Max(area.Left, area.Right - Width), area.Top);
            }
        }

        /// <summary>
        /// 顯示網點區二值化即時預覽。
        /// </summary>
        public void ShowMesh(int areaPercent, double thresholdPercent)
        {
            thresholdPercent = ClampPercent(thresholdPercent);

            using (Mat raw = standardOPage.GetMeshPreviewSource(areaPercent))
            {
                if (raw == null || raw.IsEmpty)
                {
                    ClearImage();
                    return;
                }

                using (Mat source = CropBlackPadding(raw))
                using (Mat gray = new Mat())
                using (Mat binary = new Mat())
                {
                    ToGray(source, gray);

                    CvInvoke.Threshold(
                        gray,
                        binary,
                        thresholdPercent * 2.55,
                        255,
                        ThresholdType.Binary);

                    ShowBitmap(binary.ToBitmap());
                }
            }
        }

        /// <summary>
        /// 顯示字體區二值化即時預覽。
        /// </summary>
        public void ShowFont(bool isYin, int pointSize, double thresholdPercent)
        {
            thresholdPercent = ClampPercent(thresholdPercent);

            using (Mat source = standardOPage.GetFontPreviewSource(isYin, pointSize))
            {
                if (source == null || source.IsEmpty)
                {
                    ClearImage();
                    return;
                }

                using (Mat gray = new Mat())
                using (Mat binary = new Mat())
                {
                    ToGray(source, gray);

                    CvInvoke.Threshold(gray, binary, thresholdPercent * 2.55,
                        255, ThresholdType.Binary);
                    // Native formal detection normalizes both sides to white foreground.
                    if (!isYin) CvInvoke.BitwiseNot(binary, binary);
                    Text = (isYin ? "陰版" : "陽版") + " " + pointSize + "pt | "
                        + thresholdPercent.ToString("F1") + "% | T="
                        + (thresholdPercent * 2.55).ToString("F1")
                        + " | 原始灰階 [" + GrayMin(gray) + "]";
                    ShowBitmap(binary.ToBitmap());
                }
            }
        }

        private string GrayMin(Mat gray)
        {
            double min = 0, max = 0;
            Point minLoc = Point.Empty, maxLoc = Point.Empty;
            CvInvoke.MinMaxLoc(gray, ref min, ref max, ref minLoc, ref maxLoc);
            return min.ToString("F0") + "~" + max.ToString("F0");
        }

        /// <summary>
        /// 自動去除網點 ROI 外圍的純黑 padding，
        /// 避免有效網點只佔預覽畫面的一部分。
        /// </summary>
        private Mat CropBlackPadding(Mat source)
        {
            using (Mat gray = new Mat())
            using (Mat mask = new Mat())
            using (VectorOfPoint points = new VectorOfPoint())
            {
                ToGray(source, gray);

                CvInvoke.Threshold(
                    gray,
                    mask,
                    5,
                    255,
                    ThresholdType.Binary);

                CvInvoke.FindNonZero(mask, points);

                if (points.Size <= 0)
                    return source.Clone();

                Rectangle rect = CvInvoke.BoundingRectangle(points);
                rect.Intersect(new Rectangle(Point.Empty, source.Size));

                if (rect.Width < 10 || rect.Height < 10)
                    return source.Clone();

                return new Mat(source, rect).Clone();
            }
        }

        private void ToGray(Mat source, Mat gray)
        {
            if (source.NumberOfChannels == 1)
            {
                source.CopyTo(gray);
            }
            else
            {
                CvInvoke.CvtColor(
                    source,
                    gray,
                    ColorConversion.Bgr2Gray);
            }
        }

        /// <summary>
        /// 更新預覽影像。
        /// 相同尺寸的新影像會保留目前 Zoom / Pan 狀態，
        /// 讓使用者改參數時不會一直跳回整張圖。
        /// </summary>
        private void ShowBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                ClearImage();
                return;
            }

            if (pictureBoxPreview.InvokeRequired)
            {
                Bitmap copy = new Bitmap(bitmap);
                bitmap.Dispose();

                pictureBoxPreview.BeginInvoke(
                    new Action<Bitmap>(ShowBitmap),
                    copy);

                return;
            }

            bool needResetView =
                currentImage == null ||
                currentImage.Width != bitmap.Width ||
                currentImage.Height != bitmap.Height;

            Bitmap newImage = new Bitmap(bitmap);
            bitmap.Dispose();

            Bitmap oldImage = currentImage;
            currentImage = newImage;
            oldImage?.Dispose();

            if (needResetView)
            {
                FitImageToWindow();
            }
            else
            {
                pictureBoxPreview.Invalidate();
            }
        }

        /// <summary>
        /// 滑鼠滾輪縮放。
        /// 縮放中心固定在目前滑鼠位置，操作時比較直覺。
        /// </summary>
        private void PictureBoxPreview_MouseWheel(object sender, MouseEventArgs e)
        {
            if (currentImage == null)
                return;

            float oldZoom = zoom;

            if (e.Delta > 0)
                zoom *= ZoomStep;
            else if (e.Delta < 0)
                zoom /= ZoomStep;

            zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));

            if (Math.Abs(zoom - oldZoom) < 0.0001f)
                return;

            float zoomRatio = zoom / oldZoom;

            // 讓滑鼠指到的影像位置在縮放前後維持在相同螢幕座標。
            imageOffset.X =
                e.X - (e.X - imageOffset.X) * zoomRatio;

            imageOffset.Y =
                e.Y - (e.Y - imageOffset.Y) * zoomRatio;

            pictureBoxPreview.Invalidate();
        }

        /// <summary>
        /// 左鍵開始拖曳。
        /// </summary>
        private void PictureBoxPreview_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentImage == null)
                return;

            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePoint = e.Location;
                pictureBoxPreview.Cursor = Cursors.Hand;
                pictureBoxPreview.Capture = true;
            }
        }

        /// <summary>
        /// 左鍵拖曳移動畫面。
        /// </summary>
        private void PictureBoxPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || currentImage == null)
                return;

            int dx = e.X - lastMousePoint.X;
            int dy = e.Y - lastMousePoint.Y;

            imageOffset.X += dx;
            imageOffset.Y += dy;

            lastMousePoint = e.Location;

            pictureBoxPreview.Invalidate();
        }

        /// <summary>
        /// 結束拖曳。
        /// </summary>
        private void PictureBoxPreview_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                pictureBoxPreview.Cursor = Cursors.Default;
                pictureBoxPreview.Capture = false;
            }
        }

        /// <summary>
        /// 雙擊恢復整張 Fit 顯示。
        /// </summary>
        private void PictureBoxPreview_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                FitImageToWindow();
            }
        }

        /// <summary>
        /// 滑鼠進入預覽區後取得焦點，確保 MouseWheel 可以直接使用。
        /// </summary>
        private void PictureBoxPreview_MouseEnter(object sender, EventArgs e)
        {
            pictureBoxPreview.Focus();
        }

        /// <summary>
        /// 視窗改變大小時只重新繪製。
        /// 不強迫 Reset Zoom，避免使用者放大後視窗一變就跳掉。
        /// </summary>
        private void PictureBoxPreview_Resize(object sender, EventArgs e)
        {
            pictureBoxPreview.Invalidate();
        }

        /// <summary>
        /// 自動把整張圖縮放到目前預覽視窗內並置中。
        /// </summary>
        private void FitImageToWindow()
        {
            if (currentImage == null ||
                pictureBoxPreview.ClientSize.Width <= 0 ||
                pictureBoxPreview.ClientSize.Height <= 0)
            {
                return;
            }

            float scaleX =
                (float)pictureBoxPreview.ClientSize.Width /
                currentImage.Width;

            float scaleY =
                (float)pictureBoxPreview.ClientSize.Height /
                currentImage.Height;

            zoom = Math.Min(scaleX, scaleY);
            zoom = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));

            float drawWidth = currentImage.Width * zoom;
            float drawHeight = currentImage.Height * zoom;

            imageOffset = new PointF(
                (pictureBoxPreview.ClientSize.Width - drawWidth) / 2.0f,
                (pictureBoxPreview.ClientSize.Height - drawHeight) / 2.0f);

            pictureBoxPreview.Invalidate();
        }

        /// <summary>
        /// 由 Paint 自行繪製影像，才能同時支援 Zoom / Pan。
        /// </summary>
        private void PictureBoxPreview_Paint(object sender, PaintEventArgs e)
        {
            if (currentImage == null)
                return;

            float drawWidth = currentImage.Width * zoom;
            float drawHeight = currentImage.Height * zoom;

            RectangleF destination = new RectangleF(
                imageOffset.X,
                imageOffset.Y,
                drawWidth,
                drawHeight);

            // AOI 二值圖使用 NearestNeighbor，放大時不做模糊插值。
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            e.Graphics.SmoothingMode = SmoothingMode.None;

            e.Graphics.DrawImage(currentImage, destination);
        }

        private void ClearImage()
        {
            if (pictureBoxPreview == null)
                return;

            if (pictureBoxPreview.InvokeRequired)
            {
                pictureBoxPreview.BeginInvoke(new Action(ClearImage));
                return;
            }

            Bitmap oldImage = currentImage;
            currentImage = null;
            oldImage?.Dispose();

            zoom = 1.0f;
            imageOffset = PointF.Empty;
            isDragging = false;
            pictureBoxPreview.Cursor = Cursors.Default;

            pictureBoxPreview.Invalidate();
        }

        private void Form_LivePreview_FormClosed(object sender, FormClosedEventArgs e)
        {
            ClearImage();
        }

        private double ClampPercent(double value)
        {
            return Math.Max(0.0, Math.Min(100.0, value));
        }
    }
}
