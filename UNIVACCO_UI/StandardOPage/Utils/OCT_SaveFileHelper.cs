using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;

namespace StandardOPage
{
    public class OCT_SaveFileHelper
    {
        private string resultSavePath; // 儲存路徑

        public OCT_SaveFileHelper(string resultsavepath)
        {
            this.resultSavePath = resultsavepath;
        }

        public void Save(SaveData savedata)
        {
            // 製作路徑 Ex: "C:\\UNIVACCO\\O_Result\\202608"
            string filepath = Get_FilePath(resultSavePath);

            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            string excelfilepath = Path.Combine(filepath, "DataBaseResult_O.xlsx");

            string[] database_titles = {
                "Date", "Time", "Lot No", "Remark", "URL", "CardType", "PrintType",
                "ExposureTime", "GLED_Used_Counter",
                "MeshP_10_Result", "MeshP_20_Result", "MeshP_30_Result", "MeshP_40_Result",
                "MeshP_60_Result", "MeshP_70_Result", "MeshP_80_Result", "MeshP_90_Result",
                "MeshP_Defect_Level", "MeshP_Block_Level",
                "Yin_3pt_Defect_Result", "Yin_4pt_Defect_Result", "Yin_5pt_Defect_Result", "Yin_6pt_Defect_Result",
                "Yin_7pt_Defect_Result", "Yin_8pt_Defect_Result", "Yin_9pt_Defect_Result", "Yin_10pt_Defect_Result",
                "Yin_11pt_Defect_Result", "Yin_12pt_Defect_Result", "Yin_Defect_Total_Percentage", "Yin_Defect_Level",
                "Yin_3pt_Block_Result", "Yin_4pt_Block_Result", "Yin_5pt_Block_Result", "Yin_6pt_Block_Result",
                "Yin_7pt_Block_Result", "Yin_8pt_Block_Result", "Yin_9pt_Block_Result", "Yin_10pt_Block_Result",
                "Yin_11pt_Block_Result", "Yin_12pt_Block_Result", "Yin_Block_Total_Percentage", "Yin_Block_Level",
                "Yang_3pt_Defect_Result", "Yang_4pt_Defect_Result", "Yang_5pt_Defect_Result", "Yang_6pt_Defect_Result",
                "Yang_7pt_Defect_Result", "Yang_8pt_Defect_Result", "Yang_9pt_Defect_Result", "Yang_10pt_Defect_Result",
                "Yang_11pt_Defect_Result", "Yang_12pt_Defect_Result", "Yang_Defect_Total_Percentage", "Yang_Defect_Level",
                "Yang_3pt_Block_Result", "Yang_4pt_Block_Result", "Yang_5pt_Block_Result", "Yang_6pt_Block_Result",
                "Yang_7pt_Block_Result", "Yang_8pt_Block_Result", "Yang_9pt_Block_Result", "Yang_10pt_Block_Result",
                "Yang_11pt_Block_Result", "Yang_12pt_Block_Result", "Yang_Block_Total_Percentage", "Yang_Block_Level",
                "Fullness_Defect_Result", "Fullness_Defect_Level",
                "MeshP_Standard_Name", "MeshP_Standard_Area10", "MeshP_Standard_Area20", "MeshP_Standard_Area30",
                "MeshP_Standard_Area40", "MeshP_Standard_Area60", "MeshP_Standard_Area70", "MeshP_Standard_Area80",
                "MeshP_Standard_Area90",
                "Yin_3pt_Threshold", "Yin_4pt_Threshold", "Yin_5pt_Threshold", "Yin_6pt_Threshold",
                "Yin_7pt_Threshold", "Yin_8pt_Threshold", "Yin_9pt_Threshold", "Yin_10pt_Threshold",
                "Yin_11pt_Threshold", "Yin_12pt_Threshold",
                "Yang_3pt_Threshold", "Yang_4pt_Threshold", "Yang_5pt_Threshold", "Yang_6pt_Threshold",
                "Yang_7pt_Threshold", "Yang_8pt_Threshold", "Yang_9pt_Threshold", "Yang_10pt_Threshold",
                "Yang_11pt_Threshold", "Yang_12pt_Threshold",
                "Fullness_Threshold",
                "MeshP_Rule_Block10", "MeshP_Rule_Block15", "MeshP_Rule_Block20", "MeshP_Rule_Block25",
                "MeshP_Rule_Block30", "MeshP_Rule_Block35", "MeshP_Rule_Block40", "MeshP_Rule_Block45",
                "MeshP_Rule_Block50", "MeshP_Rule_Defect10", "MeshP_Rule_Defect15", "MeshP_Rule_Defect20",
                "MeshP_Rule_Defect25", "MeshP_Rule_Defect30", "MeshP_Rule_Defect35", "MeshP_Rule_Defect40",
                "MeshP_Rule_Defect45", "MeshP_Rule_Defect50",
                "Yin_Rule_Block10", "Yin_Rule_Block15", "Yin_Rule_Block20", "Yin_Rule_Block25",
                "Yin_Rule_Block30", "Yin_Rule_Block35", "Yin_Rule_Block40", "Yin_Rule_Block45",
                "Yin_Rule_Block50", "Yin_Rule_Defect10", "Yin_Rule_Defect15", "Yin_Rule_Defect20",
                "Yin_Rule_Defect25", "Yin_Rule_Defect30", "Yin_Rule_Defect35", "Yin_Rule_Defect40",
                "Yin_Rule_Defect45", "Yin_Rule_Defect50",
                "Yang_Rule_Block10", "Yang_Rule_Block15", "Yang_Rule_Block20", "Yang_Rule_Block25",
                "Yang_Rule_Block30", "Yang_Rule_Block35", "Yang_Rule_Block40", "Yang_Rule_Block45",
                "Yang_Rule_Block50", "Yang_Rule_Defect10", "Yang_Rule_Defect15", "Yang_Rule_Defect20",
                "Yang_Rule_Defect25", "Yang_Rule_Defect30", "Yang_Rule_Defect35", "Yang_Rule_Defect40",
                "Yang_Rule_Defect45", "Yang_Rule_Defect50",
                "Fullness_Rule_Defect10", "Fullness_Rule_Defect15", "Fullness_Rule_Defect20", "Fullness_Rule_Defect25",
                "Fullness_Rule_Defect30", "Fullness_Rule_Defect35", "Fullness_Rule_Defect40", "Fullness_Rule_Defect45",
                "Fullness_Rule_Defect50"
            };

            try
            {
                ExportDataBaseExcel(excelfilepath, database_titles, savedata);
            }
            catch (Exception)
            {
                throw;
            }

            string imagefolderpath = Path.Combine(filepath, $"{DateTime.Now.Day:D2}");
            if (!Directory.Exists(imagefolderpath))
                Directory.CreateDirectory(imagefolderpath);

            SaveSourceImgs(imagefolderpath, savedata);
        }

