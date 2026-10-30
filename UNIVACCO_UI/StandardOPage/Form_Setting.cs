using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
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
        private Button button_OpenDebugImages;

        public Setting_O_Window(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            InitializeComponent();
            ConfigureResponsiveLayout();
            InitializeDebugImageButton();
            MainPage = _MainPage;
            StandardOPage = _StandardOPage;
            ParameterChangeOPage = new UserControl_ParameterChange(MainPage, StandardOPage);    //參數頁面
            ThresholdChangeOPage = new UserControl_Rules(MainPage, StandardOPage);    //規則頁面
            TempStandardChangeOPage = new UserControl_TempStandard(MainPage, StandardOPage);    //臨時標準頁面
            StandardOSettingPage = new UserControl_SaveOptions(MainPage, StandardOPage);

            // 工程視窗改為可放大的寬版配置。各功能頁在主 Panel 內自動延展。
            ParameterChangeOPage.Dock = DockStyle.Fill;
            ThresholdChangeOPage.Dock = DockStyle.Fill;
            TempStandardChangeOPage.Dock = DockStyle.Fill;
            StandardOSettingPage.Dock = DockStyle.Fill;

            panel_MainPage.Controls.Clear();
            panel_MainPage.Controls.Add(ParameterChangeOPage);
        }

        /// <summary>
        /// 工程設定視窗依目前螢幕工作區調整大小。
        /// 不再為舊的內嵌預覽保留大面積空白；主內容區會跟著視窗伸縮。
        /// </summary>
        private void ConfigureResponsiveLayout()
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;

            int targetWidth = Math.Min(1000, Math.Max(760, workingArea.Width - 40));
            int targetHeight = Math.Min(850, Math.Max(620, workingArea.Height - 40));

            ClientSize = new Size(targetWidth, targetHeight);
            // 工程設定視窗固定在目前螢幕工作區左側（保留工作列空間）。
            StartPosition = FormStartPosition.Manual;
            int sideBySideWidth = Math.Max(760, workingArea.Width / 2);
            if (ClientSize.Width > sideBySideWidth)
                ClientSize = new Size(sideBySideWidth, ClientSize.Height);
            Location = new Point(workingArea.Left, workingArea.Top);
            MaximizeBox = true;

            // 上方工具列維持原高度，主內容填滿剩餘空間。
            pictureBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox3.Width = ClientSize.Width;

            button_Save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_Save.Left = ClientSize.Width - button_Save.Width - 13;

            panel_MainPage.Location = new Point(0, 105);
            panel_MainPage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel_MainPage.Size = new Size(ClientSize.Width, Math.Max(100, ClientSize.Height - 105));

            Resize += delegate
            {
                pictureBox3.Width = ClientSize.Width;
                button_Save.Left = ClientSize.Width - button_Save.Width - 13;
                PositionDebugImageButton();
                panel_MainPage.Size = new Size(ClientSize.Width, Math.Max(100, ClientSize.Height - 105));
            };
        }

        // 開啟現有的 Debug 圖片總資料夾；不變更任何圖片輸出路徑。
        private void InitializeDebugImageButton()
        {
            button_OpenDebugImages = new Button();
            button_OpenDebugImages.Name = "button_OpenDebugImages";
            button_OpenDebugImages.Size = new Size(84, 84);
            button_OpenDebugImages.BackColor = Color.AntiqueWhite;
            button_OpenDebugImages.FlatStyle = FlatStyle.Flat;
            button_OpenDebugImages.FlatAppearance.BorderSize = 0;
            button_OpenDebugImages.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button_OpenDebugImages.Cursor = Cursors.Hand;
            button_OpenDebugImages.Text = string.Empty;
            button_OpenDebugImages.Font = new Font("Microsoft JhengHei UI", 12F, FontStyle.Bold);
            button_OpenDebugImages.ImageAlign = ContentAlignment.MiddleCenter;
            button_OpenDebugImages.Image = CreateFolderIcon();
            button_OpenDebugImages.Click += button_OpenDebugImages_Click;
            ToolTip tip = new ToolTip();
            tip.SetToolTip(button_OpenDebugImages, "開啟檢測 Debug 圖片總資料夾");
            Controls.Add(button_OpenDebugImages);
            button_OpenDebugImages.BringToFront();
            PositionDebugImageButton();
        }

        private void PositionDebugImageButton()
        {
            if (button_OpenDebugImages == null) return;
            // 與儲存按鈕邊緣保留 40 像素，避免誤觸。
            button_OpenDebugImages.Left = button_Save.Left - 40 - button_OpenDebugImages.Width;
            button_OpenDebugImages.Top = button_Save.Top;
        }

        private static Bitmap CreateFolderIcon()
        {
            Bitmap icon = new Bitmap(58, 47);
            using (Graphics g = Graphics.FromImage(icon))
            using (Pen outline = new Pen(Color.Black, 3.0f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(250, 212, 100)))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Point[] folder = { new Point(5, 12), new Point(21, 12), new Point(26, 17),
                    new Point(52, 17), new Point(52, 41), new Point(5, 41) };
                g.FillPolygon(fill, folder);
                g.DrawPolygon(outline, folder);
                g.DrawLine(outline, 5, 12, 5, 8);
                g.DrawLine(outline, 5, 8, 21, 8);
                g.DrawLine(outline, 21, 8, 26, 13);
            }
            return icon;
        }

        private void button_OpenDebugImages_Click(object sender, EventArgs e)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Debug");
            if (!Directory.Exists(folder))
            {
                MessageBox.Show(this,
                    "尚未找到 Debug 圖片資料夾。\n請先完成一次有啟用 Debug 圖片輸出的檢測。\n\n" + folder,
                    "尚無圖片", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "\"" + folder + "\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "開啟圖片資料夾失敗：" + ex.Message,
                    "開啟失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
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
                OCT_ParameterMeshP wc_meshp_Parameter = new OCT_ParameterMeshP()
                {
                    Name = "WC",
                    Area10 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_10.Text),
                    Area20 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_20.Text),
                    Area30 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_30.Text),
                    Area40 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_40.Text),
                    Area60 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_60.Text),
                    Area70 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_70.Text),
                    Area80 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_80.Text),
                    Area90 = Convert.ToDouble(ParameterChangeOPage.textBox_WC_MeshP_90.Text)
                };
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
                OCT_ParameterMeshP dc_meshp_Parameter = new OCT_ParameterMeshP()
                {
                    Name = "DC",
                    Area10 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_10.Text),
                    Area20 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_20.Text),
                    Area30 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_30.Text),
                    Area40 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_40.Text),
                    Area60 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_60.Text),
                    Area70 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_70.Text),
                    Area80 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_80.Text),
                    Area90 = Convert.ToDouble(ParameterChangeOPage.textBox_DC_MeshP_90.Text)
                };
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
                    AutoMeshThresholdEnabled = StandardOPage.AutoMeshThresholdEnabled
                };

                StandardOPage.WC_Params.Save(wc_meshp_Parameter, wc_yin_Parameter, wc_yang_Parameter, wc_fullness_Parameter, wc_breakarea_Parameter);
                StandardOPage.DC_Params.Save(dc_meshp_Parameter, dc_yin_Parameter, dc_yang_Parameter, dc_fullness_Parameter, dc_breakarea_Parameter);

                StandardOPage.OCT_Rules.Save(meshp_rules, yin_rules, yang_rules, fullness_rules);
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
