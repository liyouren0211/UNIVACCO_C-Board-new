using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using StandardOPage;

namespace UNIVACCO_UI
{
    public partial class Setting_O_Window : Form
    {
        UNIVACCO MainPage;
        UserControl_ParameterChange ParameterChangeOPage;
        UserControl_Rules ThresholdChangeOPage;
        UserControl_TempStandard TempStandardChangeOPage;
        UserControl_Main StandardOPage;
        UserControl_SaveOptions StandardOSettingPage;

        public Setting_O_Window(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            InitializeComponent();
            MainPage = _MainPage;
            StandardOPage = _StandardOPage;
            ParameterChangeOPage = new UserControl_ParameterChange(MainPage, StandardOPage);    //參數頁面
            ThresholdChangeOPage = new UserControl_Rules(MainPage, StandardOPage);    //規則頁面
            TempStandardChangeOPage = new UserControl_TempStandard(MainPage,StandardOPage);    //臨時標準頁面
            StandardOSettingPage = new UserControl_SaveOptions(MainPage, StandardOPage);
            panel_MainPage.Controls.Clear();
            panel_MainPage.Controls.Add(ParameterChangeOPage);
        }
        private void button_ThresholdChangePage_Click(object sender, EventArgs e)
        {
            panel_MainPage.Controls.Clear();
            panel_MainPage.Controls.Add(ThresholdChangeOPage);
        }
        private void button_ParameterChangePage_Click(object sender, EventArgs e)
        {
            panel_MainPage.Controls.Clear();
            panel_MainPage.Controls.Add(ParameterChangeOPage);
        }
        private void button_Save_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要儲存？", "資訊", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                #region 白卡參數
                Font_Parameter wc_yin_Parameter = new Font_Parameter()
                {
                    _3pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_3pt.Text),
                    _4pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_4pt.Text),
                    _5pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_5pt.Text),
                    _6pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_6pt.Text),
                    _7pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_7pt.Text),
                    _8pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_8pt.Text),
                    _9pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_9pt.Text),
                    _10pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_10pt.Text),
                    _11pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_11pt.Text),
                    _12pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yin_12pt.Text),
                };
                Font_Parameter wc_yang_Parameter = new Font_Parameter()
                {
                    _3pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_3pt.Text),
                    _4pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_4pt.Text),
                    _5pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_5pt.Text),
                    _6pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_6pt.Text),
                    _7pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_7pt.Text),
                    _8pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_8pt.Text),
                    _9pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_9pt.Text),
                    _10pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_10pt.Text),
                    _11pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_11pt.Text),
                    _12pt = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Yang_12pt.Text),
                };
                Fullness_Parameter wc_fullness_Parameter = new Fullness_Parameter()
                {
                    Threshold = Convert.ToDouble(ParameterChangeOPage.textBox_WC_Fullness.Text),
                };

                BreakArea_Parameter wc_breakarea_Parameter = new BreakArea_Parameter()
                {
                    Threshold_01 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_BreakArea_01_Threshold.Text),
                    Threshold_02 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_BreakArea_02_Threshold.Text),
                    Threshold_03 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_BreakArea_03_Threshold.Text),
                    Threshold_Step1 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_BreakArea_Step1_Threshold.Text)
                };

                #endregion
                #region 雙銅參數
                Font_Parameter dc_yin_Parameter = new Font_Parameter()
                {
                    _3pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_3pt.Text),
                    _4pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_4pt.Text),
                    _5pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_5pt.Text),
                    _6pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_6pt.Text),
                    _7pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_7pt.Text),
                    _8pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_8pt.Text),
                    _9pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_9pt.Text),
                    _10pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_10pt.Text),
                    _11pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_11pt.Text),
                    _12pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yin_12pt.Text),
                };
                Font_Parameter dc_yang_Parameter = new Font_Parameter()
                {
                    _3pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_3pt.Text),
                    _4pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_4pt.Text),
                    _5pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_5pt.Text),
                    _6pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_6pt.Text),
                    _7pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_7pt.Text),
                    _8pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_8pt.Text),
                    _9pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_9pt.Text),
                    _10pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_10pt.Text),
                    _11pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_11pt.Text),
                    _12pt = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Yang_12pt.Text),
                };
                Fullness_Parameter dc_fullness_Parameter = new Fullness_Parameter()
                {
                    Threshold = Convert.ToDouble(ParameterChangeOPage.textBox_DC_Fullness.Text),
                };

                BreakArea_Parameter dc_breakarea_Parameter = new BreakArea_Parameter()
                {
                    Threshold_01 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_BreakArea_01_Threshold.Text),
                    Threshold_02 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_BreakArea_02_Threshold.Text),
                    Threshold_03 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_BreakArea_03_Threshold.Text),
                    Threshold_Step1 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_BreakArea_Step1_Threshold.Text)
                };

                #endregion

                #region 網點區規則
                OCT_Rule meshp_rules = new OCT_Rule()
                {
                    Block10 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block10.Text),
                    Block15 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block15.Text),
                    Block20 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block20.Text),
                    Block25 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block25.Text),
                    Block30 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block30.Text),
                    Block35 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block35.Text),
                    Block40 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block40.Text),
                    Block45 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block45.Text),
                    Block50 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_block50.Text),
                    Defect10 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect10.Text),
                    Defect15 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect15.Text),
                    Defect20 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect20.Text),
                    Defect25 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect25.Text),
                    Defect30 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect30.Text),
                    Defect35 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect35.Text),
                    Defect40 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect40.Text),
                    Defect45 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect45.Text),
                    Defect50 = Convert.ToDouble(ThresholdChangeOPage.textBox_meshp_defect50.Text),
                };

                #endregion

                #region 陽版規則
                OCT_Rule yang_rules = new OCT_Rule()
                {
                    Block10 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block10.Text),
                    Block15 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block15.Text),
                    Block20 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block20.Text),
                    Block25 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block25.Text),
                    Block30 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block30.Text),
                    Block35 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block35.Text),
                    Block40 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block40.Text),
                    Block45 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block45.Text),
                    Block50 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_block50.Text),
                    Defect10 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect10.Text),
                    Defect15 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect15.Text),
                    Defect20 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect20.Text),
                    Defect25 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect25.Text),
                    Defect30 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect30.Text),
                    Defect35 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect35.Text),
                    Defect40 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect40.Text),
                    Defect45 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect45.Text),
                    Defect50 = Convert.ToDouble(ThresholdChangeOPage.textBox_yang_defect50.Text),
                };
                #endregion

                #region 陰版規則
                OCT_Rule yin_rules = new OCT_Rule()
                {
                    Block10 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block10.Text),
                    Block15 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block15.Text),
                    Block20 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block20.Text),
                    Block25 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block25.Text),
                    Block30 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block30.Text),
                    Block35 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block35.Text),
                    Block40 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block40.Text),
                    Block45 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block45.Text),
                    Block50 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_block50.Text),
                    Defect10 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect10.Text),
                    Defect15 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect15.Text),
                    Defect20 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect20.Text),
                    Defect25 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect25.Text),
                    Defect30 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect30.Text),
                    Defect35 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect35.Text),
                    Defect40 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect40.Text),
                    Defect45 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect45.Text),
                    Defect50 = Convert.ToDouble(ThresholdChangeOPage.textBox_yin_defect50.Text),
                };
                #endregion

                #region 飽滿度規則
                OCT_Rule fullness_rules = new OCT_Rule()
                {
                    Defect10 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect10.Text),
                    Defect15 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect15.Text),
                    Defect20 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect20.Text),
                    Defect25 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect25.Text),
                    Defect30 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect30.Text),
                    Defect35 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect35.Text),
                    Defect40 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect40.Text),
                    Defect45 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect45.Text),
                    Defect50 = Convert.ToDouble(ThresholdChangeOPage.textBox_fullness_defect50.Text),
                };
                #endregion

                SaveOption saveoption = new SaveOption()
                {
                    Roi_CropPaper = StandardOSettingPage.checkbox_roi_croppaper.Checked,
                    Roi_Rotate = StandardOSettingPage.checkbox_roi_rotate.Checked,
                    Roi_Crop = StandardOSettingPage.checkbox_roi_crop.Checked,
                    Roi_AreaCrop = StandardOSettingPage.checkbox_roi_areacrop.Checked,
                    MeshP_Crop = StandardOSettingPage.checkbox_meshp_crop.Checked,
                    Meshp_Threshold = StandardOSettingPage.checkbox_meshp_threshold.Checked,
                    Yin_Original = StandardOSettingPage.checkbox_yin_original.Checked,
                    Yin_FilterNoise = StandardOSettingPage.checkbox_yin_filternoise.Checked,
                    Yin_Threshold = StandardOSettingPage.checkbox_yin_threshold.Checked,
                    Yin_CalcOutsideBlock = StandardOSettingPage.checkbox_yin_calcoutsideblock.Checked,
                    Yin_Correction = StandardOSettingPage.checkbox_yin_correction.Checked,
                    Yang_Original = StandardOSettingPage.checkbox_yang_original.Checked,
                    Yang_FilterNoise = StandardOSettingPage.checkbox_yang_filternoise.Checked,
                    Yang_Threshold = StandardOSettingPage.checkbox_yang_threshold.Checked,
                    Yang_CalcOutsideBlock = StandardOSettingPage.checkbox_yang_calcoutsideblock.Checked,
                    Yang_Correction = StandardOSettingPage.checkbox_yang_correction.Checked,
                    Fullness_Threshold = StandardOSettingPage.checkbox_fullness_threshold.Checked,
                    MakeTemplates = StandardOSettingPage.checkbox_maketemplates.Checked,
                    IsValveEnabled = StandardOPage.IsValveEnabled,
                    AutoMeshThresholdEnabled =StandardOPage.AutoMeshThresholdEnabled
                };

                StandardOPage.WC_Params.Save(wc_yin_Parameter, wc_yang_Parameter, wc_fullness_Parameter, wc_breakarea_Parameter);
                StandardOPage.DC_Params.Save(dc_yin_Parameter, dc_yang_Parameter, dc_fullness_Parameter, dc_breakarea_Parameter);

                StandardOPage.OCT_Rules.Save(meshp_rules,yin_rules,yang_rules,fullness_rules);
                StandardOPage.SaveOptions.Save(saveoption);
                MessageBox.Show("參數儲存成功!", "系統資訊", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
        private void button_TempStandardChangePage_Click(object sender, EventArgs e)
        {
            panel_MainPage.Controls.Clear();
            panel_MainPage.Controls.Add(TempStandardChangeOPage);
        }
        private void button_StandardOSettingPage_Click(object sender, EventArgs e)
        {
            panel_MainPage.Controls.Clear();
            panel_MainPage.Controls.Add(StandardOSettingPage);
        }
    }
}
