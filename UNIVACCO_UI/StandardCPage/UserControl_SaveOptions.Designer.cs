
namespace UNIVACCO_UI
{
    partial class UserControl_SaveOptions
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary> 
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkbox_roi_areacrop = new System.Windows.Forms.CheckBox();
            this.checkbox_roi_croppaper = new System.Windows.Forms.CheckBox();
            this.checkbox_roi_crop = new System.Windows.Forms.CheckBox();
            this.checkbox_roi_rotate = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkbox_meshp_crop = new System.Windows.Forms.CheckBox();
            this.checkbox_meshp_threshold = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkbox_yin_original = new System.Windows.Forms.CheckBox();
            this.checkbox_yin_correction = new System.Windows.Forms.CheckBox();
            this.checkbox_yin_filternoise = new System.Windows.Forms.CheckBox();
            this.checkbox_yin_calcoutsideblock = new System.Windows.Forms.CheckBox();
            this.checkbox_yin_threshold = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.checkbox_yang_original = new System.Windows.Forms.CheckBox();
            this.checkbox_yang_correction = new System.Windows.Forms.CheckBox();
            this.checkbox_yang_filternoise = new System.Windows.Forms.CheckBox();
            this.checkbox_yang_calcoutsideblock = new System.Windows.Forms.CheckBox();
            this.checkbox_yang_threshold = new System.Windows.Forms.CheckBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkbox_fullness_threshold = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.checkbox_maketemplates = new System.Windows.Forms.CheckBox();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.Open_Parameters_INI_button = new System.Windows.Forms.Button();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.checkBox_Valve = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkbox_roi_areacrop);
            this.groupBox1.Controls.Add(this.checkbox_roi_croppaper);
            this.groupBox1.Controls.Add(this.checkbox_roi_crop);
            this.groupBox1.Controls.Add(this.checkbox_roi_rotate);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox1.Location = new System.Drawing.Point(24, 57);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(176, 254);
            this.groupBox1.TabIndex = 97;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ROI";
            // 
            // checkbox_roi_areacrop
            // 
            this.checkbox_roi_areacrop.AutoSize = true;
            this.checkbox_roi_areacrop.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_roi_areacrop.Location = new System.Drawing.Point(19, 191);
            this.checkbox_roi_areacrop.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_roi_areacrop.Name = "checkbox_roi_areacrop";
            this.checkbox_roi_areacrop.Size = new System.Drawing.Size(143, 28);
            this.checkbox_roi_areacrop.TabIndex = 106;
            this.checkbox_roi_areacrop.Text = "裁切陰陽飽區";
            this.checkbox_roi_areacrop.UseVisualStyleBackColor = true;
            // 
            // checkbox_roi_croppaper
            // 
            this.checkbox_roi_croppaper.AutoSize = true;
            this.checkbox_roi_croppaper.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_roi_croppaper.Location = new System.Drawing.Point(19, 58);
            this.checkbox_roi_croppaper.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_roi_croppaper.Name = "checkbox_roi_croppaper";
            this.checkbox_roi_croppaper.Size = new System.Drawing.Size(105, 28);
            this.checkbox_roi_croppaper.TabIndex = 23;
            this.checkbox_roi_croppaper.Text = "擷取紙張";
            this.checkbox_roi_croppaper.UseVisualStyleBackColor = true;
            // 
            // checkbox_roi_crop
            // 
            this.checkbox_roi_crop.AutoSize = true;
            this.checkbox_roi_crop.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_roi_crop.Location = new System.Drawing.Point(19, 145);
            this.checkbox_roi_crop.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_roi_crop.Name = "checkbox_roi_crop";
            this.checkbox_roi_crop.Size = new System.Drawing.Size(105, 28);
            this.checkbox_roi_crop.TabIndex = 104;
            this.checkbox_roi_crop.Text = "裁切區域";
            this.checkbox_roi_crop.UseVisualStyleBackColor = true;
            // 
            // checkbox_roi_rotate
            // 
            this.checkbox_roi_rotate.AutoSize = true;
            this.checkbox_roi_rotate.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_roi_rotate.Location = new System.Drawing.Point(19, 102);
            this.checkbox_roi_rotate.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_roi_rotate.Name = "checkbox_roi_rotate";
            this.checkbox_roi_rotate.Size = new System.Drawing.Size(105, 28);
            this.checkbox_roi_rotate.TabIndex = 103;
            this.checkbox_roi_rotate.Text = "旋轉校正";
            this.checkbox_roi_rotate.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkbox_meshp_crop);
            this.groupBox2.Controls.Add(this.checkbox_meshp_threshold);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox2.Location = new System.Drawing.Point(217, 57);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox2.Size = new System.Drawing.Size(149, 254);
            this.groupBox2.TabIndex = 98;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "網點區";
            // 
            // checkbox_meshp_crop
            // 
            this.checkbox_meshp_crop.AutoSize = true;
            this.checkbox_meshp_crop.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_meshp_crop.Location = new System.Drawing.Point(19, 58);
            this.checkbox_meshp_crop.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_meshp_crop.Name = "checkbox_meshp_crop";
            this.checkbox_meshp_crop.Size = new System.Drawing.Size(124, 28);
            this.checkbox_meshp_crop.TabIndex = 107;
            this.checkbox_meshp_crop.Text = "裁切網點區";
            this.checkbox_meshp_crop.UseVisualStyleBackColor = true;
            // 
            // checkbox_meshp_threshold
            // 
            this.checkbox_meshp_threshold.AutoSize = true;
            this.checkbox_meshp_threshold.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_meshp_threshold.Location = new System.Drawing.Point(19, 102);
            this.checkbox_meshp_threshold.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_meshp_threshold.Name = "checkbox_meshp_threshold";
            this.checkbox_meshp_threshold.Size = new System.Drawing.Size(86, 28);
            this.checkbox_meshp_threshold.TabIndex = 108;
            this.checkbox_meshp_threshold.Text = "二值化";
            this.checkbox_meshp_threshold.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.checkbox_yin_original);
            this.groupBox3.Controls.Add(this.checkbox_yin_correction);
            this.groupBox3.Controls.Add(this.checkbox_yin_filternoise);
            this.groupBox3.Controls.Add(this.checkbox_yin_calcoutsideblock);
            this.groupBox3.Controls.Add(this.checkbox_yin_threshold);
            this.groupBox3.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox3.Location = new System.Drawing.Point(379, 57);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox3.Size = new System.Drawing.Size(179, 254);
            this.groupBox3.TabIndex = 99;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "陰版";
            // 
            // checkbox_yin_original
            // 
            this.checkbox_yin_original.AutoSize = true;
            this.checkbox_yin_original.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.checkbox_yin_original.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yin_original.Location = new System.Drawing.Point(44, 40);
            this.checkbox_yin_original.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yin_original.Name = "checkbox_yin_original";
            this.checkbox_yin_original.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yin_original.TabIndex = 115;
            this.checkbox_yin_original.Text = "字體原圖";
            this.checkbox_yin_original.UseVisualStyleBackColor = false;
            // 
            // checkbox_yin_correction
            // 
            this.checkbox_yin_correction.AutoSize = true;
            this.checkbox_yin_correction.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yin_correction.Location = new System.Drawing.Point(44, 213);
            this.checkbox_yin_correction.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yin_correction.Name = "checkbox_yin_correction";
            this.checkbox_yin_correction.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yin_correction.TabIndex = 114;
            this.checkbox_yin_correction.Text = "影像對位";
            this.checkbox_yin_correction.UseVisualStyleBackColor = true;
            // 
            // checkbox_yin_filternoise
            // 
            this.checkbox_yin_filternoise.AutoSize = true;
            this.checkbox_yin_filternoise.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.checkbox_yin_filternoise.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yin_filternoise.Location = new System.Drawing.Point(44, 79);
            this.checkbox_yin_filternoise.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yin_filternoise.Name = "checkbox_yin_filternoise";
            this.checkbox_yin_filternoise.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yin_filternoise.TabIndex = 111;
            this.checkbox_yin_filternoise.Text = "雜訊濾除";
            this.checkbox_yin_filternoise.UseVisualStyleBackColor = false;
            // 
            // checkbox_yin_calcoutsideblock
            // 
            this.checkbox_yin_calcoutsideblock.AutoSize = true;
            this.checkbox_yin_calcoutsideblock.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yin_calcoutsideblock.Location = new System.Drawing.Point(44, 166);
            this.checkbox_yin_calcoutsideblock.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yin_calcoutsideblock.Name = "checkbox_yin_calcoutsideblock";
            this.checkbox_yin_calcoutsideblock.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yin_calcoutsideblock.TabIndex = 113;
            this.checkbox_yin_calcoutsideblock.Text = "初步裁切";
            this.checkbox_yin_calcoutsideblock.UseVisualStyleBackColor = true;
            // 
            // checkbox_yin_threshold
            // 
            this.checkbox_yin_threshold.AutoSize = true;
            this.checkbox_yin_threshold.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yin_threshold.Location = new System.Drawing.Point(44, 124);
            this.checkbox_yin_threshold.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yin_threshold.Name = "checkbox_yin_threshold";
            this.checkbox_yin_threshold.Size = new System.Drawing.Size(86, 28);
            this.checkbox_yin_threshold.TabIndex = 112;
            this.checkbox_yin_threshold.Text = "二值化";
            this.checkbox_yin_threshold.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.checkbox_yang_original);
            this.groupBox4.Controls.Add(this.checkbox_yang_correction);
            this.groupBox4.Controls.Add(this.checkbox_yang_filternoise);
            this.groupBox4.Controls.Add(this.checkbox_yang_calcoutsideblock);
            this.groupBox4.Controls.Add(this.checkbox_yang_threshold);
            this.groupBox4.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox4.Location = new System.Drawing.Point(562, 57);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox4.Size = new System.Drawing.Size(154, 254);
            this.groupBox4.TabIndex = 100;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "陽版";
            // 
            // checkbox_yang_original
            // 
            this.checkbox_yang_original.AutoSize = true;
            this.checkbox_yang_original.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yang_original.Location = new System.Drawing.Point(24, 40);
            this.checkbox_yang_original.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yang_original.Name = "checkbox_yang_original";
            this.checkbox_yang_original.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yang_original.TabIndex = 115;
            this.checkbox_yang_original.Text = "字體原圖";
            this.checkbox_yang_original.UseVisualStyleBackColor = true;
            // 
            // checkbox_yang_correction
            // 
            this.checkbox_yang_correction.AutoSize = true;
            this.checkbox_yang_correction.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yang_correction.Location = new System.Drawing.Point(24, 213);
            this.checkbox_yang_correction.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yang_correction.Name = "checkbox_yang_correction";
            this.checkbox_yang_correction.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yang_correction.TabIndex = 114;
            this.checkbox_yang_correction.Text = "影像對位";
            this.checkbox_yang_correction.UseVisualStyleBackColor = true;
            // 
            // checkbox_yang_filternoise
            // 
            this.checkbox_yang_filternoise.AutoSize = true;
            this.checkbox_yang_filternoise.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yang_filternoise.Location = new System.Drawing.Point(24, 79);
            this.checkbox_yang_filternoise.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yang_filternoise.Name = "checkbox_yang_filternoise";
            this.checkbox_yang_filternoise.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yang_filternoise.TabIndex = 111;
            this.checkbox_yang_filternoise.Text = "雜訊濾除";
            this.checkbox_yang_filternoise.UseVisualStyleBackColor = true;
            // 
            // checkbox_yang_calcoutsideblock
            // 
            this.checkbox_yang_calcoutsideblock.AutoSize = true;
            this.checkbox_yang_calcoutsideblock.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.checkbox_yang_calcoutsideblock.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yang_calcoutsideblock.Location = new System.Drawing.Point(24, 166);
            this.checkbox_yang_calcoutsideblock.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yang_calcoutsideblock.Name = "checkbox_yang_calcoutsideblock";
            this.checkbox_yang_calcoutsideblock.Size = new System.Drawing.Size(105, 28);
            this.checkbox_yang_calcoutsideblock.TabIndex = 113;
            this.checkbox_yang_calcoutsideblock.Text = "初步裁切";
            this.checkbox_yang_calcoutsideblock.UseVisualStyleBackColor = false;
            // 
            // checkbox_yang_threshold
            // 
            this.checkbox_yang_threshold.AutoSize = true;
            this.checkbox_yang_threshold.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_yang_threshold.Location = new System.Drawing.Point(24, 124);
            this.checkbox_yang_threshold.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_yang_threshold.Name = "checkbox_yang_threshold";
            this.checkbox_yang_threshold.Size = new System.Drawing.Size(86, 28);
            this.checkbox_yang_threshold.TabIndex = 112;
            this.checkbox_yang_threshold.Text = "二值化";
            this.checkbox_yang_threshold.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.checkbox_fullness_threshold);
            this.groupBox5.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox5.Location = new System.Drawing.Point(765, 57);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox5.Size = new System.Drawing.Size(154, 254);
            this.groupBox5.TabIndex = 101;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "飽滿區";
            // 
            // checkbox_fullness_threshold
            // 
            this.checkbox_fullness_threshold.AutoSize = true;
            this.checkbox_fullness_threshold.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_fullness_threshold.Location = new System.Drawing.Point(20, 47);
            this.checkbox_fullness_threshold.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_fullness_threshold.Name = "checkbox_fullness_threshold";
            this.checkbox_fullness_threshold.Size = new System.Drawing.Size(86, 28);
            this.checkbox_fullness_threshold.TabIndex = 108;
            this.checkbox_fullness_threshold.Text = "二值化";
            this.checkbox_fullness_threshold.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            this.groupBox6.Controls.Add(this.checkbox_maketemplates);
            this.groupBox6.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox6.Location = new System.Drawing.Point(24, 332);
            this.groupBox6.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox6.Size = new System.Drawing.Size(162, 101);
            this.groupBox6.TabIndex = 102;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "字體區";
            // 
            // checkbox_maketemplates
            // 
            this.checkbox_maketemplates.AutoSize = true;
            this.checkbox_maketemplates.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkbox_maketemplates.Location = new System.Drawing.Point(19, 50);
            this.checkbox_maketemplates.Margin = new System.Windows.Forms.Padding(2);
            this.checkbox_maketemplates.Name = "checkbox_maketemplates";
            this.checkbox_maketemplates.Size = new System.Drawing.Size(105, 28);
            this.checkbox_maketemplates.TabIndex = 107;
            this.checkbox_maketemplates.Text = "生成模板";
            this.checkbox_maketemplates.UseVisualStyleBackColor = true;
            this.checkbox_maketemplates.CheckedChanged += new System.EventHandler(this.checkbox_maketemplates_checkchanged);
            // 
            // groupBox7
            // 
            this.groupBox7.Font = new System.Drawing.Font("Microsoft JhengHei UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.groupBox7.Location = new System.Drawing.Point(13, 14);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(922, 429);
            this.groupBox7.TabIndex = 103;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "存圖設定";
            // 
            // groupBox8
            // 
            this.groupBox8.Controls.Add(this.Open_Parameters_INI_button);
            this.groupBox8.Font = new System.Drawing.Font("Microsoft JhengHei UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.groupBox8.Location = new System.Drawing.Point(24, 481);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.Size = new System.Drawing.Size(189, 116);
            this.groupBox8.TabIndex = 104;
            this.groupBox8.TabStop = false;
            this.groupBox8.Text = "其他參數設定";
            // 
            // Open_Parameters_INI_button
            // 
            this.Open_Parameters_INI_button.Font = new System.Drawing.Font("Microsoft JhengHei UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Open_Parameters_INI_button.Location = new System.Drawing.Point(20, 60);
            this.Open_Parameters_INI_button.Name = "Open_Parameters_INI_button";
            this.Open_Parameters_INI_button.Size = new System.Drawing.Size(132, 36);
            this.Open_Parameters_INI_button.TabIndex = 0;
            this.Open_Parameters_INI_button.Text = "開啟設定檔";
            this.Open_Parameters_INI_button.UseVisualStyleBackColor = true;
            this.Open_Parameters_INI_button.Click += new System.EventHandler(this.Open_Parameters_INI_button_Click);
            // 
            // groupBox9
            // 
            this.groupBox9.Controls.Add(this.checkBox_Valve);
            this.groupBox9.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.groupBox9.Location = new System.Drawing.Point(236, 481);
            this.groupBox9.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox9.Size = new System.Drawing.Size(162, 116);
            this.groupBox9.TabIndex = 108;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "吸盤";
            // 
            // checkBox_Valve
            // 
            this.checkBox_Valve.AutoSize = true;
            this.checkBox_Valve.Font = new System.Drawing.Font("Microsoft JhengHei UI", 13.8F, System.Drawing.FontStyle.Bold);
            this.checkBox_Valve.Location = new System.Drawing.Point(19, 50);
            this.checkBox_Valve.Margin = new System.Windows.Forms.Padding(2);
            this.checkBox_Valve.Name = "checkBox_Valve";
            this.checkBox_Valve.Size = new System.Drawing.Size(105, 28);
            this.checkBox_Valve.TabIndex = 107;
            this.checkBox_Valve.Text = "功能啟用";
            this.checkBox_Valve.UseVisualStyleBackColor = true;
            this.checkBox_Valve.CheckedChanged += new System.EventHandler(this.checkBox_Valve_CheckedChanged);
            // 
            // UserControl_SaveOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.Controls.Add(this.groupBox9);
            this.Controls.Add(this.groupBox8);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox7);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "UserControl_SaveOptions";
            this.Size = new System.Drawing.Size(948, 779);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.GroupBox groupBox6;
        public System.Windows.Forms.CheckBox checkbox_roi_croppaper;
        public System.Windows.Forms.CheckBox checkbox_roi_areacrop;
        public System.Windows.Forms.CheckBox checkbox_roi_crop;
        public System.Windows.Forms.CheckBox checkbox_roi_rotate;
        public System.Windows.Forms.CheckBox checkbox_meshp_crop;
        public System.Windows.Forms.CheckBox checkbox_meshp_threshold;
        public System.Windows.Forms.CheckBox checkbox_yin_correction;
        public System.Windows.Forms.CheckBox checkbox_yin_filternoise;
        public System.Windows.Forms.CheckBox checkbox_yin_calcoutsideblock;
        public System.Windows.Forms.CheckBox checkbox_yin_threshold;
        public System.Windows.Forms.CheckBox checkbox_yang_correction;
        public System.Windows.Forms.CheckBox checkbox_yang_filternoise;
        public System.Windows.Forms.CheckBox checkbox_yang_calcoutsideblock;
        public System.Windows.Forms.CheckBox checkbox_yang_threshold;
        public System.Windows.Forms.CheckBox checkbox_fullness_threshold;
        public System.Windows.Forms.CheckBox checkbox_maketemplates;
        public System.Windows.Forms.CheckBox checkbox_yin_original;
        public System.Windows.Forms.CheckBox checkbox_yang_original;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.GroupBox groupBox8;
        private System.Windows.Forms.Button Open_Parameters_INI_button;
        private System.Windows.Forms.GroupBox groupBox9;
        public System.Windows.Forms.CheckBox checkBox_Valve;
    }
}
