using Libplanet.KeyStore;
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
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }
         IKeyStore _keyStore = Web3KeyStore.DefaultKeyStore;
        private void Login_Load(object sender, EventArgs e)
        {
            foreach (var pair in _keyStore.List())
            {

                cmb_IDs.Items.Add(pair.Item2);
            }
            if (cmb_IDs.Items.Count > 0)
            {
                cmb_IDs.DisplayMember = "Address";
                cmb_IDs.SelectedIndex = 0;
            }
            lbl_Tips.Visible = false;

        }
        private async void btn_Login_Click(object sender, EventArgs e)
        {
            try
            {

                var ppk = cmb_IDs.SelectedItem as ProtectedPrivateKey;
                if (ppk == null) {

                    lbl_Tips.Text = "No ID!";
                    lbl_Tips.Visible = true;
                    return;
                }
                var privateKey = ppk.Unprotect(txt_Pass.Text);
                Program.PrivateKey = privateKey;

                var node = Program.Nodes.Where(x => x.PreloadEnded).OrderBy(x => x.PingDelay).FirstOrDefault();
                var agent = await Modles.Agent.GetAgent(node,ppk.Address.ToString());
                Program.Agent= agent;
                this.DialogResult = DialogResult.OK;
            }
            catch (IncorrectPassphraseException)
            {
                lbl_Tips.Text = "Invalid Password";
                lbl_Tips.Visible = true;
            }

           
        }

        private void cmb_IDs_SelectedIndexChanged(object sender, EventArgs e)
        {
            txt_Pass_Enter(null, null);
        }

        private void txt_Pass_Enter(object sender, EventArgs e)
        {
            lbl_Tips.Visible = false;
        }
    }
}
