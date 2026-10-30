using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace UNIVACCO_UI
{
    public partial class UserControl_ParameterChange : UserControl
    {
        UserControl_Main StandardOPage;
        UNIVACCO MainPage;

        // 白卡網點區參數
        public TextBox textBox_WC_MeshP_10;
        public TextBox textBox_WC_MeshP_20;
        public TextBox textBox_WC_MeshP_30;
        public TextBox textBox_WC_MeshP_40;
        public TextBox textBox_WC_MeshP_60;
        public TextBox textBox_WC_MeshP_70;
        public TextBox textBox_WC_MeshP_80;
        public TextBox textBox_WC_MeshP_90;

        // 雙銅網點區參數
        public TextBox textBox_DC_MeshP_10;
        public TextBox textBox_DC_MeshP_20;
        public TextBox textBox_DC_MeshP_30;
        public TextBox textBox_DC_MeshP_40;
        public TextBox textBox_DC_MeshP_60;
        public TextBox textBox_DC_MeshP_70;
        public TextBox textBox_DC_MeshP_80;
        public TextBox textBox_DC_MeshP_90;

        // 獨立即時預覽視窗：工程頁只負責參數，影像預覽另外彈出。
        private Form_LivePreview livePreviewForm;
        private Button button_ShowLivePreview_WC;
        private Button button_ShowLivePreview_DC;
        private Timer previewDebounceTimer;
        private Control selectedPreviewControl;
        private int selectedMeshArea = 10;
        private bool selectedFontIsYin = true;
        private int selectedFontPt = 3;
        private PreviewTarget selectedPreviewTarget = PreviewTarget.Mesh;
        private readonly Dictionary<TextBox, int> meshAreaMap = new Dictionary<TextBox, int>();

        private enum PreviewTarget
        {
            Mesh,
            Font
        }

        public UserControl_ParameterChange(UNIVACCO _MainPage, UserControl_Main _StandardOPage)
        {
            InitializeComponent();

            MainPage = _MainPage;
            StandardOPage = _StandardOPage;

            // 原本右下角「網點區參數模式：手動 / 自動」不再使用。
            // Designer 內控制項先保留，避免大幅修改 Designer；執行時直接隱藏。
            groupBox_MeshThresholdMode.Visible = false;

            // 工程頁不再內嵌預覽，Tab 直接跟著容器縮放。
            // 畫面高度不足時由 TabPage 自動提供捲軸，避免下方參數與預覽按鈕消失。
            this.AutoScroll = false;
            tabControl1.Dock = DockStyle.Fill;
            tabPage1.AutoScroll = true;
            tabPage2.AutoScroll = true;
            tabPage1.AutoScrollMinSize = new Size(880, 680);
            tabPage2.AutoScrollMinSize = new Size(880, 680);

            BuildMeshParameterUI();
            BuildPreviewLauncher();
            WireFontPreviewEvents();

            previewDebounceTimer = new Timer();
            // Wait until typing stops, so entering 12 does not calculate 1 first.
            previewDebounceTimer.Interval = 800;
            previewDebounceTimer.Tick += delegate
            {
                previewDebounceTimer.Stop();
                RefreshLivePreview();
                RefreshMainFontScore();
                RefreshMainMeshScore();
            };

            ReloadUI();
            SelectMeshParameter(textBox_WC_MeshP_10, 10);

            this.Disposed += delegate
            {
                previewDebounceTimer?.Stop();
                previewDebounceTimer?.Dispose();
                if (livePreviewForm != null && !livePreviewForm.IsDisposed)
                    livePreviewForm.Close();
            };
        }

        private void BuildMeshParameterUI()
        {
            GroupBox wcMeshGroup = CreateMeshParameterGroup(
                "groupBox_WC_MeshP_Parameter",
                out textBox_WC_MeshP_10,
                out textBox_WC_MeshP_20,
                out textBox_WC_MeshP_30,
                out textBox_WC_MeshP_40,
                out textBox_WC_MeshP_60,
                out textBox_WC_MeshP_70,
                out textBox_WC_MeshP_80,
                out textBox_WC_MeshP_90
            );
            wcMeshGroup.Location = new Point(461, 385);
            tabPage1.Controls.Add(wcMeshGroup);

            GroupBox dcMeshGroup = CreateMeshParameterGroup(
                "groupBox_DC_MeshP_Parameter",
                out textBox_DC_MeshP_10,
                out textBox_DC_MeshP_20,
                out textBox_DC_MeshP_30,
                out textBox_DC_MeshP_40,
                out textBox_DC_MeshP_60,
                out textBox_DC_MeshP_70,
                out textBox_DC_MeshP_80,
                out textBox_DC_MeshP_90
            );
            dcMeshGroup.Location = new Point(461, 385);
            tabPage2.Controls.Add(dcMeshGroup);

            // Dynamically-created inputs need stable names for active-card live scoring.
            int[] areas = { 10, 20, 30, 40, 60, 70, 80, 90 };
            TextBox[] wc = { textBox_WC_MeshP_10, textBox_WC_MeshP_20,
                textBox_WC_MeshP_30, textBox_WC_MeshP_40, textBox_WC_MeshP_60,
                textBox_WC_MeshP_70, textBox_WC_MeshP_80, textBox_WC_MeshP_90 };
            TextBox[] dc = { textBox_DC_MeshP_10, textBox_DC_MeshP_20,
                textBox_DC_MeshP_30, textBox_DC_MeshP_40, textBox_DC_MeshP_60,
                textBox_DC_MeshP_70, textBox_DC_MeshP_80, textBox_DC_MeshP_90 };
            for (int i = 0; i < areas.Length; i++)
            {
                wc[i].Name = "textBox_WC_MeshP_" + areas[i];
                dc[i].Name = "textBox_DC_MeshP_" + areas[i];
            }
        }

        private GroupBox CreateMeshParameterGroup(
            string groupName,
            out TextBox textBox10,
            out TextBox textBox20,
            out TextBox textBox30,
            out TextBox textBox40,
            out TextBox textBox60,
            out TextBox textBox70,
            out TextBox textBox80,
            out TextBox textBox90)
        {
            GroupBox groupBox = new GroupBox();
            groupBox.Name = groupName;
            groupBox.Text = "網點區參數";
            groupBox.Font = new Font("Microsoft JhengHei UI", 16F, FontStyle.Bold);
            groupBox.Size = new Size(373, 220);
            groupBox.TabStop = false;

            textBox10 = CreateMeshParameterRow(groupBox, "10%", 18, 38);
            textBox20 = CreateMeshParameterRow(groupBox, "20%", 18, 78);
            textBox30 = CreateMeshParameterRow(groupBox, "30%", 18, 118);
            textBox40 = CreateMeshParameterRow(groupBox, "40%", 18, 158);

            textBox60 = CreateMeshParameterRow(groupBox, "60%", 190, 38);
            textBox70 = CreateMeshParameterRow(groupBox, "70%", 190, 78);
            textBox80 = CreateMeshParameterRow(groupBox, "80%", 190, 118);
            textBox90 = CreateMeshParameterRow(groupBox, "90%", 190, 158);

            return groupBox;
        }

        private TextBox CreateMeshParameterRow(GroupBox parent, string areaText, int x, int y)
        {
            Font rowFont = new Font("Microsoft JhengHei UI", 13.8F, FontStyle.Bold);

            Label areaLabel = new Label();
            areaLabel.AutoSize = true;
            areaLabel.Font = rowFont;
            areaLabel.Location = new Point(x, y + 3);
            areaLabel.Text = areaText;
            parent.Controls.Add(areaLabel);

            TextBox textBox = new TextBox();
            textBox.Font = rowFont;
            textBox.Location = new Point(x + 55, y);
            textBox.Size = new Size(68, 31);
            textBox.TextChanged += new EventHandler(TextBox_NumberRangeChecker);
            textBox.TextChanged += new EventHandler(MeshParameter_TextChanged);
            textBox.Enter += new EventHandler(MeshParameter_Enter);
            meshAreaMap[textBox] = ParseAreaPercent(areaText);
            parent.Controls.Add(textBox);

            Label percentLabel = new Label();
            percentLabel.AutoSize = true;
            percentLabel.Font = rowFont;
            percentLabel.Location = new Point(x + 128, y + 3);
            percentLabel.Text = "%";
            parent.Controls.Add(percentLabel);

            return textBox;
        }

        private int ParseAreaPercent(string areaText)
        {
            int value;
            if (int.TryParse(areaText.Replace("%", string.Empty), out value))
                return value;
            return 10;
        }

        private void BuildPreviewLauncher()
        {
            button_ShowLivePreview_WC = CreatePreviewButton();
            button_ShowLivePreview_WC.Location = new Point(644, 615);
            tabPage1.Controls.Add(button_ShowLivePreview_WC);
            button_ShowLivePreview_WC.BringToFront();

            button_ShowLivePreview_DC = CreatePreviewButton();
            button_ShowLivePreview_DC.Location = new Point(644, 615);
            tabPage2.Controls.Add(button_ShowLivePreview_DC);
            button_ShowLivePreview_DC.BringToFront();
        }

        private Button CreatePreviewButton()
        {
            Button button = new Button();
            button.Text = "顯示預覽畫面";
            button.Font = new Font("Microsoft JhengHei UI", 13F, FontStyle.Bold);
            button.Size = new Size(190, 44);
            button.Click += delegate { ShowLivePreview(); };
            return button;
        }

        private void ShowLivePreview()
        {
            if (livePreviewForm == null || livePreviewForm.IsDisposed)
                livePreviewForm = new Form_LivePreview(StandardOPage);

            if (!livePreviewForm.Visible)
                livePreviewForm.Show(FindForm());

            livePreviewForm.BringToFront();
            livePreviewForm.Activate();
            RefreshLivePreview();
        }

        private void MeshParameter_Enter(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            int area = meshAreaMap.ContainsKey(tb) ? meshAreaMap[tb] : 10;
            SelectMeshParameter(tb, area);
        }

        private void MeshParameter_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null || selectedPreviewTarget != PreviewTarget.Mesh ||
                !meshAreaMap.ContainsKey(tb)) return;

            QueuePreviewRefresh();
        }

        private void SelectMeshParameter(TextBox tb, int areaPercent)
        {
            if (tb == null) return;
            ClearPreviewSelectionColor();
            selectedPreviewControl = tb;
            selectedPreviewTarget = PreviewTarget.Mesh;
            selectedMeshArea = areaPercent;
            tb.BackColor = Color.LightGoldenrodYellow;
            QueuePreviewRefresh();
        }

        private void WireFontPreviewEvents()
        {
            TextBox[] fontBoxes = new TextBox[]
            {
                textBox_WC_Yin_3pt, textBox_WC_Yin_4pt, textBox_WC_Yin_5pt, textBox_WC_Yin_6pt, textBox_WC_Yin_7pt,
                textBox_WC_Yin_8pt, textBox_WC_Yin_9pt, textBox_WC_Yin_10pt, textBox_WC_Yin_11pt, textBox_WC_Yin_12pt,
                textBox_WC_Yang_3pt, textBox_WC_Yang_4pt, textBox_WC_Yang_5pt, textBox_WC_Yang_6pt, textBox_WC_Yang_7pt,
                textBox_WC_Yang_8pt, textBox_WC_Yang_9pt, textBox_WC_Yang_10pt, textBox_WC_Yang_11pt, textBox_WC_Yang_12pt,
                textBox_DC_Yin_3pt, textBox_DC_Yin_4pt, textBox_DC_Yin_5pt, textBox_DC_Yin_6pt, textBox_DC_Yin_7pt,
                textBox_DC_Yin_8pt, textBox_DC_Yin_9pt, textBox_DC_Yin_10pt, textBox_DC_Yin_11pt, textBox_DC_Yin_12pt,
                textBox_DC_Yang_3pt, textBox_DC_Yang_4pt, textBox_DC_Yang_5pt, textBox_DC_Yang_6pt, textBox_DC_Yang_7pt,
                textBox_DC_Yang_8pt, textBox_DC_Yang_9pt, textBox_DC_Yang_10pt, textBox_DC_Yang_11pt, textBox_DC_Yang_12pt
            };

            foreach (TextBox tb in fontBoxes)
            {
                if (tb == null) continue;
                tb.Enter += FontParameter_Enter;
                tb.TextChanged += FontParameter_TextChanged;
            }
        }

        private void FontParameter_Enter(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string name = tb.Name ?? string.Empty;
            bool isYin = name.IndexOf("_Yin_", StringComparison.OrdinalIgnoreCase) >= 0;
            int pt = ParseFontPointSize(name);

            ClearPreviewSelectionColor();
            selectedPreviewControl = tb;
            selectedPreviewTarget = PreviewTarget.Font;
            selectedFontIsYin = isYin;
            selectedFontPt = pt;
            tb.BackColor = Color.LightGoldenrodYellow;
            QueuePreviewRefresh();
        }

        private void FontParameter_TextChanged(object sender, EventArgs e)
        {
            if (sender == selectedPreviewControl && selectedPreviewTarget == PreviewTarget.Font)
                QueuePreviewRefresh();
        }

        private int ParseFontPointSize(string controlName)
        {
            int ptIndex = controlName.LastIndexOf("pt", StringComparison.OrdinalIgnoreCase);
            if (ptIndex <= 0) return 3;

            int underscore = controlName.LastIndexOf('_', ptIndex);
            if (underscore < 0) return 3;

            int value;
            return int.TryParse(controlName.Substring(underscore + 1, ptIndex - underscore - 1), out value) ? value : 3;
        }

        private void ClearPreviewSelectionColor()
        {
            TextBox old = selectedPreviewControl as TextBox;
            if (old != null)
                old.BackColor = SystemColors.Window;
        }

        private void QueuePreviewRefresh()
        {
            previewDebounceTimer?.Stop();
            previewDebounceTimer?.Start();
        }

        private void RefreshLivePreview()
        {
            if (livePreviewForm == null || livePreviewForm.IsDisposed || !livePreviewForm.Visible)
                return;

            TextBox tb = selectedPreviewControl as TextBox;
            if (tb == null) return;

            double percent;
            if (!double.TryParse(tb.Text, out percent) || percent < 0 || percent > 100)
                return;

            if (selectedPreviewTarget == PreviewTarget.Mesh)
                livePreviewForm.ShowMesh(selectedMeshArea, percent);
            else
                livePreviewForm.ShowFont(selectedFontIsYin, selectedFontPt, percent);
        }

        // 800ms debounced main-screen update. Read ALL ten values of the same side,
        // not only the selected pt, so total percentages always use a consistent set.
        private void RefreshMainFontScore()
        {
            TextBox tb = selectedPreviewControl as TextBox;
            if (tb == null || selectedPreviewTarget != PreviewTarget.Font) return;
            string name = tb.Name ?? "";
            bool whiteCard = name.IndexOf("_WC_", StringComparison.OrdinalIgnoreCase) >= 0;
            bool doubleCard = name.IndexOf("_DC_", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!whiteCard && !doubleCard) return;
            // Never update the displayed active card from an inactive card tab.
            bool activeWhiteCard = object.ReferenceEquals(StandardOPage.CurrentCardType_Params, StandardOPage.WC_Params);
            bool activeDoubleCard = object.ReferenceEquals(StandardOPage.CurrentCardType_Params, StandardOPage.DC_Params);
            if ((whiteCard && !activeWhiteCard) || (doubleCard && !activeDoubleCard)) return;
            var values = new double[10];
            string card = whiteCard ? "WC" : "DC";
            string side = selectedFontIsYin ? "Yin" : "Yang";
            for (int pt = 3; pt <= 12; pt++)
            {
                Control[] matches = Controls.Find("textBox_" + card + "_" + side + "_" + pt + "pt", true);
                if (matches.Length != 1) return;
                TextBox valueBox = matches[0] as TextBox;
                double percent;
                if (valueBox == null || !double.TryParse(valueBox.Text, out percent)
                    || percent < 0 || percent > 100) return;
                values[pt - 3] = percent;
            }
            StandardOPage.RefreshFontScoreFromEngineeringUI(selectedFontIsYin, values);
        }

        // All eight thresholds are read from the active card tab in one pass,
        // matching the per-font live-score update strategy.
        private void RefreshMainMeshScore()
        {
            TextBox selected = selectedPreviewControl as TextBox;
            if (selected == null || selectedPreviewTarget != PreviewTarget.Mesh) return;
            bool whiteCard = selected.Name.IndexOf("_WC_", StringComparison.OrdinalIgnoreCase) >= 0;
            bool doubleCard = selected.Name.IndexOf("_DC_", StringComparison.OrdinalIgnoreCase) >= 0;
            bool activeWhiteCard = object.ReferenceEquals(StandardOPage.CurrentCardType_Params, StandardOPage.WC_Params);
            bool activeDoubleCard = object.ReferenceEquals(StandardOPage.CurrentCardType_Params, StandardOPage.DC_Params);
            if ((!whiteCard && !doubleCard) || (whiteCard && !activeWhiteCard) ||
                (doubleCard && !activeDoubleCard)) return;
            string prefix = whiteCard ? "WC" : "DC";
            int[] areas = { 10, 20, 30, 40, 60, 70, 80, 90 };
            double[] percentages = new double[areas.Length];
            for (int i = 0; i < areas.Length; i++)
            {
                Control[] controls = Controls.Find("textBox_" + prefix + "_MeshP_" + areas[i], true);
                TextBox box = controls.Length == 1 ? controls[0] as TextBox : null;
                double number;
                if (box == null || !double.TryParse(box.Text, out number) ||
                    double.IsNaN(number) || number < 0 || number > 100) return;
                percentages[i] = number;
            }
            StandardOPage.RefreshMeshScoreFromEngineeringUI(percentages);
        }

        private void ReloadUI()
        {
            // ==================== 白卡：字體區 ====================
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

            // ==================== 白卡：網點區 ====================
            textBox_WC_MeshP_10.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area10).ToString();
            textBox_WC_MeshP_20.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area20).ToString();
            textBox_WC_MeshP_30.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area30).ToString();
            textBox_WC_MeshP_40.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area40).ToString();
            textBox_WC_MeshP_60.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area60).ToString();
            textBox_WC_MeshP_70.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area70).ToString();
            textBox_WC_MeshP_80.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area80).ToString();
            textBox_WC_MeshP_90.Text = ThresholdToPercent(StandardOPage.WC_Params.meshp_parameter.Area90).ToString();

            textBox_WC_Fullness.Text = ThresholdToPercent(StandardOPage.WC_Params.fullness_parameter.Threshold).ToString();

            textBox_WC_BreakArea_01_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_01).ToString();
            textBox_WC_BreakArea_02_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_02).ToString();
            textBox_WC_BreakArea_03_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_03).ToString();
            textBox_WC_BreakArea_Step1_Threshold.Text = ThresholdToPercent(StandardOPage.WC_Params.breakarea_parameter.Threshold_Step1).ToString();

            // ==================== 雙銅：字體區 ====================
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

            // ==================== 雙銅：網點區 ====================
            textBox_DC_MeshP_10.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area10).ToString();
            textBox_DC_MeshP_20.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area20).ToString();
            textBox_DC_MeshP_30.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area30).ToString();
            textBox_DC_MeshP_40.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area40).ToString();
            textBox_DC_MeshP_60.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area60).ToString();
            textBox_DC_MeshP_70.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area70).ToString();
            textBox_DC_MeshP_80.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area80).ToString();
            textBox_DC_MeshP_90.Text = ThresholdToPercent(StandardOPage.DC_Params.meshp_parameter.Area90).ToString();

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

        // textbox 防呆：只允許 0 ~ 100
        private void TextBox_NumberRangeChecker(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            string input = tb.Text;

            if (double.TryParse(input, out double value))
            {
                if (value >= 0 && value <= 100)
                {
                    tb.Tag = input;
                }
                else
                {
                    MessageBox.Show("請輸入 0 到 100 的數值（可含小數）！", "輸入錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
            else
            {
                // 排除正在輸入小數點的情況，例如 "12."
                if (!string.IsNullOrEmpty(input) && input != "." && input != "-")
                {
                    MessageBox.Show("請輸入數字（可含小數點），勿輸入文字或非法符號！", "格式錯誤", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tb.Text = tb.Tag?.ToString() ?? "";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }

        private void groupBox8_Enter(object sender, EventArgs e)
        {
        }

        // Designer 仍可能保留舊事件綁定；介面已隱藏且此功能不再切換模式。
        private void radioButton_MeshManual_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void radioButton_MeshAuto_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}
