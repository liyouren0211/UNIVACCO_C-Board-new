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

        private Button button_DeleteRegion;
        private Button button_ClearAll;
        private Button button_Undo;

        // 這些位置由 Designer 已完成 DPI/字型縮放後的既有控制項取得，
        // 不再使用 737 這種固定座標，避免不同 Windows 顯示比例時按鈕跑出視窗。
        private int editRowY;
        private int editRowHeight;

        public Form_ReAnalyze(SingleAreaInfo _SingleAreaInfo)
        {
            InitializeComponent();
            SingleAreaInfo = _SingleAreaInfo ?? throw new ArgumentNullException(nameof(_SingleAreaInfo));

            // 先記住 Designer 控制項經過 WinForms AutoScale 後的實際位置。
            // button_AddArea 是原本 Designer 就有的控制項，因此它的位置可當作可靠的編輯列基準。
            editRowY = button_AddArea.Top;
            editRowHeight = button_AddArea.Height;

            // 不直接修改 Label.Tag 裡真正的 Regions。
            // 使用者只有按「確定」時才提交；按關閉 / X 都會放棄本次修改。
            workingRegions = CloneRegions(SingleAreaInfo.Regions);

            LoadUI(SingleAreaInfo);
            InitializeEditButtons();

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

        private void InitializeEditButtons()
        {
            // 新按鈕在執行期建立，但位置不能硬寫死，因為 Designer 控制項會受
            // Windows DPI / 字型縮放影響。位置一律由目前實際 ClientSize 與
            // 原本 button_AddArea 的縮放後位置動態計算。
            button_DeleteRegion = CreateEditButton("刪除選取");
            button_DeleteRegion.Click += button_DeleteRegion_Click;
            Controls.Add(button_DeleteRegion);

            button_ClearAll = CreateEditButton("全部清除");
            button_ClearAll.Click += button_ClearAll_Click;
            Controls.Add(button_ClearAll);

            button_Undo = CreateEditButton("上一步");
            button_Undo.Click += button_Undo_Click;
            Controls.Add(button_Undo);

            LayoutEditButtons();
        }

        private Button CreateEditButton(string text)
        {
            return new Button
            {
                BackColor = SystemColors.ControlLight,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold),
                ForeColor = SystemColors.ActiveCaptionText,
                Text = text,
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Bottom
            };
        }

        /// <summary>
        /// 依目前實際視窗尺寸排列 ReAnalyze 編輯列。
        /// 不使用固定 X/Y，因此 100%、125%、150% DPI 都不會把按鈕排到視窗外。
        /// </summary>
        private void LayoutEditButtons()
        {
            int left = Math.Max(8, label_Name.Left);
            int right = Math.Min(ClientSize.Width - 8, label_Name.Right);
            if (right <= left)
            {
                left = 8;
                right = ClientSize.Width - 8;
            }

            int gap = Math.Max(4, (int)Math.Round(6.0 * DeviceDpi / 96.0));
            int rowHeight = Math.Max(38, editRowHeight);

            // 編輯列永遠放在影像下方、確定/關閉上方。
            int minY = pictureBox_AreaImage.Bottom + gap;
            int maxY = button_Confirm.Top - rowHeight - gap;
            int y = editRowY;
            if (maxY >= minY)
                y = Math.Max(minY, Math.Min(y, maxY));
            else
                y = Math.Max(0, Math.Min(y, ClientSize.Height - rowHeight));

            if (!isAddingArea)
            {
                int totalWidth = Math.Max(4, right - left);
                int buttonWidth = Math.Max(50, (totalWidth - gap * 3) / 4);

                button_AddArea.SetBounds(left, y, buttonWidth, rowHeight);
                button_DeleteRegion.SetBounds(left + (buttonWidth + gap), y, buttonWidth, rowHeight);
                button_ClearAll.SetBounds(left + 2 * (buttonWidth + gap), y, buttonWidth, rowHeight);
                button_Undo.SetBounds(left + 3 * (buttonWidth + gap), y, buttonWidth, rowHeight);

                // 四顆按鈕要塞在同一列，原本 16pt 在較高 DPI 會太大。
                button_AddArea.Font = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold);
                button_DeleteRegion.Font = button_AddArea.Font;
                button_ClearAll.Font = button_AddArea.Font;
                button_Undo.Font = button_AddArea.Font;
            }
            else
            {
                int totalWidth = Math.Max(3, right - left);
                int buttonWidth = Math.Max(60, (totalWidth - gap * 2) / 3);

                button_SaveArea.SetBounds(left, y, buttonWidth, rowHeight);
                button_Undo.SetBounds(left + (buttonWidth + gap), y, buttonWidth, rowHeight);
                button_CancelArea.SetBounds(left + 2 * (buttonWidth + gap), y, buttonWidth, rowHeight);

                button_SaveArea.Font = new Font("Microsoft JhengHei UI", 10.5F, FontStyle.Bold);
                button_Undo.Font = button_SaveArea.Font;
                button_CancelArea.Font = button_SaveArea.Font;
            }

            button_AddArea.BringToFront();
            button_DeleteRegion.BringToFront();
            button_ClearAll.BringToFront();
            button_Undo.BringToFront();
            button_SaveArea.BringToFront();
            button_CancelArea.BringToFront();
        }

        private void SetEditingMode(bool editing)
        {
            isAddingArea = editing;

            button_AddArea.Visible = !editing;
            button_DeleteRegion.Visible = !editing;
            button_ClearAll.Visible = !editing;
            button_Undo.Visible = true;

            button_SaveArea.Visible = editing;
            button_CancelArea.Visible = editing;
            button_Confirm.Visible = !editing;
            button_Close.Visible = !editing;

            LayoutEditButtons();
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
