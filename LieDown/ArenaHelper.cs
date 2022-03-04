using System;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Threading.Tasks;
using Libplanet;
using Nekoyume.Model.State;


namespace Nekoyume
{
    public  class ArenaHelper 
    { 
        GameConfigState gameConfigState;
        public ArenaHelper(GameConfigState gameConfigState) 
        { 
        
            this.gameConfigState = gameConfigState; 
        }


        public  bool TryGetThisWeekAddress(long blockIndex, out Address weeklyArenaAddress)
        {
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            if (index < 0)
            {
                weeklyArenaAddress=new Address();
                return false;
            }

            weeklyArenaAddress = WeeklyArenaState.DeriveAddress(index);
            return true;
        }

        //public  async Task<WeeklyArenaState> GetThisWeekStateAsync(long blockIndex)
        //{

        //    if (!TryGetThisWeekAddress(blockIndex, out var address))
        //        return null;

        //    var state = await Game.Game.instance.Agent.GetStateAsync(address);
        //    return state is null ? null : new WeeklyArenaState(state);
        //}
      

        public  Address GetPrevWeekAddress(long thisWeekBlockIndex)
        {         
            var index = Math.Max((int) thisWeekBlockIndex / gameConfigState.WeeklyArenaInterval, 0);
            return WeeklyArenaState.DeriveAddress(index);
        }

        public  Address GetNextWeekAddress(long blockIndex)
        {
            var index = (int) blockIndex / gameConfigState.WeeklyArenaInterval;
            index++;
            return WeeklyArenaState.DeriveAddress(index);
        }
    }
}
