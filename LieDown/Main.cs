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
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
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
        private ArenaSheet _arenaSheet = new ArenaSheet();

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
                _defaultDelayIndex = GameConfigState.DailyArenaInterval - 400;
            }

            var characterSheetAddr = Nekoyume.Addresses.GetSheetAddress<CharacterSheet>();
            var costumeStatSheetAddr = Nekoyume.Addresses.GetSheetAddress<CostumeStatSheet>();
            var sheetAddr = Nekoyume.Addresses.GetSheetAddress<ArenaSheet>();

            var csv = await GetStateAsync(characterSheetAddr);
            _characterSheet.Set(csv.ToDotnetString());

            csv = await GetStateAsync(costumeStatSheetAddr);
            _costumeStatSheet.Set(csv.ToDotnetString());

            csv = await GetStateAsync(sheetAddr);
            _arenaSheet.Set(csv.ToDotnetString());

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
            if (avatarState == null) 
            { 
            
                return int.MaxValue;
            }
            
            return Nekoyume.Battle.CPHelper.GetCPV2(avatarState, _characterSheet, _costumeStatSheet);
        }


        private long _resetIndex=0;      
        private long _defaultDelayIndex=2600;
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
            var delayIndex = _avatarSettings[Program.Agent.AvatarStates.MaxBy(x => x.Level).AvatarAddress].RankingBattleBlockIndex;

            delayIndex = delayIndex == 0 ? (int)_defaultDelayIndex : delayIndex;

            //delay 2600 blocks
            if (_topBlock.Index - _resetIndex < delayIndex)
            {
                return;
            }

            var action = new BattleArena();
            if (_StageTxs.ContainsKey(GetHashCode(new List<NCAction>() { action })))
            {
                return;
            }

            await GetWeeklyArenInfo(Program.Agent.AvatarStates.Select(x => x.Address), async (address, arenalist, arenaInfo, currentRoundData) =>
           {

               if ((_topBlock.Index - _resetIndex) < delayIndex)
               {
                   return;
               }
               Address enemyAddress = default(Address);
               int minCP = int.MaxValue;
               foreach (var info in arenalist)
               {
                   int cp;
                   if (_cps.ContainsKey(info.AvatarAddress))
                   {
                       cp = _cps[info.AvatarAddress];
                   }
                   else
                   {
                       var addrBulk = new[]
                       {
                    info.AvatarAddress,
                    info.AvatarAddress.Derive(Lib9c.SerializeKeys.LegacyInventoryKey),
                    ArenaAvatarState.DeriveAddress(info.AvatarAddress)
                       };
                       var stateBulk = await GetStateBulkAsync(addrBulk);

                       var avatar = stateBulk[addrBulk[0]] is Dictionary avatarDict
                           ? new AvatarState(avatarDict)
                           : null;
                       var inventory =
                           stateBulk[addrBulk[1]] is List inventoryList
                               ? new Nekoyume.Model.Item.Inventory(inventoryList)
                               : null;
                       if (avatar is { })
                       {
                           avatar.inventory = inventory;
                       }

                       var arenaAvatar =
                           stateBulk[addrBulk[2]] is List arenaAvatarList
                               ? new ArenaAvatarState(arenaAvatarList)
                               : null;
                       if (avatar != null && arenaAvatar != null)
                           avatar.inventory = InventoryApply(avatar.inventory, arenaAvatar);

                       cp = GetCP(avatar);
                       _cps[info.AvatarAddress] = cp; //cache cp
                   }

                   if (_cps[address] >= cp * 1.2)
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
                   action.myAvatarAddress = address;
                   action.enemyAvatarAddress = enemyAddress;
                   action.costumes = _avatars[address].inventory.Costumes.Where(i => i.equipped).Select(i => i.ItemId).ToList();
                   action.equipments = _avatars[address].inventory.Equipments.Where(i => i.equipped).Select(i => i.ItemId).ToList();
                   action.championshipId = currentRoundData.ChampionshipId;
                   action.round = currentRoundData.Round;
                   action.ticket = 1;
                   TryBattleArena(action);
               }

           });
        }

        private async Task<bool> GetWeeklyArenInfo(IEnumerable<Address> avatarAddresses, Action<Address, List<Modles.ArenaInfo>, ArenaInformation, ArenaSheet.RoundData> callback)
        {
            var currentRoundData = _arenaSheet.GetRoundByBlockIndex(_topBlock.Index); 
            var participantsAddr = ArenaParticipants.DeriveAddress(
                currentRoundData.ChampionshipId,
                currentRoundData.Round);
            _resetIndex = _topBlock.Index- (_topBlock.Index - currentRoundData.StartBlockIndex) % GameConfigState.DailyArenaInterval;


            var participants
                = await GetStateAsync(participantsAddr) is List participantsList 
                    ? new ArenaParticipants(participantsList)
                    : null;
            if (participants == null)
            {
                log.Warn($"participants is null at tip index{_topBlock.Index}");
                return false;
            }           

            avatarAddresses = avatarAddresses.Where(x => participants.AvatarAddresses.Any(y => y == x)); 
            if (!avatarAddresses.Any())
            {
                return false;
            }

            var avatarAddrList = participants.AvatarAddresses;
            var avatarAndScoreAddrList = avatarAddrList
                .Select(avatarAddr => (
                    avatarAddr,
                    ArenaScore.DeriveAddress(
                        avatarAddr,
                        currentRoundData.ChampionshipId,
                        currentRoundData.Round)))
                .ToArray();

            var scores = await GetStateBulkAsync(    
              avatarAndScoreAddrList.Select(tuple => tuple.Item2));
            var avatarAddrAndScores = avatarAndScoreAddrList
                .Select(tuple =>
                {
                    var (avatarAddr, scoreAddr) = tuple;
                    return (
                        avatarAddr,
                        scores[scoreAddr] is List scoreList
                            ? (int)(Integer)scoreList[1]
                            : ArenaScore.ArenaScoreDefault
                    );
                })
                .ToList();
            var avatarAddrAndScoresWithRank =
              AddRank(avatarAddrAndScores);
            challengeEnd = true;
            foreach (var addr in avatarAddresses)
            {
                var playerTuple = avatarAddrAndScoresWithRank.First(tuple =>
                    tuple.avatarAddr.Equals(addr));


                var playerArenaInfoAddr = ArenaInformation.DeriveAddress(
               addr,
               currentRoundData.ChampionshipId,
               currentRoundData.Round);


                var infoState = await GetStateAsync(playerArenaInfoAddr) as List;

                var playerArenaInfo = infoState == null ? null : new ArenaInformation(infoState);

                var ticket = GetTicketCount(playerArenaInfo, _topBlock.Index, currentRoundData.StartBlockIndex, GameConfigState.DailyArenaInterval);

                if (_avatarCtrls.TryGetValue(addr, out var avatarCtrl))
                {
                    avatarCtrl.BindArenaInfo(playerTuple.rank, playerTuple.score, playerArenaInfo,ticket);
                }
                var isEnd = playerArenaInfo?.Ticket <= 0;
                challengeEnd = challengeEnd & isEnd;               

                if (!isEnd && callback != null)
                {

                   var boundsList = GetBoundsWithPlayerScore(avatarAddrAndScoresWithRank, currentRoundData.ArenaType, playerTuple.score);
                   /***
                    var addrBulk = boundsList
                        .SelectMany(x => new[]
                        {
                    x.AvatarAddress,
                    x.AvatarAddress.Derive(Lib9c.SerializeKeys.LegacyInventoryKey),
                    ArenaAvatarState.DeriveAddress(x.AvatarAddress),
                        })
                        .ToList();
                    var stateBulk = await GetStateBulkAsync(addrBulk);
                    var arenaInfoList = avatarAddrAndScoresWithRank.Select(tuple =>
                    {
                        var (avatarAddr, score, rank) = tuple;
                        var avatar = stateBulk[avatarAddr] is Dictionary avatarDict
                            ? new AvatarState(avatarDict)
                            : null;
                        var inventory =
                            stateBulk[avatarAddr.Derive(Lib9c.SerializeKeys.LegacyInventoryKey)] is List inventoryList
                                ? new Nekoyume.Model.Item.Inventory(inventoryList)
                                : null;
                        if (avatar is { })
                        {
                            avatar.inventory = inventory;
                        }

                        var arenaAvatar =
                            stateBulk[ArenaAvatarState.DeriveAddress(avatarAddr)] is List arenaAvatarList
                                ? new ArenaAvatarState(arenaAvatarList)
                                : null;
                        if (avatar != null && arenaAvatar != null)
                            avatar.inventory = InventoryApply(avatar.inventory, arenaAvatar);

                        return new Modles.ArenaInfo()
                        {
                            AvatarAddress = avatarAddr,
                            Score = score,
                            Rank = rank,
                            Avatar = avatar,
                            CP = GetCP(avatar),
                        };
                    }).ToList();
                   ***/
                    callback(addr, boundsList, playerArenaInfo, currentRoundData);
                }

            }
            return true;

        }

        private int GetTicketCount(ArenaInformation arenaInfo,
            long blockIndex,
            long roundStartBlockIndex,
            int gameConfigStateDailyArenaInterval)
        {
            var currentTicketResetCount = Nekoyume.Arena.ArenaHelper.GetCurrentTicketResetCount(
                blockIndex,
                roundStartBlockIndex,
                gameConfigStateDailyArenaInterval);

            return arenaInfo.TicketResetCount < currentTicketResetCount
                ? ArenaInformation.MaxTicketCount
                : arenaInfo.Ticket;
        }

        private  Inventory InventoryApply(
            Inventory inventory,
           ArenaAvatarState arenaAvatarState)
        {
            var nonFungibleIdsToEquip = new List<Guid>(arenaAvatarState.Costumes);
            nonFungibleIdsToEquip.AddRange(arenaAvatarState.Equipments);
            foreach (var itemBase in inventory.Items.Select(e => e.item))
            {
                if (!(itemBase is IEquippableItem equippableItem))
                {
                    continue;
                }

                if (!(itemBase is INonFungibleItem nonFungibleItem) ||
                    !nonFungibleIdsToEquip.Contains(nonFungibleItem.NonFungibleId))
                {
                    equippableItem.Unequip();
                    continue;
                }

                equippableItem.Equip();
                nonFungibleIdsToEquip.Remove(nonFungibleItem.NonFungibleId);
            }

            return inventory;
        }

        private List< (Address avatarAddr, int score, int rank)> AddRank(
          List<(Address avatarAddr, int score)> tuples)
        {
            if (tuples.Count == 0)
            {
                return default;
            }

            var orderedTuples = tuples
                .OrderByDescending(tuple => tuple.score)
                .ThenBy(tuple => tuple.avatarAddr)
                .Select(tuple => (tuple.avatarAddr, tuple.score, 0))
                .ToArray();

            var result = new List<(Address avatarAddr, int score, int rank)>();
            var trunk = new List<(Address avatarAddr, int score, int rank)>();
            int? currentScore = null;
            var currentRank = 1;
            for (var i = 0; i < orderedTuples.Length; i++)
            {
                var tuple = orderedTuples[i];
                if (!currentScore.HasValue)
                {
                    currentScore = tuple.score;
                    trunk.Add(tuple);
                    continue;
                }

                if (currentScore.Value == tuple.score)
                {
                    trunk.Add(tuple);
                    currentRank++;
                    if (i < orderedTuples.Length - 1)
                    {
                        continue;
                    }

                    foreach (var tupleInTrunk in trunk)
                    {
                        result.Add((
                            tupleInTrunk.avatarAddr,
                            tupleInTrunk.score,
                            currentRank));
                    }

                    trunk.Clear();

                    continue;
                }

                foreach (var tupleInTrunk in trunk)
                {
                    result.Add((
                        tupleInTrunk.avatarAddr,
                        tupleInTrunk.score,
                        currentRank));
                }

                trunk.Clear();
                if (i < orderedTuples.Length - 1)
                {
                    trunk.Add(tuple);
                    currentScore = tuple.score;
                    currentRank++;
                    continue;
                }

                result.Add((
                    tuple.avatarAddr,
                    tuple.score,
                    currentRank + 1));
            }

            return result;
        }

        private  List<Modles.ArenaInfo> GetBoundsWithPlayerScore(
           IEnumerable<(Address avatarAddr, int score, int rank)> tuples,
           ArenaType arenaType,
           int playerScore)
        {
            switch (arenaType)
            {
                case ArenaType.OffSeason:
                    return tuples.Select(x=>new Modles.ArenaInfo
                    {
                        AvatarAddress = x.avatarAddr,
                        Score = x.score,
                        Rank = x.rank
                    }).ToList();
                case ArenaType.Season:
                case ArenaType.Championship:
                    var bounds = Nekoyume.Arena.ArenaHelper.ScoreLimits[arenaType];
                    bounds = (bounds.Item1 + playerScore, bounds.Item2 + playerScore);
                    return tuples
                        .Where(tuple =>
                            tuple.score <= bounds.Item1 &&
                            tuple.score >= bounds.Item2)
                        .Select(x => new Modles.ArenaInfo
                            {
                                AvatarAddress = x.avatarAddr,
                                Score = x.score,
                                Rank = x.rank
                            }).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(arenaType), arenaType, null);
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
                    lblDailyBlock.Text = (GameConfigState.DailyArenaInterval-(topBlock-_resetIndex )).ToString(); 
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

        public async void TryBattleArena(BattleArena action)
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
                MessageBox.Show("Arena is null or have not join yet");
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
