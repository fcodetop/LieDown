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
    public partial class Preload : Form
    {
        public Preload()
        {
            InitializeComponent();
        }

        private  void Preload_Load(object sender, EventArgs e)
        {
            var nodeList = NodeInfo.GetDefaultNodeList();
            this.BeginInvoke(() => this.progressBar1.Value = 10);
            Task.WhenAll(nodeList.Select(x => x.GetPreloadEndedAsync()))
                 .ContinueWith(async (tasks) =>
                 {
                     this.BeginInvoke(() => this.progressBar1.Value = 50);
                     var dialogResult = DialogResult.Cancel;
                     var results = await tasks;
                     this.BeginInvoke(() => this.progressBar1.Value = 99);
                     await Task.Delay(300);
                     if (results.Any(x => x))
                     {
                         Program.Nodes = nodeList.ToList();
                         dialogResult = DialogResult.OK;
                     }
                     else
                     {
                         MessageBox.Show("No valiable RPC node, application exit!");
                     }
                     this.Invoke(() => this.DialogResult = dialogResult);

                 });


        }

        private void SetText(string text) 
        {
            this.BeginInvoke(new MethodInvoker(()=>this.Text = text));

        }
    }
}
