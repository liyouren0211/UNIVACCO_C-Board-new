namespace StandardOPage
{
    partial class Form_ReAnalyze
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param Name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox_AreaImage = new System.Windows.Forms.PictureBox();
            this.button_Confirm = new System.Windows.Forms.Button();
            this.button_Close = new System.Windows.Forms.Button();
            this.label_Name = new System.Windows.Forms.Label();
            this.button_AddArea = new System.Windows.Forms.Button();
            this.button_SaveArea = new System.Windows.Forms.Button();
            this.button_CancelArea = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_AreaImage)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox_AreaImage
            // 
            this.pictureBox_AreaImage.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pictureBox_AreaImage.Location = new System.Drawing.Point(30, 57);
            this.pictureBox_AreaImage.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.pictureBox_AreaImage.Name = "pictureBox_AreaImage";
            this.pictureBox_AreaImage.Size = new System.Drawing.Size(291, 455);
            this.pictureBox_AreaImage.TabIndex = 0;
            this.pictureBox_AreaImage.TabStop = false;
            // 
            // button_Confirm
            // 
            this.button_Confirm.BackColor = System.Drawing.SystemColors.ControlLight;
            this.button_Confirm.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_Confirm.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_Confirm.FlatAppearance.BorderSize = 0;
            this.button_Confirm.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.button_Confirm.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button_Confirm.Location = new System.Drawing.Point(92, 590);
            this.button_Confirm.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_Confirm.Name = "button_Confirm";
            this.button_Confirm.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_Confirm.Size = new System.Drawing.Size(92, 53);
            this.button_Confirm.TabIndex = 84;
            this.button_Confirm.Text = "確定";
            this.button_Confirm.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_Confirm.UseMnemonic = false;
            this.button_Confirm.UseVisualStyleBackColor = false;
            this.button_Confirm.Click += new System.EventHandler(this.button_Confirm_Click);
            // 
            // button_Close
            // 
            this.button_Close.BackColor = System.Drawing.SystemColors.ControlLight;
            this.button_Close.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_Close.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_Close.FlatAppearance.BorderSize = 0;
            this.button_Close.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.button_Close.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button_Close.Location = new System.Drawing.Point(190, 590);
            this.button_Close.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_Close.Name = "button_Close";
            this.button_Close.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_Close.Size = new System.Drawing.Size(92, 53);
            this.button_Close.TabIndex = 85;
            this.button_Close.Text = "關閉";
            this.button_Close.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_Close.UseMnemonic = false;
            this.button_Close.UseVisualStyleBackColor = false;
            this.button_Close.Click += new System.EventHandler(this.button_Close_Click);
            // 
            // label_Name
            // 
            this.label_Name.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label_Name.BackColor = System.Drawing.Color.Black;
            this.label_Name.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label_Name.Font = new System.Drawing.Font("Microsoft JhengHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.label_Name.ForeColor = System.Drawing.Color.White;
            this.label_Name.Location = new System.Drawing.Point(30, 8);
            this.label_Name.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_Name.Name = "label_Name";
            this.label_Name.Size = new System.Drawing.Size(291, 47);
            this.label_Name.TabIndex = 86;
            this.label_Name.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // button_AddArea
            // 
            this.button_AddArea.BackColor = System.Drawing.SystemColors.ControlLight;
            this.button_AddArea.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_AddArea.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_AddArea.FlatAppearance.BorderSize = 0;
            this.button_AddArea.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.button_AddArea.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button_AddArea.Location = new System.Drawing.Point(110, 516);
            this.button_AddArea.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_AddArea.Name = "button_AddArea";
            this.button_AddArea.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_AddArea.Size = new System.Drawing.Size(124, 53);
            this.button_AddArea.TabIndex = 87;
            this.button_AddArea.Text = "新增區域";
            this.button_AddArea.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_AddArea.UseMnemonic = false;
            this.button_AddArea.UseVisualStyleBackColor = false;
            this.button_AddArea.Click += new System.EventHandler(this.button_AddArea_Click);
            // 
            // button_SaveArea
            // 
            this.button_SaveArea.BackColor = System.Drawing.SystemColors.ControlLight;
            this.button_SaveArea.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_SaveArea.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_SaveArea.FlatAppearance.BorderSize = 0;
            this.button_SaveArea.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.button_SaveArea.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button_SaveArea.Location = new System.Drawing.Point(12, 516);
            this.button_SaveArea.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_SaveArea.Name = "button_SaveArea";
            this.button_SaveArea.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_SaveArea.Size = new System.Drawing.Size(92, 53);
            this.button_SaveArea.TabIndex = 88;
            this.button_SaveArea.Text = "儲存";
            this.button_SaveArea.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_SaveArea.UseMnemonic = false;
            this.button_SaveArea.UseVisualStyleBackColor = false;
            this.button_SaveArea.Visible = false;
            this.button_SaveArea.Click += new System.EventHandler(this.button_SaveArea_Click);
            // 
            // button_CancelArea
            // 
            this.button_CancelArea.BackColor = System.Drawing.SystemColors.ControlLight;
            this.button_CancelArea.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_CancelArea.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_CancelArea.FlatAppearance.BorderSize = 0;
            this.button_CancelArea.Font = new System.Drawing.Font("Microsoft JhengHei UI", 16F, System.Drawing.FontStyle.Bold);
            this.button_CancelArea.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button_CancelArea.Location = new System.Drawing.Point(246, 516);
            this.button_CancelArea.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.button_CancelArea.Name = "button_CancelArea";
            this.button_CancelArea.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_CancelArea.Size = new System.Drawing.Size(92, 53);
            this.button_CancelArea.TabIndex = 89;
            this.button_CancelArea.Text = "取消";
            this.button_CancelArea.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_CancelArea.UseMnemonic = false;
            this.button_CancelArea.UseVisualStyleBackColor = false;
            this.button_CancelArea.Visible = false;
            this.button_CancelArea.Click += new System.EventHandler(this.button_CancelArea_Click);
            // 
            // Form_ReAnalyze
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.ClientSize = new System.Drawing.Size(350, 656);
            this.Controls.Add(this.button_CancelArea);
            this.Controls.Add(this.button_SaveArea);
            this.Controls.Add(this.button_AddArea);
            this.Controls.Add(this.label_Name);
            this.Controls.Add(this.button_Close);
            this.Controls.Add(this.button_Confirm);
            this.Controls.Add(this.pictureBox_AreaImage);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form_ReAnalyze";
            this.Text = "StandardO_ReAnalyze";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_AreaImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.PictureBox pictureBox_AreaImage;
        private System.Windows.Forms.Button button_Confirm;
        private System.Windows.Forms.Button button_Close;
        private System.Windows.Forms.Label label_Name;
        private System.Windows.Forms.Button button_AddArea;
        private System.Windows.Forms.Button button_SaveArea;
        private System.Windows.Forms.Button button_CancelArea;
    }
}