﻿using Bencodex;
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
using Nekoyume.UI.Model;
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

        private Dictionary<Address, Avatar> _avatarCtrls=new Dictionary<Address, Avatar>();

        public Main()
        {
            InitializeComponent();            
        }

        private async void Main_Load(object sender, EventArgs e)
        {           
            var testOK = false;
            foreach (var node in Program.Nodes.Where(x => x.PreloadEnded).OrderBy(x => x.PingDelay))
            {
                Node = node;
                ConnectRpc();
                try
                {
                    await _service.GetTip();
                    testOK = true;
                    break;
                }
                catch
                {
                    await CloesRpc();
                    await Task.Delay(1000);
                }

            }

            if (!testOK) {
                log.Error("No PRC node is valid auto restart!");
                Program.Start();
                Application.Exit();            
            }

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


            _avatars = await GetAvatayrStates(Program.Agent.AvatarStates.Select(x => x.Address));

            var location = default(Point);

            foreach (var state in _avatars)
            {
                _cps[state.Key] = GetCP(state.Value);
                var addr = state.Key.ToString();
                _avatarSettings[addr] = Setting.LoadSetting(addr);
                if (_avatarSettings[addr].Stage == 0)
                {
                    _avatarSettings[addr].Stage = (state.Value.stageMap.Where(x => x.Key < 10000001).Max(x => x.Key)) + 1;
                }

                var avatar = Program.Agent.AvatarStates.First(x => x.AvatarAddress == addr);

                Avatar avatarc = new Avatar();
                avatarc.Character =avatar;
                avatarc.AvatarSetting = _avatarSettings[avatar.AvatarAddress];

                if (location != default(Point))
                {
                    avatarc.Location = location;
                }

                this.panel1.Controls.Add(avatarc);
                _avatarCtrls.Add(avatar.Address, avatarc);

                avatarc.BindAvatar(_cps[avatar.Address], _topBlock);
                avatarc.SetEnable(true);

                avatarc.OnDailyReward = TryDailyReward;
                avatarc.TryHackAndSlash = TryHackAndSlash;
                avatarc.OnSettingSave = (addr, setting) => _avatarSettings[addr] = setting;
                location = avatarc.Location;
                location.Y += avatarc.Height;

            }



            BindBlock();
           // BindAvatar();         
           
            _arenaHelper = new Nekoyume.ArenaHelper(GameConfigState);

            var task = new Task(async () =>
              {
                  while (!this.Disposing)
                  {
                      await Task.Delay(13000);
                      await ComfirmTx();
                      await GetData();

                  }
              });
            task.Start();
            var task1 = new Task(async () =>
            {
                while (!this.Disposing)
                {
                    await Task.Delay(1000 * 60);
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

            foreach (var addr in Program.FightingList) 
            {
                if (_avatarCtrls.TryGetValue(new Address(addr.Remove(0, 2)), out var avatarc)) 
                { 
                
                    avatarc.btnStart_Click(null,null);
                
                }
            }

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

           //todo every avatar
            var delayIndex = _avatarSettings[Program.Agent.AvatarStates.MaxBy(x=>x.Level).AvatarAddress].RankingBattleBlockIndex;

            delayIndex = delayIndex == 0 ? 2600 : delayIndex;

            //delay 2600 blocks
            if (_topBlock.Index - _resetIndex < delayIndex)
            {
                return;
            }           
            var action = new RankingBattle();
            if (_StageTxs.ContainsKey(GetHashCode(new List<NCAction>() { action })))
            {
                return;
            }

            if (!await GetWeeklyArenInfo(Program.Agent.AvatarStates.Select(x=>x.Address),
                  async (address, weeklyArenaState, index, arenaInfo) =>
               {
                   //if (arenaInfo != null && arenaInfo.DailyChallengeCount <= 0)
                   //{
                   //    log.Info("DailyChallengeEnd Avatar:{0}", arenaInfo.AvatarAddress);
                   //    challengeEnd = true;
                   //}
                   //else
                   //{
                        //delay 2600 blocks
                        if ((_topBlock.Index - _resetIndex) < delayIndex)
                       {
                           return;
                       }
                        var avatarAddress= arenaInfo.AvatarAddress;
                       action.weeklyArenaAddress = address;
                       action.avatarAddress = avatarAddress;
                       var up = 200;
                       var low = 10;
                       var max = 800;

                       var rank = _avatarSettings[avatarAddress.ToString()].Rank;

                       if (rank > 0 && index > rank)
                       {
                           up = index - rank < up ? up : index - rank;
                       }
                       else
                       {
                           if (index > up * 2)
                           {
                               up = index / 2;
                           }
                           else if (index < up && index > 0)
                           {
                               up = index;
                           }
                       }
                       low = max - up;

                       var infos2 = weeklyArenaState.GetArenaInfos(avatarAddress, up, low).Select(x => x.arenaInfo);
                        // Player does not play prev & this week arena.
                        if (!infos2.Any() && weeklyArenaState.OrderedArenaInfos.Any())
                       {
                           var last = weeklyArenaState.OrderedArenaInfos.Last().AvatarAddress;
                           infos2 = weeklyArenaState.GetArenaInfos(last, 90, 0).Select(x => x.arenaInfo);
                       }

                        // var infos2 = weeklyArenaState.OrderedArenaInfos;

                        Address enemyAddress = default(Address);
                       int minCP = int.MaxValue;
                       foreach (var info in infos2) //auto match
                        {
                           //if (!info.Active)
                           //    continue;
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

                           if (_cps[avatarAddress] >= cp * 1.2)
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

                  // }

               }))
            {

                log.Warn("WeeklyArenaState is null");
            }

        }
        ArenaInfoList _arenaInfoList=new ArenaInfoList();
        private async Task<bool> GetWeeklyArenInfo(IEnumerable<Address> avatarAddresses, Action<Address, ArenaInfoList, int, ArenaInfo> callback)
        {
            if (_arenaHelper.TryGetThisWeekAddress(_topBlock.Index, out var address))
            {               
                var state = await GetStateAsync(address);
                if (state != null)
                {
                    var weeklyArenaState = new WeeklyArenaState(state);
                    _arenaInfoList.Update(weeklyArenaState, false);
                    _resetIndex = weeklyArenaState.ResetIndex;

                    var rawList =
                   await GetStateAsync(
                       weeklyArenaState.address.Derive("address_list"));
                    if (rawList is List list)
                    {
                        List<Address> avatarAddressList = list.ToList(StateExtensions.ToAddress);
                        List<Address> arenaInfoAddressList = new List<Address>();
                        foreach (var avatarAddress in avatarAddressList)
                        {
                            var arenaInfoAddress = weeklyArenaState.address.Derive(avatarAddress.ToByteArray());
                            if (!arenaInfoAddressList.Contains(arenaInfoAddress))
                            {
                                arenaInfoAddressList.Add(arenaInfoAddress);
                            }
                        }
                        Dictionary<Address, IValue> result = await GetStateBulkAsync(arenaInfoAddressList);
                        var infoList = new List<ArenaInfo>();
                        foreach (var iValue in result.Values)
                        {
                            if (iValue is Dictionary dictionary)
                            {
                                var info = new ArenaInfo(dictionary);                              
                                infoList.Add(info);
                            }
                        }
                       
                        _arenaInfoList.Update(infoList);                       
                    }
                    challengeEnd=true;
                    foreach (var avatarAddress in avatarAddresses)
                    {

                        var index = -1;
                        var arenaInfo = _arenaInfoList[avatarAddress];
                        if (arenaInfo != null)
                        {
                            index = _arenaInfoList.OrderedArenaInfos.FindIndex(x => x.AvatarAddress.Equals(avatarAddress));

                            if (_avatarCtrls.TryGetValue(avatarAddress, out var avatarCtrl))
                            {
                                avatarCtrl.BindArenaInfo(index, arenaInfo);
                            }

                            //this.Invoke(() =>
                            //{
                            //    lblLeftCount.Text = arenaInfo.DailyChallengeCount.ToString();
                            //    lblWin.Text = $"{arenaInfo.ArenaRecord.Win}/{arenaInfo.ArenaRecord.Lose}";
                            //    lblScore.Text = arenaInfo.Score.ToString();
                            //    lblRank.Text = index.ToString();

                            //});

                        }
                        else 
                        {
                            arenaInfo = new ArenaInfo(_avatars[avatarAddress], _characterSheet, true);
                        }
                        var isEnd = arenaInfo.DailyChallengeCount <= 0;
                        challengeEnd = challengeEnd & isEnd;
                        if (!isEnd)
                            callback?.Invoke(address, _arenaInfoList, index, arenaInfo);
                    }
                    return true;
                }
            }
            return false;
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
            var filter = new ClientFilter(log);          
            _service = MagicOnionClient.Create<IBlockChainService>(_channel, new IClientFilter[]
            {
               filter
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
                if (Node == null)
                {
                    await Task.Delay(1000 * 60*3);
                    await Task.WhenAll(Program.Nodes.Select(x => x.GetPreloadEndedAsync()));
                    Node = Program.Nodes.Where(x => x.PreloadEnded).MinBy(x => x.PingDelay);
                    continue;
                }
                await await Node.GetPreloadEndedAsync().ContinueWith(async (preloadEnded) =>
                 {
                     if (await preloadEnded)
                     {
                       await await Node.GetBlockIndexAsync()
                         .ContinueWith(async (topBlock) => { _topBlock = await topBlock; BindBlock(); })
                         .ContinueWith(async (x) =>
                         {
                             var agent = await Agent.GetAgent(Node, Program.Agent.Address);
                             foreach (var character in agent.AvatarStates) 
                             {

                                 if (_avatarCtrls.TryGetValue(character.Address, out var avatarc)) 
                                 {
                                     avatarc.Character = character;
                                     avatarc.BindAvatar(_cps[character.Address],_topBlock);
                                 }
                             
                             }
                             //avatar = await Character.GetCharacterAync(Node, avatar.AvatarAddress);
                             //BindAvatar();
                            

                         });
                         loopNode = false;
                         if (isNewConn || _channel.State == ChannelState.Shutdown)
                             ConnectRpc();
                     }
                     else  //切换节点
                     {  
                         log.Warn("RPC Node Switch");
                         Node = Program.Nodes.Where(x => x.PreloadEnded).MinBy(x => x.PingDelay);
                         await CloesRpc();
                         isNewConn = true;                       
                     }
                 });
            }
        }

       

        private  void BindBlock()
        {
            long topBlock = _topBlock.Index;           
            this.Invoke( () =>
            {
                lblBlockDev.Text = "";
                lblBlock.Text = topBlock.ToString();
                if (_resetIndex > 0)
                    lblDailyBlock.Text = (topBlock - _resetIndex).ToString(); 
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
            try
            {
                byte[] raw = await _service.GetState(address.ToByteArray(), BlockHash.FromString(_topBlock.Hash).ToByteArray());
                return _codec.Decode(raw);
            }
            catch (System.InvalidOperationException)
            {
               var nodes = Program.Nodes.Where(x => x.PreloadEnded).ToList();
                ReConn:
                await CloesRpc();
                var host = Node.Host;
                nodes.RemoveAll(x => x.Host == host);
                if (nodes.Count == 0) 
                {
                    await Task.Delay(1000 * 60 * 5);
                    Program.Start();
                    Application.Exit();
                    return null;

                }
                Node = nodes.First();
                ConnectRpc();

                try
                {
                   await _service.GetTip();    
                }              
                catch (Exception) 
                {
                    await Task.Delay(1000 * 30);
                    goto ReConn;  
                }

                return null;
            }
            catch (Exception){
                throw;
            }
        }

        public async Task<Dictionary<Address, IValue>> GetStateBulkAsync(IEnumerable<Address> addressList)
        {
            Dictionary<byte[], byte[]> raw =
                await _service.GetStateBulk(addressList.Select(a => a.ToByteArray()),
                   BlockHash.FromString(_topBlock.Hash).ToByteArray());
            var result = new Dictionary<Address, IValue>();
            foreach (var kv in raw)
            {
                result[new Address(kv.Key)] = _codec.Decode(kv.Value);
            }
            return result;
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

        private DateTime _dailyRewardLock = DateTime.Now.AddMinutes(-30);
        public async void TryDailyReward(Address avatarAddress)
        {           
            if (_dailyRewardLock.AddMinutes(3) > DateTime.Now)
            {
                return;
            }
            var action = new DailyReward
            {
                avatarAddress = avatarAddress
            };
            var actions = new List<NCAction> { action };
            if (_StageTxs.ContainsKey(GetHashCode(actions)))
            {
                return;
            }
            if (await MakeTransaction(actions))
            {
                _dailyRewardLock = DateTime.Now;
                log.Info("DailyReward submit sucess avatar:{0}", avatarAddress);
            }

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

        public async Task<bool> TryHackAndSlash(int stageId,int playCount, Address avatarAddress)
        {
            var worldId = getWorldId(stageId);
            NCAction action;
            if (playCount > 1)
            {
                action = new HackAndSlashSweep
                {
                    worldId = worldId,
                    stageId = stageId,
                    avatarAddress = avatarAddress,
                    costumes = _avatars[avatarAddress].inventory.Costumes.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                    equipments = _avatars[avatarAddress].inventory.Equipments.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                    actionPoint = playCount * 5
                };
            }
            else
            {

                action = new HackAndSlash
                {
                    costumes = _avatars[avatarAddress].inventory.Costumes.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                    equipments = _avatars[avatarAddress].inventory.Equipments.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                    foods = new List<Guid>(),
                    worldId = worldId,
                    stageId = stageId,
                    // playCount = playCount,
                    avatarAddress = avatarAddress,
                };
            }

            
            var actions = new List<NCAction>() { action };
            if (_StageTxs.ContainsKey(GetHashCode(actions)))
            {
                return false;
            }
            return await MakeTransaction(actions);
        }

        private int getWorldId(int stageId) 
        {
            double d = 50;
            return (int)Math.Ceiling((double)stageId / d);
        
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            btnRefresh.Enabled = false;
            btnRefresh.Text = "loading";
            _avatars = await GetAvatayrStates(Program.Agent.AvatarStates.Select(x => x.Address));
            foreach (var state in _avatars)
            {
                _cps[state.Key] = GetCP(state.Value);           
                Avatar avatarc = _avatarCtrls[avatar.Address];
                avatarc.BindAvatar(_cps[avatar.Address], _topBlock);
            }

            if (!await GetWeeklyArenInfo(Program.Agent.AvatarStates.Select(x=>x.Address), null)) 
            {
                MessageBox.Show("WeeklyArenaState is null.");
            }

            btnRefresh.Enabled= true;
            btnRefresh.Text = "refresh";
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
            // todo switch node
        }
    }
}
