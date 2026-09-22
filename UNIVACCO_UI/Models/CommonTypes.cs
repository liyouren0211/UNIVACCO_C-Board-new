using Emgu.CV;
using Emgu.CV.Util;
using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UNIVACCO_UI;

namespace StandardOPage
{

    //字體區參數
    public class Font_Parameter
    {
        public double _3pt{ get; set; }
        public double _4pt{ get; set; }
        public double _5pt{ get; set; }
        public double _6pt{ get; set; }
        public double _7pt{ get; set; }
        public double _8pt{ get; set; }
        public double _9pt { get; set; }
        public double _10pt{ get; set; }
        public double _11pt{ get; set; }
        public double _12pt { get; set; }
    }
    //飽滿區參數
    public class Fullness_Parameter
    {
        public double Threshold { get; set; }
    }

    //破開區參數
    public class BreakArea_Parameter
    {
        public double Threshold_01 { get; set; }
        public double Threshold_02 { get; set; }
        public double Threshold_03 { get; set; }
        public double Threshold_Step1 { get; set; }
    }

    //網點區參數
    public class OCT_ParameterMeshP
    {
        public string Name { get; set; }
        public double Area10 { get; set; }
        public double Area20 { get; set; }
        public double Area30 { get; set; }
        public double Area40 { get; set; }
        public double Area60 { get; set; }
        public double Area70 { get; set; }
        public double Area80 { get; set; }
        public double Area90 { get; set; }
    }
    