        private void SaveSourceImgs(string imagefolderpath, SaveData savedata)
        {
            var timesplit = savedata.Time.Split(':');
            string name = timesplit.Length >= 3 ? timesplit[0] + timesplit[1] + timesplit[2] : DateTime.Now.ToString("HHmmss");

            List<Mat> sourceImgs = new List<Mat>
            {
                savedata.SourceImg1,
                savedata.SourceImg2,
                savedata.SourceImg3,
                savedata.SourceImg4,
                savedata.SourceImg5
            };

            // === 1. 儲存旋轉校正後影像 (含空值安全保護) ===
            for (int i = 0; i < sourceImgs.Count; i++)
            {
                if (sourceImgs[i] != null && !sourceImgs[i].IsEmpty)
                {
                    string imagefilepath = Path.Combine(imagefolderpath, $"{name}_{savedata.Lot_No}_s{i + 1}.jpg");
                    CvInvoke.Imwrite(imagefilepath, sourceImgs[i]);
                }
            }

            // === 2. 使用 Raw Image 製作垂直合併圖 (含邊界防呆) ===
            try
            {
                List<Mat> rawImgs = new List<Mat>
                {
                    savedata.RawImg1,
                    savedata.RawImg2,
                    savedata.RawImg3,
                    savedata.RawImg4,
                    savedata.RawImg5
                };

                if (rawImgs.All(img => img != null && !img.IsEmpty))
                {
                    int keepWidth = 5050;

                    // 輔助安全裁切函式
                    Mat SafeCrop(Mat src, int yStart, int yEnd)
                    {
                        int sy = Math.Max(0, Math.Min(src.Height - 1, yStart));
                        int ey = Math.Max(sy + 1, Math.Min(src.Height, yEnd));
                        int w = Math.Min(keepWidth, src.Width);
                        return new Mat(src, new System.Drawing.Rectangle(0, sy, w, ey - sy));
                    }

                    Mat cropped1 = SafeCrop(rawImgs[0], 0, 2855);
                    Mat cropped2 = SafeCrop(rawImgs[1], 518, 3064);
                    Mat cropped3 = SafeCrop(rawImgs[2], 969, 2526);
                    Mat cropped4 = SafeCrop(rawImgs[3], 633, 2777);
                    Mat cropped5 = SafeCrop(rawImgs[4], 385, 3647);

                    using (Mat merged = new Mat())
                    {
                        merged.PushBack(cropped1);
                        merged.PushBack(cropped2);
                        merged.PushBack(cropped3);
                        merged.PushBack(cropped4);
                        merged.PushBack(cropped5);

                        string mergedPath = Path.Combine(imagefolderpath, $"{name}_{savedata.Lot_No}_merged_raw.png");
                        CvInvoke.Imwrite(mergedPath, merged);
                        savedata.MergedRawImagePath = mergedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MergeRawImage] Error: {ex.Message}");
            }
        }

        public void ExportDataBaseExcel(string excelfilepath, string[] database_titles, SaveData savedata)
        {
            IWorkbook workbook;
            ISheet sheet;
            IRow row;
            ICell cell;
            int lastRow;

            string fileExtension = Path.GetExtension(excelfilepath).ToLower();
            bool fileExists = File.Exists(excelfilepath);

            try
            {
                if (fileExists)
                {
                    using (FileStream fs = new FileStream(excelfilepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (fileExtension == ".xlsx") workbook = new XSSFWorkbook(fs);
                        else if (fileExtension == ".xls") workbook = new HSSFWorkbook(fs);
                        else return;
                    }

                    sheet = workbook.GetSheetAt(0);
                    lastRow = sheet.LastRowNum + 1;
                }
                else
                {
                    if (fileExtension == ".xlsx") workbook = new XSSFWorkbook();
                    else if (fileExtension == ".xls") workbook = new HSSFWorkbook();
                    else return;

                    sheet = workbook.CreateSheet("Sheet1");
                    lastRow = 0;
                }
            }
            catch
            {
                MessageBox.Show("儲存失敗，請檢查是否已開啟並鎖定 Excel 檔案！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }

            if (lastRow == 0)
            {
                row = sheet.CreateRow(lastRow);
                for (int i = 0; i < database_titles.Length; i++)
                {
                    cell = row.CreateCell(i);
                    cell.SetCellValue(database_titles[i]);
                }
                lastRow++;
            }

            row = sheet.CreateRow(lastRow);
            int columnIndex = 0;

            #region 資料寫入 (含 null 數值安全防呆)
            SetCellValue(workbook, row, columnIndex++, savedata.Date, "yyyy/mm/dd");
            SetCellValue(workbook, row, columnIndex++, savedata.Time);
            SetCellValue(workbook, row, columnIndex++, savedata.Lot_No);
            SetCellValue(workbook, row, columnIndex++, savedata.Remark);
            SetCellValue(workbook, row, columnIndex++, savedata.URL);
            SetCellValue(workbook, row, columnIndex++, savedata.CardType);
            SetCellValue(workbook, row, columnIndex++, savedata.PrintType);
            SetCellValue(workbook, row, columnIndex++, savedata.ExposureTime);
            SetCellValue(workbook, row, columnIndex++, savedata.GLED_Used_Counter);

            // 網點計算結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area10 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area20 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area30 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area40 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area60 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area70 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area80 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results?.Area90 ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Results?.Defect_Level ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Results?.Block_Level ?? 0);

            // 陰版缺燙結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._3pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._4pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._5pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._6pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._7pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._8pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._9pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._10pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._11pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?._12pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results?.Defect_Total_Percentage ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Defect_Results?.Defect_Level ?? 0);

            // 陰版塞版結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._3pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._4pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._5pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._6pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._7pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._8pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._9pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._10pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._11pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?._12pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results?.Block_Total_Percentage ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Block_Results?.Block_Level ?? 0);

            // 陽版缺燙結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._3pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._4pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._5pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._6pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._7pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._8pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._9pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._10pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._11pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?._12pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results?.Defect_Total_Percentage ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Defect_Results?.Defect_Level ?? 0);

            // 陽版塞版結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._3pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._4pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._5pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._6pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._7pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._8pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._9pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._10pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._11pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?._12pt ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results?.Block_Total_Percentage ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Block_Results?.Block_Level ?? 0);

            // 飽滿區結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Fullness_Results?.Defect_Result ?? 0, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Results?.Defect_Level ?? 0);

            // 網點標準與參數
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Name ?? "");
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area60 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area70 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area80 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard?.Area90 ?? 0);

            // 陰版門檻
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._3pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._4pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._5pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._6pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._7pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._8pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._9pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._10pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._11pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter?._12pt ?? 0);

            // 陽版門檻
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._3pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._4pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._5pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._6pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._7pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._8pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._9pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._10pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._11pt ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter?._12pt ?? 0);

            // 飽滿區門檻
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Parameter?.Threshold ?? 0);

