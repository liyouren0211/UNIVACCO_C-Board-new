using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Util;
using System.Diagnostics;
using Basler.Pylon;
using System.Linq.Expressions;
using System.Windows.Forms;
using NPOI.HSSF.Record.CF;
using System.IO;

namespace StandardOPage
{
    public static class OCT_AlgorithmHelper
    {
        public static Action<Action> UpdateUIAction { get; set; }  // 主頁委託傳進來的地方


        // =====================================================================
        // 2026-09-05 Python 目前驗證版定位參數
        // 所有搜尋框一律使用 Rectangle(X, Y, W, H)
        // 之後要調黃色搜尋框，直接改這裡即可
        // =====================================================================

        public sealed class MeshPointROIConfig
        {
            // ---------- Station 1 / 2 黃色搜尋框 ----------
            public Rectangle S1MidSearchBox { get; set; } = new Rectangle(2000, 800, 800, 2400);
            public Rectangle S1TopSearchBox { get; set; } = new Rectangle(900, 0, 1500, 600);

            public Rectangle S2MidSearchBox { get; set; } = new Rectangle(2000, 1500, 800, 2100);
            public Rectangle S2BottomSearchBox { get; set; } = new Rectangle(600, 2300, 1500, 1200);

            // ---------- S2 Bottom-Y：對應目前 Python「相對最低平台左邊界」 ----------
            // darkThreshold = profileMin + ratio * (profileMax - profileMin)
            public double S2BottomRelativeDarkRatio { get; set; } = 0.15;

            // 對應 Python np.convolve(..., mode="same") 的簡單移動平均視窗
            // 必須為奇數；若設成偶數，執行時會自動 +1
            public int S2BottomProfileSmoothWindow { get; set; } = 11;

            // 低於相對黑底門檻至少連續多少列，才承認是黑色平台
            public int S2BottomMinPlatformLength { get; set; } = 80;

            // 平台左邊界前方取多少列平均，只用於 Debug
            public int S2BottomBeforeWindow { get; set; } = 30;

            // ---------- 最終網點裁切大小 ----------
            public int GridWidth { get; set; } = 2050;
            public int GridHeight { get; set; } = 1226;

            public int S1LeftXOffsetFromMid { get; set; } = 2050;
            public int S2LeftXOffsetFromMid { get; set; } = 2050;

            // ---------- BreakArea 01 ----------
            public int Break01ScanX1 { get; set; } = 800;
            public int Break01ScanX2 { get; set; } = 2400;
            public double Break01TopBlackRatio { get; set; } = 0.95;
            public double Break01BottomWhiteRatio { get; set; } = 0.90;
            public int Break01BottomExtra { get; set; } = 8;
            public int Break01LeftScanStartX { get; set; } = 1800;
            public double Break01LeftWhiteRatio { get; set; } = 0.60;
            public int Break01CropWidth { get; set; } = 2094;
        }

        public sealed class FontROIConfig
        {
            // ---------- Station 3 / 4 黃色搜尋框 ----------
            public Rectangle S3MidSearchBox { get; set; } = new Rectangle(2000, 1000, 800, 2000);
            public Rectangle S3TopSearchBox { get; set; } = new Rectangle(1000, 400, 1000, 1000);

            public Rectangle S4MidSearchBox { get; set; } = new Rectangle(2000, 500, 800, 2000);
            public Rectangle S4BottomSearchBox { get; set; } = new Rectangle(1000, 2000, 1000, 1500);

            public Rectangle SplitValleySearchBox { get; set; } = new Rectangle(1800, 0, 800, 4000);

            // ---------- Station 3 / 4 最終裁切 ----------
            public Size S3CropSize { get; set; } = new Size(4350, 1860);
            public Size S4CropSize { get; set; } = new Size(4350, 1740);

            public int S3LeftXOffsetFromMid { get; set; } = 2150;
            public int S4LeftXOffsetFromMid { get; set; } = 2150;

            // ---------- BreakArea 02 ----------
            public int Break02YOffsetFromTop { get; set; } = 600;
            public int Break02Height { get; set; } = 120;
            public int Break02LeftOffsetFromMid { get; set; } = 2100;
            public int Break02RightOffsetFromMid { get; set; } = 6;

            // ---------- 陰 / 陽版切割 ----------
            public int YangWidth { get; set; } = 2120;
            public int YinWidth { get; set; } = 2080;
        }

        public static MeshPointROIConfig MeshROI { get; } = new MeshPointROIConfig();
        public static FontROIConfig FontROI { get; } = new FontROIConfig();

        private static Rectangle ClampRectangle(Rectangle roi, int width, int height)
        {
            int x1 = Math.Max(0, roi.X);
            int y1 = Math.Max(0, roi.Y);
            int x2 = Math.Min(width, roi.Right);
            int y2 = Math.Min(height, roi.Bottom);
            if (x2 <= x1 || y2 <= y1) return Rectangle.Empty;
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        private static double[] MeanColumnProfile(Mat gray, Rectangle roi)
        {
            Rectangle r = ClampRectangle(roi, gray.Width, gray.Height);
            if (r.IsEmpty) return Array.Empty<double>();
            double[] profile = new double[r.Width];
            unsafe
            {
                byte* ptr = (byte*)gray.DataPointer;
                int step = gray.Step;
                for (int x = 0; x < r.Width; x++)
                {
                    long sum = 0;
                    int xx = r.X + x;
                    for (int y = r.Y; y < r.Bottom; y++)
                        sum += *(ptr + y * step + xx);
                    profile[x] = (double)sum / r.Height;
                }
            }
            return profile;
        }

        private static double[] MeanRowProfile(Mat gray, Rectangle roi)
        {
            Rectangle r = ClampRectangle(roi, gray.Width, gray.Height);
            if (r.IsEmpty) return Array.Empty<double>();
            double[] profile = new double[r.Height];
            unsafe
            {
                byte* ptr = (byte*)gray.DataPointer;
                int step = gray.Step;
                for (int y = 0; y < r.Height; y++)
                {
                    long sum = 0;
                    byte* row = ptr + (r.Y + y) * step;
                    for (int x = r.X; x < r.Right; x++) sum += row[x];
                    profile[y] = (double)sum / r.Width;
                }
            }
            return profile;
        }

        private static double[] GaussianSmooth1D(double[] src, int kernelSize)
        {
            if (src == null || src.Length == 0 || kernelSize <= 1) return src ?? Array.Empty<double>();
            if (kernelSize % 2 == 0) kernelSize++;
            int radius = kernelSize / 2;
            double sigma = 0.3 * (radius - 1) + 0.8; // 對應 OpenCV sigma=0 的自動估算
            double[] kernel = new double[kernelSize];
            double ksum = 0;
            for (int i = -radius; i <= radius; i++)
            {
                double v = Math.Exp(-(i * i) / (2.0 * sigma * sigma));
                kernel[i + radius] = v;
                ksum += v;
            }
            for (int i = 0; i < kernel.Length; i++) kernel[i] /= ksum;

            double[] dst = new double[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                double sum = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int idx = i + k;
                    if (idx < 0) idx = -idx;
                    if (idx >= src.Length) idx = 2 * src.Length - idx - 2;
                    idx = Math.Max(0, Math.Min(src.Length - 1, idx));
                    sum += src[idx] * kernel[k + radius];
                }
                dst[i] = sum;
            }
            return dst;
        }

        /// <summary>
        /// 對應目前 Python：
        /// np.convolve(profile, np.ones(window)/window, mode="same")
        ///
        /// 注意：
        /// Python np.convolve(..., mode="same") 在左右邊界等同以 0 補值，
        /// 這裡刻意照做，讓 C# 與目前 Python 驗證結果一致。
        /// </summary>
        private static double[] MovingAverageSameZeroPad(double[] src, int window)
        {
            if (src == null || src.Length == 0)
                return Array.Empty<double>();

            if (window < 1)
                window = 1;

            if (window % 2 == 0)
                window++;

            if (window == 1)
                return (double[])src.Clone();

            int radius = window / 2;
            double[] dst = new double[src.Length];

            for (int i = 0; i < src.Length; i++)
            {
                double sum = 0.0;

                for (int k = -radius; k <= radius; k++)
                {
                    int idx = i + k;

                    // Python np.convolve mode="same" 的邊界等同 0 padding
                    if (idx >= 0 && idx < src.Length)
                        sum += src[idx];
                }

                dst[i] = sum / window;
            }

            return dst;
        }

        private static int ArgMaxDiff(double[] profile, bool negate = false)
        {
            if (profile == null || profile.Length < 2) return 0;
            int best = 0;
            double bestVal = double.MinValue;
            for (int i = 0; i < profile.Length - 1; i++)
            {
                double d = profile[i + 1] - profile[i];
                if (negate) d = -d;
                if (d > bestVal) { bestVal = d; best = i; }
            }
            return best;
        }

        private static int ArgMin(double[] profile)
        {
            if (profile == null || profile.Length == 0) return 0;
            int idx = 0;
            for (int i = 1; i < profile.Length; i++)
                if (profile[i] < profile[idx]) idx = i;
            return idx;
        }

        private static Mat SafeCropRotateCCW(Mat src, int y1, int y2, int x1, int x2, int fallbackW, int fallbackH)
        {
            int sx1 = Math.Max(0, x1), sy1 = Math.Max(0, y1);
            int sx2 = Math.Min(src.Width, x2), sy2 = Math.Min(src.Height, y2);
            Mat crop;
            if (sx2 <= sx1 || sy2 <= sy1)
            {
                crop = new Mat(new Size(fallbackW, fallbackH), src.Depth, src.NumberOfChannels);
                crop.SetTo(new MCvScalar(0));
            }
            else
            {
                crop = new Mat(src, new Rectangle(sx1, sy1, sx2 - sx1, sy2 - sy1)).Clone();
            }
            Mat rotated = new Mat();
            CvInvoke.Rotate(crop, rotated, RotateFlags.Rotate90CounterClockwise);
            crop.Dispose();
            return rotated;
        }

