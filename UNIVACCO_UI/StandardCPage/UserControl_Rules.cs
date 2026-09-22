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
    public partial class UserControl_Rules : UserControl
    {
        UNIVACCO MainPage;
        UserControl_Main StandardOPage;
        public UserControl_Rules(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            InitializeComponent();
            MainPage = _MainPage;
            StandardOPage = _StandardOPage;
            textBox_fullness_defect10.TextChanged += textBoxTop_TextChanged;
            textBox_meshp_block10.TextChanged += textBoxTop_TextChanged;
            textBox_meshp_defect10.TextChanged += textBoxTop_TextChanged;
            textBox_yang_block10.TextChanged += textBoxTop_TextChanged;
            textBox_yang_defect10.TextChanged += textBoxTop_TextChanged;
            textBox_yin_block10.TextChanged += textBoxTop_TextChanged;
            textBox_yin_defect10.TextChanged += textBoxTop_TextChanged;
            ReloadUI();
        }
        private void ReloadUI()
        {
            //網點區參數載入
            textBox_meshp_block10.Text = StandardOPage.OCT_Rules.meshp_rule.Block10.ToString();
            textBox_meshp_block15.Text = StandardOPage.OCT_Rules.meshp_rule.Block15.ToString();
            textBox_meshp_block20.Text = StandardOPage.OCT_Rules.meshp_rule.Block20.ToString();
            textBox_meshp_block25.Text = StandardOPage.OCT_Rules.meshp_rule.Block25.ToString();
            textBox_meshp_block30.Text = StandardOPage.OCT_Rules.meshp_rule.Block30.ToString();
            textBox_meshp_block35.Text = StandardOPage.OCT_Rules.meshp_rule.Block35.ToString();
            textBox_meshp_block40.Text = StandardOPage.OCT_Rules.meshp_rule.Block40.ToString();
            textBox_meshp_block45.Text = StandardOPage.OCT_Rules.meshp_rule.Block45.ToString();
            textBox_meshp_block50.Text = StandardOPage.OCT_Rules.meshp_rule.Block50.ToString();
            textBox_meshp_defect10.Text = StandardOPage.OCT_Rules.meshp_rule.Defect10.ToString();
            textBox_meshp_defect15.Text = StandardOPage.OCT_Rules.meshp_rule.Defect15.ToString();
            textBox_meshp_defect20.Text = StandardOPage.OCT_Rules.meshp_rule.Defect20.ToString();
            textBox_meshp_defect25.Text = StandardOPage.OCT_Rules.meshp_rule.Defect25.ToString();
            textBox_meshp_defect30.Text = StandardOPage.OCT_Rules.meshp_rule.Defect30.ToString();
            textBox_meshp_defect35.Text = StandardOPage.OCT_Rules.meshp_rule.Defect35.ToString();
            textBox_meshp_defect40.Text = StandardOPage.OCT_Rules.meshp_rule.Defect40.ToString();
            textBox_meshp_defect45.Text = StandardOPage.OCT_Rules.meshp_rule.Defect45.ToString();
            textBox_meshp_defect50.Text = StandardOPage.OCT_Rules.meshp_rule.Defect50.ToString();

            //陽版區參數載入
            textBox_yang_block10.Text = StandardOPage.OCT_Rules.yang_rule.Block10.ToString();
            textBox_yang_block15.Text = StandardOPage.OCT_Rules.yang_rule.Block15.ToString();
            textBox_yang_block20.Text = StandardOPage.OCT_Rules.yang_rule.Block20.ToString();
            textBox_yang_block25.Text = StandardOPage.OCT_Rules.yang_rule.Block25.ToString();
            textBox_yang_block30.Text = StandardOPage.OCT_Rules.yang_rule.Block30.ToString();
            textBox_yang_block35.Text = StandardOPage.OCT_Rules.yang_rule.Block35.ToString();
            textBox_yang_block40.Text = StandardOPage.OCT_Rules.yang_rule.Block40.ToString();
            textBox_yang_block45.Text = StandardOPage.OCT_Rules.yang_rule.Block45.ToString();
            textBox_yang_block50.Text = StandardOPage.OCT_Rules.yang_rule.Block50.ToString();
            textBox_yang_defect10.Text = StandardOPage.OCT_Rules.yang_rule.Defect10.ToString();
            textBox_yang_defect15.Text = StandardOPage.OCT_Rules.yang_rule.Defect15.ToString();
            textBox_yang_defect20.Text = StandardOPage.OCT_Rules.yang_rule.Defect20.ToString();
            textBox_yang_defect25.Text = StandardOPage.OCT_Rules.yang_rule.Defect25.ToString();
            textBox_yang_defect30.Text = StandardOPage.OCT_Rules.yang_rule.Defect30.ToString();
            textBox_yang_defect35.Text = StandardOPage.OCT_Rules.yang_rule.Defect35.ToString();
            textBox_yang_defect40.Text = StandardOPage.OCT_Rules.yang_rule.Defect40.ToString();
            textBox_yang_defect45.Text = StandardOPage.OCT_Rules.yang_rule.Defect45.ToString();
            textBox_yang_defect50.Text = StandardOPage.OCT_Rules.yang_rule.Defect50.ToString();
            
            //陰版區參數載入
            textBox_yin_block10.Text = StandardOPage.OCT_Rules.yin_rule.Block10.ToString();
            textBox_yin_block15.Text = StandardOPage.OCT_Rules.yin_rule.Block15.ToString();
            textBox_yin_block20.Text = StandardOPage.OCT_Rules.yin_rule.Block20.ToString();
            textBox_yin_block25.Text = StandardOPage.OCT_Rules.yin_rule.Block25.ToString();
            textBox_yin_block30.Text = StandardOPage.OCT_Rules.yin_rule.Block30.ToString();
            textBox_yin_block35.Text = StandardOPage.OCT_Rules.yin_rule.Block35.ToString();
            textBox_yin_block40.Text = StandardOPage.OCT_Rules.yin_rule.Block40.ToString();
            textBox_yin_block45.Text = StandardOPage.OCT_Rules.yin_rule.Block45.ToString();
            textBox_yin_block50.Text = StandardOPage.OCT_Rules.yin_rule.Block50.ToString();
            textBox_yin_defect10.Text = StandardOPage.OCT_Rules.yin_rule.Defect10.ToString();
            textBox_yin_defect15.Text = StandardOPage.OCT_Rules.yin_rule.Defect15.ToString();
            textBox_yin_defect20.Text = StandardOPage.OCT_Rules.yin_rule.Defect20.ToString();
            textBox_yin_defect25.Text = StandardOPage.OCT_Rules.yin_rule.Defect25.ToString();
            textBox_yin_defect30.Text = StandardOPage.OCT_Rules.yin_rule.Defect30.ToString();
            textBox_yin_defect35.Text = StandardOPage.OCT_Rules.yin_rule.Defect35.ToString();
            textBox_yin_defect40.Text = StandardOPage.OCT_Rules.yin_rule.Defect40.ToString();
            textBox_yin_defect45.Text = StandardOPage.OCT_Rules.yin_rule.Defect45.ToString();
            textBox_yin_defect50.Text = StandardOPage.OCT_Rules.yin_rule.Defect50.ToString();

            //陰版區參數載入
            textBox_fullness_defect10.Text = StandardOPage.OCT_Rules.fullness_rule.Defect10.ToString();
            textBox_fullness_defect15.Text = StandardOPage.OCT_Rules.fullness_rule.Defect15.ToString();
            textBox_fullness_defect20.Text = StandardOPage.OCT_Rules.fullness_rule.Defect20.ToString();
            textBox_fullness_defect25.Text = StandardOPage.OCT_Rules.fullness_rule.Defect25.ToString();
            textBox_fullness_defect30.Text = StandardOPage.OCT_Rules.fullness_rule.Defect30.ToString();
            textBox_fullness_defect35.Text = StandardOPage.OCT_Rules.fullness_rule.Defect35.ToString();
            textBox_fullness_defect40.Text = StandardOPage.OCT_Rules.fullness_rule.Defect40.ToString();
            textBox_fullness_defect45.Text = StandardOPage.OCT_Rules.fullness_rule.Defect45.ToString();
            textBox_fullness_defect50.Text = StandardOPage.OCT_Rules.fullness_rule.Defect50.ToString();
        }
        private void textBoxTop_TextChanged(object sender, EventArgs e)
        {
            textBox_fullness_defect05.Text = textBox_fullness_defect10.Text;
            textBox_meshp_block05.Text  = textBox_meshp_block10.Text;
            textBox_meshp_defect05.Text = textBox_meshp_defect10.Text;
            textBox_yang_block05.Text = textBox_yang_block10.Text;
            textBox_yang_defect05.Text = textBox_yang_defect10.Text;
            textBox_yin_block05.Text = textBox_yin_block10.Text;
            textBox_yin_defect05.Text = textBox_yin_defect10.Text;
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
