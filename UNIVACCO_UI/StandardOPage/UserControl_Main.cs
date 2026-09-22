using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.Diagnostics;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using System.IO;
using System.Threading;
using System.Drawing.Imaging;
using NPOI.XWPF.UserModel;
using Excel = Microsoft.Office.Interop.Excel;
using static NPOI.HSSF.Util.HSSFColor;
using MySqlX.XDevAPI.Common;
using StandardOPage;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Windows.Media.Media3D;
using new_UNIVACCO;
using NPOI.OpenXml4Net.OPC.Internal;
using NPOI.SS.Formula.PTG;

namespace UNIVACCO_UI
{
    public partial class UserControl_Main : UserControl
    {
        UNIVACCO MainPage;
        Setting_O_Window StandardOParameterSetting;
        LightCorrection LightCorrectionPage;
        private string LogString;
        private bool ReadSourceImgFlag = false;
        private bool SaveResultFlag = false;
        public bool IsValveEnabled;

        public OCT_SaveFileHelper OCT_SaveFileHelper; // 存檔助手
        public SaveData SaveData = new SaveData();   // 儲存資料
        public OCT_DatabaseHelper MeshP_Param;       // 資料庫助手
        public List<OCT_ParameterMeshP> temp_standards; // 臨時參數列表
        public OCT_ParameterMeshP temp_standard;    // 臨時標準
        public OCT_ParameterMeshP abs_standard;     // 絕對標準
        public OCT_ParameterMeshP Current_Standard; // 現在標準
        public OCT_Parameters_CardType WC_Params;   // 白卡參數
        public OCT_Parameters_CardType DC_Params;   // 雙銅參數
        public OCT_Parameters_CardType CurrentCardType_Params;     // 現在卡型參數
        public OCT_Parameters_PrintType LabPrint_Params;           // 內側印鑑參數
        public OCT_Parameters_PrintType OffsetPrint_Params;        // 外側印鑑參數
        public OCT_Parameters_PrintType CurrentPrintType_Params;   // 目前印鑑參數
        public OCT_Rules OCT_Rules;                                // 判定規則
        public OCT_Parameters_SaveOptions SaveOptions;             // 選擇儲存演算法圖片
        public List<double> meshp_current_thresholds = new List<double>(); // 網點區二值化閾值
        // 網點區二值化模式
        // false = 手動固定 Threshold
        // true  = 自動 Otsu + 線性補償
        public bool AutoMeshThresholdEnabled = false;
      

        // 模板宣告
        public List<TemplateData> YinTemplates = new List<TemplateData>();  // 陰版模版
        public List<TemplateData> YangTemplates = new List<TemplateData>(); // 陽版模版                                                        

        // Raw 原始影像（未旋轉，用於拼接）
        Mat RawImage0;
        Mat RawImage1;
        Mat RawImage2;
        Mat RawImage3;
        Mat RawImage4;
        Mat RawImage5;

        // Corrected 旋轉校正後影像（AOI / UI 用）
        Mat SourceImage;
        Mat SourceImage0;
        Mat SourceImage1;
        Mat SourceImage2;
        Mat SourceImage3;
        Mat SourceImage4;
        Mat SourceImage5;

        // 網點區宣告
        MeshP_Imgs MeshP_Imgs = new MeshP_Imgs();           // 網點區裁切原圖
        MeshP_Imgs MeshP_Result_Imgs = new MeshP_Imgs(); // 網點區計算結果圖
        MeshP_Results MeshP_Results = new MeshP_Results();      // 網點區計算結果

        // 陰版宣告
        Font_Imgs Yin_Imgs = new Font_Imgs();               // 陰版原始裁切圖
        Font_Imgs Yin_Block_Imgs = new Font_Imgs();         // 陰版塞版結果圖
        Font_Imgs Yin_Defect_Imgs = new Font_Imgs();        // 陰版缺燙結果圖
        Font_Results Yin_Defect_Results = new Font_Results(); // 陰版缺燙計算結果
        Font_Results Yin_Block_Results = new Font_Results();  // 陰版塞版計算結果
        List<FontInfo> Yin_Font_Infos = new List<FontInfo>(); // 陰版計算資訊

        // 陽版宣告
        Font_Imgs Yang_Imgs = new Font_Imgs();              // 陽版原始裁切圖
        Font_Imgs Yang_Defect_Imgs = new Font_Imgs();       // 陽版缺燙結果圖
        Font_Imgs Yang_Block_Imgs = new Font_Imgs();        // 陽版塞版結果圖
        Font_Results Yang_Defect_Results = new Font_Results(); // 陽版缺燙計算結果
        Font_Results Yang_Block_Results = new Font_Results();  // 陽版塞版計算結果
        List<FontInfo> Yang_Font_Infos = new List<FontInfo>(); // 陽版計算資訊

        // 飽滿區宣告
        Fullness_Results Fullness_Results = new Fullness_Results(); // 飽滿度計算結果

        public PictureBox pictureBox_SourceImage = new PictureBox();
        private TaskCompletionSource<Bitmap> captureTaskSource;

        private Mat BreakArea_01_crop;
        private Mat BreakArea_02_crop;
        private Mat BreakArea_03_crop;
        private Mat fullness_area;

        #region 初始化
        public UserControl_Main(UNIVACCO _MainPage)
        {
            InitializeComponent();
            MainPage = _MainPage;
            ClearAll();
            LoadStandard();
            LoadPamaraters();
            MainPage.camera.CameraSnapshotCaptured += Camera_OneShotEvent;
            OCT_AlgorithmHelper.UpdateUIAction += UpdateUI;
        }

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

        private void LoadStandard()
        {
            if (radioButton_AbsStandard.Checked)
            {
                comboBox_TempStandard.Items.Clear();
                comboBox_TempStandard.Enabled = false;
                MeshP_Param = new OCT_DatabaseHelper("OCTParameters");
                List<OCT_ParameterMeshP> abs_standards = MeshP_Param.GetAllData("ABSMeshP");
                abs_standard = abs_standards.FirstOrDefault(x => x.Name == "abs");
                meshp_current_thresholds = new List<double>()
                {
                    abs_standard.Area10,
                    abs_standard.Area20,
                    abs_standard.Area30,
                    abs_standard.Area40,
                    abs_standard.Area60,
                    abs_standard.Area70,
                    abs_standard.Area80,
                    abs_standard.Area90,
                };
            }
        }

        private void LoadTemplates(string cardType)
        {
            string templatesDir = cardType == "雙銅"
                ? "template_double sided coated paper"
                : "template_white card";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string fullTemplatesDir = Path.Combine(baseDir, templatesDir);

            if (!Directory.Exists(fullTemplatesDir))
                fullTemplatesDir = Path.Combine(baseDir, "templates");

            YinTemplates.Clear();
            YangTemplates.Clear();

            for (int pt = 3; pt <= 12; pt++)
            {
                string yinPath = Path.Combine(fullTemplatesDir, $"Yin_template_{pt}pt.bmp");
                if (!File.Exists(yinPath)) return;
                YinTemplates.Add(new TemplateData(yinPath));
            }

            for (int pt = 3; pt <= 12; pt++)
            {
                string yangPath = Path.Combine(fullTemplatesDir, $"Yang_template_{pt}pt.bmp");
                if (!File.Exists(yangPath)) return;
                YangTemplates.Add(new TemplateData(yangPath));
            }
        }

        private void LoadPamaraters()
        {
            try
            {
                MainPage.ExposureTime = MainPage.Hot_ExposureTime_B;
                MainPage.camera.ExposureTime(MainPage.ExposureTime);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"錯誤訊息: {ex.Message}");
            }
            OCT_SaveFileHelper = new OCT_SaveFileHelper(MainPage.ResultSavePath);
            WC_Params = new OCT_Parameters_CardType(MainPage, "WC");
            DC_Params = new OCT_Parameters_CardType(MainPage, "DC");
            CurrentCardType_Params = WC_Params;
            LabPrint_Params = new OCT_Parameters_PrintType(MainPage, "LabPrint");
            OffsetPrint_Params = new OCT_Parameters_PrintType(MainPage, "OffsetPrint");
            CurrentPrintType_Params = LabPrint_Params;
            OCT_Rules = new OCT_Rules(MainPage);
            SaveOptions = new OCT_Parameters_SaveOptions(MainPage);

            IsValveEnabled =
                SaveOptions.saveoption.IsValveEnabled;

