using LieDown.Modles;
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
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public Preload()
        {
            InitializeComponent();
        }

        private async  void Preload_Load(object sender, EventArgs e)
        {
            var nodeList = NodeInfo.GetDefaultNodeList();
            bool retry = false;          
            do
            {
                this.BeginInvoke(() => this.progressBar1.Value = 10);
                await await Task.WhenAll(nodeList.Select(x => x.GetPreloadEndedAsync()))
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
                             if ((Program.PrivateKey?.Equals(null)).HasValue)  //delay and retry
                             {
                                 await Task.Delay(1000 * 60 * 5);
                                 retry = true;
                                 log.Warn("No valiable RPC node retry ...");
                                 return;
                             }
                             MessageBox.Show("No valiable RPC node, application exit!");
                         }
                         retry = false;
                        this.Invoke(() => this.DialogResult = dialogResult);

                     });
            }
            while (retry);

        }

        private void SetText(string text) 
        {
            this.BeginInvoke(new MethodInvoker(()=>this.Text = text));

        }
    }
}
