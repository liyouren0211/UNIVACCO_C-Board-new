using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UNIVACCO_UI
{
    public partial class UserControl_ParameterChange : UserControl
    {
        UserControl_Main StandardOPage;
        UNIVACCO MainPage;
        public UserControl_ParameterChange(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            MainPage = _MainPage;
            StandardOPage = _StandardOPage;
            InitializeComponent();
            ReloadUI();
        }
        private void ReloadUI()
        {
            textBox_WC_Yin_3pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._3pt).ToString();
            textBox_WC_Yin_4pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._4pt).ToString();
            textBox_WC_Yin_5pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._5pt).ToString();
            textBox_WC_Yin_6pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._6pt).ToString();
            textBox_WC_Yin_7pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._7pt).ToString();
            textBox_WC_Yin_8pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._8pt).ToString();
            textBox_WC_Yin_9pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._9pt).ToString();
            textBox_WC_Yin_10pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._10pt).ToString();
            textBox_WC_Yin_11pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._11pt).ToString();
            textBox_WC_Yin_12pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yin_parameter._12pt).ToString();

            textBox_WC_Yang_3pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._3pt).ToString();
            textBox_WC_Yang_4pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._4pt).ToString();
            textBox_WC_Yang_5pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._5pt).ToString();
            textBox_WC_Yang_6pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._6pt).ToString();
            textBox_WC_Yang_7pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._7pt).ToString();
            textBox_WC_Yang_8pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._8pt).ToString();
            textBox_WC_Yang_9pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._9pt).ToString();
            textBox_WC_Yang_10pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._10pt).ToString();
            textBox_WC_Yang_11pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._11pt).ToString();
            textBox_WC_Yang_12pt.Text = ThresholdToPercent(StandardOPage.WC_Params.yang_parameter._12pt).ToString();

            textBox_WC_Fullness.Text = ThresholdToPercent(StandardOPage.WC_Params.fullness_parameter.Threshold).ToString();

            textBox_WC_BreakArea_01_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_01).ToString();
            textBox_WC_BreakArea_02_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_02).ToString();
            textBox_WC_BreakArea_03_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_03).ToString();
            textBox_WC_BreakArea_Step1_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_Step1).ToString();

            textBox_DC_Yin_3pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._3pt).ToString();
            textBox_DC_Yin_4pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._4pt).ToString();
            textBox_DC_Yin_5pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._5pt).ToString();
            textBox_DC_Yin_6pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._6pt).ToString();
            textBox_DC_Yin_7pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._7pt).ToString();
            textBox_DC_Yin_8pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._8pt).ToString();
            textBox_DC_Yin_9pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._9pt).ToString();
            textBox_DC_Yin_10pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._10pt).ToString();
            textBox_DC_Yin_11pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._11pt).ToString();
            textBox_DC_Yin_12pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yin_parameter._12pt).ToString();

            textBox_DC_Yang_3pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._3pt).ToString();
            textBox_DC_Yang_4pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._4pt).ToString();
            textBox_DC_Yang_5pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._5pt).ToString();
            textBox_DC_Yang_6pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._6pt).ToString();
            textBox_DC_Yang_7pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._7pt).ToString();
            textBox_DC_Yang_8pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._8pt).ToString();
            textBox_DC_Yang_9pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._9pt).ToString();
            textBox_DC_Yang_10pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._10pt).ToString();
            textBox_DC_Yang_11pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._11pt).ToString();
            textBox_DC_Yang_12pt.Text = ThresholdToPercent(StandardOPage.DC_Params.yang_parameter._12pt).ToString();

            textBox_DC_Fullness.Text = ThresholdToPercent(StandardOPage.DC_Params.fullness_parameter.Threshold).ToString();

            textBox_DC_BreakArea_01_Threshold.Text = ThresholdToPercent(StandardOPage.DC_Params.breakarea_parameter.Threshold_01).ToString();
            textBox_DC_BreakArea_02_Threshold.Text = ThresholdToPercent(StandardOPage.DC_Params.breakarea_parameter.Threshold_02).ToString();
            textBox_DC_BreakArea_03_Threshold.Text = ThresholdToPercent(StandardOPage.DC_Params.breakarea_parameter.Threshold_03).ToString();
            textBox_DC_BreakArea_Step1_Threshold.Text = ThresholdToPercent(StandardOPage.DC_Params.breakarea_parameter.Threshold_Step1).ToString();
        }


        private double ThresholdToPercent(double value)
        {
            return Math.Round(value / 255.0 * 100.0);
        }



        //textbox防呆
        private void TextBox_NumberRangeChecker(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string input = tb.Text;

            // 嘗試轉為 double
            if (double.TryParse(input, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    tb.Tag = input;  // 更新合法值到 Tag
                }
                else
                {
                    MessageBox.Show("請輸入 0 到 100 的數值（可含小數）！", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tb.Text = tb.Tag?.ToString() ?? "";  // 還原上次合法值（或空字串）
                    tb.SelectionStart = tb.Text.Length;  // 光標移到最後
                }
            }
            else
            {
                // 排除正在輸入小數點情況（例如 "12."），避免過早干預
                if (!string.IsNullOrEmpty(input) && input != "." && input != "-")
                {
                    MessageBox.Show("請輸入數字（可含小數點），勿輸入文字或非法符號！", "格式錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }

    }

}
