using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNIVACCO_UI;
namespace StandardOPage
{
    public class OCT_Parameters_CardType
    {
        UNIVACCO MainPage;
        //網點區參數
        public Font_Parameter yin_parameter = new Font_Parameter();
        public Font_Parameter yang_parameter = new Font_Parameter();
        public Fullness_Parameter fullness_parameter = new Fullness_Parameter();
        public BreakArea_Parameter breakarea_parameter = new BreakArea_Parameter();
        private string cardtype;
        // 初始化並從 INI 讀取
        public OCT_Parameters_CardType(UNIVACCO _MainPage, string _cardtype)
        {

            MainPage = _MainPage;
            cardtype = _cardtype;
            Reload();
        }
        // 將 0~100 轉為 0~255
        public void Reload()
        {
            //讀取ini值百分比轉為Threshold值
            yin_parameter._3pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_3pt", "OCTParameters.ini")));
            yin_parameter._4pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_4pt", "OCTParameters.ini")));
            yin_parameter._5pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_5pt", "OCTParameters.ini")));
            yin_parameter._6pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_6pt", "OCTParameters.ini")));
            yin_parameter._7pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_7pt", "OCTParameters.ini")));
            yin_parameter._8pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_8pt", "OCTParameters.ini")));
            yin_parameter._9pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_9pt", "OCTParameters.ini")));
            yin_parameter._10pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_10pt", "OCTParameters.ini")));
            yin_parameter._11pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_11pt", "OCTParameters.ini")));
            yin_parameter._12pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yin_Parameter", "_12pt", "OCTParameters.ini")));
            yang_parameter._3pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_3pt", "OCTParameters.ini")));
            yang_parameter._4pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_4pt", "OCTParameters.ini")));
            yang_parameter._5pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_5pt", "OCTParameters.ini")));
            yang_parameter._6pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_6pt", "OCTParameters.ini")));
            yang_parameter._7pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_7pt", "OCTParameters.ini")));
            yang_parameter._8pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_8pt", "OCTParameters.ini")));
            yang_parameter._9pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_9pt", "OCTParameters.ini")));
            yang_parameter._10pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_10pt", "OCTParameters.ini")));
            yang_parameter._11pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_11pt", "OCTParameters.ini")));
            yang_parameter._12pt = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Yang_Parameter", "_12pt", "OCTParameters.ini")));
            fullness_parameter.Threshold = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_Fullness_Parameter", "Threshold", "OCTParameters.ini")));
            breakarea_parameter.Threshold_01 = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_BreakArea_Parameter", "Threshold_01", "OCTParameters.ini")));
            breakarea_parameter.Threshold_02 = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_BreakArea_Parameter", "Threshold_02", "OCTParameters.ini")));
            breakarea_parameter.Threshold_03 = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_BreakArea_Parameter", "Threshold_03", "OCTParameters.ini")));
            breakarea_parameter.Threshold_Step1 = PercentToThreshold(Convert.ToDouble(MainPage.ReadIniFile($"{cardtype}_BreakArea_Parameter", "Threshold_Step1", "OCTParameters.ini")));

        }
        public void Save(Font_Parameter _yin_parameter, Font_Parameter _yang_parameter, Fullness_Parameter _fullness_parameter, BreakArea_Parameter _breakarea_parameter)
        {
            //UI傳遞進來的百分比值轉為Threshold值
            yin_parameter._3pt =    PercentToThreshold(_yin_parameter._3pt);
            yin_parameter._4pt =    PercentToThreshold(_yin_parameter._4pt);
            yin_parameter._5pt =    PercentToThreshold(_yin_parameter._5pt);
            yin_parameter._6pt =    PercentToThreshold(_yin_parameter._6pt);
            yin_parameter._7pt =    PercentToThreshold(_yin_parameter._7pt);
            yin_parameter._8pt =    PercentToThreshold(_yin_parameter._8pt);
            yin_parameter._9pt =    PercentToThreshold(_yin_parameter._9pt);
            yin_parameter._10pt =   PercentToThreshold(_yin_parameter._10pt);
            yin_parameter._11pt =   PercentToThreshold(_yin_parameter._11pt);
            yin_parameter._12pt =   PercentToThreshold(_yin_parameter._12pt);
            yang_parameter._3pt =   PercentToThreshold(_yang_parameter._3pt);
            yang_parameter._4pt =   PercentToThreshold(_yang_parameter._4pt);
            yang_parameter._5pt =   PercentToThreshold(_yang_parameter._5pt);
            yang_parameter._6pt =   PercentToThreshold(_yang_parameter._6pt);
            yang_parameter._7pt =   PercentToThreshold(_yang_parameter._7pt);
            yang_parameter._8pt =   PercentToThreshold(_yang_parameter._8pt);
            yang_parameter._9pt =   PercentToThreshold(_yang_parameter._9pt);
            yang_parameter._10pt =  PercentToThreshold(_yang_parameter._10pt);
            yang_parameter._11pt =  PercentToThreshold(_yang_parameter._11pt);
            yang_parameter._12pt = PercentToThreshold(_yang_parameter._12pt);
            fullness_parameter.Threshold = PercentToThreshold(_fullness_parameter.Threshold);
            breakarea_parameter.Threshold_01 = PercentToThreshold(_breakarea_parameter.Threshold_01);
            breakarea_parameter.Threshold_02 = PercentToThreshold(_breakarea_parameter.Threshold_02);
            breakarea_parameter.Threshold_03 = PercentToThreshold(_breakarea_parameter.Threshold_03);
            breakarea_parameter.Threshold_Step1 = PercentToThreshold(_breakarea_parameter.Threshold_Step1);

            //UI傳遞進來的百分比值存入ini
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_3pt", _yin_parameter._3pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_4pt", _yin_parameter._4pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_5pt", _yin_parameter._5pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_6pt", _yin_parameter._6pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_7pt", _yin_parameter._7pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_8pt", _yin_parameter._8pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_9pt", _yin_parameter._9pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_10pt",_yin_parameter._10pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_11pt",_yin_parameter._11pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yin_Parameter", "_12pt",_yin_parameter._12pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_3pt", _yang_parameter._3pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_4pt", _yang_parameter._4pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_5pt", _yang_parameter._5pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_6pt", _yang_parameter._6pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_7pt", _yang_parameter._7pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_8pt", _yang_parameter._8pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_9pt", _yang_parameter._9pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_10pt",_yang_parameter._10pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_11pt",_yang_parameter._11pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Yang_Parameter", "_12pt",_yang_parameter._12pt.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_Fullness_Parameter", "Threshold", _fullness_parameter.Threshold.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_BreakArea_Parameter", "Threshold_01", _breakarea_parameter.Threshold_01.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_BreakArea_Parameter", "Threshold_02", _breakarea_parameter.Threshold_02.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_BreakArea_Parameter", "Threshold_03", _breakarea_parameter.Threshold_03.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue($"{cardtype}_BreakArea_Parameter", "Threshold_Step1",_breakarea_parameter.Threshold_Step1.ToString(), "OCTParameters.ini");
        }
        private double PercentToThreshold(double percent)
        {
            return percent / 100.0 * 255.0;
        }
    }
    public class OCT_Parameters_SaveOptions
    {
        UNIVACCO MainPage;
        public SaveOption saveoption = new SaveOption();
        public OCT_Parameters_SaveOptions(UNIVACCO _MainPage)
        {
            MainPage = _MainPage;
            Reload();
        }
        public void Reload()
        {
            saveoption.Roi_CropPaper = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Roi_CropPaper", "OCTParameters.ini"));
            saveoption.Roi_Rotate = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Roi_Rotate", "OCTParameters.ini"));
            saveoption.Roi_Crop = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Roi_Crop", "OCTParameters.ini"));
            saveoption.Roi_AreaCrop = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Roi_AreaCrop", "OCTParameters.ini"));
            saveoption.MeshP_Crop = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "MeshP_Crop", "OCTParameters.ini"));
            saveoption.Meshp_Threshold = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Meshp_Threshold", "OCTParameters.ini"));
            saveoption.Yin_Original = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yin_Original", "OCTParameters.ini"));
            saveoption.Yin_FilterNoise = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yin_FilterNoise", "OCTParameters.ini"));
            saveoption.Yin_Threshold = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yin_Threshold", "OCTParameters.ini"));
            saveoption.Yin_CalcOutsideBlock = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yin_CalcOutsideBlock", "OCTParameters.ini"));
            saveoption.Yin_Correction = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yin_Correction", "OCTParameters.ini"));
            saveoption.Yang_Original = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yang_Original", "OCTParameters.ini"));
            saveoption.Yang_FilterNoise = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yang_FilterNoise", "OCTParameters.ini"));
            saveoption.Yang_Threshold = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yang_Threshold", "OCTParameters.ini"));
            saveoption.Yang_CalcOutsideBlock = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yang_CalcOutsideBlock", "OCTParameters.ini"));
            saveoption.Yang_Correction = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Yang_Correction", "OCTParameters.ini"));
            saveoption.Fullness_Threshold = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "Fullness_Threshold", "OCTParameters.ini"));
            saveoption.MakeTemplates = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "MakeTemplates", "OCTParameters.ini"));
            saveoption.IsValveEnabled = Convert.ToBoolean(MainPage.ReadIniFile("SaveOptions", "IsValveEnabled", "OCTParameters.ini"));
        }
        public void Save(SaveOption saveoption)
        {
            this.saveoption.Roi_CropPaper = saveoption.Roi_CropPaper;
            this.saveoption.Roi_Rotate = saveoption.Roi_Rotate;
            this.saveoption.Roi_Crop = saveoption.Roi_Crop;
            this.saveoption.Roi_AreaCrop = saveoption.Roi_AreaCrop;
            this.saveoption.MeshP_Crop = saveoption.MeshP_Crop;
            this.saveoption.Meshp_Threshold = saveoption.Meshp_Threshold;
            this.saveoption.Yin_Original = saveoption.Yin_Original;
            this.saveoption.Yin_FilterNoise = saveoption.Yin_FilterNoise;
            this.saveoption.Yin_Threshold = saveoption.Yin_Threshold;
            this.saveoption.Yin_CalcOutsideBlock = saveoption.Yin_CalcOutsideBlock;
            this.saveoption.Yin_Correction = saveoption.Yin_Correction;
            this.saveoption.Yang_Original = saveoption.Yang_Original;
            this.saveoption.Yang_FilterNoise = saveoption.Yang_FilterNoise;
            this.saveoption.Yang_Threshold = saveoption.Yang_Threshold;
            this.saveoption.Yang_CalcOutsideBlock = saveoption.Yang_CalcOutsideBlock;
            this.saveoption.Yang_Correction = saveoption.Yang_Correction;
            this.saveoption.Fullness_Threshold = saveoption.Fullness_Threshold;
            this.saveoption.MakeTemplates = saveoption.MakeTemplates;
            this.saveoption.IsValveEnabled = saveoption.IsValveEnabled;
            MainPage.IniWriteValue("SaveOptions", "Roi_CropPaper", saveoption.Roi_CropPaper.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Roi_Rotate", saveoption.Roi_Rotate.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Roi_Crop", saveoption.Roi_Crop.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Roi_AreaCrop", saveoption.Roi_AreaCrop.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "MeshP_Crop", saveoption.MeshP_Crop.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Meshp_Threshold", saveoption.Meshp_Threshold.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yin_Original", saveoption.Yin_Original.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yin_FilterNoise", saveoption.Yin_FilterNoise.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yin_Threshold", saveoption.Yin_Threshold.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yin_CalcOutsideBlock", saveoption.Yin_CalcOutsideBlock.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yin_Correction", saveoption.Yin_Correction.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yang_Original", saveoption.Yang_Original.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yang_FilterNoise", saveoption.Yang_FilterNoise.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yang_Threshold", saveoption.Yang_Threshold.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yang_CalcOutsideBlock", saveoption.Yang_CalcOutsideBlock.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Yang_Correction", saveoption.Yang_Correction.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "Fullness_Threshold", saveoption.Fullness_Threshold.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "MakeTemplates", saveoption.MakeTemplates.ToString().ToLower(), "OCTParameters.ini");
            MainPage.IniWriteValue("SaveOptions", "IsValveEnabled", saveoption.IsValveEnabled.ToString().ToLower(), "OCTParameters.ini");

        }
    }
}