            AutoMeshThresholdEnabled =
                SaveOptions.saveoption.AutoMeshThresholdEnabled;
            LoadTemplates("白卡");
        }
        #endregion

        #region 系統流程
        private async Task SystemProceess_MeshP(Mat sourceimg1, Mat sourceimg2)
        {
            var meshp_task = Task.Run(() => OCT_AlgorithmHelper.MeshP_anylz(sourceimg1, sourceimg2, meshp_current_thresholds, CurrentPrintType_Params, SaveOptions, AutoMeshThresholdEnabled));
            await Task.WhenAll(meshp_task);
            var meshp_result = meshp_task.Result;

            if (meshp_result == null)
            {
                OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, "網點區分析未產出結果");

                // 補齊破開區 Mat 避免後續空指標例外
                BreakArea_01_crop = new Mat(new Size(2094, 100), DepthType.Cv8U, 3);
                BreakArea_01_crop.SetTo(new MCvScalar(0, 0, 0));
                MeshP_Results = new MeshP_Results();

                UpdateUI(() =>
                {
                    MessageBox.Show("網點區分析未產出結果！\n（影像已保留，流程結束後可點擊「儲存檔案」手動存檔）",
                                    "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });

                return;
            }

            (MeshP_Imgs, MeshP_Result_Imgs, MeshP_Results, BreakArea_01_crop) = meshp_result.Value;
            MeshP_Results = OCT_AlgorithmHelper.Evaluate_MeshP(OCT_Rules, MeshP_Results);
            Update_MeshP(MeshP_Imgs, MeshP_Results);
        }

        private async Task SystemProceess_FontArea(Mat sourceimg3, Mat sourceimg4)
        {
            Mat yinarea = new Mat(), yangarea = new Mat();

            // ================================================================
            // 0. 模板完整性檢查：3pt ~ 12pt 應各有 10 張
            //    避免模板載入不完整時，後面以 YinTemplates[i] / YangTemplates[i]
            //    取值造成 IndexOutOfRange 或分析直接回傳 null。
            // ================================================================
            if (YinTemplates == null || YinTemplates.Count != 10)
            {
                throw new Exception($"陰版模板數量錯誤，目前={YinTemplates?.Count ?? 0}，正常應為 10 張（3pt~12pt）。");
            }

            if (YangTemplates == null || YangTemplates.Count != 10)
            {
                throw new Exception($"陽版模板數量錯誤，目前={YangTemplates?.Count ?? 0}，正常應為 10 張（3pt~12pt）。");
            }

            // ================================================================
            // 1. Station 3 / 4 字體區拼接（OCT_AlgorithmHelper 內已換新版）
            // ================================================================
            var (fontarea, BreakArea_02_crop) = await OCT_AlgorithmHelper.Merge_FontArea(sourceimg3, sourceimg4);
            this.BreakArea_02_crop = BreakArea_02_crop;

            if (fontarea == null || fontarea.IsEmpty)
            {
                throw new Exception("Station 3 / 4 字體拼接結果為空，無法進行陰陽版分析。");
            }

            // ================================================================
            // 2. Python 新版陰陽版切割：Valley 灰階 Profile + Gaussian
            // ================================================================
            (yinarea, yangarea) = await CropYinYangArea(fontarea);

            if (yinarea == null || yinarea.IsEmpty)
                throw new Exception("陰版裁切結果為空。");

            if (yangarea == null || yangarea.IsEmpty)
                throw new Exception("陽版裁切結果為空。");

            UpdateUI(() => pictureBox_Yin.Image = yinarea.ToBitmap());
            UpdateUI(() => pictureBox_Yang.Image = yangarea.ToBitmap());
            CvInvoke.Imwrite("yangarea.bmp", yangarea);
            CvInvoke.Imwrite("yinarea.bmp", yinarea);

            string selectedCardType = comboBox_CardType.SelectedItem?.ToString() ?? "白卡";

            Debug.WriteLine($"[FontArea] Yin size={yinarea.Width}x{yinarea.Height}, Yang size={yangarea.Width}x{yangarea.Height}");
            Debug.WriteLine($"[FontArea] YinTemplates={YinTemplates.Count}, YangTemplates={YangTemplates.Count}");

            var breakArea03Ready = new TaskCompletionSource<Mat>();

            var yin_task = Task.Run(() =>
                OCT_AlgorithmHelper.Yin_analz(
                    yinarea,
                    YinTemplates,
                    CurrentCardType_Params,
                    CurrentPrintType_Params,
                    SaveOptions,
                    selectedCardType));

            var yang_task = Task.Run(() =>
                OCT_AlgorithmHelper.Yang_analz(
                    yangarea,
                    YangTemplates,
                    CurrentCardType_Params,
                    CurrentPrintType_Params,
                    SaveOptions,
                    selectedCardType,
                    breakArea03Ready));

            // BreakArea03 如果正常產出就先存入全域；若 Yang 分析失敗，
            // 這個 continuation 不影響下面真正的錯誤判斷。
            _ = breakArea03Ready.Task.ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                    this.BreakArea_03_crop = t.Result;
            }, TaskScheduler.FromCurrentSynchronizationContext());

            await Task.WhenAll(yin_task, yang_task);

            var yin_result = yin_task.Result;
            var yang_result = yang_task.Result;

            // ================================================================
            // 3. Nullable 防呆
            //    舊版直接 .Value，任何分析失敗都只會跳：
            //    「可為 Null 的物件必須具有值」
            //    現在改成明確指出是哪一側分析失敗。
            // ================================================================
            if (!yin_result.HasValue)
            {
                OCT_LogHelper.WriteLog(
                    LogLevel.Error,
                    Page.O,
                    "陰版分析 Yin_analz 未產出結果");

                throw new Exception("陰版字體分析失敗：Yin_analz 回傳 null。請查看 Visual Studio Output / Log 內的 [Yin_analz ERROR]。");
            }

            if (!yang_result.HasValue)
            {
                OCT_LogHelper.WriteLog(
                    LogLevel.Error,
                    Page.O,
                    "陽版分析 Yang_analz 未產出結果");

                throw new Exception("陽版字體分析失敗：Yang_analz 回傳 null。請查看 Visual Studio Output / Log 內的 [Yang_analz ERROR]。");
            }

            (Yin_Imgs, Yin_Block_Imgs, Yin_Block_Results, Yin_Defect_Imgs, Yin_Defect_Results, Yin_Font_Infos) = yin_result.Value;
            (Yang_Imgs, Yang_Block_Imgs, Yang_Block_Results, Yang_Defect_Imgs, Yang_Defect_Results, Yang_Font_Infos, BreakArea_03_crop) = yang_result.Value;

            // ================================================================
            // 4. 原本的等級判定與 UI 更新完全保留
            // ================================================================
            var yinTask = Task.Run(() => OCT_AlgorithmHelper.Evaluate_Yin(OCT_Rules, Yin_Defect_Results, Yin_Block_Results));
            var yangTask = Task.Run(() => OCT_AlgorithmHelper.Evaluate_Yang(OCT_Rules, Yang_Defect_Results, Yang_Block_Results));

            await Task.WhenAll(yinTask, yangTask);

            (Yin_Defect_Results, Yin_Block_Results) = yinTask.Result;
            (Yang_Defect_Results, Yang_Block_Results) = yangTask.Result;

            Update_Yin(Yin_Defect_Results, Yin_Block_Results);
            Update_Yang(Yang_Defect_Results, Yang_Block_Results);
        }

        private async Task SystemProceess_FullnessArea(Mat sourceimg5)
        {
            fullness_area = await OCT_AlgorithmHelper.CropFullnessArea(sourceimg5);
            pictureBox_Fullness.Image = fullness_area.ToBitmap();
            Fullness_Results = await Task.Run(() =>
                OCT_AlgorithmHelper.Fullness_analz(fullness_area, CurrentCardType_Params, SaveOptions)
            );
            Fullness_Results = OCT_AlgorithmHelper.Evaluate_Fullness(OCT_Rules, Fullness_Results);
            Update_Fullness(Fullness_Results);
        }

