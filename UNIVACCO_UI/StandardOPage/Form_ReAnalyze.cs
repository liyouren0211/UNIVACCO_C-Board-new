using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Util;

namespace StandardOPage
{
    public partial class Form_ReAnalyze : Form
    {
        private readonly SingleAreaInfo SingleAreaInfo;

        private float zoomFactor = 1.0f;
        private Point offset = new Point(0, 0);
        private Point panStartPoint;
        private bool isPanning = false;
        private bool isAddingArea = false;

        private readonly List<Point> tempPoints = new List<Point>();
        private Bitmap displayImage;
        private List<Region> workingRegions;
        private readonly Stack<List<Point[]>> editHistory = new Stack<List<Point[]>>();
        private int selectedRegionIndex = -1;

        public Form_ReAnalyze(SingleAreaInfo _SingleAreaInfo)
        {
            InitializeComponent();
            SingleAreaInfo = _SingleAreaInfo ?? throw new ArgumentNullException(nameof(_SingleAreaInfo));

            // 不直接修改 Label.Tag 裡真正的 Regions。
            // 使用者只有按「確定」時才提交；按關閉 / X 都會放棄本次修改。
            workingRegions = CloneRegions(SingleAreaInfo.Regions);

            // 視窗重新開啟時，依目前既有框選區域重建 Undo 歷史。
            // 例如已有 A、B、C 三個區域：上一步會依序回到 A+B、A、空白。
            InitializeUndoHistoryFromExistingRegions();

            LoadUI(SingleAreaInfo);

            pictureBox_AreaImage.MouseWheel += PictureBox_AreaImage_MouseWheel;
            pictureBox_AreaImage.MouseDown += PictureBox_AreaImage_MouseDown;
            pictureBox_AreaImage.MouseMove += PictureBox_AreaImage_MouseMove;
            pictureBox_AreaImage.MouseUp += PictureBox_AreaImage_MouseUp;
            pictureBox_AreaImage.MouseClick += PictureBox_AreaImage_SelectRegion;

            // Paint 只綁這一個入口，避免舊版 Paint_TempArea / Paint_SaveArea 重複繪製。
            pictureBox_AreaImage.Paint += PictureBox_AreaImage_Paint;
            FormClosed += Form_ReAnalyze_FormClosed;

            SetInitialImageDisplay();
            SetEditingMode(false);
        }

        public void AddEvent()
        {
            // 保留舊介面；事件已在建構式統一綁定。
        }

        public void LoadUI(SingleAreaInfo singleAreaInfo)
        {
            label_Name.Text = singleAreaInfo.Label_Name;
            displayImage?.Dispose();
            displayImage = singleAreaInfo.Image.ToBitmap();
            // PictureBox 本身不再持有 Image，避免控制項預設繪製 + Paint 自繪造成重複繪圖。
            pictureBox_AreaImage.Image = null;
        }

        private void SetEditingMode(bool editing)
        {
            isAddingArea = editing;

            // 位置、大小、字型全部交給 WinForms Designer。
            // 這裡只負責切換操作模式與按鈕顯示狀態，
            // 避免執行時覆蓋 Designer 中手動調整的版面。
            button_AddArea.Visible = !editing;
            button_DeleteRegion.Visible = !editing;
            button_ClearAll.Visible = !editing;

            button_SaveArea.Visible = editing;
            button_CancelArea.Visible = editing;

            button_Confirm.Visible = !editing;
            button_Close.Visible = !editing;

            // 上一步在兩種模式都保留：
            // 新增模式 = 撤回上一個點；一般模式 = 復原上一個 Region 操作。
            button_Undo.Visible = true;
            button_Undo.Enabled = true;
        }

