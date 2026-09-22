using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UNIVACCO_UI
{
    public partial class UserControl_SaveOptions : UserControl
    {
        UserControl_Main StandardOPage;
        UNIVACCO MainPage;
        public UserControl_SaveOptions(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            InitializeComponent();
            this.MainPage = _MainPage;
            this.StandardOPage = _StandardOPage;
            ReloadUI();
        }
        private void ReloadUI()
        {
            //網點區參數載入
            checkbox_roi_croppaper.Checked = StandardOPage.SaveOptions.saveoption.Roi_CropPaper;
            checkbox_roi_rotate.Checked = StandardOPage.SaveOptions.saveoption.Roi_Rotate;
            checkbox_roi_crop.Checked = StandardOPage.SaveOptions.saveoption.Roi_Crop;
            checkbox_roi_areacrop.Checked = StandardOPage.SaveOptions.saveoption.Roi_AreaCrop;
            checkbox_meshp_crop.Checked = StandardOPage.SaveOptions.saveoption.MeshP_Crop;
            checkbox_meshp_threshold.Checked = StandardOPage.SaveOptions.saveoption.Meshp_Threshold;
            checkbox_yin_original.Checked = StandardOPage.SaveOptions.saveoption.Yin_Original;
            checkbox_yin_filternoise.Checked = StandardOPage.SaveOptions.saveoption.Yin_FilterNoise;
            checkbox_yin_threshold.Checked = StandardOPage.SaveOptions.saveoption.Yin_Threshold;
            checkbox_yin_calcoutsideblock.Checked = StandardOPage.SaveOptions.saveoption.Yin_CalcOutsideBlock;
            checkbox_yin_correction.Checked = StandardOPage.SaveOptions.saveoption.Yin_Correction;
            checkbox_yang_original.Checked = StandardOPage.SaveOptions.saveoption.Yang_Original;
            checkbox_yang_filternoise.Checked = StandardOPage.SaveOptions.saveoption.Yang_FilterNoise;
            checkbox_yang_threshold.Checked = StandardOPage.SaveOptions.saveoption.Yang_Threshold;
            checkbox_yang_calcoutsideblock.Checked = StandardOPage.SaveOptions.saveoption.Yang_CalcOutsideBlock;
            checkbox_yang_correction.Checked = StandardOPage.SaveOptions.saveoption.Yang_Correction;
            checkbox_fullness_threshold.Checked = StandardOPage.SaveOptions.saveoption.Fullness_Threshold;
            checkbox_maketemplates.Checked = StandardOPage.SaveOptions.saveoption.MakeTemplates;
            checkBox_Valve.Checked = StandardOPage.IsValveEnabled;

        }
        private void checkbox_maketemplates_checkchanged(object sender, EventArgs e)
        {
            if (checkbox_maketemplates.Checked)
            {
                if (MessageBox.Show(
                    "生成模板提示請注意:\n" +
                    "1.請將字體標準樣放入再進行分析\n" +
                    "2.生成後會自動覆蓋template資料夾內的模板\n" +
                    "3.平常分析時請保持關閉狀態"
                    , "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                {
                    checkbox_maketemplates.Checked = false;
                    return;
                }
            }
        }

        private void Open_Parameters_INI_button_Click(object sender, EventArgs e)
        {
            string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OCTParameters.ini");
            if (File.Exists(iniPath))
            {
                System.Diagnostics.Process.Start(iniPath);
            }
            else
            {
                MessageBox.Show("找不到 OCTParameters.ini 檔案！",
                                "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkBox_Valve_CheckedChanged(object sender, EventArgs e)
        {
            StandardOPage.IsValveEnabled = checkBox_Valve.Checked;
        }
    }
}
