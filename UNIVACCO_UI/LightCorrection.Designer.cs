namespace UNIVACCO_UI
{
    partial class LightCorrection
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LightCorrection));
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.pictureBox_SourceImg = new System.Windows.Forms.PictureBox();
            this.button_VedioCam = new System.Windows.Forms.Button();
            this.button_ExposureTimeSave = new System.Windows.Forms.Button();
            this.textBox_ExposureTime = new System.Windows.Forms.TextBox();
            this.label_ExposureTime = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Motor_moves_right_button = new System.Windows.Forms.Button();
            this.button_check_right_limit = new System.Windows.Forms.Button();
            this.button_check_left_limit = new System.Windows.Forms.Button();
            this.Pixels_setting_textBox = new System.Windows.Forms.TextBox();
            this.Gohome_button = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_SourceImg)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackColor = System.Drawing.Color.AntiqueWhite;
            this.pictureBox3.Location = new System.Drawing.Point(-20, -1);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(1135, 81);
            this.pictureBox3.TabIndex = 70;
            this.pictureBox3.TabStop = false;
            // 
            // pictureBox_SourceImg
            // 
            this.pictureBox_SourceImg.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.pictureBox_SourceImg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox_SourceImg.Location = new System.Drawing.Point(10, 86);
            this.pictureBox_SourceImg.Name = "pictureBox_SourceImg";
            this.pictureBox_SourceImg.Size = new System.Drawing.Size(684, 487);
            this.pictureBox_SourceImg.TabIndex = 72;
            this.pictureBox_SourceImg.TabStop = false;
            // 
            // button_VedioCam
            // 
            this.button_VedioCam.BackColor = System.Drawing.Color.AntiqueWhite;
            this.button_VedioCam.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_VedioCam.BackgroundImage")));
            this.button_VedioCam.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_VedioCam.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_VedioCam.FlatAppearance.BorderSize = 0;
            this.button_VedioCam.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_VedioCam.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button_VedioCam.Location = new System.Drawing.Point(974, -1);
            this.button_VedioCam.Name = "button_VedioCam";
            this.button_VedioCam.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_VedioCam.Size = new System.Drawing.Size(91, 77);
            this.button_VedioCam.TabIndex = 86;
            this.button_VedioCam.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_VedioCam.UseMnemonic = false;
            this.button_VedioCam.UseVisualStyleBackColor = false;
            this.button_VedioCam.Click += new System.EventHandler(this.button_VedioCam_Click);
            // 
            // button_ExposureTimeSave
            // 
            this.button_ExposureTimeSave.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.button_ExposureTimeSave.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button_ExposureTimeSave.BackgroundImage")));
            this.button_ExposureTimeSave.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.button_ExposureTimeSave.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.button_ExposureTimeSave.FlatAppearance.BorderSize = 0;
            this.button_ExposureTimeSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button_ExposureTimeSave.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button_ExposureTimeSave.Location = new System.Drawing.Point(1039, 129);
            this.button_ExposureTimeSave.Name = "button_ExposureTimeSave";
            this.button_ExposureTimeSave.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.button_ExposureTimeSave.Size = new System.Drawing.Size(39, 30);
            this.button_ExposureTimeSave.TabIndex = 91;
            this.button_ExposureTimeSave.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.button_ExposureTimeSave.UseMnemonic = false;
            this.button_ExposureTimeSave.UseVisualStyleBackColor = false;
            this.button_ExposureTimeSave.Click += new System.EventHandler(this.button_ExposureTimeSave_Click);
            // 
            // textBox_ExposureTime
            // 
            this.textBox_ExposureTime.Font = new System.Drawing.Font("標楷體", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.textBox_ExposureTime.Location = new System.Drawing.Point(882, 128);
            this.textBox_ExposureTime.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.textBox_ExposureTime.Name = "textBox_ExposureTime";
            this.textBox_ExposureTime.Size = new System.Drawing.Size(152, 33);
            this.textBox_ExposureTime.TabIndex = 90;
            this.textBox_ExposureTime.TextChanged += new System.EventHandler(this.textBox_ExposureTime_TextChanged);
            // 
            // label_ExposureTime
            // 
            this.label_ExposureTime.AutoSize = true;
            this.label_ExposureTime.Font = new System.Drawing.Font("標楷體", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label_ExposureTime.Location = new System.Drawing.Point(711, 135);
            this.label_ExposureTime.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label_ExposureTime.Name = "label_ExposureTime";
            this.label_ExposureTime.Size = new System.Drawing.Size(175, 24);
            this.label_ExposureTime.TabIndex = 89;
            this.label_ExposureTime.Text = "曝光時間(µs):";
            this.label_ExposureTime.Click += new System.EventHandler(this.label_ExposureTime_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.Gohome_button);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.Motor_moves_right_button);
            this.groupBox3.Controls.Add(this.button_check_right_limit);
            this.groupBox3.Controls.Add(this.button_check_left_limit);
            this.groupBox3.Controls.Add(this.Pixels_setting_textBox);
            this.groupBox3.Font = new System.Drawing.Font("標楷體", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.groupBox3.Location = new System.Drawing.Point(715, 245);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(372, 148);
            this.groupBox3.TabIndex = 92;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "馬達控制";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(217, 49);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 19);
            this.label1.TabIndex = 72;
            this.label1.Text = "Pixels:";
            // 
            // Motor_moves_right_button
            // 
            this.Motor_moves_right_button.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Motor_moves_right_button.ForeColor = System.Drawing.SystemColors.InfoText;
            this.Motor_moves_right_button.Location = new System.Drawing.Point(113, 42);
            this.Motor_moves_right_button.Margin = new System.Windows.Forms.Padding(2);
            this.Motor_moves_right_button.Name = "Motor_moves_right_button";
            this.Motor_moves_right_button.Size = new System.Drawing.Size(99, 35);
            this.Motor_moves_right_button.TabIndex = 70;
            this.Motor_moves_right_button.Text = "馬達移動右";
            this.Motor_moves_right_button.UseVisualStyleBackColor = true;
            this.Motor_moves_right_button.Click += new System.EventHandler(this.Motor_moves_right_button_Click);
            // 
            // button_check_right_limit
            // 
            this.button_check_right_limit.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.button_check_right_limit.Font = new System.Drawing.Font("標楷體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_check_right_limit.Location = new System.Drawing.Point(115, 97);
            this.button_check_right_limit.Margin = new System.Windows.Forms.Padding(2);
            this.button_check_right_limit.Name = "button_check_right_limit";
            this.button_check_right_limit.Size = new System.Drawing.Size(97, 37);
            this.button_check_right_limit.TabIndex = 74;
            this.button_check_right_limit.Text = "檢查右極限";
            this.button_check_right_limit.UseVisualStyleBackColor = false;
            this.button_check_right_limit.Click += new System.EventHandler(this.button_check_right_limit_Click);
            // 
            // button_check_left_limit
            // 
            this.button_check_left_limit.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.button_check_left_limit.Font = new System.Drawing.Font("標楷體", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.button_check_left_limit.Location = new System.Drawing.Point(13, 97);
            this.button_check_left_limit.Margin = new System.Windows.Forms.Padding(2);
            this.button_check_left_limit.Name = "button_check_left_limit";
            this.button_check_left_limit.Size = new System.Drawing.Size(92, 37);
            this.button_check_left_limit.TabIndex = 73;
            this.button_check_left_limit.Text = "檢查左極限";
            this.button_check_left_limit.UseVisualStyleBackColor = false;
            this.button_check_left_limit.Click += new System.EventHandler(this.button_check_left_limit_Click);
            // 
            // Pixels_setting_textBox
            // 
            this.Pixels_setting_textBox.Font = new System.Drawing.Font("標楷體", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Pixels_setting_textBox.Location = new System.Drawing.Point(267, 44);
            this.Pixels_setting_textBox.Margin = new System.Windows.Forms.Padding(2);
            this.Pixels_setting_textBox.Name = "Pixels_setting_textBox";
            this.Pixels_setting_textBox.Size = new System.Drawing.Size(91, 30);
            this.Pixels_setting_textBox.TabIndex = 71;
            // 
            // Gohome_button
            // 
            this.Gohome_button.BackColor = System.Drawing.Color.LightSalmon;
            this.Gohome_button.Font = new System.Drawing.Font("標楷體", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.Gohome_button.Location = new System.Drawing.Point(13, 42);
            this.Gohome_button.Name = "Gohome_button";
            this.Gohome_button.Size = new System.Drawing.Size(99, 36);
            this.Gohome_button.TabIndex = 75;
            this.Gohome_button.Text = "回歸原點";
            this.Gohome_button.UseVisualStyleBackColor = false;
            this.Gohome_button.Click += new System.EventHandler(this.Gohome_button_Click);
            // 
            // LightCorrection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.ClientSize = new System.Drawing.Size(1085, 576);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.button_ExposureTimeSave);
            this.Controls.Add(this.textBox_ExposureTime);
            this.Controls.Add(this.label_ExposureTime);
            this.Controls.Add(this.button_VedioCam);
            this.Controls.Add(this.pictureBox_SourceImg);
            this.Controls.Add(this.pictureBox3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Name = "LightCorrection";
            this.Text = "光源校正";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.LightCorrection_FormClosed);
            this.Load += new System.EventHandler(this.LightCorrection_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_SourceImg)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox3;
        public System.Windows.Forms.PictureBox pictureBox_SourceImg;
        private System.Windows.Forms.Button button_VedioCam;
        private System.Windows.Forms.Button button_ExposureTimeSave;
        private System.Windows.Forms.TextBox textBox_ExposureTime;
        private System.Windows.Forms.Label label_ExposureTime;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button Motor_moves_right_button;
        private System.Windows.Forms.Button button_check_right_limit;
        private System.Windows.Forms.Button button_check_left_limit;
        private System.Windows.Forms.TextBox Pixels_setting_textBox;
        private System.Windows.Forms.Button Gohome_button;
    }
}