        /// <summary>
        /// Python 新版陰陽版拆分
        /// 1. 不做 Otsu 二值化，直接使用原始灰階
        /// 2. 在固定 Valley 搜尋框 (1800, 0, 800, 4000) 內做 X 方向灰階平均
        /// 3. GaussianBlur(kernel=21) 平滑 Profile
        /// 4. 取灰階平均最低點作為陰陽版交界 middleBoundary
        /// 5. 中線左側切 2120px 為陽版、右側切 2080px 為陰版
        /// 6. 兩側皆逆時針旋轉 90 度
        /// </summary>
        private Task<(Mat, Mat)> CropYinYangArea(Mat fontarea)
        {
            const int yangWidth = 2120;
            const int yinWidth = 2080;

            // 對應 Python:
            // split_valley_search_box = (1800, 0, 800, 4000)
            const int searchX = 1800;
            const int searchY = 0;
            const int searchWidth = 800;
            const int searchHeight = 4000;

            if (fontarea == null || fontarea.IsEmpty)
                throw new Exception("FontArea 為空，無法切割陰陽版。");

            Mat gray = new Mat();

            try
            {
                if (fontarea.NumberOfChannels == 3)
                    CvInvoke.CvtColor(fontarea, gray, ColorConversion.Bgr2Gray);
                else
                    gray = fontarea.Clone();

                int imgWidth = gray.Width;
                int imgHeight = gray.Height;

                if (searchX >= imgWidth || searchY >= imgHeight)
                {
                    throw new Exception(
                        $"陰陽版 Valley 搜尋框起點超出影像範圍：" +
                        $"Search=({searchX},{searchY}), Image={imgWidth}x{imgHeight}");
                }

                int roiWidth = Math.Min(searchWidth, imgWidth - searchX);
                int roiHeight = Math.Min(searchHeight, imgHeight - searchY);

                if (roiWidth < 3 || roiHeight < 3)
                    throw new Exception("陰陽版 Valley 搜尋 ROI 太小，無法進行定位。");

                Rectangle searchRoi = new Rectangle(searchX, searchY, roiWidth, roiHeight);

                // ------------------------------------------------------------
                // Python: x_prof = np.mean(mid_strip, axis=0)
                // ------------------------------------------------------------
                Matrix<double> profile = new Matrix<double>(1, roiWidth);
                Matrix<double> smoothProfile = new Matrix<double>(1, roiWidth);

                try
                {
                    using (Mat roi = new Mat(gray, searchRoi))
                    {
                        unsafe
                        {
                            byte* ptr = (byte*)roi.DataPointer;
                            int step = roi.Step;

                            for (int x = 0; x < roi.Width; x++)
                            {
                                double sum = 0.0;

                                for (int y = 0; y < roi.Height; y++)
                                {
                                    byte* px = ptr + (y * step + x);
                                    sum += *px;
                                }

                                profile[0, x] = sum / roi.Height;
                            }
                        }
                    }

                    // Python:
                    // x_smooth = cv2.GaussianBlur(x_prof.reshape(1,-1), (1,21), 0)
                    // 對 1xN 水平 Profile，在 C# / OpenCV 中 kernel 要使用 (21,1)。
                    CvInvoke.GaussianBlur(
                        profile,
                        smoothProfile,
                        new Size(21, 1),
                        0);

                    // Python: mid_boundary = vx + np.argmin(x_smooth)
                    int valleyIndex = 0;
                    double minGray = double.MaxValue;

                    for (int x = 0; x < roiWidth; x++)
                    {
                        double value = smoothProfile[0, x];
                        if (value < minGray)
                        {
                            minGray = value;
                            valleyIndex = x;
                        }
                    }

                    int middleBoundary = searchX + valleyIndex;
                    int leftBoundary = middleBoundary - yangWidth;
                    int rightBoundary = middleBoundary + yinWidth;

                    Debug.WriteLine(
                        $"[YinYang Valley] ROI=({searchX},{searchY},{roiWidth},{roiHeight}), " +
                        $"minGray={minGray:F2}, left={leftBoundary}, " +
                        $"middle={middleBoundary}, right={rightBoundary}, imageWidth={fontarea.Width}");

                    if (leftBoundary < 0 || rightBoundary > fontarea.Width)
                    {
                        throw new Exception(
                            $"陰陽版裁切超出範圍：" +
                            $"left={leftBoundary}, middle={middleBoundary}, right={rightBoundary}, " +
                            $"imageWidth={fontarea.Width}");
                    }

                    Mat yangArea = new Mat(
                        fontarea,
                        new Rectangle(leftBoundary, 0, yangWidth, fontarea.Height)
                    ).Clone();

                    Mat yinArea = new Mat(
                        fontarea,
                        new Rectangle(middleBoundary, 0, yinWidth, fontarea.Height)
                    ).Clone();

                    Mat yinRotated = new Mat();
                    Mat yangRotated = new Mat();

                    CvInvoke.Rotate(yinArea, yinRotated, RotateFlags.Rotate90CounterClockwise);
                    CvInvoke.Rotate(yangArea, yangRotated, RotateFlags.Rotate90CounterClockwise);

                    yinArea.Dispose();
                    yangArea.Dispose();

                    Debug.WriteLine(
                        $"[YinYang Valley] Rotate OK: Yin={yinRotated.Width}x{yinRotated.Height}, " +
                        $"Yang={yangRotated.Width}x{yangRotated.Height}");

                    return Task.FromResult((yinRotated, yangRotated));
                }
                finally
                {
                    profile.Dispose();
                    smoothProfile.Dispose();
                }
            }
            finally
            {
                gray.Dispose();
            }
        }
        #endregion

        #region 例外處理
        private void ClearAll()
        {
            ClearPictureBox_SourceImg();
            ClearUI();
        }
        #endregion

