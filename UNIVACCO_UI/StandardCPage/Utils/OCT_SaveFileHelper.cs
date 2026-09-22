using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using NPOI.SS.Formula.Functions;
using Microsoft.VisualBasic;
using Microsoft.Office.Interop.Excel;

namespace StandardOPage
{
    public class OCT_SaveFileHelper
    {
        private string resultSavePath;    //儲存路徑

        public OCT_SaveFileHelper(string resultsavepath)
        {
            this.resultSavePath = resultsavepath;
        }
        public void Save(SaveData savedata)
        {
            //製作路徑 Ex: "C:\\UNIVACCO\\O_Result\\202504"
            string filepath = Get_FilePath(resultSavePath);   //製作存Excel路徑

            //如果路徑不存在則創建
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            //輸出Excel
            string excelfilepath = Path.Combine(filepath, "DataBaseResult_O.xlsx");
            //DataBase所有標題
            string[] database_titles = {
                "Date",
                "Time",
                "Lot No",
                "Remark",
                "URL",
                "CardType",
                "PrintType",
                "ExposureTime",
                "GLED_Used_Counter",
                "MeshP_10_Result",
                "MeshP_20_Result",
                "MeshP_30_Result",
                "MeshP_40_Result",
                "MeshP_60_Result",
                "MeshP_70_Result",
                "MeshP_80_Result",
                "MeshP_90_Result",
                "MeshP_Defect_Level",
                "MeshP_Block_Level",
                "Yin_3pt_Defect_Result",
                "Yin_4pt_Defect_Result",
                "Yin_5pt_Defect_Result",
                "Yin_6pt_Defect_Result",
                "Yin_7pt_Defect_Result",
                "Yin_8pt_Defect_Result",
                "Yin_9pt_Defect_Result",
                "Yin_10pt_Defect_Result",
                "Yin_11pt_Defect_Result",
                "Yin_12pt_Defect_Result",
                "Yin_Defect_Total_Percentage",
                "Yin_Defect_Level",
                "Yin_3pt_Block_Result",
                "Yin_4pt_Block_Result",
                "Yin_5pt_Block_Result",
                "Yin_6pt_Block_Result",
                "Yin_7pt_Block_Result",
                "Yin_8pt_Block_Result",
                "Yin_9pt_Block_Result",
                "Yin_10pt_Block_Result",
                "Yin_11pt_Block_Result",
                "Yin_12pt_Block_Result",
                "Yin_Block_Total_Percentage",
                "Yin_Block_Level",
                "Yang_3pt_Defect_Result",
                "Yang_4pt_Defect_Result",
                "Yang_5pt_Defect_Result",
                "Yang_6pt_Defect_Result",
                "Yang_7pt_Defect_Result",
                "Yang_8pt_Defect_Result",
                "Yang_9pt_Defect_Result",
                "Yang_10pt_Defect_Result",
                "Yang_11pt_Defect_Result",
                "Yang_12pt_Defect_Result",
                "Yang_Defect_Total_Percentage",
                "Yang_Defect_Level",
                "Yang_3pt_Block_Result",
                "Yang_4pt_Block_Result",
                "Yang_5pt_Block_Result",
                "Yang_6pt_Block_Result",
                "Yang_7pt_Block_Result",
                "Yang_8pt_Block_Result",
                "Yang_9pt_Block_Result",
                "Yang_10pt_Block_Result",
                "Yang_11pt_Block_Result",
                "Yang_12pt_Block_Result",
                "Yang_Block_Total_Percentage",
                "Yang_Block_Level",
                "Fullness_Defect_Result",
                "Fullness_Defect_Level",
                "MeshP_Standard_Name",
                "MeshP_Standard_Area10",
                "MeshP_Standard_Area20",
                "MeshP_Standard_Area30",
                "MeshP_Standard_Area40",
                "MeshP_Standard_Area60",
                "MeshP_Standard_Area70",
                "MeshP_Standard_Area80",
                "MeshP_Standard_Area90",
                "Yin_3pt_Threshold",
                "Yin_4pt_Threshold",
                "Yin_5pt_Threshold",
                "Yin_6pt_Threshold",
                "Yin_7pt_Threshold",
                "Yin_8pt_Threshold",
                "Yin_9pt_Threshold",
                "Yin_10pt_Threshold",
                "Yin_11pt_Threshold",
                "Yin_12pt_Threshold",
                "Yang_3pt_Threshold",
                "Yang_4pt_Threshold",
                "Yang_5pt_Threshold",
                "Yang_6pt_Threshold",
                "Yang_7pt_Threshold",
                "Yang_8pt_Threshold",
                "Yang_9pt_Threshold",
                "Yang_10pt_Threshold",
                "Yang_11pt_Threshold",
                "Yang_12pt_Threshold",
                "Fullness_Threshold",
                "MeshP_Rule_Block10",
                "MeshP_Rule_Block15",
                "MeshP_Rule_Block20",
                "MeshP_Rule_Block25",
                "MeshP_Rule_Block30",
                "MeshP_Rule_Block35",
                "MeshP_Rule_Block40",
                "MeshP_Rule_Block45",
                "MeshP_Rule_Block50",
                "MeshP_Rule_Defect10",
                "MeshP_Rule_Defect15",
                "MeshP_Rule_Defect20",
                "MeshP_Rule_Defect25",
                "MeshP_Rule_Defect30",
                "MeshP_Rule_Defect35",
                "MeshP_Rule_Defect40",
                "MeshP_Rule_Defect45",
                "MeshP_Rule_Defect50",
                "Yin_Rule_Block10",
                "Yin_Rule_Block15",
                "Yin_Rule_Block20",
                "Yin_Rule_Block25",
                "Yin_Rule_Block30",
                "Yin_Rule_Block35",
                "Yin_Rule_Block40",
                "Yin_Rule_Block45",
                "Yin_Rule_Block50",
                "Yin_Rule_Defect10",
                "Yin_Rule_Defect15",
                "Yin_Rule_Defect20",
                "Yin_Rule_Defect25",
                "Yin_Rule_Defect30",
                "Yin_Rule_Defect35",
                "Yin_Rule_Defect40",
                "Yin_Rule_Defect45",
                "Yin_Rule_Defect50",
                "Yang_Rule_Block10",
                "Yang_Rule_Block15",
                "Yang_Rule_Block20",
                "Yang_Rule_Block25",
                "Yang_Rule_Block30",
                "Yang_Rule_Block35",
                "Yang_Rule_Block40",
                "Yang_Rule_Block45",
                "Yang_Rule_Block50",
                "Yang_Rule_Defect10",
                "Yang_Rule_Defect15",
                "Yang_Rule_Defect20",
                "Yang_Rule_Defect25",
                "Yang_Rule_Defect30",
                "Yang_Rule_Defect35",
                "Yang_Rule_Defect40",
                "Yang_Rule_Defect45",
                "Yang_Rule_Defect50",
                "Fullness_Rule_Defect10",
                "Fullness_Rule_Defect15",
                "Fullness_Rule_Defect20",
                "Fullness_Rule_Defect25",
                "Fullness_Rule_Defect30",
                "Fullness_Rule_Defect35",
                "Fullness_Rule_Defect40",
                "Fullness_Rule_Defect45",
                "Fullness_Rule_Defect50"
            };
            try
            {
                ExportDataBaseExcel(excelfilepath, database_titles, savedata);
            }
            catch(Exception ex)
            {
                throw;
            }
            string imagefolderpath = Path.Combine(filepath, $"{DateTime.Now.Day:D2}"); //資料夾名稱 = 月   
            //如果路徑不存在則創建
            if (!Directory.Exists(imagefolderpath))
                Directory.CreateDirectory(imagefolderpath);
            SaveSourceImgs(imagefolderpath, savedata);
        }
        private void SaveSourceImgs(string imagefolderpath, SaveData savedata)
        {
            var timesplit = savedata.Time.Split(':');
            string name = timesplit[0] + timesplit[1] + timesplit[2]; // 檔名 = 時 + 分 + 秒

            List<Mat> sourceImgs = new List<Mat>
            {
                savedata.SourceImg1,
                savedata.SourceImg2,
                savedata.SourceImg3,
                savedata.SourceImg4,
                savedata.SourceImg5
            };

            // === 1. 儲存原始五張 (不做裁切) ===
            for (int i = 0; i < sourceImgs.Count; i++)
            {
                string imagefilepath = Path.Combine(imagefolderpath, $"{name}_{savedata.Lot_No}_s{i + 1}.jpg");
                CvInvoke.Imwrite(imagefilepath, sourceImgs[i]);
            }

            // === 2. 使用 Raw Image 製作合併圖 ===
            try
            {
                if (savedata.RawImg1 != null && savedata.RawImg2 != null &&
                    savedata.RawImg3 != null && savedata.RawImg4 != null &&
                    savedata.RawImg5 != null)
                {
                    int keepWidth = 5050;

                    // ---------- RawImg1 ----------
                    Mat cropped1 = savedata.RawImg1;
                    int y1Start = 0;
                    int y1End = 2855;

                    {
                        int width = Math.Min(keepWidth, cropped1.Width);
                        int height = Math.Min(y1End - y1Start, cropped1.Height - y1Start);
                        cropped1 = new Mat(
                            cropped1,
                            new System.Drawing.Rectangle(0, y1Start, width, height)
                        );
                    }

                    // ---------- RawImg2 ----------
                    Mat cropped2 = savedata.RawImg2;
                    int y2Start = 518;
                    int y2End = 3064;

                    {
                        int width = Math.Min(keepWidth, cropped2.Width);
                        int height = Math.Min(y2End - y2Start, cropped2.Height - y2Start);
                        cropped2 = new Mat(
                            cropped2,
                            new System.Drawing.Rectangle(0, y2Start, width, height)
                        );
                    }

                    // ---------- RawImg3 ----------
                    Mat cropped3 = savedata.RawImg3;
                    int y3Start = 969;
                    int y3End = 2526;

                    {
                        int width = Math.Min(keepWidth, cropped3.Width);
                        int height = Math.Min(y3End - y3Start, cropped3.Height - y3Start);
                        cropped3 = new Mat(
                            cropped3,
                            new System.Drawing.Rectangle(0, y3Start, width, height)
                        );
                    }

                    // ---------- RawImg4 ----------
                    Mat cropped4 = savedata.RawImg4;
                    int y4Start = 633;
                    int y4End = 2777;

                    {
                        int width = Math.Min(keepWidth, cropped4.Width);
                        int height = Math.Min(y4End - y4Start, cropped4.Height - y4Start);
                        cropped4 = new Mat(
                            cropped4,
                            new System.Drawing.Rectangle(0, y4Start, width, height)
                        );
                    }

                    // ---------- RawImg5 ----------
                    Mat cropped5 = savedata.RawImg5;
                    int y5Start = 385;
                    int y5End = 3647;

                    {
                        int width = Math.Min(keepWidth, cropped5.Width);
                        int height = Math.Min(y5End - y5Start, cropped5.Height - y5Start);
                        cropped5 = new Mat(
                            cropped5,
                            new System.Drawing.Rectangle(0, y5Start, width, height)
                        );
                    }

                    // ---------- 垂直合併 ----------
                    Mat merged = new Mat();
                    merged.PushBack(cropped1);
                    merged.PushBack(cropped2);
                    merged.PushBack(cropped3);
                    merged.PushBack(cropped4);
                    merged.PushBack(cropped5);

                    // ---------- 儲存 ----------
                    string mergedPath = Path.Combine(
                        imagefolderpath,
                        $"{name}_{savedata.Lot_No}_merged_raw.png"
                    );
                    CvInvoke.Imwrite(mergedPath, merged);

                    // ---------- 記錄路徑 ----------
                    savedata.MergedRawImagePath = mergedPath;
                }
            }
            catch (Exception ex)
            {
                // 視你原本專案需求，可加 log
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
            try { 
                //如果沒有excel則新建
                if (fileExists)
                {
                    // 讀取階段
                    using (FileStream fs = new FileStream(excelfilepath, FileMode.Open, FileAccess.Read))
                    {
                        if (fileExtension == ".xlsx") { workbook = new XSSFWorkbook(fs); }
                        else if (fileExtension == ".xls") { workbook = new HSSFWorkbook(fs); }
                        else { return; }
                    } // 這裡 fs 已經關閉，但 workbook 仍在記憶體中

                    sheet = workbook.GetSheetAt(0); // 獲取工作簿中的第一個工作表
                    IRow headerRow = sheet.GetRow(0);   // 獲取工作表的第一行（通常是標題行）
                    int cellCount = headerRow.LastCellNum;  // 獲取第一行的單元格數量（即標題的列數）
                    lastRow = sheet.LastRowNum + 1; // 獲取工作表的總行數，這裡需要加 1 來獲取實際的行數（LastRowNum 返回的是索引）

                }
                else
                {
                    // 檔案不存在，顯示訊息並建立新工作簿
                    MessageBox.Show("目錄無舊版Excel，建立一個新的Excel。", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    if (fileExtension == ".xlsx") { workbook = new XSSFWorkbook(); }
                    else if (fileExtension == ".xls") { workbook = new HSSFWorkbook(); }
                    else { return; }

                    sheet = workbook.CreateSheet("Sheet1"); //創建工作表
                    lastRow = 0;    //總行數設置0
                }
            }
            catch
            {
                MessageBox.Show("儲存失敗，請檢查是否關閉Excel檔案!", "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                OCT_LogHelper.WriteLog(LogLevel.Warning, Page.O, "儲存失敗，請檢查是否關閉Excel檔案!");
                throw;
            }
            // 如果是新創建的sheet，建立標題行
            if (lastRow == 0)
            {
                row = sheet.CreateRow(lastRow);
                for(int i = 0;i< database_titles.Length; i++)
                {
                    cell = row.CreateCell(i);
                    cell.SetCellValue(database_titles[i]);
                }
                lastRow++;
            }

            row = sheet.CreateRow(lastRow);
            int columnIndex = 0;
            #region 資料寫入
            // 基本資訊
            SetCellValue(workbook, row, columnIndex++, savedata.Date, "yyyy/mm/dd");
            SetCellValue(workbook, row, columnIndex++, savedata.Time, "yyyy:mm:dd");
            SetCellValue(workbook, row, columnIndex++, savedata.Lot_No);
            SetCellValue(workbook, row, columnIndex++, savedata.Remark);
            SetCellValue(workbook, row, columnIndex++, savedata.URL);
            SetCellValue(workbook, row, columnIndex++, savedata.CardType);
            SetCellValue(workbook, row, columnIndex++, savedata.PrintType);
            SetCellValue(workbook, row, columnIndex++, savedata.ExposureTime);
            SetCellValue(workbook, row, columnIndex++, savedata.GLED_Used_Counter);
            //網點計算結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area10,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area20,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area30,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area40,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area60,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area70,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area80,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.MeshP_Results.Area90,2));
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Results.Defect_Level);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Results.Block_Level);
            //陰版缺燙結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._3pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._4pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._5pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._6pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._7pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._8pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._9pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._10pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._11pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results._12pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Defect_Results.Defect_Total_Percentage,2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Defect_Results.Defect_Level);
            //陰版塞版結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._3pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._4pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._5pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._6pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._7pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._8pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._9pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._10pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._11pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results._12pt, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yin_Block_Results.Block_Total_Percentage, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Block_Results.Block_Level);
            //陽版缺燙結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._3pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._4pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._5pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._6pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._7pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._8pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._9pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._10pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._11pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results._12pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Defect_Results.Defect_Total_Percentage,2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Defect_Results.Defect_Level);
            //陽版塞版結果                                       
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._3pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._4pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._5pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._6pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._7pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._8pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._9pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._10pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._11pt,2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results._12pt, 2));
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Yang_Block_Results.Block_Total_Percentage, 2));
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Block_Results.Block_Level);
            //飽滿區結果
            SetCellValue(workbook, row, columnIndex++, Math.Round(savedata.Fullness_Results.Defect_Result,2));
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Results.Defect_Level);
            //網點區標準
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Name);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area10);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area20);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area30);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area40);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area60);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area70);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area80);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Standard.Area90);
            //陰版參數
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._3pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._4pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._5pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._6pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._7pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._8pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._9pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._10pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._11pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Parameter._12pt);
            //陽版參數
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._3pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._4pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._5pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._6pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._7pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._8pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._9pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._10pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._11pt);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Parameter._12pt);
            //飽滿區參數
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Parameter.Threshold);
            //網點區規則
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block10);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block15);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block20);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block25);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block30);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block35);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block40);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block45);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Block50);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect10);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect15);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect20);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect25);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect30);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect35);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect40);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect45);
            SetCellValue(workbook, row, columnIndex++, savedata.MeshP_Rule.Defect50);
            //陰版規則
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block10);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block15);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block20);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block25);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block30);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block35);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block40);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block45);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Block50);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect10);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect15);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect20);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect25);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect30);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect35);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect40);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect45);
            SetCellValue(workbook, row, columnIndex++, savedata.Yin_Rule.Defect50);
            //陽版規則
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block10);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block15);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block20);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block25);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block30);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block35);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block40);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block45);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Block50);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect10);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect15);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect20);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect25);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect30);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect35);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect40);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect45);
            SetCellValue(workbook, row, columnIndex++, savedata.Yang_Rule.Defect50);
            //飽滿區規則
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect10);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect15);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect20);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect25);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect30);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect35);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect40);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect45);
            SetCellValue(workbook, row, columnIndex++, savedata.Fullness_Rule.Defect50);
            #endregion
            //轉為位元組陣列  
            MemoryStream stream = new MemoryStream();
            workbook.Write(stream);
            var buf = stream.ToArray();

            // 寫入階段，使用獨立的 FileStream
            using (FileStream fs = new FileStream(excelfilepath, FileMode.Create, FileAccess.Write))
            {
                fs.Write(buf, 0, buf.Length);
                fs.Flush();
            }

        }
        //製作excel路徑=>"C:\\UNIVACCO\\O_Result\\202504"
        private string Get_FilePath(string resultsavepath)
        {
            DateTime now = DateTime.Now;
            string foldername = $"{now.Year}{now.Month:D2}";   //資料夾名稱 = 年份+月
            string excelfilepath = Path.Combine(resultsavepath, "O_Result", foldername);
            return excelfilepath;
        }

        // 建立一個輔助方法來設定單元格值及格式
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
        }
    }

}
