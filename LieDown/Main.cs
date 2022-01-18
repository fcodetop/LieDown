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
    public partial class Main : Form
    {
        private const string GenesisHash = "4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce";        
        private Character avatar = Program.Agent.AvatarStates[0];
        private NodeInfo Node;

        public Main()
        {
            InitializeComponent();           
            Node = Program.Nodes.Where(x => x.PreloadEnded).MaxBy(x => x.PingDelay);       
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            var topBlock = await Node.GetBlockIndexAsync();
            BindAvatar();
            BindBlock(topBlock);

            var task=new Task(async () =>
            {
                while (!this.Disposing)
                {
                  await  GetData();
                  await Task.Delay(5000);
                }
            });
            task.Start();
        }

        private async Task GetData() 
        {
            bool loopNode = true;
            while (loopNode)
            {
                await Node.GetPreloadEndedAsync().ContinueWith(async (preloadEnded) =>
                 {
                     if (await preloadEnded)
                     {
                         await Node.GetBlockIndexAsync()
                         .ContinueWith(async (topBlock) => BindBlock(await topBlock))
                         .ContinueWith(async (x) => avatar = await Character.GetCharacterAync(Node, avatar.AvatarAddress));
                         loopNode = false;
                     }
                     else  //切换节点
                {
                         Node = Program.Nodes.Where(x => x.PreloadEnded).MaxBy(x => x.PingDelay);
                     }
                 });
            }
        }

        private void BindAvatar()
        {
            this.Invoke(() =>
            {

                lblAP.Text = avatar.ActionPoint.ToString();
                lblBlockDev.Text = "";
                lblExp.Text = avatar.Exp.ToString();
                lblLevel.Text = avatar.Level.ToString();
                lblName.Text = avatar.Name;
                lblStage.Text = (avatar.StageMap.Pairs.Where(x => x[0] < 10000001).Max(x => x[0]) + 1).ToString();
            });
        }

        private void BindBlock(long topBlock)
        {
            this.Invoke(() =>
            {
                lblBlock.Text = topBlock.ToString();
                lblPMeter.Text = $"{topBlock - avatar.DailyRewardReceivedIndex}/1700";
            });
        }

    }
}