        private void button_Close_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void button_Confirm_Click(object sender, EventArgs e)
        {
            ValidateAllAreas();
            CommitWorkingRegions();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button_AddArea_Click(object sender, EventArgs e)
        {
            tempPoints.Clear();
            selectedRegionIndex = -1;
            pictureBox_AreaImage.MouseClick -= PictureBox_AreaImage_AddPoint;
            pictureBox_AreaImage.MouseClick += PictureBox_AreaImage_AddPoint;
            SetEditingMode(true);
            pictureBox_AreaImage.Invalidate();
        }

        private void button_CancelArea_Click(object sender, EventArgs e)
        {
            tempPoints.Clear();
            pictureBox_AreaImage.MouseClick -= PictureBox_AreaImage_AddPoint;
            SetEditingMode(false);
            pictureBox_AreaImage.Invalidate();
        }

        private void button_SaveArea_Click(object sender, EventArgs e)
        {
            pictureBox_AreaImage.MouseClick -= PictureBox_AreaImage_AddPoint;

            if (tempPoints.Count >= 3)
            {
                PushHistory();
                var contour = new VectorOfPoint(tempPoints.ToArray());
                double area = Math.Abs(CvInvoke.ContourArea(contour));
                workingRegions.Add(new Region
                {
                    Contour = contour,
                    Area = area
                });
                selectedRegionIndex = workingRegions.Count - 1;
            }
            else
            {
                MessageBox.Show("至少需要 3 個點才能建立排除區域。", "排除區域", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            tempPoints.Clear();
            SetEditingMode(false);
            pictureBox_AreaImage.Invalidate();
        }

        private void button_DeleteRegion_Click(object sender, EventArgs e)
        {
            if (selectedRegionIndex < 0 || selectedRegionIndex >= workingRegions.Count)
            {
                MessageBox.Show("請先在圖片上點選要刪除的紅框。", "刪除區域", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PushHistory();
            Region region = workingRegions[selectedRegionIndex];
            region?.Contour?.Dispose();
            workingRegions.RemoveAt(selectedRegionIndex);
            selectedRegionIndex = -1;
            pictureBox_AreaImage.Invalidate();
        }

        private void button_ClearAll_Click(object sender, EventArgs e)
        {
            if (workingRegions.Count == 0)
                return;

            if (MessageBox.Show(
                    "確定要清除目前所有排除區域嗎？\n按『上一步』仍可復原。",
                    "全部清除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            PushHistory();
            DisposeRegions(workingRegions);
            workingRegions = new List<Region>();
            selectedRegionIndex = -1;
            pictureBox_AreaImage.Invalidate();
        }

        private void button_Undo_Click(object sender, EventArgs e)
        {
            // 畫多邊形期間：「上一步」= 移除上一個點。
            if (isAddingArea)
            {
                if (tempPoints.Count > 0)
                {
                    tempPoints.RemoveAt(tempPoints.Count - 1);
                    pictureBox_AreaImage.Invalidate();
                }
                return;
            }

            // 一般模式：「上一步」= 復原上一個新增/刪除/全部清除動作。
            if (editHistory.Count == 0)
                return;

            RestoreSnapshot(editHistory.Pop());
            selectedRegionIndex = -1;
            pictureBox_AreaImage.Invalidate();
        }

        #region 基礎滑動 / 縮放 / 繪圖

        private void SetInitialImageDisplay()
        {
            if (displayImage == null)
                return;

            float imgWidth = displayImage.Width;
            float imgHeight = displayImage.Height;
            float boxWidth = pictureBox_AreaImage.Width;
            float boxHeight = pictureBox_AreaImage.Height;

            zoomFactor = Math.Max(
                boxWidth / imgWidth,
                boxHeight / imgHeight);

            offset.X = (int)((boxWidth - imgWidth * zoomFactor) / 2);
            offset.Y = (int)((boxHeight - imgHeight * zoomFactor) / 2);
            pictureBox_AreaImage.Invalidate();
        }

        private void PictureBox_AreaImage_MouseWheel(object sender, MouseEventArgs e)
        {
            if (displayImage == null)
                return;

            float zoomIncrement = 0.1f;
            float minZoomFactor = Math.Max(
                (float)pictureBox_AreaImage.Width / displayImage.Width,
                (float)pictureBox_AreaImage.Height / displayImage.Height);

            float mouseX = (e.X - offset.X) / zoomFactor;
            float mouseY = (e.Y - offset.Y) / zoomFactor;

            if (e.Delta > 0)
                zoomFactor += zoomIncrement;
            else if (e.Delta < 0)
                zoomFactor -= zoomIncrement;

            zoomFactor = Math.Max(zoomFactor, minZoomFactor);
            offset.X = (int)(e.X - mouseX * zoomFactor);
            offset.Y = (int)(e.Y - mouseY * zoomFactor);

            ClampOffset();
            pictureBox_AreaImage.Invalidate();
        }

        private void PictureBox_AreaImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isPanning = true;
                panStartPoint = e.Location;
            }
        }

        private void PictureBox_AreaImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isPanning || displayImage == null)
                return;

            offset.X += e.X - panStartPoint.X;
            offset.Y += e.Y - panStartPoint.Y;
            panStartPoint = e.Location;
            ClampOffset();
            pictureBox_AreaImage.Invalidate();
        }

        private void PictureBox_AreaImage_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                isPanning = false;
        }

        private void ClampOffset()
        {
            if (displayImage == null)
                return;

            int imageWidth = (int)(displayImage.Width * zoomFactor);
            int imageHeight = (int)(displayImage.Height * zoomFactor);

            if (imageWidth <= pictureBox_AreaImage.Width)
                offset.X = (pictureBox_AreaImage.Width - imageWidth) / 2;
            else
                offset.X = Math.Max(pictureBox_AreaImage.Width - imageWidth, Math.Min(0, offset.X));

            if (imageHeight <= pictureBox_AreaImage.Height)
                offset.Y = (pictureBox_AreaImage.Height - imageHeight) / 2;
            else
                offset.Y = Math.Max(pictureBox_AreaImage.Height - imageHeight, Math.Min(0, offset.Y));
        }

        private void PictureBox_AreaImage_Paint(object sender, PaintEventArgs e)
        {
            if (displayImage == null)
                return;

            e.Graphics.DrawImage(
                displayImage,
                new Rectangle(
                    offset.X,
                    offset.Y,
                    (int)(displayImage.Width * zoomFactor),
                    (int)(displayImage.Height * zoomFactor)));

            DrawSavedRegions(e.Graphics);
            if (isAddingArea)
                DrawTempArea(e.Graphics);
        }

        private void DrawSavedRegions(Graphics graphics)
        {
            for (int i = 0; i < workingRegions.Count; i++)
            {
                Point[] points = workingRegions[i].Contour?.ToArray();
                if (points == null || points.Length < 3)
                    continue;

                Point[] transformed = points.Select(ImageToScreen).ToArray();
                bool selected = i == selectedRegionIndex;
                using (Pen pen = new Pen(selected ? Color.Yellow : Color.Red, selected ? 4F : 2F))
                {
                    graphics.DrawPolygon(pen, transformed);
                }
            }
        }

        private void DrawTempArea(Graphics graphics)
        {
            if (tempPoints.Count == 0)
                return;

            Point[] transformed = tempPoints.Select(ImageToScreen).ToArray();
            foreach (Point point in transformed)
            {
                graphics.FillEllipse(Brushes.Blue, point.X - 4, point.Y - 4, 8, 8);
            }

            using (Pen pen = new Pen(Color.Red, 2F))
            {
                if (transformed.Length == 2)
                    graphics.DrawLines(pen, transformed);
                else if (transformed.Length >= 3)
                    graphics.DrawPolygon(pen, transformed);
            }
        }

        private Point ImageToScreen(Point p)
        {
            return new Point(
                (int)(p.X * zoomFactor + offset.X),
                (int)(p.Y * zoomFactor + offset.Y));
        }

        private Point ScreenToImage(Point p)
        {
            return new Point(
                (int)((p.X - offset.X) / zoomFactor),
                (int)((p.Y - offset.Y) / zoomFactor));
        }

        private bool IsInsideImage(Point p)
        {
            return displayImage != null &&
                   p.X >= 0 && p.Y >= 0 &&
                   p.X < displayImage.Width &&
                   p.Y < displayImage.Height;
        }

        private void PictureBox_AreaImage_AddPoint(object sender, MouseEventArgs e)
        {
            if (!isAddingArea || e.Button != MouseButtons.Left)
                return;

            Point imagePoint = ScreenToImage(e.Location);
            if (!IsInsideImage(imagePoint))
                return;

            tempPoints.Add(imagePoint);
            pictureBox_AreaImage.Invalidate();
        }

        private void PictureBox_AreaImage_SelectRegion(object sender, MouseEventArgs e)
        {
            if (isAddingArea || e.Button != MouseButtons.Left)
                return;

            Point imagePoint = ScreenToImage(e.Location);
            if (!IsInsideImage(imagePoint))
                return;

            selectedRegionIndex = -1;
            // 從最後加入的區域開始找，重疊時優先選到最上層。
            for (int i = workingRegions.Count - 1; i >= 0; i--)
            {
                Point[] points = workingRegions[i].Contour?.ToArray();
                if (points == null || points.Length < 3)
                    continue;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddPolygon(points);
                    if (path.IsVisible(imagePoint))
                    {
                        selectedRegionIndex = i;
                        break;
                    }
                }
            }

            pictureBox_AreaImage.Invalidate();
        }

        #endregion

        #region Region 編輯交易 / 復原

        private static List<Region> CloneRegions(IEnumerable<Region> source)
        {
            var result = new List<Region>();
            if (source == null)
                return result;

            foreach (Region region in source)
            {
                Point[] points = region?.Contour?.ToArray();
                if (points == null || points.Length < 3)
                    continue;

                result.Add(new Region
                {
                    Contour = new VectorOfPoint(points),
                    Area = region.Area
                });
            }
            return result;
        }

        /// <summary>
        /// 視窗重新開啟後，依目前已存在的 Regions 重建 Undo 歷史。
        /// editHistory 的最上層會是「少最後一個 Region」的狀態，
        /// 因此重新開啟視窗後仍可使用「上一步」逐一移除既有框選區域。
        /// </summary>
        private void InitializeUndoHistoryFromExistingRegions()
        {
            editHistory.Clear();

            if (workingRegions == null || workingRegions.Count == 0)
                return;

            // 依 Region 原本的加入順序建立前綴快照：
            // []、[A]、[A,B] ...
            // Stack 最上層最後會是「目前狀態少最後一個 Region」。
            for (int count = 0; count < workingRegions.Count; count++)
            {
                List<Point[]> snapshot = workingRegions
                    .Take(count)
                    .Select(r => r.Contour?.ToArray() ?? new Point[0])
                    .ToList();

                editHistory.Push(snapshot);
            }
        }

        private void PushHistory()
        {
            editHistory.Push(
                workingRegions
                    .Select(r => r.Contour?.ToArray() ?? new Point[0])
                    .ToList());
        }

        private void RestoreSnapshot(List<Point[]> snapshot)
        {
            DisposeRegions(workingRegions);
            workingRegions = new List<Region>();

            foreach (Point[] points in snapshot)
            {
                if (points == null || points.Length < 3)
                    continue;

                var contour = new VectorOfPoint(points);
                workingRegions.Add(new Region
                {
                    Contour = contour,
                    Area = Math.Abs(CvInvoke.ContourArea(contour))
                });
            }
        }

        private void CommitWorkingRegions()
        {
            if (SingleAreaInfo.Regions != null)
                DisposeRegions(SingleAreaInfo.Regions);

            SingleAreaInfo.Regions = CloneRegions(workingRegions);
        }

        private static void DisposeRegions(IEnumerable<Region> regions)
        {
            if (regions == null)
                return;

            foreach (Region region in regions)
                region?.Contour?.Dispose();
        }

        private void Form_ReAnalyze_FormClosed(object sender, FormClosedEventArgs e)
        {
            pictureBox_AreaImage.MouseClick -= PictureBox_AreaImage_AddPoint;
            DisposeRegions(workingRegions);
            workingRegions.Clear();
            displayImage?.Dispose();
            displayImage = null;
        }

        private void ValidateAllAreas()
        {
            Debug.WriteLine($"Confirming with {workingRegions.Count} Regions:");
            for (int i = 0; i < workingRegions.Count; i++)
            {
                Region region = workingRegions[i];
                Point[] points = region.Contour.ToArray();
                Debug.WriteLine($"Region {i} has {points.Length} points, Area: {region.Area:F2}");
            }
        }

        #endregion
    }
}