        #region 工具
        public void Camera_OneShotEvent(Bitmap bmp)
        {
            Bitmap t = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format24bppRgb);
            pictureBox_SourceImage.Image = t;
        }

        public async Task<(List<Mat> meshp_areas, List<double> meshp_dectectpixels, List<double> autotune_thresholds)?> Autotune(Mat sourceimg1, Mat sourceimg2, OCT_Parameters_PrintType current_printtype_params)
        {
            List<Mat> meshp_areas = new List<Mat>();
            try
            {
                (meshp_areas, _) = await OCT_AlgorithmHelper.Meshpoint_crop(sourceimg1, sourceimg2);
            }
            catch (Exception)
            {
                return null;
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
            };

            List<double> autotune_thresholds = await Task.Run(() => OCT_AlgorithmHelper.Autotune_threshold(meshp_areas));
            List<Mat> meshp_areas_threshold = meshp_areas.Select(mat => mat.Clone()).ToList();
            meshp_areas_threshold = OCT_AlgorithmHelper.Meshp_areas_threshold(meshp_areas_threshold, autotune_thresholds);

            List<int> meshp_standard_blackpxls = new List<int>();
            List<double> meshp_dectectpixels = new List<double>();
            int meshp_index = 10;
            int blackpixels = 0;

            foreach (Mat threshold_meshp_area in meshp_areas_threshold)
            {
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
                            if (*current_ptr == 0) blackpixels++;
                        }
                    }
                }

                double detect_pixels = (double)(blackpixels - standard_pixels) / (double)total_pixels * 100;
                meshp_dectectpixels.Add(detect_pixels);

                if (meshp_index == 40) meshp_index = 60;
                else meshp_index += 10;
            }

            return (meshp_areas, meshp_dectectpixels, autotune_thresholds);
        }

        private void ClearGlobalVars()
        {
            SourceImage?.Dispose();
            MeshP_Imgs?.Dispose();
            MeshP_Result_Imgs?.Dispose();
            Yin_Imgs?.Dispose();
            Yin_Block_Imgs?.Dispose();
            Yin_Defect_Imgs?.Dispose();
            Yang_Imgs?.Dispose();
            Yang_Block_Imgs?.Dispose();
            Yang_Defect_Imgs?.Dispose();

            MeshP_Results = new MeshP_Results();
            Yin_Defect_Results = new Font_Results();
            Yin_Block_Results = new Font_Results();
            Yin_Font_Infos = new List<FontInfo>();
            Yang_Defect_Results = new Font_Results();
            Yang_Block_Results = new Font_Results();
            Yang_Font_Infos = new List<FontInfo>();
            Fullness_Results = new Fullness_Results();
        }

        public (int movebias, double rotationAngle) calc_motor_movebias(Mat firstimg)
        {
            int movebias = 0;
            double slope = 0;

            CvInvoke.CvtColor(firstimg, firstimg, ColorConversion.Bgr2Gray);
            CvInvoke.Threshold(firstimg, firstimg, 0, 255, ThresholdType.Otsu);
            CvInvoke.Imwrite("calcmovebias_01_otsu.bmp", firstimg);

            Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(13, 13), new Point(-1, -1));
            Mat opened = new Mat();
            CvInvoke.MorphologyEx(firstimg, opened, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            CvInvoke.Imwrite("calcmovebias_02_opened.bmp", opened);

            int leftBoundary = 0;
            int rightBoundary = opened.Width - 1;
            int middleX = opened.Width / 2;
            int middleY = 0;
            unsafe
            {
                byte* ptr = (byte*)opened.DataPointer;
                int step = opened.Step;

                bool isMiddleYFound = false;
                for (int y = Math.Min(3500, opened.Height - 1); y > 0; y--)
                {
                    if (isMiddleYFound) break;

                    int whitePixels = 0;
                    for (int x = 800; x < Math.Min(2400, opened.Width); x++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0)
                        {
                            whitePixels++;
                            if (whitePixels > (int)((2400 - 800) * 0.95))
                            {
                                middleY = y;
                                isMiddleYFound = true;
                                break;
                            }
                        }
                    }
                }

                bool rightBoundaryFound = false;
                for (int x = 1800; x < opened.Width; x++)
                {
                    if (rightBoundaryFound) break;
                    int blackPixels = 0;
                    for (int y = middleY; y < Math.Min(2000, opened.Height); y++)
                    {
                        byte* currentPixel = ptr + (y * step + x);
                        if (*currentPixel == 0) blackPixels++;
                    }
                    if (blackPixels > (int)((2000 - middleY) * 0.5))
                    {
                        rightBoundary = x;
                        rightBoundaryFound = true;
                    }
                }
                rightBoundary -= 100;
                leftBoundary = rightBoundary - 2000;
            }

            // 安全邊界裁切防呆
            int safeLeft = Math.Max(0, leftBoundary);
            int safeTop = Math.Max(0, middleY - 250);
            int safeWidth = Math.Min(opened.Width - safeLeft, Math.Max(10, rightBoundary - leftBoundary));
            int safeHeight = Math.Min(opened.Height - safeTop, 500);

            if (safeWidth <= 10 || safeHeight <= 10)
            {
                throw new Exception("樣品擺放偏移過大，無法擷取定位特徵。");
            }

            Mat ROI = new Mat(opened, new Rectangle(safeLeft, safeTop, safeWidth, safeHeight));

            Mat houghimg = new Mat();
            CvInvoke.BitwiseNot(ROI, houghimg);
            CvInvoke.Imwrite("calcmovebias_03_ROI1.bmp", houghimg);
            LineSegment2D[] lines = CvInvoke.HoughLinesP(houghimg, 1, Math.PI / 180, 50, 100, 10);

            Point center = new Point(houghimg.Width / 2, houghimg.Height / 2);
            List<Point> leftPoints = new List<Point>();
            List<Point> rightPoints = new List<Point>();

            CvInvoke.CvtColor(houghimg, houghimg, ColorConversion.Gray2Bgr);
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    CvInvoke.Line(houghimg, line.P1, line.P2, new MCvScalar(0, 255, 0), 2);
                    LineSegment2D normalizedLine = NormalizeLineSegment(line);

                    if (normalizedLine.P1.X < center.X || normalizedLine.P2.X < center.X)
                    {
                        if (normalizedLine.P1.X < center.X) leftPoints.Add(normalizedLine.P1);
                        if (normalizedLine.P2.X < center.X) leftPoints.Add(normalizedLine.P2);
                    }

                    if (normalizedLine.P1.X > center.X || normalizedLine.P2.X > center.X)
                    {
                        if (normalizedLine.P1.X > center.X) rightPoints.Add(normalizedLine.P1);
                        if (normalizedLine.P2.X > center.X) rightPoints.Add(normalizedLine.P2);
                    }
                }
            }

            Point rotLeftBottom = new Point(0, 0);
            Point rotRightBottom = new Point(0, 0);

            if (leftPoints.Count > 0)
            {
                int minLeftX = leftPoints.Min(p => p.X);
                var leftCandidates = leftPoints.Where(p => Math.Abs(p.X - minLeftX) <= 200).ToList();
                rotLeftBottom = leftCandidates.OrderByDescending(p => p.Y).First();
            }

            if (rightPoints.Count > 0)
            {
                int maxRightX = rightPoints.Max(p => p.X);
                var rightCandidates = rightPoints.Where(p => Math.Abs(p.X - maxRightX) <= 200).ToList();
                rotRightBottom = rightCandidates.OrderByDescending(p => p.Y).First();
            }

            double rotationAngle = 0;
            if (leftPoints.Count > 0 && rightPoints.Count > 0)
            {
                double deltaX = rotRightBottom.X - rotLeftBottom.X;
                double deltaY = rotRightBottom.Y - rotLeftBottom.Y;

                if (deltaX != 0)
                {
                    slope = deltaY / deltaX;
                    rotationAngle = Math.Atan(slope) * 180.0 / Math.PI;
                    opened = opened.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(0, 0, 0)).Mat;
                }
            }

            CvInvoke.Imwrite("calcmovebias_04_rotate.bmp", opened);

            if (leftPoints.Count > 0 && rightPoints.Count > 0 && rotationAngle != 0)
            {
                Point roiCenter = new Point(houghimg.Width / 2, houghimg.Height / 2);
                int leftRelativeX = rotLeftBottom.X - roiCenter.X;
                int leftRelativeY = rotLeftBottom.Y - roiCenter.Y;
                int rightRelativeX = rotRightBottom.X - roiCenter.X;
                int rightRelativeY = rotRightBottom.Y - roiCenter.Y;

                double cosAngle = Math.Cos(-rotationAngle * Math.PI / 180.0);
                double sinAngle = Math.Sin(-rotationAngle * Math.PI / 180.0);

                int rotatedLeftX = (int)(leftRelativeX * cosAngle - leftRelativeY * sinAngle);
                int rotatedLeftY = (int)(leftRelativeX * sinAngle + leftRelativeY * cosAngle);
                int rotatedRightX = (int)(rightRelativeX * cosAngle - rightRelativeY * sinAngle);
                int rotatedRightY = (int)(rightRelativeX * sinAngle + rightRelativeY * cosAngle);

                rotatedLeftY += roiCenter.Y;
                rotatedRightY += roiCenter.Y;

                int leftYInOriginal = rotatedLeftY + safeTop;
                int rightYInOriginal = rotatedRightY + safeTop;

                int referenceY = Math.Max(leftYInOriginal, rightYInOriginal);
                movebias = referenceY - 400;
            }
            return (movebias, rotationAngle);
        }
        #endregion

        #region UI元件
        private async void button_Start_Click(object sender, EventArgs e)
        {
            LoadStandard();

            if (SaveResultFlag)
            {
                if (MessageBox.Show("剛剛的結果尚未儲存，確定要繼續嗎?", "資訊", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
                SaveResultFlag = false;
            }
            ClearGlobalVars();

            if (ReadSourceImgFlag)
            {
                SourceImage = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                ReadSourceImgFlag = false;
            }
            else
            {
                try
                {
                    ClearPictureBox_SourceImg();
                    ClearUI();
                    Update();

                    MainPage.GLED_Used_Counter++;
                    MainPage.ArduinoOpenGreenLight();
                    await Task.Delay(1000);

                    if (IsValveEnabled)
                        await MainPage.ArduinoOpenValve();

                    while (true)
                    {
                        var result = MessageBox.Show(
                            "待測物是否已定位完成？",
                            "定位確認",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.OK) break;
                        if (result == DialogResult.Cancel) return;
                    }

                    // 1. 首次拍攝計算偏差與斜率（失敗時降級為預設值 0 繼續執行）
                    int movebias = 0;
                    double rotationAngle = 0.0;

                    try
                    {
                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(500);

                        if (pictureBox_SourceImage.Image != null)
                        {
                            SourceImage0 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                            CvInvoke.Imwrite("SourceImage_first.bmp", SourceImage0);

                            var result = calc_motor_movebias(SourceImage0.Clone());

                            movebias = result.movebias;

                            const double MAX_VALID_ROTATION = 1.0;

                            if (Math.Abs(result.rotationAngle) <= MAX_VALID_ROTATION)
                            {
                                rotationAngle = result.rotationAngle;
                            }
                            else
                            {
                                rotationAngle = 0.0;
                            }
                            Debug.WriteLine(
                                $"[定位測試] 原始計算角度 = {result.rotationAngle:F3}°, " +
                                $"實際套用角度 = {rotationAngle:F3}°"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, $"定位計算未產出有效值，使用預設值繼續拍照: {ex.Message}");
                        movebias = 0;
                        rotationAngle = 0.0;
                    }

                    // 2. 依序拍攝各站點照片並顯示至 UI
                    try
                    {
                        // 站點 1
                        // 站點 1 (只有 movebias 大於 0 才移動馬達，為 0 或負數則直接原地拍攝)
                        if (movebias > 0)
                        {
                            await MainPage.arduino.SpecificRightMoveAsync(movebias);
                            await Task.Delay(200);
                        }

                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(400);
                        RawImage1 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                        SourceImage1 = RawImage1.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                        pictureBox_1.Image = SourceImage1.ToBitmap();
                        CvInvoke.Imwrite("SourceImage_O1.bmp", SourceImage1);
                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(400);
                        RawImage1 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                        SourceImage1 = RawImage1.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                        pictureBox_1.Image = SourceImage1.ToBitmap();
                        CvInvoke.Imwrite("SourceImage_O1.bmp", SourceImage1);

                        // 站點 2
                        await MainPage.arduino.SpecificRightMoveAsync(2350);
                        await Task.Delay(200);
                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(400);
                        RawImage2 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                        SourceImage2 = RawImage2.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                        pictureBox_2.Image = SourceImage2.ToBitmap();
                        CvInvoke.Imwrite("SourceImage_O2.bmp", SourceImage2);

                        var task_meshp = SystemProceess_MeshP(SourceImage1, SourceImage2);

                        // 站點 3
                        await MainPage.arduino.SpecificRightMoveAsync(2100);
                        await Task.Delay(200);
                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(400);
                        RawImage3 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                        SourceImage3 = RawImage3.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                        pictureBox_3.Image = SourceImage3.ToBitmap();
                        CvInvoke.Imwrite("SourceImage_O3.bmp", SourceImage3);

                        // 站點 4
                        await MainPage.arduino.SpecificRightMoveAsync(1900);
                        await Task.Delay(200);
                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(400);
                        RawImage4 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                        SourceImage4 = RawImage4.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                        pictureBox_4.Image = SourceImage4.ToBitmap();
                        CvInvoke.Imwrite("SourceImage_O4.bmp", SourceImage4);

                        var task_fontarea = SystemProceess_FontArea(SourceImage3, SourceImage4);

                        // 站點 5
                        await MainPage.arduino.SpecificRightMoveAsync(2400);
                        await Task.Delay(200);
                        MainPage.camera.CaptureOneFrame();
                        Thread.Sleep(400);
                        RawImage5 = new Bitmap(pictureBox_SourceImage.Image).ToImage<Bgr, byte>().Mat;
                        SourceImage5 = RawImage5.ToImage<Bgr, byte>().Rotate(-rotationAngle, new Bgr(255, 255, 255)).Mat;
                        CvInvoke.Imwrite("SourceImage_O5.bmp", SourceImage5);

                        var task_fullnessarea = SystemProceess_FullnessArea(SourceImage5);

                        MainPage.ArduinoCloseGreenLight();
                        if (IsValveEnabled)
                            await MainPage.ArduinoCloseValve();
                        await MainPage.arduino.GoHomeAsync();

                        await Task.WhenAll(task_meshp, task_fontarea, task_fullnessarea);

                        try
                        {
                            var breakAreaParams = (comboBox_CardType.SelectedItem?.ToString() == "雙銅")
                                ? DC_Params.breakarea_parameter
                                : WC_Params.breakarea_parameter;

                            if (BreakArea_01_crop != null && BreakArea_02_crop != null && BreakArea_03_crop != null)
                            {
                                OCT_AlgorithmHelper.BreakArea_Process(BreakArea_01_crop, BreakArea_02_crop, BreakArea_03_crop, breakAreaParams);
                            }
                        }
                        catch { }

                        // 3. 流程結束，開啟手動存檔功能
                        SaveResultFlag = true;
                        button_Output.Enabled = true;

                        Label_Add_ClickEvent(MeshP_Imgs, Yin_Imgs, Yang_Imgs, fullness_area);
                        MessageBox.Show("樣品拍攝與分析完成！請輸入「品名」後點擊「儲存檔案」存檔。", "系統資訊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, "拍照或運動流程中斷", ex.ToString());
                        SaveResultFlag = true;
                        button_Output.Enabled = true;
                        MessageBox.Show($"流程執行異常：{ex.Message}\n（已拍攝之照片仍可點擊「儲存檔案」保存）", "系統視窗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                finally
                {
                    MainPage.ArduinoCloseGreenLight();
                    if (IsValveEnabled)
                    {
                        try { await MainPage.ArduinoCloseValve(); }
                        catch
                        {
                            await Task.Delay(500);
                            await MainPage.ArduinoCloseValve();
                        }
                    }
                }
            }
        }

        public static LineSegment2D NormalizeLineSegment(LineSegment2D line)
        {
            if (line.P1.X > line.P2.X)
            {
                return new LineSegment2D(line.P2, line.P1);
            }
            return line;
        }

        private void button_Parameter_Click(object sender, EventArgs e)
        {
            MainPage.AdministratorPassword = MainPage.ReadIniFile("Setting", "AdministratorPassword", "OCTParameters.ini");
            string password = Interaction.InputBox("請輸入管理員密碼", "系統訊息", "", -1, -1);

            if (string.IsNullOrWhiteSpace(password)) return;

            if (password != MainPage.AdministratorPassword)
            {
                MessageBox.Show("密碼錯誤無法開啟參數設定介面!", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, "開啟參數設定頁面成功");
            StandardOParameterSetting = new Setting_O_Window(MainPage, this);
            StandardOParameterSetting.ShowDialog();
        }

        private void button_LightCorrection_Click(object sender, EventArgs e)
        {
            MainPage.AdministratorPassword = MainPage.ReadIniFile("Setting", "AdministratorPassword", "OCTParameters.ini");
            string password = Interaction.InputBox("請輸入管理員密碼", "系統訊息", "", -1, -1);

            if (string.IsNullOrWhiteSpace(password)) return;

            if (password != MainPage.AdministratorPassword)
            {
                MessageBox.Show("密碼錯誤無法開啟光源校正介面!", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, "開啟光源校正頁面成功");
            LightCorrectionPage = new LightCorrection(MainPage);

            MainPage.camera.CameraImageEvent -= MainPage.Camera_CameraImageEvent;
            MainPage.camera.CameraImageEvent += LightCorrectionPage.Camera_LightCorrectionImageEvent;

            try
            {
                LightCorrectionPage.ShowDialog();
            }
            finally
            {
                MainPage.camera.CameraImageEvent -= LightCorrectionPage.Camera_LightCorrectionImageEvent;
                MainPage.camera.CameraImageEvent += MainPage.Camera_CameraImageEvent;
                MainPage.switchbox = this.pictureBox_1;
            }
        }

        private void radioButton_Standard_changed(object sender, EventArgs e)
        {
            if (radioButton_AbsStandard.Checked)
            {
                comboBox_TempStandard.Items.Clear();
                comboBox_TempStandard.Enabled = false;
                meshp_current_thresholds = new List<double>()
                {
                    abs_standard.Area10,
                    abs_standard.Area20,
                    abs_standard.Area30,
                    abs_standard.Area40,
                    abs_standard.Area60,
                    abs_standard.Area70,
                    abs_standard.Area80,
                    abs_standard.Area90,
                };
            }
            else
            {
                comboBox_TempStandard.Items.Clear();
                comboBox_TempStandard.Enabled = true;
                temp_standards = MeshP_Param.GetAllData("TempMeshP");
                foreach (OCT_ParameterMeshP temp_standard in temp_standards)
                {
                    comboBox_TempStandard.Items.Add(temp_standard.Name);
                }
                comboBox_TempStandard.SelectedIndex = 0;
                string selectname = comboBox_TempStandard.SelectedItem?.ToString();
                temp_standard = temp_standards.FirstOrDefault(x => x.Name == selectname);
                meshp_current_thresholds = new List<double>()
                {
                    temp_standard.Area10,
                    temp_standard.Area20,
                    temp_standard.Area30,
                    temp_standard.Area40,
                    temp_standard.Area60,
                    temp_standard.Area70,
                    temp_standard.Area80,
                    temp_standard.Area90,
                };
            }
        }

        private void radioButton_PrintType_changed(object sender, EventArgs e)
        {
            CurrentPrintType_Params = radioButton_LabPrint.Checked ? LabPrint_Params : OffsetPrint_Params;
        }

        private void comboBox_TempStandard_changed(object sender, EventArgs e)
        {
            string selectname = comboBox_TempStandard.SelectedItem?.ToString();
            temp_standard = temp_standards.FirstOrDefault(x => x.Name == selectname);
            meshp_current_thresholds = new List<double>()
            {
                temp_standard.Area10,
                temp_standard.Area20,
                temp_standard.Area30,
                temp_standard.Area40,
                temp_standard.Area60,
                temp_standard.Area70,
                temp_standard.Area80,
                temp_standard.Area90,
            };
        }

        private void comboBox_TempStandard_DropDown(object sender, EventArgs e)
        {
            string currentSelected = comboBox_TempStandard.SelectedItem?.ToString();
            comboBox_TempStandard.Items.Clear();
            MeshP_Param = new OCT_DatabaseHelper("OCTParameters");
            temp_standards = MeshP_Param.GetAllData("TempMeshP");
            foreach (var standard in temp_standards)
            {
                comboBox_TempStandard.Items.Add(standard.Name);
            }

            if (currentSelected != null && comboBox_TempStandard.Items.Contains(currentSelected))
            {
                comboBox_TempStandard.SelectedItem = currentSelected;
            }
        }

        private void comboBox_CardType_changed(object sender, EventArgs e)
        {
            string selectname = comboBox_CardType.SelectedItem?.ToString();
            if (selectname == "白卡")
            {
                CurrentCardType_Params = WC_Params;
            }
            else if (selectname == "雙銅")
            {
                CurrentCardType_Params = DC_Params;
            }
            LoadTemplates(selectname);
        }

        private void button_Output_Click(object sender, EventArgs e)
        {
            if (textBox_ProductionItem.Text == "")
            {
                MessageBox.Show("請輸入品名!", "資訊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("你確定要儲存？", "資訊", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                DateTime now = DateTime.Now;
                string standardname = radioButton_AbsStandard.Checked ? "abs" : comboBox_TempStandard.Text;

                SaveData savedata = new SaveData()
                {
                    SourceImg1 = SourceImage1?.Clone(),
                    SourceImg2 = SourceImage2?.Clone(),
                    SourceImg3 = SourceImage3?.Clone(),
                    SourceImg4 = SourceImage4?.Clone(),
                    SourceImg5 = SourceImage5?.Clone(),

                    RawImg1 = RawImage1?.Clone(),
                    RawImg2 = RawImage2?.Clone(),
                    RawImg3 = RawImage3?.Clone(),
                    RawImg4 = RawImage4?.Clone(),
                    RawImg5 = RawImage5?.Clone(),
                    Date = now.ToString("yyyy/MM/dd"),
                    Time = now.ToString("HH:mm:ss"),
                    Lot_No = textBox_ProductionItem.Text,
                    Remark = textBox_Remarks.Text,
                    URL = Path.Combine(MainPage.ResultSavePath, "O_Result",
                                       $"{now.Year}{now.Month:D2}",
                                       $"{now.Hour:D2}{now.Minute:D2}{now.Second:D2}"),
                    CardType = comboBox_CardType.Text,
                    PrintType = radioButton_LabPrint.Checked ? "內側印鑑" : "外側印鑑",
                    ExposureTime = MainPage.ExposureTime,
                    GLED_Used_Counter = MainPage.GLED_Used_Counter,
                    MeshP_Results = MeshP_Results,
                    Yin_Defect_Results = Yin_Defect_Results,
                    Yin_Block_Results = Yin_Block_Results,
                    Yang_Defect_Results = Yang_Defect_Results,
                    Yang_Block_Results = Yang_Block_Results,
                    Fullness_Results = Fullness_Results,
                    MeshP_Standard = new OCT_ParameterMeshP
                    {
                        Name = standardname,
                        Area10 = meshp_current_thresholds[0],
                        Area20 = meshp_current_thresholds[1],
                        Area30 = meshp_current_thresholds[2],
                        Area40 = meshp_current_thresholds[3],
                        Area60 = meshp_current_thresholds[4],
                        Area70 = meshp_current_thresholds[5],
                        Area80 = meshp_current_thresholds[6],
                        Area90 = meshp_current_thresholds[7]
                    },
                    Yin_Parameter = CurrentCardType_Params.yin_parameter,
                    Yang_Parameter = CurrentCardType_Params.yang_parameter,
                    Fullness_Parameter = CurrentCardType_Params.fullness_parameter,
                    MeshP_Rule = OCT_Rules.meshp_rule,
                    Yin_Rule = OCT_Rules.yin_rule,
                    Yang_Rule = OCT_Rules.yang_rule,
                    Fullness_Rule = OCT_Rules.fullness_rule
                };

                try
                {
                    OCT_SaveFileHelper.Save(savedata);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"儲存或拼接時發生錯誤: {ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                SaveResultFlag = false;
                OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, "儲存結果成功");
                MessageBox.Show("儲存成功!", "系統資訊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearAll();
            }
        }

        private void button_CatchImg_Click(object sender, EventArgs e)
        {
            if (SaveResultFlag)
            {
                if (MessageBox.Show("剛剛的結果尚未儲存，確定要繼續嗎?", "資訊", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
                SaveResultFlag = false;
            }
            ClearPictureBox_SourceImg();
            ClearUI();

            OpenFileDialog Openfile = new OpenFileDialog();
            Openfile.Title = "圖片開啟";
            Openfile.Filter = @"bmp|*.bmp|jpeg|*.jpg|bmp|*.bmp|gif|*.gif|tif|*.tif";
            Openfile.FilterIndex = 1;
            PictureBox temp = MainPage.switchbox;
            if (Openfile.ShowDialog() == DialogResult.OK)
            {
                Mat Myimg = CvInvoke.Imread(Openfile.FileName);
                temp.Image = Myimg.ToBitmap();
                GC.Collect();
                ReadSourceImgFlag = true;
            }
        }

        private void Label_Click(object sender, EventArgs e)
        {
            var label = sender as Label;
            SingleAreaInfo single_area_info = label?.Tag as SingleAreaInfo;

            if (single_area_info == null) return;

            OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, $"重新計算{single_area_info.Label_Name}頁面開啟成功");
            Form_ReAnalyze StandardO_ReAnalyze = new Form_ReAnalyze(single_area_info);
            var result = StandardO_ReAnalyze.ShowDialog();
            if (result != DialogResult.OK) return;

            if (single_area_info.Type == "meshp")
            {
                try
                {
                    (MeshP_Imgs, MeshP_Result_Imgs, MeshP_Results) = OCT_AlgorithmHelper.Single_MeshP_anylz(MeshP_Imgs, MeshP_Result_Imgs, MeshP_Results, single_area_info, meshp_current_thresholds, SaveOptions);
                    MeshP_Results = OCT_AlgorithmHelper.Evaluate_MeshP(OCT_Rules, MeshP_Results);
                    Update_MeshP(MeshP_Imgs, MeshP_Results);
                }
                catch (Exception) { return; }
            }
            else if (single_area_info.Type == "yin")
            {
                try
                {
                    (Yin_Imgs, Yin_Block_Imgs, Yin_Block_Results, Yin_Defect_Imgs, Yin_Defect_Results, Yin_Font_Infos) = OCT_AlgorithmHelper.Single_Yin_analz(Yin_Imgs, Yin_Block_Imgs, Yin_Block_Results, Yin_Defect_Imgs, Yin_Defect_Results, single_area_info, CurrentCardType_Params, Yin_Font_Infos, YinTemplates, SaveOptions);
                    (Yin_Defect_Results, Yin_Block_Results) = OCT_AlgorithmHelper.Evaluate_Yin(OCT_Rules, Yin_Defect_Results, Yin_Block_Results);
                    Update_Yin(Yin_Defect_Results, Yin_Block_Results);
                }
                catch (Exception) { return; }
            }
            else if (single_area_info.Type == "yang")
            {
                try
                {
                    (Yang_Imgs, Yang_Block_Imgs, Yang_Block_Results, Yang_Defect_Imgs, Yang_Defect_Results, Yang_Font_Infos) = OCT_AlgorithmHelper.Single_Yang_analz(Yang_Imgs, Yang_Block_Imgs, Yang_Block_Results, Yang_Defect_Imgs, Yang_Defect_Results, single_area_info, CurrentCardType_Params, Yang_Font_Infos, YangTemplates, SaveOptions);
                    (Yang_Defect_Results, Yang_Block_Results) = OCT_AlgorithmHelper.Evaluate_Yang(OCT_Rules, Yang_Defect_Results, Yang_Block_Results);
                    Update_Yang(Yang_Defect_Results, Yang_Block_Results);
                }
                catch (Exception) { return; }
            }
            else if (single_area_info.Type == "fullness")
            {
                try
                {
                    Fullness_Results = OCT_AlgorithmHelper.Single_Fullness_analz(Fullness_Results, single_area_info, CurrentCardType_Params, SaveOptions);
                    Fullness_Results = OCT_AlgorithmHelper.Evaluate_Fullness(OCT_Rules, Fullness_Results);
                    Update_Fullness(Fullness_Results);
                }
                catch (Exception ex)
                {
                    OCT_LogHelper.WriteLog(LogLevel.Error, Page.O, $"重新計算飽滿區失敗：{ex.Message}");
                    return;
                }
            }
            OCT_LogHelper.WriteLog(LogLevel.Info, Page.O, $"重新計算{single_area_info.Label_Name}成功");
        }

        private void Label_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Label label) label.BackColor = Color.Gray;
        }

        private void Label_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Label label) label.BackColor = Color.DarkGray;
        }
        #endregion

        #region UI狀態更新
        private void ClearUI()
        {
            PictureBox[] pictureBoxes =
            {
                pictureBox_1, pictureBox_2, pictureBox_3, pictureBox_4,
                pictureBox_MeshP10, pictureBox_MeshP20, pictureBox_MeshP30, pictureBox_MeshP40,
                pictureBox_MeshP60, pictureBox_MeshP70, pictureBox_MeshP80, pictureBox_MeshP90,
                pictureBox_Fullness, pictureBox_Yin, pictureBox_Yang
            };

            foreach (var pictureBox in pictureBoxes)
            {
                if (pictureBox.Image != null)
                {
                    pictureBox.Image.Dispose();
                    pictureBox.Image = null;
                }
            }

            Label[] label_results =
            {
                label_MeshP_Result10, label_MeshP_Result20, label_MeshP_Result30, label_MeshP_Result40,
                label_MeshP_Result60, label_MeshP_Result70, label_MeshP_Result80, label_MeshP_Result90,
                label_MeshPDefectLevel, label_MeshPBlockLevel,
                label_YinDefect_3pt, label_YinDefect_4pt, label_YinDefect_5pt, label_YinDefect_6pt,
                label_YinDefect_7pt, label_YinDefect_8pt, label_YinDefect_9pt, label_YinDefect_10pt,
                label_YinDefect_11pt, label_YinDefect_12pt, label_Yin_Total_DefectPercentage, label_YinDefectLevel,
                label_YinBlock_3pt, label_YinBlock_4pt, label_YinBlock_5pt, label_YinBlock_6pt,
                label_YinBlock_7pt, label_YinBlock_8pt, label_YinBlock_9pt, label_YinBlock_10pt,
                label_YinBlock_11pt, label_YinBlock_12pt, label_Yin_Total_BlockPercentage, label_YinBlockLevel,
                label_YangDefect_3pt, label_YangDefect_4pt, label_YangDefect_5pt, label_YangDefect_6pt,
                label_YangDefect_7pt, label_YangDefect_8pt, label_YangDefect_9pt, label_YangDefect_10pt,
                label_YangDefect_11pt, label_YangDefect_12pt, label_Yang_Total_DefectPercentage, label_YangDefectLevel,
                label_YangBlock_3pt, label_YangBlock_4pt, label_YangBlock_5pt, label_YangBlock_6pt,
                label_YangBlock_7pt, label_YangBlock_8pt, label_YangBlock_9pt, label_YangBlock_10pt,
                label_YangBlock_11pt, label_YangBlock_12pt, label_Yang_Total_BlockPercentage, label_YangBlockLevel,
                label_Fullness_Result, label_FullnessDefectLevel,
            };

            foreach (var label_result in label_results)
            {
                label_result.Text = string.Empty;
            }

            List<Label> label_Selectors = new List<Label>()
            {
                label_MeshP_area10, label_MeshP_area20, label_MeshP_area30, label_MeshP_area40,
                label_MeshP_area60, label_MeshP_area70, label_MeshP_area80, label_MeshP_area90,
                label_Yin_3pt, label_Yin_4pt, label_Yin_5pt, label_Yin_6pt, label_Yin_7pt,
                label_Yin_8pt, label_Yin_9pt, label_Yin_10pt, label_Yin_11pt, label_Yin_12pt,
                label_Yang_3pt, label_Yang_4pt, label_Yang_5pt, label_Yang_6pt, label_Yang_7pt,
                label_Yang_8pt, label_Yang_9pt, label_Yang_10pt, label_Yang_11pt, label_Yang_12pt, label_Fullness_area,
            };

            foreach (Label label_Selector in label_Selectors)
            {
                label_Selector.BackColor = Color.Black;
                label_Selector.MouseEnter -= Label_MouseEnter;
                label_Selector.MouseLeave -= Label_MouseLeave;
                label_Selector.Tag = null;
                label_Selector.Click -= Label_Click;
            }

            TextBox[] textboxes = { textBox_ProductionItem, textBox_Remarks };
            foreach (var textbox in textboxes)
            {
                textbox.Text = string.Empty;
            }
        }

        private void ClearPictureBox_SourceImg()
        {
            if (pictureBox_1.Image != null)
            {
                pictureBox_1.Image.Dispose();
                pictureBox_1.Image = null;
            }
        }

        private void Update_MeshP(MeshP_Imgs MeshP_Imgs, MeshP_Results meshp_results)
        {
            UpdateUI(() => pictureBox_MeshP10.Image = MeshP_Imgs.Area10?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP20.Image = MeshP_Imgs.Area20?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP30.Image = MeshP_Imgs.Area30?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP40.Image = MeshP_Imgs.Area40?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP60.Image = MeshP_Imgs.Area60?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP70.Image = MeshP_Imgs.Area70?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP80.Image = MeshP_Imgs.Area80?.ToBitmap());
            UpdateUI(() => pictureBox_MeshP90.Image = MeshP_Imgs.Area90?.ToBitmap());
            UpdateUI(() => label_MeshP_Result10.Text = meshp_results.Area10.ToString("F2"));
            UpdateUI(() => label_MeshP_Result20.Text = meshp_results.Area20.ToString("F2"));
            UpdateUI(() => label_MeshP_Result30.Text = meshp_results.Area30.ToString("F2"));
            UpdateUI(() => label_MeshP_Result40.Text = meshp_results.Area40.ToString("F2"));
            UpdateUI(() => label_MeshP_Result60.Text = meshp_results.Area60.ToString("F2"));
            UpdateUI(() => label_MeshP_Result70.Text = meshp_results.Area70.ToString("F2"));
            UpdateUI(() => label_MeshP_Result80.Text = meshp_results.Area80.ToString("F2"));
            UpdateUI(() => label_MeshP_Result90.Text = meshp_results.Area90.ToString("F2"));
            UpdateUI(() => label_MeshPDefectLevel.Text = meshp_results.Defect_Level.ToString("F1"));
            UpdateUI(() => label_MeshPBlockLevel.Text = meshp_results.Block_Level.ToString("F1"));
        }

        private void Update_Yin(Font_Results yin_defect_results, Font_Results yin_block_results)
        {
            UpdateUI(() => label_YinDefect_3pt.Text = yin_defect_results._3pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_4pt.Text = yin_defect_results._4pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_5pt.Text = yin_defect_results._5pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_6pt.Text = yin_defect_results._6pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_7pt.Text = yin_defect_results._7pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_8pt.Text = yin_defect_results._8pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_9pt.Text = yin_defect_results._9pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_10pt.Text = yin_defect_results._10pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_11pt.Text = yin_defect_results._11pt.ToString("F1"));
            UpdateUI(() => label_YinDefect_12pt.Text = yin_defect_results._12pt.ToString("F1"));
            UpdateUI(() => label_Yin_Total_DefectPercentage.Text = yin_defect_results.Defect_Total_Percentage.ToString("F1"));
            UpdateUI(() => label_YinDefectLevel.Text = yin_defect_results.Defect_Level.ToString("F1"));
            UpdateUI(() => label_YinBlock_3pt.Text = yin_block_results._3pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_4pt.Text = yin_block_results._4pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_5pt.Text = yin_block_results._5pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_6pt.Text = yin_block_results._6pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_7pt.Text = yin_block_results._7pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_8pt.Text = yin_block_results._8pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_9pt.Text = yin_block_results._9pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_10pt.Text = yin_block_results._10pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_11pt.Text = yin_block_results._11pt.ToString("F1"));
            UpdateUI(() => label_YinBlock_12pt.Text = yin_block_results._12pt.ToString("F1"));
            UpdateUI(() => label_Yin_Total_BlockPercentage.Text = yin_block_results.Block_Total_Percentage.ToString("F1"));
            UpdateUI(() => label_YinBlockLevel.Text = yin_block_results.Block_Level.ToString("F1"));
        }

        private void Update_Yang(Font_Results yang_defect_results, Font_Results yang_block_results)
        {
            UpdateUI(() => label_YangDefect_3pt.Text = yang_defect_results._3pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_4pt.Text = yang_defect_results._4pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_5pt.Text = yang_defect_results._5pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_6pt.Text = yang_defect_results._6pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_7pt.Text = yang_defect_results._7pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_8pt.Text = yang_defect_results._8pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_9pt.Text = yang_defect_results._9pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_10pt.Text = yang_defect_results._10pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_11pt.Text = yang_defect_results._11pt.ToString("F1"));
            UpdateUI(() => label_YangDefect_12pt.Text = yang_defect_results._12pt.ToString("F1"));
            UpdateUI(() => label_Yang_Total_DefectPercentage.Text = yang_defect_results.Defect_Total_Percentage.ToString("F1"));
            UpdateUI(() => label_YangDefectLevel.Text = yang_defect_results.Defect_Level.ToString("F1"));
            UpdateUI(() => label_YangBlock_3pt.Text = yang_block_results._3pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_4pt.Text = yang_block_results._4pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_5pt.Text = yang_block_results._5pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_6pt.Text = yang_block_results._6pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_7pt.Text = yang_block_results._7pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_8pt.Text = yang_block_results._8pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_9pt.Text = yang_block_results._9pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_10pt.Text = yang_block_results._10pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_11pt.Text = yang_block_results._11pt.ToString("F1"));
            UpdateUI(() => label_YangBlock_12pt.Text = yang_block_results._12pt.ToString("F1"));
            UpdateUI(() => label_Yang_Total_BlockPercentage.Text = yang_block_results.Block_Total_Percentage.ToString("F1"));
            UpdateUI(() => label_YangBlockLevel.Text = yang_block_results.Block_Level.ToString("F1"));
        }

        private void Update_Fullness(Fullness_Results fullness_results)
        {
            UpdateUI(() => label_Fullness_Result.Text = fullness_results.Defect_Result.ToString("F2"));
            UpdateUI(() => label_FullnessDefectLevel.Text = fullness_results.Defect_Level.ToString("F1"));
        }

        private void Label_Add_ClickEvent(MeshP_Imgs MeshP_Imgs, Font_Imgs Yin_Imgs, Font_Imgs Yang_Imgs, Mat fullness_area)
        {
            var label_Selectors = new List<Label>
            {
                label_MeshP_area10, label_MeshP_area20, label_MeshP_area30, label_MeshP_area40,
                label_MeshP_area60, label_MeshP_area70, label_MeshP_area80, label_MeshP_area90,
                label_Yin_3pt, label_Yin_4pt, label_Yin_5pt, label_Yin_6pt, label_Yin_7pt,
                label_Yin_8pt, label_Yin_9pt, label_Yin_10pt, label_Yin_11pt, label_Yin_12pt,
                label_Yang_3pt, label_Yang_4pt, label_Yang_5pt, label_Yang_6pt, label_Yang_7pt,
                label_Yang_8pt, label_Yang_9pt, label_Yang_10pt, label_Yang_11pt, label_Yang_12pt, label_Fullness_area,
            };

            var areas = new List<(string label_name, string type, string name, Mat img)>
            {
                ("網點區10%區域", "meshp", "Area10", MeshP_Imgs?.Area10 ?? new Mat()),
                ("網點區20%區域", "meshp", "Area20", MeshP_Imgs?.Area20 ?? new Mat()),
                ("網點區30%區域", "meshp", "Area30", MeshP_Imgs?.Area30 ?? new Mat()),
                ("網點區40%區域", "meshp", "Area40", MeshP_Imgs?.Area40 ?? new Mat()),
                ("網點區60%區域", "meshp", "Area60", MeshP_Imgs?.Area60 ?? new Mat()),
                ("網點區70%區域", "meshp", "Area70", MeshP_Imgs?.Area70 ?? new Mat()),
                ("網點區80%區域", "meshp", "Area80", MeshP_Imgs?.Area80 ?? new Mat()),
                ("網點區90%區域", "meshp", "Area90", MeshP_Imgs?.Area90 ?? new Mat()),
                ("陰版3pt區域", "yin", "3pt", Yin_Imgs?._3pt ?? new Mat()),
                ("陰版4pt區域", "yin", "4pt", Yin_Imgs?._4pt ?? new Mat()),
                ("陰版5pt區域", "yin", "5pt", Yin_Imgs?._5pt ?? new Mat()),
                ("陰版6pt區域", "yin", "6pt", Yin_Imgs?._6pt ?? new Mat()),
                ("陰版7pt區域", "yin", "7pt", Yin_Imgs?._7pt ?? new Mat()),
                ("陰版8pt區域", "yin", "8pt", Yin_Imgs?._8pt ?? new Mat()),
                ("陰版9pt區域", "yin", "9pt", Yin_Imgs?._9pt ?? new Mat()),
                ("陰版10pt區域", "yin", "10pt", Yin_Imgs?._10pt ?? new Mat()),
                ("陰版11pt區域", "yin", "11pt", Yin_Imgs?._11pt ?? new Mat()),
                ("陰版12pt區域", "yin", "12pt", Yin_Imgs?._12pt ?? new Mat()),
                ("陽版3pt區域", "yang", "3pt", Yang_Imgs?._3pt ?? new Mat()),
                ("陽版4pt區域", "yang", "4pt", Yang_Imgs?._4pt ?? new Mat()),
                ("陽版5pt區域", "yang", "5pt", Yang_Imgs?._5pt ?? new Mat()),
                ("陽版6pt區域", "yang", "6pt", Yang_Imgs?._6pt ?? new Mat()),
                ("陽版7pt區域", "yang", "7pt", Yang_Imgs?._7pt ?? new Mat()),
                ("陽版8pt區域", "yang", "8pt", Yang_Imgs?._8pt ?? new Mat()),
                ("陽版9pt區域", "yang", "9pt", Yang_Imgs?._9pt ?? new Mat()),
                ("陽版10pt區域", "yang", "10pt", Yang_Imgs?._10pt ?? new Mat()),
                ("陽版11pt區域", "yang", "11pt", Yang_Imgs?._11pt ?? new Mat()),
                ("陽版12pt區域", "yang", "12pt", Yang_Imgs?._12pt ?? new Mat()),
                ("飽滿區區域", "fullness", "Fullness", fullness_area ?? new Mat()),
            };

            for (int i = 0; i < label_Selectors.Count; i++)
            {
                label_Selectors[i].BackColor = Color.Gray;
                label_Selectors[i].MouseEnter += Label_MouseEnter;
                label_Selectors[i].MouseLeave += Label_MouseLeave;
                label_Selectors[i].Tag = new SingleAreaInfo()
                {
                    Label_Name = areas[i].label_name,
                    Image = areas[i].img,
                    Type = areas[i].type,
                    Name = areas[i].name,
                    Regions = new List<StandardOPage.Region>()
                };
                label_Selectors[i].Click += Label_Click;
            }
        }
        #endregion

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

        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }

        private void BreakArea_button_Click(object sender, EventArgs e)
        {
            if (!SaveResultFlag)
            {
                MessageBox.Show("請先完成樣品分析。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            BreakAreaForm breakAreaForm = new BreakAreaForm(BreakArea_01_crop, BreakArea_02_crop, BreakArea_03_crop);
            breakAreaForm.Show();
        }
    }
}