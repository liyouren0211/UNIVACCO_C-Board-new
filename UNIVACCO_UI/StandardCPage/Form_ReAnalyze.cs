using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Emgu.CV;
using Emgu.CV.Util;
using NPOI.SS.Formula.Functions;
using Excel = Microsoft.Office.Interop.Excel;

namespace StandardOPage
{
    public partial class Form_ReAnalyze : Form
    {
        SingleAreaInfo SingleAreaInfo;
        private float zoomFactor = 1.0f; // 縮放比例
        private Point offset = new Point(0, 0); // 平移偏移量
        private Point panStartPoint; // 拖動起點
        private bool isPanning = false; // 是否正在拖動
        private List<Point> tempPoints = new List<Point>();
        public Form_ReAnalyze(SingleAreaInfo _SingleAreaInfo)
        {
            InitializeComponent();
            SingleAreaInfo = _SingleAreaInfo;
            LoadUI(SingleAreaInfo);
            pictureBox_AreaImage.MouseWheel += PictureBox_AreaImage_MouseWheel;
            pictureBox_AreaImage.MouseDown += PictureBox_AreaImage_MouseDown;
            pictureBox_AreaImage.MouseMove += PictureBox_AreaImage_MouseMove;
            pictureBox_AreaImage.MouseUp += PictureBox_AreaImage_MouseUp;
            pictureBox_AreaImage.Paint += PictureBox_AreaImage_Paint;

            SetInitialImageDisplay();
        }
        public void AddEvent()
        {

        }
        public void LoadUI(SingleAreaInfo SingleAreaInfo)
        {
            label_Name.Text = SingleAreaInfo.Label_Name;
            pictureBox_AreaImage.Image = SingleAreaInfo.Image.ToBitmap();
        }
        private void button_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void button_Confirm_Click(object sender, EventArgs e)
        {
            ValidateAllAreas();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private void button_AddArea_Click(object sender, EventArgs e)
        {
            button_CancelArea.Visible = true;
            button_SaveArea.Visible = true;
            button_AddArea.Visible = false;
            button_Confirm.Visible = false;
            button_Close.Visible = false;
            // 清空臨時點列表
            tempPoints.Clear();

            // 添加滑鼠左鍵點擊事件來新增點
            pictureBox_AreaImage.MouseClick += PictureBox_AreaImage_AddPoint;

            // 添加繪製臨時點的事件
            pictureBox_AreaImage.Paint -= Paint_SaveArea; // 先移除原來的Paint事件
            pictureBox_AreaImage.Paint += Paint_TempArea; // 添加新的Paint事件

        }
        private void button_CancelArea_Click(object sender, EventArgs e)
        {
            button_CancelArea.Visible = false;
            button_SaveArea.Visible = false;
            button_AddArea.Visible = true;
            button_Confirm.Visible = true;
            button_Close.Visible = true;
            // 清空臨時點列表
            tempPoints.Clear();

            // 移除臨時事件
            pictureBox_AreaImage.MouseClick -= PictureBox_AreaImage_AddPoint;
            pictureBox_AreaImage.Paint -= Paint_TempArea;
            pictureBox_AreaImage.Paint += Paint_SaveArea;

            // 重繪
            pictureBox_AreaImage.Invalidate();
        }
        private void button_SaveArea_Click(object sender, EventArgs e)
        {
            button_CancelArea.Visible = false;
            button_SaveArea.Visible = false;
            button_AddArea.Visible = true;
            button_Confirm.Visible = true;
            button_Close.Visible = true;

            // 移除臨時事件
            pictureBox_AreaImage.MouseClick -= PictureBox_AreaImage_AddPoint;
            pictureBox_AreaImage.Paint -= Paint_TempArea;
            pictureBox_AreaImage.Paint += Paint_SaveArea;

            // 只有當有足夠的點才保存
            if (tempPoints.Count >= 3)
            {
                var contour = new VectorOfPoint(tempPoints.ToArray());
                double area = Math.Abs(CvInvoke.ContourArea(contour));

                SingleAreaInfo.Regions.Add(new Region
                {
                    Contour = contour,
                    Area = area
                });

            }

            // 清空臨時點列表
            tempPoints.Clear();

            // 重繪
            pictureBox_AreaImage.Invalidate();

        }



        #region 基礎滑動
        // 當圖片載入時設定初始縮放比例
        private void SetInitialImageDisplay()
        {
            if (pictureBox_AreaImage.Image == null)
                return;

            // 獲取圖片的尺寸與 PictureBox 的尺寸
            float imgWidth = pictureBox_AreaImage.Image.Width;
            float imgHeight = pictureBox_AreaImage.Image.Height;
            float boxWidth = pictureBox_AreaImage.Width;
            float boxHeight = pictureBox_AreaImage.Height;

            // 使用與滾輪事件中相同的最小縮放比例計算方法
            zoomFactor = Math.Max(
                (float)pictureBox_AreaImage.Width / pictureBox_AreaImage.Image.Width,
                (float)pictureBox_AreaImage.Height / pictureBox_AreaImage.Image.Height
            );

            // 計算初始的偏移量，讓圖片居中顯示
            offset.X = (int)((boxWidth - imgWidth * zoomFactor) / 2);
            offset.Y = (int)((boxHeight - imgHeight * zoomFactor) / 2);

            pictureBox_AreaImage.Invalidate(); // 重繪畫面
        }


        private void PictureBox_AreaImage_MouseWheel(object sender, MouseEventArgs e)
        {
            if (pictureBox_AreaImage.Image == null) return; // 如果圖片為空，直接返回

            float oldZoomFactor = zoomFactor;
            float zoomIncrement = 0.1f;

            // 計算最小縮放比例，確保圖片能完全填滿 PictureBox
            float minZoomFactor = Math.Max(
                (float)pictureBox_AreaImage.Width / pictureBox_AreaImage.Image.Width,
                (float)pictureBox_AreaImage.Height / pictureBox_AreaImage.Image.Height
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
            int imageWidth = (int)(pictureBox_AreaImage.Image.Width * zoomFactor);
            int imageHeight = (int)(pictureBox_AreaImage.Image.Height * zoomFactor);

            // 如果縮放後的圖像小於 PictureBox，則居中顯示
            if (imageWidth < pictureBox_AreaImage.Width)
            {
                offset.X = (pictureBox_AreaImage.Width - imageWidth) / 2;
            }
            else
            {
                // 確保左邊界不會出現空白
                offset.X = Math.Min(offset.X, 0);
                // 確保右邊界不會出現空白
                offset.X = Math.Max(offset.X, pictureBox_AreaImage.Width - imageWidth);
            }

            if (imageHeight < pictureBox_AreaImage.Height)
            {
                offset.Y = (pictureBox_AreaImage.Height - imageHeight) / 2;
            }
            else
            {
                // 確保上邊界不會出現空白
                offset.Y = Math.Min(offset.Y, 0);
                // 確保下邊界不會出現空白
                offset.Y = Math.Max(offset.Y, pictureBox_AreaImage.Height - imageHeight);
            }

            pictureBox_AreaImage.Invalidate(); // 使用 Refresh 代替 Invalidate，強制立即重繪
        }
        // 當滑鼠按下左鍵時開始平移
        private void PictureBox_AreaImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = true;
                panStartPoint = e.Location;
            }
        }

