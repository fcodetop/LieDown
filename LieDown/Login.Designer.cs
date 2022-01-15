namespace LieDown
{
    partial class Login
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
            this.btn_Login = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.cmb_IDs = new System.Windows.Forms.ComboBox();
            this.txt_Pass = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lbl_Tips = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btn_Login
            // 
            this.btn_Login.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_Login.Location = new System.Drawing.Point(283, 56);
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(86, 26);
            this.btn_Login.TabIndex = 0;
            this.btn_Login.Text = "Login";
            this.btn_Login.UseVisualStyleBackColor = true;
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(22, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(23, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "ID";
            // 
            // cmb_IDs
            // 
            this.cmb_IDs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmb_IDs.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmb_IDs.FormattingEnabled = true;
            this.cmb_IDs.Location = new System.Drawing.Point(55, 19);
            this.cmb_IDs.Name = "cmb_IDs";
            this.cmb_IDs.Size = new System.Drawing.Size(356, 26);
            this.cmb_IDs.TabIndex = 2;
            this.cmb_IDs.SelectedIndexChanged += new System.EventHandler(this.cmb_IDs_SelectedIndexChanged);
            // 
            // txt_Pass
            // 
            this.txt_Pass.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_Pass.Location = new System.Drawing.Point(56, 56);
            this.txt_Pass.Name = "txt_Pass";
            this.txt_Pass.PasswordChar = '*';
            this.txt_Pass.Size = new System.Drawing.Size(211, 26);
            this.txt_Pass.TabIndex = 3;
            this.txt_Pass.Enter += new System.EventHandler(this.txt_Pass_Enter);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(6, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "Pass";
            // 
            // lbl_Tips
            // 
            this.lbl_Tips.AutoSize = true;
            this.lbl_Tips.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_Tips.ForeColor = System.Drawing.Color.Red;
            this.lbl_Tips.Location = new System.Drawing.Point(52, 99);
            this.lbl_Tips.Name = "lbl_Tips";
            this.lbl_Tips.Size = new System.Drawing.Size(37, 18);
            this.lbl_Tips.TabIndex = 1;
            this.lbl_Tips.Text = "Tips";
            // 
            // Login
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 137);
            this.Controls.Add(this.txt_Pass);
            this.Controls.Add(this.cmb_IDs);
            this.Controls.Add(this.lbl_Tips);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btn_Login);
            this.Name = "Login";
            this.Text = "Login";
            this.Load += new System.EventHandler(this.Login_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Login;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmb_IDs;
        private System.Windows.Forms.TextBox txt_Pass;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbl_Tips;
    }
}