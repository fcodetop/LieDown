﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LieDown
{
    public partial class FrmSetting : Form
    {
        public FrmSetting()
        {
            InitializeComponent();
        }

        public string AvatarAddress { get; set; }

        public Modles.Setting Setting { get; set; }
        private void FrmSetting_Load(object sender, EventArgs e)
        {
            chAutoFill.Checked = Setting.AutoFillAP;
            txtStage.Text = Setting.Stage.ToString();
            clBMode.SelectedIndex = (int)Setting.Mode;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            Setting.AutoFillAP = chAutoFill.Checked;
            Setting.Stage = int.Parse(txtStage.Text);
            Setting.Mode = (Modles.SlashMode)clBMode.SelectedIndex;

           



        }


    }
}