        // 滑鼠移動事件：拖曳圖片
        private void PictureBox_AreaImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && pictureBox_AreaImage.Image != null)
            {
                // 計算圖片縮放後的寬高
                float scaledWidth = pictureBox_AreaImage.Image.Width * zoomFactor;
                float scaledHeight = pictureBox_AreaImage.Image.Height * zoomFactor;

                // 更新偏移量
                offset.X += e.X - panStartPoint.X;
                offset.Y += e.Y - panStartPoint.Y;

                // 計算邊界範圍
                int maxX = 0; // 圖片左邊界
                int minX = (int)(pictureBox_AreaImage.Width - scaledWidth); // 圖片右邊界
                int maxY = 0; // 圖片上邊界
                int minY = (int)(pictureBox_AreaImage.Height - scaledHeight); // 圖片下邊界

                // 限制 offset 不超出邊界
                if (offset.X > maxX) offset.X = maxX;
                if (offset.X < minX) offset.X = minX;
                if (offset.Y > maxY) offset.Y = maxY;
                if (offset.Y < minY) offset.Y = minY;

                // 更新滑鼠起點
                panStartPoint = e.Location;

                // 重繪畫面
                pictureBox_AreaImage.Invalidate();
            }
        }

        // 滑鼠放開事件：結束平移
        private void PictureBox_AreaImage_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = false;
            }
        }

        // 畫面更新：顯示圖片並處理縮放和平移
        private void PictureBox_AreaImage_Paint(object sender, PaintEventArgs e)
        {
            // 確保圖片已經加載
            if (pictureBox_AreaImage.Image == null) return;

            // 創建 Graphics 物件來繪製圖形
            Graphics g = e.Graphics;

            // 先繪製縮放和平移後的圖片
            g.DrawImage(pictureBox_AreaImage.Image,
                new Rectangle(offset.X, offset.Y,
                (int)(pictureBox_AreaImage.Image.Width * zoomFactor),
                (int)(pictureBox_AreaImage.Image.Height * zoomFactor)));

            // 如果在添加區域模式，繪製臨時點
            if (button_SaveArea.Visible)
            {
                Paint_TempArea(sender, e);
            }
            else
            {
                // 繪製已保存的區域
                foreach (var region in SingleAreaInfo.Regions)
                {
                    Point[] transformedPoints = region.Contour.ToArray()
                        .Select(p => new Point(
                            (int)(p.X * zoomFactor + offset.X),
                            (int)(p.Y * zoomFactor + offset.Y)
                        ))
                        .ToArray();

                    // 設定畫筆顏色並繪製多邊形
                    using (Pen pen = new Pen(Color.Red, 2))  // 紅色畫筆，寬度為 2
                    {
                        if (transformedPoints.Length >= 3)
                        {
                            g.DrawPolygon(pen, transformedPoints);
                        }
                    }
                }

            }
        }
        // 更新繪製臨時點的方法
        private void Paint_TempArea(object sender, PaintEventArgs e)
        {
            // 確保圖片已經加載
            if (pictureBox_AreaImage.Image == null) return;

            // 創建 Graphics 物件來繪製圖形
            Graphics g = e.Graphics;

            // 先繪製縮放和平移後的圖片
            g.DrawImage(pictureBox_AreaImage.Image,
                new Rectangle(offset.X, offset.Y,
                (int)(pictureBox_AreaImage.Image.Width * zoomFactor),
                (int)(pictureBox_AreaImage.Image.Height * zoomFactor)));

            // 如果沒有點可繪製，直接返回
            if (tempPoints.Count == 0) return;

            // 設定畫筆顏色
            Pen pen = new Pen(Color.Red, 2);  // 紅色畫筆，寬度為 2

            // 根據縮放和偏移計算轉換後的點
            Point[] transformedPoints = tempPoints.Select(p => new Point(
                (int)(p.X * zoomFactor + offset.X),
                (int)(p.Y * zoomFactor + offset.Y)
            )).ToArray();

            // 繪製每個點（藍色標記）
            foreach (var transformedPoint in transformedPoints)
            {
                g.FillEllipse(Brushes.Blue,
                    transformedPoint.X - 4,
                    transformedPoint.Y - 4,
                    8, 8);  // 用藍色小圓點顯示每個點
            }

            // 只有當點數至少為 3 時才繪製連線
            if (transformedPoints.Length >= 3)
            {
                g.DrawPolygon(pen, transformedPoints);  // 連接點，繪製多邊形
            }

            // 清理資源
            pen.Dispose();
        }
        private void PictureBox_AreaImage_AddPoint(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 計算實際圖像上的點位置（考慮縮放和偏移）
                Point imagePoint = new Point(
                    (int)((e.X - offset.X) / zoomFactor),
                    (int)((e.Y - offset.Y) / zoomFactor)
                );

                // 添加點到臨時點列表
                tempPoints.Add(imagePoint);

                // 重繪以顯示新點
                pictureBox_AreaImage.Invalidate();
            }
        }
        private void Paint_SaveArea(object sender, PaintEventArgs e)
        {
            // 確保圖片已經加載
            if (pictureBox_AreaImage.Image == null) return;

            // 創建 Graphics 物件來繪製圖形
            Graphics g = e.Graphics;

            // 先繪製縮放和平移後的圖片
            g.DrawImage(pictureBox_AreaImage.Image,
                new Rectangle(offset.X, offset.Y,
                (int)(pictureBox_AreaImage.Image.Width * zoomFactor),
                (int)(pictureBox_AreaImage.Image.Height * zoomFactor)));

            // 設定畫筆顏色
            Pen pen = new Pen(Color.Red, 2);  // 紅色畫筆，寬度為 2

            // 繪製所有已保存的區域
            foreach (var region in SingleAreaInfo.Regions)
            {
                Point[] points = region.Contour.ToArray();
                if (points.Length >= 3)
                {
                    Point[] transformedPoints = points.Select(p => new Point(
                        (int)(p.X * zoomFactor + offset.X),
                        (int)(p.Y * zoomFactor + offset.Y)
                    )).ToArray();

                    g.DrawPolygon(pen, transformedPoints);
                }
            }


            // 清理資源
            pen.Dispose();
        }
        #endregion
        private void ValidateAllAreas()
        {
            Debug.WriteLine($"Confirming with {SingleAreaInfo.Regions.Count} Regions:");

            for (int i = 0; i < SingleAreaInfo.Regions.Count; i++)
            {
                var region = SingleAreaInfo.Regions[i];
                Point[] points = region.Contour.ToArray();

                Debug.WriteLine($"Region {i} has {points.Length} points, Area: {region.Area:F2}");

                foreach (var point in points)
                {
                    Debug.WriteLine($"  Point: ({point.X}, {point.Y})");
                }
            }
        }

    }
}
