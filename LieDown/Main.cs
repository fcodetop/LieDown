using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blocks;
using Libplanet.Tx;
using LieDown.Modles;
using MagicOnion.Client;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Nekoyume.Shared.Services;
using Nekoyume.TableData;
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
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly  BlockHash GenesisHash = BlockHash.FromString("4582250d0da33b06779a8475d283d5dd210c683b9b999d74d03fac4f58fa6bce");
        private Character avatar = Program.Agent.AvatarStates.MaxBy(x=>x.Level);
        public Address Address => Program.PrivateKey.PublicKey.ToAddress();
        private NodeInfo Node;
        private Channel _channel;
        private IBlockChainService _service;
        private Codec _codec = new Codec();
        private NodeStatus.Block _topBlock;
        public static GameConfigState GameConfigState;

        private CharacterSheet _characterSheet = new CharacterSheet();
        private CostumeStatSheet _costumeStatSheet = new CostumeStatSheet();

        private Nekoyume.ArenaHelper _arenaHelper;

        Dictionary<Address, AvatarState> _avatars = new Dictionary<Address, AvatarState>();
        Dictionary<Address,int> _cps=   new Dictionary<Address,int>();


        private ConcurrentDictionary<int, TxId> _StageTxs = new ConcurrentDictionary<int, TxId>();

        private Dictionary<string,Setting> _avatarSettings = new Dictionary<string, Setting>();


        public Main()
        {
            InitializeComponent();
            Node = Program.Nodes.Where(x => x.PreloadEnded).MinBy(x => x.PingDelay);
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            ConnectRpc();
            _topBlock = await Node.GetBlockIndexAsync();

            if (await GetStateAsync(GameConfigState.Address) is Dictionary configDict)
            {
                GameConfigState = new GameConfigState(configDict);
            }

            var characterSheetAddr = Nekoyume.Addresses.GetSheetAddress<CharacterSheet>();
            var costumeStatSheetAddr = Nekoyume.Addresses.GetSheetAddress<CostumeStatSheet>();

            var csv = await GetStateAsync(characterSheetAddr);
            _characterSheet.Set(csv.ToDotnetString());

            csv = await GetStateAsync(costumeStatSheetAddr);
            _costumeStatSheet.Set(csv.ToDotnetString());


            _avatars = await GetAvatayrStates(Program.Agent.AvatarStates.Select(x => new Address(x.AvatarAddress.Remove(0, 2))));
            foreach (var state in _avatars)
            {
                _cps[state.Key] = GetCP(state.Value);
                var addr=state.Key.ToString();
                _avatarSettings[addr] = Setting.LoadSetting(addr);
                if (_avatarSettings[addr].Stage == 0) {
                    _avatarSettings[addr].Stage = (state.Value.stageMap.Where(x => x.Key < 10000001).Max(x => x.Key)) + 1;
                }
            }

            BindBlock();
            BindAvatar();
           
            _arenaHelper = new Nekoyume.ArenaHelper(GameConfigState);

            var task = new Task(async () =>
              {
                  while (!this.Disposing)
                  {
                      await Task.Delay(5000);
                      await ComfirmTx();
                      await GetData();

                  }
              });
            task.Start();
            var task1 = new Task(async () =>
            {
                while (!this.Disposing)
                {
                    await Task.Delay(1000 * 60 * 3);

                    try
                    {
                        await WeeklyArena();
                    }
                    catch (Exception ex) 
                    { 
                        log.Error(ex, "WeeklyArena error");
                    }

                }
            });
            task1.Start();
            log.Info("start avatar{0}", avatar.AvatarAddress);

        }

        public int GetCP( AvatarState avatarState)
        {           
            return Nekoyume.Battle.CPHelper.GetCPV2(avatarState, _characterSheet, _costumeStatSheet);
        }


        private long _resetIndex=0;
        private bool challengeEnd=false;

        private async Task WeeklyArena()
        {            
            if (_resetIndex > 0 && _topBlock.Index > _resetIndex + GameConfigState.DailyArenaInterval)
            {
                //next interval
                challengeEnd = false;
            }
            if (challengeEnd) 
            {
                return;
            }
            //delay 800 blocks
            if ( _topBlock.Index - _resetIndex< 1400)
            {
                return;
            }
            var avatarAddress = new Address(avatar.AvatarAddress.Remove(0, 2));
             var action = new RankingBattle { avatarAddress = avatarAddress };
            if (_StageTxs.ContainsKey(GetHashCode(new List<NCAction>() { action })))
            {
                return;
            }

            if (_arenaHelper.TryGetThisWeekAddress(_topBlock.Index, out var address))
            {
                var state = await GetStateAsync(address);
                if (state != null)
                {
                  var  weeklyArenaState = new WeeklyArenaState(state); 
                    _resetIndex = weeklyArenaState.ResetIndex;                   
                    var arenaInfo = weeklyArenaState.GetArenaInfo(avatarAddress);
                    if (arenaInfo != null)
                    {
                        this.Invoke(() =>
                        {
                            lblLeftCount.Text =arenaInfo.DailyChallengeCount.ToString();
                            lblWin.Text = $"{arenaInfo.ArenaRecord.Win}/{arenaInfo.ArenaRecord.Lose}";

                        });
                    }
                    if (arenaInfo!=null&&arenaInfo.DailyChallengeCount <= 0)
                    {
                        log.Info("DailyChallengeEnd Avatar:{0}", avatar.AvatarAddress);
                        challengeEnd=true;
                    }
                    else
                    {
                        //delay 800 blocks
                        if ((_topBlock.Index - _resetIndex) < 1400)
                         {
                            return;
                        }
                        action.weeklyArenaAddress = address;

                       // offical rule
                        var infos2 = weeklyArenaState.GetArenaInfos(avatarAddress, 200, 10).Select(x=>x.arenaInfo);
                        // Player does not play prev & this week arena.
                        if (!infos2.Any() && weeklyArenaState.OrderedArenaInfos.Any())
                        {
                            var last = weeklyArenaState.OrderedArenaInfos.Last().AvatarAddress;
                            infos2 =weeklyArenaState.GetArenaInfos(last, 90, 0).Select(x => x.arenaInfo);
                        }

                       // var infos2 = weeklyArenaState.OrderedArenaInfos;

                        Address enemyAddress = default(Address);
                        int minCP = int.MaxValue;
                        foreach (var info in infos2) //auto match
                        {
                            if (!info.Active)
                                continue;
                            int cp;

                            if (_cps.ContainsKey(info.AvatarAddress))
                            {
                                cp = _cps[info.AvatarAddress];
                            }
                            else
                            {
                                var avatarState = (await GetAvatayrStates(new List<Address>() { info.AvatarAddress })).FirstOrDefault().Value;
                                cp = GetCP(avatarState);
                                _cps[info.AvatarAddress] = cp; //cache cp
                            }

                            if (_cps[avatarAddress] >= cp * 1.18)
                            {
                                enemyAddress = info.AvatarAddress;
                                break;
                            }
                            if (cp < minCP)
                            {
                                minCP = cp;
                                enemyAddress = info.AvatarAddress;
                            }
                        }
                        if (enemyAddress != default(Address))
                        {
                            action.enemyAddress = enemyAddress;
                            action.costumeIds = _avatars[avatarAddress].inventory.Costumes.Where(i => i.equipped).Select(i => i.ItemId).ToList();
                            action.equipmentIds = _avatars[avatarAddress].inventory.Equipments.Where(i => i.equipped).Select(i => i.ItemId).ToList();

                            TryRankingBattle(action);
                        }

                    }
                }
                else
                {
                    log.Warn("WeeklyArenaState is null");
                }
            }

        }
       

        public async Task<Dictionary<Address, AvatarState>> GetAvatayrStates(IEnumerable<Address> addressList)
        {
            Dictionary<byte[], byte[]> raw =
                await _service.GetAvatarStates(addressList.Select(a => a.ToByteArray()),
                    BlockHash.FromString(_topBlock.Hash).ToByteArray());
            var result = new Dictionary<Address, AvatarState>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = new AvatarState((Dictionary)_codec.Decode(kv.Value));
            }
            return result;
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
                new ClientFilter(log)
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
                         .ContinueWith(async (topBlock) => { _topBlock = await topBlock; BindBlock(); }) 
                         .ContinueWith(async (x) => {
                             avatar = await Character.GetCharacterAync(Node, avatar.AvatarAddress);
                             BindAvatar();
                         });
                         loopNode = false;
                         if(isNewConn||_channel.State== ChannelState.Shutdown)
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
                var key = new Address(avatar.AvatarAddress.Remove(0, 2));
                if (_cps.ContainsKey(key))
                    lblCP.Text = _cps[key].ToString();
                lblStage.Text = avatar.StageId.ToString();
            });
        }

        private  void BindBlock()
        {
            long topBlock = _topBlock.Index;           
            this.Invoke( () =>
            {
                lblBlock.Text = topBlock.ToString();
                if (_resetIndex > 0)
                    lblDailyBlock.Text = (topBlock - _resetIndex).ToString();
                var meter = topBlock - avatar.DailyRewardReceivedIndex;
                if (meter >= 1700)
                {
                    meter = 1700;
                    if (avatar.ActionPoint == 0&& _service!=null) 
                    {
                       TryDailyReward();
                    }
                }
                lblPMeter.Text = $"{meter}/1700";

            });
        }

        private async void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            await CloesRpc();
        }

        private SemaphoreSlim _singleTran = new SemaphoreSlim(1,1);
        private async Task<bool> MakeTransaction(List<NCAction> actions)
        {
            try
            {
                await _singleTran.WaitAsync();
                var nonce = await GetNonceAsync();
                var tx = NCTx.Create(
                    nonce,
                    Program.PrivateKey,
                   GenesisHash,
                    actions
                );
                if (_StageTxs.TryAdd(GetHashCode(actions), tx.Id))
                {
                    try
                    {
                        var result = await _service.PutTransaction(tx.Serialize(true));
                        if (!result)
                            throw new Exception("PutTransaction return false");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, $"PutTransaction error action:{actions.ToJson()}");
                        _StageTxs.TryRemove(new KeyValuePair<int, TxId>(GetHashCode(actions), tx.Id));
                    }
                }

                return false;
            }
           catch (Exception ex)
            {
                log.Error(ex, $"MakeTransaction error action:{actions.ToJson()}");               
                return false;
            }
            finally
            {
                _singleTran.Release();
            }
        }

        private async Task<bool> IsTransactionStaged(TxId txId) { 
        
            return await _service.IsTransactionStaged(txId.ToByteArray());
        }


        public async Task<IValue> GetStateAsync(Address address)
        {
            byte[] raw = await _service.GetState(address.ToByteArray(), BlockHash.FromString(_topBlock.Hash).ToByteArray());
            return _codec.Decode(raw);
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
                            if (_StageTxs.TryRemove(key, out  tx)&& result.TxStatus == TxStatus.SUCCESS) 
                            { 
                                //complete tx
                            }
                        }

                        //if (!await IsTransactionStaged(tx))
                        //{                        
                        //    _StageTxs.TryRemove(key, out var _);
                        //}

                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "GetTransaction error");
                      
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

        public async void TryDailyReward() 
        {
            var action = new DailyReward
            {
                avatarAddress =new Address(avatar.AvatarAddress.Remove(0,2))
            };
            var actions = new List<NCAction> { action };
            if (_StageTxs.ContainsKey(GetHashCode( actions)) )
            {
                return;
            }
            await MakeTransaction(actions);
        }

        public async void TryRankingBattle(RankingBattle action)
        {         
            var actions = new List<NCAction> { action };
            if (_StageTxs.ContainsKey(GetHashCode(actions)))
            {
                return;
            }
            await MakeTransaction(actions);
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            var frm = new FrmSetting();
            frm.AvatarAddress = avatar.AvatarAddress;
            frm.Setting = _avatarSettings[avatar.AvatarAddress];

            if (frm.ShowDialog(this) == DialogResult.OK) 
            {
                _avatarSettings[avatar.AvatarAddress] = frm.Setting;
            }
        }
        bool isFighting = false;
        private void btnStart_Click(object sender, EventArgs e)
        {
            isFighting = !isFighting;
            btnStart.Text = isFighting ? "stop" : "start";
           
            if (isFighting)
            {
                lblFightStatus.Text = "Starting...";
                Task.Run(async () =>
                {
                    while (isFighting)
                    {
                        await Task.Delay(30000);
                        if (!isFighting) {
                            return;
                        }
                        if (avatar.ActionPoint == 0)
                        {
                            this.Invoke(() =>lblFightStatus.Text="Waiting AP...");
                            continue;
                        }
                        else
                        {
                            this.Invoke(() => lblFightStatus.Text = "Fighting...");
                            var playCount = 1;
                            var setting = _avatarSettings[avatar.AvatarAddress];
                            var stageId = avatar.StageId;
                            if (setting.Mode == Modles.SlashMode.Bootstrap)
                            {
                                stageId = setting.Stage;
                                if (stageId > avatar.StageId)
                                {

                                    MessageBox.Show("prev stage is not clear");
                                    btnStart_Click(null, null);
                                    break;
                                }
                                playCount = avatar.ActionPoint / 50;
                                playCount = playCount > 8 ? 8 : playCount; //max 8
                            }
                            var worldId = getWorldId(stageId);

                            var avatarAddress = new Address(avatar.AvatarAddress.Remove(0, 2));

                            var action = new HackAndSlash
                            {
                                costumes = _avatars[avatarAddress].inventory.Costumes.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                                equipments = _avatars[avatarAddress].inventory.Equipments.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                                foods = new List<Guid>(),
                                worldId = worldId,
                                stageId = stageId,
                                playCount = playCount,
                                avatarAddress = avatarAddress,
                            };
                            var actions = new List<NCAction>() { action };
                            if (_StageTxs.ContainsKey(GetHashCode(actions)))
                            {
                                continue;
                            }
                            await MakeTransaction(actions);
                        }
                    }
                });
            }
            else {

                lblFightStatus.Text = "Stopped";
            }
            
            

        }
        private int getWorldId(int stageId) 
        {
            double d = 50;
            return (int)Math.Ceiling((double)stageId / d);
        
        }
    }

   

    public class ClientFilter : IClientFilter
    {
        NLog.Logger _log;
        public ClientFilter(NLog.Logger log)
        {
            this._log = log;
        }

        public async ValueTask<ResponseContext> SendAsync(RequestContext context, Func<RequestContext, ValueTask<ResponseContext>> next)
        {
            var retryCount = 0;          
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
                    _log.Error(e, $"ClientFilter error count:{retryCount}");                
                }
            }            
            return null;
        }
    }
}
