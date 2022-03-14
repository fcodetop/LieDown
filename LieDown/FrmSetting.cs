using System;
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
            txtRank.Text = Setting.Rank.ToString();
            txtRBIndex.Text = Setting.RankingBattleBlockIndex == 0 ? "10" : Setting.RankingBattleBlockIndex.ToString();
            switch (Setting.Mode)
            {
                case Modles.SlashMode.Progress:
                    rbProgress.Checked = true;
                    break;
                case Modles.SlashMode.Bootstrap:
                    rbBootstrap.Checked = true;
                    break;

            }
            //  clBMode.SelectedIndex = (int)Setting.Mode;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            Setting.AutoFillAP = chAutoFill.Checked;
            Setting.Stage = int.Parse(txtStage.Text);
            Setting.Rank = int.Parse(txtRank.Text);
            var index=int.Parse(txtRBIndex.Text);
            if (index < 10) 
            {
                MessageBox.Show(this, "RankingBattleBlockIndex must greater than 10", "Error");
                return;
            }
            Setting.RankingBattleBlockIndex = index;
            //  Setting.Mode = (Modles.SlashMode)clBMode.SelectedIndex;
            Setting.Mode = rbProgress.Checked ? Modles.SlashMode.Progress : Modles.SlashMode.Bootstrap;


            Setting.Save(AvatarAddress);

            this.DialogResult = DialogResult.OK;

        }
    }
}
