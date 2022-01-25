using Bencodex;
using Grpc.Core;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Tx;
using LieDown.Modles;
using MagicOnion.Client;
using Nekoyume.Action;
using Nekoyume.Shared.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using NCTx = Libplanet.Tx.Transaction<Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>>;

namespace LieDown
{
    public partial class Main : Form
    {
        private readonly static BlockHash GenesisHash = BlockHash.FromString("4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce");
        private Character avatar = Program.Agent.AvatarStates[0];
        public Address Address => Program.PrivateKey.PublicKey.ToAddress();
        private NodeInfo Node;
        private Channel _channel;
        private IBlockChainService _service;
        private Codec _codec = new Codec();

        private ConcurrentDictionary<int, TxId> _StageTxs = new ConcurrentDictionary<int, TxId>();

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

            var task = new Task(async () =>
              {
                  while (!this.Disposing)
                  {
                      await Task.Delay(5000);
                      await Task.WhenAll(GetData(), ComfirmTx());
                  }
              });
            task.Start();

            ConnectRpc();

        }

        private void ConnectRpc()
        {
            _channel = new Channel(
                 Node.Host,
                  Node.RpcPort,
                  ChannelCredentials.Insecure,
                  new[]
                  {
                    new ChannelOption("grpc.max_receive_message_length", -1)
                  }
              );
            _service = MagicOnionClient.Create<IBlockChainService>(_channel, new IClientFilter[]
            {
                new ClientFilter()
            }).WithCancellationToken(_channel.ShutdownToken);
        }
        private async Task CloesRpc()
        {
            if (_channel != null)
                await _channel.ShutdownAsync();
        }

        private async Task GetData()
        {
            var loopNode = true;
            var isNewConn = false;
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
                         if(isNewConn)
                             ConnectRpc();
                     }
                     else  //切换节点
                     {                       
                         Node = Program.Nodes.Where(x => x.PreloadEnded).MinBy(x => x.PingDelay);
                         await CloesRpc();
                         isNewConn = true;
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
                var meter = topBlock - avatar.DailyRewardReceivedIndex;
                if (meter >= 1700)
                {
                    meter = 1700;
                    if (avatar.ActionPoint == 0) 
                    { 
                      //auto add AP

                    }
                }
                lblPMeter.Text = $"{meter}/1700";

            });
        }

        private async void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            await CloesRpc();
        }

        private async Task MakeTransaction(List<NCAction> actions)
        {
            var nonce = await GetNonceAsync();
            var tx = NCTx.Create(
                nonce,
                Program.PrivateKey,
               GenesisHash,
                actions
            );
          
            await _service.PutTransaction(tx.Serialize(true));
            _StageTxs.TryAdd(GetHashCode(actions), tx.Id);
        }

        private async Task ComfirmTx() 
        {
            foreach (var key in _StageTxs.Keys) 
            {
                if (_StageTxs.TryGetValue(key, out var tx)) 
                {
                    try
                    {
                        var result = await Node.GetTransactionResultAsync(tx.ToString());
                        if (result.TxStatus == TxStatus.FAILURE || result.TxStatus == TxStatus.SUCCESS)
                        {
                            _StageTxs.TryRemove(key, out var _);
                        }
                    }
                    catch (Exception ex)
                    {

                        Debug.WriteLine(ex);
                    }
                }
            }
        }

        private int GetHashCode(List<NCAction> actions) 
        {
            return string.Join("-", actions.Select(x => x.ToString()).OrderBy(x => x)).GetHashCode();
        }

        private async Task<long> GetNonceAsync()
        {
            return await _service.GetNextTxNonce( Address.ToByteArray());
        }

        public async Task TryDailyReward() 
        {
            var action = new DailyReward
            {
                avatarAddress =Address,
            };
            var actions = new List<NCAction> { action };
            if (_StageTxs.ContainsKey(GetHashCode( actions)) )
            {
                return;
            }
            await MakeTransaction(actions);
        }
    }

    public class ClientFilter : IClientFilter
    {
        public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
        {
            var retryCount = 0;
            Exception exception = null;
            while (retryCount < 3)
            {
                try
                {
                    return await next(context);
                }
                catch (Exception e)
                {
                    await Task.Delay((3 - retryCount) * 1000);
                    retryCount++;
                    exception = e;
                }
            }
            Debug.WriteLine($"Filter Catch Exception: {exception}");
            return null;
        }
    }
}