    //印鑑參數
    public class OCT_Parameters_PrintType
    {
        public int meshp_abs_x1{ get; set; }
        public int meshp_abs_x2 { get; set; }
        public int meshp_abs_x3 { get; set; }
        public int font_range_3pt { get; set; }
        public int font_range_4pt { get; set; }
        public int font_range_5pt { get; set; }
        public int font_range_6pt { get; set; }
        public int font_range_7pt { get; set; }
        public int font_range_8pt { get; set; }
        public int font_range_9pt { get; set; }
        public int font_range_10pt { get; set; }
        public int font_range_11pt { get; set; }
        public int font_range_12pt { get; set; }
        private string printtype;
        UNIVACCO MainPage;
        public OCT_Parameters_PrintType(UNIVACCO _MainPage,string _printtype)
        {
            MainPage = _MainPage;
            printtype = _printtype;
            Reload();
        }
        public void Reload()
        {
            meshp_abs_x1 = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "meshp_abs_x1", "OCTParameters.ini"));
            meshp_abs_x2 = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "meshp_abs_x2", "OCTParameters.ini"));
            meshp_abs_x3 = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "meshp_abs_x3", "OCTParameters.ini"));
            font_range_3pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_3pt", "OCTParameters.ini"));
            font_range_4pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_4pt", "OCTParameters.ini"));
            font_range_5pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_5pt", "OCTParameters.ini"));
            font_range_6pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_6pt", "OCTParameters.ini"));
            font_range_7pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_7pt", "OCTParameters.ini"));
            font_range_8pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_8pt", "OCTParameters.ini"));
            font_range_9pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_9pt", "OCTParameters.ini"));
            font_range_10pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_10pt", "OCTParameters.ini"));
            font_range_11pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_11pt", "OCTParameters.ini"));
            font_range_12pt = Convert.ToInt16(MainPage.ReadIniFile($"{printtype}_Parameter", "font_range_12pt", "OCTParameters.ini"));          
        }
    }
    //判定規則
    public class OCT_Rule
    {
        public double Block10{ get; set; }
        public double Block15{ get; set; }
        public double Block20{ get; set; }
        public double Block25{ get; set; }
        public double Block30{ get; set; }
        public double Block35{ get; set; }
        public double Block40{ get; set; }
        public double Block45{ get; set; }
        public double Block50 { get; set; }
        public double Defect10{ get; set; }
        public double Defect15{ get; set; }
        public double Defect20{ get; set; }
        public double Defect25{ get; set; }
        public double Defect30{ get; set; }
        public double Defect35{ get; set; }
        public double Defect40{ get; set; }
        public double Defect45{ get; set; }
        public double Defect50 { get; set; }
    }
    //字體分析資訊
    public class FontInfo
    {
        public string Fontname;       // 字體名稱
        public Rectangle Bounds;  // 矩形範圍
        public int Block_Pixels;    //塞版數
        public int Defect_Pixels;   //缺燙數
    }
    //字體區分析結果
    public class Font_Results
    {
        public double _3pt { get; set; }
        public double _4pt { get; set; }
        public double _5pt { get; set; }
        public double _6pt { get; set; }
        public double _7pt { get; set; }
        public double _8pt { get; set; }
        public double _9pt { get; set; }
        public double _10pt { get; set; }
        public double _11pt { get; set; }
        public double _12pt { get; set; }
        public double Defect_Total_Percentage { get; set; }
        public double Block_Total_Percentage { get; set; } //新增總塞版
        public double Defect_Level { get; set; }
        public double Block_Level { get; set; }
    }
    //網點區分割影像
    public class MeshP_Imgs : IDisposable
    {
        public Mat Area10 { get; set; }
        public Mat Area20 { get; set; }
        public Mat Area30 { get; set; }
        public Mat Area40 { get; set; }
        public Mat Area60 { get; set; }
        public Mat Area70 { get; set; }
        public Mat Area80 { get; set; }
        public Mat Area90 { get; set; }

        public void Save(string filename)
        {
            if (Area10 != null && !Area10.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area10);
            if (Area20 != null && !Area20.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area20);
            if (Area30 != null && !Area30.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area30);
            if (Area40 != null && !Area40.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area40);
            if (Area60 != null && !Area60.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area60);
            if (Area70 != null && !Area70.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area70);
            if (Area80 != null && !Area80.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area80);
            if (Area90 != null && !Area90.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", Area90);
        }
        public void Dispose()
        {
            Area10?.Dispose();
            Area20?.Dispose();
            Area30?.Dispose();
            Area40?.Dispose();
            Area60?.Dispose();
            Area70?.Dispose();
            Area80?.Dispose();
            Area90?.Dispose();
        }
    }
    //網點區分析結果
    public class MeshP_Results
    {
        public double Area10 { get; set; }
        public double Area20 { get; set; }
        public double Area30 { get; set; }
        public double Area40 { get; set; }
        public double Area60 { get; set; }
        public double Area70 { get; set; }
        public double Area80 { get; set; }
        public double Area90 { get; set; }
        public double Defect_Level { get; set; }
        public double Block_Level { get; set; }
    }
    //字體區影像
    public class Font_Imgs : IDisposable
    {
        public Mat _3pt { get; set; }
        public Mat _4pt { get; set; }
        public Mat _5pt { get; set; }
        public Mat _6pt { get; set; }
        public Mat _7pt { get; set; }
        public Mat _8pt { get; set; }
        public Mat _9pt { get; set; }
        public Mat _10pt { get; set; }
        public Mat _11pt { get; set; }
        public Mat _12pt { get; set; }
        public void Save(string filename)
        {
            if (_3pt != null && !_3pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _3pt);
            if (_4pt != null && !_4pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _4pt);
            if (_5pt != null && !_5pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _5pt);
            if (_6pt != null && !_6pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _6pt);
            if (_7pt != null && !_7pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _7pt);
            if (_8pt != null && !_8pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _8pt);
            if (_9pt != null && !_9pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _9pt);
            if (_10pt != null && !_10pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _10pt);
            if (_11pt != null && !_11pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _11pt);
            if (_12pt != null && !_12pt.IsEmpty) CvInvoke.Imwrite($"{filename}.bmp", _12pt);
        }
        public void Dispose()
        {
            _3pt?.Dispose();
            _4pt?.Dispose();
            _5pt?.Dispose();
            _6pt?.Dispose();
            _7pt?.Dispose();
            _8pt?.Dispose();
            _9pt?.Dispose();
            _10pt?.Dispose();
        }
    }
    // 破開端影像 03/03
    public class BreakArea_Results
    {
        public Mat BreakArea_01_crop { get; set; }
        public Mat BreakArea_02_crop { get; set; }
        public Mat BreakArea_03_crop { get; set; }
    }
    //飽滿區分析結果
    public class Fullness_Results
    {
        public double Defect_Result { get; set; }
        public double Defect_Level { get; set; }
    }
    //字體模板資訊
    public class TemplateData
    {
        public Mat Image { get; set; }
        public int Width { get; set; }
        public int Height{ get; set; }
        public int Size  { get; set; }
        public int Total_BlockPixels { get; set; }
        public int Total_DefectPixels { get; set; }
        public TemplateData(string filePath)
        {
            Image = CvInvoke.Imread(filePath, Emgu.CV.CvEnum.ImreadModes.Grayscale);
            Width = Image.Width; // 直接存圖片寬度
            Height = Image.Height; // 直接存圖片寬度
            Size = Width * Height;
            (Total_BlockPixels,Total_DefectPixels) = Calc_totalpixels(Image);
            //Debug.WriteLine($"總數{Size}=塞版總數{Total_BlockPixels}+缺燙總數{Total_DefectPixels}");
        }
        private (int,int) Calc_totalpixels(Mat template)
        {
            int blockpixels=0, defectpixels;
            unsafe
            {
                byte* ptr = (byte*)template.DataPointer;
                for (int x = 0; x < template.Width; x++)
                {
                    for (int y = 0; y < template.Height; y++)
                    {
                        byte* current_ptr = ptr + (y * template.Width + x);
                        if (*current_ptr == 255)
                        {
                            blockpixels++;
                        }
                    }
                }
            }
            defectpixels = Size - blockpixels;
            return (blockpixels, defectpixels);
        }
    }
    //重新分析框選區域
    public class Region
    {
        public VectorOfPoint Contour { get; set; }
        public double Area { get; set; }
    }
    //重新分析區域資訊
    public class SingleAreaInfo
    {
        public Mat Image { get; set; }
        public string Label_Name { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public List<Region> Regions { get; set; }   // 變成包含面積的區域

    }
    //儲存資訊
    public class SaveData
    {
        // Corrected
        public Mat SourceImg1 { get; set; }
        public Mat SourceImg2 { get; set; }
        public Mat SourceImg3 { get; set; }
        public Mat SourceImg4 { get; set; }
        public Mat SourceImg5 { get; set; }
        // Raw
        public Mat RawImg1 { get; set; }
        public Mat RawImg2 { get; set; }
        public Mat RawImg3 { get; set; }
        public Mat RawImg4 { get; set; }
        public Mat RawImg5 { get; set; }

        public string MergedRawImagePath { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Lot_No { get; set; }
        public string Remark { get; set; }
        public string URL { get; set; }
        public string CardType { get; set; }
        public string PrintType { get; set; }
        public int ExposureTime { get; set; }
        public int GLED_Used_Counter {  get; set; }
        public MeshP_Results MeshP_Results { get; set; }
        public Font_Results Yin_Defect_Results { get; set; }
        public Font_Results Yin_Block_Results { get; set; }
        public Font_Results Yang_Defect_Results { get; set; }
        public Font_Results Yang_Block_Results { get; set; }
        public Fullness_Results Fullness_Results { get; set; }
        public OCT_ParameterMeshP MeshP_Standard { get; set; }  //網點區標準
        public Font_Parameter Yin_Parameter { get; set; }
        public Font_Parameter Yang_Parameter { get; set; }
        public Fullness_Parameter Fullness_Parameter { get; set; }

        public OCT_Rule MeshP_Rule { get; set; }
        public OCT_Rule Yin_Rule { get; set; }
        public OCT_Rule Yang_Rule { get; set; }
        public OCT_Rule Fullness_Rule { get; set; }

    }
    public class SaveOption
    {
        public bool Roi_CropPaper { get; set; }
        public bool Roi_Rotate { get; set; }
        public bool Roi_Crop { get; set; }
        public bool Roi_AreaCrop { get; set; }
        public bool MeshP_Crop { get; set; }
        public bool Meshp_Threshold { get; set; }
        public bool Yin_Original { get; set; }
        public bool Yin_FilterNoise { get; set; }
        public bool Yin_Threshold { get; set; }
        public bool Yin_CalcOutsideBlock { get; set; }
        public bool Yin_Correction { get; set; }
        public bool Yang_Original {  get; set; }
        public bool Yang_FilterNoise { get; set; }
        public bool Yang_Threshold { get; set; }
        public bool Yang_CalcOutsideBlock { get; set; }
        public bool Yang_Correction { get; set; }
        public bool Fullness_Threshold { get; set; }
        public bool MakeTemplates { get; set; }
        public bool IsValveEnabled { get; set; }

        public bool AutoMeshThresholdEnabled { get; set; }
    }
    public enum LogLevel
    {
        Info,    // ℹ️
        Warning, // ⚠️
        Error    // ❌
    }
    public enum Page
    {
        A,
        B,
        O,
    }
}
