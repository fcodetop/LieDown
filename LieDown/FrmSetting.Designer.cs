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
            this.clBMode = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chAutoFill = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.txtStage = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // clBMode
            // 
            this.clBMode.FormattingEnabled = true;
            this.clBMode.Items.AddRange(new object[] {
            "Progress",
            "Bootstrap"});
            this.clBMode.Location = new System.Drawing.Point(110, 22);
            this.clBMode.Name = "clBMode";
            this.clBMode.Size = new System.Drawing.Size(89, 40);
            this.clBMode.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(45, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 99;
            this.label1.Text = "Mode:";
            // 
            // chAutoFill
            // 
            this.chAutoFill.AutoSize = true;
            this.chAutoFill.Location = new System.Drawing.Point(110, 116);
            this.chAutoFill.Name = "chAutoFill";
            this.chAutoFill.Size = new System.Drawing.Size(85, 21);
            this.chAutoFill.TabIndex = 2;
            this.chAutoFill.Text = "AutoFillAp";
            this.chAutoFill.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(45, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 17);
            this.label2.TabIndex = 99;
            this.label2.Text = "Stage:";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(110, 157);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // txtStage
            // 
            this.txtStage.Location = new System.Drawing.Point(110, 78);
            this.txtStage.Name = "txtStage";
            this.txtStage.Size = new System.Drawing.Size(100, 23);
            this.txtStage.TabIndex = 1;
            // 
            // FrmSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(296, 229);
            this.Controls.Add(this.txtStage);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chAutoFill);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.clBMode);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSetting";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Setting";
            this.Load += new System.EventHandler(this.FrmSetting_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CheckedListBox clBMode;
        private Label label1;
        private CheckBox chAutoFill;
        private Label label2;
        private Button btnSave;
        private TextBox txtStage;
    }
}