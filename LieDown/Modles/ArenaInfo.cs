using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LieDown.Modles
{
    public class ArenaInfo
    {
        public Libplanet.Address AvatarAddress { get; set; }

        public int Score { get; set; }

        public int Rank { get; set; }   

        public Nekoyume.Model.State.AvatarState Avatar { get; set; }

        public int CP { get; set; }
    }
}