        #region 小工具
        /// <summary>
        /// 擷取紙張
        /// </summary>
        private static Mat Crop_Paper(Mat sourceimg, OCT_Parameters_SaveOptions saveoptions)
        {
            Mat processimg = sourceimg.Clone();
            // 儲存原始圖像
            if (saveoptions.saveoption.Roi_CropPaper)
            {
                CvInvoke.Imwrite("CropPaper_1_Source.bmp", processimg);
            }
            // 將圖像轉為灰階
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            // 二值化處理
            CvInvoke.Threshold(processimg, processimg, 100, 255, ThresholdType.Binary);
            if (saveoptions.saveoption.Roi_CropPaper)
            {
                CvInvoke.Imwrite("CropPaper_2_threshold.bmp", processimg);
            }
            // 水平膨脹
            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 1), new Point(-1, -1)); // 水平膨脹結構元素
            CvInvoke.Dilate(processimg, processimg, kernel, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(0)); // 水平膨脹
            //垂直膨脹
            kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(1, 5), new Point(-1, -1)); // 垂直膨脹結構元素
            CvInvoke.Dilate(processimg, processimg, kernel, new Point(-1, -1), 1, BorderType.Constant, new MCvScalar(0)); // 垂直膨脹
            if (saveoptions.saveoption.Roi_CropPaper)
            {
                CvInvoke.Imwrite("CropPaper_3_dilated.bmp", processimg);
            }
            // 找輪廓
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            VectorOfRect hierarchy = new VectorOfRect();
            CvInvoke.FindContours(processimg, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Gray2Bgr);
            // 過濾出面積最大的物件
            int MaxAreaIndex = 0;
            for (int i = 0; i < contours.Size; i++)
            {
                double area = CvInvoke.ContourArea(contours[i]);
                double maxarea = CvInvoke.ContourArea(contours[MaxAreaIndex]);
                if (area > maxarea)
                    MaxAreaIndex = i;

                CvInvoke.DrawContours(processimg, contours, i, new MCvScalar(0, 0, 255), 5);
            }
            if (saveoptions.saveoption.Roi_CropPaper)
            {
                CvInvoke.Imwrite("CropPaper_4_contours.bmp", processimg);
            }
            //try
            //{
            //計算最大輪廓的邊界矩形
            Rectangle boundingRect = CvInvoke.BoundingRectangle(contours[MaxAreaIndex]);
            boundingRect.X += 40;  // 將x起點偏移20
            boundingRect.Width -= 40;  // 將x起點偏移20
            return new Mat(sourceimg, boundingRect);
            //}
            //catch
            //{

            //}
        }
        /// <summary>
        /// 旋轉校正
        /// </summary>
        private static Mat Rotate(Mat sourceimg, OCT_Parameters_SaveOptions saveoptions)
        {
            Mat processimg = sourceimg.Clone();
            if (saveoptions.saveoption.Roi_Rotate)
            {
                CvInvoke.Imwrite($"HoughRotate_1_source.bmp", processimg);
            }
            // 將圖像轉為灰階
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            // 二值化處理
            CvInvoke.Threshold(processimg, processimg, 200, 255, ThresholdType.Binary);
            if (saveoptions.saveoption.Roi_Rotate)
            {
                CvInvoke.Imwrite($"HoughRotate_2_threshold.bmp", processimg);
            }
            // 儲存裁切後的圖像
            int left_x = 0, right_x = 0, right_start_bias = 1000;    //左邊界 右邊界 右邊界起始點偏移量
            double rightx_threshold = 0.35, leftx_threshold = 0.35; // 水平白色像素的百分比閾值
            bool is_leftx_found = false;
            bool is_rightx_found = false;
            //裁切左右兩側
            unsafe
            {
                int centerY = processimg.Height / 2;
                int whitepixels = 0;
                // 重新初始化指標，使用新的 ROI 影像
                byte* ptr = (byte*)processimg.DataPointer;
                // 從左往右找
                for (int i = 0; i < processimg.Width; i++)
                {
                    if (is_leftx_found)
                    {
                        //Debug.WriteLine($"leftx is found:{left_x}");
                        break;
                    }
                    whitepixels = 0;
                    for (int j = 0; j < processimg.Height; j++)
                    {
                        byte* currentPixel = ptr + (j * processimg.Width + i);
                        if (*currentPixel == 255)
                        {
                            whitepixels++;
                            if (whitepixels > (processimg.Height * leftx_threshold))
                            {
                                left_x = i;
                                is_leftx_found = true;
                                break;
                            }
                        }
                    }
                    //Debug.WriteLine($"Column {i} whitepixels count: {whitepixels}");
                }
                // 從中右往右找（從寬度-1000開始）
                for (int i = processimg.Width - right_start_bias; i < processimg.Width; i++)
                {
                    if (is_rightx_found)
                    {
                        //Debug.WriteLine($"rightx is found:{right_x}");
                        break;
                    }
                    whitepixels = 0;
                    for (int j = 0; j < processimg.Height; j++)
                    {
                        byte* currentPixel = ptr + (j * processimg.Width + i);
                        if (*currentPixel == 255)
                        {
                            whitepixels++;
                            if (whitepixels > (processimg.Height * rightx_threshold))
                            {
                                right_x = i;
                                is_rightx_found = true;
                                break;
                            }
                        }
                    }
                    //Debug.WriteLine($"Right column {i} whitepixels count: {whitepixels}");
                }
            }
            //SourceImg = new Mat(SourceImg, new Rectangle(left_x-100, 0, right_x - left_x +100, SourceImg.Height)).Clone();
            Mat houghimg = new Mat();
            houghimg = new Mat(processimg, new Rectangle(left_x, 0, right_x - left_x, processimg.Height)).Clone();  //277pxls約為1cm(廠商保證) 採取往內推0.5cm為138pxls
            if (saveoptions.saveoption.Roi_Rotate)
            {
                CvInvoke.Imwrite($"HoughRotate_3_crop.bmp", houghimg);
            }
            //先對影像反向
            CvInvoke.BitwiseNot(houghimg, houghimg);
            if (saveoptions.saveoption.Roi_Rotate)
            {
                CvInvoke.Imwrite($"HoughRotate_4_reverse.bmp", houghimg);
            }
            //輪廓檢測濾除上方邊條與雜訊
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            using (Mat hierarchy = new Mat())
            {
                CvInvoke.FindContours(houghimg, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                int maxArea = 1000000;
                for (int i = 0; i < contours.Size; i++)
                {
                    double area = CvInvoke.ContourArea(contours[i]);
                    Rectangle boundingRect1 = CvInvoke.BoundingRectangle(contours[i]);
                    if (area < maxArea)
                    {
                        if (boundingRect1.Y < 200 || boundingRect1.Y > 1250)
                        {
                            CvInvoke.FillPoly(houghimg, contours[i], new MCvScalar(0, 0, 0));
                        }
                    }
                }
            }
            if (saveoptions.saveoption.Roi_Rotate)
            {
                CvInvoke.Imwrite("HoughRotate_5_fillcontour.bmp", houghimg);
            }

            // 創建一個白色底色的遮罩
            using (Mat mask = new Mat(houghimg.Size, DepthType.Cv8U, 1))
            {
                // 先將整個遮罩填充為白色
                mask.SetTo(new MCvScalar(255));

                // 在x=300到x=houghimg.Width-300之間的區域填充為黑色
                Rectangle centerRegion = new Rectangle(300, 0, houghimg.Width - 600, houghimg.Height);
                CvInvoke.Rectangle(mask, centerRegion, new MCvScalar(0), -1); // -1表示填充矩形
                // 將y=houghimg.Height/2到底部的區域填充為黑色
                Rectangle bottomRegion = new Rectangle(0, houghimg.Height / 2, houghimg.Width, houghimg.Height / 2);
                CvInvoke.Rectangle(mask, bottomRegion, new MCvScalar(0), -1); // -1表示填充矩形
                // 將遮罩與houghimg相乘
                using (Mat result = new Mat())
                {
                    CvInvoke.BitwiseAnd(houghimg, houghimg, result, mask);
                    houghimg = result.Clone(); // 將結果複製回houghimg
                }

                //// 儲存遮罩和結果以便檢視
                //CvInvoke.Imwrite("Mask.bmp", mask);
                //CvInvoke.Imwrite("MaskedResult.bmp", houghimg); // 可選：儲存處理後的圖像
            }
            LineSegment2D[] lines = CvInvoke.HoughLinesP(
                houghimg,    // 使用邊緣圖像作為輸入
                1,            // 距離解析度
                Math.PI / 180, // 角度解析度
                50,           // 降低最小投票數
                30,           // 降低最小線段長度
                10            // 允許較小的間隙
            );
            Point center = new Point(houghimg.Width / 2, houghimg.Height / 2);
            Point rot_left_top = new Point(int.MaxValue, int.MaxValue);
            Point rot_right_top = new Point(int.MinValue, int.MaxValue);
            // 繪製檢測到的線段
            Debug.WriteLine($"============================================================================");
            CvInvoke.CvtColor(houghimg, houghimg, ColorConversion.Gray2Bgr);
            foreach (var line in lines)
            {
                CvInvoke.Line(houghimg, line.P1, line.P2, new MCvScalar(0, 255, 0), 2); // 綠色線條
                //中心點以上的線才進來比較
                if (line.P1.Y < center.Y && line.P2.Y < center.Y)
                {
                    LineSegment2D tempLine = NormalizeLineSegment(line);//線段整理讓P1在左P2在右
                    if (tempLine.P1.X < center.X) //確保P1在左半邊
                    {
                        if (tempLine.P1.X <= rot_left_top.X || Math.Abs(tempLine.P1.X - rot_left_top.X) <= 30) //解決有時會因印刷突出造成X被拉到最小而沒有進入條件
                        {

                            rot_left_top.X = Math.Min(tempLine.P1.X, rot_left_top.X);
                            int y = Math.Min(tempLine.P1.Y, tempLine.P2.Y);
                            rot_left_top.Y = Math.Min(y, rot_left_top.Y);
                            if (rot_left_top.Y > 200)
                            {
                                Debug.WriteLine($"P1.X {tempLine.P1.X} 進入條件 <= rot_left_top.X {rot_left_top.X}");
                                Debug.WriteLine($"P2(X,Y) = ({tempLine.P2.X} ,{tempLine.P2.Y})");
                            }
                        }
                    }
                    else //確保P2在右半邊
                    {
                        if (tempLine.P2.X >= rot_right_top.X || Math.Abs(tempLine.P2.X - rot_right_top.X) <= 30)
                        {


                            rot_right_top.X = Math.Max(tempLine.P2.X, rot_right_top.X);
                            int y = Math.Min(tempLine.P1.Y, tempLine.P2.Y);
                            rot_right_top.Y = Math.Min(y, rot_right_top.Y);
                            if (tempLine.P2.Y == 260)
                            {
                                Debug.WriteLine($"P2.X {tempLine.P2.X} 進入條件 >= rot_right_top.X {rot_right_top.X}");
                                Debug.WriteLine($"P2(X,Y) = ({tempLine.P2.X} ,{tempLine.P2.Y})");
                            }
                        }
                    }
                }
            }
            if (saveoptions.saveoption.Roi_Rotate)
            {
                CvInvoke.Imwrite("HoughRotate_6_houghline.bmp", houghimg);
            }
            //以左上角點減右上角點計算斜率
            double delta_x = rot_left_top.X - rot_right_top.X;
            double delta_y = rot_left_top.Y - rot_right_top.Y;
            Debug.WriteLine($"左上角(x,y)=({rot_left_top.X},{rot_left_top.Y}),右上角({rot_right_top.X},{rot_right_top.Y})");
            //水平情況不用校正，跳過旋轉步驟
            bool is_rotated = true;
            if (delta_y == 0)
            {
                is_rotated = false;
                if (saveoptions.saveoption.Roi_Rotate)
                {
                    CvInvoke.Imwrite("HoughRotate_7_rotate.bmp", sourceimg);
                }
                return sourceimg;
            }
            double angle = 0.0;
            //旋轉原圖
            if (is_rotated)
            {
                angle = Math.Atan(delta_y / delta_x) * (180.0 / Math.PI);
                sourceimg = sourceimg.ToImage<Bgr, byte>().Rotate(-angle, new Bgr(0, 0, 0)).Mat;
                if (saveoptions.saveoption.Roi_Rotate)
                {
                    CvInvoke.Imwrite("HoughRotate_7_rotate.bmp", sourceimg);
                }
            }
            return sourceimg;
        }
        /// <summary>
        /// 裁切檢測區域
        /// </summary>
        private static Mat Crop_ROI(Mat sourceimg, int crop_pixels, OCT_Parameters_SaveOptions saveoptions)
        {
            Mat processimg = sourceimg.Clone();
            //垂直投影
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(processimg, processimg, 100, 255, ThresholdType.Binary);
            if (saveoptions.saveoption.Roi_Crop)
            {
                CvInvoke.Imwrite("ROICrop_1_source.bmp", processimg);
            }
            Point center = new Point(sourceimg.Width / 2, sourceimg.Height / 2);
            int top_y = center.Y, bottom_y = center.Y;
            //上下裁切
            unsafe
            {
                // 初始化指標和變數
                double top_percentageThreshold = 0.93; // 水平白色像素的百分比閾值
                double buttom_percentageThreshold = 0.95; // 水平白色像素的百分比閾值
                byte* ptr = (byte*)processimg.DataPointer;
                int centerY = processimg.Height / 2;
                int whitepixels = 0;
                // 從中心往上找
                for (int y = centerY; y >= 0; y--)
                {
                    whitepixels = 0;
                    byte* rowPtr = ptr + (y * processimg.Width);
                    for (int x = 0; x < processimg.Width; x++)
                    {
                        if (rowPtr[x] == 255)
                        {
                            whitepixels++;
                        }
                    }
                    if (whitepixels > (processimg.Width * top_percentageThreshold))
                    {
                        top_y = y;
                        break;
                    }
                }
                // 從中心往下找
                for (int y = centerY; y < processimg.Height; y++)
                {
                    whitepixels = 0;
                    byte* rowPtr = ptr + (y * processimg.Width);
                    for (int x = 0; x < processimg.Width; x++)
                    {
                        if (rowPtr[x] == 255)
                        {
                            whitepixels++;
                        }
                    }
                    if (whitepixels > (processimg.Width * buttom_percentageThreshold))
                    {
                        bottom_y = y;
                        break;
                    }
                }
            }
            Mat crop1 = new Mat(sourceimg, new Rectangle(0, top_y, processimg.Width, bottom_y - top_y));
            if (saveoptions.saveoption.Roi_Crop)
            {
                CvInvoke.Imwrite("ROICrop_2_crop1.bmp", crop1);
            }
            Mat procrssimg2 = crop1.Clone();
            CvInvoke.CvtColor(procrssimg2, procrssimg2, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(procrssimg2, procrssimg2, 230, 255, ThresholdType.Binary);
            if (saveoptions.saveoption.Roi_Crop)
            {
                CvInvoke.Imwrite("RoiCrop_3_crop1threshold.bmp", procrssimg2);
            }
            int left_x = 0, right_x = sourceimg.Width;
            //左右裁切
            unsafe
            {
                double leftx_threshold = 0.3;
                bool is_leftx_found = false;
                int centerY = sourceimg.Height / 2;
                int whitepixels = 0;
                // 重新初始化指標，使用新的 ROI 影像
                byte* ptr = (byte*)procrssimg2.DataPointer;
                // 從左往右找
                for (int i = 0; i < procrssimg2.Width; i++)
                {
                    if (is_leftx_found)
                    {
                        //Debug.WriteLine($"leftx is found:{left_x}");
                        break;
                    }
                    whitepixels = 0;
                    for (int j = 0; j < crop1.Height; j++)
                    {
                        byte* currentPixel = ptr + (j * crop1.Width + i);
                        if (*currentPixel == 255)
                        {
                            whitepixels++;
                            if (whitepixels > (crop1.Height * leftx_threshold))
                            {
                                left_x = i;
                                is_leftx_found = true;
                                break;
                            }
                        }
                    }
                }
            }
            Mat roiimage = new Mat();
            //這邊可加else新增找不到left_x時跳錯
            if (left_x != 0)
            {
                roiimage = new Mat(crop1, new Rectangle(left_x, 0, crop_pixels, crop1.Height));
            }
            //double crop_pixels = Math.Round(crop_centimeter * 5472 / 20);
            return roiimage;
        }
        /// <summary>
        /// 霍夫直線交換兩線
        /// </summary>
        public static LineSegment2D NormalizeLineSegment(LineSegment2D line)
        {
            // 如果 P1.X 大於 P2.X，交換兩個點
            if (line.P1.X > line.P2.X)
            {
                return new LineSegment2D(line.P2, line.P1);
            }
            return line;
        }
        /// <summary>
        /// 網點區自動 Threshold
        /// Otsu + 各區線性補償
        /// </summary>
        public static double GetAutoMeshThreshold(Mat area, int areaPercent)
        {
            if (area == null || area.IsEmpty)
            {
                throw new ArgumentException(
                    $"Area{areaPercent} 影像為空，無法計算自動 Threshold"
                );
            }

            using (Mat gray = new Mat())
            using (Mat otsuBinary = new Mat())
            {
                if (area.NumberOfChannels == 3)
                {
                    CvInvoke.CvtColor(
                        area,
                        gray,
                        ColorConversion.Bgr2Gray
                    );
                }
                else
                {
                    area.CopyTo(gray);
                }

                double otsuThreshold =
                    CvInvoke.Threshold(
                        gray,
                        otsuBinary,
                        0,
                        255,
                        ThresholdType.Binary |
                        ThresholdType.Otsu
                    );

                double finalThreshold;

                switch (areaPercent)
                {
                    case 10:
                        finalThreshold =
                            1.6363636364 * otsuThreshold
                            - 88.9090909091;
                        break;

                    case 20:
                        finalThreshold =
                            1.1538461538 * otsuThreshold
                            + 3.1538461538;
                        break;

                    case 30:
                        finalThreshold =
                            1.0000000000 * otsuThreshold
                            + 34.0000000000;
                        break;

                    case 40:
                        finalThreshold =
                            0.9666666667 * otsuThreshold
                            + 39.0333333333;
                        break;

                    case 60:
                        finalThreshold =
                            1.3214285714 * otsuThreshold
                            - 22.1785714286;
                        break;

                    case 70:
                        finalThreshold =
                            1.4642857143 * otsuThreshold
                            - 31.8571428571;
                        break;

                    case 80:
                        finalThreshold =
                            1.4000000000 * otsuThreshold
                            - 6.8000000000;
                        break;

                    case 90:
                        finalThreshold =
                            1.5769230769 * otsuThreshold
                            - 7.3461538462;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(areaPercent),
                            $"不支援 Area{areaPercent}"
                        );
                }

                finalThreshold =
                    Math.Max(
                        0.0,
                        Math.Min(
                            255.0,
                            finalThreshold
                        )
                    );

                double roundedThreshold =
                    Math.Round(finalThreshold);

                Debug.WriteLine(
                    $"[AutoMesh] " +
                    $"Area{areaPercent}% " +
                    $"Otsu={otsuThreshold:F1}, " +
                    $"Final={roundedThreshold:F0}"
                );

                return roundedThreshold;
            }
        }


        /// <summary>
        /// 一次取得 8 個網點區的自動 Threshold
        /// 順序：10、20、30、40、60、70、80、90
        /// </summary>
        public static List<double> GetAutoMeshThresholds(
            List<Mat> meshpAreas)
        {
            if (meshpAreas == null)
            {
                throw new ArgumentNullException(
                    nameof(meshpAreas)
                );
            }

            if (meshpAreas.Count != 8)
            {
                throw new ArgumentException(
                    $"網點 ROI 數量錯誤，" +
                    $"目前為 {meshpAreas.Count}，" +
                    $"正常應為 8"
                );
            }

            int[] areaPercents =
            {
        10,
        20,
        30,
        40,
        60,
        70,
        80,
        90
    };

            List<double> thresholds =
                new List<double>();

            for (int i = 0;
                 i < meshpAreas.Count;
                 i++)
            {
                double threshold =
                    GetAutoMeshThreshold(
                        meshpAreas[i],
                        areaPercents[i]
                    );

                thresholds.Add(
                    threshold
                );
            }

            Debug.WriteLine(
                "[AutoMesh] Final Thresholds = " +
                string.Join(
                    ", ",
                    thresholds.Select(
                        x => x.ToString("F0")
                    )
                )
            );

            return thresholds;
        }
        /// <summary>
        /// 網點區批量二值化
        /// </summary>
        public static List<Mat> Meshp_areas_threshold(List<Mat> meshp_areas, List<double> thresholds)
        {
            // 定義 PictureBox 陣列，對應 PictureBox2 ~ PictureBox9
            for (int i = 0; i < meshp_areas.Count; i++)
            {
                CvInvoke.CvtColor(meshp_areas[i], meshp_areas[i], ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(meshp_areas[i], meshp_areas[i], thresholds[i], 255, ThresholdType.Binary);
            }
            return meshp_areas;
        }
        /// <summary>
        /// 判定矩形是否有在範圍內
        /// </summary>
        public static bool IsInsideRange(Rectangle rect, Rectangle range)
        {
            return rect.X >= range.X && rect.Y >= range.Y &&
                   (rect.X + rect.Width) <= (range.X + range.Width) &&
                   (rect.Y + rect.Height) <= (range.Y + range.Height);
        }
        /// <summary>
        /// 判定該記憶體位址是否為孤立白點
        /// </summary>
        public static unsafe bool IsIsolatedPixel(byte* ptr, int step, int width, int height, Point p)
        {
            // 8 鄰域偏移
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nx = p.X + dx[i];
                int ny = p.Y + dy[i];

                // 確保不超出影像範圍
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    byte* neighbor_ptr = ptr + (ny * step + nx); // 直接使用指標存取像素
                    if (*neighbor_ptr == 255)
                    {
                        return false; // 如果鄰近有白點，就不是孤立點
                    }
                }
            }

            return true; // 若所有鄰近點都是黑色 (0)，則為孤立白點
        }
        /// <summary>
        /// 連通區域分析
        /// </summary>
        public static List<Rectangle> DetectConnectedComponents(Mat binaryImage)
        {
            // 把影像中「所有連在一起的白色區域」分組編號
            Mat labels = new Mat(); // 每個 pixel 屬於哪一個區域
            Mat stats = new Mat(); // 每個區域的統計資料
            Mat centroids = new Mat(); // 每個區域的中心點
            int numLabels = CvInvoke.ConnectedComponentsWithStats(binaryImage, labels, stats, centroids, LineType.EightConnected); // 總共分成幾個區域（含背景），會用 EightConnected 因為3pt～12pt 的字體筆劃非常細，很多筆劃只靠斜角相連(EightConnected 斜角相連也算同一區)，故使用 EightConnected 可以避免字被切碎，


            // 建立彩色圖片用於繪製結果
            Mat colorImage = new Mat();
            CvInvoke.CvtColor(binaryImage, colorImage, ColorConversion.Gray2Bgr);

            Random rng = new Random();
            List<Rectangle> fontRectangles = new List<Rectangle>();

            using (Matrix<int> statsMatrix = new Matrix<int>(stats.Rows, stats.Cols, stats.NumberOfChannels))
            {
                stats.CopyTo(statsMatrix);
                for (int i = 1; i < numLabels; i++) // 跳過背景(0)，label 0 是背景
                {
                    // 每個 label 取出 5 個關鍵數據
                    int left = statsMatrix.Data[i, 0]; // 區塊左上角 X
                    int top = statsMatrix.Data[i, 1]; // 區塊左上角 Y
                    int width = statsMatrix.Data[i, 2]; // 區塊寬度
                    int height = statsMatrix.Data[i, 3];   // 區塊高度
                    int area = statsMatrix.Data[i, 4];  // 白色像素總數

                    // 把不可能是字的東西丟掉
                    if (800 < area && area < 44000) // 過濾面積(area<200 幾乎都是雜點、噪聲；area > 44000 可能是背景、大片污漬、誤判區)
                    {
                        fontRectangles.Add(new Rectangle(left, top, width, height)); // 這裡還不知道是不是完整的一個字，可能只是字的一部分，真正的字級與裁切，後面才會做

                        //// 產生隨機顏色
                        //MCvScalar color = new MCvScalar(rng.Next(0, 255), rng.Next(0, 255), rng.Next(0, 255));

                        //// 畫矩形框
                        //CvInvoke.Rectangle(colorImage, new Rectangle(left, top, width, height), color, 2);

                        //// 加上文字標籤
                        //CvInvoke.PutText(
                        //    colorImage,
                        //    $"Region {i} (Area: {area})",
                        //    new Point(left, top - 5),
                        //    FontFace.HersheyComplex,
                        //    0.6,
                        //    color,
                        //    2
                        //);
                    }
                }
            }

            // 儲存偵測結果圖片
            //CvInvoke.Imwrite("connected_components_result.bmp", colorImage);
            //Debug.WriteLine($"偵測到 {fontRectangles.Count} 個連通區域，結果已儲存至 connected_components_result.bmp");

            return fontRectangles;
        }
        /// <summary>
        /// 分析出的連通區域整理成單一大矩形
        /// </summary>
        public static Rectangle CalculateBoundingRectangle(List<Rectangle> rectangles, int imageHeight)
        {
            // 如果清單為空或沒有矩形，返回 Rectangle.Empty
            if (rectangles == null || rectangles.Count == 0)
            {
                return Rectangle.Empty;
            }

            // 找到最小的 x 和最大的 x+width
            int minX = rectangles.Min(rect => rect.X);
            int maxX = rectangles.Max(rect => rect.X + rect.Width);

            // 固定 Y 的範圍為 0 ~ imageHeight
            int minY = 0;
            int totalHeight = imageHeight;

            // 計算 X 的總寬
            int totalWidth = maxX - minX;

            // 回傳新的矩形，Y 範圍固定
            return new Rectangle(minX, minY, totalWidth, totalHeight);
        }
        /// <summary>
        /// 絕對位置輔助區域分類
        /// </summary>
        public static List<List<Rectangle>> ClassifyFontRegions(List<Rectangle> fontRectangles, List<Rectangle> ranges)
        {
            List<List<Rectangle>> fontLists = new List<List<Rectangle>>();
            // ranges.Count 有幾個，就建立幾個空的 List
            for (int i = 0; i < ranges.Count; i++)
                fontLists.Add(new List<Rectangle>());
            // 對每一個「剛剛偵測到的字體區塊」做分類
            foreach (var rect in fontRectangles)
            {
                // 檢查這個字框「落在哪一個字級區」(從第一個字級區（3pt）開始，如果不是換下一個字級區)
                for (int i = 0; i < ranges.Count; i++)
                {
                    if (IsInsideRange(rect, ranges[i]))
                    {
                        // 一個字體框只會屬於一個字級，一找到就加入對應字級的清單，不再往後檢查，避免被分到多個字級
                        fontLists[i].Add(rect);
                        break;
                    }
                }
            }
            return fontLists;
        }

        /// <summary>
        /// Python 新版陰陽版在左右切割後會旋轉 90 度，因此字級 X 座標的實際寬度
        /// 會與舊版未旋轉影像不同。保留原本 3pt~12pt 的相對分區比例，
        /// 再依目前影像寬度自動縮放，避免所有 connected component 都落不到舊座標範圍。
        /// </summary>
        private static List<Rectangle> BuildScaledFontRanges(
            OCT_Parameters_PrintType currentPrintTypeParams,
            int imageWidth,
            int imageHeight,
            string debugLabel = "")
        {
            int[] starts = new int[]
            {
                currentPrintTypeParams.font_range_3pt,
                currentPrintTypeParams.font_range_4pt,
                currentPrintTypeParams.font_range_5pt,
                currentPrintTypeParams.font_range_6pt,
                currentPrintTypeParams.font_range_7pt,
                currentPrintTypeParams.font_range_8pt,
                currentPrintTypeParams.font_range_9pt,
                currentPrintTypeParams.font_range_10pt,
                currentPrintTypeParams.font_range_11pt,
                currentPrintTypeParams.font_range_12pt
            };

            // 舊設定只有每一級的起點，12pt 沒有終點。
            // 用最後兩級的間距推估舊座標系的總寬，等同 Python PrintTypeParams
            // 0,200,...,1800 時推估總寬為 2000。
            int lastStep = starts.Length >= 2
                ? Math.Max(1, starts[starts.Length - 1] - starts[starts.Length - 2])
                : Math.Max(1, imageWidth / 10);

            int referenceEnd = Math.Max(starts[starts.Length - 1] + lastStep, 1);
            double scale = (double)imageWidth / referenceEnd;

            int[] scaled = new int[starts.Length + 1];
            for (int i = 0; i < starts.Length; i++)
            {
                scaled[i] = (int)Math.Round(starts[i] * scale);
                scaled[i] = Math.Max(0, Math.Min(imageWidth, scaled[i]));
            }
            scaled[starts.Length] = imageWidth;

            // 確保座標單調遞增，避免參數設定異常造成負寬度。
            for (int i = 1; i < scaled.Length; i++)
            {
                if (scaled[i] < scaled[i - 1])
                    scaled[i] = scaled[i - 1];
            }

            List<Rectangle> ranges = new List<Rectangle>();
            for (int i = 0; i < 10; i++)
            {
                int x1 = scaled[i];
                int x2 = scaled[i + 1];
                int width = Math.Max(1, x2 - x1);
                if (x1 + width > imageWidth)
                    width = Math.Max(1, imageWidth - x1);

                ranges.Add(new Rectangle(x1, 0, width, imageHeight));
            }

            Debug.WriteLine($"[PythonLogic][{debugLabel} FontRanges] imageW={imageWidth}, referenceEnd={referenceEnd}, scale={scale:F4}");
            for (int i = 0; i < ranges.Count; i++)
            {
                Debug.WriteLine($"[PythonLogic][{debugLabel} FontRanges] {i + 3}pt: X={ranges[i].X} ~ {ranges[i].Right}");
            }

            return ranges;
        }

        private static void DebugFontClassification(string label, List<Rectangle> allRects, List<List<Rectangle>> classified)
        {
            Debug.WriteLine($"[PythonLogic][{label} FontDetect] total components={allRects.Count}");
            for (int i = 0; i < classified.Count; i++)
            {
                Debug.WriteLine($"[PythonLogic][{label} {i + 3}pt] components={classified[i].Count}");
            }
        }
        /// <summary>
        /// 自動校正閾值(建立臨時標準功能)
        /// </summary>
        /// <summary>
        /// 若某一字級因二值化/連通域波動沒有抓到 component，
        /// 改用該字級完整 X 分區作為 fallback ROI，避免後續產生 Bounds.Empty
        /// 導致 Resize 被跳過、模板比對尺寸不一致而中斷整體分析。
        /// </summary>
        private static void ApplyFontRangeFallback(
            List<List<Rectangle>> classifiedLists,
            List<Rectangle> ranges,
            string debugLabel)
        {
            if (classifiedLists == null || ranges == null)
                return;

            int count = Math.Min(classifiedLists.Count, ranges.Count);
            for (int i = 0; i < count; i++)
            {
                if (classifiedLists[i] == null)
                    classifiedLists[i] = new List<Rectangle>();

                if (classifiedLists[i].Count == 0)
                {
                    Rectangle fallback = ranges[i];
                    if (fallback.Width > 0 && fallback.Height > 0)
                    {
                        classifiedLists[i].Add(fallback);
                        Debug.WriteLine(
                            $"[PythonLogic][{debugLabel} {i + 3}pt] no component -> fallback range " +
                            $"X={fallback.X}~{fallback.Right}, W={fallback.Width}");
                    }
                }
            }
        }

        public static List<double> Autotune_threshold(List<Mat> meshp_areas_sourceimg)
        {
            List<Mat> meshp_areas = meshp_areas_sourceimg.Select(mat => mat.Clone()).ToList();
            List<double> accurate_thresholds = Enumerable.Repeat(128.0, meshp_areas.Count).ToList(); // 初始值 128
            double tolerance = 0.001; // 設定容許誤差
            double initial_bias = 10; // 初始變動值

            int aa = 1;
            for (int i = 0; i < meshp_areas.Count; i++)
            {
                CvInvoke.CvtColor(meshp_areas[i], meshp_areas[i], ColorConversion.Bgr2Gray);

                double threshold = accurate_thresholds[i];
                double target_ratio = (i < 4) ? (0.1 + 0.1 * i) : (0.2 + 0.1 * i); // 設定目標比例 10~40%, 60~90%
                double bias = initial_bias; // 每張圖的初始 bias
                bool previous_direction = true; // true: 增加閾值, false: 降低閾值
                Debug.WriteLine($"[初始] 區塊 {target_ratio * 100}% 目標比例: {target_ratio:P2}, 初始 Threshold: {threshold}");
                while (true)
                {
                    Mat temp = new Mat();
                    CvInvoke.Threshold(meshp_areas[i], temp, threshold, 255, ThresholdType.Binary);

                    int total_pixels = temp.Width * temp.Height;
                    int black_pixels = 0;

                    unsafe
                    {
                        byte* ptr = (byte*)temp.DataPointer;
                        for (int x = 0; x < temp.Width; x++)
                        {
                            for (int y = 0; y < temp.Height; y++)
                            {
                                byte* current_ptr = ptr + (x + (temp.Width * y));
                                if (*current_ptr == 0) black_pixels++;
                            }
                        }
                    }

                    double black_ratio = (double)black_pixels / total_pixels; // 當前黑色比例
                    Debug.WriteLine($"[調整] 區塊 {target_ratio * 100}% 目前 Threshold: {threshold:F2}, 黑色比例: {black_ratio:P2}, Bias: {bias:F2}");
                    // 結束條件
                    if (Math.Abs(Math.Abs(black_ratio) - target_ratio) < tolerance || bias < 0.2)
                    {
                        //CvInvoke.Imwrite($"test_{target_ratio * 100}_{aa}.bmp", temp);
                        aa++;
                        Debug.WriteLine($"[完成] 區塊 {target_ratio * 100}% 最終 Threshold: {threshold:F2}, 最終黑色比例: {black_ratio:P2}");
                        break;
                    }

                    bool current_direction = black_ratio < target_ratio; // true: 需要增加閾值, false: 需要降低閾值

                    if (current_direction != previous_direction)
                    {
                        bias /= 2; // 方向改變，縮小 bias
                    }

                    // 更新 Threshold
                    if (current_direction)
                    {
                        threshold += bias; // 黑點太少 → 提高閾值
                    }
                    else
                    {
                        threshold -= bias; // 黑點太多 → 降低閾值
                    }

                    previous_direction = current_direction;
                }

                accurate_thresholds[i] = threshold;
            }
            return accurate_thresholds;
        }
        public static void UpdateUI(Action uiAction)
        {
            UpdateUIAction?.Invoke(uiAction);
        }
        #endregion

        #region 影像前處理
        /// <summary>
        /// ROI整張紙
        /// </summary>
        public static Mat Get_ROI_Image(Mat sourceimg, OCT_Parameters_SaveOptions saveoptions)
        {
            //擷取紙張
            Mat crop_paper_img = Crop_Paper(sourceimg, saveoptions);
            CvInvoke.Imwrite("ROI_1_croppaper.bmp", crop_paper_img);
            //旋轉校正
            Mat rot_img = Rotate(crop_paper_img, saveoptions);
            CvInvoke.Imwrite("ROI_2_rotate.bmp", rot_img);
            //裁切檢測區域
            Mat roi_img = Crop_ROI(rot_img, 3146, saveoptions);
            CvInvoke.Imwrite("ROI_3_roi.bmp", roi_img);
            return roi_img;
        }
        /// <summary>
        /// 網點區裁切
        /// </summary>
        public static async Task<(List<Mat>, Mat)> Meshpoint_crop(Mat sourceimg1, Mat sourceimg2)
        {
            int gridWidth = MeshROI.GridWidth;
            int gridHeight = MeshROI.GridHeight;

            List<Mat> meshp_areas = new List<Mat>();

            Mat gray1 = sourceimg1.Clone();
            Mat gray2 = sourceimg2.Clone();
            CvInvoke.CvtColor(gray1, gray1, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(gray2, gray2, ColorConversion.Bgr2Gray);

            // ================================================================
            // Station 1
            // 黃框範圍內直接做灰階投影，再找一階梯度最大值
            // ================================================================

            Rectangle midRoi1 = ClampRectangle(MeshROI.S1MidSearchBox, gray1.Width, gray1.Height);
            if (midRoi1.IsEmpty)
                throw new InvalidOperationException("Station 1 Mid-X 搜尋框無效");

            double[] xProfile1 = MeanColumnProfile(gray1, midRoi1);
            int midIdx1 = ArgMaxDiff(xProfile1, negate: true);   // -diff：由亮變暗
            int midX1 = midRoi1.X + midIdx1;
            int leftX1 = Math.Max(0, midX1 - MeshROI.S1LeftXOffsetFromMid);

            Rectangle topRoi1 = ClampRectangle(MeshROI.S1TopSearchBox, gray1.Width, gray1.Height);
            if (topRoi1.IsEmpty)
                throw new InvalidOperationException("Station 1 Top-Y 搜尋框無效");

            double[] yProfile1 = MeanRowProfile(gray1, topRoi1);
            int topIdx1 = ArgMaxDiff(yProfile1, negate: false);  // +diff：由暗變亮
            int topY = topRoi1.Y + topIdx1;

            Mat Area10 = SafeCropRotateCCW(
                sourceimg1,
                topY,
                topY + gridHeight,
                leftX1,
                midX1,
                gridWidth,
                gridHeight
            );

            Mat Area20 = SafeCropRotateCCW(
                sourceimg1,
                topY + gridHeight,
                topY + 2 * gridHeight,
                leftX1,
                midX1,
                gridWidth,
                gridHeight
            );

            Mat Area60 = SafeCropRotateCCW(
                sourceimg1,
                topY,
                topY + gridHeight,
                midX1,
                midX1 + gridWidth,
                gridWidth,
                gridHeight
            );

            Mat Area70 = SafeCropRotateCCW(
                sourceimg1,
                topY + gridHeight,
                topY + 2 * gridHeight,
                midX1,
                midX1 + gridWidth,
                gridWidth,
                gridHeight
            );

            // ================================================================
            // BreakArea 01：維持 Python Otsu 掃描邏輯
            // ================================================================

            Mat thresh1 = new Mat();
            CvInvoke.Threshold(gray1, thresh1, 0, 255, ThresholdType.Binary | ThresholdType.Otsu);

            int scanX1 = Math.Max(0, Math.Min(MeshROI.Break01ScanX1, thresh1.Width - 1));
            int scanX2 = Math.Max(scanX1 + 1, Math.Min(MeshROI.Break01ScanX2, thresh1.Width));

            int bk01TopY = 0;

            unsafe
            {
                byte* ptr = (byte*)thresh1.DataPointer;
                int step = thresh1.Step;

                for (int y = 0; y < thresh1.Height; y++)
                {
                    int black = 0;

                    for (int x = scanX1; x < scanX2; x++)
                    {
                        if (*(ptr + y * step + x) == 0)
                            black++;
                    }

                    if (black > (scanX2 - scanX1) * MeshROI.Break01TopBlackRatio)
                    {
                        bk01TopY = y;
                        break;
                    }
                }
            }

            int bk01BtnY = Math.Min(thresh1.Height, bk01TopY + 100);

            unsafe
            {
                byte* ptr = (byte*)thresh1.DataPointer;
                int step = thresh1.Step;

                for (int y = bk01TopY; y < thresh1.Height; y++)
                {
                    int white = 0;

                    for (int x = scanX1; x < scanX2; x++)
                    {
                        if (*(ptr + y * step + x) == 255)
                            white++;
                    }

                    if (white > (scanX2 - scanX1) * MeshROI.Break01BottomWhiteRatio)
                    {
                        bk01BtnY = Math.Min(
                            thresh1.Height,
                            y + MeshROI.Break01BottomExtra
                        );
                        break;
                    }
                }
            }

            int bk01LeftX = 0;

            unsafe
            {
                byte* ptr = (byte*)thresh1.DataPointer;
                int step = thresh1.Step;

                int startX = Math.Min(MeshROI.Break01LeftScanStartX, thresh1.Width - 1);

                for (int x = startX; x > 0; x--)
                {
                    int white = 0;

                    for (int y = bk01TopY; y < bk01BtnY; y++)
                    {
                        if (*(ptr + y * step + x) == 255)
                            white++;
                    }

                    if (white > Math.Max(1, bk01BtnY - bk01TopY) * MeshROI.Break01LeftWhiteRatio)
                    {
                        bk01LeftX = x;
                        break;
                    }
                }
            }

            int bk01RightX = Math.Min(
                sourceimg1.Width,
                bk01LeftX + MeshROI.Break01CropWidth
            );

            Mat BreakArea_01_crop =
                (bk01BtnY > bk01TopY && bk01RightX > bk01LeftX)
                ? new Mat(
                    sourceimg1,
                    new Rectangle(
                        bk01LeftX,
                        bk01TopY,
                        bk01RightX - bk01LeftX,
                        bk01BtnY - bk01TopY
                    )
                  ).Clone()
                : new Mat();

            // ================================================================
            // Station 2
            // ================================================================

            Rectangle midRoi2 = ClampRectangle(MeshROI.S2MidSearchBox, gray2.Width, gray2.Height);
            if (midRoi2.IsEmpty)
                throw new InvalidOperationException("Station 2 Mid-X 搜尋框無效");

            // Python 目前驗證版實際以這個 profile 找中線。
            // 這裡直接使用原始 1D profile，避免 C# 與 Python 定位結果不一致。
            double[] xProfile2 = MeanColumnProfile(gray2, midRoi2);
            int midIdx2 = ArgMaxDiff(xProfile2, negate: true);
            int midX2 = midRoi2.X + midIdx2;
            int leftX2 = Math.Max(0, midX2 - MeshROI.S2LeftXOffsetFromMid);

            Rectangle bottomRoi2 = ClampRectangle(MeshROI.S2BottomSearchBox, gray2.Width, gray2.Height);
            if (bottomRoi2.IsEmpty)
                throw new InvalidOperationException("Station 2 Bottom-Y 搜尋框無效");

            double[] yProfile2 = MeanRowProfile(gray2, bottomRoi2);
            if (yProfile2 == null || yProfile2.Length < 2)
                throw new InvalidOperationException("Station 2 Bottom-Y 搜尋框高度太小，無法計算一階梯度");

            // ================================================================
            // ★ 2026-09-05 對應目前 Python 驗證版：
            // S2 Bottom-Y「相對最低平台左邊界」
            //
            // 不再使用：
            //   1. 固定灰階 <75
            //   2. plateau + local gradient window
            //   3. 整個黃色框直接 ArgMaxDiff
            //
            // 現在流程：
            //   1. 每列平均灰階 yProfile2
            //   2. 簡單移動平均（對應 np.convolve mode="same"）
            //   3. 用本張 ROI 自己的 min / max 建立相對黑底門檻
            //   4. 找第一段連續夠長的低亮度平台
            //   5. 取該平台左邊界作為 Bottom-Y
            //   6. 若完全找不到長平台，fallback 到平滑 profile 最低點
            // ================================================================

            int smoothWindow = MeshROI.S2BottomProfileSmoothWindow;
            if (smoothWindow < 1)
                smoothWindow = 1;
            if (smoothWindow % 2 == 0)
                smoothWindow++;

            double[] yProfile2Smooth =
                MovingAverageSameZeroPad(yProfile2, smoothWindow);

            double profileMin = yProfile2Smooth.Min();
            double profileMax = yProfile2Smooth.Max();

            double relativeRatio = MeshROI.S2BottomRelativeDarkRatio;
            relativeRatio = Math.Max(0.0, Math.Min(1.0, relativeRatio));

            double darkThreshold =
                profileMin +
                relativeRatio * (profileMax - profileMin);

            bool[] darkMask = new bool[yProfile2Smooth.Length];

            for (int i = 0; i < yProfile2Smooth.Length; i++)
            {
                darkMask[i] =
                    yProfile2Smooth[i] <= darkThreshold;
            }

            int minPlatformLength =
                Math.Max(1, MeshROI.S2BottomMinPlatformLength);

            // 每一段格式：
            // Item1 = start
            // Item2 = end
            // Item3 = length
            List<Tuple<int, int, int>> platformRuns =
                new List<Tuple<int, int, int>>();

            int runStart = -1;

            for (int i = 0; i < darkMask.Length; i++)
            {
                if (darkMask[i])
                {
                    if (runStart < 0)
                        runStart = i;
                }
                else
                {
                    if (runStart >= 0)
                    {
                        int runEnd = i - 1;
                        int runLength =
                            runEnd - runStart + 1;

                        if (runLength >= minPlatformLength)
                        {
                            platformRuns.Add(
                                Tuple.Create(
                                    runStart,
                                    runEnd,
                                    runLength
                                )
                            );
                        }

                        runStart = -1;
                    }
                }
            }

            // ROI 結尾仍然落在低平台時，補上最後一段
            if (runStart >= 0)
            {
                int runEnd =
                    darkMask.Length - 1;

                int runLength =
                    runEnd - runStart + 1;

                if (runLength >= minPlatformLength)
                {
                    platformRuns.Add(
                        Tuple.Create(
                            runStart,
                            runEnd,
                            runLength
                        )
                    );
                }
            }

            bool fallbackUsed = false;

            int chosenStart;
            int chosenEnd;
            int chosenLength;
            int bottomIdx;

            if (platformRuns.Count > 0)
            {
                Tuple<int, int, int> chosen =
                    platformRuns[0];

                chosenStart = chosen.Item1;
                chosenEnd = chosen.Item2;
                chosenLength = chosen.Item3;

                // ★ 第一段長低平台的左邊界
                bottomIdx = chosenStart;
            }
            else
            {
                // 對應 Python：
                // 完全找不到長平台時，不再 fallback 到最大梯度，
                // 而是改用「平滑 profile 最低點」。
                bottomIdx = 0;

                for (int i = 1; i < yProfile2Smooth.Length; i++)
                {
                    if (yProfile2Smooth[i] <
                        yProfile2Smooth[bottomIdx])
                    {
                        bottomIdx = i;
                    }
                }

                chosenStart = bottomIdx;
                chosenEnd = bottomIdx;
                chosenLength = 1;
                fallbackUsed = true;
            }

            int bottomY =
                bottomRoi2.Y + bottomIdx;

            // ------------------------------------------------------------
            // Debug：對應 Python before_mean / platform_mean
            // ------------------------------------------------------------
            int beforeN =
                Math.Max(1, MeshROI.S2BottomBeforeWindow);

            int beforeStart =
                Math.Max(0, bottomIdx - beforeN);

            int beforeEnd =
                bottomIdx;

            double? beforeMean = null;

            if (beforeEnd > beforeStart)
            {
                double sum = 0.0;

                for (int i = beforeStart; i < beforeEnd; i++)
                    sum += yProfile2Smooth[i];

                beforeMean =
                    sum / (beforeEnd - beforeStart);
            }

            double platformSum = 0.0;

            for (int i = chosenStart; i <= chosenEnd; i++)
                platformSum += yProfile2Smooth[i];

            double platformMean =
                platformSum /
                Math.Max(1, chosenEnd - chosenStart + 1);

            string platformRunText =
                string.Join(
                    ", ",
                    platformRuns.Select(
                        r => $"({r.Item1},{r.Item2},{r.Item3})"
                    )
                );

            Debug.WriteLine(
                $"[PythonCurrent][S2 Bottom-Y] " +
                $"ROI=({bottomRoi2.X},{bottomRoi2.Y},{bottomRoi2.Width},{bottomRoi2.Height}), " +
                $"profileMin={profileMin:F3}, profileMax={profileMax:F3}, " +
                $"relativeRatio={relativeRatio:F3}, darkThreshold={darkThreshold:F3}, " +
                $"smoothWindow={smoothWindow}, minPlatformLength={minPlatformLength}"
            );

            Debug.WriteLine(
                $"[PythonCurrent][S2 Bottom-Y] " +
                $"platformRuns=[{platformRunText}], " +
                $"chosenStart={chosenStart}, chosenEnd={chosenEnd}, chosenLength={chosenLength}, " +
                $"platformMean={platformMean:F3}, " +
                $"beforeMean={(beforeMean.HasValue ? beforeMean.Value.ToString("F3") : "None")}, " +
                $"bottomIdx={bottomIdx}, bottomY={bottomY}, fallbackToProfileMin={fallbackUsed}"
            );

            // 保留 Python 驗證版目前的安全限制
            if (bottomY < 2 * gridHeight)
            {
                Debug.WriteLine(
                    $"[PythonNewLogic][WARNING] bottomY={bottomY} 太高，" +
                    $"安全限制改成 {2 * gridHeight + 50}"
                );

                bottomY = 2 * gridHeight + 50;
            }

            Mat Area30 = SafeCropRotateCCW(
                sourceimg2,
                bottomY - 2 * gridHeight,
                bottomY - gridHeight,
                leftX2,
                midX2,
                gridWidth,
                gridHeight
            );

            Mat Area40 = SafeCropRotateCCW(
                sourceimg2,
                bottomY - gridHeight,
                bottomY,
                leftX2,
                midX2,
                gridWidth,
                gridHeight
            );

            Mat Area80 = SafeCropRotateCCW(
                sourceimg2,
                bottomY - 2 * gridHeight,
                bottomY - gridHeight,
                midX2,
                midX2 + gridWidth,
                gridWidth,
                gridHeight
            );

            Mat Area90 = SafeCropRotateCCW(
                sourceimg2,
                bottomY - gridHeight,
                bottomY,
                midX2,
                midX2 + gridWidth,
                gridWidth,
                gridHeight
            );

            // 保留既有輸出檔名，避免影響現場其他流程
            CvInvoke.Imwrite("Area10.bmp", Area10);
            CvInvoke.Imwrite("Area20.bmp", Area20);
            CvInvoke.Imwrite("Area30.bmp", Area30);
            CvInvoke.Imwrite("Area40.bmp", Area40);
            CvInvoke.Imwrite("Area60.bmp", Area60);
            CvInvoke.Imwrite("Area70.bmp", Area70);
            CvInvoke.Imwrite("Area80.bmp", Area80);
            CvInvoke.Imwrite("Area90.bmp", Area90);

            if (!BreakArea_01_crop.IsEmpty)
                CvInvoke.Imwrite("BreakArea_01_crop.bmp", BreakArea_01_crop);

            Debug.WriteLine(
                $"[PythonNewLogic][S1] " +
                $"TopROI=({topRoi1.X},{topRoi1.Y},{topRoi1.Width},{topRoi1.Height}), " +
                $"MidROI=({midRoi1.X},{midRoi1.Y},{midRoi1.Width},{midRoi1.Height}), " +
                $"topY={topY}, midX={midX1}, leftX={leftX1}"
            );

            Debug.WriteLine(
                $"[PythonNewLogic][S2] " +
                $"BottomROI=({bottomRoi2.X},{bottomRoi2.Y},{bottomRoi2.Width},{bottomRoi2.Height}), " +
                $"MidROI=({midRoi2.X},{midRoi2.Y},{midRoi2.Width},{midRoi2.Height}), " +
                $"bottomY={bottomY}, midX={midX2}, leftX={leftX2}"
            );

            meshp_areas.AddRange(
                new[]
                {
                    Area10,
                    Area20,
                    Area30,
                    Area40,
                    Area60,
                    Area70,
                    Area80,
                    Area90
                }
            );

            gray1.Dispose();
            gray2.Dispose();
            thresh1.Dispose();

            await Task.CompletedTask;
            return (meshp_areas, BreakArea_01_crop);
        }

        public static async Task<(Mat, Mat)> Merge_FontArea(Mat sourceimg3, Mat sourceimg4)
        {
            Mat gray3 = sourceimg3.Clone();
            Mat gray4 = sourceimg4.Clone();

            CvInvoke.CvtColor(gray3, gray3, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(gray4, gray4, ColorConversion.Bgr2Gray);

            // ================================================================
            // Station 3
            // ================================================================

            Rectangle s3Mid = ClampRectangle(FontROI.S3MidSearchBox, gray3.Width, gray3.Height);
            if (s3Mid.IsEmpty)
                throw new InvalidOperationException("Station 3 Mid-X 搜尋框無效");

            // 對應目前 Python 驗證版：直接在整個黃色框 profile 找一階梯度
            double[] xProf3 = MeanColumnProfile(gray3, s3Mid);
            int midIdx3 = ArgMaxDiff(xProf3, negate: true);
            int midX3 = s3Mid.X + midIdx3;

            Rectangle s3Top = ClampRectangle(FontROI.S3TopSearchBox, gray3.Width, gray3.Height);
            if (s3Top.IsEmpty)
                throw new InvalidOperationException("Station 3 Top-Y 搜尋框無效");

            double[] yProf3 = MeanRowProfile(gray3, s3Top);
            int topIdx3 = ArgMaxDiff(yProf3, negate: false);
            int topY3 = s3Top.Y + topIdx3;

            int leftX3 = Math.Max(0, midX3 - FontROI.S3LeftXOffsetFromMid);
            int rightX3 = Math.Min(
                sourceimg3.Width,
                leftX3 + FontROI.S3CropSize.Width
            );

            int crop3Bottom = Math.Min(
                sourceimg3.Height,
                topY3 + FontROI.S3CropSize.Height
            );

            if (rightX3 <= leftX3 || crop3Bottom <= topY3)
                throw new InvalidOperationException("Station 3 字體區裁切範圍無效");

            Mat crop3 = new Mat(
                sourceimg3,
                new Rectangle(
                    leftX3,
                    topY3,
                    rightX3 - leftX3,
                    crop3Bottom - topY3
                )
            ).Clone();

            // ================================================================
            // Station 4
            // ================================================================

            Rectangle s4Mid = ClampRectangle(FontROI.S4MidSearchBox, gray4.Width, gray4.Height);
            if (s4Mid.IsEmpty)
                throw new InvalidOperationException("Station 4 Mid-X 搜尋框無效");

            double[] xProf4 = MeanColumnProfile(gray4, s4Mid);
            int midIdx4 = ArgMaxDiff(xProf4, negate: true);
            int midX4 = s4Mid.X + midIdx4;

            Rectangle s4Bottom = ClampRectangle(FontROI.S4BottomSearchBox, gray4.Width, gray4.Height);
            if (s4Bottom.IsEmpty)
                throw new InvalidOperationException("Station 4 Bottom-Y 搜尋框無效");

            double[] yProf4 = MeanRowProfile(gray4, s4Bottom);
            int btnIdx4 = ArgMaxDiff(yProf4, negate: true);
            int btnY4 = s4Bottom.Y + btnIdx4;

            int leftX4 = Math.Max(0, midX4 - FontROI.S4LeftXOffsetFromMid);
            int rightX4 = Math.Min(
                sourceimg4.Width,
                leftX4 + FontROI.S4CropSize.Width
            );

            int crop4Top = Math.Max(
                0,
                btnY4 - FontROI.S4CropSize.Height
            );

            if (rightX4 <= leftX4 || btnY4 <= crop4Top)
                throw new InvalidOperationException("Station 4 字體區裁切範圍無效");

            Mat crop4 = new Mat(
                sourceimg4,
                new Rectangle(
                    leftX4,
                    crop4Top,
                    rightX4 - leftX4,
                    btnY4 - crop4Top
                )
            ).Clone();

            // ================================================================
            // BreakArea 02
            // 對應 Python：直接相對 Station 3 的 topY / midX 裁切
            // ================================================================

            int baX1 = Math.Max(
                0,
                midX3 - FontROI.Break02LeftOffsetFromMid
            );

            int baX2 = Math.Min(
                sourceimg3.Width,
                midX3 - FontROI.Break02RightOffsetFromMid
            );

            int baY1 = Math.Max(
                0,
                topY3 + FontROI.Break02YOffsetFromTop
            );

            int baY2 = Math.Min(
                sourceimg3.Height,
                baY1 + FontROI.Break02Height
            );

            Mat BreakArea_02_crop =
                (baX2 > baX1 && baY2 > baY1)
                ? new Mat(
                    sourceimg3,
                    new Rectangle(
                        baX1,
                        baY1,
                        baX2 - baX1,
                        baY2 - baY1
                    )
                  ).Clone()
                : new Mat();

            // ================================================================
            // 拼接
            // Python：兩張裁切寬度不同時，resize 到較大的寬度後上下拼接
            // ================================================================

            int targetWidth = Math.Max(crop3.Width, crop4.Width);

            Mat resized3 = crop3;
            Mat resized4 = crop4;

            if (crop3.Width != targetWidth)
            {
                resized3 = new Mat();
                CvInvoke.Resize(
                    crop3,
                    resized3,
                    new Size(targetWidth, crop3.Height),
                    0,
                    0,
                    Inter.Linear
                );
            }

            if (crop4.Width != targetWidth)
            {
                resized4 = new Mat();
                CvInvoke.Resize(
                    crop4,
                    resized4,
                    new Size(targetWidth, crop4.Height),
                    0,
                    0,
                    Inter.Linear
                );
            }

            Mat mergedImage = new Mat(
                new Size(targetWidth, resized3.Height + resized4.Height),
                resized3.Depth,
                resized3.NumberOfChannels
            );

            using (Mat upper = new Mat(
                mergedImage,
                new Rectangle(0, 0, targetWidth, resized3.Height)))
            {
                resized3.CopyTo(upper);
            }

            using (Mat lower = new Mat(
                mergedImage,
                new Rectangle(0, resized3.Height, targetWidth, resized4.Height)))
            {
                resized4.CopyTo(lower);
            }

            // 保留既有輸出檔名
            CvInvoke.Imwrite("processimg3.bmp", crop3);
            CvInvoke.Imwrite("processimg4.bmp", crop4);
            CvInvoke.Imwrite("merged_image.bmp", mergedImage);

            if (!BreakArea_02_crop.IsEmpty)
                CvInvoke.Imwrite("BreakArea_02_crop.bmp", BreakArea_02_crop);

            Debug.WriteLine(
                $"[PythonNewLogic][S3] " +
                $"MidROI=({s3Mid.X},{s3Mid.Y},{s3Mid.Width},{s3Mid.Height}), " +
                $"TopROI=({s3Top.X},{s3Top.Y},{s3Top.Width},{s3Top.Height}), " +
                $"topY={topY3}, midX={midX3}, crop={crop3.Width}x{crop3.Height}"
            );

            Debug.WriteLine(
                $"[PythonNewLogic][S4] " +
                $"MidROI=({s4Mid.X},{s4Mid.Y},{s4Mid.Width},{s4Mid.Height}), " +
                $"BottomROI=({s4Bottom.X},{s4Bottom.Y},{s4Bottom.Width},{s4Bottom.Height}), " +
                $"bottomY={btnY4}, midX={midX4}, crop={crop4.Width}x{crop4.Height}"
            );

            gray3.Dispose();
            gray4.Dispose();

            if (!ReferenceEquals(resized3, crop3))
                resized3.Dispose();

            if (!ReferenceEquals(resized4, crop4))
                resized4.Dispose();

            crop3.Dispose();
            crop4.Dispose();

            await Task.CompletedTask;
            return (mergedImage, BreakArea_02_crop);
        }

        /// <summary>
        /// 切出陰版、陽版、飽滿區
        /// </summary>
        public static Task<Mat> CropFullnessArea(Mat sourceimg5)
        {
            // 建立副本避免影響原圖
            Mat processimg = sourceimg5.Clone();
            //灰階化
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            //otsu二值化
            CvInvoke.Threshold(processimg, processimg, 0, 255, ThresholdType.Otsu);
            CvInvoke.Imwrite("sourceimg5_otsu.bmp", processimg);
            #region 字體區1
            // 找左右邊界和中線
            int rightBoundary = processimg.Width - 1;
            int leftBoundary = 0;
            int top_y = 0;
            unsafe
            {
                byte* ptr = (byte*)processimg.DataPointer;
                int step = processimg.Step;
                // 找上邊界 - 從2000往上找
                bool istopyFound = false;
                for (int y = 2000; y > 0; y--)
                {
                    if (istopyFound) break;

                    int whitePixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitePixels++;
                            if (whitePixels > (int)((2400 - 800) * 0.95))
                            {
                                top_y = y;    //上邊界
                                istopyFound = true;
                                break;
                            }
                        }
                    }
                }
                // 從3400往右找右邊界
                bool rightBoundaryFound = false;
                for (int x = 3400; x < processimg.Width; x++)
                {
                    if (rightBoundaryFound)
                    {
                        break;
                    }
                    int whitepixels = 0;
                    for (int y = top_y; y < 3000; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitepixels++;
                        }
                    }
                    if (whitepixels > (int)((3000 - top_y) * 0.8))
                    {
                        rightBoundary = x;
                        rightBoundaryFound = true;
                    }
                }
                // 從1800往左找左邊界
                bool leftBoundaryFound = false;
                for (int x = 1800; x > 0; x--)
                {
                    if (leftBoundaryFound)
                    {
                        break;
                    }
                    int whiltepixels = 0;
                    for (int y = top_y; y < 3000; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whiltepixels++;
                        }
                    }

                    if (whiltepixels > (int)((3000 - top_y) * 0.8))
                    {
                        leftBoundary = x;
                        leftBoundaryFound = true;
                    }
                }
            }
            Debug.WriteLine($"processimg1 Found boundaries: top_y={top_y}, left_x={leftBoundary}, right_x={rightBoundary}");

            sourceimg5 = new Mat(sourceimg5, new Rectangle(leftBoundary, top_y, rightBoundary - leftBoundary, 3000)).Clone();

            CvInvoke.Imwrite("fullnessarea.bmp", sourceimg5);


            #endregion
            return Task.FromResult(sourceimg5); // 確保是 Task<Mat>
        }

        public static (Mat, Mat, Mat) Area_cropped(Mat SourceImg, OCT_Parameters_SaveOptions saveoptions)
        {
            if (SourceImg == null || SourceImg.IsEmpty)
                return (new Mat(), new Mat(), new Mat());

            Mat gray = SourceImg.Clone();
            CvInvoke.CvtColor(gray, gray, ColorConversion.Bgr2Gray);

            Rectangle valleyRoi = ClampRectangle(
                FontROI.SplitValleySearchBox,
                gray.Width,
                gray.Height
            );

            if (valleyRoi.IsEmpty)
            {
                gray.Dispose();
                return (new Mat(), new Mat(), new Mat());
            }

            // 對應目前 Python 驗證版：
            // 在整個黃色 Valley ROI 內計算 X 向平均灰階，
            // 直接找最暗的位置作為陰 / 陽版中線。
            double[] xProfile = MeanColumnProfile(gray, valleyRoi);
            int valleyIdx = ArgMin(xProfile);
            int midBoundary = valleyRoi.X + valleyIdx;

            int leftBoundary = midBoundary - FontROI.YangWidth;
            int rightBoundary = midBoundary + FontROI.YinWidth;

            if (
                leftBoundary < 0 ||
                rightBoundary > SourceImg.Width ||
                midBoundary <= leftBoundary ||
                rightBoundary <= midBoundary
            )
            {
                Debug.WriteLine(
                    $"[PythonNewLogic][YinYang] 切割越界 " +
                    $"left={leftBoundary}, mid={midBoundary}, right={rightBoundary}, " +
                    $"imageW={SourceImg.Width}"
                );

                gray.Dispose();
                return (new Mat(), new Mat(), new Mat());
            }

            Mat yangArea = new Mat(
                SourceImg,
                new Rectangle(
                    leftBoundary,
                    0,
                    midBoundary - leftBoundary,
                    SourceImg.Height
                )
            ).Clone();

            Mat yinArea = new Mat(
                SourceImg,
                new Rectangle(
                    midBoundary,
                    0,
                    rightBoundary - midBoundary,
                    SourceImg.Height
                )
            ).Clone();

            Mat yinRotated = new Mat();
            Mat yangRotated = new Mat();

            CvInvoke.Rotate(
                yinArea,
                yinRotated,
                RotateFlags.Rotate90CounterClockwise
            );

            CvInvoke.Rotate(
                yangArea,
                yangRotated,
                RotateFlags.Rotate90CounterClockwise
            );

            yinArea.Dispose();
            yangArea.Dispose();
            gray.Dispose();

            if (saveoptions.saveoption.Roi_AreaCrop)
            {
                CvInvoke.Imwrite("YinArea_1.bmp", yinRotated);
                CvInvoke.Imwrite("YangArea_1.bmp", yangRotated);
            }

            Debug.WriteLine(
                $"[PythonNewLogic][YinYang] " +
                $"ValleyROI=({valleyRoi.X},{valleyRoi.Y},{valleyRoi.Width},{valleyRoi.Height}), " +
                $"valleyIdx={valleyIdx}, left={leftBoundary}, mid={midBoundary}, right={rightBoundary}, " +
                $"YangW={FontROI.YangWidth}, YinW={FontROI.YinWidth}"
            );

            // Python 新版飽滿區由 Station 5 的 CropFullnessArea() 獨立取得。
            // 為避免破壞既有 C# 呼叫介面，第三個回傳值保留空 Mat。
            Mat legacyFullnessArea = new Mat();

            return (yinRotated, yangRotated, legacyFullnessArea);
        }

        /// <summary>
        /// 字體與模板計算Y差值並對位
        /// </summary>
        public static Mat FontImg_Correction(Mat sourceimg, Mat template, bool isYin)
        {
            int source_maxY = int.MinValue;
            int source_maxY_atX = int.MinValue;
            int template_maxY = int.MinValue;
            int delta_y = 0;

            // 根據是陰版或陽版，設定前景色與背景色
            byte targetPixelValue = isYin ? (byte)255 : (byte)0;
            byte backgroundPixelValue = isYin ? (byte)0 : (byte)255;

            unsafe
            {
                byte* ptr = (byte*)sourceimg.DataPointer;
                int step = (int)sourceimg.Step;

                int startY = Math.Min(sourceimg.Height - 6, sourceimg.Height - 1);  // 從底部往上掃，預留下緣6px不處理
                int endY = Math.Max(5, 0);// 預留上緣5px不處理
                int startX = Math.Max(5, 0);
                int endX = Math.Min(sourceimg.Width - 6, sourceimg.Width - 1);

                for (int y = startY; y >= endY; y--)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);// 計算像素位址：第 y 列第 x 欄
                        if (*currentPixel == targetPixelValue) // 若符合指定像素值
                        {
                            if (y > source_maxY)// 記錄目前找到最下面的點
                            {
                                source_maxY = y;
                                source_maxY_atX = x;
                            }
                        }
                    }
                    if (source_maxY != int.MinValue) break;// 一找到就跳出 y 迴圈（只找最下面一個點）
                }

                if (source_maxY == int.MinValue)
                    return sourceimg.Clone();// 若沒找到，回傳原圖（代表找不到符合的像素）
            }

            unsafe
            {
                byte* ptr = (byte*)template.DataPointer;
                int step = (int)template.Step;
                int x = Math.Min(Math.Max(source_maxY_atX, 0), template.Width - 1); // 對應 source 中找到的 X，避免越界
                for (int y = template.Height - 1; y >= 0; y--)// 從 template 底部往上掃
                {
                    byte* currentPixel = ptr + (y * step + x);
                    if (*currentPixel == targetPixelValue)
                    {
                        template_maxY = y;// 找到 template 中對應點的 Y
                        break;
                    }
                }
                if (template_maxY == int.MinValue)
                    return sourceimg.Clone();
            }
            delta_y = template_maxY - source_maxY;
            // 若沒找到，也回傳原圖
            if (delta_y == 0)
                return sourceimg.Clone();

            //複製一張圖為output，補償後將白點依序貼在這張圖上
            int width = sourceimg.Width;
            int height = sourceimg.Height;
            Mat outputImage = new Mat(height, width, sourceimg.Depth, sourceimg.NumberOfChannels);
            outputImage.SetTo(new MCvScalar(backgroundPixelValue)); // 填入背景色
            unsafe
            {
                byte* src_ptr = (byte*)sourceimg.DataPointer;
                byte* dst_ptr = (byte*)outputImage.DataPointer;
                int srcStep = (int)sourceimg.Step;
                int dstStep = (int)outputImage.Step;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = y * srcStep + x;
                        if (src_ptr[srcIdx] == targetPixelValue)
                        {
                            int newY = (y > height / 2) ? y + delta_y : y;  //如果白點是在影像下半部就會補償，上半部就不補償
                            if (newY >= 0 && newY < height)
                            {
                                int dstIdx = newY * dstStep + x;
                                dst_ptr[dstIdx] = targetPixelValue;
                            }
                        }
                    }
                }
            }
            return outputImage;
        }
        /// <summary>
        /// 陰版雜點濾除
        /// </summary>
        public static Mat Yin_filternoise(Mat source, string fontsize)
        {
            Dictionary<string, int> fontSizeToThreshold = new Dictionary<string, int>
            {
                { "3pt", 30 },
                { "4pt", 40 },
                { "5pt", 50 },
                { "6pt", 60 },
                { "7pt", 40 },
                { "8pt", 50 },
                { "9pt", 60 },
                { "10pt", 70 },
                { "11pt", 100 },
                { "12pt", 130 }
            };
            int area_threshold;
            fontSizeToThreshold.TryGetValue(fontsize, out area_threshold);

            Mat processimg = source.Clone();
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(processimg, processimg, 150, 255, ThresholdType.Binary);

            using (Mat hierarchy = new Mat())
            {
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                /*  
                    RetrType 是 FindContours 的參數之一，用於定義如何檢測輪廓的層次結構：
                    RetrType.External：僅檢測外層輪廓（默認）。
                    RetrType.List：檢測所有輪廓，但不建立層次結構。
                    RetrType.Ccomp：建立雙層層次結構（外輪廓和內輪廓）。
                    RetrType.Tree：建立完整的層次結構，包括嵌套的內外輪廓
                    */

                CvInvoke.FindContours(processimg, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);

                for (int i = 0; i < contours.Size; i++)
                {
                    double area = CvInvoke.ContourArea(contours[i]);
                    if (area < area_threshold)
                    {
                        CvInvoke.DrawContours(source, contours, i, new MCvScalar(0, 0, 0), -1);
                    }
                }
            }
            return source;
        }
        /// <summary>
        /// 陰版單一白點濾除
        /// </summary>
        public static Mat RemoveIsolatedWhitePixels(Mat source)
        {
            VectorOfPoint points = new VectorOfPoint();
            CvInvoke.FindNonZero(source, points);
            Point[] whitePixels = points.ToArray();
            unsafe
            {
                byte* ptr = (byte*)source.DataPointer;
                foreach (Point p in whitePixels)
                {
                    byte* current_ptr = ptr + (p.Y * source.Step + p.X);
                    if (IsIsolatedPixel(ptr, source.Step, source.Width, source.Height, p))
                    {
                        *current_ptr = 0;
                    }
                }
            }
            return source;
        }
        /// <summary>
        /// 陰版修邊+初步計算塞版
        /// </summary>
        public static (Mat, int) Yin_Calc_OutsideBlock_ResizeFontImg(Mat sourceimg, string fontsize, int templatewidth)
        {
            Dictionary<string, double> fontSizeToThreshold = new Dictionary<string, double>
            {
                { "3pt",  0.05 },
                { "4pt",  0.05 },
                { "5pt",  0.05 },
                { "6pt",  0.05 },
                { "7pt",  0.05 },
                { "8pt",  0.05 },
                { "9pt",  0.05 },
                { "10pt", 0.05 },
                { "11pt", 0.05 },
                { "12pt", 0.05 }
            };
            double threshold;
            fontSizeToThreshold.TryGetValue(fontsize, out threshold);
            //計算模板字體總pixels
            int defectpixels = 0, whitepixels = 0;
            int leftx = 0, rightx = sourceimg.Width - 1;
            bool is_right_find = false;
            //Debug.WriteLine($"影像:字體={fontsize} 寬={sourceimg.Width}, 高={sourceimg.Height} 模版：寬={templatewidth}");
            unsafe
            {
                byte* ptr = (byte*)sourceimg.DataPointer;
                // 從右往左
                for (int x = sourceimg.Width - 1; x >= 0; x--)
                {
                    defectpixels += whitepixels;
                    whitepixels = 0; // 每列的白色像素計數歸零
                    if (is_right_find) // 退出外層迴圈
                    {
                        break;
                    }

                    for (int y = 0; y < sourceimg.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * sourceimg.Width + x);
                        if (*currentPixel == 255)  // 白色像素
                        {
                            whitepixels++;
                        }
                        if (whitepixels > sourceimg.Height * threshold) // 達到門檻值即確定右邊界
                        {
                            is_right_find = true;
                            rightx = x + 1;   //找到後往右邊補1pixel
                            break;
                        }
                    }
                    //Debug.WriteLine($"x = {x}, 白色像素數量 = {whitepixels}");
                }

                //從左到右
                leftx = Math.Max(leftx, rightx - templatewidth);    //確保不超出邊界
                for (int x = 0; x < rightx - templatewidth; x++)
                {
                    for (int y = 0; y < sourceimg.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * sourceimg.Width + x);
                        if (*currentPixel == 255)  // 白色像素
                        {
                            //whitepixels++;
                            defectpixels++;
                        }
                    }
                    //Debug.WriteLine($"x = {x}, 白色像素數量 = {whitepixels}");
                }
                ptr = (byte*)sourceimg.DataPointer;
            }
            //Debug.WriteLine($"陰版字體{fontsize}塞版數為{blockpixels}");
            int yinCropWidth = rightx - leftx;
            if (sourceimg == null || sourceimg.IsEmpty || leftx < 0 || rightx > sourceimg.Width || yinCropWidth <= 0)
            {
                Debug.WriteLine($"[YinCrop][{fontsize}] ROI 無效，回傳原圖 Clone");
                return (sourceimg == null ? new Mat() : sourceimg.Clone(), defectpixels);
            }

            Mat resultimg = new Mat(sourceimg, new Rectangle(leftx, 0, yinCropWidth, sourceimg.Height)).Clone();
            if (resultimg.IsEmpty)
            {
                resultimg.Dispose();
                Debug.WriteLine($"[YinCrop][{fontsize}] 裁切結果 Empty，回傳原圖 Clone");
                return (sourceimg.Clone(), defectpixels);
            }
            return (resultimg, defectpixels);
        }
        /// <summary>
        /// 陽版修邊+初步計算塞版
        /// 2026-09-05 安全修正版：避免 0 寬 ROI / Empty Mat 造成 Imwrite 當機。
        /// </summary>
        public static (Mat, int) Yang_Calc_OutsideBlock_ResizeFontImg(Mat sourceimg, string fontsize, int templatewidth)
        {
            if (sourceimg == null || sourceimg.IsEmpty || sourceimg.Width <= 0 || sourceimg.Height <= 0)
            {
                Debug.WriteLine($"[YangCrop][{fontsize}] sourceimg 為空");
                return (new Mat(), 0);
            }

            Dictionary<string, double> fontSizeToThreshold = new Dictionary<string, double>
            {
                { "3pt", 0.05 }, { "4pt", 0.05 }, { "5pt", 0.05 }, { "6pt", 0.05 },
                { "7pt", 0.05 }, { "8pt", 0.05 }, { "9pt", 0.05 }, { "10pt", 0.05 },
                { "11pt", 0.05 }, { "12pt", 0.05 }
            };

            double threshold = 0.05;
            if (!fontSizeToThreshold.TryGetValue(fontsize, out threshold))
                threshold = 0.05;

            int blockpixels = 0;
            int imageWidth = sourceimg.Width;
            int imageHeight = sourceimg.Height;
            int leftx = 0;
            bool isLeftFound = false;
            int safeTemplateWidth = Math.Max(1, Math.Min(templatewidth, imageWidth));

            unsafe
            {
                byte* ptr = (byte*)sourceimg.DataPointer;
                int step = sourceimg.Step;

                for (int x = 0; x < imageWidth; x++)
                {
                    int blackpixels = 0;
                    for (int y = 0; y < imageHeight; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0) blackpixels++;
                    }

                    if (blackpixels > imageHeight * threshold)
                    {
                        leftx = x;
                        isLeftFound = true;
                        break;
                    }
                    blockpixels += blackpixels;
                }

                if (!isLeftFound)
                {
                    Debug.WriteLine($"[YangCrop][{fontsize}] 找不到有效 leftx，回傳原圖 Clone");
                    return (sourceimg.Clone(), blockpixels);
                }

                int rightx = Math.Min(imageWidth, leftx + safeTemplateWidth);
                if (rightx - leftx < safeTemplateWidth && imageWidth >= safeTemplateWidth)
                {
                    leftx = Math.Max(0, imageWidth - safeTemplateWidth);
                    rightx = imageWidth;
                }

                for (int x = imageWidth - 1; x >= rightx; x--)
                {
                    for (int y = 0; y < imageHeight; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0) blockpixels++;
                    }
                }

                int cropWidth = rightx - leftx;
                Debug.WriteLine($"[YangCrop][{fontsize}] source={imageWidth}x{imageHeight}, templateW={templatewidth}, left={leftx}, right={rightx}, cropW={cropWidth}, outside={blockpixels}");

                if (leftx < 0 || rightx > imageWidth || rightx <= leftx || cropWidth <= 0)
                {
                    Debug.WriteLine($"[YangCrop][{fontsize}] ROI 無效，回傳原圖 Clone");
                    return (sourceimg.Clone(), blockpixels);
                }

                Mat resultimg = new Mat(sourceimg, new Rectangle(leftx, 0, cropWidth, imageHeight)).Clone();
                if (resultimg.IsEmpty)
                {
                    resultimg.Dispose();
                    Debug.WriteLine($"[YangCrop][{fontsize}] 裁切結果 Empty，回傳原圖 Clone");
                    return (sourceimg.Clone(), blockpixels);
                }

                return (resultimg, blockpixels);
            }
        }
        ///// <summary>
        ///// 使用 Correlation 做字體平移對位
        ///// </summary>
        //public static Mat FontImg_CorrelationAlign(
        //    Mat src,
        //    Mat tpl,
        //    int maxShift,
        //    out double bestScore
        //)
        //{
        //    bestScore = double.MinValue;

        //    // 防呆
        //    if (src.IsEmpty || tpl.IsEmpty)
        //        return src;

        //    if (src.Size != tpl.Size)
        //        return src;

        //    // 確保為 float 計算
        //    Mat srcF = new Mat();
        //    Mat tplF = new Mat();
        //    src.ConvertTo(srcF, DepthType.Cv32F);
        //    tpl.ConvertTo(tplF, DepthType.Cv32F);

        //    int bestDx = 0;
        //    int bestDy = 0;

        //    // 建立平移用的暫存圖
        //    Mat shifted = new Mat(src.Size, src.Depth, src.NumberOfChannels);

        //    for (int dy = -maxShift; dy <= maxShift; dy++)
        //    {
        //        for (int dx = -maxShift; dx <= maxShift; dx++)
        //        {
        //            // 建立平移矩陣
        //            Matrix<double> M = new Matrix<double>(2, 3);
        //            M[0, 0] = 1; M[0, 1] = 0; M[0, 2] = dx;
        //            M[1, 0] = 0; M[1, 1] = 1; M[1, 2] = dy;

        //            // 平移影像
        //            CvInvoke.WarpAffine(
        //                srcF,
        //                shifted,
        //                M,
        //                src.Size,
        //                Inter.Nearest,
        //                Warp.Default,
        //                BorderType.Constant,
        //                new MCvScalar(0)
        //            );

        //            // 計算 correlation（Normalized Cross Correlation）
        //            using (Mat result = new Mat())
        //            {
        //                CvInvoke.MatchTemplate(
        //                    shifted,
        //                    tplF,
        //                    result,
        //                    TemplateMatchingType.CcorrNormed
        //                );

        //                float score = result.GetData().Cast<float>().First();

        //                if (score > bestScore)
        //                {
        //                    bestScore = score;
        //                    bestDx = dx;
        //                    bestDy = dy;
        //                }
        //            }
        //        }
        //    }

        //    // 若 correlation 太差，回退原圖（避免亂對）
        //    const double SCORE_THRESHOLD = 0.3;
        //    if (bestScore < SCORE_THRESHOLD)
        //        return src;

        //    // 套用最佳平移
        //    Matrix<double> bestM = new Matrix<double>(2, 3);
        //    bestM[0, 0] = 1; bestM[0, 1] = 0; bestM[0, 2] = bestDx;
        //    bestM[1, 0] = 0; bestM[1, 1] = 1; bestM[1, 2] = bestDy;

        //    Mat aligned = new Mat(src.Size, src.Depth, src.NumberOfChannels);
        //    CvInvoke.WarpAffine(
        //        src,
        //        aligned,
        //        bestM,
        //        src.Size,
        //        Inter.Nearest,
        //        Warp.Default,
        //        BorderType.Constant,
        //        new MCvScalar(0)
        //    );

        //    return aligned;
        //}

        /// <summary>
        /// 通用：Correlation 粗平移對位（陰/陽都可用）
        /// - isYin: true=陰版, false=陽版
        /// - 自動測試 tpl / NOT(tpl) 兩種極性，取分數較高者
        /// - 補邊顏色依 isYin 自動選擇：陰版 0、陽版 255
        /// - scoreThreshold：分數太低直接回原圖，避免亂對位
        /// </summary>
        public static Mat FontImg_CorrelationAlign_Universal(
            Mat src,
            Mat tpl,
            int maxShift,
            bool isYin,
            out double bestScore,
            double scoreThreshold = 0.30,
            string label = ""    // ← 新增
)
        {
            bestScore = double.MinValue;

            if (src == null || tpl == null || src.IsEmpty || tpl.IsEmpty)
                return src;

            if (src.Size != tpl.Size)
                return src;

            byte borderValue = isYin ? (byte)0 : (byte)255;

            Mat aligned = null;

            try
            {
                // 只使用原始 tpl 進行對位
                aligned = CorrelationAlign_Core(src, tpl, maxShift, borderValue, out bestScore, label);

                // 檢查分數是否達標
                if (bestScore < scoreThreshold)
                {
                    if (aligned != null)
                    {
                        aligned.Dispose();
                        aligned = null;
                    }
                    return src;
                }

                // 回傳對位結果
                Mat ret = aligned;
                aligned = null;
                return ret;
            }
            finally
            {
                if (aligned != null) aligned.Dispose();
            }
        }

        /// <summary>
        /// 核心：在 [-maxShift, +maxShift] 搜尋最佳(dx,dy)並套用平移
        /// - tpl 極性在外層決定（此函式不做 NOT）
        /// - borderValue 用來避免補邊干擾 correlation（陰版 0、陽版 255）
        /// </summary>

        private static Mat CorrelationAlign_Core(
            Mat src,
            Mat tpl,
            int maxShift,
            byte borderValue,
            out double bestScore,
            string label = ""    // ← 新增
)
        {
            bestScore = double.MinValue;

            if (src == null || tpl == null || src.IsEmpty || tpl.IsEmpty)
                return src == null ? null : src.Clone();

            if (src.Size != tpl.Size)
                return src.Clone();

            Mat srcF = null;
            Mat tplF = null;
            Mat shifted = null;

            int bestDx = 0, bestDy = 0;

            try
            {
                srcF = new Mat();
                tplF = new Mat();
                src.ConvertTo(srcF, DepthType.Cv32F);
                tpl.ConvertTo(tplF, DepthType.Cv32F);

                shifted = new Mat(srcF.Size, srcF.Depth, srcF.NumberOfChannels);

                for (int dy = -maxShift; dy <= maxShift; dy++)
                {
                    for (int dx = -maxShift; dx <= maxShift; dx++)
                    {
                        Matrix<double> M = null;
                        Mat result = null;

                        try
                        {
                            M = new Matrix<double>(2, 3);
                            M[0, 0] = 1; M[0, 1] = 0; M[0, 2] = dx;
                            M[1, 0] = 0; M[1, 1] = 1; M[1, 2] = dy;

                            CvInvoke.WarpAffine(
                                srcF,
                                shifted,
                                M,
                                srcF.Size,
                                Inter.Nearest,
                                Warp.Default,
                                BorderType.Constant,
                                new MCvScalar(borderValue)
                            );

                            result = new Mat();
                            CvInvoke.MatchTemplate(shifted, tplF, result, TemplateMatchingType.CcorrNormed);

                            double minVal = 0, maxVal = 0;
                            Point minLoc = default(Point), maxLoc = default(Point);
                            CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                            if (maxVal > bestScore)
                            {
                                bestScore = maxVal;
                                bestDx = dx;
                                bestDy = dy;
                            }
                        }
                        finally
                        {
                            if (result != null) result.Dispose();
                            if (M != null) M.Dispose();
                        }
                    }
                }
                // 終端打硬確認各字級的最佳平移(Dx & Dy)與分數
                Debug.WriteLine($"[Font Align] {label} BestShift: (dx:{bestDx}, dy:{bestDy}), Score:{bestScore:F4}");
                //Debug.WriteLine($"[Font Align] BestShift: (dx:{bestDx}, dy:{bestDy}), Score:{bestScore:F4}");

                // 套用最佳平移到原始 src（保持二值）
                Matrix<double> bestM = null;
                try
                {
                    bestM = new Matrix<double>(2, 3);
                    bestM[0, 0] = 1; bestM[0, 1] = 0; bestM[0, 2] = bestDx;
                    bestM[1, 0] = 0; bestM[1, 1] = 1; bestM[1, 2] = bestDy;

                    Mat aligned = new Mat(src.Size, src.Depth, src.NumberOfChannels);
                    CvInvoke.WarpAffine(
                        src,
                        aligned,
                        bestM,
                        src.Size,
                        Inter.Nearest,
                        Warp.Default,
                        BorderType.Constant,
                        new MCvScalar(borderValue)
                    );
                    return aligned;
                }
                finally
                {
                    if (bestM != null) bestM.Dispose();
                }
            }
            finally
            {
                if (shifted != null) shifted.Dispose();
                if (srcF != null) srcF.Dispose();
                if (tplF != null) tplF.Dispose();
            }
        }

        /// <summary>
        /// 用補邊方式對齊尺寸,不破壞原始筆畫
        /// </summary>
        private static Mat AlignSizePadding(Mat src, Size targetSize, bool isYin)
        {
            // 如果尺寸已經一致,直接返回複製
            if (src.Size == targetSize)
                return src.Clone();

            byte bgColor = isYin ? (byte)0 : (byte)255;

            Mat result = new Mat(targetSize, src.Depth, src.NumberOfChannels);
            result.SetTo(new MCvScalar(bgColor));

            int srcW = src.Width;
            int srcH = src.Height;
            int dstW = targetSize.Width;
            int dstH = targetSize.Height;

            // 情況1: 待測物比模板大 → 居中裁切
            if (srcW >= dstW && srcH >= dstH)
            {
                int cropX = (srcW - dstW) / 2;
                int cropY = (srcH - dstH) / 2;

                Rectangle srcROI = new Rectangle(cropX, cropY, dstW, dstH);
                using (Mat cropped = new Mat(src, srcROI))
                {
                    cropped.CopyTo(result);
                }
            }
            // 情況2: 待測物比模板小 → 居中補邊
            else if (srcW <= dstW && srcH <= dstH)
            {
                int padX = (dstW - srcW) / 2;
                int padY = (dstH - srcH) / 2;

                Rectangle dstROI = new Rectangle(padX, padY, srcW, srcH);
                using (Mat dstCrop = new Mat(result, dstROI))
                {
                    src.CopyTo(dstCrop);
                }
            }
            // 情況3: 混合 (一邊大一邊小) → 分別處理
            else
            {
                int copyW = Math.Min(srcW, dstW);
                int copyH = Math.Min(srcH, dstH);

                int srcX = srcW > dstW ? (srcW - dstW) / 2 : 0;
                int srcY = srcH > dstH ? (srcH - dstH) / 2 : 0;
                int dstX = srcW < dstW ? (dstW - srcW) / 2 : 0;
                int dstY = srcH < dstH ? (dstH - srcH) / 2 : 0;

                Rectangle srcROI = new Rectangle(srcX, srcY, copyW, copyH);
                Rectangle dstROI = new Rectangle(dstX, dstY, copyW, copyH);

                using (Mat srcCrop = new Mat(src, srcROI))
                using (Mat dstCrop = new Mat(result, dstROI))
                {
                    srcCrop.CopyTo(dstCrop);
                }
            }

            return result;
        }




        /// <summary>
        /// 陰版影像對位
        /// </summary>
        //public static List<Mat> Yin_Resize(List<FontInfo> yin_fontInfos, List<Mat> Yin_Image_List, List<TemplateData> YinTemplates)
        //{
        //    for (int i = 0; i < yin_fontInfos.Count; i++)
        //    {
        //        // 檢查條件，跳過不符合的情況
        //        if (yin_fontInfos[i].Bounds.IsEmpty || Yin_Image_List[i].IsEmpty)
        //        {
        //            continue;
        //        }
        //        // 尺寸不一致時進行 Resize
        //        if (Yin_Image_List[i].Size != YinTemplates[i].Image.Size)
        //        {
        //            CvInvoke.Resize(Yin_Image_List[i], Yin_Image_List[i], YinTemplates[i].Image.Size, 0, 0, Inter.Nearest);    //使用最近鄰插值方式
        //        }
        //        //字體對位
        //        //Yin_Image_List[i] = FontImg_Correction(Yin_Image_List[i], YinTemplates[i].Image, true);
        //        double corrScore;
        //        Yin_Image_List[i] = FontImg_CorrelationAlign(Yin_Image_List[i], YinTemplates[i].Image, maxShift: 8, out corrScore); //Correlation 粗對位
        //        Yin_Image_List[i] = FontImg_Correction(Yin_Image_List[i], YinTemplates[i].Image, true);
        //        CvInvoke.Imwrite($"Yin_Correction_{yin_fontInfos[i].Fontname}.bmp", Yin_Image_List[i]);
        //    }
        //    return Yin_Image_List;
        //}
        /// <summary>
        /// 陰版影像對位（Correlation 粗對位）
        /// </summary>By Johnson

        //public static List<Mat> Yin_Resize(
        //    List<FontInfo> yin_fontInfos,
        //    List<Mat> Yin_Image_List,
        //    List<TemplateData> YinTemplates
        //)
        //{
        //    for (int i = 0; i < yin_fontInfos.Count; i++)
        //    {
        //        // 跳過無效資料
        //        if (yin_fontInfos[i].Bounds.IsEmpty || Yin_Image_List[i].IsEmpty)
        //            continue;

        //        // 尺寸不一致先 Resize（二值圖用 Nearest）
        //        if (Yin_Image_List[i].Size != YinTemplates[i].Image.Size)
        //        {
        //            CvInvoke.Resize(
        //                Yin_Image_List[i],
        //                Yin_Image_List[i],
        //                YinTemplates[i].Image.Size,
        //                0, 0,
        //                Inter.Nearest
        //            );
        //        }

        //        // ✅ Correlation 粗對位（陰版 isYin=true）
        //        double corrScore;
        //        Yin_Image_List[i] = OCT_AlgorithmHelper.FontImg_CorrelationAlign_Universal(
        //            Yin_Image_List[i],
        //            YinTemplates[i].Image,
        //            maxShift: 8,
        //            isYin: true,
        //            out corrScore,
        //            scoreThreshold: 0.30
        //        );

        //        //// ✅ 原本的 Y 微調（陰版 isYin=true）
        //        //Yin_Image_List[i] = OCT_AlgorithmHelper.FontImg_Correction(
        //        //    Yin_Image_List[i],
        //        //    YinTemplates[i].Image,
        //        //    true
        //        //);

        //        // 把對位校正後的影像存起來以供檢查及後續疊圖
        //        CvInvoke.Imwrite($"Yin_Correction_{yin_fontInfos[i].Fontname}.bmp", Yin_Image_List[i]);
        //    }

        //    return Yin_Image_List;
        //}

        /// <summary>
        /// 陰版影像對位（Parallel 並行版）
        /// </summary>
        //public static List<Mat> Yin_Resize(
        //    List<FontInfo> yin_fontInfos,
        //    List<Mat> Yin_Image_List,
        //    List<TemplateData> YinTemplates
        //)
        //{
        //    // 強制 Clone，確保每個 Mat 記憶體獨立，避免多執行緒共用記憶體衝突
        //    Mat[] resultArray = Yin_Image_List.Select(m => m.Clone()).ToArray();
        //    Mat[] templateCopies = YinTemplates.Select(t => t.Image.Clone()).ToArray();

        //    Parallel.For(0, yin_fontInfos.Count, new ParallelOptions
        //    {
        //        MaxDegreeOfParallelism = 2  // 先保守用2，確認穩定後再調高
        //    },
        //    i =>
        //    {
        //        try
        //        {
        //            // 跳過無效資料
        //            if (yin_fontInfos[i].Bounds.IsEmpty || resultArray[i].IsEmpty)
        //                return;

        //            // 尺寸不一致先 Resize（二值圖用 Nearest）
        //            if (resultArray[i].Size != templateCopies[i].Size)
        //            {
        //                Mat resized = new Mat();
        //                CvInvoke.Resize(
        //                    resultArray[i],
        //                    resized,
        //                    templateCopies[i].Size,
        //                    0, 0,
        //                    Inter.Nearest
        //                );
        //                resultArray[i].Dispose();
        //                resultArray[i] = resized;
        //            }

        //            // Correlation 粗對位（陰版 isYin=true）
        //            double corrScore;
        //            Mat aligned = FontImg_CorrelationAlign_Universal(
        //                resultArray[i],
        //                templateCopies[i],
        //                maxShift: 8,
        //                isYin: true,
        //                out corrScore,
        //                scoreThreshold: 0.30
        //            );

        //            // 若對位結果與原圖不同，釋放原圖
        //            if (!ReferenceEquals(aligned, resultArray[i]))
        //                resultArray[i].Dispose();

        //            resultArray[i] = aligned;

        //            CvInvoke.Imwrite($"Yin_Correction_{yin_fontInfos[i].Fontname}.bmp", resultArray[i]);
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"[Yin_Resize 錯誤] 字級 {yin_fontInfos[i].Fontname}: {ex.GetType().Name}");
        //            Debug.WriteLine($"[Yin_Resize 錯誤] 訊息: {ex.Message}");
        //            Debug.WriteLine($"[Yin_Resize 錯誤] StackTrace: {ex.StackTrace}");
        //        }
        //    });

        //    // 釋放複製的 template
        //    foreach (var t in templateCopies) t.Dispose();

        //    return resultArray.ToList();
        //}

        /// <summary>
        /// 陰版影像對位（Multithreading 並行版 + 環境變數控制執行緒）
        /// </summary>
        public static List<Mat> Yin_Resize(
            List<FontInfo> yin_fontInfos,
            List<Mat> Yin_Image_List,
            List<TemplateData> YinTemplates
        )
        {
            Mat[] resultArray = Yin_Image_List.Select(m => m.Clone()).ToArray();
            Mat[] templateCopies = YinTemplates.Select(t => t.Image.Clone()).ToArray();

            int outerParallel = 2;

            // 用環境變數限制 OpenCV 內部執行緒數
            int innerThreads = Math.Max(1, Environment.ProcessorCount / outerParallel);
            string originalThreadEnv = Environment.GetEnvironmentVariable("OMP_NUM_THREADS") ?? "";
            Environment.SetEnvironmentVariable("OMP_NUM_THREADS", innerThreads.ToString());

            Parallel.For(0, yin_fontInfos.Count, new ParallelOptions
            {
                MaxDegreeOfParallelism = outerParallel
            },
            i =>
            {
                try
                {
                    if (yin_fontInfos[i].Bounds.IsEmpty || resultArray[i].IsEmpty)
                        return;

                    if (resultArray[i].Size != templateCopies[i].Size)
                    {
                        Mat resized = new Mat();
                        CvInvoke.Resize(
                            resultArray[i],
                            resized,
                            templateCopies[i].Size,
                            0, 0,
                            Inter.Nearest
                        );
                        resultArray[i].Dispose();
                        resultArray[i] = resized;
                    }

                    double corrScore;
                    Mat aligned = FontImg_CorrelationAlign_Universal(
                        resultArray[i],
                        templateCopies[i],
                        maxShift: 5,
                        isYin: true,
                        out corrScore,
                        scoreThreshold: 0.30,
                        label: $"Yin_{yin_fontInfos[i].Fontname}"    // ← 新增
                    );

                    if (!ReferenceEquals(aligned, resultArray[i]))
                        resultArray[i].Dispose();

                    resultArray[i] = aligned;

                    CvInvoke.Imwrite($"Yin_Correction_{yin_fontInfos[i].Fontname}.bmp", resultArray[i]);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Yin_Resize 錯誤] {yin_fontInfos[i].Fontname}: {ex.GetType().Name}");
                    Debug.WriteLine($"[Yin_Resize 錯誤] 訊息: {ex.Message}");
                    Debug.WriteLine($"[Yin_Resize 錯誤] StackTrace: {ex.StackTrace}");
                }
            });

            // 還原環境變數
            Environment.SetEnvironmentVariable("OMP_NUM_THREADS", originalThreadEnv);

            foreach (var t in templateCopies) t.Dispose();
            return resultArray.ToList();
        }

        /// <summary>
        /// 陽版影像對位
        /// </summary>
        //public static List<Mat> Yang_Resize(List<FontInfo> yang_fontInfos, List<Mat> Yang_Image_List, List<TemplateData> YangTemplates)
        //{
        //    for (int i = 0; i < yang_fontInfos.Count; i++)
        //    {
        //        // 檢查條件，跳過不符合的情況
        //        if (yang_fontInfos[i].Bounds.IsEmpty || Yang_Image_List[i].IsEmpty)
        //        {
        //            continue;
        //        }
        //        // 尺寸不一致時進行 Resize
        //        if (Yang_Image_List[i].Size != YangTemplates[i].Image.Size)
        //        {
        //            CvInvoke.Resize(Yang_Image_List[i], Yang_Image_List[i], YangTemplates[i].Image.Size, 0, 0, Inter.Nearest);    //使用最近鄰插值方式

        //        }
        //        //字體對位
        //        Yang_Image_List[i] = FontImg_Correction(Yang_Image_List[i], YangTemplates[i].Image, false);
        //        CvInvoke.Imwrite($"Yang_Correction_{yang_fontInfos[i].Fontname}.bmp", Yang_Image_List[i]);
        //    }
        //    return Yang_Image_List;
        //}

        /// <summary>
        /// 陽版影像對位（Correlation 粗對位）By Johnson
        /// </summary>
        //public static List<Mat> Yang_Resize(
        //    List<FontInfo> yang_fontInfos,
        //    List<Mat> Yang_Image_List,
        //    List<TemplateData> YangTemplates
        //)
        //{
        //    for (int i = 0; i < yang_fontInfos.Count; i++)
        //    {
        //        // 跳過無效資料
        //        if (yang_fontInfos[i].Bounds.IsEmpty || Yang_Image_List[i].IsEmpty)
        //            continue;

        //        // 尺寸不一致先 Resize（二值圖用 Nearest）
        //        if (Yang_Image_List[i].Size != YangTemplates[i].Image.Size)
        //        {
        //            CvInvoke.Resize(
        //                Yang_Image_List[i],
        //                Yang_Image_List[i],
        //                YangTemplates[i].Image.Size,
        //                0, 0,
        //                Inter.Nearest
        //            );
        //        }

        //        // ✅ Correlation 粗對位（陽版 isYin=false）
        //        double corrScore;
        //        Yang_Image_List[i] = OCT_AlgorithmHelper.FontImg_CorrelationAlign_Universal(
        //            Yang_Image_List[i],
        //            YangTemplates[i].Image,
        //            maxShift: 8,
        //            isYin: false,
        //            out corrScore,
        //            scoreThreshold: 0.30
        //        );

        //        // ✅ 原本的 Y 微調（陽版 isYin=false）
        //        //Yang_Image_List[i] = OCT_AlgorithmHelper.FontImg_Correction(
        //        //    Yang_Image_List[i],
        //        //    YangTemplates[i].Image,
        //        //    false
        //        //);

        //        CvInvoke.Imwrite($"Yang_Correction_{yang_fontInfos[i].Fontname}.bmp", Yang_Image_List[i]);
        //    }

        //    return Yang_Image_List;
        //}

        /// <summary>
        /// 陽版影像對位（Parallel 並行版）
        /// </summary>
        //public static List<Mat> Yang_Resize(
        //    List<FontInfo> yang_fontInfos,
        //    List<Mat> Yang_Image_List,
        //    List<TemplateData> YangTemplates
        //)
        //{
        //    // 強制 Clone，確保每個 Mat 記憶體獨立，避免多執行緒共用記憶體衝突
        //    Mat[] resultArray = Yang_Image_List.Select(m => m.Clone()).ToArray();
        //    Mat[] templateCopies = YangTemplates.Select(t => t.Image.Clone()).ToArray();

        //    Parallel.For(0, yang_fontInfos.Count, new ParallelOptions
        //    {
        //        MaxDegreeOfParallelism = 2  // 先保守用2，確認穩定後再調高
        //    },
        //    i =>
        //    {
        //        try
        //        {
        //            // 跳過無效資料
        //            if (yang_fontInfos[i].Bounds.IsEmpty || resultArray[i].IsEmpty)
        //                return;

        //            // 尺寸不一致先 Resize（二值圖用 Nearest）
        //            if (resultArray[i].Size != templateCopies[i].Size)
        //            {
        //                Mat resized = new Mat();
        //                CvInvoke.Resize(
        //                    resultArray[i],
        //                    resized,
        //                    templateCopies[i].Size,
        //                    0, 0,
        //                    Inter.Nearest
        //                );
        //                resultArray[i].Dispose();
        //                resultArray[i] = resized;
        //            }

        //            // Correlation 粗對位（陽版 isYin=false）
        //            double corrScore;
        //            Mat aligned = FontImg_CorrelationAlign_Universal(
        //                resultArray[i],
        //                templateCopies[i],
        //                maxShift: 8,
        //                isYin: false,
        //                out corrScore,
        //                scoreThreshold: 0.30
        //            );

        //            // 若對位結果與原圖不同，釋放原圖
        //            if (!ReferenceEquals(aligned, resultArray[i]))
        //                resultArray[i].Dispose();

        //            resultArray[i] = aligned;

        //            CvInvoke.Imwrite($"Yang_Correction_{yang_fontInfos[i].Fontname}.bmp", resultArray[i]);
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"[Yang_Resize 錯誤] 字級 {yang_fontInfos[i].Fontname}: {ex.GetType().Name}");
        //            Debug.WriteLine($"[Yang_Resize 錯誤] 訊息: {ex.Message}");
        //            Debug.WriteLine($"[Yang_Resize 錯誤] StackTrace: {ex.StackTrace}");
        //        }
        //    });

        //    // 釋放複製的 template
        //    foreach (var t in templateCopies) t.Dispose();

        //    return resultArray.ToList();
        //}

        /// <summary>
        /// 陽版影像對位（Multithreading 並行版 + 環境變數控制執行緒）
        /// </summary>
        public static List<Mat> Yang_Resize(
            List<FontInfo> yang_fontInfos,
            List<Mat> Yang_Image_List,
            List<TemplateData> YangTemplates
        )
        {
            Mat[] resultArray = Yang_Image_List.Select(m => m.Clone()).ToArray();
            Mat[] templateCopies = YangTemplates.Select(t => t.Image.Clone()).ToArray();

            int outerParallel = 2;

            // 用環境變數限制 OpenCV 內部執行緒數
            int innerThreads = Math.Max(1, Environment.ProcessorCount / outerParallel);
            string originalThreadEnv = Environment.GetEnvironmentVariable("OMP_NUM_THREADS") ?? "";
            Environment.SetEnvironmentVariable("OMP_NUM_THREADS", innerThreads.ToString());

            Parallel.For(0, yang_fontInfos.Count, new ParallelOptions
            {
                MaxDegreeOfParallelism = outerParallel
            },
            i =>
            {
                try
                {
                    if (yang_fontInfos[i].Bounds.IsEmpty || resultArray[i].IsEmpty)
                        return;

                    if (resultArray[i].Size != templateCopies[i].Size)
                    {
                        Mat resized = new Mat();
                        CvInvoke.Resize(
                            resultArray[i],
                            resized,
                            templateCopies[i].Size,
                            0, 0,
                            Inter.Nearest
                        );
                        resultArray[i].Dispose();
                        resultArray[i] = resized;
                    }

                    double corrScore;
                    Mat aligned = FontImg_CorrelationAlign_Universal(
                        resultArray[i],
                        templateCopies[i],
                        maxShift: 5,
                        isYin: false,
                        out corrScore,
                        scoreThreshold: 0.60,
                        label: $"Yang_{yang_fontInfos[i].Fontname}"    // ← 新增
                    );

                    if (!ReferenceEquals(aligned, resultArray[i]))
                        resultArray[i].Dispose();

                    resultArray[i] = aligned;

                    CvInvoke.Imwrite($"Yang_Correction_{yang_fontInfos[i].Fontname}.bmp", resultArray[i]);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Yang_Resize 錯誤] {yang_fontInfos[i].Fontname}: {ex.GetType().Name}");
                    Debug.WriteLine($"[Yang_Resize 錯誤] 訊息: {ex.Message}");
                    Debug.WriteLine($"[Yang_Resize 錯誤] StackTrace: {ex.StackTrace}");
                }
            });

            // 還原環境變數
            Environment.SetEnvironmentVariable("OMP_NUM_THREADS", originalThreadEnv);

            foreach (var t in templateCopies) t.Dispose();
            return resultArray.ToList();
        }

        #endregion

        #region 陰版計算
        /// <summary>
        /// 陰版所有字體雜訊、雜點濾除、切割並初步計算缺燙
        /// </summary>
        public static (Font_Imgs, List<Mat>, List<FontInfo>)
            PrepareYinFontCrops(List<List<Rectangle>> yin_lists, Mat yinimage, List<TemplateData> YinTemplates, OCT_Parameters_CardType current_params, OCT_Parameters_SaveOptions saveoptions, string cardType)

        {
            string[] fontNames = { "3pt", "4pt", "5pt", "6pt", "7pt", "8pt", "9pt", "10pt", "11pt", "12pt" };
            List<Mat> Yin_ProcessImage_List = new List<Mat>();
            List<FontInfo> yin_fontInfos = new List<FontInfo>();
            Font_Imgs yin_Imgs = new Font_Imgs();
            for (int i = 0; i < yin_lists.Count; i++) // 「一個字級一個字級」處理(3pt整包、4pt整包，以此類推...)
            {
                var yin_list = yin_lists[i];
                string fontName = fontNames[i];
                int defect_outsidepixels = 0;
                // 計算總範圍矩形
                Rectangle font_boundingRect = OCT_AlgorithmHelper.CalculateBoundingRectangle(yin_list, yinimage.Height);// 把這個字級裡「所有小連通區」，合併成 一個最大外接矩形
                if (font_boundingRect.IsEmpty) // 若該字級真的不存在或因前處理/二值化波動而漏抓，塞一張全白圖(「流程穩定性優先」設計)
                {
                    //創建一張空的影像加入
                    Size safeSize = (YinTemplates != null && i < YinTemplates.Count && YinTemplates[i]?.Image != null && !YinTemplates[i].Image.IsEmpty)
                        ? YinTemplates[i].Image.Size
                        : yinimage.Size;
                    Mat emptyImage = new Mat(safeSize, DepthType.Cv8U, 1);
                    emptyImage.SetTo(new MCvScalar(0)); // 陰版背景為黑色
                    Yin_ProcessImage_List.Add(emptyImage);

                    // 如果未找到，記錄未找到的狀態
                    yin_fontInfos.Add(new FontInfo
                    {
                        Fontname = fontName,
                        Bounds = Rectangle.Empty,
                        Defect_Pixels = 0
                    });
                    //Debug.WriteLine($"陽版字體 {fontName}: 未找到有效的矩形，略過處理。");
                }
                else
                {
                    // 如果找到矩形，處理圖像
                    Mat yin_image = new Mat(yinimage, font_boundingRect);
                    double current_threshold = 0.0;
                    if (saveoptions.saveoption.Yin_Original)
                    {
                        yin_image.Save($"Yin_{fontName}.bmp");
                    }
                    switch (fontName)
                    {
                        case "3pt":
                            yin_Imgs._3pt = yin_image;
                            current_threshold = current_params.yin_parameter._3pt;
                            break;
                        case "4pt":
                            yin_Imgs._4pt = yin_image;
                            current_threshold = current_params.yin_parameter._4pt;
                            break;
                        case "5pt":
                            yin_Imgs._5pt = yin_image;
                            current_threshold = current_params.yin_parameter._5pt;
                            break;
                        case "6pt":
                            yin_Imgs._6pt = yin_image;
                            current_threshold = current_params.yin_parameter._6pt;
                            break;
                        case "7pt":
                            yin_Imgs._7pt = yin_image;
                            current_threshold = current_params.yin_parameter._7pt;
                            break;
                        case "8pt":
                            yin_Imgs._8pt = yin_image;
                            current_threshold = current_params.yin_parameter._8pt;
                            break;
                        case "9pt":
                            yin_Imgs._9pt = yin_image;
                            current_threshold = current_params.yin_parameter._9pt;
                            break;
                        case "10pt":
                            yin_Imgs._10pt = yin_image;
                            current_threshold = current_params.yin_parameter._10pt;
                            break;
                        case "11pt":
                            yin_Imgs._11pt = yin_image;
                            current_threshold = current_params.yin_parameter._11pt;
                            break;
                        case "12pt":
                            yin_Imgs._12pt = yin_image;
                            current_threshold = current_params.yin_parameter._12pt;
                            break;
                    }   //保存原始裁切圖像，以便後續廠商在ui上可以點擊修改單一圖片重分析


                    Mat filter_yin_image = new Mat();
                    if (saveoptions.saveoption.MakeTemplates)
                    {
                        CvInvoke.CvtColor(yin_image, filter_yin_image, ColorConversion.Bgr2Gray);
                        CvInvoke.Threshold(filter_yin_image, filter_yin_image, 110, 255, ThresholdType.Binary);
                    }
                    else
                    {
                        filter_yin_image = Yin_filternoise(yin_image.Clone(), fontName);
                        if (saveoptions.saveoption.Yin_FilterNoise)
                        {
                            filter_yin_image.Save($"Yin_filternoise_{fontName}.bmp");
                        }
                        CvInvoke.CvtColor(filter_yin_image, filter_yin_image, ColorConversion.Bgr2Gray);
                        CvInvoke.Threshold(filter_yin_image, filter_yin_image, current_threshold, 255, ThresholdType.Binary); //給廠商調的參數
                    }

                    if (saveoptions.saveoption.Yin_Threshold)
                    {
                        filter_yin_image.Save($"Yin_threshold_{fontName}.bmp");
                    }
                    // 去除孤立白點
                    filter_yin_image = RemoveIsolatedWhitePixels(filter_yin_image);
                    (filter_yin_image, defect_outsidepixels) = Yin_Calc_OutsideBlock_ResizeFontImg(filter_yin_image, fontName, YinTemplates[i].Width);// 修剪不該存在的外圍白色（塞版前處理），把字圖調整到與模板寬度一致
                    //Debug.WriteLine($"Yin_outsidepixels{fontName}={defect_outsidepixels}");   //觀看裁切量
                    if (saveoptions.saveoption.Yin_CalcOutsideBlock)
                    {
                        CvInvoke.Imwrite($"Yin_Resizefont_{fontName}.bmp", filter_yin_image);
                    }

                    // 2026.01.04 張植竣新增模板儲存功能

                    if (saveoptions.saveoption.MakeTemplates)
                    {
                        string templatesDir = cardType == "雙銅"
                            ? "template_double sided coated paper"
                            : "template_white card";

                        if (!Directory.Exists(templatesDir))
                            Directory.CreateDirectory(templatesDir);

                        string templatePath = Path.Combine(templatesDir, $"Yin_template_{fontName}.bmp");
                        filter_yin_image.Save(templatePath);
                    }

                    //if (saveoptions.saveoption.MakeTemplates) 
                    //{
                    //    // 確保 templates 資料夾存在
                    //    string templatesDir = "templates";
                    //    if (!Directory.Exists(templatesDir))
                    //    {
                    //        Directory.CreateDirectory(templatesDir);
                    //    }

                    //    // 儲存到 templates 資料夾
                    //    string templatePath = Path.Combine(templatesDir, $"Yin_template_{fontName}.bmp");

                    //    filter_yin_image.Save(templatePath);

                    //    // filter_yin_image.Save($"Yin_template_{fontName}.bmp");
                    //}

                    Yin_ProcessImage_List.Add(filter_yin_image);
                    // 記錄找到的狀態
                    yin_fontInfos.Add(new FontInfo
                    {
                        Fontname = fontName,
                        Bounds = font_boundingRect,
                        Defect_Pixels = defect_outsidepixels
                    });
                }
            }
            return (yin_Imgs, Yin_ProcessImage_List, yin_fontInfos);
        }
        /// <summary>
        /// 陰版計算所有字體
        /// </summary>
        public static (Font_Imgs, Font_Results, Font_Imgs, Font_Results) Yin_Calculate(List<FontInfo> yin_fontInfos, List<Mat> Yin_Image_List, List<TemplateData> YinTemplates) // 回傳塞版 & 缺燙結果圖和數值
        {
            Font_Results yin_block_results = new Font_Results();
            Font_Results yin_defect_results = new Font_Results();
            Font_Imgs yin_block_imgs = new Font_Imgs();
            Font_Imgs yin_defect_imgs = new Font_Imgs();
            double total_defectpixels = 0.0;
            double total_defectpixels_templates = 0.0;

            // 新增塞版總計
            double total_blockpixels = 0.0;
            double total_blockpixels_templates = 0.0;

            for (int i = 0; i < yin_fontInfos.Count; i++)
            {
                Mat yin_defect_img = new Mat();
                Mat yin_block_img = new Mat();
                double defect_percentage;   //缺燙百分比
                double block_percentage;    //塞版百分比
                try
                {
                    if (Yin_Image_List[i].Size != YinTemplates[i].Image.Size)
                    {
                        Mat resizedSafe = new Mat();
                        CvInvoke.Resize(Yin_Image_List[i], resizedSafe, YinTemplates[i].Image.Size, 0, 0, Inter.Nearest);
                        Yin_Image_List[i] = resizedSafe;
                    }
                    (yin_block_img, block_percentage, yin_defect_img, defect_percentage) = Yin_Calc_Single(yin_fontInfos[i], Yin_Image_List[i], YinTemplates[i]);
                    CvInvoke.Imwrite($"Yin_block_{yin_fontInfos[i].Fontname}.bmp", yin_block_img);
                    CvInvoke.Imwrite($"Yin_defect_{yin_fontInfos[i].Fontname}.bmp", yin_defect_img);
                }
                catch (Exception ex)
                {
                    OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, $"陰版字體{yin_fontInfos[i].Fontname}分析失敗", ex.ToString());
                    throw;
                }
                total_defectpixels += yin_fontInfos[i].Defect_Pixels;
                total_defectpixels_templates += YinTemplates[i].Total_DefectPixels;

                // ★ 新增：塞版總百分比分子/分母
                total_blockpixels += yin_fontInfos[i].Block_Pixels;
                total_blockpixels_templates += YinTemplates[i].Total_BlockPixels;

                switch (yin_fontInfos[i].Fontname)
                {
                    case "3pt":
                        yin_block_imgs._3pt = yin_block_img;
                        yin_defect_imgs._3pt = yin_defect_img;
                        yin_block_results._3pt = block_percentage;
                        yin_defect_results._3pt = defect_percentage;
                        break;
                    case "4pt":
                        yin_block_imgs._4pt = yin_block_img;
                        yin_defect_imgs._4pt = yin_defect_img;
                        yin_block_results._4pt = block_percentage;
                        yin_defect_results._4pt = defect_percentage;
                        break;
                    case "5pt":
                        yin_block_imgs._5pt = yin_block_img;
                        yin_defect_imgs._5pt = yin_defect_img;
                        yin_block_results._5pt = block_percentage;
                        yin_defect_results._5pt = defect_percentage;
                        break;
                    case "6pt":
                        yin_block_imgs._6pt = yin_block_img;
                        yin_defect_imgs._6pt = yin_defect_img;
                        yin_block_results._6pt = block_percentage;
                        yin_defect_results._6pt = defect_percentage;
                        break;
                    case "7pt":
                        yin_block_imgs._7pt = yin_block_img;
                        yin_defect_imgs._7pt = yin_defect_img;
                        yin_block_results._7pt = block_percentage;
                        yin_defect_results._7pt = defect_percentage;
                        break;
                    case "8pt":
                        yin_block_imgs._8pt = yin_block_img;
                        yin_defect_imgs._8pt = yin_defect_img;
                        yin_block_results._8pt = block_percentage;
                        yin_defect_results._8pt = defect_percentage;
                        break;
                    case "9pt":
                        yin_block_imgs._9pt = yin_block_img;
                        yin_defect_imgs._9pt = yin_defect_img;
                        yin_block_results._9pt = block_percentage;
                        yin_defect_results._9pt = defect_percentage;
                        break;
                    case "10pt":
                        yin_block_imgs._10pt = yin_block_img;
                        yin_defect_imgs._10pt = yin_defect_img;
                        yin_block_results._10pt = block_percentage;
                        yin_defect_results._10pt = defect_percentage;
                        break;
                    case "11pt":
                        yin_block_imgs._11pt = yin_block_img;
                        yin_defect_imgs._11pt = yin_defect_img;
                        yin_block_results._11pt = block_percentage;
                        yin_defect_results._11pt = defect_percentage;
                        break;
                    case "12pt":
                        yin_block_imgs._12pt = yin_block_img;
                        yin_defect_imgs._12pt = yin_defect_img;
                        yin_block_results._12pt = block_percentage;
                        yin_defect_results._12pt = defect_percentage;
                        break;
                }
            }
            yin_defect_results.Defect_Total_Percentage = total_defectpixels / total_defectpixels_templates * 100; //(陰版)總缺燙百分比
            yin_block_results.Block_Total_Percentage = total_blockpixels / total_blockpixels_templates * 100; //(陰版)總塞版百分比
            return (yin_block_imgs, yin_block_results, yin_defect_imgs, yin_defect_results);
        }
        /// <summary>
        /// 陰版計算單字體
        /// </summary>
        public static (Mat, double, Mat, double) Yin_Calc_Single(FontInfo fontInfo, Mat yin_font_image, TemplateData YinTemplate)
        {
            Mat yin_block_img = new Mat();
            Mat yin_defect_img = new Mat();
            double yin_defect_percentage;
            double yin_block_percentage;
            int defectCompensation = 0;
            int blockCompensation = 0;
            //補償pixel 查表（陰版字體）
            Dictionary<string, (int defect_compensation, int block_compensation)> compensations = new Dictionary<string, (int, int)>
            {
                //缺燙=初步裁切量+實際缺燙量
                { "3pt", (261 + 434, 493) },
                { "4pt", (299 + 296, 295) },
                { "5pt", (164 + 354, 433) },
                { "6pt", (249 + 262, 296) },
                { "7pt", (212 + 641, 578) },
                { "8pt", (181 + 399, 551) },
                { "9pt", (456 + 685, 563) },
                { "10pt",(646 + 457, 592) },
                { "11pt",(453 + 699, 745) },
                { "12pt",(445 + 657, 764) }
            };
            ////補初步計算
            //var compensations = new Dictionary<string, (int defect_compensation, int block_compensation)>
            //{
            //    { "3pt", (261, 0) },
            //    { "4pt", (299, 0) },
            //    { "5pt", (164, 0) },
            //    { "6pt", (249, 0) },
            //    { "7pt", (212, 0) },
            //    { "8pt", (181, 0) },
            //    { "9pt", (456, 0) },
            //    { "10pt", (646, 0) },
            //    { "11pt", (453, 0) },
            //    { "12pt", (445, 0) }
            //};
            // 查詢補償數字
            if (compensations.ContainsKey(fontInfo.Fontname))
            {
                var compensation = compensations[fontInfo.Fontname];
                defectCompensation = compensation.defect_compensation;
                blockCompensation = compensation.block_compensation;
            }
            //測試使用十字kernal侵蝕把偏移誤差除掉
            Mat kernel = new Mat(5, 5, DepthType.Cv8U, 1); // 3x3 大小的結構元素
            kernel.SetTo(new MCvScalar(0)); // 設置結構元素值為1
            kernel.Col(2).SetTo(new MCvScalar(1));  // 中間列設為 1（垂直線）
            kernel.Row(2).SetTo(new MCvScalar(1));  // 中間行設為 1（水平線）
            try
            {
                CvInvoke.Subtract(YinTemplate.Image, yin_font_image, yin_block_img);
                //CvInvoke.Erode(yin_block_img, yin_block_img, kernel, new Point(-1, -1), 1, BorderType.Reflect, new MCvScalar(0));
            }
            catch (Exception ex)
            {
                throw;
            }
            //CvInvoke.Imwrite($"Yin_block_{fontNames[index]}.bmp", yin_block_img);
            int block_pixels = 0;    //塞版總數
            unsafe
            {
                byte* ptr = (byte*)yin_block_img.DataPointer;
                for (int x = 0; x < yin_block_img.Width; x++)
                {
                    for (int y = 0; y < yin_block_img.Height; y++)
                    {
                        byte* current_ptr = ptr + (y * yin_block_img.Width + x);
                        if (*current_ptr == 255)
                        {
                            block_pixels++;
                        }
                    }
                }
                fontInfo.Block_Pixels += block_pixels;
                fontInfo.Block_Pixels = Math.Max(0, fontInfo.Block_Pixels - blockCompensation);    //補償誤差
            }
            yin_block_percentage = (fontInfo.Block_Pixels * 1.0 / YinTemplate.Total_BlockPixels) * 100;

            //缺燙檢測
            Mat inverted_template = new Mat();
            CvInvoke.BitwiseNot(YinTemplate.Image, inverted_template);
            CvInvoke.BitwiseAnd(inverted_template, yin_font_image, yin_defect_img);
            //CvInvoke.Erode(yin_defect_img, yin_defect_img, kernel, new Point(-1, -1), 1, BorderType.Reflect, new MCvScalar(0));
            //CvInvoke.Imwrite($"Yin_defect_{fontNames[index]}.bmp", yin_defect_img);
            int defect_pixels = 0;    //缺燙總數
            unsafe
            {
                byte* ptr = (byte*)yin_defect_img.DataPointer;
                for (int x = 0; x < yin_defect_img.Width; x++)
                {
                    for (int y = 0; y < yin_defect_img.Height; y++)
                    {
                        byte* current_ptr = ptr + (y * yin_defect_img.Width + x);
                        if (*current_ptr == 255)
                        {
                            defect_pixels++;
                        }
                    }
                }
                fontInfo.Defect_Pixels += defect_pixels;
                fontInfo.Defect_Pixels = Math.Max(0, fontInfo.Defect_Pixels - defectCompensation);
            }
            yin_defect_percentage = (fontInfo.Defect_Pixels * 1.0 / YinTemplate.Total_DefectPixels) * 100;
            Debug.WriteLine($"陰版字體{fontInfo.Fontname}," +
                $"缺燙百分比={fontInfo.Defect_Pixels}/{YinTemplate.Total_DefectPixels}={(yin_defect_percentage):F2}%," +
                $"塞版百分比={fontInfo.Block_Pixels}/{YinTemplate.Total_BlockPixels}={(yin_block_percentage):F2}%"
                );
            //Debug.WriteLine($"陰版{fontInfo.Fontname}," +
            //     $"缺燙{fontInfo.Defect_Pixels}," +
            //     $"塞版{fontInfo.Block_Pixels}"
            //);
            return (yin_block_img, yin_block_percentage, yin_defect_img, yin_defect_percentage);
        }
        #endregion

        #region 陽版計算
        /// <summary>
        /// 陽版所有字切割並初步計算塞版
        /// </summary>
        public static (Font_Imgs, List<Mat>, List<FontInfo>, Mat)
            PrepareYangFontCrops(
                List<List<Rectangle>> yang_lists,
                Mat yangimage, List<TemplateData> YangTemplates,
                OCT_Parameters_CardType current_params,
                OCT_Parameters_SaveOptions saveoptions,
                string cardType
            )
        {
            string[] fontNames = { "3pt", "4pt", "5pt", "6pt", "7pt", "8pt", "9pt", "10pt", "11pt", "12pt" };
            List<Mat> Yang_Image_List = new List<Mat>();
            List<FontInfo> yang_fontInfos = new List<FontInfo>();
            Font_Imgs yang_Imgs = new Font_Imgs();
            for (int i = 0; i < yang_lists.Count; i++)
            {
                var yang_list = yang_lists[i];
                string fontName = fontNames[i];
                int block_outsidepixels = 0;
                // 計算總範圍矩形
                Rectangle font_boundingRect = OCT_AlgorithmHelper.CalculateBoundingRectangle(yang_list, yangimage.Height);
                if (font_boundingRect.IsEmpty)
                {
                    //創建一張空的影像加入
                    Size safeSize = (YangTemplates != null && i < YangTemplates.Count && YangTemplates[i]?.Image != null && !YangTemplates[i].Image.IsEmpty)
                        ? YangTemplates[i].Image.Size
                        : yangimage.Size;
                    Mat emptyImage = new Mat(safeSize, DepthType.Cv8U, 1);
                    emptyImage.SetTo(new MCvScalar(255)); // 陽版背景為白色
                    Yang_Image_List.Add(emptyImage);

                    // 如果未找到，記錄未找到的狀態
                    yang_fontInfos.Add(new FontInfo
                    {
                        Fontname = fontName,
                        Bounds = Rectangle.Empty,
                        Block_Pixels = 0
                    });

                    //Debug.WriteLine($"陽版字體 {fontName}: 未找到有效的矩形，略過處理。");
                }
                else
                {
                    // 如果找到矩形，處理圖像
                    Mat yang_image = new Mat(yangimage, font_boundingRect);
                    // 儲存圖像
                    double current_threshold = 0.0;
                    if (saveoptions.saveoption.Yang_Original)
                    {
                        yang_image.Save($"Yang_{fontName}.bmp");
                    }
                    //保存原始裁切圖像，以便後續廠商在ui上可以點擊修改單一圖片重分析
                    switch (fontName)
                    {
                        case "3pt":
                            yang_Imgs._3pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._3pt;
                            break;
                        case "4pt":
                            yang_Imgs._4pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._4pt;
                            break;
                        case "5pt":
                            yang_Imgs._5pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._5pt;
                            break;
                        case "6pt":
                            yang_Imgs._6pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._6pt;
                            break;
                        case "7pt":
                            yang_Imgs._7pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._7pt;
                            break;
                        case "8pt":
                            yang_Imgs._8pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._8pt;
                            break;
                        case "9pt":
                            yang_Imgs._9pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._9pt;
                            break;
                        case "10pt":
                            yang_Imgs._10pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._10pt;
                            break;
                        case "11pt":
                            yang_Imgs._11pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._11pt;
                            break;
                        case "12pt":
                            yang_Imgs._12pt = yang_image.Clone();
                            current_threshold = current_params.yang_parameter._12pt;
                            break;
                    }
                    CvInvoke.CvtColor(yang_image, yang_image, ColorConversion.Bgr2Gray);
                    if (saveoptions.saveoption.MakeTemplates)
                    {
                        CvInvoke.Threshold(yang_image, yang_image, 180, 255, ThresholdType.Binary);    //陽版存模板的二值化要盡量把字體留下
                    }
                    else
                    {
                        CvInvoke.Threshold(yang_image, yang_image, current_threshold, 255, ThresholdType.Binary);    //給廠商改的參數
                    }
                    //CvInvoke.Threshold(yang_image, yang_image, 180, 255, ThresholdType.Binary);    //給廠商改的參數
                    if (saveoptions.saveoption.Yang_Threshold)
                    {
                        yang_image.Save($"Yang_Threshold_{fontName}.bmp");
                    }
                    Debug.WriteLine($"[Yang Before Crop][{fontName}] W={yang_image.Width}, H={yang_image.Height}, IsEmpty={yang_image.IsEmpty}");
                    Mat yang_image_before_crop = yang_image.Clone();

                    (yang_image, block_outsidepixels) = OCT_AlgorithmHelper.Yang_Calc_OutsideBlock_ResizeFontImg(
                        yang_image, fontName, YangTemplates[i].Width);

                    if (yang_image == null || yang_image.IsEmpty)
                    {
                        Debug.WriteLine($"[Yang SAFE FALLBACK][{fontName}] 裁切結果 Empty，退回裁切前影像");
                        if (yang_image != null) yang_image.Dispose();
                        yang_image = yang_image_before_crop.Clone();
                        block_outsidepixels = 0;
                    }
                    yang_image_before_crop.Dispose();

                    Debug.WriteLine($"[Yang After Crop][{fontName}] W={yang_image.Width}, H={yang_image.Height}, IsEmpty={yang_image.IsEmpty}");

                    if (saveoptions.saveoption.Yang_CalcOutsideBlock)
                    {
                        if (!yang_image.IsEmpty)
                            CvInvoke.Imwrite($"Yang_Resizefont_{fontName}.bmp", yang_image);
                        else
                            Debug.WriteLine($"[Yang Imwrite Skip][{fontName}] Empty Mat，略過存圖");
                    }
                    if (saveoptions.saveoption.MakeTemplates)// 2026.03.13 張植竣新增模板儲存功能 
                    {
                        string templatesDir = cardType == "雙銅"
                            ? "template_double sided coated paper"
                            : "template_white card";

                        if (!Directory.Exists(templatesDir))
                            Directory.CreateDirectory(templatesDir);

                        string templatePath = Path.Combine(templatesDir, $"Yang_template_{fontName}.bmp");
                        if (yang_image != null && !yang_image.IsEmpty)
                            yang_image.Save(templatePath);
                        else
                            Debug.WriteLine($"[Yang Template Skip][{fontName}] Empty Mat，略過模板存檔");
                    }

                    Yang_Image_List.Add(yang_image);
                    // 記錄找到的狀態
                    yang_fontInfos.Add(new FontInfo
                    {
                        Fontname = fontName,
                        Bounds = font_boundingRect,
                        Block_Pixels = block_outsidepixels
                    });
                }
            }
            Mat BreakArea_03_crop = new Mat();
            if (yang_Imgs._12pt != null && !yang_Imgs._12pt.IsEmpty)
            {
                BreakArea_03_crop = BreakArea_analz(yang_Imgs._12pt, saveoptions);
            }
            return (yang_Imgs, Yang_Image_List, yang_fontInfos, BreakArea_03_crop);
        }

        /// <summary>
        /// 陽版計算所有字體
        /// </summary>
        public static (Font_Imgs, Font_Results, Font_Imgs, Font_Results) Yang_Calculate(List<FontInfo> yang_fontInfos, List<Mat> Yang_Image_List, List<TemplateData> YangTemplates)
        {
            Font_Results yang_block_results = new Font_Results();
            Font_Results yang_defect_results = new Font_Results();
            Font_Imgs yang_block_imgs = new Font_Imgs();
            Font_Imgs yang_defect_imgs = new Font_Imgs();

            double total_defectpixels = 0.0;
            double total_defectpixels_templates = 0.0;

            // 新增塞版總計
            double total_blockpixels = 0.0;
            double total_blockpixels_templates = 0.0;

            for (int i = 0; i < yang_fontInfos.Count; i++)
            {
                Mat yang_defect_img = new Mat();
                Mat yang_block_img = new Mat();
                double defect_percentage;
                double block_percentage;
                try
                {
                    if (Yang_Image_List[i].Size != YangTemplates[i].Image.Size)
                    {
                        Mat resizedSafe = new Mat();
                        CvInvoke.Resize(Yang_Image_List[i], resizedSafe, YangTemplates[i].Image.Size, 0, 0, Inter.Nearest);
                        Yang_Image_List[i] = resizedSafe;
                    }
                    (yang_block_img, block_percentage, yang_defect_img, defect_percentage) = Yang_Calc_Single(yang_fontInfos[i], Yang_Image_List[i], YangTemplates[i]);
                    CvInvoke.Imwrite($"Yang_block_{yang_fontInfos[i].Fontname}.bmp", yang_block_img);
                    CvInvoke.Imwrite($"Yang_defect_{yang_fontInfos[i].Fontname}.bmp", yang_defect_img);
                }
                catch (Exception ex)
                {
                    OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, $"陽版字體{yang_fontInfos[i].Fontname}分析失敗", ex.ToString());
                    throw;
                }
                switch (yang_fontInfos[i].Fontname)
                {
                    case "3pt":
                        yang_block_imgs._3pt = yang_block_img;
                        yang_defect_imgs._3pt = yang_defect_img;
                        yang_block_results._3pt = block_percentage;
                        yang_defect_results._3pt = defect_percentage;
                        break;
                    case "4pt":
                        yang_block_imgs._4pt = yang_block_img;
                        yang_defect_imgs._4pt = yang_defect_img;
                        yang_block_results._4pt = block_percentage;
                        yang_defect_results._4pt = defect_percentage;
                        break;
                    case "5pt":
                        yang_block_imgs._5pt = yang_block_img;
                        yang_defect_imgs._5pt = yang_defect_img;
                        yang_block_results._5pt = block_percentage;
                        yang_defect_results._5pt = defect_percentage;
                        break;
                    case "6pt":
                        yang_block_imgs._6pt = yang_block_img;
                        yang_defect_imgs._6pt = yang_defect_img;
                        yang_block_results._6pt = block_percentage;
                        yang_defect_results._6pt = defect_percentage;
                        break;
                    case "7pt":
                        yang_block_imgs._7pt = yang_block_img;
                        yang_defect_imgs._7pt = yang_defect_img;
                        yang_block_results._7pt = block_percentage;
                        yang_defect_results._7pt = defect_percentage;
                        break;
                    case "8pt":
                        yang_block_imgs._8pt = yang_block_img;
                        yang_defect_imgs._8pt = yang_defect_img;
                        yang_block_results._8pt = block_percentage;
                        yang_defect_results._8pt = defect_percentage;
                        break;
                    case "9pt":
                        yang_block_imgs._9pt = yang_block_img;
                        yang_defect_imgs._9pt = yang_defect_img;
                        yang_block_results._9pt = block_percentage;
                        yang_defect_results._9pt = defect_percentage;
                        break;
                    case "10pt":
                        yang_block_imgs._10pt = yang_block_img;
                        yang_defect_imgs._10pt = yang_defect_img;
                        yang_block_results._10pt = block_percentage;
                        yang_defect_results._10pt = defect_percentage;
                        break;
                    case "11pt":
                        yang_block_imgs._11pt = yang_block_img;
                        yang_defect_imgs._11pt = yang_defect_img;
                        yang_block_results._11pt = block_percentage;
                        yang_defect_results._11pt = defect_percentage;
                        break;
                    case "12pt":
                        yang_block_imgs._12pt = yang_block_img;
                        yang_defect_imgs._12pt = yang_defect_img;
                        yang_block_results._12pt = block_percentage;
                        yang_defect_results._12pt = defect_percentage;
                        break;
                }
                total_defectpixels += yang_fontInfos[i].Defect_Pixels;
                total_defectpixels_templates += YangTemplates[i].Total_DefectPixels;

                // ★ 新增
                total_blockpixels += yang_fontInfos[i].Block_Pixels;
                total_blockpixels_templates += YangTemplates[i].Total_BlockPixels;



            }
            yang_defect_results.Defect_Total_Percentage = total_defectpixels / total_defectpixels_templates * 100; //(陽版)總缺燙百分比
            yang_block_results.Block_Total_Percentage = total_blockpixels / total_blockpixels_templates * 100; //(陽版)總塞版百分比
            return (yang_block_imgs, yang_block_results, yang_defect_imgs, yang_defect_results);
        }
        /// <summary>
        /// 陽版計算單字體
        /// </summary>
        public static (Mat, double, Mat, double) Yang_Calc_Single(FontInfo fontInfo, Mat yang_font_image, TemplateData YangTemplate)
        {
            Mat yang_block_img = new Mat();
            Mat yang_defect_img = new Mat();
            double yang_defect_percentage;
            double yang_block_percentage;
            int defectCompensation = 0;
            int blockCompensation = 0;
            // 補償pixel 查表（陽版字體）
            Dictionary<string, (int defect_compensation, int block_compensation)> compensations = new Dictionary<string, (int, int)>
            {
                //陽版塞版補償=初步裁切量+實際塞版量
                { "3pt", (560, 241 + 667) },
                { "4pt", (525, 136 + 666) },
                { "5pt", (347, 151 + 416) },
                { "6pt", (353, 249 + 650) },
                { "7pt", (480, 367 + 849) },
                { "8pt", (311, 169 + 456) },
                { "9pt", (387, 210 + 577) },
                { "10pt", (430, 299 + 740) },
                { "11pt", (720, 193 + 727) },
                { "12pt", (608, 393 + 829) }
            };
            ////補初步計算
            //var compensations = new Dictionary<string, (int defect_compensation, int block_compensation)>
            //{
            //    { "3pt", (0, 241) },
            //    { "4pt", (0, 136) },
            //    { "5pt", (0, 151) },
            //    { "6pt", (0, 249) },
            //    { "7pt", (0, 367) },
            //    { "8pt", (0, 169) },
            //    { "9pt", (0, 210) },
            //    { "10pt", (0, 299) },
            //    { "11pt", (0, 193) },
            //    { "12pt", (0, 393) }
            //};
            // 查詢補償數字
            if (compensations.ContainsKey(fontInfo.Fontname))
            {
                var compensation = compensations[fontInfo.Fontname];
                defectCompensation = compensation.defect_compensation;
                blockCompensation = compensation.block_compensation;
            }

            //測試使用十字kernal侵蝕把偏移誤差除掉
            Mat kernel = new Mat(5, 5, DepthType.Cv8U, 1); // 3x3 大小的結構元素
            kernel.SetTo(new MCvScalar(0)); // 設置結構元素值為1
            kernel.Col(2).SetTo(new MCvScalar(1));  // 中間列設為 1（垂直線）
            kernel.Row(2).SetTo(new MCvScalar(1));  // 中間行設為 1（水平線）
            Mat inverted_template = new Mat();
            CvInvoke.BitwiseNot(YangTemplate.Image, inverted_template);

            try
            {
                //缺燙檢測
                CvInvoke.BitwiseAnd(inverted_template, yang_font_image, yang_defect_img);
                //CvInvoke.Erode(yang_defect_img, yang_defect_img, kernel,new Point(-1,-1),1,BorderType.Reflect,new MCvScalar(0));
            }
            catch (Exception ex)
            {
                throw;
            }

            //CvInvoke.Imwrite($"Yang_defect_{fontNames[index]}.bmp", yang_defect_img);


            //塞版檢測
            CvInvoke.BitwiseNot(yang_font_image, yang_font_image);
            //CvInvoke.Imwrite($"test1_{yang_fontInfos[i].Fontname}.bmp", Yang_Image_List[i]);
            Mat yang_orimg = new Mat();
            CvInvoke.BitwiseOr(yang_defect_img, yang_font_image, yang_orimg);
            //CvInvoke.Imwrite($"test2_{yang_fontInfos[i].Fontname}.bmp", yang_orimg);

            CvInvoke.Subtract(yang_orimg, inverted_template, yang_block_img);
            //CvInvoke.Imwrite($"Yang_block_{fontNames[index]}.bmp", yang_block_img);
            //CvInvoke.Erode(yang_block_img, yang_block_img, kernel, new Point(-1, -1), 1, BorderType.Reflect, new MCvScalar(0));
            int defect_pixels = 0;
            unsafe
            {
                byte* ptr = (byte*)yang_defect_img.DataPointer;
                for (int x = 0; x < yang_defect_img.Width; x++)  // 從中間往右邊找
                {
                    for (int y = 0; y < yang_defect_img.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * yang_defect_img.Width + x);
                        if (*currentPixel == 255)  // 白色像素
                        {
                            defect_pixels++;
                        }
                    }
                }
                fontInfo.Defect_Pixels += defect_pixels;
                fontInfo.Defect_Pixels = Math.Max(0, fontInfo.Defect_Pixels - defectCompensation);    //減去補償值
            }

            yang_defect_percentage = (fontInfo.Defect_Pixels * 1.0 / YangTemplate.Total_DefectPixels) * 100;
            unsafe
            {
                int block_pixels = 0;
                byte* ptr = (byte*)yang_block_img.DataPointer;
                for (int x = 0; x < yang_block_img.Width; x++)
                {
                    for (int y = 0; y < yang_block_img.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * yang_block_img.Width + x);
                        if (*currentPixel == 255)  // 白色像素
                        {
                            block_pixels++;
                        }
                    }
                }
                fontInfo.Block_Pixels += block_pixels;
                fontInfo.Block_Pixels = Math.Max(0, fontInfo.Block_Pixels - blockCompensation);    //減去補償值
            }
            yang_block_percentage = (fontInfo.Block_Pixels * 1.0 / YangTemplate.Total_BlockPixels) * 100;
            Debug.WriteLine(
                $"陽版字體{fontInfo.Fontname}," +
                $"缺燙百分比={fontInfo.Defect_Pixels}/{YangTemplate.Total_DefectPixels}={(yang_defect_percentage):F2}%," +
                $"塞版百分比={fontInfo.Block_Pixels}/{YangTemplate.Total_BlockPixels}={(yang_block_percentage):F2}%"
                );
            //Debug.WriteLine($"陽版{fontInfo.Fontname}," +
            //     $"缺燙{fontInfo.Defect_Pixels}," +
            //     $"塞版{fontInfo.Block_Pixels}"
            //);
            return (yang_block_img, yang_block_percentage, yang_defect_img, yang_defect_percentage);
        }
        #endregion

        #region 規則判斷
        /// <summary>
        /// 所有區塊同時判定
        /// </summary>
        public async static Task<(MeshP_Results, Font_Results, Font_Results, Font_Results, Font_Results, Fullness_Results)> EvaluateRules(
        OCT_Rules OCT_Rules,    //判定規則
        MeshP_Results meshp_results,    //網點區計算結果
        Font_Results yin_defect_results,  //陰版缺燙計算結果
        Font_Results yin_block_results,    //陰版塞版計算結果
        Font_Results yang_defect_results,    //陽版缺燙計算結果
        Font_Results yang_block_results,  //陽版塞版計算結果
        Fullness_Results fullness_results   //飽滿區計算結果
        )
        {
            var meshpTask = Task.Run(() => Evaluate_MeshP(OCT_Rules, meshp_results));
            var yinTask = Task.Run(() => Evaluate_Yin(OCT_Rules, yin_defect_results, yin_block_results));
            var yangTask = Task.Run(() => Evaluate_Yang(OCT_Rules, yang_defect_results, yang_block_results));
            var fullnessTask = Task.Run(() => Evaluate_Fullness(OCT_Rules, fullness_results));

            await Task.WhenAll(meshpTask, yinTask, yangTask, fullnessTask);

            // 組合 Evaluate_Yin 與 Evaluate_Yang 的雙輸出結果
            (yin_defect_results, yin_block_results) = yinTask.Result;
            (yang_defect_results, yang_block_results) = yangTask.Result;

            return (
                meshpTask.Result,
                yin_defect_results, yin_block_results,
                yang_defect_results, yang_block_results,
                fullnessTask.Result
            );
        }
        /// <summary>
        /// 網點區等級判定
        /// </summary>
        public static MeshP_Results Evaluate_MeshP(OCT_Rules OCT_Rules, MeshP_Results meshp_results)
        {
            // 網點區缺燙等級判定前處理
            MeshP_Results meshp_defect_results = new MeshP_Results()
            {
                //網點區缺燙 如果 Area10 小於等於0（缺燙）→ 取絕對值 ；否則轉為0 視為塞版
                Area10 = meshp_results.Area10 <= 0 ? Math.Abs(meshp_results.Area10) : 0,
                Area20 = meshp_results.Area20 <= 0 ? Math.Abs(meshp_results.Area20) : 0,
                Area30 = meshp_results.Area30 <= 0 ? Math.Abs(meshp_results.Area30) : 0,
                Area40 = meshp_results.Area40 <= 0 ? Math.Abs(meshp_results.Area40) : 0,
                Area60 = meshp_results.Area60 <= 0 ? Math.Abs(meshp_results.Area60) : 0,
                Area70 = meshp_results.Area70 <= 0 ? Math.Abs(meshp_results.Area70) : 0,
                Area80 = meshp_results.Area80 <= 0 ? Math.Abs(meshp_results.Area80) : 0,
                Area90 = meshp_results.Area90 <= 0 ? Math.Abs(meshp_results.Area90) : 0,
            };
            // 網點區塞版等級判定前處理
            MeshP_Results meshp_block_results = new MeshP_Results()
            {
                // 網點區塞版 如果 Area10 大於等於0（塞版）→ 保留原本數值；否則轉為0 視為缺燙
                Area10 = meshp_results.Area10 >= 0 ? meshp_results.Area10 : 0,
                Area20 = meshp_results.Area20 >= 0 ? meshp_results.Area20 : 0,
                Area30 = meshp_results.Area30 >= 0 ? meshp_results.Area30 : 0,
                Area40 = meshp_results.Area40 >= 0 ? meshp_results.Area40 : 0,
                Area60 = meshp_results.Area60 >= 0 ? meshp_results.Area60 : 0,
                Area70 = meshp_results.Area70 >= 0 ? meshp_results.Area70 : 0,
                Area80 = meshp_results.Area80 >= 0 ? meshp_results.Area80 : 0,
                Area90 = meshp_results.Area90 >= 0 ? meshp_results.Area90 : 0
            };
            if (meshp_defect_results.Area10 < OCT_Rules.meshp_rule.Defect50) meshp_results.Defect_Level = 5.0;
            else if (meshp_defect_results.Area10 < OCT_Rules.meshp_rule.Defect45) meshp_results.Defect_Level = 4.5;
            else if (meshp_defect_results.Area20 < OCT_Rules.meshp_rule.Defect40) meshp_results.Defect_Level = 4.0;
            else if (meshp_defect_results.Area20 < OCT_Rules.meshp_rule.Defect35) meshp_results.Defect_Level = 3.5;
            else if (meshp_defect_results.Area30 < OCT_Rules.meshp_rule.Defect30) meshp_results.Defect_Level = 3.0;
            else if (meshp_defect_results.Area30 < OCT_Rules.meshp_rule.Defect25) meshp_results.Defect_Level = 2.5;
            else if (meshp_defect_results.Area40 < OCT_Rules.meshp_rule.Defect20) meshp_results.Defect_Level = 2.0;
            else if (meshp_defect_results.Area40 < OCT_Rules.meshp_rule.Defect15) meshp_results.Defect_Level = 1.5;
            else if (meshp_defect_results.Area60 < OCT_Rules.meshp_rule.Defect10) meshp_results.Defect_Level = 1.0;
            else meshp_results.Defect_Level = 0.5;

            if (meshp_block_results.Area90 < OCT_Rules.meshp_rule.Block50) meshp_results.Block_Level = 5.0;
            else if (meshp_block_results.Area90 < OCT_Rules.meshp_rule.Block45) meshp_results.Block_Level = 4.5;
            else if (meshp_block_results.Area80 < OCT_Rules.meshp_rule.Block40) meshp_results.Block_Level = 4.0;
            else if (meshp_block_results.Area70 < OCT_Rules.meshp_rule.Block35) meshp_results.Block_Level = 3.5;
            else if (meshp_block_results.Area60 < OCT_Rules.meshp_rule.Block30) meshp_results.Block_Level = 3.0;
            else if (meshp_block_results.Area40 < OCT_Rules.meshp_rule.Block25) meshp_results.Block_Level = 2.5;
            else if (meshp_block_results.Area30 < OCT_Rules.meshp_rule.Block20) meshp_results.Block_Level = 2.0;
            else if (meshp_block_results.Area20 < OCT_Rules.meshp_rule.Block15) meshp_results.Block_Level = 1.5;
            else if (meshp_block_results.Area10 < OCT_Rules.meshp_rule.Block10) meshp_results.Block_Level = 1.0;
            else meshp_results.Block_Level = 0.5;

            return meshp_results;
        }   //網點規則判定
        /// <summary>
        /// 陰版等級判定
        /// </summary>
        public static (Font_Results, Font_Results) Evaluate_Yin(OCT_Rules OCT_Rules, Font_Results yin_defect_results, Font_Results yin_block_results)
        {
            if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect50) yin_defect_results.Defect_Level = 5.0;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect45) yin_defect_results.Defect_Level = 4.5;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect40) yin_defect_results.Defect_Level = 4.0;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect35) yin_defect_results.Defect_Level = 3.5;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect30) yin_defect_results.Defect_Level = 3.0;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect25) yin_defect_results.Defect_Level = 2.5;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect20) yin_defect_results.Defect_Level = 2.0;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect15) yin_defect_results.Defect_Level = 1.5;
            else if (yin_defect_results.Defect_Total_Percentage < OCT_Rules.yin_rule.Defect10) yin_defect_results.Defect_Level = 1.0;
            else yin_defect_results.Defect_Level = 0.5;

            if (yin_block_results._3pt < OCT_Rules.yin_rule.Block50) yin_block_results.Block_Level = 5.0;
            else if (yin_block_results._4pt < OCT_Rules.yin_rule.Block45) yin_block_results.Block_Level = 4.5;
            else if (yin_block_results._5pt < OCT_Rules.yin_rule.Block40) yin_block_results.Block_Level = 4.0;
            else if (yin_block_results._6pt < OCT_Rules.yin_rule.Block35) yin_block_results.Block_Level = 3.5;
            else if (yin_block_results._7pt < OCT_Rules.yin_rule.Block30) yin_block_results.Block_Level = 3.0;
            else if (yin_block_results._8pt < OCT_Rules.yin_rule.Block25) yin_block_results.Block_Level = 2.5;
            else if (yin_block_results._9pt < OCT_Rules.yin_rule.Block20) yin_block_results.Block_Level = 2.0;
            else if (yin_block_results._10pt < OCT_Rules.yin_rule.Block15) yin_block_results.Block_Level = 1.5;
            else if (yin_block_results._11pt < OCT_Rules.yin_rule.Block10) yin_block_results.Block_Level = 1.0;
            else yin_block_results.Block_Level = 0.5;                                   // 60% ≧ 30%
            return (yin_defect_results, yin_block_results);
        }   //陰版規則判定
        /// <summary>
        /// 陽版等級判定
        /// </summary>
        public static (Font_Results, Font_Results) Evaluate_Yang(OCT_Rules OCT_Rules, Font_Results yang_defect_results, Font_Results yang_block_results)
        {
            if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect50) yang_defect_results.Defect_Level = 5.0;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect45) yang_defect_results.Defect_Level = 4.5;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect40) yang_defect_results.Defect_Level = 4.0;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect35) yang_defect_results.Defect_Level = 3.5;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect30) yang_defect_results.Defect_Level = 3.0;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect25) yang_defect_results.Defect_Level = 2.5;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect20) yang_defect_results.Defect_Level = 2.0;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect15) yang_defect_results.Defect_Level = 1.5;
            else if (yang_defect_results.Defect_Total_Percentage < OCT_Rules.yang_rule.Defect10) yang_defect_results.Defect_Level = 1.0;
            else yang_defect_results.Defect_Level = 0.5;

            if (yang_block_results._3pt < OCT_Rules.yang_rule.Block50) yang_block_results.Block_Level = 5.0;
            else if (yang_block_results._4pt < OCT_Rules.yang_rule.Block45) yang_block_results.Block_Level = 4.5;
            else if (yang_block_results._5pt < OCT_Rules.yang_rule.Block40) yang_block_results.Block_Level = 4.0;
            else if (yang_block_results._6pt < OCT_Rules.yang_rule.Block35) yang_block_results.Block_Level = 3.5;
            else if (yang_block_results._7pt < OCT_Rules.yang_rule.Block30) yang_block_results.Block_Level = 3.0;
            else if (yang_block_results._8pt < OCT_Rules.yang_rule.Block25) yang_block_results.Block_Level = 2.5;
            else if (yang_block_results._9pt < OCT_Rules.yang_rule.Block20) yang_block_results.Block_Level = 2.0;
            else if (yang_block_results._10pt < OCT_Rules.yang_rule.Block15) yang_block_results.Block_Level = 1.5;
            else if (yang_block_results._11pt < OCT_Rules.yang_rule.Block10) yang_block_results.Block_Level = 1.0;
            else yang_block_results.Block_Level = 0.5;                                   // 60% ≧ 30%


            return (yang_defect_results, yang_block_results);
        }   //陽版規則判定
        /// <summary>
        /// 飽滿度等級判定
        /// </summary>
        public static Fullness_Results Evaluate_Fullness(OCT_Rules OCT_Rules, Fullness_Results fullness_results)
        {
            if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect50) fullness_results.Defect_Level = 5.0;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect45) fullness_results.Defect_Level = 4.5;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect40) fullness_results.Defect_Level = 4.0;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect35) fullness_results.Defect_Level = 3.5;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect30) fullness_results.Defect_Level = 3.0;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect25) fullness_results.Defect_Level = 2.5;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect20) fullness_results.Defect_Level = 2.0;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect15) fullness_results.Defect_Level = 1.5;
            else if (fullness_results.Defect_Result < OCT_Rules.fullness_rule.Defect10) fullness_results.Defect_Level = 1.0;
            else fullness_results.Defect_Level = 0.5;

            return (fullness_results);
        }   //飽滿區規則判定
        #endregion

        #region 主演算法
        /// <summary>
        /// 網點區分析
        /// </summary>
        public async static Task<(MeshP_Imgs, MeshP_Imgs, MeshP_Results, Mat)?>
            MeshP_anylz(
                Mat SourceImg1,
                Mat SourceImg2,
                List<double> accurate_thresholds,
                OCT_Parameters_PrintType current_printtype_params,
                OCT_Parameters_SaveOptions saveoptions,
                bool autoMeshThresholdEnabled
            )
        {
            List<Mat> meshp_areas = new List<Mat>();
            Mat breakArea_01_crop;
            try
            {
                //meshp_areas =  Meshpoint_crop(SourceImg, current_printtype_params, saveoptions);  //八個區域裁切
                //meshp_areas = await Meshpoint_crop(SourceImg1, SourceImg2);

                (meshp_areas, breakArea_01_crop) = await Meshpoint_crop(SourceImg1, SourceImg2);
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "網點區裁切錯誤", ex.ToString());
                return null;
            }
            int index = 10;
            foreach (Mat area in meshp_areas)
            {
                CvInvoke.Imwrite($"MeshP_{index}%.bmp", area);
                if (index == 40)
                {
                    index = 60; // 直接跳到 60%
                }
                else
                {
                    index += 10; // 其他情況維持增加 10
                }

            }
            MeshP_Imgs meshp_imgs = new MeshP_Imgs()
            {
                Area10 = meshp_areas[0],
                Area20 = meshp_areas[1],
                Area30 = meshp_areas[2],
                Area40 = meshp_areas[3],
                Area60 = meshp_areas[4],
                Area70 = meshp_areas[5],
                Area80 = meshp_areas[6],
                Area90 = meshp_areas[7]
            };  //製造裁切原圖回傳資料以便重新計算使用

            MeshP_Results meshp_results = new MeshP_Results();
            List<Mat> meshp_areas_threshold =
    meshp_areas.Select(mat => mat.Clone()).ToList();

            List<double> finalThresholds;

            if (autoMeshThresholdEnabled)
            {
                // =====================================================
                // 自動模式
                // Otsu + Area10~90 各區線性補償
                // =====================================================
                finalThresholds =
                    GetAutoMeshThresholds(
                        meshp_areas
                    );

                Debug.WriteLine(
                    "[Mesh Threshold] " +
                    "使用自動模式：Otsu + 各區線性補償"
                );
            }
            else
            {
                // =====================================================
                // 手動模式
                // 維持原本資料庫 Threshold
                // =====================================================
                finalThresholds =
                    new List<double>(
                        accurate_thresholds
                    );

                Debug.WriteLine(
                    "[Mesh Threshold] " +
                    "使用手動模式：原本固定 Threshold"
                );
            }

            meshp_areas_threshold =
                Meshp_areas_threshold(
                    meshp_areas_threshold,
                    finalThresholds
                );
            MeshP_Imgs meshp_result_imgs = new MeshP_Imgs()
            {
                Area10 = meshp_areas_threshold[0],
                Area20 = meshp_areas_threshold[1],
                Area30 = meshp_areas_threshold[2],
                Area40 = meshp_areas_threshold[3],
                Area60 = meshp_areas_threshold[4],
                Area70 = meshp_areas_threshold[5],
                Area80 = meshp_areas_threshold[6],
                Area90 = meshp_areas_threshold[7]
            };  //製造回傳資料

            List<int> meshp_standard_blackpxls = new List<int>();
            int meshp_index = 10;
            int blackpixels = 0;
            foreach (Mat threshold_meshp_area in meshp_areas_threshold)
            {
                CvInvoke.Imwrite($"MeshP_Threshold_{meshp_index}%.bmp", threshold_meshp_area);
                int total_pixels = threshold_meshp_area.Width * threshold_meshp_area.Height;
                int standard_pixels = Convert.ToInt32(total_pixels * meshp_index * 0.01);
                meshp_standard_blackpxls.Add(standard_pixels);
                blackpixels = 0;
                unsafe
                {
                    byte* ptr = (byte*)threshold_meshp_area.DataPointer;
                    for (int x = 0; x < threshold_meshp_area.Width; x++)
                    {
                        for (int y = 0; y < threshold_meshp_area.Height; y++)
                        {
                            byte* current_ptr = ptr + (x + (threshold_meshp_area.Width * y));
                            if (*current_ptr == 0)
                            {
                                blackpixels++;
                            }
                        }
                    }
                }
                double meshp_percentage = (double)(blackpixels - standard_pixels) / (double)total_pixels * 100;
                double meshp_ratio = meshp_index / 100.0;
                //缺燙
                if (meshp_percentage < 0)
                {
                    meshp_percentage = meshp_percentage / meshp_ratio;
                    //Debug.WriteLine($"網點區{meshp_index}% => (目前黑點數量 - 正常黑點數量(面積)) / (長*寬) / 轉換率 = {blackpixels} - {standard_pixels} / {total_pixels} / {meshp_ratio} = {meshp_percentage:F2} => 缺燙{Math.Abs(meshp_percentage):F2}%");
                }
                //塞版
                else
                {
                    meshp_percentage = meshp_percentage / (1 - meshp_ratio);
                    //Debug.WriteLine($"網點區{meshp_index}% => (目前黑點數量 - 正常黑點數量(面積)) / (長*寬) / 轉換率 = {blackpixels} - {standard_pixels} / {total_pixels} / {1 - meshp_ratio} = {meshp_percentage:F2} => 塞版{Math.Abs(meshp_percentage):F2}%");
                }
                switch (meshp_index)
                {
                    case 10:
                        meshp_results.Area10 = meshp_percentage;
                        break;
                    case 20:
                        meshp_results.Area20 = meshp_percentage;
                        break;
                    case 30:
                        meshp_results.Area30 = meshp_percentage;
                        break;
                    case 40:
                        meshp_results.Area40 = meshp_percentage;
                        break;
                    case 60:
                        meshp_results.Area60 = meshp_percentage;
                        break;
                    case 70:
                        meshp_results.Area70 = meshp_percentage;
                        break;
                    case 80:
                        meshp_results.Area80 = meshp_percentage;
                        break;
                    case 90:
                        meshp_results.Area90 = meshp_percentage;
                        break;
                }


                if (meshp_index == 40)
                {
                    meshp_index = 60; // 直接跳到 60%
                }
                else
                {
                    meshp_index += 10; // 其他情況維持增加 10
                }

            }
            Debug.WriteLine(
    "[AutoMesh Result] " +
    $"Area10={meshp_results.Area10:F3}, " +
    $"Area20={meshp_results.Area20:F3}, " +
    $"Area30={meshp_results.Area30:F3}, " +
    $"Area40={meshp_results.Area40:F3}, " +
    $"Area60={meshp_results.Area60:F3}, " +
    $"Area70={meshp_results.Area70:F3}, " +
    $"Area80={meshp_results.Area80:F3}, " +
    $"Area90={meshp_results.Area90:F3}"
);
            return (meshp_imgs, meshp_result_imgs, meshp_results, breakArea_01_crop);
        }
        /// <summary>
        /// 陰版分析 - 2026-09-23 改接 Font v15 新對位演算法。
        /// 舊版 Connected Components / Resize 流程保留在 Yin_analz_Legacy() 供回退比對。
        /// </summary>
        public static (Font_Imgs, Font_Imgs, Font_Results, Font_Imgs, Font_Results, List<FontInfo>)?
            Yin_analz(
                Mat yinimage,
                List<TemplateData> YinTemplates,
                OCT_Parameters_CardType current_params,
                OCT_Parameters_PrintType current_printtype_params,
                OCT_Parameters_SaveOptions saveoptions,
                string cardType
            )
        {
            try
            {
                Debug.WriteLine("[FontV15][Yin] 使用 v15：整條粗定位 -> 分字級局部 NCC -> ±5px 精細對位 -> Binary Difference");
                return FontV15Algorithm.AnalyzeYin(
                    yinimage,
                    YinTemplates,
                    saveoptions,
                    cardType);
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "Yin FontV15 分析失敗", ex.ToString());
                Debug.WriteLine($"[FontV15][Yin ERROR] {ex}");
                return null;
            }
        }

        /// <summary> 
        /// 陰版分析（Legacy 舊版，保留供回退/比對）
        /// </summary>
        public static (Font_Imgs, Font_Imgs, Font_Results, Font_Imgs, Font_Results, List<FontInfo>)?
            Yin_analz_Legacy(
                Mat yinimage,
                List<TemplateData> YinTemplates,
                OCT_Parameters_CardType current_params,
                OCT_Parameters_PrintType current_printtype_params,
                OCT_Parameters_SaveOptions saveoptions,
                string cardType
            )
        {
            Mat yin_processimg = yinimage.Clone(); // 複製一份處理用影像，避免動到原圖
            CvInvoke.CvtColor(yin_processimg, yin_processimg, ColorConversion.Bgr2Gray); // 灰階
            CvInvoke.Threshold(yin_processimg, yin_processimg, 150, 255, ThresholdType.Binary); // 二值化，固定閾值 150 做二值化（>150 變白，<=150 變黑）
            if (saveoptions.saveoption.Roi_AreaCrop) // 儲存 ROI 區域裁切結果，就把目前處理到的影像存成一張圖
            {
                CvInvoke.Imwrite("YinArea_2.bmp", yin_processimg);
            }

            //偵測連通區域
            List<Rectangle> yin_fontRectangles = DetectConnectedComponents(yin_processimg); // 只有一張黑白圖(還不知道哪裡是字)，所以要把黑白圖裡「一塊一塊連在一起的白色」影像找出來拆成一堆「區塊（Rectangle）」，每一塊代表「可能是字或字的一部分」

            // 字體大小(pt)分類
            // 陰版字體區絕對位置(限制連通區域落入位置)
            // 透過印鑑參數選擇決定範圍
            // 先在圖片上畫出 10 條「垂直分區」，再把剛剛找出來的字體框框，丟進它們各自該屬於的分區裡。
            // Python 新版旋轉後，依目前影像寬度重新縮放 3pt~12pt X 分區。
            List<Rectangle> yin_ranges = BuildScaledFontRanges(
                current_printtype_params, yinimage.Width, yinimage.Height, "Yin");
            List<List<Rectangle>> yin_lists = ClassifyFontRegions(yin_fontRectangles, yin_ranges); // 把字體框框分到對應字級
            DebugFontClassification("Yin", yin_fontRectangles, yin_lists);
            ApplyFontRangeFallback(yin_lists, yin_ranges, "Yin");

            //裁切字體
            Font_Imgs yin_Imgs = new Font_Imgs(); // 原始裁切圖（人工檢查「裁切對不對」、UI 顯示）
            List<Mat> Yin_ProcessImage_List = new List<Mat>(); // 「真正要拿去算的字圖」
            List<FontInfo> yin_fontInfos = new List<FontInfo>(); // 字體資訊列表(記錄字在哪一區、計算百分比時用、給後面缺燙/塞版統計當依據)
            // yin_lists：每個字級的字框集合；yinimage：原始圖；YinTemplates：模板資料；current_params：各字級門檻；saveoptions：是否存圖
            (yin_Imgs, Yin_ProcessImage_List, yin_fontInfos) = PrepareYinFontCrops(yin_lists, yinimage, YinTemplates, current_params, saveoptions, cardType);

            //字體對位校正
            var sw_yin = System.Diagnostics.Stopwatch.StartNew();
            Yin_ProcessImage_List = Yin_Resize(yin_fontInfos, Yin_ProcessImage_List, YinTemplates);
            sw_yin.Stop();
            Debug.WriteLine($"[計時] Yin_Resize 耗時: {sw_yin.ElapsedMilliseconds} ms");

            //計算缺燙塞版
            try
            {
                var (yin_block_imgs, yin_block_results, yin_defect_imgs, yin_defect_results) = Yin_Calculate(yin_fontInfos, Yin_ProcessImage_List, YinTemplates);
                return (yin_Imgs, yin_block_imgs, yin_block_results, yin_defect_imgs, yin_defect_results, yin_fontInfos);
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "Yin_analz 最終計算失敗", ex.ToString());
                Debug.WriteLine($"[Yin_analz ERROR] {ex}");
                return null;
            }
        }
        /// <summary>
        /// 陽版分析 - 2026-09-23 改接 Font v15 新對位演算法。
        /// 舊版 Connected Components / Resize 流程保留在 Yang_analz_Legacy() 供回退比對。
        /// </summary>
        public static (Font_Imgs, Font_Imgs, Font_Results, Font_Imgs, Font_Results, List<FontInfo>, Mat)?
            Yang_analz(
                Mat yangimage,
                List<TemplateData> YangTemplates,
                OCT_Parameters_CardType current_params,
                OCT_Parameters_PrintType current_printtype_params,
                OCT_Parameters_SaveOptions saveoptions,
                string cardType,
                TaskCompletionSource<Mat> breakArea03Ready = null)
        {
            try
            {
                Debug.WriteLine("[FontV15][Yang] 使用 v15：整條粗定位 -> 分字級局部 NCC -> ±5px 精細對位 -> Binary Difference");
                return FontV15Algorithm.AnalyzeYang(
                    yangimage,
                    YangTemplates,
                    saveoptions,
                    cardType,
                    breakArea03Ready);
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "Yang FontV15 分析失敗", ex.ToString());
                Debug.WriteLine($"[FontV15][Yang ERROR] {ex}");
                return null;
            }
        }

        /// <summary>
        /// 陽版分析（Legacy 舊版，保留供回退/比對）
        /// </summary>
        public static (Font_Imgs, Font_Imgs, Font_Results, Font_Imgs, Font_Results, List<FontInfo>, Mat)?
    Yang_analz_Legacy(Mat yangimage, List<TemplateData> YangTemplates, OCT_Parameters_CardType current_params, OCT_Parameters_PrintType current_printtype_params, OCT_Parameters_SaveOptions saveoptions, string cardType,
                TaskCompletionSource<Mat> breakArea03Ready = null)
        {
            Mat yang_processimg = yangimage.Clone();
            CvInvoke.CvtColor(yang_processimg, yang_processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(yang_processimg, yang_processimg, 230, 255, ThresholdType.BinaryInv);
            if (saveoptions.saveoption.Roi_AreaCrop)
            {
                CvInvoke.Imwrite("YangArea_2.bmp", yang_processimg);
            }
            List<Rectangle> yang_fontRectangles = DetectConnectedComponents(yang_processimg);
            // Python 新版旋轉後，依目前影像寬度重新縮放 3pt~12pt X 分區。
            List<Rectangle> yang_ranges = BuildScaledFontRanges(
                current_printtype_params, yangimage.Width, yangimage.Height, "Yang");
            List<List<Rectangle>> yang_lists = ClassifyFontRegions(yang_fontRectangles, yang_ranges);
            DebugFontClassification("Yang", yang_fontRectangles, yang_lists);
            ApplyFontRangeFallback(yang_lists, yang_ranges, "Yang");
            List<Mat> Yang_Image_List = new List<Mat>();
            List<FontInfo> yang_fontInfos = new List<FontInfo>();
            Font_Imgs yang_Imgs = new Font_Imgs();
            Mat BreakArea_03_crop = new Mat();
            (yang_Imgs, Yang_Image_List, yang_fontInfos, BreakArea_03_crop) = PrepareYangFontCrops(yang_lists, yangimage, YangTemplates, current_params, saveoptions, cardType);

            breakArea03Ready?.SetResult(BreakArea_03_crop);  // ← Resize 前就通知，提前存入全域

            //var resizeTask = Task.Run(() => Yang_Resize(yang_fontInfos, Yang_Image_List, YangTemplates));  // ← 背景執行
            //Yang_Image_List = resizeTask.Result;  // ← 等待完成
            var sw_yang = System.Diagnostics.Stopwatch.StartNew();
            Yang_Image_List = Yang_Resize(yang_fontInfos, Yang_Image_List, YangTemplates);
            sw_yang.Stop();
            Debug.WriteLine($"[計時] Yang_Resize 耗時: {sw_yang.ElapsedMilliseconds} ms");

            try
            {
                var (yang_block_imgs, yang_block_results, yang_defect_imgs, yang_defect_results) = Yang_Calculate(yang_fontInfos, Yang_Image_List, YangTemplates);
                return (yang_Imgs, yang_block_imgs, yang_block_results, yang_defect_imgs, yang_defect_results, yang_fontInfos, BreakArea_03_crop);
            }
            catch (Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "Yang_analz 最終計算失敗", ex.ToString());
                Debug.WriteLine($"[Yang_analz ERROR] {ex}");
                return null;
            }
        }

        /// <summary>
        /// 飽滿度分析
        /// </summary>
        public static Fullness_Results
            Fullness_analz(
                Mat fullness_area,
                OCT_Parameters_CardType current_params,
                OCT_Parameters_SaveOptions saveoptions
            )
        {
            Fullness_Results fullness_results = new Fullness_Results();
            Mat fullness_processimg = fullness_area.Clone();
            CvInvoke.CvtColor(fullness_processimg, fullness_processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(fullness_processimg, fullness_processimg, current_params.fullness_parameter.Threshold, 255, ThresholdType.Binary);   //給廠商調的參數
            if (saveoptions.saveoption.Fullness_Threshold)
            {
                CvInvoke.Imwrite("Fullness_2_threshold.bmp", fullness_processimg);
            }
            int whitepixels = 0;
            int totalPixels = fullness_processimg.Width * fullness_processimg.Height;

            unsafe
            {
                byte* ptr = (byte*)fullness_processimg.DataPointer;
                for (int x = 0; x < fullness_processimg.Width; x++)
                {
                    for (int y = 0; y < fullness_processimg.Height; y++)
                    {
                        byte* current_ptr = ptr + (x + (fullness_processimg.Width * y));
                        if (*current_ptr == 255)
                        {
                            whitepixels++;
                        }
                    }
                }
            }

            // 計算白色像素佔總面積的比例，並乘以100
            fullness_results.Defect_Result = (double)whitepixels / totalPixels * 100;
            //Debug.WriteLine($"飽滿區百分比={whitepixels}/{totalPixels}={fullness_results.Defect_Result:F2}%");
            return fullness_results;
        }
        #endregion

        /// <summary>
        /// 破開區03
        /// 裁切邏輯：以圖片中心點 X + 50 為 left boundary 往右掃描，找到第一條白色像素超過 80% 的列，將該列 X 座標 + 10 作為 right boundary，從而裁切出破開區03
        /// </summary>
        public static Mat BreakArea_analz(Mat breakarea_img, OCT_Parameters_SaveOptions saveoptions)
        {
            Mat processimg = breakarea_img.Clone();

            // 建立處理用圖（灰階 + Otsu二值化）
            Mat processimg_binary = breakarea_img.Clone();
            CvInvoke.CvtColor(processimg_binary, processimg_binary, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(processimg_binary, processimg_binary, 0, 255, ThresholdType.Otsu);

            int centerX = processimg_binary.Width / 2;
            int BreakArea_03_RightBoundary = 0;
            int BreakArea_03_LeftBoundary = 0;

            unsafe
            {
                byte* ptr = (byte*)processimg_binary.DataPointer;
                int step = processimg_binary.Step;

                // 從中心點 X 由左往右掃
                bool isRightBoundaryFound = false;
                for (int x = centerX; x < processimg_binary.Width; x++)
                {
                    if (isRightBoundaryFound) break;
                    int whitePixels = 0;
                    for (int y = 0; y < processimg_binary.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitePixels++;
                        }
                    }
                    if (whitePixels > (int)(processimg_binary.Height * 0.95))
                    {
                        BreakArea_03_RightBoundary = Math.Min(x + 10, processimg_binary.Width - 1); // 找到後加且防止超出影像寬度 
                        isRightBoundaryFound = true;
                    }
                }
            }

            // 左邊界直接推算
            BreakArea_03_LeftBoundary = BreakArea_03_RightBoundary - 135;

            Debug.WriteLine($"BreakArea_03: Left={BreakArea_03_LeftBoundary}, Right={BreakArea_03_RightBoundary}");

            // 防呆
            if (BreakArea_03_LeftBoundary < 0 || BreakArea_03_RightBoundary <= BreakArea_03_LeftBoundary)
            {
                return new Mat();
            }

            // 裁切原圖
            Rectangle roi = new Rectangle(
                BreakArea_03_LeftBoundary,
                0,
                BreakArea_03_RightBoundary - BreakArea_03_LeftBoundary,
                processimg.Height
            );
            Mat croppedImg = new Mat(processimg, roi).Clone();

            if (saveoptions.saveoption.Roi_AreaCrop)
            {
                Mat rotated = new Mat();
                CvInvoke.Rotate(croppedImg, rotated, RotateFlags.Rotate90Clockwise);
                CvInvoke.Imwrite("BreakArea_03_crop.bmp", rotated);
            }

            return croppedImg;
        }

        /// <summary>
        /// 破開區影像處理（01、02、03 通用）
        /// STEP1. 降噪 對原始灰階影像套用「中值濾波」
        /// STEP2. 背景壓黑 將灰階值 160～255 的高亮度區域(背景白色)直接填為純黑(0)，灰階值 159 以下的區域維持原始灰階值不變將影像純白(255)變純黑(0)，原本黑的維持黑色。

        /// STEP3. 閾值二值化 依照使用者設定閾值參數，將灰階值高於閾值的區域變為純黑(0)。
        /// 處理完直接存檔至程式執行目錄
        /// </summary>
        public static void BreakArea_Process(Mat breakArea01, Mat breakArea02, Mat breakArea03, BreakArea_Parameter breakAreaParams)
        {

            double step1 = breakAreaParams.Threshold_Step1;

            // 破開區01
            if (breakArea01 != null && !breakArea01.IsEmpty)
            {
                Mat result = Process(breakArea01, breakAreaParams.Threshold_01, step1);
                CvInvoke.Imwrite("BreakArea_01_process.bmp", result);
            }

            // 破開區02
            if (breakArea02 != null && !breakArea02.IsEmpty)
            {
                Mat result = Process(breakArea02, breakAreaParams.Threshold_02, step1);
                CvInvoke.Imwrite("BreakArea_02_process.bmp", result);
            }

            // 破開區03
            if (breakArea03 != null && !breakArea03.IsEmpty)
            {
                Mat result = Process(breakArea03, breakAreaParams.Threshold_03, step1);

                //// 存 STEP1 後的中間圖（旋轉90度）
                //Mat step1Rotated = step1Result.Clone();
                //CvInvoke.Rotate(step1Rotated, step1Rotated, RotateFlags.Rotate90Clockwise);
                //CvInvoke.Imwrite("BreakArea_03_step1.bmp", step1Rotated);

                CvInvoke.Rotate(result, result, RotateFlags.Rotate90Clockwise);
                CvInvoke.Imwrite("BreakArea_03_process.bmp", result);
            }
        }

        /// <summary>
        /// 單張破開區影像處理核心
        /// </summary>
        private static Mat Process(Mat srcImage, double threshold, double step1Threshold)
        {
            Mat result = srcImage.Clone();

            if (result.NumberOfChannels > 1)
                CvInvoke.CvtColor(result, result, ColorConversion.Bgr2Gray);

            //CvInvoke.GaussianBlur(result, result, new Size(3, 3), 0);// 高斯濾波
            CvInvoke.MedianBlur(result, result, 5);// 中值濾波 kernel size 3，可視雜點大小調整為5或7


            // STEP1: 找出 灰階 ≤ 235~255 標記為黑色(0)
            Mat whiteMask = new Mat();
            CvInvoke.Threshold(result, whiteMask, step1Threshold, 255, ThresholdType.Binary);
            result.SetTo(new MCvScalar(0), whiteMask);

            // STEP2: 閾值以上變黑
            CvInvoke.Threshold(result, result, threshold, 255, ThresholdType.BinaryInv);

            return result;
        }

        /// <summary>
        /// 破開區黑點垂直投影計算
        /// 固定 x，由下往上統計黑點數，結果乘以像素解析度 0.01mm/pixel
        /// </summary>
        /// <param name="processImg">已處理的二值圖</param>
        /// <param name="useGapLimit">true = 間隔超過 maxGap 停止累計（01、02用）；false = 統計全部黑點（03用）</param>
        /// /// <returns>每個 x 位置的黑點累計長度陣列（單位 mm）</returns>
        public static double[] BreakArea_Calculate(Mat processImg, bool useGapLimit = false)
        {
            if (processImg == null || processImg.IsEmpty)
                return Array.Empty<double>();

            int width = processImg.Width;
            int height = processImg.Height;
            double[] blackPointLengths = new double[width];
            const double pixelResolution = 0.01;
            const int maxGap = 10;

            unsafe
            {
                byte* ptr = (byte*)processImg.DataPointer;
                int step = processImg.Step;

                for (int x = 0; x < width; x++)
                {
                    int blackCount = 0;
                    int gapCount = 0;

                    for (int y = height - 1; y >= 0; y--)
                    {
                        byte* pixel = ptr + (y * step + x);

                        if (*pixel == 0)
                        {
                            blackCount++;
                            gapCount = 0;
                        }
                        else if (useGapLimit)
                        {
                            gapCount++;
                            if (gapCount > maxGap)
                                break;
                        }
                    }

                    blackPointLengths[x] = blackCount * pixelResolution;
                }
            }

            return blackPointLengths;
        }



        #region 重新計算單獨區域
        /// <summary>
        /// 重新分析網點單區
        /// </summary>
        public static (MeshP_Imgs, MeshP_Imgs, MeshP_Results) Single_MeshP_anylz
        (
            MeshP_Imgs MeshP_Imgs,
            MeshP_Imgs MeshP_Result_Imgs,
            MeshP_Results MeshP_Results,
            SingleAreaInfo single_area_info,
            List<double> accurate_thresholds,
            OCT_Parameters_SaveOptions saveoptions
        )
        {
            Mat processimg = single_area_info.Image.Clone();
            double threshold = 0.0;
            Mat meshp_result_img = null;
            double meshp_index = 0.1;

            // 根據區域選擇二值化參數
            switch (single_area_info.Name)
            {
                case "Area10":
                    threshold = accurate_thresholds[0];
                    meshp_result_img = MeshP_Result_Imgs.Area10;
                    meshp_index = 0.1;
                    break;
                case "Area20":
                    threshold = accurate_thresholds[1];
                    meshp_result_img = MeshP_Result_Imgs.Area20;
                    meshp_index = 0.2;
                    break;
                case "Area30":
                    threshold = accurate_thresholds[2];
                    meshp_result_img = MeshP_Result_Imgs.Area30;
                    meshp_index = 0.3;
                    break;
                case "Area40":
                    threshold = accurate_thresholds[3];
                    meshp_result_img = MeshP_Result_Imgs.Area40;
                    meshp_index = 0.4;
                    break;
                case "Area60":
                    threshold = accurate_thresholds[4];
                    meshp_result_img = MeshP_Result_Imgs.Area60;
                    meshp_index = 0.6;
                    break;
                case "Area70":
                    threshold = accurate_thresholds[5];
                    meshp_result_img = MeshP_Result_Imgs.Area70;
                    meshp_index = 0.7;
                    break;
                case "Area80":
                    threshold = accurate_thresholds[6];
                    meshp_result_img = MeshP_Result_Imgs.Area80;
                    meshp_index = 0.8;
                    break;
                case "Area90":
                    threshold = accurate_thresholds[7];
                    meshp_result_img = MeshP_Result_Imgs.Area90;
                    meshp_index = 0.9;
                    break;
            }

            // 轉換為灰階並套用閾值
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(processimg, meshp_result_img, threshold, 255, ThresholdType.Binary);
            //CvInvoke.Imwrite("SingleAreaThreshold.bmp", meshp_result_img);
            // 建立遮罩圖像（255表示要處理的像素，0表示要忽略的像素）
            Mat mask = new Mat(processimg.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
            mask.SetTo(new MCvScalar(255)); // 初始設置所有像素都要處理

            // 繪製所有要排除的區域（在遮罩中設為0）
            foreach (var region in single_area_info.Regions)
            {
                CvInvoke.DrawContours(mask, new VectorOfVectorOfPoint(new[] { region.Contour }), 0, new MCvScalar(0), -1); // 用0填充輪廓內部
            }
            //CvInvoke.Imwrite("SingleAreaMask.bmp", mask);
            int total_pixels = 0; // 計算有效像素總數（排除區域外）
            int blackpixels = 0;  // 計算有效區域內的黑色像素

            unsafe
            {
                byte* ptr = (byte*)meshp_result_img.DataPointer;
                byte* maskPtr = (byte*)mask.DataPointer;
                int imageStep = meshp_result_img.Step;
                int maskStep = mask.Step;

                for (int y = 0; y < meshp_result_img.Height; y++)
                {
                    byte* row = ptr + y * imageStep;
                    byte* maskRow = maskPtr + y * maskStep;
                    for (int x = 0; x < meshp_result_img.Width; x++)
                    {
                        if (maskRow[x] == 255)
                        {
                            total_pixels++;
                            if (row[x] == 0)
                                blackpixels++;
                        }
                    }
                }
            }

            // total_pixels 已經只統計 mask==255 的有效像素，這裡不能再扣一次排除面積。
            if (total_pixels <= 0)
            {
                mask.Dispose();
                processimg.Dispose();
                throw new InvalidOperationException("重新分析網點區失敗：有效像素為 0，請縮小排除區域。");
            }

            int standard_pixels = Convert.ToInt32(total_pixels * meshp_index);
            double meshp_percentage = (double)(blackpixels - standard_pixels) / total_pixels * 100;
            //缺燙
            if (meshp_percentage < 0)
            {
                meshp_percentage = meshp_percentage / meshp_index;
                //Debug.WriteLine($"網點區{meshp_index*100}% => (目前黑點數量 - 正常黑點數量(面積)) / (長*寬) / 轉換率 = {blackpixels} - {standard_pixels} / {total_pixels} / {meshp_index} = {meshp_percentage:F2} => 缺燙{Math.Abs(meshp_percentage):F2}%");
            }
            //塞版
            else
            {
                meshp_percentage = meshp_percentage / (1 - meshp_index);
                //Debug.WriteLine($"網點區{meshp_index*100}% => (目前黑點數量 - 正常黑點數量(面積)) / (長*寬) / 轉換率 = {blackpixels} - {standard_pixels} / {total_pixels} / {1 - meshp_index} = {meshp_percentage:F2} => 塞版{Math.Abs(meshp_percentage):F2}%");
            }
            switch (meshp_index)
            {
                case 0.1:
                    MeshP_Results.Area10 = meshp_percentage;
                    break;
                case 0.2:
                    MeshP_Results.Area20 = meshp_percentage;
                    break;
                case 0.3:
                    MeshP_Results.Area30 = meshp_percentage;
                    break;
                case 0.4:
                    MeshP_Results.Area40 = meshp_percentage;
                    break;
                case 0.6:
                    MeshP_Results.Area60 = meshp_percentage;
                    break;
                case 0.7:
                    MeshP_Results.Area70 = meshp_percentage;
                    break;
                case 0.8:
                    MeshP_Results.Area80 = meshp_percentage;
                    break;
                case 0.9:
                    MeshP_Results.Area90 = meshp_percentage;
                    break;
            }

            mask.Dispose();
            processimg.Dispose();
            return (MeshP_Imgs, MeshP_Result_Imgs, MeshP_Results);
        }
        /// <summary>
        /// 重新分析陰版單字體 - 2026-09-24 改用 Font v15。
        /// 人工框選區域會從缺燙/塞版計算與分母中真正排除。
        /// </summary>
        public static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>
            ) Single_Yin_analz(
            Font_Imgs yin_imgs,
            Font_Imgs yin_block_imgs,
            Font_Results yin_block_results,
            Font_Imgs yin_defect_imgs,
            Font_Results yin_defect_results,
            SingleAreaInfo single_area_info,
            OCT_Parameters_CardType currentcardtype_params,
            List<FontInfo> yin_font_infos,
            List<TemplateData> yintemplates,
            OCT_Parameters_SaveOptions saveoptions,
            string cardType = "白卡"
            )
        {
            string fontName = single_area_info.Name;
            int index = FontNameToIndex(fontName);

            var result = FontV15Algorithm.ReAnalyzeSingleFont(
                single_area_info.Image,
                fontName,
                true,
                yintemplates,
                single_area_info.Regions,
                cardType,
                saveoptions);

            SetSingleFontReAnalyzeResult(
                fontName,
                yin_block_imgs,
                yin_block_results,
                yin_defect_imgs,
                yin_defect_results,
                result.blockImg,
                result.blockPercentage,
                result.defectImg,
                result.defectPercentage);

            if (yin_font_infos != null && index >= 0 && index < yin_font_infos.Count)
            {
                Rectangle oldBounds = yin_font_infos[index]?.Bounds ?? Rectangle.Empty;
                result.fontInfo.Bounds = oldBounds;
                yin_font_infos[index] = result.fontInfo;
            }

            FontV15Algorithm.RecalculateTotals(
                yin_defect_results,
                yin_block_results,
                yin_font_infos);

            Debug.WriteLine(
                $"[FontV15][ReAnalyze][Yin][{fontName}] " +
                $"defect={result.defectPercentage:F4}%, block={result.blockPercentage:F4}%, " +
                $"excluded={single_area_info.Regions?.Count ?? 0}");

            return (yin_imgs, yin_block_imgs, yin_block_results, yin_defect_imgs, yin_defect_results, yin_font_infos);
        }

        /// <summary>
        /// 重新分析陽版單字體 - 2026-09-24 改用 Font v15。
        /// 人工框選區域會從缺燙/塞版計算與分母中真正排除。
        /// </summary>
        public static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>
            ) Single_Yang_analz(
            Font_Imgs yang_imgs,
            Font_Imgs yang_block_imgs,
            Font_Results yang_block_results,
            Font_Imgs yang_defect_imgs,
            Font_Results yang_defect_results,
            SingleAreaInfo single_area_info,
            OCT_Parameters_CardType currentcardtype_params,
            List<FontInfo> yang_font_infos,
            List<TemplateData> yangtemplates,
            OCT_Parameters_SaveOptions saveoptions,
            string cardType = "白卡"
            )
        {
            string fontName = single_area_info.Name;
            int index = FontNameToIndex(fontName);

            var result = FontV15Algorithm.ReAnalyzeSingleFont(
                single_area_info.Image,
                fontName,
                false,
                yangtemplates,
                single_area_info.Regions,
                cardType,
                saveoptions);

            SetSingleFontReAnalyzeResult(
                fontName,
                yang_block_imgs,
                yang_block_results,
                yang_defect_imgs,
                yang_defect_results,
                result.blockImg,
                result.blockPercentage,
                result.defectImg,
                result.defectPercentage);

            if (yang_font_infos != null && index >= 0 && index < yang_font_infos.Count)
            {
                Rectangle oldBounds = yang_font_infos[index]?.Bounds ?? Rectangle.Empty;
                result.fontInfo.Bounds = oldBounds;
                yang_font_infos[index] = result.fontInfo;
            }

            FontV15Algorithm.RecalculateTotals(
                yang_defect_results,
                yang_block_results,
                yang_font_infos);

            Debug.WriteLine(
                $"[FontV15][ReAnalyze][Yang][{fontName}] " +
                $"defect={result.defectPercentage:F4}%, block={result.blockPercentage:F4}%, " +
                $"excluded={single_area_info.Regions?.Count ?? 0}");

            return (yang_imgs, yang_block_imgs, yang_block_results, yang_defect_imgs, yang_defect_results, yang_font_infos);
        }

        private static int FontNameToIndex(string fontName)
        {
            switch (fontName)
            {
                case "3pt": return 0;
                case "4pt": return 1;
                case "5pt": return 2;
                case "6pt": return 3;
                case "7pt": return 4;
                case "8pt": return 5;
                case "9pt": return 6;
                case "10pt": return 7;
                case "11pt": return 8;
                case "12pt": return 9;
                default: throw new ArgumentException("未知字級：" + fontName);
            }
        }

        private static void SetSingleFontReAnalyzeResult(
            string fontName,
            Font_Imgs blockImgs,
            Font_Results blockResults,
            Font_Imgs defectImgs,
            Font_Results defectResults,
            Mat blockImg,
            double blockPercentage,
            Mat defectImg,
            double defectPercentage)
        {
            switch (fontName)
            {
                case "3pt": blockImgs._3pt = blockImg; blockResults._3pt = blockPercentage; defectImgs._3pt = defectImg; defectResults._3pt = defectPercentage; break;
                case "4pt": blockImgs._4pt = blockImg; blockResults._4pt = blockPercentage; defectImgs._4pt = defectImg; defectResults._4pt = defectPercentage; break;
                case "5pt": blockImgs._5pt = blockImg; blockResults._5pt = blockPercentage; defectImgs._5pt = defectImg; defectResults._5pt = defectPercentage; break;
                case "6pt": blockImgs._6pt = blockImg; blockResults._6pt = blockPercentage; defectImgs._6pt = defectImg; defectResults._6pt = defectPercentage; break;
                case "7pt": blockImgs._7pt = blockImg; blockResults._7pt = blockPercentage; defectImgs._7pt = defectImg; defectResults._7pt = defectPercentage; break;
                case "8pt": blockImgs._8pt = blockImg; blockResults._8pt = blockPercentage; defectImgs._8pt = defectImg; defectResults._8pt = defectPercentage; break;
                case "9pt": blockImgs._9pt = blockImg; blockResults._9pt = blockPercentage; defectImgs._9pt = defectImg; defectResults._9pt = defectPercentage; break;
                case "10pt": blockImgs._10pt = blockImg; blockResults._10pt = blockPercentage; defectImgs._10pt = defectImg; defectResults._10pt = defectPercentage; break;
                case "11pt": blockImgs._11pt = blockImg; blockResults._11pt = blockPercentage; defectImgs._11pt = defectImg; defectResults._11pt = defectPercentage; break;
                case "12pt": blockImgs._12pt = blockImg; blockResults._12pt = blockPercentage; defectImgs._12pt = defectImg; defectResults._12pt = defectPercentage; break;
                default: throw new ArgumentException("未知字級：" + fontName);
            }
        }

        /// <summary>
        /// 重新分析飽滿區。
        /// 人工排除區域同時從白像素與有效總像素分母中排除。
        /// </summary>
        public static Fullness_Results Single_Fullness_analz(
            Fullness_Results fullness_results,
            SingleAreaInfo single_area_info,
            OCT_Parameters_CardType current_params,
            OCT_Parameters_SaveOptions saveoptions
        )
        {
            Mat fullness_processimg = single_area_info.Image.Clone();
            Mat mask = null;
            try
            {
                CvInvoke.CvtColor(fullness_processimg, fullness_processimg, ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(
                    fullness_processimg,
                    fullness_processimg,
                    current_params.fullness_parameter.Threshold,
                    255,
                    ThresholdType.Binary);

                if (saveoptions.saveoption.Fullness_Threshold)
                    CvInvoke.Imwrite("Fullness_2_threshold.bmp", fullness_processimg);

                mask = new Mat(fullness_processimg.Size, DepthType.Cv8U, 1);
                mask.SetTo(new MCvScalar(255));

                if (single_area_info.Regions != null)
                {
                    foreach (Region region in single_area_info.Regions)
                    {
                        if (region?.Contour == null || region.Contour.Size < 3)
                            continue;
                        using (var contours = new VectorOfVectorOfPoint(region.Contour))
                        {
                            CvInvoke.FillPoly(mask, contours, new MCvScalar(0));
                        }
                    }
                }

                int whitepixels = 0;
                int effectivePixels = 0;

                unsafe
                {
                    byte* imagePtr = (byte*)fullness_processimg.DataPointer;
                    byte* maskPtr = (byte*)mask.DataPointer;
                    int imageStep = fullness_processimg.Step;
                    int maskStep = mask.Step;

                    for (int y = 0; y < fullness_processimg.Height; y++)
                    {
                        byte* row = imagePtr + y * imageStep;
                        byte* maskRow = maskPtr + y * maskStep;
                        for (int x = 0; x < fullness_processimg.Width; x++)
                        {
                            if (maskRow[x] != 255)
                                continue;

                            effectivePixels++;
                            if (row[x] == 255)
                                whitepixels++;
                        }
                    }
                }

                if (effectivePixels <= 0)
                    throw new InvalidOperationException("重新分析飽滿區失敗：有效像素為 0，請縮小排除區域。");

                fullness_results.Defect_Result = whitepixels * 100.0 / effectivePixels;
                return fullness_results;
            }
            finally
            {
                mask?.Dispose();
                fullness_processimg.Dispose();
            }
        }
        #endregion

    }
}

