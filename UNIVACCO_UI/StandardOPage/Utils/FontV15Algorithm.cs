using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StandardOPage
{
    /// <summary>
    /// 字體區新對位分支 v15 的 C# / EmguCV 移植版。
    ///
    /// 對應 Python 主流程：
    /// 1. Yin / Yang 固定二值化（Yin=150、Yang=150 並反相）只負責 v15 對位。
    /// 2. 3pt~12pt 固定參考範圍（總寬 3550）。
    /// 3. 整條字體 0.5 倍 NCC 粗定位 start_x。
    /// 4. 每一字級在預期位置 ±40 px 做局部 NCC。
    /// 5. 再於 ±5 px 做 X/Y 精細 NCC 對位。
    /// 6. 對位完成後，實測影像改用工程介面各字級 Threshold 二值化。
    /// 7. Template - Measured 產生缺燙；Measured - Template 產生塞版。
    ///
    /// 為了相容目前 C-Board 舊模板（例如 21x575），若找不到真正的 v15
    /// 高解析模板，本類別會將舊模板等比例放大並置中到該字級的固定區段。
    /// 若 SaveOptions.MakeTemplates=true，會把本次 Golden Sample 的固定區段
    /// 另存成真正的 v15 高解析模板，之後會優先使用，不再走相容模板。
    /// </summary>
    public static class FontV15Algorithm
    {
        private static readonly string[] FontNames =
        {
            "3pt", "4pt", "5pt", "6pt", "7pt",
            "8pt", "9pt", "10pt", "11pt", "12pt"
        };

        // Python v15 的固定參考座標。
        private static readonly int[] ReferenceX =
        {
            0, 192, 454, 757, 1089, 1432,
            1836, 2239, 2673, 3111, 3550
        };

        private const int ReferenceWidth = 3550;
        private const int YinFixedThreshold = 150;
        private const int YangFixedThreshold = 150;
        private const int LocalAlignmentMaxShift = 40;
        private const int FineAlignmentMaxShift = 5;
        private const double CoarseScale = 0.5;
        // V24: full-resolution horizontal-profile refinement after coarse NCC.
        private const int CoarseRefineRadius = 20;
        // V25 diagnostic: keep complete Golden/measurement images. Exclude only
        // their top/bottom horizontal border bands from NCC and Edge scoring.
        // No changes to the aligned pixels or engineering defect/block masks.
        private const bool ExcludeTopBottomBordersForAlignment = true;
        private const int AlignmentBorderBandPixels = 36;

        // V17: independent capture groups, robust position trend and constrained rescanning.
        private const int GroupSplitIndex = 6;
        private const int GroupMinimumAnchors = 2;
        private const double AnchorMinimumScore = 0.55;
        private const int GroupOutlierX = 8;
        private const int GroupOutlierY = 6;
        private const int GroupRescanRadiusX = 8;
        private const double TrendOutlierX = 7.0;
        private const double TrendOutlierY = 5.0;
        private const double TrendMaxSlopeX = 0.035; // px shift / px position
        private const double TrendMaxSlopeY = 0.018;
        private const double MaximumAcceptedScoreLoss = 0.08;
        private const double MinimumAcceptedScore = 0.35;
        // V19 test: estimate rotation from 8pt only, for A exposure (3-8pt).
        // Angles mean rotating the GOLDEN template, not the measured image.
        private const bool EnableAGroupRotation = true;
        private const int RotationAnchorIndex = 5; // 8pt
        private const double RotationMinDegrees = -2.00;
        private const double RotationMaxDegrees = 2.00;
        private const double RotationStepDegrees = 0.25;
        private const double RotationMinimumGain = 0.006;
        private const int RotationAGroupFineRadius = 36; // Only the 8pt anchor may use this broad Y search.
        private const int RotationResidualYRadius = 5;
        private const int RotationVerifyFineRadius = 24;
        private const int RotationTopCandidates = 4;
        // V22: edge-chamfer is used only to rank a small number of rotation candidates.
        private const int EdgeChamferRadius = 4;
        private const int EdgeSampleStride = 3;
        private const double EdgeWeight = 0.65;
        // V18: 3pt-only rescue; do not modify the other font alignments.
        private const int ThreePtExtraLeft = 32;
        private const int ThreePtRescanRadius = 12;
        private const int ThreePtFineRadius = 12;

        // 最近一次正常分析所使用的工程參數。
        // ReAnalyze 會沿用同一個 CardType 的工程參數，避免人工重分析時又回到固定 150。
        // 正常分析仍由 OCT_AlgorithmHelper 明確傳入 current_params。
        private static OCT_Parameters_CardType _lastEngineeringParameters;
        private static readonly object EngineeringParameterLock = new object();

        private sealed class FineAlignResult : IDisposable
        {
            public Mat Aligned;
            public double Score;
            public int ShiftX;
            public int ShiftY;

            public void Dispose()
            {
                Aligned?.Dispose();
            }
        }

        private sealed class SegmentInspection : IDisposable
        {
            public string FontName;
            public int Index;
            public int ExpectedX1;
            public int ExpectedX2;
            public int FoundX1;
            public double CoarseScore;
            public double FineScore;
            public int ShiftX;
            public int ShiftY;
            public bool HitBoundary;
            public int OriginalShiftX;
            public int OriginalShiftY;
            public double OriginalScore;
            public string CorrectionStatus = "original";
            public string CaptureGroup;
            public double GroupShiftX;
            public double GroupShiftY;
            public double PredictedShiftX;
            public double PredictedShiftY;
            public string AlignmentDiagnostic = "";
            public double EngineeringThreshold;
            public Mat Original;
            public Mat Nominal;
            public Mat Selected;
            public Mat Template;
            public Mat Aligned;
            public Mat Defect;
            public Mat Block;
            public int DefectPixels;
            public int BlockPixels;
            public int TemplateForegroundPixels;
            public int TemplateBackgroundPixels;

            public double DefectPercentage
            {
                get
                {
                    return TemplateForegroundPixels <= 0
                        ? 0.0
                        : DefectPixels * 100.0 / TemplateForegroundPixels;
                }
            }

            public double BlockPercentage
            {
                get
                {
                    return TemplateBackgroundPixels <= 0
                        ? 0.0
                        : BlockPixels * 100.0 / TemplateBackgroundPixels;
                }
            }

            public void Dispose()
            {
                Nominal?.Dispose();
                Selected?.Dispose();
                Template?.Dispose();
                Aligned?.Dispose();
                // Original / Defect / Block 會交給 Font_Imgs，不在這裡 Dispose。
            }
        }

        public static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>) AnalyzeYin(
                Mat yinArea,
                List<TemplateData> legacyTemplates,
                OCT_Parameters_CardType currentParams,
                OCT_Parameters_SaveOptions saveOptions,
                string cardType)
        {
            RememberEngineeringParameters(currentParams);

            var result = AnalyzeSide(
                yinArea,
                legacyTemplates,
                true,
                currentParams,
                saveOptions,
                cardType);

            return (
                result.Item1,
                result.Item2,
                result.Item3,
                result.Item4,
                result.Item5,
                result.Item6);
        }

        /// <summary>
        /// 舊呼叫介面保留，避免其他測試工具立即失效。
        /// 正式主流程應使用有 currentParams 的 overload。
        /// </summary>
        public static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>) AnalyzeYin(
                Mat yinArea,
                List<TemplateData> legacyTemplates,
                OCT_Parameters_SaveOptions saveOptions,
                string cardType)
        {
            OCT_Parameters_CardType cached = GetRememberedEngineeringParameters();
            return AnalyzeYin(
                yinArea,
                legacyTemplates,
                cached,
                saveOptions,
                cardType);
        }

        public static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>,
            Mat) AnalyzeYang(
                Mat yangArea,
                List<TemplateData> legacyTemplates,
                OCT_Parameters_CardType currentParams,
                OCT_Parameters_SaveOptions saveOptions,
                string cardType,
                TaskCompletionSource<Mat> breakArea03Ready = null)
        {
            RememberEngineeringParameters(currentParams);

            var result = AnalyzeSide(
                yangArea,
                legacyTemplates,
                false,
                currentParams,
                saveOptions,
                cardType);

            Mat breakArea03 = new Mat();
            try
            {
                if (result.Item1 != null &&
                    result.Item1._12pt != null &&
                    !result.Item1._12pt.IsEmpty)
                {
                    breakArea03 = OCT_AlgorithmHelper.BreakArea_analz(
                        result.Item1._12pt,
                        saveOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FontV15][Yang][BreakArea03] " + ex);
                breakArea03 = new Mat();
            }

            breakArea03Ready?.TrySetResult(breakArea03);

            return (
                result.Item1,
                result.Item2,
                result.Item3,
                result.Item4,
                result.Item5,
                result.Item6,
                breakArea03);
        }

        /// <summary>
        /// 舊呼叫介面保留，避免其他測試工具立即失效。
        /// 正式主流程應使用有 currentParams 的 overload。
        /// </summary>
        public static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>,
            Mat) AnalyzeYang(
                Mat yangArea,
                List<TemplateData> legacyTemplates,
                OCT_Parameters_SaveOptions saveOptions,
                string cardType,
                TaskCompletionSource<Mat> breakArea03Ready = null)
        {
            OCT_Parameters_CardType cached = GetRememberedEngineeringParameters();
            return AnalyzeYang(
                yangArea,
                legacyTemplates,
                cached,
                saveOptions,
                cardType,
                breakArea03Ready);
        }

        /// <summary>
        /// Font v15 單一字級重新分析。
        ///
        /// 對位仍使用固定 150，不受工程介面參數改變，避免破壞目前已驗證的 NCC 對位。
        /// 對位完成後才用工程介面的 3pt~12pt 門檻重新二值化並計算缺燙/塞版。
        /// 人工框選區域是真正的忽略區：不計入缺燙/塞版，也不計入分母。
        /// </summary>
        public static (
            Mat blockImg,
            double blockPercentage,
            Mat defectImg,
            double defectPercentage,
            FontInfo fontInfo) ReAnalyzeSingleFont(
                Mat source,
                string fontName,
                bool isYin,
                List<TemplateData> legacyTemplates,
                IEnumerable<Region> excludedRegions,
                string cardType,
                OCT_Parameters_SaveOptions saveOptions)
        {
            return ReAnalyzeSingleFont(
                source,
                fontName,
                isYin,
                legacyTemplates,
                GetRememberedEngineeringParameters(),
                excludedRegions,
                cardType,
                saveOptions);
        }

        public static (
            Mat blockImg,
            double blockPercentage,
            Mat defectImg,
            double defectPercentage,
            FontInfo fontInfo) ReAnalyzeSingleFont(
                Mat source,
                string fontName,
                bool isYin,
                List<TemplateData> legacyTemplates,
                OCT_Parameters_CardType currentParams,
                IEnumerable<Region> excludedRegions,
                string cardType,
                OCT_Parameters_SaveOptions saveOptions)
        {
            if (source == null || source.IsEmpty)
                throw new ArgumentException("Font v15 重新分析來源影像為空");

            int index = Array.IndexOf(FontNames, fontName);
            if (index < 0)
                throw new ArgumentException("未知字級：" + fontName);

            if (legacyTemplates == null || legacyTemplates.Count != FontNames.Length)
                throw new ArgumentException("Template 數量錯誤，正常應為 10 張（3pt~12pt）");

            RememberEngineeringParameters(currentParams);

            string sideName = isYin ? "Yin" : "Yang";
            int targetWidth = ReferenceX[index + 1] - ReferenceX[index];

            Mat sourceGray = null;
            Mat measuredGray = null;
            Mat alignmentMeasuredBinary = null;
            Mat engineeringMeasuredBinary = null;
            Mat templateGray = null;
            Mat templateBinary = null;
            Mat engineeringAligned = null;
            Mat validMaskSource = null;
            Mat validMask = null;
            Mat alignedValidMask = null;
            Mat rawDefect = null;
            Mat rawBlock = null;
            Mat defect = null;
            Mat block = null;
            Mat templateForegroundValid = null;
            FineAlignResult fine = null;

            try
            {
                sourceGray = ToGray(source);

                templateGray = TryLoadHighResolutionTemplate(
                    sideName,
                    fontName,
                    cardType,
                    sourceGray.Height,
                    targetWidth);

                if (templateGray == null)
                {
                    templateGray = BuildCompatibilityGrayTemplate(
                        legacyTemplates[index].Image,
                        targetWidth,
                        sourceGray.Height,
                        isYin);
                }

                if (sourceGray.Width == targetWidth &&
                    sourceGray.Height == templateGray.Height)
                {
                    measuredGray = sourceGray.Clone();
                }
                else
                {
                    measuredGray = new Mat();
                    CvInvoke.Resize(
                        sourceGray,
                        measuredGray,
                        new Size(targetWidth, templateGray.Height),
                        0,
                        0,
                        Inter.Linear);
                }

                // 1) 對位：固定 150，保留目前 v15 行為。
                alignmentMeasuredBinary = FixedFontBinary(measuredGray, isYin);
                templateBinary = FixedFontBinary(templateGray, isYin);
                fine = FineAlignBinary(
                    alignmentMeasuredBinary,
                    templateBinary,
                    FineAlignmentMaxShift);

                // 2) 計算：改用工程介面該字級門檻。
                double engineeringThreshold = GetEngineeringThreshold(
                    currentParams,
                    index,
                    isYin,
                    saveOptions);

                engineeringMeasuredBinary = FontBinaryWithThreshold(
                    measuredGray,
                    isYin,
                    engineeringThreshold);

                engineeringAligned = AlignBinaryWithKnownShift(
                    engineeringMeasuredBinary,
                    templateBinary.Size,
                    FineAlignmentMaxShift,
                    fine.ShiftX,
                    fine.ShiftY);

                // 先在使用者看到的原圖座標建立有效遮罩，再縮放到 v15 比對尺寸。
                validMaskSource = new Mat(source.Size, DepthType.Cv8U, 1);
                validMaskSource.SetTo(new MCvScalar(255));

                if (excludedRegions != null)
                {
                    foreach (Region region in excludedRegions)
                    {
                        if (region?.Contour == null || region.Contour.Size < 3)
                            continue;

                        using (var contours =
                            new Emgu.CV.Util.VectorOfVectorOfPoint(region.Contour))
                        {
                            CvInvoke.FillPoly(
                                validMaskSource,
                                contours,
                                new MCvScalar(0));
                        }
                    }
                }

                if (validMaskSource.Size == engineeringMeasuredBinary.Size)
                {
                    validMask = validMaskSource.Clone();
                }
                else
                {
                    validMask = new Mat();
                    CvInvoke.Resize(
                        validMaskSource,
                        validMask,
                        engineeringMeasuredBinary.Size,
                        0,
                        0,
                        Inter.Nearest);
                }

                alignedValidMask = AlignMaskWithFineShift(
                    validMask,
                    templateBinary.Size,
                    FineAlignmentMaxShift,
                    fine.ShiftX,
                    fine.ShiftY);

                rawDefect = BuildDefectImage(
                    engineeringAligned,
                    templateBinary);
                rawBlock = BuildBlockImage(
                    engineeringAligned,
                    templateBinary);

                defect = new Mat();
                block = new Mat();

                CvInvoke.BitwiseAnd(rawDefect, alignedValidMask, defect);
                CvInvoke.BitwiseAnd(rawBlock, alignedValidMask, block);

                templateForegroundValid = new Mat();
                CvInvoke.BitwiseAnd(
                    templateBinary,
                    alignedValidMask,
                    templateForegroundValid);

                int defectPixels = CountWhitePixels(defect);
                int blockPixels = CountWhitePixels(block);
                int foregroundPixels = CountWhitePixels(templateForegroundValid);
                int validPixels = CountWhitePixels(alignedValidMask);
                int backgroundPixels = Math.Max(
                    0,
                    validPixels - foregroundPixels);

                double defectPercentage =
                    foregroundPixels <= 0
                        ? 0.0
                        : defectPixels * 100.0 / foregroundPixels;

                double blockPercentage =
                    backgroundPixels <= 0
                        ? 0.0
                        : blockPixels * 100.0 / backgroundPixels;

                FontInfo fontInfo = new FontInfo
                {
                    Fontname = fontName,
                    Bounds = new Rectangle(
                        0,
                        0,
                        source.Width,
                        source.Height),
                    Defect_Pixels = defectPixels,
                    Block_Pixels = blockPixels,
                    TemplateForegroundPixels = foregroundPixels,
                    TemplateBackgroundPixels = backgroundPixels
                };

                Debug.WriteLine(
                    $"[FontV15][ReAnalyze][{sideName}][{fontName}] " +
                    $"alignThreshold={(isYin ? YinFixedThreshold : YangFixedThreshold)}, " +
                    $"engineeringThreshold={engineeringThreshold:F2} " +
                    $"({ThresholdToPercent(engineeringThreshold):F1}%), " +
                    $"score={fine.Score:F6}, shift=({fine.ShiftX},{fine.ShiftY}), " +
                    $"defect={defectPixels} ({defectPercentage:F3}%), " +
                    $"block={blockPixels} ({blockPercentage:F3}%)");

                if (ShouldSaveDebug(saveOptions))
                {
                    SaveSingleReAnalyzeDebug(
                        sideName,
                        fontName,
                        templateBinary,
                        alignedValidMask,
                        engineeringAligned,
                        fine.Score,
                        fine.ShiftX,
                        fine.ShiftY,
                        engineeringThreshold,
                        defectPercentage,
                        blockPercentage);
                }

                Mat retBlock = block;
                Mat retDefect = defect;
                block = null;
                defect = null;

                return (
                    retBlock,
                    blockPercentage,
                    retDefect,
                    defectPercentage,
                    fontInfo);
            }
            finally
            {
                sourceGray?.Dispose();
                measuredGray?.Dispose();
                alignmentMeasuredBinary?.Dispose();
                engineeringMeasuredBinary?.Dispose();
                templateGray?.Dispose();
                templateBinary?.Dispose();
                engineeringAligned?.Dispose();
                validMaskSource?.Dispose();
                validMask?.Dispose();
                alignedValidMask?.Dispose();
                rawDefect?.Dispose();
                rawBlock?.Dispose();
                defect?.Dispose();
                block?.Dispose();
                templateForegroundValid?.Dispose();
                fine?.Dispose();
            }
        }

        /// <summary>
        /// 依 FontInfo 中目前有效像素重新計算 v15 整體缺燙/塞版百分比。
        /// </summary>
        public static void RecalculateTotals(
            Font_Results defectResults,
            Font_Results blockResults,
            List<FontInfo> fontInfos)
        {
            if (defectResults == null ||
                blockResults == null ||
                fontInfos == null)
                return;

            long defectPixels = 0;
            long blockPixels = 0;
            long foregroundPixels = 0;
            long backgroundPixels = 0;

            foreach (FontInfo info in fontInfos)
            {
                if (info == null)
                    continue;

                defectPixels += Math.Max(0, info.Defect_Pixels);
                blockPixels += Math.Max(0, info.Block_Pixels);
                foregroundPixels += Math.Max(
                    0,
                    info.TemplateForegroundPixels);
                backgroundPixels += Math.Max(
                    0,
                    info.TemplateBackgroundPixels);
            }

            defectResults.Defect_Total_Percentage =
                foregroundPixels > 0
                    ? defectPixels * 100.0 / foregroundPixels
                    : 0.0;

            blockResults.Block_Total_Percentage =
                backgroundPixels > 0
                    ? blockPixels * 100.0 / backgroundPixels
                    : 0.0;
        }

        /// <summary>
        /// 回傳：OriginalImgs, BlockImgs, BlockResults, DefectImgs, DefectResults, FontInfos
        /// </summary>

        /// <summary>
        /// 定位字元 Golden Sample。Visual Studio 的 bin\Debug、bin\Release 下執行時，
        /// 優先搜尋專案目錄 templates；發布 EXE 時則可將 templates 放在 EXE 旁。
        /// 支援 CSV/BMP 直接放在 templates，或 templates\GoldenSample 子目錄。
        /// </summary>
        private static string ResolveCharacterGoldenPath()
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            var checkedPaths = new List<string>();
            while (dir != null)
            {
                string folder = Path.Combine(dir.FullName, "templates", "golden_characters_v2_top_only");
                checkedPaths.Add(folder);
                if (File.Exists(Path.Combine(folder, "character_coordinates.csv")))
                    return folder;
                dir = dir.Parent;
            }
            throw new FileNotFoundException(
                "找不到 Golden Sample CSV（保持 Python 原資料夾結構）。已搜尋：" +
                Environment.NewLine + string.Join(Environment.NewLine, checkedPaths));
        }

        // Native C# character matcher integrated into the existing O-station output contract.
        // Golden reference is read from deployment's GoldenSample folder (CSV + 282 BMP files).
        // No Python process / modules are loaded.
        private static (
            Font_Imgs, Font_Imgs, Font_Results, Font_Imgs, Font_Results, List<FontInfo>)
            AnalyzeSideNative(Mat area, List<TemplateData> legacyTemplates,
                bool isYin, OCT_Parameters_CardType currentParams,
                OCT_Parameters_SaveOptions saveOptions, string cardType)
        {
            if (area == null || area.IsEmpty)
                throw new ArgumentException("O-station font image is empty");
            string side = isYin ? "Yin" : "Yang";
            string goldenPath = ResolveCharacterGoldenPath();
            var reference = GoldenCharacters.Load(goldenPath, side);
            var groups = NativeCharacterAlignmentPipeline.Analyze(area, goldenPath, side);
            if (ShouldSaveDebug(saveOptions))
            {
                try
                {
                    SaveNativeAlignmentDebug(groups, side);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[NativeFont][DebugSave] " + ex);
                }
            }
            var original = new Font_Imgs();
            var blocks = new Font_Imgs();
            var defects = new Font_Imgs();
            var blockStats = new Font_Results();
            var defectStats = new Font_Results();
            var infos = new List<FontInfo>();
            double allMiss = 0, allExtra = 0, allForeground = 0, allBackground = 0;
            for (int pt = 3; pt <= 12; pt++)
            {
                int fontIndex = pt - 3;
                var group = groups.Single(g => g.Group == (pt <= 8 ? "A" : "B"));
                int left = reference.xoffsets[pt] - (pt <= 8 ? reference.xoffsets[3] : reference.xoffsets[9]);
                int next = pt == 12 ? reference.reference.GetLength(1) : reference.xoffsets[pt + 1];
                int width = next - reference.xoffsets[pt];
                int height = group.Golden.GetLength(0);
                var goldenFont = CharacterBandMatcher.Slice(group.Golden, left, 0, width, height);
                var alignedFont = CharacterBandMatcher.Slice(group.Aligned, left, 0, width, height);
                Mat template = CharacterBandMatcher.WriteGray(goldenFont);
                Mat aligned = CharacterBandMatcher.WriteGray(alignedFont);
                Mat raw = CharacterBandMatcher.WriteGray(alignedFont);
                Mat defect = null, block = null;
                try
                {
                    defect = BuildDefectImage(aligned, template);
                    block = BuildBlockImage(aligned, template);
                    int miss = CountWhitePixels(defect);
                    int extra = CountWhitePixels(block);
                    int fg = CountWhitePixels(template);
                    int bg = width * height - fg;
                    // Original station display and result consumers retain same per-font Mat objects.
                    SetFontImg(original, fontIndex, EnsureBgr(raw));
                    SetFontImg(blocks, fontIndex, block);
                    SetFontImg(defects, fontIndex, defect);
                    SetFontResult(blockStats, fontIndex, bg > 0 ? extra * 100.0 / bg : 0);
                    SetFontResult(defectStats, fontIndex, fg > 0 ? miss * 100.0 / fg : 0);
                    infos.Add(new FontInfo
                    {
                        Fontname = FontNames[fontIndex],
                        Bounds = new Rectangle(group.StripStartX + (pt <= 8 ? reference.xoffsets[3] : reference.xoffsets[9]) + left,
                            0, width, height),
                        Block_Pixels = extra,
                        Defect_Pixels = miss,
                        TemplateForegroundPixels = fg,
                        TemplateBackgroundPixels = bg
                    });
                    allMiss += miss; allExtra += extra; allForeground += fg; allBackground += bg;
                    // Transfer ownership to result objects.
                    block = null; defect = null;
                    Debug.WriteLine($"[NativeFont][{side}][{pt}pt] aligned characters={group.Characters.Count(c => c.FontPt == pt)}");
                }
                finally
                {
                    template.Dispose(); aligned.Dispose(); raw.Dispose();
                    block?.Dispose(); defect?.Dispose();
                }
            }
            defectStats.Defect_Total_Percentage = allForeground > 0 ? allMiss * 100 / allForeground : 0;
            blockStats.Block_Total_Percentage = allBackground > 0 ? allExtra * 100 / allBackground : 0;
            return (original, blocks, blockStats, defects, defectStats, infos);
        }

        private static (
            Font_Imgs,
            Font_Imgs,
            Font_Results,
            Font_Imgs,
            Font_Results,
            List<FontInfo>) AnalyzeSide(
                Mat area,
                List<TemplateData> legacyTemplates,
                bool isYin,
                OCT_Parameters_CardType currentParams,
                OCT_Parameters_SaveOptions saveOptions,
                string cardType)
        {
            return AnalyzeSideNative(area, legacyTemplates, isYin, currentParams, saveOptions, cardType);
            string sideName = isYin ? "Yin" : "Yang";
            Stopwatch sw = Stopwatch.StartNew();

            if (area == null || area.IsEmpty)
                throw new ArgumentException(sideName + " area 為空");

            if (area.Width < ReferenceWidth)
            {
                throw new ArgumentException(
                    sideName + " 實測寬度不足，v15 至少需要 " +
                    ReferenceWidth + " px，目前=" + area.Width + " px");
            }

            if (legacyTemplates == null || legacyTemplates.Count != 10)
            {
                throw new ArgumentException(
                    sideName + " Template 數量錯誤，正常應為 10 張（3pt~12pt）");
            }

            Mat gray = null;
            Mat measuredBinaryFull = null;
            Mat fullTemplate = null;
            Mat fullTemplateBinary = null;
            Mat measuredBinary = null;
            Mat grayComparison = null;
            Font_Imgs originalImgs = new Font_Imgs();
            Font_Imgs blockImgs = new Font_Imgs();
            Font_Imgs defectImgs = new Font_Imgs();
            Font_Results blockResults = new Font_Results();
            Font_Results defectResults = new Font_Results();
            List<FontInfo> fontInfos = new List<FontInfo>();
            List<SegmentInspection> segmentResults = new List<SegmentInspection>();

            try
            {
                gray = ToGray(area);
                measuredBinaryFull = FixedFontBinary(gray, isYin);

                bool compatibilityTemplateUsed;
                fullTemplate = BuildFullStripTemplate(
                    sideName,
                    cardType,
                    gray.Height,
                    legacyTemplates,
                    isYin,
                    out compatibilityTemplateUsed);

                fullTemplateBinary = FixedFontBinary(fullTemplate, isYin);

                double coarseScore;
                int startX = FindStripStart(
                    measuredBinaryFull,
                    fullTemplateBinary,
                    out coarseScore);

                measuredBinary = new Mat(
                    measuredBinaryFull,
                    new Rectangle(startX, 0, ReferenceWidth, measuredBinaryFull.Height)
                ).Clone();

                grayComparison = new Mat(
                    gray,
                    new Rectangle(startX, 0, ReferenceWidth, gray.Height)
                ).Clone();

                Debug.WriteLine(
                    $"[FontV15][{sideName}] startX={startX}, coarse={coarseScore:F6}, " +
                    $"compatibilityTemplate={compatibilityTemplateUsed}, area={area.Width}x{area.Height}");

                // MakeTemplates 時，以本次 Golden Sample 直接建立真正的 v15 高解析 Template。
                if (saveOptions != null && saveOptions.saveoption.MakeTemplates)
                {
                    SaveHighResolutionTemplates(
                        grayComparison,
                        sideName,
                        cardType);
                }

                double totalDefectPixels = 0.0;
                double totalBlockPixels = 0.0;
                double totalTemplateForeground = 0.0;
                double totalTemplateBackground = 0.0;

                // V23: independent *diagnostic only*. Do not modify production alignment.
                if (ShouldSaveDebug(saveOptions))
                {
                    try { SaveRigidRotationDiagnostic(fullTemplateBinary, measuredBinary, sideName); }
                    catch (Exception ex) { Debug.WriteLine("[FontV23][RigidDiagnostic] " + ex); }
                }

                // V19: rotate Golden A-group about the 8pt center; B pixels stay identical.
                // Keep zero angle when improvement over the baseline is insufficient.
                double v19Angle = 0, v19Baseline = 0, v19Best = 0;
                string v19Status = "disabled";
                if (EnableAGroupRotation)
                {
                    try
                    {
                        ApplyAGroupRotationFromEightPt(fullTemplateBinary,
                            measuredBinary, out v19Angle, out v19Baseline,
                            out v19Best, out v19Status, sideName, saveOptions);
                    }
                    catch (Exception rotationError)
                    {
                        v19Status = "rotation_error_" + rotationError.GetType().Name;
                        Debug.WriteLine("[FontV19] " + rotationError);
                    }
                }
                Debug.WriteLine(String.Format("[FontV19][{0}] Golden angle={1:F2}, " +
                    "8pt baseline={2:F6}, best={3:F6}, status={4}",
                    sideName, v19Angle, v19Baseline, v19Best, v19Status));
                if (ShouldSaveDebug(saveOptions))
                {
                    try
                    {
                        string v19Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            "FontV15_Debug", sideName);
                        Directory.CreateDirectory(v19Dir);
                        File.WriteAllText(Path.Combine(v19Dir, "rotation_v19.txt"),
                            String.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "reference=8pt\nrotation_target=golden_A_3to8pt\nangle_degrees={0:F3}\n" +
                            "score_8pt_0deg={1:F6}\nscore_8pt_selected={2:F6}\nstatus={3}\n",
                            v19Angle, v19Baseline, v19Best, v19Status));
                    }
                    catch (Exception logError) { Debug.WriteLine("[FontV19][Log] " + logError); }
                }

                int v19AFineRadius = Math.Abs(v19Angle) > 0.000001
                    ? RotationAGroupFineRadius : FineAlignmentMaxShift;

                // V21: measure a real Y anchor at 8pt, then constrain other A-group
                // residual Ys around the rotation-induced geometric prediction.
                // A large 8pt radius is used only for this reference, never freely
                // for every small-font segment.
                double? aAnchorY = null;
                if (Math.Abs(v19Angle) > 0.000001)
                {
                    SegmentInspection anchor = InspectSegment(area, grayComparison,
                        measuredBinary, fullTemplateBinary, currentParams, saveOptions,
                        startX, RotationAnchorIndex, sideName, isYin,
                        null, null, RotationAGroupFineRadius);
                    aAnchorY = anchor.ShiftY;
                    DisposeInspection(anchor);
                }
                // Stage 1: independent V15 alignment for all segments.
                for (int i = 0; i < FontNames.Length; i++)
                {
                    double? expectedY = aAnchorY.HasValue && i < GroupSplitIndex
                        ? PredictRotationShiftY(aAnchorY.Value, v19Angle, i) : (double?)null;
                    SegmentInspection seg = InspectSegment(
                        area, grayComparison, measuredBinary, fullTemplateBinary,
                        currentParams, saveOptions, startX, i, sideName, isYin,
                        null, null, i < GroupSplitIndex ? v19AFineRadius : FineAlignmentMaxShift,
                        expectedY, expectedY.HasValue ? RotationResidualYRadius : (int?)null);
                    seg.OriginalShiftX = seg.ShiftX;
                    seg.OriginalShiftY = seg.ShiftY;
                    seg.OriginalScore = seg.FineScore;
                    seg.CaptureGroup = i < GroupSplitIndex ? "A(3-8pt)" : "B(9-12pt)";
                    segmentResults.Add(seg);
                }

                // Stage 2: each physical exposure has its OWN robust reference.
                CorrectCaptureGroup(segmentResults, 0, GroupSplitIndex,
                    area, grayComparison, measuredBinary, fullTemplateBinary,
                    currentParams, saveOptions, startX, sideName, isYin, v19AFineRadius);
                // 3pt may need pixels to the LEFT of the globally cropped 3550px strip.
                // Rescue it from the original camera image after the A-group trend is known.
                RescueThreePt(segmentResults, area, gray, measuredBinaryFull,
                    fullTemplateBinary, currentParams, saveOptions, startX, sideName, isYin, v19AFineRadius);
                CorrectCaptureGroup(segmentResults, GroupSplitIndex, FontNames.Length,
                    area, grayComparison, measuredBinary, fullTemplateBinary,
                    currentParams, saveOptions, startX, sideName, isYin, FineAlignmentMaxShift);

                // V21 diagnostic: show geometric Y prediction separately from final Y.
                if (ShouldSaveDebug(saveOptions) && aAnchorY.HasValue)
                {
                    try
                    {
                        string yDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            "FontV15_Debug", sideName);
                        Directory.CreateDirectory(yDir);
                        var yLines = new List<string>();
                        yLines.Add("angle_deg=" + v19Angle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                        yLines.Add("anchor_8pt_y=" + aAnchorY.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
                        yLines.Add("font,predicted_y,final_y,residual_y");
                        for (int k = 0; k < GroupSplitIndex; k++)
                        {
                            double py = PredictRotationShiftY(aAnchorY.Value, v19Angle, k);
                            yLines.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0},{1:F3},{2},{3:F3}", FontNames[k], py,
                                segmentResults[k].ShiftY, segmentResults[k].ShiftY - py));
                        }
                        File.WriteAllLines(Path.Combine(yDir, "rotation_y_v21.txt"), yLines);
                    }
                    catch (Exception logError) { Debug.WriteLine("[FontV21][YLog] " + logError); }
                }

                // Stage 3: publish final aligned images and recompute ALL statistics.
                for (int i = 0; i < segmentResults.Count; i++)
                {
                    SegmentInspection seg = segmentResults[i];
                    SetFontImg(originalImgs, i, seg.Original);
                    SetFontImg(blockImgs, i, seg.Block);
                    SetFontImg(defectImgs, i, seg.Defect);
                    SetFontResult(blockResults, i, seg.BlockPercentage);
                    SetFontResult(defectResults, i, seg.DefectPercentage);

                    fontInfos.Add(new FontInfo
                    {
                        Fontname = seg.FontName,
                        Bounds = new Rectangle(
                            startX + seg.FoundX1,
                            0,
                            seg.ExpectedX2 - seg.ExpectedX1,
                            area.Height),
                        Block_Pixels = seg.BlockPixels,
                        Defect_Pixels = seg.DefectPixels,
                        TemplateForegroundPixels = seg.TemplateForegroundPixels,
                        TemplateBackgroundPixels = seg.TemplateBackgroundPixels
                    });

                    totalDefectPixels += seg.DefectPixels;
                    totalBlockPixels += seg.BlockPixels;
                    totalTemplateForeground += seg.TemplateForegroundPixels;
                    totalTemplateBackground += seg.TemplateBackgroundPixels;

                    Debug.WriteLine(
                        $"[FontV15][{sideName}][{seg.FontName}] " +
                        $"score={seg.FineScore:F6}, shift=({seg.ShiftX},{seg.ShiftY}), " +
                        $"coarseX={seg.FoundX1}, boundary={seg.HitBoundary}, " +
                        $"engineeringThreshold={seg.EngineeringThreshold:F2} " +
                        $"({ThresholdToPercent(seg.EngineeringThreshold):F1}%), " +
                        $"defect={seg.DefectPixels} ({seg.DefectPercentage:F3}%), " +
                        $"block={seg.BlockPixels} ({seg.BlockPercentage:F3}%)");
                }

                defectResults.Defect_Total_Percentage =
                    totalTemplateForeground <= 0
                        ? 0.0
                        : totalDefectPixels * 100.0 / totalTemplateForeground;

                blockResults.Block_Total_Percentage =
                    totalTemplateBackground <= 0
                        ? 0.0
                        : totalBlockPixels * 100.0 / totalTemplateBackground;

                if (ShouldSaveDebug(saveOptions))
                {
                    SaveDebug(
                        sideName,
                        gray,
                        measuredBinaryFull,
                        grayComparison,
                        fullTemplate,
                        fullTemplateBinary,
                        measuredBinary,
                        startX,
                        coarseScore,
                        compatibilityTemplateUsed,
                        segmentResults,
                        defectResults,
                        blockResults);
                }

                sw.Stop();
                Debug.WriteLine(
                    $"[FontV15][{sideName}] 完成，耗時={sw.ElapsedMilliseconds} ms, " +
                    $"DefectTotal={defectResults.Defect_Total_Percentage:F3}%, " +
                    $"BlockTotal={blockResults.Block_Total_Percentage:F3}%");

                // SegmentInspection 不 Dispose Original/Defect/Block，因為已交給回傳物件。
                foreach (SegmentInspection seg in segmentResults)
                {
                    seg.Original = null;
                    seg.Defect = null;
                    seg.Block = null;
                    seg.Dispose();
                }
                segmentResults.Clear();

                return (
                    originalImgs,
                    blockImgs,
                    blockResults,
                    defectImgs,
                    defectResults,
                    fontInfos);
            }
            catch
            {
                originalImgs?.Dispose();
                blockImgs?.Dispose();
                defectImgs?.Dispose();
                foreach (SegmentInspection seg in segmentResults)
                {
                    seg.Original?.Dispose();
                    seg.Defect?.Dispose();
                    seg.Block?.Dispose();
                    seg.Dispose();
                }
                throw;
            }
            finally
            {
                gray?.Dispose();
                measuredBinaryFull?.Dispose();
                fullTemplate?.Dispose();
                fullTemplateBinary?.Dispose();
                measuredBinary?.Dispose();
                grayComparison?.Dispose();
            }
        }

        // V17: fit an independent, robust affine shift trend per physical exposure.
        // Use pixel positions (rather than pt index) because glyph widths differ.
        private static double Median(IEnumerable<int> values)
        {
            int[] sorted = values.OrderBy(v => v).ToArray();
            if (sorted.Length == 0) return 0.0;
            int middle = sorted.Length / 2;
            return sorted.Length % 2 == 1 ? sorted[middle]
                : (sorted[middle - 1] + sorted[middle]) / 2.0;
        }

        private static double Clamp(double v, double lo, double hi)
        {
            return Math.Max(lo, Math.Min(hi, v));
        }

        private static double CenterX(SegmentInspection seg)
        {
            return (seg.ExpectedX1 + seg.ExpectedX2) * 0.5;
        }

        // Least-squares fit with one residual rejection pass. The fit is deliberately
        // not extrapolated beyond a conservative slope, so a false small-font match
        // cannot dominate the whole group.
        private static void FitTrend(
            List<SegmentInspection> anchors, bool vertical, double fallback,
            out double intercept, out double slope)
        {
            intercept = fallback;
            slope = 0.0;
            if (anchors.Count < 3) return;
            List<SegmentInspection> fit = anchors.ToList();
            for (int pass = 0; pass < 2; pass++)
            {
                double cx = fit.Average(CenterX);
                double cy = fit.Average(s => vertical ? s.ShiftY : s.ShiftX);
                double xx = fit.Sum(s => (CenterX(s) - cx) * (CenterX(s) - cx));
                double xy = fit.Sum(s => (CenterX(s) - cx) *
                    ((vertical ? s.ShiftY : s.ShiftX) - cy));
                double limit = vertical ? TrendMaxSlopeY : TrendMaxSlopeX;
                slope = xx > 0.001 ? Clamp(xy / xx, -limit, limit) : 0.0;
                intercept = cy - slope * cx;
                if (pass == 0 && fit.Count >= 4)
                {
                    double threshold = vertical ? TrendOutlierY : TrendOutlierX;
                    // C# forbids capturing out parameters in a lambda (CS1628).
                    // Take value snapshots before using them in the Where predicate.
                    double fittedIntercept = intercept;
                    double fittedSlope = slope;
                    var kept = fit.Where(s => Math.Abs(
                        (vertical ? s.ShiftY : s.ShiftX) -
                        (fittedIntercept + fittedSlope * CenterX(s))) <= threshold).ToList();
                    if (kept.Count >= 3 && kept.Count < fit.Count) fit = kept;
                }
            }
        }

        private static void DisposeInspection(SegmentInspection seg)
        {
            if (seg == null) return;
            seg.Original?.Dispose();
            seg.Defect?.Dispose();
            seg.Block?.Dispose();
            seg.Dispose();
        }

        private static void CorrectCaptureGroup(
            List<SegmentInspection> segments, int begin, int end,
            Mat originalArea, Mat grayComparison, Mat measuredBinary,
            Mat fullTemplateBinary, OCT_Parameters_CardType currentParams,
            OCT_Parameters_SaveOptions saveOptions, int globalStartX,
            string sideName, bool isYin, int groupFineRadius)
        {
            var group = segments.Skip(begin).Take(end - begin).ToList();
            var anchors = group.Where(s => s.FineScore >= AnchorMinimumScore &&
                                             !s.HitBoundary).ToList();
            if (anchors.Count < GroupMinimumAnchors)
            {
                foreach (var seg in group)
                {
                    seg.CorrectionStatus = "skipped_insufficient_anchors";
                    seg.AlignmentDiagnostic = "no_reliable_group_model";
                }
                return;
            }
            double medX = Median(anchors.Select(s => s.ShiftX));
            double medY = Median(anchors.Select(s => s.ShiftY));
            double ax, bx, ay, by;
            FitTrend(anchors, false, medX, out ax, out bx);
            FitTrend(anchors, true, medY, out ay, out by);
            foreach (var seg in group)
            {
                seg.GroupShiftX = medX;
                seg.GroupShiftY = medY;
                double predictedX = ax + bx * CenterX(seg);
                double predictedY = ay + by * CenterX(seg);
                seg.PredictedShiftX = predictedX;
                seg.PredictedShiftY = predictedY;
                double oldDistance = Math.Abs(seg.ShiftX - predictedX) +
                                     Math.Abs(seg.ShiftY - predictedY);
                bool suspicious = seg.HitBoundary || seg.FineScore < AnchorMinimumScore ||
                    Math.Abs(seg.ShiftX - predictedX) > TrendOutlierX ||
                    Math.Abs(seg.ShiftY - predictedY) > TrendOutlierY;
                if (!suspicious)
                {
                    seg.AlignmentDiagnostic = "trend_consistent";
                    continue;
                }

                // Reject impossible template positions rather than silently clamp a
                // negative left-edge shift (which produces misleading defect maps).
                int predictedCoarse = (int)Math.Round(predictedX);
                int minPossible = -seg.ExpectedX1;
                int maxPossible = measuredBinary.Width - seg.ExpectedX2;
                if (predictedCoarse < minPossible - GroupRescanRadiusX ||
                    predictedCoarse > maxPossible + GroupRescanRadiusX)
                {
                    seg.CorrectionStatus = "unresolved_outside_image";
                    seg.AlignmentDiagnostic = "predicted_coarse_out_of_image";
                    continue;
                }
                var centers = new List<int> { predictedCoarse };
                if (Math.Abs(predictedCoarse - (int)Math.Round(medX)) > 3)
                    centers.Add((int)Math.Round(medX));

                SegmentInspection best = null;
                double bestQuality = Double.NegativeInfinity;
                string failure = "";
                foreach (int center in centers.Distinct())
                {
                    SegmentInspection candidate = null;
                    try
                    {
                        candidate = InspectSegment(originalArea, grayComparison,
                            measuredBinary, fullTemplateBinary, currentParams,
                            saveOptions, globalStartX, seg.Index, sideName, isYin,
                            center, null, groupFineRadius,
                            seg.Index < GroupSplitIndex && groupFineRadius > FineAlignmentMaxShift
                                ? (double?)seg.ShiftY : null,
                            seg.Index < GroupSplitIndex && groupFineRadius > FineAlignmentMaxShift
                                ? (int?)RotationResidualYRadius : null);
                        double deviation = Math.Abs(candidate.ShiftX - predictedX) +
                                           Math.Abs(candidate.ShiftY - predictedY);
                        bool safe = !candidate.HitBoundary &&
                            Math.Abs(candidate.ShiftX - predictedX) <= TrendOutlierX &&
                            Math.Abs(candidate.ShiftY - predictedY) <= TrendOutlierY &&
                            candidate.FineScore >= MinimumAcceptedScore &&
                            candidate.FineScore >= seg.FineScore - MaximumAcceptedScoreLoss;
                        // Score and geometric plausibility jointly rank candidates.
                        // Both dimensions are evaluated; the log shift is never altered alone.
                        double quality = candidate.FineScore - 0.012 * deviation;
                        if (safe && quality > bestQuality)
                        {
                            DisposeInspection(best);
                            best = candidate;
                            bestQuality = quality;
                            candidate = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        failure = ex.GetType().Name + ": " + ex.Message;
                        Debug.WriteLine("[FontV17] " + sideName + " " + seg.FontName + ": " + ex);
                    }
                    finally { DisposeInspection(candidate); }
                }
                if (best != null)
                {
                    double newDistance = Math.Abs(best.ShiftX - predictedX) +
                                         Math.Abs(best.ShiftY - predictedY);
                    // Require a real geometric improvement, not just a second NCC run.
                    if (newDistance + 1.0 < oldDistance)
                    {
                        best.OriginalShiftX = seg.OriginalShiftX;
                        best.OriginalShiftY = seg.OriginalShiftY;
                        best.OriginalScore = seg.OriginalScore;
                        best.CaptureGroup = seg.CaptureGroup;
                        best.GroupShiftX = medX;
                        best.GroupShiftY = medY;
                        best.PredictedShiftX = predictedX;
                        best.PredictedShiftY = predictedY;
                        best.CorrectionStatus = "trend_rescan_accepted";
                        best.AlignmentDiagnostic = "constrained_ncc_pass";
                        DisposeInspection(seg);
                        segments[seg.Index] = best;
                        best = null;
                    }
                    else
                    {
                        seg.CorrectionStatus = "trend_rescan_rejected_keep_original";
                        seg.AlignmentDiagnostic = "insufficient_improvement";
                    }
                }
                else
                {
                    seg.CorrectionStatus = "trend_rescan_failed_keep_original";
                    seg.AlignmentDiagnostic = String.IsNullOrEmpty(failure)
                        ? "no_safe_candidate" : failure.Replace(',', ';');
                }
                DisposeInspection(best);
            }
        }


        // V18: a dedicated 3pt probe on the uncropped camera pixels.
        // The 3550px global strip discards a real left margin (startX pixels).
        // A negative predicted coarse shift can therefore be valid in the ORIGINAL
        // camera image even when it is impossible inside the cropped strip.
        private static void RescueThreePt(
            List<SegmentInspection> segments, Mat originalArea, Mat grayFull,
            Mat measuredBinaryFull, Mat templateBinary,
            OCT_Parameters_CardType currentParams,
            OCT_Parameters_SaveOptions saveOptions, int startX,
            string sideName, bool isYin, int aFineRadius)
        {
            if (segments.Count == 0) return;
            SegmentInspection old = segments[0];
            int left = Math.Min(ThreePtExtraLeft, startX);
            if (left < 8)
            {
                old.AlignmentDiagnostic += ";v18_no_left_margin";
                return;
            }
            int extendedStart = startX - left;
            if (extendedStart < 0 ||
                extendedStart + ReferenceWidth > measuredBinaryFull.Width ||
                extendedStart + ReferenceWidth > grayFull.Width)
            {
                old.AlignmentDiagnostic += ";v18_extended_strip_unavailable";
                return;
            }
            SegmentInspection candidate = null;
            string outcome = "not_evaluated";
            try
            {
                using (Mat extBinary = new Mat(measuredBinaryFull,
                    new Rectangle(extendedStart, 0, ReferenceWidth,
                        measuredBinaryFull.Height)))
                using (Mat extGray = new Mat(grayFull,
                    new Rectangle(extendedStart, 0, ReferenceWidth,
                        grayFull.Height)))
                {
                    // Convert predicted ORIGINAL-strip shift to extended-strip coords.
                    int center = (int)Math.Round(old.PredictedShiftX) + left;
                    // For invalid trend values, do not force an arbitrary candidate.
                    if (Double.IsNaN(old.PredictedShiftX) ||
                        Double.IsInfinity(old.PredictedShiftX))
                    {
                        outcome = "invalid_trend";
                    }
                    else
                    {
                        candidate = InspectSegment(originalArea, extGray, extBinary,
                            templateBinary, currentParams, saveOptions,
                            extendedStart, 0, sideName, isYin, center,
                            ThreePtRescanRadius, Math.Max(ThreePtFineRadius, aFineRadius),
                            aFineRadius > FineAlignmentMaxShift ? (double?)old.ShiftY : null,
                            aFineRadius > FineAlignmentMaxShift ? (int?)RotationResidualYRadius : null);
                        // Restore all external output coordinates to original strip.
                        candidate.ShiftX -= left;
                        candidate.FoundX1 -= left;
                        double deltaX = Math.Abs(candidate.ShiftX - old.PredictedShiftX);
                        double deltaY = Math.Abs(candidate.ShiftY - old.PredictedShiftY);
                        // Require physical candidate, an actual score and trend support.
                        bool valid = !candidate.HitBoundary &&
                            Math.Abs(candidate.ShiftX - old.ShiftX) >= 2 &&
                            deltaX <= 7.0 && deltaY <= 7.0 &&
                            candidate.FineScore >= 0.25 &&
                            candidate.FineScore >= old.FineScore - 0.025;
                        outcome = valid ? "accepted" : "rejected";
                        if (ShouldSaveDebug(saveOptions))
                            SaveThreePtProbe(sideName, old, candidate, outcome, left);
                        if (valid)
                        {
                            candidate.OriginalShiftX = old.OriginalShiftX;
                            candidate.OriginalShiftY = old.OriginalShiftY;
                            candidate.OriginalScore = old.OriginalScore;
                            candidate.CaptureGroup = old.CaptureGroup;
                            candidate.GroupShiftX = old.GroupShiftX;
                            candidate.GroupShiftY = old.GroupShiftY;
                            candidate.PredictedShiftX = old.PredictedShiftX;
                            candidate.PredictedShiftY = old.PredictedShiftY;
                            candidate.CorrectionStatus = "v18_3pt_extended_accepted";
                            candidate.AlignmentDiagnostic = "left_margin_rescue";
                            DisposeInspection(old);
                            segments[0] = candidate;
                            candidate = null;
                        }
                        else
                        {
                            old.CorrectionStatus = "v18_3pt_probe_rejected";
                            old.AlignmentDiagnostic += ";left_margin_probe_rejected";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                outcome = "error:" + ex.GetType().Name;
                old.AlignmentDiagnostic += ";v18_probe_error_" + ex.GetType().Name;
                Debug.WriteLine("[FontV18] 3pt probe: " + ex);
            }
            finally
            {
                Debug.WriteLine("[FontV18][" + sideName + "][3pt] " + outcome);
                DisposeInspection(candidate);
            }
        }

        private static void SaveThreePtProbe(string sideName,
            SegmentInspection original, SegmentInspection candidate,
            string outcome, int addedLeft)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "FontV15_Debug", sideName, "three_pt_v18");
                Directory.CreateDirectory(dir);
                CvInvoke.Imwrite(Path.Combine(dir, "01_template.bmp"), original.Template);
                CvInvoke.Imwrite(Path.Combine(dir, "02_original_aligned.bmp"), original.Aligned);
                CvInvoke.Imwrite(Path.Combine(dir, "03_candidate_aligned.bmp"), candidate.Aligned);
                using (Mat originalDiff = new Mat())
                using (Mat candidateDiff = new Mat())
                {
                    CvInvoke.AbsDiff(original.Aligned, original.Template, originalDiff);
                    CvInvoke.AbsDiff(candidate.Aligned, candidate.Template, candidateDiff);
                    CvInvoke.Imwrite(Path.Combine(dir, "04_original_diff.bmp"), originalDiff);
                    CvInvoke.Imwrite(Path.Combine(dir, "05_candidate_diff.bmp"), candidateDiff);
                }
                File.WriteAllText(Path.Combine(dir, "probe.txt"),
                    "outcome=" + outcome + Environment.NewLine +
                    "left_extra=" + addedLeft + Environment.NewLine +
                    "original_shift=" + original.ShiftX + "," + original.ShiftY + Environment.NewLine +
                    "original_score=" + original.FineScore.ToString("F6") + Environment.NewLine +
                    "predicted_shift=" + original.PredictedShiftX.ToString("F2") + "," + original.PredictedShiftY.ToString("F2") + Environment.NewLine +
                    "candidate_shift=" + candidate.ShiftX + "," + candidate.ShiftY + Environment.NewLine +
                    "candidate_score=" + candidate.FineScore.ToString("F6") + Environment.NewLine +
                    "candidate_boundary=" + candidate.HitBoundary);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FontV18] 3pt debug save failed: " + ex);
            }
        }

        // V20: coarse 8pt rotation search, followed by independent 3/6/8pt
        // validation. All angles rotate GOLDEN around the SAME 8pt reference center.
        // Candidate scoring is diagnostic, not the downstream defect judgement.
        private sealed class RotationCandidate
        {
            public double Angle;
            public double AnchorScore;
            public double Score3;
            public double Score6;
            public double Score8;
            public double Combined;
            public double Edge3;
            public double Edge6;
            public double Edge8;
            public double CombinedEdge;
            public double CombinedHybrid;
            public bool Verified;
        }

        private static void ApplyAGroupRotationFromEightPt(
            Mat goldenBinary, Mat measuredBinary,
            out double chosenAngle, out double zeroScore,
            out double selectedScore, out string status,
            string sideName, OCT_Parameters_SaveOptions saveOptions)
        {
            chosenAngle = 0.0;
            zeroScore = Double.NegativeInfinity;
            selectedScore = Double.NegativeInfinity;
            status = "zero_retained";
            int x1 = ReferenceX[RotationAnchorIndex];
            int x2 = ReferenceX[RotationAnchorIndex + 1];
            PointF center = new PointF((x1 + x2) * 0.5f, goldenBinary.Height * 0.5f);
            List<RotationCandidate> candidates = new List<RotationCandidate>();
            for (double angle = RotationMinDegrees;
                angle <= RotationMaxDegrees + 0.00001; angle += RotationStepDegrees)
            {
                double currentAngle = Math.Round(angle, 4);
                using (Mat rotated = Math.Abs(currentAngle) < 0.000001
                    ? goldenBinary.Clone() : RotateGolden(goldenBinary, currentAngle, center))
                using (Mat roi = new Mat(rotated,
                    new Rectangle(x1, 0, x2 - x1, rotated.Height)))
                {
                    double score = ScoreEightPtRotation(measuredBinary, roi);
                    candidates.Add(new RotationCandidate
                    {
                        Angle = currentAngle,
                        AnchorScore = score
                    });
                    if (Math.Abs(currentAngle) < 0.000001) zeroScore = score;
                }
            }
            // Always verify the zero-angle baseline; choose a few diverse high
            // ranking angles, rather than blindly using the 8pt maximum.
            List<RotationCandidate> ranked = candidates
                .OrderByDescending(c => c.AnchorScore).ToList();
            List<RotationCandidate> verify = new List<RotationCandidate>();
            RotationCandidate baseline = candidates.First(c => Math.Abs(c.Angle) < 0.000001);
            verify.Add(baseline);
            foreach (RotationCandidate candidate in ranked)
            {
                if (verify.Count >= RotationTopCandidates + 1) break;
                if (verify.Any(c => Math.Abs(c.Angle - candidate.Angle) < 0.001)) continue;
                verify.Add(candidate);
            }
            // Explicit manual observations of +/-1 degree should be diagnosable
            // even when those angles are not among 8pt's top scorers.
            foreach (double checkAngle in new double[] { -1.0, 1.0 })
            {
                RotationCandidate candidate = candidates.FirstOrDefault(
                    c => Math.Abs(c.Angle - checkAngle) < 0.001);
                if (candidate != null && !verify.Contains(candidate)) verify.Add(candidate);
            }
            foreach (RotationCandidate candidate in verify)
            {
                using (Mat rotated = Math.Abs(candidate.Angle) < 0.000001
                    ? goldenBinary.Clone() : RotateGolden(goldenBinary, candidate.Angle, center))
                {
                    // Each candidate gets independent XY search on each ROI.
                    double rotationAnchorY;
                    candidate.Score8 = ScoreRotationSegment(measuredBinary, rotated, 5,
                        null, out rotationAnchorY, out candidate.Edge8);
                    double ignoredY;
                    candidate.Score3 = ScoreRotationSegment(measuredBinary, rotated, 0,
                        PredictRotationShiftY(rotationAnchorY, candidate.Angle, 0),
                        out ignoredY, out candidate.Edge3);
                    candidate.Score6 = ScoreRotationSegment(measuredBinary, rotated, 3,
                        PredictRotationShiftY(rotationAnchorY, candidate.Angle, 3),
                        out ignoredY, out candidate.Edge6);
                    // 3pt is less reliable because of the left boundary, so 6pt
                    // and 8pt anchor geometry must still carry most of the weight.
                    candidate.Combined = 0.25 * candidate.Score3
                        + 0.35 * candidate.Score6 + 0.40 * candidate.Score8;
                    candidate.CombinedEdge = 0.15 * candidate.Edge3
                        + 0.40 * candidate.Edge6 + 0.45 * candidate.Edge8;
                    candidate.CombinedHybrid = EdgeWeight * candidate.CombinedEdge
                        + (1.0 - EdgeWeight) * candidate.Combined;
                    candidate.Verified = true;
                }
            }
            RotationCandidate zero = baseline;
            RotationCandidate bestNcc = verify.OrderByDescending(c => c.Combined).First();
            RotationCandidate bestEdge = verify.OrderByDescending(c => c.CombinedEdge).First();
            RotationCandidate best = verify.OrderByDescending(c => c.CombinedHybrid).First();
            selectedScore = best.AnchorScore;
            if (Math.Abs(best.Angle) < 0.000001 ||
                best.CombinedHybrid < zero.CombinedHybrid + RotationMinimumGain)
            {
                best = zero;
                selectedScore = zeroScore;
                status = "zero_retained_no_multifont_gain";
            }
            else
            {
                using (Mat rotated = RotateGolden(goldenBinary, best.Angle, center))
                using (Mat dstA = new Mat(goldenBinary,
                    new Rectangle(0, 0, ReferenceX[GroupSplitIndex], goldenBinary.Height)))
                using (Mat srcA = new Mat(rotated,
                    new Rectangle(0, 0, ReferenceX[GroupSplitIndex], rotated.Height)))
                {
                    srcA.CopyTo(dstA);
                }
                chosenAngle = best.Angle;
                status = "rotated_golden_A_edge_chamfer_v22";
            }
            // This file is deliberately separate from V19's compatibility log.
            if (ShouldSaveDebug(saveOptions))
            {
                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "FontV15_Debug", sideName);
                    Directory.CreateDirectory(dir);
                    File.WriteAllLines(Path.Combine(dir, "border_mask_v25.txt"), new string[] {
                        "mode=alignment_only_top_bottom_excluded",
                        "enabled=" + ExcludeTopBottomBordersForAlignment,
                        "border_band_pixels=" + GetAlignmentBorderBand(goldenBinary.Height),
                        "template_height=" + goldenBinary.Height,
                        "ncc_and_edge_scoring=center_vertical_region",
                        "inspection_defect_block=unchanged",
                        "note=This is a test band mask, not a verified physical border segmentation."
                    });
                    using (StreamWriter writer = new StreamWriter(
                        Path.Combine(dir, "rotation_v22_candidates.csv"), false))
                    {
                        writer.WriteLine("angle_deg,score8_coarse,score3,score6,score8_verified,combined,verified,edge3,edge6,edge8,combined_edge,combined_hybrid");
                        foreach (RotationCandidate candidate in candidates.OrderBy(c => c.Angle))
                        {
                            writer.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0:F3},{1:F6},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                                candidate.Angle, candidate.AnchorScore,
                                candidate.Verified ? candidate.Score3.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.Score6.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.Score8.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.Combined.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? "true" : "false",
                                candidate.Verified ? candidate.Edge3.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.Edge6.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.Edge8.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.CombinedEdge.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : "",
                                candidate.Verified ? candidate.CombinedHybrid.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) : ""));
                        }
                    }
                    File.WriteAllLines(Path.Combine(dir, "rotation_v22_selection.txt"), new string[] {
                        "ncc_angle=" + bestNcc.Angle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                        "edge_angle=" + bestEdge.Angle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                        "hybrid_angle_before_gain_gate=" + verify.OrderByDescending(c => c.CombinedHybrid).First().Angle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                        "applied_angle=" + chosenAngle.ToString("F3", System.Globalization.CultureInfo.InvariantCulture),
                        "status=" + status,
                        "edge_weight=" + EdgeWeight.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                        "note=Only shortlisted angles are compared using edges. NCC still locates XY."
                    });
                }
                catch (Exception ex) { Debug.WriteLine("[FontV22][Log] " + ex); }
            }
        }


        // V23 diagnostic: one rigid translation per angle, shared across 3pt-8pt.
        // Never changes actual Rotation/NCC/Defect/Block decisions.
        private static void SaveRigidRotationDiagnostic(Mat originalGolden, Mat measured, string sideName)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "FontV15_Debug", sideName, "rigid_v23");
            Directory.CreateDirectory(dir);
            int anchor = RotationAnchorIndex;
            int ax1 = ReferenceX[anchor];
            int ax2 = ReferenceX[anchor + 1];
            PointF rotationCenter = new PointF((ax1 + ax2) * 0.5f,
                originalGolden.Height * 0.5f);
            var report = new List<string>();
            report.Add("angle_deg,shared_dx,shared_dy,anchor_ncc,edge_3pt,edge_6pt,edge_8pt,weighted_edge,remarks");
            foreach (double angle in new double[] { -1.0, 0.0, 0.75, 1.0 })
            {
                string tag = angle < 0 ? "minus" + Math.Abs(angle).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    : "plus" + angle.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                try
                {
                    using (Mat golden = Math.Abs(angle) < 0.000001 ? originalGolden.Clone()
                        : RotateGolden(originalGolden, angle, rotationCenter))
                    using (Mat anchorTemplate = new Mat(golden,
                        new Rectangle(ax1, 0, ax2 - ax1, golden.Height)))
                    {
                        int left = Math.Max(0, ax1 - LocalAlignmentMaxShift);
                        int right = Math.Min(measured.Width, ax2 + LocalAlignmentMaxShift);
                        using (Mat window = new Mat(measured,
                            new Rectangle(left, 0, right - left, measured.Height)))
                        {
                            Point coarseLoc;
                            MatchTemplateBest(window, anchorTemplate, out coarseLoc);
                            int foundX = left + coarseLoc.X;
                            using (Mat selected = new Mat(measured,
                                new Rectangle(foundX, 0, anchorTemplate.Width, anchorTemplate.Height)))
                            using (FineAlignResult fine = FineAlignBinary(selected, anchorTemplate, RotationAGroupFineRadius))
                            {
                                int dx = foundX - ax1 + fine.ShiftX;
                                int dy = fine.ShiftY;
                                using (Mat aligned = new Mat())
                                using (Matrix<double> affine = new Matrix<double>(2, 3))
                                {
                                    affine[0, 0] = 1; affine[0, 1] = 0; affine[0, 2] = -dx;
                                    affine[1, 0] = 0; affine[1, 1] = 1; affine[1, 2] = -dy;
                                    CvInvoke.WarpAffine(measured, aligned, affine, measured.Size,
                                        Inter.Nearest, Warp.Default, BorderType.Constant, new MCvScalar(0));
                                    double[] edge = new double[3];
                                    int[] indices = new int[] { 0, 3, 5 };
                                    for (int j = 0; j < indices.Length; j++)
                                    {
                                        int i = indices[j];
                                        int x1 = ReferenceX[i], width = ReferenceX[i + 1] - x1;
                                        using (Mat gm = new Mat(golden, new Rectangle(x1, 0, width, golden.Height)))
                                        using (Mat mm = new Mat(aligned, new Rectangle(x1, 0, width, aligned.Height)))
                                        {
                                            edge[j] = EdgeChamferSimilarity(mm, gm);
                                            SaveOverlayPair(mm, gm,
                                                Path.Combine(dir, tag + "_" + FontNames[i] + "_overlay.png"),
                                                Path.Combine(dir, tag + "_" + FontNames[i] + "_edge.png"));
                                        }
                                    }
                                    double weighted = 0.15 * edge[0] + 0.40 * edge[1] + 0.45 * edge[2];
                                    report.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                        "{0:F2},{1},{2},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},shared_8pt_translation",
                                        angle, dx, dy, fine.Score, edge[0], edge[1], edge[2], weighted));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    report.Add(angle.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        + ",,,,,,,,error_" + ex.GetType().Name);
                    Debug.WriteLine("[FontV23][" + tag + "] " + ex);
                }
            }
            File.WriteAllLines(Path.Combine(dir, "rigid_rotation_comparison.csv"), report);
            File.WriteAllText(Path.Combine(dir, "README.txt"),
                "V23 DIAGNOSTIC ONLY: This folder does not affect inspection decisions.\r\n" +
                "For each GOLDEN rotation (-1,0,+0.75,+1 deg), 8pt determines ONE dx/dy.\r\n" +
                "Exactly the same dx/dy is applied to 3pt, 6pt, 8pt; no independent shifts.\r\n" +
                "Higher weighted_edge suggests closer contours but is not ground truth.\r\n" +
                "Red=measured, Green=golden. Boundary/cropping may reduce scores.\r\n");
        }

        private static double PredictRotationShiftY(double anchorShiftY, double angleDegrees, int index)
        {
            double anchorX = (ReferenceX[RotationAnchorIndex] + ReferenceX[RotationAnchorIndex + 1]) * 0.5;
            double segmentX = (ReferenceX[index] + ReferenceX[index + 1]) * 0.5;
            // RotateGolden uses the same sign convention in its affine matrix.
            return anchorShiftY + (segmentX - anchorX) * Math.Sin(angleDegrees * Math.PI / 180.0);
        }

        private static double ScoreRotationSegment(Mat measuredBinary, Mat rotatedGolden,
            int segmentIndex, double? expectedY, out double actualY, out double edgeScore)
        {
            actualY = 0.0;
            edgeScore = 0.0;
            int x1 = ReferenceX[segmentIndex];
            int x2 = ReferenceX[segmentIndex + 1];
            int left = Math.Max(0, x1 - LocalAlignmentMaxShift);
            int right = Math.Min(measuredBinary.Width, x2 + LocalAlignmentMaxShift);
            if (right - left < x2 - x1) return -1.0;
            using (Mat template = new Mat(rotatedGolden,
                new Rectangle(x1, 0, x2 - x1, rotatedGolden.Height)))
            using (Mat window = new Mat(measuredBinary,
                new Rectangle(left, 0, right - left, measuredBinary.Height)))
            {
                Point loc;
                MatchTemplateBest(window, template, out loc);
                int found = left + loc.X;
                int effectiveRadius = expectedY.HasValue
                    ? Math.Max(RotationVerifyFineRadius,
                        (int)Math.Ceiling(Math.Abs(expectedY.Value)) + RotationResidualYRadius)
                    : RotationVerifyFineRadius;
                using (Mat selected = new Mat(measuredBinary,
                    new Rectangle(found, 0, template.Width, template.Height)))
                using (FineAlignResult fine = FineAlignBinary(selected,
                    template, effectiveRadius,
                    expectedY, expectedY.HasValue ? (int?)RotationResidualYRadius : null))
                {
                    actualY = fine.ShiftY;
                    edgeScore = EdgeChamferSimilarity(fine.Aligned, template);
                    return fine.Score;
                }
            }
        }

        // V22: bounded, symmetric chamfer on Canny edge maps. The cap avoids
        // giving scratches/noise unbounded influence. Scores are normalized 0..1.
        // Evaluated only for shortlisted angles, never for each coarse angle.
        private static double EdgeChamferSimilarity(Mat aligned, Mat golden)
        {
            using (Mat ea = new Mat())
            using (Mat eg = new Mat())
            {
                // Exclude the same top/bottom bands from edge scoring.
                int band = GetAlignmentBorderBand(golden.Height);
                if (band > 0 && aligned.Height == golden.Height && aligned.Width == golden.Width)
                {
                    using (Mat alignedCenter = new Mat(aligned,
                        new Rectangle(0, band, aligned.Width, aligned.Height - 2 * band)))
                    using (Mat goldenCenter = new Mat(golden,
                        new Rectangle(0, band, golden.Width, golden.Height - 2 * band)))
                    {
                        CvInvoke.Canny(alignedCenter, ea, 60.0, 140.0);
                        CvInvoke.Canny(goldenCenter, eg, 60.0, 140.0);
                    }
                }
                else
                {
                    CvInvoke.Canny(aligned, ea, 60.0, 140.0);
                    CvInvoke.Canny(golden, eg, 60.0, 140.0);
                }
                using (Image<Gray, byte> a = ea.ToImage<Gray, byte>())
                using (Image<Gray, byte> g = eg.ToImage<Gray, byte>())
                {
                    double forward = EdgeChamferOneWay(g.Data, a.Data);
                    double reverse = EdgeChamferOneWay(a.Data, g.Data);
                    return Math.Max(0.0, 1.0 - (forward + reverse) / (2.0 * EdgeChamferRadius));
                }
            }
        }

        private static double EdgeChamferOneWay(byte[,,] source, byte[,,] target)
        {
            int h = Math.Min(source.GetLength(0), target.GetLength(0));
            int w = Math.Min(source.GetLength(1), target.GetLength(1));
            int r = EdgeChamferRadius;
            double sum = 0.0;
            int count = 0;
            // Avoid the warped black border and sample sparsely for speed.
            for (int y = r; y < h - r; y += EdgeSampleStride)
                for (int x = r; x < w - r; x += EdgeSampleStride)
                {
                    if (source[y, x, 0] == 0) continue;
                    double best2 = (double)r * r;
                    for (int dy = -r; dy <= r; dy++)
                        for (int dx = -r; dx <= r; dx++)
                        {
                            int d2 = dx * dx + dy * dy;
                            if (d2 >= best2) continue;
                            if (target[y + dy, x + dx, 0] != 0)
                                best2 = d2;
                        }
                    sum += Math.Sqrt(best2);
                    count++;
                }
            return count < 20 ? r : sum / count;
        }

        private static Mat RotateGolden(Mat src, double degrees, PointF center)
        {
            // Standard 2x3 affine rotation; fixed output bounds and zero background.
            double a = degrees * Math.PI / 180.0;
            double c = Math.Cos(a), si = Math.Sin(a);
            Mat dst = new Mat();
            using (Matrix<double> m = new Matrix<double>(2, 3))
            {
                m[0, 0] = c;
                m[0, 1] = si;
                m[0, 2] = (1 - c) * center.X - si * center.Y;
                m[1, 0] = -si;
                m[1, 1] = c;
                m[1, 2] = si * center.X + (1 - c) * center.Y;
                CvInvoke.WarpAffine(src, dst, m, src.Size,
                    Inter.Nearest, Warp.Default, BorderType.Constant,
                    new MCvScalar(0));
            }
            return dst;
        }

        private static double ScoreEightPtRotation(Mat measuredBinary, Mat template8)
        {
            int x1 = ReferenceX[RotationAnchorIndex];
            int x2 = ReferenceX[RotationAnchorIndex + 1];
            int searchLeft = Math.Max(0, x1 - LocalAlignmentMaxShift);
            int searchRight = Math.Min(measuredBinary.Width,
                x2 + LocalAlignmentMaxShift);
            using (Mat window = new Mat(measuredBinary,
                new Rectangle(searchLeft, 0, searchRight - searchLeft,
                    measuredBinary.Height)))
            {
                Point loc;
                double coarse = MatchTemplateBest(window, template8, out loc);
                int foundX = searchLeft + loc.X;
                using (Mat selected = new Mat(measuredBinary,
                    new Rectangle(foundX, 0, template8.Width, template8.Height)))
                using (FineAlignResult fine = FineAlignBinary(selected,
                    template8, FineAlignmentMaxShift))
                {
                    // Use final score, not the coarse-only score.
                    return fine.Score;
                }
            }
        }

        private static SegmentInspection InspectSegment(
            Mat originalArea,
            Mat grayComparison,
            Mat measuredBinary,
            Mat fullTemplateBinary,
            OCT_Parameters_CardType currentParams,
            OCT_Parameters_SaveOptions saveOptions,
            int globalStartX,
            int index,
            string sideName,
            bool isYin,
            int? restrictedCoarseShiftX = null,
            int? restrictedRadiusX = null,
            int? fineRadius = null,
            double? predictedFineY = null,
            int? allowedResidualY = null)
        {
            int expectedX1 = ReferenceX[index];
            int expectedX2 = ReferenceX[index + 1];
            int segmentWidth = expectedX2 - expectedX1;

            Mat templateSegment = new Mat(
                fullTemplateBinary,
                new Rectangle(
                    expectedX1,
                    0,
                    segmentWidth,
                    fullTemplateBinary.Height)
            ).Clone();

            int localRadius = restrictedRadiusX ?? GroupRescanRadiusX;
            int effectiveFineRadius = fineRadius ?? FineAlignmentMaxShift;
            if (predictedFineY.HasValue)
                effectiveFineRadius = Math.Max(effectiveFineRadius,
                    (int)Math.Ceiling(Math.Abs(predictedFineY.Value)) + RotationResidualYRadius);
            int searchMin = restrictedCoarseShiftX.HasValue
                ? restrictedCoarseShiftX.Value - localRadius
                : -LocalAlignmentMaxShift;
            int searchMax = restrictedCoarseShiftX.HasValue
                ? restrictedCoarseShiftX.Value + localRadius
                : LocalAlignmentMaxShift;
            int windowX1 = Math.Max(0, expectedX1 + searchMin);
            int windowX2 = Math.Min(measuredBinary.Width, expectedX2 + searchMax);
            int windowWidth = windowX2 - windowX1;

            if (windowWidth < segmentWidth)
            {
                templateSegment.Dispose();
                throw new InvalidOperationException(
                    $"{sideName} {FontNames[index]} 局部搜尋窗口不足：" +
                    $"window={windowWidth}, template={segmentWidth}");
            }

            Mat window = null;
            Mat alignmentSelected = null;
            Mat engineeringSelected = null;
            Mat engineeringNominal = null;
            Mat engineeringAligned = null;
            Mat original = null;
            Mat block = null;
            Mat defect = null;
            FineAlignResult fine = null;

            try
            {
                // A. 對位階段：仍然全部固定 150，不讓工程參數改變 NCC 位置。
                window = new Mat(
                    measuredBinary,
                    new Rectangle(
                        windowX1,
                        0,
                        windowWidth,
                        measuredBinary.Height)
                ).Clone();

                Point localLoc;
                double localScore = MatchTemplateBest(
                    window,
                    templateSegment,
                    out localLoc);

                int foundX1 = windowX1 + localLoc.X;
                int foundX2 = foundX1 + segmentWidth;

                foundX1 = Math.Max(
                    0,
                    Math.Min(
                        foundX1,
                        measuredBinary.Width - segmentWidth));
                foundX2 = foundX1 + segmentWidth;

                bool hitBoundary =
                    localLoc.X == 0 ||
                    localLoc.X == (windowWidth - segmentWidth);

                alignmentSelected = new Mat(
                    measuredBinary,
                    new Rectangle(
                        foundX1,
                        0,
                        segmentWidth,
                        measuredBinary.Height)
                ).Clone();

                fine = FineAlignBinary(
                    alignmentSelected,
                    templateSegment,
                    effectiveFineRadius,
                    predictedFineY, allowedResidualY);

                // B. 最終缺燙/塞版計算階段：
                //    用工程介面各字級自己的 Threshold，但沿用 A 階段找到的位置與 shift。
                double engineeringThreshold =
                    GetEngineeringThreshold(
                        currentParams,
                        index,
                        isYin,
                        saveOptions);

                using (Mat selectedGray = new Mat(
                    grayComparison,
                    new Rectangle(
                        foundX1,
                        0,
                        segmentWidth,
                        grayComparison.Height)))
                {
                    engineeringSelected =
                        FontBinaryWithThreshold(
                            selectedGray,
                            isYin,
                            engineeringThreshold);
                }

                using (Mat nominalGray = new Mat(
                    grayComparison,
                    new Rectangle(
                        expectedX1,
                        0,
                        segmentWidth,
                        grayComparison.Height)))
                {
                    engineeringNominal =
                        FontBinaryWithThreshold(
                            nominalGray,
                            isYin,
                            engineeringThreshold);
                }

                engineeringAligned =
                    AlignBinaryWithKnownShift(
                        engineeringSelected,
                        templateSegment.Size,
                        effectiveFineRadius,
                        fine.ShiftX,
                        fine.ShiftY);

                defect = BuildDefectImage(
                    engineeringAligned,
                    templateSegment);
                block = BuildBlockImage(
                    engineeringAligned,
                    templateSegment);

                int defectPixels = CountWhitePixels(defect);
                int blockPixels = CountWhitePixels(block);
                int templateForeground =
                    CountWhitePixels(templateSegment);
                int templateBackground =
                    templateSegment.Width *
                    templateSegment.Height -
                    templateForeground;

                int sourceX =
                    globalStartX + foundX1;
                sourceX = Math.Max(
                    0,
                    Math.Min(
                        sourceX,
                        originalArea.Width - segmentWidth));

                using (Mat originalRoi = new Mat(
                    originalArea,
                    new Rectangle(
                        sourceX,
                        0,
                        segmentWidth,
                        originalArea.Height)))
                {
                    original = EnsureBgr(originalRoi);
                }

                SegmentInspection result =
                    new SegmentInspection
                    {
                        FontName = FontNames[index],
                        Index = index,
                        ExpectedX1 = expectedX1,
                        ExpectedX2 = expectedX2,
                        FoundX1 = foundX1,
                        CoarseScore = localScore,
                        FineScore = fine.Score,
                        ShiftX =
                            (foundX1 - expectedX1) +
                            fine.ShiftX,
                        ShiftY = fine.ShiftY,
                        HitBoundary = hitBoundary,
                        EngineeringThreshold =
                            engineeringThreshold,
                        Original = original,
                        Nominal = engineeringNominal,
                        Selected = engineeringSelected,
                        Template = templateSegment,
                        Aligned = engineeringAligned,
                        Defect = defect,
                        Block = block,
                        DefectPixels = defectPixels,
                        BlockPixels = blockPixels,
                        TemplateForegroundPixels =
                            templateForeground,
                        TemplateBackgroundPixels =
                            templateBackground
                    };

                // 物件所有權交給 SegmentInspection。
                original = null;
                engineeringNominal = null;
                engineeringSelected = null;
                templateSegment = null;
                engineeringAligned = null;
                defect = null;
                block = null;

                return result;
            }
            catch
            {
                original?.Dispose();
                engineeringNominal?.Dispose();
                engineeringSelected?.Dispose();
                templateSegment?.Dispose();
                engineeringAligned?.Dispose();
                defect?.Dispose();
                block?.Dispose();
                throw;
            }
            finally
            {
                window?.Dispose();
                alignmentSelected?.Dispose();
                fine?.Dispose();
            }
        }

        private static Mat BuildFullStripTemplate(
            string sideName,
            string cardType,
            int expectedHeight,
            List<TemplateData> legacyTemplates,
            bool isYin,
            out bool compatibilityTemplateUsed)
        {
            Mat full = new Mat(
                new Size(ReferenceWidth, expectedHeight),
                DepthType.Cv8U,
                1);
            full.SetTo(new MCvScalar(isYin ? 0 : 255));

            compatibilityTemplateUsed = false;

            for (int i = 0; i < FontNames.Length; i++)
            {
                int x1 = ReferenceX[i];
                int x2 = ReferenceX[i + 1];
                int segmentWidth = x2 - x1;

                Mat highResolutionGray = TryLoadHighResolutionTemplate(
                    sideName,
                    FontNames[i],
                    cardType,
                    expectedHeight,
                    segmentWidth);

                if (highResolutionGray != null)
                {
                    using (Mat dst = new Mat(
                        full,
                        new Rectangle(x1, 0, segmentWidth, expectedHeight)))
                    {
                        highResolutionGray.CopyTo(dst);
                    }
                    highResolutionGray.Dispose();
                    continue;
                }

                compatibilityTemplateUsed = true;
                Mat compatibility = BuildCompatibilityGrayTemplate(
                    legacyTemplates[i].Image,
                    segmentWidth,
                    expectedHeight,
                    isYin);

                using (Mat dst = new Mat(
                    full,
                    new Rectangle(x1, 0, segmentWidth, expectedHeight)))
                {
                    compatibility.CopyTo(dst);
                }
                compatibility.Dispose();
            }

            return full;
        }

        private static string GetProjectDataRoot()
        {
            string baseDir =
                AppDomain.CurrentDomain.BaseDirectory;

            DirectoryInfo current =
                new DirectoryInfo(baseDir);

            while (current != null)
            {
                if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "UNIVACCO_UI.csproj")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return baseDir;
        }

        private static Mat TryLoadHighResolutionTemplate(
            string sideName,
            string fontName,
            string cardType,
            int expectedHeight,
            int expectedWidth)
        {
            string baseDir =
                AppDomain.CurrentDomain.BaseDirectory;
            string projectDataRoot =
                GetProjectDataRoot();

            string filename =
                sideName + "_template_" +
                fontName + ".bmp";

            string safeCardType =
                string.IsNullOrWhiteSpace(cardType)
                    ? "白卡"
                    : cardType.Trim();

            string[] candidates =
            {
                // 正式 Git 金樣本位置
                Path.Combine(
                    projectDataRoot,
                    "font_v15_templates",
                    safeCardType,
                    filename),
                Path.Combine(
                    projectDataRoot,
                    "font_v15_templates",
                    filename),
                Path.Combine(
                    projectDataRoot,
                    "baseline_test",
                    "templates",
                    filename),

                // EXE 部署 / 舊版相容位置
                Path.Combine(
                    baseDir,
                    "font_v15_templates",
                    safeCardType,
                    filename),
                Path.Combine(
                    baseDir,
                    "font_v15_templates",
                    filename),
                Path.Combine(
                    baseDir,
                    "baseline_test",
                    "templates",
                    filename)
            };

            foreach (string path in candidates)
            {
                if (!File.Exists(path))
                    continue;

                Mat tpl = CvInvoke.Imread(path, ImreadModes.Grayscale);
                if (tpl == null || tpl.IsEmpty)
                {
                    tpl?.Dispose();
                    continue;
                }

                if (tpl.Width == expectedWidth && tpl.Height == expectedHeight)
                {
                    Debug.WriteLine($"[FontV15][Template] 使用高解析模板：{path}");
                    return tpl;
                }

                Debug.WriteLine(
                    $"[FontV15][Template] 尺寸不符，略過：{path}, " +
                    $"actual={tpl.Width}x{tpl.Height}, expected={expectedWidth}x{expectedHeight}");
                tpl.Dispose();
            }

            return null;
        }

        /// <summary>
        /// 以目前舊 TemplateData 建立 v15 相容金樣本：
        /// 保持長寬比、不拉伸字體，置中補邊到固定字級區段。
        /// </summary>
        private static Mat BuildCompatibilityGrayTemplate(
            Mat legacyTemplate,
            int targetWidth,
            int targetHeight,
            bool isYin)
        {
            if (legacyTemplate == null || legacyTemplate.IsEmpty)
                throw new ArgumentException("Legacy Template 為空");

            Mat source = ToGray(legacyTemplate);
            Mat resized = null;
            Mat canvas = null;

            try
            {
                // 舊 Yang Template 是一般 Binary（白底/黑字），v15 的 FixedFontBinary
                // 會再做 BinaryInv，因此這裡維持舊灰階極性，不先反相。
                double scale = Math.Min(
                    targetWidth / (double)source.Width,
                    targetHeight / (double)source.Height);

                int newWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                int newHeight = Math.Max(1, (int)Math.Round(source.Height * scale));

                resized = new Mat();
                CvInvoke.Resize(
                    source,
                    resized,
                    new Size(newWidth, newHeight),
                    0,
                    0,
                    Inter.Nearest);

                byte borderValue = isYin ? (byte)0 : (byte)255;
                canvas = new Mat(
                    new Size(targetWidth, targetHeight),
                    DepthType.Cv8U,
                    1);
                canvas.SetTo(new MCvScalar(borderValue));

                int dx = Math.Max(0, (targetWidth - newWidth) / 2);
                int dy = Math.Max(0, (targetHeight - newHeight) / 2);
                int roiWidth = Math.Min(newWidth, targetWidth - dx);
                int roiHeight = Math.Min(newHeight, targetHeight - dy);

                using (Mat dst = new Mat(
                    canvas,
                    new Rectangle(dx, dy, roiWidth, roiHeight)))
                using (Mat srcRoi = new Mat(
                    resized,
                    new Rectangle(0, 0, roiWidth, roiHeight)))
                {
                    srcRoi.CopyTo(dst);
                }

                Mat ret = canvas;
                canvas = null;
                return ret;
            }
            finally
            {
                source.Dispose();
                resized?.Dispose();
                canvas?.Dispose();
            }
        }

        private static Mat FixedFontBinary(
            Mat grayInput,
            bool isYin)
        {
            return FontBinaryWithThreshold(
                grayInput,
                isYin,
                isYin
                    ? YinFixedThreshold
                    : YangFixedThreshold);
        }

        /// <summary>
        /// 使用指定灰階門檻二值化。
        /// Yin / Yang 最後都統一成「字體/前景 = 255、背景 = 0」，
        /// 因此 Yang 在 Binary 後會反相。
        /// </summary>
        private static Mat FontBinaryWithThreshold(
            Mat grayInput,
            bool isYin,
            double threshold)
        {
            Mat gray = ToGray(grayInput);
            Mat binary = new Mat();

            try
            {
                threshold = Math.Max(
                    0.0,
                    Math.Min(255.0, threshold));

                CvInvoke.Threshold(
                    gray,
                    binary,
                    threshold,
                    255,
                    ThresholdType.Binary);

                if (!isYin)
                {
                    Mat inverted = new Mat();
                    CvInvoke.BitwiseNot(
                        binary,
                        inverted);
                    binary.Dispose();
                    binary = inverted;
                }

                Mat ret = binary;
                binary = null;
                return ret;
            }
            finally
            {
                gray.Dispose();
                binary?.Dispose();
            }
        }

        private static void RememberEngineeringParameters(
            OCT_Parameters_CardType currentParams)
        {
            if (currentParams == null)
                return;

            lock (EngineeringParameterLock)
            {
                _lastEngineeringParameters =
                    currentParams;
            }
        }

        private static OCT_Parameters_CardType
            GetRememberedEngineeringParameters()
        {
            lock (EngineeringParameterLock)
            {
                return _lastEngineeringParameters;
            }
        }

        /// <summary>
        /// 取得工程介面 3pt~12pt 的 Threshold。
        /// OCT_Parameters_CardType 內的值已經由百分比換算成 0~255。
        ///
        /// MakeTemplates 時維持 v15 固定 Threshold，
        /// 避免建立 Golden Sample 時受現場工程參數影響。
        /// </summary>
        private static double GetEngineeringThreshold(
            OCT_Parameters_CardType currentParams,
            int index,
            bool isYin,
            OCT_Parameters_SaveOptions saveOptions)
        {
            double fallback =
                isYin
                    ? YinFixedThreshold
                    : YangFixedThreshold;

            if (saveOptions != null &&
                saveOptions.saveoption.MakeTemplates)
            {
                return fallback;
            }

            if (currentParams == null)
            {
                Debug.WriteLine(
                    "[FontV15][Threshold] currentParams=null，" +
                    "暫時使用固定門檻 " +
                    fallback.ToString("F2"));
                return fallback;
            }

            Font_Parameter p =
                isYin
                    ? currentParams.yin_parameter
                    : currentParams.yang_parameter;

            if (p == null)
                return fallback;

            double value;

            switch (index)
            {
                case 0: value = p._3pt; break;
                case 1: value = p._4pt; break;
                case 2: value = p._5pt; break;
                case 3: value = p._6pt; break;
                case 4: value = p._7pt; break;
                case 5: value = p._8pt; break;
                case 6: value = p._9pt; break;
                case 7: value = p._10pt; break;
                case 8: value = p._11pt; break;
                case 9: value = p._12pt; break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(index));
            }

            return Math.Max(
                0.0,
                Math.Min(255.0, value));
        }

        private static double ThresholdToPercent(
            double threshold)
        {
            return threshold / 255.0 * 100.0;
        }

        /// <summary>
        /// 不重新搜尋位置，只把工程參數二值化後的影像套用
        /// v15 固定門檻 NCC 已找到的 fine shift。
        /// </summary>
        private static Mat AlignBinaryWithKnownShift(
            Mat measured,
            Size targetSize,
            int maxShift,
            int shiftX,
            int shiftY)
        {
            Mat padded = new Mat(
                new Size(
                    measured.Width + maxShift * 2,
                    measured.Height + maxShift * 2),
                measured.Depth,
                measured.NumberOfChannels);

            padded.SetTo(new MCvScalar(0));

            try
            {
                using (Mat center = new Mat(
                    padded,
                    new Rectangle(
                        maxShift,
                        maxShift,
                        measured.Width,
                        measured.Height)))
                {
                    measured.CopyTo(center);
                }

                int x = maxShift + shiftX;
                int y = maxShift + shiftY;

                x = Math.Max(
                    0,
                    Math.Min(
                        x,
                        padded.Width - targetSize.Width));
                y = Math.Max(
                    0,
                    Math.Min(
                        y,
                        padded.Height - targetSize.Height));

                return new Mat(
                    padded,
                    new Rectangle(
                        x,
                        y,
                        targetSize.Width,
                        targetSize.Height)
                ).Clone();
            }
            finally
            {
                padded.Dispose();
            }
        }

        private static Mat AlignMaskWithFineShift(
            Mat validMask,
            Size targetSize,
            int maxShift,
            int shiftX,
            int shiftY)
        {
            Mat padded = new Mat(
                new Size(
                    validMask.Width + maxShift * 2,
                    validMask.Height + maxShift * 2),
                DepthType.Cv8U,
                1);

            padded.SetTo(new MCvScalar(0));

            try
            {
                using (Mat center = new Mat(
                    padded,
                    new Rectangle(
                        maxShift,
                        maxShift,
                        validMask.Width,
                        validMask.Height)))
                {
                    validMask.CopyTo(center);
                }

                int x = maxShift + shiftX;
                int y = maxShift + shiftY;

                x = Math.Max(
                    0,
                    Math.Min(
                        x,
                        padded.Width - targetSize.Width));
                y = Math.Max(
                    0,
                    Math.Min(
                        y,
                        padded.Height - targetSize.Height));

                return new Mat(
                    padded,
                    new Rectangle(
                        x,
                        y,
                        targetSize.Width,
                        targetSize.Height)
                ).Clone();
            }
            finally
            {
                padded.Dispose();
            }
        }

        private static int FindStripStart(
            Mat areaBinary,
            Mat templateBinary,
            out double bestScore)
        {
            if (areaBinary.Height != templateBinary.Height ||
                areaBinary.Width < templateBinary.Width)
            {
                throw new ArgumentException(
                    $"粗對位尺寸不符：實測={areaBinary.Width}x{areaBinary.Height}, " +
                    $"模板={templateBinary.Width}x{templateBinary.Height}");
            }

            Mat smallArea = new Mat();
            Mat smallTemplate = new Mat();
            try
            {
                int areaW = Math.Max(1, (int)Math.Round(areaBinary.Width * CoarseScale));
                int areaH = Math.Max(1, (int)Math.Round(areaBinary.Height * CoarseScale));
                int tplW = Math.Max(1, (int)Math.Round(templateBinary.Width * CoarseScale));
                int tplH = Math.Max(1, (int)Math.Round(templateBinary.Height * CoarseScale));

                CvInvoke.Resize(
                    areaBinary,
                    smallArea,
                    new Size(areaW, areaH),
                    0,
                    0,
                    Inter.Nearest);

                CvInvoke.Resize(
                    templateBinary,
                    smallTemplate,
                    new Size(tplW, tplH),
                    0,
                    0,
                    Inter.Nearest);

                Point maxLoc;
                bestScore = MatchTemplateBest(smallArea, smallTemplate, out maxLoc);

                int start = (int)Math.Round(maxLoc.X / CoarseScale);
                int maxStart = areaBinary.Width - templateBinary.Width;
                start = Math.Max(0, Math.Min(start, maxStart));
                // The 0.5x NCC result can be inaccurate by several pixels. Compare
                // full-resolution X foreground profiles near the coarse estimate.
                // This changes ONLY the global crop start, not Defect/Block formulas.
                try
                {
                    int refinedStart;
                    double refinedScore;
                    if (TryRefineStripStartByProjection(areaBinary, templateBinary,
                        start, out refinedStart, out refinedScore))
                    {
                        Debug.WriteLine("[FontV24][StripRefine] coarse=" + start +
                            ", refined=" + refinedStart + ", profileNcc=" +
                            refinedScore.ToString("F6"));
                        start = refinedStart;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[FontV24][StripRefine] fallback to coarse: " + ex.Message);
                }
                return start;
            }
            finally
            {
                smallArea.Dispose();
                smallTemplate.Dispose();
            }
        }

        // Uses only an X-axis foreground projection for fast, full-resolution
        // translation refinement. No scale or rotation assumptions are added.
        // Ambiguous profiles fall back to the existing NCC coarse start.
        private static bool TryRefineStripStartByProjection(
            Mat measured, Mat golden, int initialStart,
            out int refinedStart, out double refinedScore)
        {
            refinedStart = initialStart;
            refinedScore = 0.0;
            int maxStart = measured.Width - golden.Width;
            if (maxStart <= 0 || measured.Height != golden.Height)
                return false;

            int lo = Math.Max(0, initialStart - CoarseRefineRadius);
            int hi = Math.Min(maxStart, initialStart + CoarseRefineRadius);
            if (lo >= hi) return false;

            using (Image<Gray, byte> a = measured.ToImage<Gray, byte>())
            using (Image<Gray, byte> t = golden.ToImage<Gray, byte>())
            {
                byte[,,] ad = a.Data;
                byte[,,] td = t.Data;
                int h = golden.Height;
                int aw = measured.Width;
                int tw = golden.Width;
                double[] ap = new double[aw];
                double[] tp = new double[tw];
                // Scan every fourth row, avoiding a costly full-res 2D NCC.
                for (int y = 0; y < h; y += 4)
                {
                    for (int x = lo; x < Math.Min(aw, hi + tw); x++)
                        if (ad[y, x, 0] > 0) ap[x]++;
                    for (int x = 0; x < tw; x++)
                        if (td[y, x, 0] > 0) tp[x]++;
                }
                double tMean = tp.Average();
                double tVar = 0.0;
                for (int x = 0; x < tw; x++)
                {
                    tp[x] -= tMean;
                    tVar += tp[x] * tp[x];
                }
                if (tVar < 1.0) return false;
                double best = double.NegativeInfinity;
                int bestX = initialStart;
                for (int shift = lo; shift <= hi; shift++)
                {
                    double mean = 0;
                    for (int x = 0; x < tw; x++) mean += ap[x + shift];
                    mean /= tw;
                    double sum = 0, avar = 0;
                    for (int x = 0; x < tw; x++)
                    {
                        double value = ap[x + shift] - mean;
                        sum += value * tp[x];
                        avar += value * value;
                    }
                    if (avar < 1.0) continue;
                    double score = sum / Math.Sqrt(avar * tVar);
                    if (score > best)
                    {
                        best = score;
                        bestX = shift;
                    }
                }
                if (double.IsNaN(best) || double.IsInfinity(best) || best < 0.10)
                    return false;
                refinedStart = bestX;
                refinedScore = best;
                return true;
            }
        }

        private static double MatchTemplateBest(Mat image, Mat template, out Point maxLoc)
        {
            // Match only the central vertical region. Cropping the same rows
            // from both images preserves the X and Y match-location coordinates.
            // The full original images are still used to construct final output.
            int band = GetAlignmentBorderBand(template.Height);
            if (band > 0 && image.Height >= template.Height)
            {
                using (Mat centralImage = new Mat(image,
                    new Rectangle(0, band, image.Width, image.Height - 2 * band)))
                using (Mat centralTemplate = new Mat(template,
                    new Rectangle(0, band, template.Width, template.Height - 2 * band)))
                {
                    return MatchTemplateBestFull(centralImage, centralTemplate, out maxLoc);
                }
            }
            return MatchTemplateBestFull(image, template, out maxLoc);
        }

        private static int GetAlignmentBorderBand(int templateHeight)
        {
            if (!ExcludeTopBottomBordersForAlignment || templateHeight < 400)
                return 0;
            return Math.Min(AlignmentBorderBandPixels, templateHeight / 12);
        }

        private static double MatchTemplateBestFull(Mat image, Mat template, out Point maxLoc)
        {
            Mat imageF = new Mat();
            Mat templateF = new Mat();
            Mat result = new Mat();
            try
            {
                image.ConvertTo(imageF, DepthType.Cv32F);
                template.ConvertTo(templateF, DepthType.Cv32F);
                CvInvoke.MatchTemplate(imageF, templateF, result,
                    TemplateMatchingType.CcorrNormed);
                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = default(Point);
                maxLoc = default(Point);
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal,
                    ref minLoc, ref maxLoc);
                return maxVal;
            }
            finally
            {
                imageF.Dispose();
                templateF.Dispose();
                result.Dispose();
            }
        }

        private static FineAlignResult FineAlignBinary(
            Mat measured,
            Mat template,
            int maxShift,
            double? predictedY = null,
            int? residualRadiusY = null)
        {
            Mat padded = null;
            try
            {
                padded = new Mat(
                    new Size(
                        measured.Width + maxShift * 2,
                        measured.Height + maxShift * 2),
                    measured.Depth,
                    measured.NumberOfChannels);
                padded.SetTo(new MCvScalar(0));

                using (Mat center = new Mat(
                    padded,
                    new Rectangle(
                        maxShift,
                        maxShift,
                        measured.Width,
                        measured.Height)))
                {
                    measured.CopyTo(center);
                }

                Point maxLoc;
                double score;
                if (predictedY.HasValue && residualRadiusY.HasValue)
                {
                    int minY = Math.Max(0, (int)Math.Round(predictedY.Value) - residualRadiusY.Value + maxShift);
                    int maxY = Math.Min(2 * maxShift, (int)Math.Round(predictedY.Value) + residualRadiusY.Value + maxShift);
                    if (minY > maxY)
                        throw new InvalidOperationException("V21 predicted Y is outside the available fine-alignment window.");
                    using (Mat restricted = new Mat(padded, new Rectangle(
                        0, minY, padded.Width, template.Height + maxY - minY)))
                    {
                        Point local;
                        score = MatchTemplateBest(restricted, template, out local);
                        maxLoc = new Point(local.X, local.Y + minY);
                    }
                }
                else
                {
                    score = MatchTemplateBest(padded, template, out maxLoc);
                }

                Mat aligned = new Mat(
                    padded,
                    new Rectangle(
                        maxLoc.X,
                        maxLoc.Y,
                        template.Width,
                        template.Height)
                ).Clone();

                return new FineAlignResult
                {
                    Aligned = aligned,
                    Score = score,
                    ShiftX = maxLoc.X - maxShift,
                    ShiftY = maxLoc.Y - maxShift
                };
            }
            finally
            {
                padded?.Dispose();
            }
        }

        private static Mat BuildDefectImage(Mat measured, Mat template)
        {
            // Python: defect = template_fg & ~measured_fg
            Mat notMeasured = new Mat();
            Mat defect = new Mat();
            try
            {
                CvInvoke.BitwiseNot(measured, notMeasured);
                CvInvoke.BitwiseAnd(template, notMeasured, defect);
                Mat ret = defect;
                defect = null;
                return ret;
            }
            finally
            {
                notMeasured.Dispose();
                defect?.Dispose();
            }
        }

        private static Mat BuildBlockImage(Mat measured, Mat template)
        {
            // Python: block = measured_fg & ~template_fg
            Mat notTemplate = new Mat();
            Mat block = new Mat();
            try
            {
                CvInvoke.BitwiseNot(template, notTemplate);
                CvInvoke.BitwiseAnd(measured, notTemplate, block);
                Mat ret = block;
                block = null;
                return ret;
            }
            finally
            {
                notTemplate.Dispose();
                block?.Dispose();
            }
        }


        private static int CountWhitePixels(Mat binary)
        {
            if (binary == null || binary.IsEmpty)
                return 0;

            if (binary.NumberOfChannels != 1 || binary.Depth != DepthType.Cv8U)
                throw new ArgumentException("CountWhitePixels 只接受 8-bit 單通道影像");

            int count = 0;
            unsafe
            {
                byte* ptr = (byte*)binary.DataPointer;
                int step = binary.Step;
                for (int y = 0; y < binary.Height; y++)
                {
                    byte* row = ptr + y * step;
                    for (int x = 0; x < binary.Width; x++)
                    {
                        if (row[x] == 255)
                            count++;
                    }
                }
            }
            return count;
        }

        private static Mat ToGray(Mat input)
        {
            if (input.NumberOfChannels == 1)
                return input.Clone();

            Mat gray = new Mat();
            CvInvoke.CvtColor(input, gray, ColorConversion.Bgr2Gray);
            return gray;
        }

        private static Mat EnsureBgr(Mat input)
        {
            if (input.NumberOfChannels == 3)
                return input.Clone();

            Mat bgr = new Mat();
            CvInvoke.CvtColor(input, bgr, ColorConversion.Gray2Bgr);
            return bgr;
        }

        private static void SaveHighResolutionTemplates(
            Mat grayComparison,
            string sideName,
            string cardType)
        {
            string safeCardType = string.IsNullOrWhiteSpace(cardType) ? "白卡" : cardType.Trim();
            string dir = Path.Combine(
                GetProjectDataRoot(),
                "font_v15_templates",
                safeCardType);
            Directory.CreateDirectory(dir);

            for (int i = 0; i < FontNames.Length; i++)
            {
                int x1 = ReferenceX[i];
                int width = ReferenceX[i + 1] - x1;
                using (Mat roi = new Mat(
                    grayComparison,
                    new Rectangle(x1, 0, width, grayComparison.Height)))
                {
                    string path = Path.Combine(
                        dir,
                        sideName + "_template_" + FontNames[i] + ".bmp");
                    CvInvoke.Imwrite(path, roi);
                }
            }

            Debug.WriteLine(
                $"[FontV15][Template] 已建立高解析模板：{dir} ({sideName})");
        }

        private static void SaveNativeAlignmentDebug(
            List<NativeCharacterAlignmentPipeline.GroupResult> groups,
            string sideName)
        {
            if (groups == null || groups.Count == 0)
                return;

            string root = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FontAlignment_Debug",
                sideName);

            Directory.CreateDirectory(root);

            foreach (var group in groups.OrderBy(g => g.Group))
            {
                string dir = Path.Combine(root, group.Group);
                Directory.CreateDirectory(dir);

                using (Mat finalOverlay = CharacterBandMatcher.BuildFinalOverlay(group.Golden, group.Aligned))
                using (Mat skeletonOverlay = CharacterBandMatcher.BuildSkeletonOverlay(group.Golden, group.Aligned))
                {
                    CvInvoke.Imwrite(Path.Combine(dir, "03_FINAL_OVERLAY.png"), finalOverlay);
                    CvInvoke.Imwrite(Path.Combine(dir, "04_SK_OVERLAY_AFTER.png"), skeletonOverlay);
                }

                File.WriteAllLines(
                    Path.Combine(dir, "00_alignment_info.txt"),
                    new string[]
                    {
                        "side=" + group.Side,
                        "group=" + group.Group,
                        "strip_start_x=" + group.StripStartX,
                        "ecc_rotation_deg=" + group.EccRotation.ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
                        "matched_characters=" + group.Characters.Count
                    });
            }
        }

        private static bool ShouldSaveDebug(OCT_Parameters_SaveOptions saveOptions)
        {
            return saveOptions != null && saveOptions.saveoption.Roi_AreaCrop;
        }

        private static void SaveDebug(
            string sideName,
            Mat gray,
            Mat measuredBinaryFull,
            Mat grayComparison,
            Mat fullTemplate,
            Mat fullTemplateBinary,
            Mat measuredBinary,
            int startX,
            double coarseScore,
            bool compatibilityTemplateUsed,
            List<SegmentInspection> segments,
            Font_Results defectResults,
            Font_Results blockResults)
        {
            string sideRoot = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FontV15_Debug",
                sideName);

            Directory.CreateDirectory(sideRoot);

            // 清掉舊版逐步小圖，現場只保留最後一張合併 rigid_diff。
            string legacySegmentRoot =
                Path.Combine(sideRoot, "font_segments");

            if (Directory.Exists(legacySegmentRoot))
            {
                try
                {
                    Directory.Delete(
                        legacySegmentRoot,
                        true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "[FontV15][Debug] 無法清除舊 font_segments：" +
                        ex.Message);
                }
            }

            DeleteDebugBmpFiles(sideRoot);

            using (Mat alignedFull = new Mat(
                new Size(
                    ReferenceWidth,
                    measuredBinary.Height),
                DepthType.Cv8U,
                1))
            using (Mat rigidDiff = new Mat())
            {
                alignedFull.SetTo(
                    new MCvScalar(0));

                for (int i = 0;
                    i < segments.Count &&
                    i < FontNames.Length;
                    i++)
                {
                    SegmentInspection seg =
                        segments[i];

                    if (seg?.Aligned == null ||
                        seg.Aligned.IsEmpty)
                    {
                        continue;
                    }

                    int x1 = ReferenceX[i];
                    int width =
                        ReferenceX[i + 1] -
                        ReferenceX[i];

                    int copyWidth =
                        Math.Min(
                            width,
                            seg.Aligned.Width);

                    int copyHeight =
                        Math.Min(
                            alignedFull.Height,
                            seg.Aligned.Height);

                    if (copyWidth <= 0 ||
                        copyHeight <= 0)
                    {
                        continue;
                    }

                    using (Mat srcRoi = new Mat(
                        seg.Aligned,
                        new Rectangle(
                            0,
                            0,
                            copyWidth,
                            copyHeight)))
                    using (Mat dstRoi = new Mat(
                        alignedFull,
                        new Rectangle(
                            x1,
                            0,
                            copyWidth,
                            copyHeight)))
                    {
                        srcRoi.CopyTo(dstRoi);
                    }
                }

                CvInvoke.AbsDiff(
                    alignedFull,
                    fullTemplateBinary,
                    rigidDiff);

                string rigidPath =
                    Path.Combine(
                        sideRoot,
                        "rigid_diff.bmp");

                bool saved =
                    CvInvoke.Imwrite(
                        rigidPath,
                        rigidDiff);

                if (saved)
                {
                    TouchFileTimestamp(
                        rigidPath);
                }

                // V20 Overlay Debug: use the same final aligned binary images as rigid_diff.
                // Green=Golden, Red=Measured, Yellow=overlap (50% each).
                try
                {
                    SaveOverlayPair(alignedFull, fullTemplateBinary,
                        Path.Combine(sideRoot, "rigid_overlay_50.png"),
                        Path.Combine(sideRoot, "rigid_edge_overlay.png"));

                    string segmentOverlayDir = Path.Combine(sideRoot, "font_overlays");
                    Directory.CreateDirectory(segmentOverlayDir);
                    for (int i = 0; i < segments.Count && i < FontNames.Length; i++)
                    {
                        SegmentInspection seg = segments[i];
                        if (seg == null || seg.Aligned == null || seg.Template == null ||
                            seg.Aligned.IsEmpty || seg.Template.IsEmpty)
                            continue;
                        SaveOverlayPair(seg.Aligned, seg.Template,
                            Path.Combine(segmentOverlayDir, FontNames[i] + "_overlay_50.png"),
                            Path.Combine(segmentOverlayDir, FontNames[i] + "_edge_overlay.png"));
                    }
                }
                catch (Exception overlayError)
                {
                    Debug.WriteLine("[FontV20][OverlayDebug] " + overlayError);
                }

                Debug.WriteLine(
                    "[FontV15][Debug] rigid_diff save=" +
                    saved +
                    ", path=" +
                    rigidPath);
            }

            string txtPath = Path.Combine(
                sideRoot,
                "ncc_alignment.txt");

            using (StreamWriter writer =
                new StreamWriter(
                    txtPath,
                    false,
                    System.Text.Encoding.UTF8))
            {
                writer.WriteLine(
                    "FontV15 C# integration");

                writer.WriteLine(
                    "generated_time=" +
                    DateTime.Now.ToString(
                        "yyyy/MM/dd HH:mm:ss"));

                writer.WriteLine(
                    "side=" + sideName);

                writer.WriteLine(
                    "reference_width=" +
                    ReferenceWidth);

                writer.WriteLine(
                    "alignment_threshold=" +
                    (sideName == "Yin"
                        ? YinFixedThreshold
                        : YangFixedThreshold));

                writer.WriteLine(
                    "coarse_start_x=" +
                    startX);

                writer.WriteLine(
                    "coarse_score=" +
                    coarseScore.ToString("F6"));

                writer.WriteLine(
                    "compatibility_template=" +
                    compatibilityTemplateUsed);

                writer.WriteLine(
                    "defect_total_percentage=" +
                    defectResults
                        .Defect_Total_Percentage
                        .ToString("F6"));

                writer.WriteLine(
                    "block_total_percentage=" +
                    blockResults
                        .Block_Total_Percentage
                        .ToString("F6"));

                foreach (
                    SegmentInspection seg
                    in segments)
                {
                    writer.WriteLine(
                        seg.FontName + ": " +
                        "score=" +
                        seg.FineScore
                            .ToString("F6") +
                        ", shift=(" +
                        seg.ShiftX + "," +
                        seg.ShiftY + ")" +
                        ", original_shift=(" + seg.OriginalShiftX + "," + seg.OriginalShiftY + ")" +
                        ", original_score=" + seg.OriginalScore.ToString("F6") +
                        ", group=" + seg.CaptureGroup +
                        ", group_shift=(" + seg.GroupShiftX.ToString("F2") + "," + seg.GroupShiftY.ToString("F2") + ")" +
                        ", predicted_shift=(" + seg.PredictedShiftX.ToString("F2") + "," + seg.PredictedShiftY.ToString("F2") + ")" +
                        ", correction=" + seg.CorrectionStatus +
                        ", alignment_diagnostic=" + seg.AlignmentDiagnostic +
                        ", coarse_x1=" +
                        seg.FoundX1 +
                        ", coarse_score=" +
                        seg.CoarseScore
                            .ToString("F6") +
                        ", boundary=" +
                        seg.HitBoundary +
                        ", engineering_threshold=" +
                        seg.EngineeringThreshold
                            .ToString("F2") +
                        ", engineering_percent=" +
                        ThresholdToPercent(
                            seg.EngineeringThreshold)
                            .ToString("F2") +
                        ", defect=" +
                        seg.DefectPixels +
                        ", block=" +
                        seg.BlockPixels +
                        ", defect_pct=" +
                        seg.DefectPercentage
                            .ToString("F6") +
                        ", block_pct=" +
                        seg.BlockPercentage
                            .ToString("F6"));
                }
            }

            TouchFileTimestamp(txtPath);
        }

        // Debug only. This function never changes alignment or defect/block results.
        private static void SaveOverlayPair(Mat measuredBinary, Mat goldenBinary,
            string overlayPath, string edgeOverlayPath)
        {
            if (measuredBinary == null || goldenBinary == null ||
                measuredBinary.IsEmpty || goldenBinary.IsEmpty ||
                measuredBinary.Size != goldenBinary.Size)
                return;

            using (Mat goldenColor = new Mat(goldenBinary.Size, DepthType.Cv8U, 3))
            using (Mat measuredColor = new Mat(measuredBinary.Size, DepthType.Cv8U, 3))
            using (Mat overlay = new Mat())
            using (Mat goldenEdges = new Mat())
            using (Mat measuredEdges = new Mat())
            using (Mat goldenEdgeColor = new Mat(goldenBinary.Size, DepthType.Cv8U, 3))
            using (Mat measuredEdgeColor = new Mat(measuredBinary.Size, DepthType.Cv8U, 3))
            using (Mat edgeOverlay = new Mat())
            {
                // BGR colors: golden green, measured red.
                goldenColor.SetTo(new MCvScalar(0, 0, 0));
                measuredColor.SetTo(new MCvScalar(0, 0, 0));
                goldenColor.SetTo(new MCvScalar(0, 255, 0), goldenBinary);
                measuredColor.SetTo(new MCvScalar(0, 0, 255), measuredBinary);
                CvInvoke.AddWeighted(goldenColor, 0.5, measuredColor, 0.5, 0.0, overlay);
                if (CvInvoke.Imwrite(overlayPath, overlay))
                    TouchFileTimestamp(overlayPath);

                CvInvoke.Canny(goldenBinary, goldenEdges, 50, 150);
                CvInvoke.Canny(measuredBinary, measuredEdges, 50, 150);
                goldenEdgeColor.SetTo(new MCvScalar(0, 0, 0));
                measuredEdgeColor.SetTo(new MCvScalar(0, 0, 0));
                goldenEdgeColor.SetTo(new MCvScalar(0, 255, 0), goldenEdges);
                measuredEdgeColor.SetTo(new MCvScalar(0, 0, 255), measuredEdges);
                CvInvoke.AddWeighted(goldenEdgeColor, 0.5, measuredEdgeColor, 0.5, 0.0,
                    edgeOverlay);
                if (CvInvoke.Imwrite(edgeOverlayPath, edgeOverlay))
                    TouchFileTimestamp(edgeOverlayPath);
            }
        }

        private static void SaveSingleReAnalyzeDebug(
            string sideName,
            string fontName,
            Mat templateBinary,
            Mat alignedValidMask,
            Mat engineeringAligned,
            double score,
            int shiftX,
            int shiftY,
            double engineeringThreshold,
            double defectPercentage,
            double blockPercentage)
        {
            string root = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FontV15_Debug",
                "ReAnalyze",
                sideName,
                fontName);

            Directory.CreateDirectory(root);
            DeleteDebugBmpFiles(root);

            string rigidPath = Path.Combine(
                root,
                "rigid_diff.bmp");

            using (Mat rigidDiff =
                new Mat())
            using (Mat maskedRigidDiff =
                new Mat())
            {
                CvInvoke.AbsDiff(
                    engineeringAligned,
                    templateBinary,
                    rigidDiff);

                CvInvoke.BitwiseAnd(
                    rigidDiff,
                    alignedValidMask,
                    maskedRigidDiff);

                bool saved =
                    CvInvoke.Imwrite(
                        rigidPath,
                        maskedRigidDiff);

                if (saved)
                {
                    TouchFileTimestamp(
                        rigidPath);
                }
            }

            string txtPath = Path.Combine(
                root,
                "reanalysis.txt");

            using (StreamWriter writer =
                new StreamWriter(
                    txtPath,
                    false,
                    System.Text.Encoding.UTF8))
            {
                writer.WriteLine(
                    "generated_time=" +
                    DateTime.Now.ToString(
                        "yyyy/MM/dd HH:mm:ss"));

                writer.WriteLine(
                    "side=" + sideName);

                writer.WriteLine(
                    "font=" + fontName);

                writer.WriteLine(
                    "alignment_threshold=" +
                    (sideName == "Yin"
                        ? YinFixedThreshold
                        : YangFixedThreshold));

                writer.WriteLine(
                    "engineering_threshold=" +
                    engineeringThreshold
                        .ToString("F2"));

                writer.WriteLine(
                    "engineering_percent=" +
                    ThresholdToPercent(
                        engineeringThreshold)
                        .ToString("F2"));

                writer.WriteLine(
                    "score=" +
                    score.ToString("F6"));

                writer.WriteLine(
                    "shift=(" +
                    shiftX + "," +
                    shiftY + ")");

                writer.WriteLine(
                    "defect_pct=" +
                    defectPercentage
                        .ToString("F6"));

                writer.WriteLine(
                    "block_pct=" +
                    blockPercentage
                        .ToString("F6"));
            }

            TouchFileTimestamp(txtPath);
        }

        private static void DeleteDebugBmpFiles(
            string root)
        {
            if (!Directory.Exists(root))
                return;

            foreach (
                string path
                in Directory.GetFiles(
                    root,
                    "*.bmp",
                    SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "[FontV15][Debug] 無法刪除舊圖：" +
                        path +
                        " / " +
                        ex.Message);
                }
            }
        }

        /// <summary>
        /// Windows 檔案總管有時顯示「建立日期」而不是「修改日期」。
        /// Debug 圖每次更新後把三個時間都同步成現在，避免現場誤判成舊圖。
        /// </summary>
        private static void TouchFileTimestamp(
            string path)
        {
            if (!File.Exists(path))
                return;

            try
            {
                DateTime now =
                    DateTime.Now;

                File.SetCreationTime(
                    path,
                    now);

                File.SetLastWriteTime(
                    path,
                    now);

                File.SetLastAccessTime(
                    path,
                    now);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[FontV15][Debug] 更新檔案時間失敗：" +
                    path +
                    " / " +
                    ex.Message);
            }
        }

        private static void SetFontImg(Font_Imgs imgs, int index, Mat value)
        {
            switch (index)
            {
                case 0: imgs._3pt = value; break;
                case 1: imgs._4pt = value; break;
                case 2: imgs._5pt = value; break;
                case 3: imgs._6pt = value; break;
                case 4: imgs._7pt = value; break;
                case 5: imgs._8pt = value; break;
                case 6: imgs._9pt = value; break;
                case 7: imgs._10pt = value; break;
                case 8: imgs._11pt = value; break;
                case 9: imgs._12pt = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static void SetFontResult(Font_Results results, int index, double value)
        {
            switch (index)
            {
                case 0: results._3pt = value; break;
                case 1: results._4pt = value; break;
                case 2: results._5pt = value; break;
                case 3: results._6pt = value; break;
                case 4: results._7pt = value; break;
                case 5: results._8pt = value; break;
                case 6: results._9pt = value; break;
                case 7: results._10pt = value; break;
                case 8: results._11pt = value; break;
                case 9: results._12pt = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