            // 網點規則
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Block50 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule?.Defect50 ?? 0);

            // 陰版規則
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Block50 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule?.Defect50 ?? 0);

            // 陽版規則
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Block50 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule?.Defect50 ?? 0);

            // 飽滿區規則
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect10 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect15 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect20 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect25 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect30 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect35 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect40 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect45 ?? 0);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule?.Defect50 ?? 0);
            #endregion

            using (MemoryStream stream = new MemoryStream())
            {
                workbook.Write(stream);
                var buf = stream.ToArray();

                using (FileStream fs = new FileStream(excelfilepath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buf, 0, buf.Length);
                    fs.Flush();
                }
            }
        }

        private string Get_FilePath(string resultsavepath)
        {
            DateTime now = DateTime.Now;
            string foldername = $"{now.Year}{now.Month:D2}";
            string excelfilepath = Path.Combine(resultsavepath, "O_Result", foldername);
            return excelfilepath;
        }

        private void SetCellValue(IWorkbook workbook, IRow row, int column, object value, string formatPattern = null)
        {
            ICell cell = row.CreateCell(column);
            if (formatPattern != null)
            {
                ICellStyle style = workbook.CreateCellStyle();
                IDataFormat format = workbook.CreateDataFormat();
                style.DataFormat = format.GetFormat(formatPattern);
                cell.CellStyle = style;
            }

            if (value is string strValue)
                cell.SetCellValue(strValue);
            else if (value is double doubleValue)
                cell.SetCellValue(doubleValue);
            else if (value is int intValue)
                cell.SetCellValue(intValue);
            else if (value is DateTime dateValue)
                cell.SetCellValue(dateValue);
            else if (value != null)
                cell.SetCellValue(value.ToString());
            else
                cell.SetCellValue("");
        }
    }
}