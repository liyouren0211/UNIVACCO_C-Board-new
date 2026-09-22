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
    /// 1. Yin / Yang 固定二值化（Yin=150、Yang=230 並反相）
    /// 2. 3pt~12pt 固定參考範圍（總寬 3550）
    /// 3. 整條字體 0.5 倍 NCC 粗定位 start_x
    /// 4. 每一字級在預期位置 ±40 px 做局部 NCC
    /// 5. 再於 ±5 px 做 X/Y 精細 NCC 對位
    /// 6. Template - Measured 產生缺燙；Measured - Template 產生塞版
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
                OCT_Parameters_SaveOptions saveOptions,
                string cardType)
        {
            var result = AnalyzeSide(
                yinArea,
                legacyTemplates,
                true,
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
            var result = AnalyzeSide(
                yangArea,
                legacyTemplates,
                false,
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
        /// 回傳：OriginalImgs, BlockImgs, BlockResults, DefectImgs, DefectResults, FontInfos
        /// </summary>
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
                OCT_Parameters_SaveOptions saveOptions,
                string cardType)
        {
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

                for (int i = 0; i < FontNames.Length; i++)
                {
                    SegmentInspection seg = InspectSegment(
                        area,
                        measuredBinary,
                        fullTemplateBinary,
                        startX,
                        i,
                        sideName);

                    segmentResults.Add(seg);

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
                        Defect_Pixels = seg.DefectPixels
                    });

                    totalDefectPixels += seg.DefectPixels;
                    totalBlockPixels += seg.BlockPixels;
                    totalTemplateForeground += seg.TemplateForegroundPixels;
                    totalTemplateBackground += seg.TemplateBackgroundPixels;

                    Debug.WriteLine(
                        $"[FontV15][{sideName}][{seg.FontName}] " +
                        $"score={seg.FineScore:F6}, shift=({seg.ShiftX},{seg.ShiftY}), " +
                        $"coarseX={seg.FoundX1}, boundary={seg.HitBoundary}, " +
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

        private static SegmentInspection InspectSegment(
            Mat originalArea,
            Mat measuredBinary,
            Mat fullTemplateBinary,
            int globalStartX,
            int index,
            string sideName)
        {
            int expectedX1 = ReferenceX[index];
            int expectedX2 = ReferenceX[index + 1];
            int segmentWidth = expectedX2 - expectedX1;

            Mat templateSegment = new Mat(
                fullTemplateBinary,
                new Rectangle(expectedX1, 0, segmentWidth, fullTemplateBinary.Height)
            ).Clone();

            int windowX1 = Math.Max(0, expectedX1 - LocalAlignmentMaxShift);
            int windowX2 = Math.Min(measuredBinary.Width, expectedX2 + LocalAlignmentMaxShift);
            int windowWidth = windowX2 - windowX1;

            if (windowWidth < segmentWidth)
            {
                templateSegment.Dispose();
                throw new InvalidOperationException(
                    $"{sideName} {FontNames[index]} 局部搜尋窗口不足：" +
                    $"window={windowWidth}, template={segmentWidth}");
            }

            Mat window = null;
            Mat selected = null;
            Mat nominal = null;
            Mat original = null;
            Mat block = null;
            Mat defect = null;
            FineAlignResult fine = null;

            try
            {
                window = new Mat(
                    measuredBinary,
                    new Rectangle(windowX1, 0, windowWidth, measuredBinary.Height)
                ).Clone();

                Point localLoc;
                double localScore = MatchTemplateBest(window, templateSegment, out localLoc);

                int foundX1 = windowX1 + localLoc.X;
                int foundX2 = foundX1 + segmentWidth;
                foundX1 = Math.Max(0, Math.Min(foundX1, measuredBinary.Width - segmentWidth));
                foundX2 = foundX1 + segmentWidth;

                bool hitBoundary =
                    localLoc.X == 0 ||
                    localLoc.X == (windowWidth - segmentWidth);

                selected = new Mat(
                    measuredBinary,
                    new Rectangle(foundX1, 0, segmentWidth, measuredBinary.Height)
                ).Clone();

                nominal = new Mat(
                    measuredBinary,
                    new Rectangle(expectedX1, 0, segmentWidth, measuredBinary.Height)
                ).Clone();

                fine = FineAlignBinary(selected, templateSegment, FineAlignmentMaxShift);

                defect = BuildDefectImage(fine.Aligned, templateSegment);
                block = BuildBlockImage(fine.Aligned, templateSegment);

                int defectPixels = CountWhitePixels(defect);
                int blockPixels = CountWhitePixels(block);
                int templateForeground = CountWhitePixels(templateSegment);
                int templateBackground = templateSegment.Width * templateSegment.Height - templateForeground;

                int sourceX = globalStartX + foundX1;
                sourceX = Math.Max(0, Math.Min(sourceX, originalArea.Width - segmentWidth));
                using (Mat originalRoi = new Mat(
                    originalArea,
                    new Rectangle(sourceX, 0, segmentWidth, originalArea.Height)))
                {
                    original = EnsureBgr(originalRoi);
                }

                return new SegmentInspection
                {
                    FontName = FontNames[index],
                    Index = index,
                    ExpectedX1 = expectedX1,
                    ExpectedX2 = expectedX2,
                    FoundX1 = foundX1,
                    CoarseScore = localScore,
                    FineScore = fine.Score,
                    ShiftX = (foundX1 - expectedX1) + fine.ShiftX,
                    ShiftY = fine.ShiftY,
                    HitBoundary = hitBoundary,
                    Original = original,
                    Nominal = nominal,
                    Selected = selected,
                    Template = templateSegment,
                    Aligned = fine.Aligned,
                    Defect = defect,
                    Block = block,
                    DefectPixels = defectPixels,
                    BlockPixels = blockPixels,
                    TemplateForegroundPixels = templateForeground,
                    TemplateBackgroundPixels = templateBackground
                };
            }
            catch
            {
                original?.Dispose();
                nominal?.Dispose();
                selected?.Dispose();
                templateSegment?.Dispose();
                fine?.Dispose();
                defect?.Dispose();
                block?.Dispose();
                throw;
            }
            finally
            {
                window?.Dispose();
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

        private static Mat TryLoadHighResolutionTemplate(
            string sideName,
            string fontName,
            string cardType,
            int expectedHeight,
            int expectedWidth)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string filename = sideName + "_template_" + fontName + ".bmp";
            string safeCardType = string.IsNullOrWhiteSpace(cardType) ? "白卡" : cardType.Trim();

            string[] candidates =
            {
                Path.Combine(baseDir, "font_v15_templates", safeCardType, filename),
                Path.Combine(baseDir, "font_v15_templates", filename),
                Path.Combine(baseDir, "baseline_test", "templates", filename)
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

        private static Mat FixedFontBinary(Mat grayInput, bool isYin)
        {
            Mat gray = ToGray(grayInput);
            Mat binary = new Mat();
            try
            {
                CvInvoke.Threshold(
                    gray,
                    binary,
                    isYin ? YinFixedThreshold : YangFixedThreshold,
                    255,
                    ThresholdType.Binary);

                if (!isYin)
                {
                    Mat inverted = new Mat();
                    CvInvoke.BitwiseNot(binary, inverted);
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
                return Math.Max(0, Math.Min(start, maxStart));
            }
            finally
            {
                smallArea.Dispose();
                smallTemplate.Dispose();
            }
        }

        private static double MatchTemplateBest(Mat image, Mat template, out Point maxLoc)
        {
            Mat imageF = new Mat();
            Mat templateF = new Mat();
            Mat result = new Mat();
            try
            {
                image.ConvertTo(imageF, DepthType.Cv32F);
                template.ConvertTo(templateF, DepthType.Cv32F);
                CvInvoke.MatchTemplate(
                    imageF,
                    templateF,
                    result,
                    TemplateMatchingType.CcorrNormed);

                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = default(Point);
                maxLoc = default(Point);
                CvInvoke.MinMaxLoc(
                    result,
                    ref minVal,
                    ref maxVal,
                    ref minLoc,
                    ref maxLoc);
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
            int maxShift)
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
                double score = MatchTemplateBest(padded, template, out maxLoc);

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
                AppDomain.CurrentDomain.BaseDirectory,
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
            string root = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "FontV15_Debug",
                sideName,
                "font_segments");
            Directory.CreateDirectory(root);

            CvInvoke.Imwrite(Path.Combine(root, "input_gray.bmp"), gray);
            CvInvoke.Imwrite(Path.Combine(root, "input_binary_full.bmp"), measuredBinaryFull);
            CvInvoke.Imwrite(Path.Combine(root, "input_coarse_crop.bmp"), grayComparison);
            CvInvoke.Imwrite(Path.Combine(root, "template_gray.bmp"), fullTemplate);
            CvInvoke.Imwrite(Path.Combine(root, "template_binary.bmp"), fullTemplateBinary);
            CvInvoke.Imwrite(Path.Combine(root, "input_binary.bmp"), measuredBinary);

            foreach (SegmentInspection seg in segments)
            {
                string prefix = seg.FontName + "_";
                CvInvoke.Imwrite(Path.Combine(root, prefix + "00_nominal.bmp"), seg.Nominal);
                CvInvoke.Imwrite(Path.Combine(root, prefix + "01_measured.bmp"), seg.Selected);
                CvInvoke.Imwrite(Path.Combine(root, prefix + "02_template.bmp"), seg.Template);
                CvInvoke.Imwrite(Path.Combine(root, prefix + "03_aligned.bmp"), seg.Aligned);
                CvInvoke.Imwrite(Path.Combine(root, prefix + "04_defect.bmp"), seg.Defect);
                CvInvoke.Imwrite(Path.Combine(root, prefix + "05_block.bmp"), seg.Block);
            }

            using (StreamWriter writer = new StreamWriter(
                Path.Combine(root, "ncc_alignment.txt"),
                false,
                System.Text.Encoding.UTF8))
            {
                writer.WriteLine("FontV15 C# integration");
                writer.WriteLine("side=" + sideName);
                writer.WriteLine("reference_width=" + ReferenceWidth);
                writer.WriteLine("threshold=" + (sideName == "Yin" ? YinFixedThreshold : YangFixedThreshold));
                writer.WriteLine("coarse_start_x=" + startX);
                writer.WriteLine("coarse_score=" + coarseScore.ToString("F6"));
                writer.WriteLine("compatibility_template=" + compatibilityTemplateUsed);
                writer.WriteLine("defect_total_percentage=" + defectResults.Defect_Total_Percentage.ToString("F6"));
                writer.WriteLine("block_total_percentage=" + blockResults.Block_Total_Percentage.ToString("F6"));

                foreach (SegmentInspection seg in segments)
                {
                    writer.WriteLine(
                        seg.FontName + ": " +
                        "score=" + seg.FineScore.ToString("F6") + ", " +
                        "shift=(" + seg.ShiftX + "," + seg.ShiftY + "), " +
                        "coarse_x1=" + seg.FoundX1 + ", " +
                        "coarse_score=" + seg.CoarseScore.ToString("F6") + ", " +
                        "boundary=" + seg.HitBoundary + ", " +
                        "defect=" + seg.DefectPixels + ", " +
                        "block=" + seg.BlockPixels + ", " +
                        "defect_pct=" + seg.DefectPercentage.ToString("F6") + ", " +
                        "block_pct=" + seg.BlockPercentage.ToString("F6"));
                }
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
