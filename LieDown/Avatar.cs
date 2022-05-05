using Libplanet;
using LieDown.Modles;
using Nekoyume.Action;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace LieDown
{
    public delegate  Task<bool> TryHackAndSlash(int stageId,  Address avatarAddress);

    public partial class Avatar : UserControl
    {
        public Avatar()
        {
            InitializeComponent();
        }

       public Character Character { get; set; }

        public Setting AvatarSetting { get; set; }

        public TryHackAndSlash TryHackAndSlash;

       public Action<Address> OnDailyReward;

        public Action<string, Setting> OnSettingSave;


        public void BindAvatar(int CP, NodeStatus.Block topBlock)
        {
            this.Invoke(() =>
            {
                lblAP.Text = Character.ActionPoint.ToString();
                lblExp.Text = Character.Exp.ToString();
                lblLevel.Text = Character.Level.ToString();
                lblName.Text = Character.Name;
                lblCP.Text = CP.ToString();
                lblStage.Text = Character.StageId.ToString();
                var meter = topBlock.Index - Character.DailyRewardReceivedIndex;
                if (meter >= 1700)
                {
                    meter = 1700;
                    if (Character.ActionPoint == 0 && btnSetting.Enabled)
                    {
                        OnDailyReward?.Invoke(Character.Address);
                    }
                }
                lblPMeter.Text = $"{meter}/1700";
            });
        }

        public void BindArenaInfo(int index, ArenaInfo arenaInfo) 
        {
            this.Invoke(() =>
            {
                lblLeftCount.Text = arenaInfo.DailyChallengeCount.ToString();
                lblWin.Text = $"{arenaInfo.ArenaRecord.Win}/{arenaInfo.ArenaRecord.Lose}";
                lblScore.Text = arenaInfo.Score.ToString();
                lblRank.Text = index.ToString();

            });
        }

        public void SetEnable(bool isEnable)
        {
            btnSetting.Enabled = isEnable;
            btnStart.Enabled = isEnable;
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            var frm = new FrmSetting();
            frm.AvatarAddress = Character.AvatarAddress;
            frm.Setting = AvatarSetting;

            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                AvatarSetting = frm.Setting;
                OnSettingSave?.Invoke(Character.AvatarAddress, AvatarSetting);
            }
        }

        bool isFighting = false;
        public void btnStart_Click(object sender, EventArgs e)
        {
            isFighting = !isFighting;
            btnStart.Text = isFighting ? "stop" : "start";

            if (isFighting)
            {
                lblFightStatus.Text = "Starting...";
                Program.FightingList.Add(Character.AgentAddress);
                Task.Run(async () =>
                {
                    while (isFighting)
                    {
                        await Task.Delay(30000);
                        if (!isFighting)
                        {
                            return;
                        }
                        if (Character.ActionPoint == 0)
                        {
                            this.Invoke(() => lblFightStatus.Text = "Waiting AP...");
                            continue;
                        }
                        else
                        {
                            this.Invoke(() => lblFightStatus.Text = "Fighting...");
                            var playCount = 1;                            
                            var stageId = Character.StageId;
                            if (AvatarSetting.Mode == Modles.SlashMode.Bootstrap)
                            {
                                stageId = AvatarSetting.Stage;
                                if (stageId > Character.StageId)
                                {

                                    MessageBox.Show("prev stage is not clear");
                                    btnStart_Click(sender, e);
                                    break;
                                }
                                playCount = Character.ActionPoint / 5;
                                playCount = playCount > 8 ? 8 : playCount; //max 8
                            }

                            if (TryHackAndSlash!=null&&! await TryHackAndSlash(stageId, Character.Address )) {
                                continue;
                            }

                            //var worldId = getWorldId(stageId);

                            //var avatarAddress = Character.Address;

                            //var action = new HackAndSlash
                            //{
                            //    costumes = AvatarState.inventory.Costumes.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                            //    equipments = AvatarState.inventory.Equipments.Where(i => i.equipped).Select(i => i.ItemId).ToList(),
                            //    foods = new List<Guid>(),
                            //    worldId = worldId,
                            //    stageId = stageId,
                            //    playCount = playCount,
                            //    avatarAddress = avatarAddress,
                            //};
                            //var actions = new List<NCAction>() { action };
                            //if (_StageTxs.ContainsKey(GetHashCode(actions)))
                            //{
                            //    continue;
                            //}
                            //await MakeTransaction(actions);
                        }
                    }
                });
            }
            else
            {
                lblFightStatus.Text = "Stopped";
                Program.FightingList.Remove(Character.AgentAddress);
            }

        }
      

        private void Avatar_Load(object sender, EventArgs e)
        {
            groupBox1.Text = Character.AvatarAddress;
            SetEnable(false);
        }
    }
}
