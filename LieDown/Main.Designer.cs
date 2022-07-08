namespace LieDown
{
    partial class Main
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label11 = new System.Windows.Forms.Label();
            this.lblBlock = new System.Windows.Forms.Label();
            this.lblBlockDev = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblDailyBlock = new System.Windows.Forms.Label();
            this.lblDailyBlockt = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(42, 15);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(70, 17);
            this.label11.TabIndex = 0;
            this.label11.Text = "Top Block:";
            // 
            // lblBlock
            // 
            this.lblBlock.AutoSize = true;
            this.lblBlock.Location = new System.Drawing.Point(118, 15);
            this.lblBlock.Name = "lblBlock";
            this.lblBlock.Size = new System.Drawing.Size(52, 17);
            this.lblBlock.TabIndex = 1;
            this.lblBlock.Text = "loading";
            // 
            // lblBlockDev
            // 
            this.lblBlockDev.AutoSize = true;
            this.lblBlockDev.Location = new System.Drawing.Point(293, 15);
            this.lblBlockDev.Name = "lblBlockDev";
            this.lblBlockDev.Size = new System.Drawing.Size(52, 17);
            this.lblBlockDev.TabIndex = 3;
            this.lblBlockDev.Text = "loading";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(193, 15);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(94, 17);
            this.label14.TabIndex = 2;
            this.label14.Text = "Top Block.dev:";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(543, 12);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 6;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(4, 42);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(684, 346);
            this.panel1.TabIndex = 8;
            // 
            // lblDailyBlock
            // 
            this.lblDailyBlock.AutoSize = true;
            this.lblDailyBlock.Location = new System.Drawing.Point(453, 15);
            this.lblDailyBlock.Name = "lblDailyBlock";
            this.lblDailyBlock.Size = new System.Drawing.Size(52, 17);
            this.lblDailyBlock.TabIndex = 24;
            this.lblDailyBlock.Text = "loading";
            // 
            // lblDailyBlockt
            // 
            this.lblDailyBlockt.AutoSize = true;
            this.lblDailyBlockt.Location = new System.Drawing.Point(351, 15);
            this.lblDailyBlockt.Name = "lblDailyBlockt";
            this.lblDailyBlockt.Size = new System.Drawing.Size(98, 17);
            this.lblDailyBlockt.TabIndex = 23;
            this.lblDailyBlockt.Text = "ArenaBlockLeft:";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 480);
            this.Controls.Add(this.lblDailyBlock);
            this.Controls.Add(this.lblDailyBlockt);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.lblBlockDev);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.lblBlock);
            this.Controls.Add(this.label11);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Main";
            this.Text = "v100241";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Main_FormClosed);
            this.Load += new System.EventHandler(this.Main_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label label11;
        private Label lblBlock;
        private Label lblBlockDev;
        private Label label14;
        private Button btnRefresh;
        private Panel panel1;
        private Label lblDailyBlock;
        private Label lblDailyBlockt;
    }
}

