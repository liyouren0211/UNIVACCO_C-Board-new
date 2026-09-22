using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UNIVACCO_UI;

namespace StandardOPage
{
    public class OCT_Rules
    {
        UNIVACCO MainPage;
        //網點區參數

        public OCT_Rule meshp_rule = new OCT_Rule();
        public OCT_Rule yin_rule = new OCT_Rule();
        public OCT_Rule yang_rule = new OCT_Rule();
        public OCT_Rule fullness_rule = new OCT_Rule();
        // 初始化並從 INI 讀取
        public OCT_Rules(UNIVACCO _MainPage)
        {
            MainPage = _MainPage;
            Reload();
        }
        // 將 0~100 轉為 0~255
        public void Reload()
        {
            // 陰版參數
            meshp_rule.Block10 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block10", "OCTParameters.ini"));
            meshp_rule.Block15 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block15", "OCTParameters.ini"));
            meshp_rule.Block20 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block20", "OCTParameters.ini"));
            meshp_rule.Block25 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block25", "OCTParameters.ini"));
            meshp_rule.Block30 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block30", "OCTParameters.ini"));
            meshp_rule.Block35 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block35", "OCTParameters.ini"));
            meshp_rule.Block40 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block40", "OCTParameters.ini"));
            meshp_rule.Block45 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block45", "OCTParameters.ini"));
            meshp_rule.Block50 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Block50", "OCTParameters.ini"));
            meshp_rule.Defect10 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect10", "OCTParameters.ini"));
            meshp_rule.Defect15 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect15", "OCTParameters.ini"));
            meshp_rule.Defect20 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect20", "OCTParameters.ini"));
            meshp_rule.Defect25 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect25", "OCTParameters.ini"));
            meshp_rule.Defect30 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect30", "OCTParameters.ini"));
            meshp_rule.Defect35 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect35", "OCTParameters.ini"));
            meshp_rule.Defect40 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect40", "OCTParameters.ini"));
            meshp_rule.Defect45 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect45", "OCTParameters.ini"));
            meshp_rule.Defect50 = Convert.ToDouble(MainPage.ReadIniFile($"MeshP_Rule", "Defect50", "OCTParameters.ini"));

            yin_rule.Block10 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block10", "OCTParameters.ini"));
            yin_rule.Block15 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block15", "OCTParameters.ini"));
            yin_rule.Block20 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block20", "OCTParameters.ini"));
            yin_rule.Block25 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block25", "OCTParameters.ini"));
            yin_rule.Block30 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block30", "OCTParameters.ini"));
            yin_rule.Block35 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block35", "OCTParameters.ini"));
            yin_rule.Block40 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block40", "OCTParameters.ini"));
            yin_rule.Block45 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block45", "OCTParameters.ini"));
            yin_rule.Block50 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Block50", "OCTParameters.ini"));
            yin_rule.Defect10 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect10", "OCTParameters.ini"));
            yin_rule.Defect15 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect15", "OCTParameters.ini"));
            yin_rule.Defect20 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect20", "OCTParameters.ini"));
            yin_rule.Defect25 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect25", "OCTParameters.ini"));
            yin_rule.Defect30 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect30", "OCTParameters.ini"));
            yin_rule.Defect35 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect35", "OCTParameters.ini"));
            yin_rule.Defect40 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect40", "OCTParameters.ini"));
            yin_rule.Defect45 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect45", "OCTParameters.ini"));
            yin_rule.Defect50 = Convert.ToDouble(MainPage.ReadIniFile($"Yin_Rule", "Defect50", "OCTParameters.ini"));

            yang_rule.Block10 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block10", "OCTParameters.ini"));
            yang_rule.Block15 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block15", "OCTParameters.ini"));
            yang_rule.Block20 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block20", "OCTParameters.ini"));
            yang_rule.Block25 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block25", "OCTParameters.ini"));
            yang_rule.Block30 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block30", "OCTParameters.ini"));
            yang_rule.Block35 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block35", "OCTParameters.ini"));
            yang_rule.Block40 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block40", "OCTParameters.ini"));
            yang_rule.Block45 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block45", "OCTParameters.ini"));
            yang_rule.Block50 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Block50", "OCTParameters.ini"));
            yang_rule.Defect10 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect10", "OCTParameters.ini"));
            yang_rule.Defect15 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect15", "OCTParameters.ini"));
            yang_rule.Defect20 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect20", "OCTParameters.ini"));
            yang_rule.Defect25 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect25", "OCTParameters.ini"));
            yang_rule.Defect30 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect30", "OCTParameters.ini"));
            yang_rule.Defect35 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect35", "OCTParameters.ini"));
            yang_rule.Defect40 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect40", "OCTParameters.ini"));
            yang_rule.Defect45 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect45", "OCTParameters.ini"));
            yang_rule.Defect50 = Convert.ToDouble(MainPage.ReadIniFile($"Yang_Rule", "Defect50", "OCTParameters.ini"));

            fullness_rule.Defect10 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect10", "OCTParameters.ini"));
            fullness_rule.Defect15 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect15", "OCTParameters.ini"));
            fullness_rule.Defect20 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect20", "OCTParameters.ini"));
            fullness_rule.Defect25 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect25", "OCTParameters.ini"));
            fullness_rule.Defect30 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect30", "OCTParameters.ini"));
            fullness_rule.Defect35 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect35", "OCTParameters.ini"));
            fullness_rule.Defect40 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect40", "OCTParameters.ini"));
            fullness_rule.Defect45 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect45", "OCTParameters.ini"));
            fullness_rule.Defect50 = Convert.ToDouble(MainPage.ReadIniFile($"Fullness_Rule", "Defect50", "OCTParameters.ini"));

        }
        public void Save(OCT_Rule _meshp_rule, OCT_Rule _yin_rule, OCT_Rule _yang_rule, OCT_Rule _fullness_rule)
        {
            meshp_rule.Block10  = _meshp_rule.Block10;
            meshp_rule.Block15  = _meshp_rule.Block15;
            meshp_rule.Block20  = _meshp_rule.Block20;
            meshp_rule.Block25  = _meshp_rule.Block25;
            meshp_rule.Block30  = _meshp_rule.Block30;
            meshp_rule.Block35  = _meshp_rule.Block35;
            meshp_rule.Block40  = _meshp_rule.Block40;
            meshp_rule.Block45  = _meshp_rule.Block45;
            meshp_rule.Block50  = _meshp_rule.Block50;
            meshp_rule.Defect10 = _meshp_rule.Defect10;
            meshp_rule.Defect15 = _meshp_rule.Defect15;
            meshp_rule.Defect20 = _meshp_rule.Defect20;
            meshp_rule.Defect25 = _meshp_rule.Defect25;
            meshp_rule.Defect30 = _meshp_rule.Defect30;
            meshp_rule.Defect35 = _meshp_rule.Defect35;
            meshp_rule.Defect40 = _meshp_rule.Defect40;
            meshp_rule.Defect45 = _meshp_rule.Defect45;
            meshp_rule.Defect50 = _meshp_rule.Defect50;

            yin_rule.Block10 =  _yin_rule.Block10;
            yin_rule.Block15 =  _yin_rule.Block15;
            yin_rule.Block20 =  _yin_rule.Block20;
            yin_rule.Block25 =  _yin_rule.Block25;
            yin_rule.Block30 =  _yin_rule.Block30;
            yin_rule.Block35 =  _yin_rule.Block35;
            yin_rule.Block40 =  _yin_rule.Block40;
            yin_rule.Block45 =  _yin_rule.Block45;
            yin_rule.Block50 =  _yin_rule.Block50;
            yin_rule.Defect10 = _yin_rule.Defect10;
            yin_rule.Defect15 = _yin_rule.Defect15;
            yin_rule.Defect20 = _yin_rule.Defect20;
            yin_rule.Defect25 = _yin_rule.Defect25;
            yin_rule.Defect30 = _yin_rule.Defect30;
            yin_rule.Defect35 = _yin_rule.Defect35;
            yin_rule.Defect40 = _yin_rule.Defect40;
            yin_rule.Defect45 = _yin_rule.Defect45;
            yin_rule.Defect50 = _yin_rule.Defect50;

            yang_rule.Block10  = _yang_rule.Block10;
            yang_rule.Block15  = _yang_rule.Block15;
            yang_rule.Block20  = _yang_rule.Block20;
            yang_rule.Block25  = _yang_rule.Block25;
            yang_rule.Block30  = _yang_rule.Block30;
            yang_rule.Block35  = _yang_rule.Block35;
            yang_rule.Block40  = _yang_rule.Block40;
            yang_rule.Block45  = _yang_rule.Block45;
            yang_rule.Block50  = _yang_rule.Block50;

            yang_rule.Defect10 = _yang_rule.Defect10;
            yang_rule.Defect15 = _yang_rule.Defect15;
            yang_rule.Defect20 = _yang_rule.Defect20;
            yang_rule.Defect25 = _yang_rule.Defect25;
            yang_rule.Defect30 = _yang_rule.Defect30;
            yang_rule.Defect35 = _yang_rule.Defect35;
            yang_rule.Defect40 = _yang_rule.Defect40;
            yang_rule.Defect45 = _yang_rule.Defect45;
            yang_rule.Defect50 = _yang_rule.Defect50;

            fullness_rule.Defect10 = _fullness_rule.Defect10;
            fullness_rule.Defect15 = _fullness_rule.Defect15;
            fullness_rule.Defect20 = _fullness_rule.Defect20;
            fullness_rule.Defect25 = _fullness_rule.Defect25;
            fullness_rule.Defect30 = _fullness_rule.Defect30;
            fullness_rule.Defect35 = _fullness_rule.Defect35;
            fullness_rule.Defect40 = _fullness_rule.Defect40;
            fullness_rule.Defect45 = _fullness_rule.Defect45;
            fullness_rule.Defect50 = _fullness_rule.Defect50;

            MainPage.IniWriteValue("OCT_Rule", "Block10" , (string)meshp_rule.Block10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block15" , (string)meshp_rule.Block15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block20" , (string)meshp_rule.Block20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block25" , (string)meshp_rule.Block25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block30" , (string)meshp_rule.Block30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block35" , (string)meshp_rule.Block35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block40", (string)meshp_rule.Block40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block45", (string)meshp_rule.Block45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block50", (string)meshp_rule.Block50.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect10", (string)meshp_rule.Defect10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect15", (string)meshp_rule.Defect15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect20", (string)meshp_rule.Defect20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect25", (string)meshp_rule.Defect25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect30", (string)meshp_rule.Defect30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect35", (string)meshp_rule.Defect35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect40", (string)meshp_rule.Defect40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect45", (string)meshp_rule.Defect45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect50", (string)meshp_rule.Defect50.ToString(), "OCTParameters.ini");

            MainPage.IniWriteValue("OCT_Rule", "Block10", (string)yin_rule.Block10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block15", (string)yin_rule.Block15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block20", (string)yin_rule.Block20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block25", (string)yin_rule.Block25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block30", (string)yin_rule.Block30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block35", (string)yin_rule.Block35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block40", (string)yin_rule.Block40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block45", (string)yin_rule.Block45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block50", (string)yin_rule.Block50.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect10", (string)yin_rule.Defect10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect15", (string)yin_rule.Defect15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect20", (string)yin_rule.Defect20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect25", (string)yin_rule.Defect25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect30", (string)yin_rule.Defect30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect35", (string)yin_rule.Defect35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect40", (string)yin_rule.Defect40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect45", (string)yin_rule.Defect45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect50", (string)yin_rule.Defect50.ToString(), "OCTParameters.ini");

            MainPage.IniWriteValue("OCT_Rule", "Block10", yang_rule.Block10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block15", yang_rule.Block15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block20", yang_rule.Block20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block25", yang_rule.Block25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block30", yang_rule.Block30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block35", yang_rule.Block35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block40", yang_rule.Block40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block45", yang_rule.Block45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Block50", yang_rule.Block50.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect10", (string)yang_rule.Defect10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect15", (string)yang_rule.Defect15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect20", (string)yang_rule.Defect20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect25", (string)yang_rule.Defect25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect30", (string)yang_rule.Defect30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect35", (string)yang_rule.Defect35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect40", (string)yang_rule.Defect40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect45", (string)yang_rule.Defect45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect50", (string)yang_rule.Defect50.ToString(), "OCTParameters.ini");

            MainPage.IniWriteValue("OCT_Rule", "Defect10", (string)fullness_rule.Defect10.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect15", (string)fullness_rule.Defect15.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect20", (string)fullness_rule.Defect20.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect25", (string)fullness_rule.Defect25.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect30", (string)fullness_rule.Defect30.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect35", (string)fullness_rule.Defect35.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect40", (string)fullness_rule.Defect40.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect45", (string)fullness_rule.Defect45.ToString(), "OCTParameters.ini");
            MainPage.IniWriteValue("OCT_Rule", "Defect50", (string)fullness_rule.Defect50.ToString(), "OCTParameters.ini");
        }
    }
}
