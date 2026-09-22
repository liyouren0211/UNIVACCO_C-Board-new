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
        private static Mat Rotate(Mat sourceimg ,OCT_Parameters_SaveOptions saveoptions)
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
                    if(tempLine.P1.X < center.X) //確保P1在左半邊
                    {
                        if (tempLine.P1.X <= rot_left_top.X || Math.Abs(tempLine.P1.X - rot_left_top.X) <= 30) //解決有時會因印刷突出造成X被拉到最小而沒有進入條件
                        {

                            rot_left_top.X = Math.Min(tempLine.P1.X, rot_left_top.X);
                            int y = Math.Min(tempLine.P1.Y, tempLine.P2.Y);
                            rot_left_top.Y = Math.Min(y, rot_left_top.Y);
                            if (rot_left_top.Y >200)
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
        /// 自動校正閾值(建立臨時標準功能)
        /// </summary>
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
            List<Mat> meshp_areas = new List<Mat>();

            // 格子尺寸定義
            int gridWidth = 2050;   // 每個格子的寬度
            int gridHeight = 1226;  // 每個格子的高度

            // 建立副本避免影響原圖
            Mat processimg1 = sourceimg1.Clone();
            Mat processimg2 = sourceimg2.Clone();
            //灰階化
            CvInvoke.CvtColor(processimg1, processimg1, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(processimg2, processimg2, ColorConversion.Bgr2Gray);
            //otsu二值化
            CvInvoke.Threshold(processimg1, processimg1, 0, 255, ThresholdType.Otsu);
            CvInvoke.Threshold(processimg2, processimg2, 0, 255, ThresholdType.Otsu);
            CvInvoke.Imwrite("Meshpoint_crop_img1_otsu.png", processimg1);
            CvInvoke.Imwrite("Meshpoint_crop_img2_otsu.png", processimg2);

            #region 網點區1267裁切
            // 找左右邊界和中線
            int middleBoundary1 = processimg1.Width - 1;
            int top_y = 0;
            unsafe
            {
                byte* ptr = (byte*)processimg1.DataPointer;
                int step = processimg1.Step;
                // 找上邊界 - 從下往上找
                bool istopyFound = false;
                for (int y = 3500; y > 0; y--)
                {
                    if (istopyFound) break;

                    int whitePixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
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
                // 從1800往中找中邊界
                bool middleBoundaryFound = false;
                for (int x = 1800; x < processimg1.Width; x++)
                {
                    if (middleBoundaryFound)
                    {
                        break;
                    }
                    int blackPixels = 0;
                    for (int y = top_y; y < processimg1.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                        }
                    }

                    if (blackPixels > (int)((processimg1.Height - top_y) * 0.6))
                    {
                        middleBoundary1 = x;
                        middleBoundaryFound = true;
                    }
                }
            }
            int leftBoundary1 = middleBoundary1 - gridWidth;
            Debug.WriteLine($"processimg1 Found boundaries: top_y={top_y}, middle_x={middleBoundary1}, left_x={leftBoundary1}");

            Mat Area10 = new Mat(sourceimg1, new Rectangle(leftBoundary1, top_y, middleBoundary1 - leftBoundary1, gridHeight)).Clone();
            Mat Area20 = new Mat(sourceimg1, new Rectangle(leftBoundary1, top_y + gridHeight, middleBoundary1 - leftBoundary1, gridHeight)).Clone();
            Mat Area60 = new Mat(sourceimg1, new Rectangle(middleBoundary1, top_y, gridWidth, gridHeight)).Clone();
            Mat Area70 = new Mat(sourceimg1, new Rectangle(middleBoundary1, top_y + gridHeight, gridWidth, gridHeight)).Clone();

            // 旋轉90度後存圖
            Area10 = Area10.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;
            Area20 = Area20.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;
            Area60 = Area60.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;
            Area70 = Area70.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;

            CvInvoke.Imwrite("Area10.bmp", Area10);
            CvInvoke.Imwrite("Area20.bmp", Area20);
            CvInvoke.Imwrite("Area60.bmp", Area60);
            CvInvoke.Imwrite("Area70.bmp", Area70);
            #endregion

            #region 網點區3489裁切
            int middleBoundary2 = processimg2.Width - 1;
            int bottom_y = 0;
            unsafe
            {
                byte* ptr = (byte*)processimg2.DataPointer;
                int step = processimg2.Step;
                // 找下邊界 - 從上往下找
                bool isbottomyFound = false;
                for (int y = 148; y < processimg2.Height; y++)
                {
                    if (isbottomyFound) break;

                    int whitePixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            whitePixels++;
                            if (whitePixels > (int)((2400 - 800) * 0.95))
                            {
                                bottom_y = y;    //下邊界
                                isbottomyFound = true;
                                break;
                            }
                        }
                    }
                }
                // 從1800往中找中邊界
                bool middleBoundaryFound = false;
                for (int x = 1800; x < processimg2.Width; x++)
                {
                    if (middleBoundaryFound)
                    {
                        break;
                    }
                    int blackPixels = 0;
                    for (int y = 0; y < bottom_y; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                        }
                    }

                    if (blackPixels > (int)(bottom_y * 0.8))
                    {
                        middleBoundary2 = x;
                        middleBoundaryFound = true;
                    }
                }
            }
            #region 破開區01
            // 建立副本避免影響原圖
            Mat processimg_BreakArea_01 = sourceimg1.Clone();
            Mat BreakArea_01_crop = new Mat();
            // 灰階化
            CvInvoke.CvtColor(processimg_BreakArea_01, processimg_BreakArea_01, ColorConversion.Bgr2Gray);
            // Otsu二值化
            CvInvoke.Threshold(processimg_BreakArea_01, processimg_BreakArea_01, 0, 255, ThresholdType.Otsu);

            int BreakArea_01_top_y = 0;
            int BreakArea_01_button_y = 0;
            int BreakArea_01_LeftBoundary = 0;
            int BreakArea_01_RightBoundary = 0;

            unsafe
            {
                byte* ptr = (byte*)processimg_BreakArea_01.DataPointer;
                int step = processimg_BreakArea_01.Step;

                // 找上邊界 - 從 y=0 由上往下掃
                bool isTopYFound = false;
                for (int y = 0; y < processimg_BreakArea_01.Height; y++)
                {
                    if (isTopYFound) break;
                    int blackPixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                            if (blackPixels > (int)((2400 - 800) * 0.95))
                            {
                                BreakArea_01_top_y = y;
                                isTopYFound = true;
                                break;
                            }
                        }
                    }
                }

                // 找下邊界 - 從 y=BreakArea_01_top_y 由上往下掃
                bool isButtonYFound = false;
                for (int y = BreakArea_01_top_y; y < processimg_BreakArea_01.Height; y++)
                {
                    if (isButtonYFound) break;
                    int whitePixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitePixels++;
                            if (whitePixels > (int)((2400 - 800) * 0.90))
                            {
                                BreakArea_01_button_y = y + 8;    // 找到後加8
                                isButtonYFound = true;
                                break;
                            }
                        }
                    }
                }

                // 找左邊界 - 從 x=1800 由右往左掃
                bool isLeftBoundaryFound = false;
                for (int x = 1800; x > 0; x--)
                {
                    if (isLeftBoundaryFound) break;
                    int whitePixels = 0;
                    for (int y = BreakArea_01_top_y; y < BreakArea_01_button_y; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitePixels++;
                        }
                    }
                    if (whitePixels > (int)((BreakArea_01_button_y - BreakArea_01_top_y) * 0.6))
                    {
                        BreakArea_01_LeftBoundary = x;
                        isLeftBoundaryFound = true;
                    }
                }
            }

            // 右邊界直接推算
            BreakArea_01_RightBoundary = BreakArea_01_LeftBoundary + 2094;

            Debug.WriteLine($"BreakArea_01: top_y={BreakArea_01_top_y}, button_y={BreakArea_01_button_y}, Left={BreakArea_01_LeftBoundary}, Right={BreakArea_01_RightBoundary}");

            // 裁切原圖
            if (BreakArea_01_LeftBoundary < BreakArea_01_RightBoundary &&
                BreakArea_01_top_y < BreakArea_01_button_y)
            {
                Rectangle BreakArea_01_ROI = new Rectangle(
                    BreakArea_01_LeftBoundary,
                    BreakArea_01_top_y,
                    BreakArea_01_RightBoundary - BreakArea_01_LeftBoundary,
                    BreakArea_01_button_y - BreakArea_01_top_y
                );
                BreakArea_01_crop = new Mat(sourceimg1, BreakArea_01_ROI).Clone();  // 用原圖裁切
                CvInvoke.Imwrite("BreakArea_01_crop.bmp", BreakArea_01_crop);
            }
            #endregion
            int leftBoundary2 = middleBoundary2 - gridWidth;
            Debug.WriteLine($"processimg2 Found boundaries: bottom_y={bottom_y}, middle_x={middleBoundary2}, left_x={leftBoundary2}");

            Mat Area30 = new Mat(sourceimg2, new Rectangle(leftBoundary2, bottom_y - gridHeight - gridHeight, middleBoundary2 - leftBoundary2, gridHeight)).Clone();
            Mat Area40 = new Mat(sourceimg2, new Rectangle(leftBoundary2, bottom_y - gridHeight, middleBoundary2 - leftBoundary2, gridHeight)).Clone();
            Mat Area80 = new Mat(sourceimg2, new Rectangle(middleBoundary2, bottom_y - gridHeight - gridHeight, gridWidth, gridHeight)).Clone();
            Mat Area90 = new Mat(sourceimg2, new Rectangle(middleBoundary2, bottom_y - gridHeight, gridWidth, gridHeight)).Clone();

            // 旋轉90度後存圖
            Area30 = Area30.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;
            Area40 = Area40.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;
            Area80 = Area80.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;
            Area90 = Area90.ToImage<Bgr, byte>().Rotate(-90, new Bgr(), false).Mat;

            CvInvoke.Imwrite("Area30.bmp", Area30);
            CvInvoke.Imwrite("Area40.bmp", Area40);
            CvInvoke.Imwrite("Area80.bmp", Area80);
            CvInvoke.Imwrite("Area90.bmp", Area90);
            #endregion

            // 將所有區域加入回傳列表
            meshp_areas.AddRange(new[] { Area10, Area20, Area30, Area40, Area60, Area70, Area80, Area90 });

            return (meshp_areas, BreakArea_01_crop);
        }
        public static async Task<(Mat, Mat)> Merge_FontArea(Mat sourceimg3, Mat sourceimg4)
        {
           // 陽版寬度定義
            int fontWidth = 2150;   // 中間到左側的距離

            // 建立副本避免影響原圖
            Mat processimg3 = sourceimg3.Clone();
            Mat processimg4 = sourceimg4.Clone();
            //灰階化
            CvInvoke.CvtColor(processimg3, processimg3, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(processimg4, processimg4, ColorConversion.Bgr2Gray);
            //otsu二值化
            CvInvoke.Threshold(processimg3, processimg3, 0, 255, ThresholdType.Otsu);
            CvInvoke.Threshold(processimg4, processimg4, 0, 255, ThresholdType.Otsu);
            CvInvoke.Imwrite("Meshpoint_crop_img1_otsu.bmp", processimg3);
            CvInvoke.Imwrite("Meshpoint_crop_img2_otsu.bmp", processimg4);

            #region 字體區1
            // 找左右邊界和中線
            int middleBoundary1 = processimg3.Width - 1;
            int rightBoundary = processimg3.Width - 1;
            int top_y = 0;
            unsafe
            {
                byte* ptr = (byte*)processimg3.DataPointer;
                int step = processimg3.Step;
                // 找上邊界 - 從上往下找
                bool istopyFound = false;
                for (int y = 3500; y > 0; y--)
                {
                    if (istopyFound) break;

                    int whitePixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
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
                // 從1800往中找中邊界
                bool middleBoundaryFound = false;
                for (int x = 1800; x < processimg3.Width; x++)
                {
                    if (middleBoundaryFound)
                    {
                        break;
                    }
                    int blackPixels = 0;
                    for (int y = top_y; y < processimg3.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                        }
                    }

                    if (blackPixels > (int)((processimg3.Height - top_y) * 0.6))
                    {
                        middleBoundary1 = x;
                        middleBoundaryFound = true;
                    }
                }
                // 從3400往右找右邊界
                bool rightBoundaryFound = false;
                for (int x = 3400; x < processimg4.Width; x++)
                {
                    if (rightBoundaryFound)
                    {
                        break;
                    }
                    int whitepixels = 0;
                    for (int y = top_y; y < processimg3.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitepixels++;
                        }
                    }

                    if (whitepixels > (int)((processimg3.Height - top_y) * 0.8))
                    {
                        rightBoundary = x;
                        rightBoundaryFound = true;
                    }
                }
            }
            int leftBoundary1 = middleBoundary1 - fontWidth;
            Debug.WriteLine($"processimg1 Found boundaries: top_y={top_y}, middle_x={middleBoundary1}, left_x={leftBoundary1}");

            processimg3 = new Mat(sourceimg3, new Rectangle(leftBoundary1, top_y, rightBoundary - leftBoundary1, 1860)).Clone();

            CvInvoke.Imwrite("processimg3.bmp", processimg3);


            #endregion

            #region 字體區2
            int middleBoundary2 = processimg4.Width - 1;
            rightBoundary = processimg4.Width - 1;
            int bottom_y = 0;
            unsafe
            {
                byte* ptr = (byte*)processimg4.DataPointer;
                int step = processimg4.Step;
                // 找下邊界 - 從下往上找
                bool isbottomyFound = false;
                for (int y = 148; y < processimg4.Height; y++)
                {
                    if (isbottomyFound) break;

                    int whitePixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            whitePixels++;
                            if (whitePixels > (int)((2400 - 800) * 0.95))
                            {
                                bottom_y = y;    //下邊界
                                isbottomyFound = true;
                                break;
                            }
                        }
                    }
                }
                // 從1800往中找中邊界
                bool middleBoundaryFound = false;
                for (int x = 1800; x < processimg4.Width; x++)
                {
                    if (middleBoundaryFound)
                    {
                        break;
                    }
                    int blackPixels = 0;
                    for (int y = 0; y < bottom_y; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                        }
                    }

                    if (blackPixels > (int)(bottom_y * 0.8))
                    {
                        middleBoundary2 = x;
                        middleBoundaryFound = true;
                    }
                }
                // 從3400往中找中邊界
                bool rightBoundaryFound = false;
                for (int x = 3400; x < processimg4.Width; x++)
                {
                    if (rightBoundaryFound)
                    {
                        break;
                    }
                    int whitepixels = 0;
                    for (int y = 0; y < bottom_y; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitepixels++;
                        }
                    }

                    if (whitepixels > (int)(bottom_y * 0.8))
                    {
                        rightBoundary = x;
                        rightBoundaryFound = true;
                    }
                }
            }
            int leftBoundary2 = middleBoundary2 - fontWidth;
            Debug.WriteLine($"processimg2 Found boundaries: bottom_y={bottom_y}, middle_x={middleBoundary2}, left_x={leftBoundary2},rightBoundary={rightBoundary}");

            processimg4 = new Mat(sourceimg4, new Rectangle(leftBoundary2, bottom_y - 1740, rightBoundary - leftBoundary2, 1740)).Clone();
            CvInvoke.Imwrite("processimg4.bmp", processimg4);
            #endregion

            #region 破開區02
            // 建立副本避免影響原圖
            Mat processimg_BreakArea_02 = sourceimg3.Clone();
            // 灰階化
            CvInvoke.CvtColor(processimg_BreakArea_02, processimg_BreakArea_02, ColorConversion.Bgr2Gray);
            // Otsu二值化
            CvInvoke.Threshold(processimg_BreakArea_02, processimg_BreakArea_02, 0, 255, ThresholdType.Otsu);

            int BreakArea_02_top_y = 0;
            int BreakArea_02_button_y = 0;
            int BreakArea_02_LeftBoundary = 0;
            int BreakArea_02_RightBoundary = 0;

            unsafe
            {
                byte* ptr = (byte*)processimg_BreakArea_02.DataPointer;
                int step = processimg_BreakArea_02.Step;

                // 找上邊界 - 從 y=0 由上往下掃
                bool isTopYFound = false;
                for (int y = 780; y < processimg_BreakArea_02.Height; y++)
                {
                    if (isTopYFound) break;
                    int blackPixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                            if (blackPixels > (int)((2400 - 800) * 0.95))
                            {
                                BreakArea_02_top_y = y;
                                isTopYFound = true;
                                break;
                            }
                        }
                    }
                }

                // 找下邊界 - 從 y=BreakArea_02_top_y-300 由下往上掃
                bool isButtonYFound = false;
                for (int y = BreakArea_02_top_y + 300; y > 0; y--)
                {
                    if (isButtonYFound) break;
                    int blackPixels = 0;
                    for (int x = 800; x < 2400; x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            blackPixels++;
                            if (blackPixels > (int)((2400 - 800) * 0.95))
                            {
                                BreakArea_02_button_y = y + 11;
                                isButtonYFound = true;
                                break;
                            }
                        }
                    }
                }

                // 找左邊界 - 從 x=1800 由右往左掃
                bool isLeftBoundaryFound = false;
                for (int x = 1800; x > 0; x--)
                {
                    if (isLeftBoundaryFound) break;
                    int whitePixels = 0;
                    for (int y = BreakArea_02_top_y; y < BreakArea_02_button_y; y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 255)
                        {
                            whitePixels++;
                        }
                    }
                    if (whitePixels > (int)((BreakArea_02_button_y - BreakArea_02_top_y) * 0.8))
                    {
                        BreakArea_02_LeftBoundary = x;
                        isLeftBoundaryFound = true;
                    }
                }

                // 右邊界直接推算
                BreakArea_02_RightBoundary = BreakArea_02_LeftBoundary + 2094;
            }

            Debug.WriteLine($"BreakArea_02: top_y={BreakArea_02_top_y}, button_y={BreakArea_02_button_y}, Left={BreakArea_02_LeftBoundary}, Right={BreakArea_02_RightBoundary}");

            // 裁切原圖
            Mat BreakArea_02_crop = new Mat();
            if (BreakArea_02_LeftBoundary < BreakArea_02_RightBoundary &&
                BreakArea_02_top_y < BreakArea_02_button_y)
            {
                Rectangle BreakArea_02_ROI = new Rectangle(
                    BreakArea_02_LeftBoundary,
                    BreakArea_02_top_y,
                    BreakArea_02_RightBoundary - BreakArea_02_LeftBoundary,
                    BreakArea_02_button_y - BreakArea_02_top_y
                );
                BreakArea_02_crop = new Mat(sourceimg3, BreakArea_02_ROI).Clone();  // 用原圖裁切
                CvInvoke.Imwrite("BreakArea_02_crop.bmp", BreakArea_02_crop);
            }
            #endregion

            #region 圖片拼接
            // 檢查兩張圖片的寬度是否相同，如果不同則resize
            Mat resizedImg3 = processimg3.Clone();
            Mat resizedImg4 = processimg4.Clone();
            
            int targetWidth = Math.Max(processimg3.Width, processimg4.Width);

            // 如果寬度不同，將較小的圖片resize到較大的寬度
            if (processimg3.Width != processimg4.Width)
            {
                if (processimg3.Width < targetWidth)
                {
                    CvInvoke.Resize(processimg3, resizedImg3, new Size(targetWidth, processimg3.Height), 0, 0, Inter.Linear);
                    Debug.WriteLine($"Resized processimg3 from {processimg3.Width}x{processimg3.Height} to {targetWidth}x{processimg3.Height}");
                }

                if (processimg4.Width < targetWidth)
                {
                    CvInvoke.Resize(processimg4, resizedImg4, new Size(targetWidth, processimg4.Height), 0, 0, Inter.Linear);
                    Debug.WriteLine($"Resized processimg4 from {processimg4.Width}x{processimg4.Height} to {targetWidth}x{processimg4.Height}");
                }
            }

            // 建立拼接後的圖片（上下拼接）
            int totalHeight = resizedImg3.Height + resizedImg4.Height;
            Mat mergedImage = new Mat(new Size(targetWidth, totalHeight), resizedImg3.Depth, resizedImg3.NumberOfChannels);

            // 將 processimg3 複製到上半部
            Rectangle upperRegion = new Rectangle(0, 0, targetWidth, resizedImg3.Height);
            Mat upperRoi = new Mat(mergedImage, upperRegion);
            resizedImg3.CopyTo(upperRoi);

            // 將 processimg4 複製到下半部
            Rectangle lowerRegion = new Rectangle(0, resizedImg3.Height, targetWidth, resizedImg4.Height);
            Mat lowerRoi = new Mat(mergedImage, lowerRegion);
            resizedImg4.CopyTo(lowerRoi);

            // 儲存拼接結果
            CvInvoke.Imwrite("merged_image.bmp", mergedImage);
            Debug.WriteLine($"成功拼接圖片，最終尺寸：{targetWidth}x{totalHeight}");
            #endregion
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
            Mat sourceimg = SourceImg.Clone();
            CvInvoke.CvtColor(sourceimg, sourceimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(sourceimg, sourceimg, 120, 255, ThresholdType.Binary);
            if (saveoptions.saveoption.Roi_AreaCrop)
            {
                CvInvoke.Imwrite("Fontarea_1_binary.bmp", sourceimg);
            }
            int rightx = 0;
            bool is_rightx_found = false;
            double rightx_threshold = 0.8; // 水平黑色像素的百分比閾值

            int leftx = 0;
            bool is_leftx_found = false;
            double leftx_threshold = 0.8; // 水平黑色像素的百分比閾值

            int middley = 0;
            bool is_middley_found = false;
            double middley_threshold = 0.8;
            // 記錄每列黑色像素數量的數據
            var blackPixelCounts = new List<int>();

            unsafe
            {
                int width = sourceimg.Width;
                int height = sourceimg.Height;
                int blackpixels = 0;
                byte* ptr = (byte*)sourceimg.DataPointer;

                // 從中間往右找
                for (int i = sourceimg.Width / 2; i < sourceimg.Width; i++)  // 從中間往右邊找
                {
                    if (is_rightx_found)
                    {
                        break;
                    }
                    blackpixels = 0;
                    for (int j = 0; j < sourceimg.Height; j++)
                    {
                        byte* currentPixel = ptr + (j * sourceimg.Width + i);
                        if (*currentPixel == 0)  // 黑色像素
                        {
                            blackpixels++;
                            if (blackpixels > (sourceimg.Height * rightx_threshold))
                            {
                                rightx = i;
                                is_rightx_found = true;
                                break;
                            }
                        }
                    }
                }

                // 從中間往左找
                for (int i = sourceimg.Width / 2; i >= 0; i--)  // 從中間往左邊找
                {
                    if (is_leftx_found)
                    {
                        break;
                    }
                    blackpixels = 0;
                    for (int j = 0; j < sourceimg.Height; j++)
                    {
                        byte* currentPixel = ptr + (j * sourceimg.Width + i);
                        if (*currentPixel == 0)  // 黑色像素
                        {
                            blackpixels++;
                            if (blackpixels > (sourceimg.Height * leftx_threshold))
                            {
                                leftx = i;
                                is_leftx_found = true;
                                break;
                            }
                        }
                    }
                }

                //從底部往上找
                for (int y = sourceimg.Height - 1; y >= 0; y--)
                {
                    if (is_middley_found)
                    {
                        break;
                    }
                    blackpixels = 0;
                    // 檢查每一列的x範圍（從leftx到rightx）
                    for (int x = leftx; x <= rightx; x++)
                    {
                        byte* currentPixel = ptr + (y * sourceimg.Width + x);
                        if (*currentPixel == 0)  // 黑色像素
                        {
                            blackpixels++;
                            // 如果黑色像素數量大於所需的閾值，則輸出該y位置
                            if (blackpixels > (int)((rightx - leftx) * middley_threshold))
                            {
                                middley = y;
                                is_middley_found = true;
                                break;
                            }
                        }
                    }
                }
            }

            //Debug.WriteLine($"leftx={leftx}, rightx={rightx},middley={middley}");
            Mat yin_fontarea = new Mat(SourceImg, new Rectangle(leftx, 0, rightx - leftx, middley));    //+5是為了偏移邊界白色雜訊
            Mat yang_fontarea = new Mat(SourceImg, new Rectangle(leftx, middley, rightx - leftx, SourceImg.Height - middley));   //+5是為了偏移邊界黑色雜訊
            Mat fullness_area = new Mat(SourceImg, new Rectangle(rightx, 0 , SourceImg.Width - rightx, SourceImg.Height));
            if (saveoptions.saveoption.Roi_AreaCrop)
            {
                CvInvoke.Imwrite("YinArea_1.bmp", yin_fontarea);
                CvInvoke.Imwrite("YangArea_1.bmp", yang_fontarea);
                CvInvoke.Imwrite("fullnessArea_1.bmp", fullness_area);
            }
            return (yin_fontarea, yang_fontarea, fullness_area);

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
                            rightx = x+1;   //找到後往右邊補1pixel
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
            Mat resultimg = new Mat(sourceimg, new Rectangle(leftx, 0, rightx-leftx, sourceimg.Height));
            return (resultimg, defectpixels);
        }
        /// <summary>
        /// 陽版修邊+初步計算塞版
        /// </summary>
        public static (Mat, int) Yang_Calc_OutsideBlock_ResizeFontImg(Mat sourceimg, string fontsize, int templatewidth)
        {
            //Debug.WriteLine($"test{fontsize}");
            Dictionary<string, double> fontSizeToThreshold = new Dictionary<string, double>
            {
                { "3pt", 0.05 },
                { "4pt", 0.05 },
                { "5pt", 0.05 },
                { "6pt", 0.05 },
                { "7pt", 0.05 },
                { "8pt", 0.05 },
                { "9pt", 0.05 },
                { "10pt", 0.05 },
                { "11pt", 0.05 },
                { "12pt", 0.05 }
            };
            double threshold;
            fontSizeToThreshold.TryGetValue(fontsize, out threshold);
            //計算模板字體總pixels
            int blockpixels = 0, blackpixels = 0;
            int leftx = 0, rightx = sourceimg.Width - 1;
            bool is_leftx_find = false, is_right_find = false;
            unsafe
            {
                byte* ptr = (byte*)sourceimg.DataPointer;
                //從左到右掃描
                for (int x = 0; x < sourceimg.Width; x++)
                {
                    blockpixels += blackpixels;
                    blackpixels = 0;
                    if (is_leftx_find)
                    {
                        break;
                    }

                    for (int y = 0; y < sourceimg.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * sourceimg.Width + x);
                        if (*currentPixel == 0)  // 黑色像素
                        {
                            blackpixels++;
                        }
                        if (blackpixels > sourceimg.Height * threshold)
                        {
                            is_leftx_find = true;
                            leftx = x;
                            break;
                        }
                    }
                }
                ptr = (byte*)sourceimg.DataPointer;
                rightx = Math.Min(rightx, leftx + templatewidth);//確保不超出邊界
                for (int x = sourceimg.Width - 1; x >= rightx; x--) // 從右往左掃描遇到left+模版寬就停
                {
                    for (int y = 0; y < sourceimg.Height; y++)
                    {
                        byte* currentPixel = ptr + (y * sourceimg.Width + x);
                        if (*currentPixel == 0)  // 黑色像素
                        {
                            blockpixels++;
                            //blackpixels++;
                        }

                    }
                }
            }
            //Debug.WriteLine($"陽版字體{fontsize}塞版數為{blockpixels}");
            Mat resultimg = new Mat(sourceimg, new Rectangle(leftx, 0, rightx-leftx, sourceimg.Height));
            return (resultimg, blockpixels);
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
                        maxShift: 5 ,
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
                    Mat emptyImage = new Mat(yinimage.Size, DepthType.Cv8U, 1);
                    emptyImage.SetTo(new MCvScalar(255)); // 設定為白色
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
        public static (Font_Imgs,Font_Results,Font_Imgs,Font_Results) Yin_Calculate(List<FontInfo> yin_fontInfos, List<Mat> Yin_Image_List, List<TemplateData> YinTemplates) // 回傳塞版 & 缺燙結果圖和數值
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
                    (yin_block_img, block_percentage, yin_defect_img, defect_percentage) = Yin_Calc_Single(yin_fontInfos[i], Yin_Image_List[i], YinTemplates[i]);
                    CvInvoke.Imwrite($"Yin_block_{yin_fontInfos[i].Fontname}.bmp", yin_block_img);
                    CvInvoke.Imwrite($"Yin_defect_{yin_fontInfos[i].Fontname}.bmp", yin_defect_img);
                }
                catch(Exception ex)
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
            yin_defect_results.Defect_Total_Percentage = total_defectpixels / total_defectpixels_templates *100; //(陰版)總缺燙百分比
            yin_block_results.Block_Total_Percentage = total_blockpixels / total_blockpixels_templates * 100; //(陰版)總塞版百分比
            return (yin_block_imgs, yin_block_results, yin_defect_imgs, yin_defect_results);
        }
        /// <summary>
        /// 陰版計算單字體
        /// </summary>
        public static (Mat,double , Mat ,double ) Yin_Calc_Single(FontInfo fontInfo, Mat yin_font_image,TemplateData YinTemplate)
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
            catch(Exception ex)
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
                    Mat emptyImage = new Mat(yangimage.Size, DepthType.Cv8U, 1);
                    emptyImage.SetTo(new MCvScalar(255)); // 設定為白色
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
                    (yang_image, block_outsidepixels) = OCT_AlgorithmHelper.Yang_Calc_OutsideBlock_ResizeFontImg(yang_image, fontName, YangTemplates[i].Width);
                    //Debug.WriteLine($"Yang_outsidepixels{fontName}={block_outsidepixels}");   //觀看補償量
                    if (saveoptions.saveoption.Yang_CalcOutsideBlock)
                    {
                        CvInvoke.Imwrite($"Yang_Resizefont_{fontName}.bmp", yang_image);
                    }
                    if (saveoptions.saveoption.MakeTemplates)// 2026.03.13 張植竣新增模板儲存功能 
                    {
                        string templatesDir = cardType == "雙銅"
                            ? "template_double sided coated paper"
                            : "template_white card";

                        if (!Directory.Exists(templatesDir))
                            Directory.CreateDirectory(templatesDir);

                        string templatePath = Path.Combine(templatesDir, $"Yang_template_{fontName}.bmp");
                        yang_image.Save(templatePath);
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
        public static (Font_Imgs,Font_Results,Font_Imgs,Font_Results) Yang_Calculate(List<FontInfo> yang_fontInfos,List<Mat> Yang_Image_List,List<TemplateData> YangTemplates)
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
                    (yang_block_img, block_percentage, yang_defect_img, defect_percentage) = Yang_Calc_Single(yang_fontInfos[i], Yang_Image_List[i], YangTemplates[i]);
                    CvInvoke.Imwrite($"Yang_block_{yang_fontInfos[i].Fontname}.bmp", yang_block_img);
                    CvInvoke.Imwrite($"Yang_defect_{yang_fontInfos[i].Fontname}.bmp", yang_defect_img);
                }                       
                catch(Exception ex)
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
        public static (Mat,double,Mat,double) Yang_Calc_Single(FontInfo fontInfo, Mat yang_font_image, TemplateData YangTemplate)
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
            catch(Exception ex)
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
        public async static  Task<(MeshP_Imgs, MeshP_Imgs, MeshP_Results, Mat)?> 
            MeshP_anylz(
                Mat SourceImg1,
                Mat SourceImg2,
                List<double> accurate_thresholds, 
                OCT_Parameters_PrintType current_printtype_params, 
                OCT_Parameters_SaveOptions saveoptions
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
            catch(Exception ex)
            {
                OCT_LogHelper.WriteLog(LogLevel.Error,Page.O,"網點區裁切錯誤",ex.ToString());
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
            List<Mat> meshp_areas_threshold = meshp_areas.Select(mat => mat.Clone()).ToList();  //先複製一份避免共用記憶體

            meshp_areas_threshold = Meshp_areas_threshold(meshp_areas_threshold, accurate_thresholds); //二值化
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
            return (meshp_imgs, meshp_result_imgs, meshp_results, breakArea_01_crop);
        } 
        /// <summary> 
        /// 陰版分析
        /// </summary>
        public static (Font_Imgs,Font_Imgs, Font_Results, Font_Imgs, Font_Results,List<FontInfo>)? 
            Yin_analz(
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
            List<Rectangle> yin_ranges = new List<Rectangle>
            {
                new Rectangle(current_printtype_params.font_range_3pt, 0, current_printtype_params.font_range_4pt, yinimage.Height),    // 3pt
                new Rectangle(current_printtype_params.font_range_4pt, 0, current_printtype_params.font_range_5pt - current_printtype_params.font_range_4pt, yinimage.Height),   // 4pt
                new Rectangle(current_printtype_params.font_range_5pt, 0, current_printtype_params.font_range_6pt - current_printtype_params.font_range_5pt, yinimage.Height), // 5pt
                new Rectangle(current_printtype_params.font_range_6pt, 0, current_printtype_params.font_range_7pt - current_printtype_params.font_range_6pt, yinimage.Height), // 6pt
                new Rectangle(current_printtype_params.font_range_7pt, 0, current_printtype_params.font_range_8pt - current_printtype_params.font_range_7pt, yinimage.Height), // 7pt
                new Rectangle(current_printtype_params.font_range_8pt, 0, current_printtype_params.font_range_9pt - current_printtype_params.font_range_8pt, yinimage.Height), // 8pt
                new Rectangle(current_printtype_params.font_range_9pt, 0, current_printtype_params.font_range_10pt - current_printtype_params.font_range_9pt,yinimage.Height), // 9pt
                new Rectangle(current_printtype_params.font_range_10pt, 0, current_printtype_params.font_range_11pt - current_printtype_params.font_range_10pt, yinimage.Height), // 10pt
                new Rectangle(current_printtype_params.font_range_11pt, 0, current_printtype_params.font_range_12pt - current_printtype_params.font_range_11pt, yinimage.Height), // 11pt
                new Rectangle(current_printtype_params.font_range_12pt, 0, yinimage.Width - current_printtype_params.font_range_12pt, yinimage.Height)  // 12pt
            };
            List<List<Rectangle>> yin_lists = ClassifyFontRegions(yin_fontRectangles, yin_ranges); // 把字體框框分到對應字級

            //裁切字體
            Font_Imgs yin_Imgs = new Font_Imgs(); // 原始裁切圖（人工檢查「裁切對不對」、UI 顯示）
            List<Mat> Yin_ProcessImage_List = new List<Mat>(); // 「真正要拿去算的字圖」
            List<FontInfo> yin_fontInfos = new List<FontInfo>(); // 字體資訊列表(記錄字在哪一區、計算百分比時用、給後面缺燙/塞版統計當依據)
            // yin_lists：每個字級的字框集合；yinimage：原始圖；YinTemplates：模板資料；current_params：各字級門檻；saveoptions：是否存圖
            (yin_Imgs,Yin_ProcessImage_List, yin_fontInfos) = PrepareYinFontCrops(yin_lists, yinimage, YinTemplates, current_params, saveoptions, cardType);

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
            catch (Exception ex) {
                return null;
            }
        }
        /// <summary>
        /// 陽版分析
        /// </summary>

        public static (Font_Imgs, Font_Imgs, Font_Results, Font_Imgs, Font_Results, List<FontInfo>, Mat)?
    Yang_analz(Mat yangimage, List<TemplateData> YangTemplates, OCT_Parameters_CardType current_params, OCT_Parameters_PrintType current_printtype_params, OCT_Parameters_SaveOptions saveoptions, string cardType,
                TaskCompletionSource<Mat> breakArea03Ready = null)  // ← 新增參數
        {
            Mat yang_processimg = yangimage.Clone();
            CvInvoke.CvtColor(yang_processimg, yang_processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(yang_processimg, yang_processimg, 230, 255, ThresholdType.BinaryInv);
            if (saveoptions.saveoption.Roi_AreaCrop)
            {
                CvInvoke.Imwrite("YangArea_2.bmp", yang_processimg);
            }
            List<Rectangle> yang_fontRectangles = DetectConnectedComponents(yang_processimg);
            List<Rectangle> yang_ranges = new List<Rectangle>
            {
                new Rectangle(current_printtype_params.font_range_3pt, 0, current_printtype_params.font_range_4pt, yangimage.Height),    // 3pt
                new Rectangle(current_printtype_params.font_range_4pt, 0, current_printtype_params.font_range_5pt - current_printtype_params.font_range_4pt, yangimage.Height),   // 4pt
                new Rectangle(current_printtype_params.font_range_5pt, 0, current_printtype_params.font_range_6pt - current_printtype_params.font_range_5pt, yangimage.Height), // 5pt
                new Rectangle(current_printtype_params.font_range_6pt, 0, current_printtype_params.font_range_7pt - current_printtype_params.font_range_6pt, yangimage.Height), // 6pt
                new Rectangle(current_printtype_params.font_range_7pt, 0, current_printtype_params.font_range_8pt - current_printtype_params.font_range_7pt, yangimage.Height), // 7pt
                new Rectangle(current_printtype_params.font_range_8pt, 0, current_printtype_params.font_range_9pt - current_printtype_params.font_range_8pt, yangimage.Height), // 8pt
                new Rectangle(current_printtype_params.font_range_9pt, 0, current_printtype_params.font_range_10pt - current_printtype_params.font_range_9pt, yangimage.Height), // 9pt
                new Rectangle(current_printtype_params.font_range_10pt, 0, current_printtype_params.font_range_11pt - current_printtype_params.font_range_10pt, yangimage.Height), // 10pt
                new Rectangle(current_printtype_params.font_range_11pt, 0, current_printtype_params.font_range_12pt - current_printtype_params.font_range_11pt, yangimage.Height), // 11pt
                new Rectangle(current_printtype_params.font_range_12pt, 0, yangimage.Width - current_printtype_params.font_range_12pt, yangimage.Height)  // 12pt
            };
            List<List<Rectangle>> yang_lists = ClassifyFontRegions(yang_fontRectangles, yang_ranges);
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
        public static void BreakArea_Process(Mat breakArea01, Mat breakArea02, Mat breakArea03,BreakArea_Parameter breakAreaParams)
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

                for (int x = 0; x < meshp_result_img.Width; x++)
                {
                    for (int y = 0; y < meshp_result_img.Height; y++)
                    {
                        // 獲取原始方式的指針位置
                        byte* current_ptr = ptr + (x + (meshp_result_img.Width * y));
                        // 獲取對應位置的遮罩值
                        byte* current_mask_ptr = maskPtr + (x + (mask.Width * y));

                        // 檢查該像素是否在排除區域外（遮罩值為255）
                        if (*current_mask_ptr == 255)
                        {
                            // 這個像素不在任何排除區域內
                            total_pixels++;

                            // 檢查它是否為黑色像素
                            if (*current_ptr == 0)
                            {
                                blackpixels++;
                            }
                        }
                    }
                }
            }

            // 計算標準像素數
            double total_eliminate_areas = single_area_info.Regions.Sum(x => x.Area);   //計算排除總區域
            int standard_pixels = Convert.ToInt32((total_pixels- total_eliminate_areas) * meshp_index);
            double meshp_percentage = (double)(blackpixels - standard_pixels) / (double)total_pixels * 100;
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

            return (MeshP_Imgs, MeshP_Result_Imgs, MeshP_Results);
        }
        /// <summary>
        /// 重新分析陰版單字體
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
            OCT_Parameters_SaveOptions saveoptions
            )
        {
            string fontName = single_area_info.Name;
            double threshold = 0.0;
            Mat processimg = new Mat();        
            int index = 0;
            switch (fontName)
            {
                case "3pt":
                    processimg = yin_imgs._3pt;
                    threshold = currentcardtype_params.yin_parameter._3pt;
                    index = 0;
                    break;
                case "4pt":
                    processimg = yin_imgs._4pt;
                    threshold = currentcardtype_params.yin_parameter._4pt;
                    index = 1;
                    break;
                case "5pt":
                    processimg = yin_imgs._5pt;
                    threshold = currentcardtype_params.yin_parameter._5pt;
                    index = 2;
                    break;
                case "6pt":
                    processimg = yin_imgs._6pt;
                    threshold = currentcardtype_params.yin_parameter._6pt;
                    index = 3;
                    break;
                case "7pt":
                    processimg = yin_imgs._7pt;
                    threshold = currentcardtype_params.yin_parameter._7pt;
                    index = 4;
                    break;
                case "8pt":
                    processimg = yin_imgs._8pt;
                    threshold = currentcardtype_params.yin_parameter._8pt;
                    index = 5;
                    break;
                case "9pt":
                    processimg = yin_imgs._9pt;
                    threshold = currentcardtype_params.yin_parameter._9pt;
                    index = 6;
                    break;
                case "10pt":
                    processimg = yin_imgs._10pt;
                    threshold = currentcardtype_params.yin_parameter._10pt;
                    index = 7;
                    break;
                case "11pt":
                    processimg = yin_imgs._11pt;
                    threshold = currentcardtype_params.yin_parameter._11pt;
                    index = 8;
                    break;
                case "12pt":
                    processimg = yin_imgs._12pt;
                    threshold = currentcardtype_params.yin_parameter._12pt;
                    index = 9;
                    break;
            }
            //processimg.Save($"test0.bmp");
            Mat filter_yin_image = Yin_filternoise(processimg.Clone(), fontName);
            //filter_yin_image.Save($"test1.bmp");
            CvInvoke.CvtColor(filter_yin_image, filter_yin_image, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(filter_yin_image, filter_yin_image, threshold, 255, ThresholdType.Binary); //給廠商調的參數
            if (single_area_info.Regions.Count != 0)
            {
                //輪廓補黑 
                foreach (Region region in single_area_info.Regions)
                {
                    CvInvoke.FillPoly(filter_yin_image, new VectorOfVectorOfPoint(region.Contour), new MCvScalar(0));
                }
            }

            filter_yin_image = RemoveIsolatedWhitePixels(filter_yin_image);

            int defect_outsidepixels = 0;
            (filter_yin_image, defect_outsidepixels) = Yin_Calc_OutsideBlock_ResizeFontImg(filter_yin_image, fontName, yintemplates[index].Width);

            yin_font_infos[index].Defect_Pixels = 0;  //將之前計算結果歸零
            yin_font_infos[index].Block_Pixels = 0;   //將之前計算結果歸零
            yin_font_infos[index].Defect_Pixels = defect_outsidepixels; //將剛剛初步計算缺燙數加入

            //字體對位校正
            CvInvoke.Resize(filter_yin_image, filter_yin_image, yintemplates[index].Image.Size, 0, 0, Inter.Nearest);

            filter_yin_image = FontImg_Correction(filter_yin_image, yintemplates[index].Image, true);

            Mat yin_defect_img = new Mat();
            Mat yin_block_img = new Mat();
            double defect_percentage;
            double block_percentage;
            (yin_block_img, block_percentage, yin_defect_img, defect_percentage) = Yin_Calc_Single(yin_font_infos[index], filter_yin_image, yintemplates[index]);
            
            switch (fontName)
            {
                case "3pt":
                    yin_defect_imgs._3pt = yin_defect_img;
                    yin_defect_results._3pt = defect_percentage;
                    yin_block_imgs._3pt = yin_block_img;
                    yin_block_results._3pt = block_percentage;
                    break;
                case "4pt":
                    yin_defect_imgs._4pt = yin_defect_img;
                    yin_defect_results._4pt = defect_percentage;
                    yin_block_imgs._4pt = yin_block_img;
                    yin_block_results._4pt = block_percentage;
                    break;
                case "5pt":
                    yin_defect_imgs._5pt = yin_defect_img;
                    yin_defect_results._5pt = defect_percentage;
                    yin_block_imgs._5pt = yin_block_img;
                    yin_block_results._5pt = block_percentage;
                    break;
                case "6pt":
                    yin_defect_imgs._6pt = yin_defect_img;
                    yin_defect_results._6pt = defect_percentage;
                    yin_block_imgs._6pt = yin_block_img;
                    yin_block_results._6pt = block_percentage;
                    break;
                case "7pt":
                    yin_defect_imgs._7pt = yin_defect_img;
                    yin_defect_results._7pt = defect_percentage;
                    yin_block_imgs._7pt = yin_block_img;
                    yin_block_results._7pt = block_percentage;
                    break;
                case "8pt":
                    yin_defect_imgs._8pt = yin_defect_img;
                    yin_defect_results._8pt = defect_percentage;
                    yin_block_imgs._8pt = yin_block_img;
                    yin_block_results._8pt = block_percentage;
                    break;
                case "9pt":
                    yin_defect_imgs._9pt = yin_defect_img;
                    yin_defect_results._9pt = defect_percentage;
                    yin_block_imgs._9pt = yin_block_img;
                    yin_block_results._9pt = block_percentage;
                    break;
                case "10pt":
                    yin_defect_imgs._10pt = yin_defect_img;
                    yin_defect_results._10pt = defect_percentage;
                    yin_block_imgs._10pt = yin_block_img;
                    yin_block_results._10pt = block_percentage;
                    break;
                case "11pt":
                    yin_defect_imgs._11pt = yin_defect_img;
                    yin_defect_results._11pt = defect_percentage;
                    yin_block_imgs._11pt = yin_block_img;
                    yin_block_results._11pt = block_percentage;
                    break;
                case "12pt":
                    yin_defect_imgs._12pt = yin_defect_img;
                    yin_defect_results._12pt = defect_percentage;
                    yin_block_imgs._12pt = yin_block_img;
                    yin_block_results._12pt = block_percentage;
                    break;

            }   //計算結果寫入到對應結果
            return (yin_imgs, yin_block_imgs, yin_block_results, yin_defect_imgs, yin_defect_results, yin_font_infos);
        }
        /// <summary>
        /// 重新分析陽版單字體
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
            OCT_Parameters_SaveOptions saveoptions
            )
        {
            string fontName = single_area_info.Name;
            double threshold = 0.0;
            Mat processimg = new Mat();
            int index = 0;
            switch (fontName)
            {
                case "3pt":
                    processimg = yang_imgs._3pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._3pt;
                    index = 0;
                    break;
                case "4pt":
                    processimg = yang_imgs._4pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._4pt;
                    index = 1;
                    break;
                case "5pt":
                    processimg = yang_imgs._5pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._5pt;
                    index = 2;
                    break;
                case "6pt":
                    processimg = yang_imgs._6pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._6pt;
                    index = 3;
                    break;
                case "7pt":
                    processimg = yang_imgs._7pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._7pt;
                    index = 4;
                    break;
                case "8pt":
                    processimg = yang_imgs._8pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._8pt;
                    index = 5;
                    break;
                case "9pt":
                    processimg = yang_imgs._9pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._9pt;
                    index = 6;
                    break;
                case "10pt":
                    processimg = yang_imgs._10pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._10pt;
                    index = 7;
                    break;
                case "11pt":
                    processimg = yang_imgs._11pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._11pt;
                    index = 8;
                    break;
                case "12pt":
                    processimg = yang_imgs._12pt.Clone();
                    threshold = currentcardtype_params.yang_parameter._12pt;
                    index = 9;
                    break;
            }
            //連通區域裁切後進來
            CvInvoke.CvtColor(processimg, processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(processimg, processimg, threshold, 255, ThresholdType.Binary); //給廠商調的參數

            //陽版字體補黑
            if (single_area_info.Regions.Count != 0)
            {
                //輪廓補黑 
                foreach (Region region in single_area_info.Regions)
                {
                    CvInvoke.FillPoly(processimg, new VectorOfVectorOfPoint(region.Contour), new MCvScalar(0));
                }
            }

            int block_outsidepixels = 0;
            (processimg, block_outsidepixels) = Yang_Calc_OutsideBlock_ResizeFontImg(processimg, fontName, yangtemplates[index].Width);
            yang_font_infos[index].Defect_Pixels = 0;  //將之前計算結果歸零
            yang_font_infos[index].Block_Pixels = 0;   //將之前計算結果歸零
            yang_font_infos[index].Block_Pixels = block_outsidepixels; //將剛剛初步計算缺燙數加入

            // 尺寸不一致時進行 Resize
            if (processimg.Size != yangtemplates[index].Image.Size)
            {
                CvInvoke.Resize(processimg, processimg, yangtemplates[index].Image.Size, 0, 0, Inter.Nearest);
            }
            //字體對位校正
            processimg = FontImg_Correction(processimg, yangtemplates[index].Image, false);
            Mat yang_defect_img = new Mat();
            Mat yang_block_img = new Mat();
            double defect_percentage;
            double block_percentage;
            try
            {
                (yang_block_img, block_percentage, yang_defect_img, defect_percentage) = Yang_Calc_Single(yang_font_infos[index], processimg, yangtemplates[index]);
                switch (fontName)
                {
                    case "3pt":
                        yang_defect_imgs._3pt = yang_defect_img;
                        yang_defect_results._3pt = defect_percentage;
                        yang_block_imgs._3pt = yang_block_img;
                        yang_block_results._3pt = block_percentage;
                        break;
                    case "4pt":
                        yang_defect_imgs._4pt = yang_defect_img;
                        yang_defect_results._4pt = defect_percentage;
                        yang_block_imgs._4pt = yang_block_img;
                        yang_block_results._4pt = block_percentage;
                        break;
                    case "5pt":
                        yang_defect_imgs._5pt = yang_defect_img;
                        yang_defect_results._5pt = defect_percentage;
                        yang_block_imgs._5pt = yang_block_img;
                        yang_block_results._5pt = block_percentage;
                        break;
                    case "6pt":
                        yang_defect_imgs._6pt = yang_defect_img;
                        yang_defect_results._6pt = defect_percentage;
                        yang_block_imgs._6pt = yang_block_img;
                        yang_block_results._6pt = block_percentage;
                        break;
                    case "7pt":
                        yang_defect_imgs._7pt = yang_defect_img;
                        yang_defect_results._7pt = defect_percentage;
                        yang_block_imgs._7pt = yang_block_img;
                        yang_block_results._7pt = block_percentage;
                        break;
                    case "8pt":
                        yang_defect_imgs._8pt = yang_defect_img;
                        yang_defect_results._8pt = defect_percentage;
                        yang_block_imgs._8pt = yang_block_img;
                        yang_block_results._8pt = block_percentage;
                        break;
                    case "9pt":
                        yang_defect_imgs._9pt = yang_defect_img;
                        yang_defect_results._9pt = defect_percentage;
                        yang_block_imgs._9pt = yang_block_img;
                        yang_block_results._9pt = block_percentage;
                        break;
                    case "10pt":
                        yang_defect_imgs._10pt = yang_defect_img;
                        yang_defect_results._10pt = defect_percentage;
                        yang_block_imgs._10pt = yang_block_img;
                        yang_block_results._10pt = block_percentage;
                        break;
                    case "11pt":
                        yang_defect_imgs._11pt = yang_defect_img;
                        yang_defect_results._11pt = defect_percentage;
                        yang_block_imgs._11pt = yang_block_img;
                        yang_block_results._11pt = block_percentage;
                        break;
                    case "12pt":
                        yang_defect_imgs._12pt = yang_defect_img;
                        yang_defect_results._12pt = defect_percentage;
                        yang_block_imgs._12pt = yang_block_img;
                        yang_block_results._12pt = block_percentage;
                        break;
                }
                return (yang_imgs, yang_block_imgs, yang_block_results, yang_defect_imgs, yang_defect_results, yang_font_infos);
            }
            catch (Exception ex)
            {
                throw;
            }

        }
        /// <summary>
        /// 重新分析飽滿區
        /// </summary>
        public static Fullness_Results Single_Fullness_analz(
            Fullness_Results fullness_results,
            SingleAreaInfo single_area_info,
            OCT_Parameters_CardType current_params,
            OCT_Parameters_SaveOptions saveoptions
)
        {
            Mat fullness_processimg = single_area_info.Image.Clone();
            CvInvoke.CvtColor(fullness_processimg, fullness_processimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(fullness_processimg, fullness_processimg, current_params.fullness_parameter.Threshold, 255, ThresholdType.Binary);

            if (saveoptions.saveoption.Fullness_Threshold)
            {
                CvInvoke.Imwrite("Fullness_2_threshold.bmp", fullness_processimg);
            }

            // 排除使用者框選的區域（與 MeshP 的遮罩邏輯一致）
            if (single_area_info.Regions.Count != 0)
            {
                foreach (Region region in single_area_info.Regions)
                {
                    CvInvoke.FillPoly(fullness_processimg, new VectorOfVectorOfPoint(region.Contour), new MCvScalar(0));
                }
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

            fullness_results.Defect_Result = (double)whitepixels / totalPixels * 100;
            return fullness_results;
        }
        #endregion

    }
}
