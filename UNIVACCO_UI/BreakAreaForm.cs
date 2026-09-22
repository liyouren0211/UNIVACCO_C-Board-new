using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using StandardOPage;

namespace UNIVACCO_UI
{
    public partial class BreakAreaForm : Form
    {
        public BreakAreaForm(Mat breakArea01, Mat breakArea02, Mat breakArea03)
        {
            InitializeComponent();

            BreakArea01_pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            BreakArea02_pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            BreakArea03_pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            if (breakArea01 != null && !breakArea01.IsEmpty)
                BreakArea01_pictureBox.Image = breakArea01.ToBitmap();

            if (breakArea02 != null && !breakArea02.IsEmpty)
                BreakArea02_pictureBox.Image = breakArea02.ToBitmap();

            if (breakArea03 != null && !breakArea03.IsEmpty)
            {
                Mat rotated = breakArea03.ToImage<Bgr, byte>().Rotate(90, new Bgr(255, 255, 255), false).Mat;
                BreakArea03_pictureBox.Image = rotated.ToBitmap();
            }

            Mat process01 = CvInvoke.Imread("BreakArea_01_process.bmp", ImreadModes.Grayscale);
            Mat process02 = CvInvoke.Imread("BreakArea_02_process.bmp", ImreadModes.Grayscale);
            Mat process03 = CvInvoke.Imread("BreakArea_03_process.bmp", ImreadModes.Grayscale);

            if (process01 != null && !process01.IsEmpty)
            {
                double[] lengths01 = OCT_AlgorithmHelper.BreakArea_Calculate(process01, useGapLimit: true);
                DrawBreakAreaChart(chartBreak01, lengths01, process01.Height, process01.Width, "");
            }

            if (process02 != null && !process02.IsEmpty)
            {
                double[] lengths02 = OCT_AlgorithmHelper.BreakArea_Calculate(process02, useGapLimit: true);
                DrawBreakAreaChart(chartBreak02, lengths02, process02.Height, process02.Width, "");
            }

            if (process03 != null && !process03.IsEmpty)
            {
                double[] lengths03 = OCT_AlgorithmHelper.BreakArea_Calculate(process03, useGapLimit: false);
                DrawBreakAreaChart(chartBreak03, lengths03, process03.Height, process03.Width, "");
            }
        }

        private void DrawBreakAreaChart(
            Chart chart,
            double[] blackPointLengths,
            int imageHeight,
            int imageWidth,
            string title)
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Titles.Clear();

            // 副標題：顯示游標 X、Y 數值
            chart.Titles.Add(new Title("")
            {
                Name = "CursorInfo",
                Font = new Font("Arial", 9),
                ForeColor = Color.FromArgb(180, 0, 0)
            });

            ChartArea chartArea = new ChartArea("MainArea");
            chartArea.BackColor = Color.FromArgb(245, 245, 245);
            chartArea.BorderColor = Color.LightGray;

            // X 軸
            chartArea.AxisX.Title = "像素位置(x)";
            chartArea.AxisX.TitleFont = new Font("Microsoft JhengHei UI", 9, FontStyle.Bold);
            chartArea.AxisX.TitleForeColor = Color.Red;
            chartArea.AxisX.LabelStyle.Font = new Font("Arial", 8);
            chartArea.AxisX.LabelStyle.Format = "F0";
            chartArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = imageWidth - 1;
            chartArea.AxisX.Interval = 0;

            // Y 軸
            chartArea.AxisY.Title = "破開長度(mm)";
            chartArea.AxisY.TitleFont = new Font("Microsoft JhengHei UI", 9, FontStyle.Bold);
            chartArea.AxisY.TitleForeColor = Color.Red;
            chartArea.AxisY.LabelStyle.Font = new Font("Arial", 8);
            chartArea.AxisY.Maximum = imageHeight * 0.01;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.MajorGrid.LineColor = Color.LightGray;

            // 十字游標
            chartArea.CursorX.IsUserEnabled = false;
            chartArea.CursorX.IsUserSelectionEnabled = false;
            chartArea.CursorX.LineColor = Color.Red;
            chartArea.CursorX.LineDashStyle = ChartDashStyle.Dash;
            chartArea.CursorY.IsUserEnabled = false;
            chartArea.CursorY.IsUserSelectionEnabled = false;
            chartArea.CursorY.LineColor = Color.Red;
            chartArea.CursorY.LineDashStyle = ChartDashStyle.Dash;

            chart.ChartAreas.Add(chartArea);

            Series series = new Series("")
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(30, 90, 180),
                BorderWidth = 1,
                ChartArea = "MainArea",
                IsVisibleInLegend = false
            };

            for (int x = 0; x < blackPointLengths.Length; x++)
                series.Points.AddXY(x, blackPointLengths[x]);

            chart.Series.Add(series);

            // 滑鼠移動：十字游標跟隨，Y 對齊圖表數據值，同時顯示 X、Y 數值
            chart.MouseMove += (sender, e) =>
            {
                try
                {
                    var area = chart.ChartAreas["MainArea"];
                    double xVal = area.AxisX.PixelPositionToValue(e.X);
                    int xIndex = (int)Math.Round(xVal);

                    if (xIndex >= 0 && xIndex < blackPointLengths.Length)
                    {
                        double yVal = blackPointLengths[xIndex];

                        area.CursorX.Position = xIndex;
                        area.CursorY.Position = yVal;

                        chart.Titles["CursorInfo"].Text =
                            $"X = {xIndex} px     Y = {yVal:F2} mm";
                    }
                }
                catch { }
            };

            // 滑鼠離開時清除游標資訊
            chart.MouseLeave += (sender, e) =>
            {
                try
                {
                    var area = chart.ChartAreas["MainArea"];
                    area.CursorX.Position = double.NaN;
                    area.CursorY.Position = double.NaN;
                    chart.Titles["CursorInfo"].Text = "";
                }
                catch { }
            };

            // 滑鼠滾輪縮放
            chart.MouseWheel += (sender, e) =>
            {
                try
                {
                    var area = chart.ChartAreas["MainArea"];
                    double xMin = area.AxisX.ScaleView.ViewMinimum;
                    double xMax = area.AxisX.ScaleView.ViewMaximum;
                    double viewWidth = xMax - xMin;
                    double center = (xMin + xMax) / 2;
                    double zoomFactor = e.Delta > 0 ? 0.8 : 1.2;

                    double newWidth = viewWidth * zoomFactor;
                    double newMin = Math.Round(center - newWidth / 2);
                    double newMax = Math.Round(center + newWidth / 2);

                    newMin = Math.Max(0, newMin);
                    newMax = Math.Min(imageWidth - 1, newMax);

                    if (newMax - newMin > 10)
                        area.AxisX.ScaleView.Zoom(newMin, newMax);
                }
                catch { }
            };

            // 雙擊左鍵還原縮放
            chart.MouseDoubleClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    chart.ChartAreas["MainArea"].AxisX.ScaleView.ZoomReset();
            };
        }
    }
}