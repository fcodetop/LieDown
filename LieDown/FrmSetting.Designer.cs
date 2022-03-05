namespace LieDown
{
    partial class FrmSetting
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            this.chAutoFill = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.txtStage = new System.Windows.Forms.TextBox();
            this.rbBootstrap = new System.Windows.Forms.RadioButton();
            this.rbProgress = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // chAutoFill
            // 
            this.chAutoFill.AutoSize = true;
            this.chAutoFill.Location = new System.Drawing.Point(95, 111);
            this.chAutoFill.Name = "chAutoFill";
            this.chAutoFill.Size = new System.Drawing.Size(85, 21);
            this.chAutoFill.TabIndex = 2;
            this.chAutoFill.Text = "AutoFillAp";
            this.chAutoFill.UseVisualStyleBackColor = true;          
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(45, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 17);
            this.label2.TabIndex = 99;
            this.label2.Text = "Stage:";         
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(95, 147);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // txtStage
            // 
            this.txtStage.Location = new System.Drawing.Point(95, 76);
            this.txtStage.Name = "txtStage";
            this.txtStage.Size = new System.Drawing.Size(100, 23);
            this.txtStage.TabIndex = 1;          
            // 
            // rbBootstrap
            // 
            this.rbBootstrap.AutoSize = true;
            this.rbBootstrap.Location = new System.Drawing.Point(90, 22);
            this.rbBootstrap.Name = "rbBootstrap";
            this.rbBootstrap.Size = new System.Drawing.Size(84, 21);
            this.rbBootstrap.TabIndex = 100;
            this.rbBootstrap.TabStop = true;
            this.rbBootstrap.Text = "Bootstrap";
            this.rbBootstrap.UseVisualStyleBackColor = true;
            // 
            // rbProgress
            // 
            this.rbProgress.AutoSize = true;
            this.rbProgress.Location = new System.Drawing.Point(6, 22);
            this.rbProgress.Name = "rbProgress";
            this.rbProgress.Size = new System.Drawing.Size(78, 21);
            this.rbProgress.TabIndex = 100;
            this.rbProgress.TabStop = true;
            this.rbProgress.Text = "Progress";
            this.rbProgress.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbProgress);
            this.groupBox1.Controls.Add(this.rbBootstrap);
            this.groupBox1.Location = new System.Drawing.Point(45, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(182, 54);
            this.groupBox1.TabIndex = 101;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Mode";
            // 
            // FrmSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(296, 198);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtStage);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chAutoFill);
            this.Controls.Add(this.label2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSetting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Setting";
            this.Load += new System.EventHandler(this.FrmSetting_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private CheckBox chAutoFill;
        private Label label2;
        private Button btnSave;
        private TextBox txtStage;
        private RadioButton rbBootstrap;
        private RadioButton rbProgress;
        private GroupBox groupBox1;
    }
}