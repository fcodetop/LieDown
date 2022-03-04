using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LieDown.Modles
{
    public class Character
    {
        public int ActionPoint { get; set; }
        [Newtonsoft.Json.JsonProperty("address")]
        public string AvatarAddress { get; set; }
        public string AgentAddress { get; set; }
        public string RankingMapAddress { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int Pmeter { get; set; }
        public long DailyRewardReceivedIndex { get; set; }

        public long UpdatedAt { get; set; }

        public ColletionMap StageMap { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public int CP { get; set; }

        public class ColletionMap { 
        
            public int Count { get; set; }
            public int[][] Pairs { get; set; }
        }

        public async static Task<Character> GetCharacterAync(NodeInfo node,string avatarAddress) 
        {           
           var wrap = await HttpUtils.PostAsync<StateQueryWrap>(node.GraphqlServer, "{\"query\":\"query{stateQuery{avatar(avatarAddress:\\\""+ avatarAddress + "\\\"){actionPoint,address,agentAddress,name,level,exp,dailyRewardReceivedIndex,updatedAt,stageMap{count,pairs}}}}\"}");
            return wrap.StateQuery.Avatar;
        }

    }

    public class StateQueryWrap
    {
        public StateQuery StateQuery { get; set; }
    }
    public class StateQuery
    {
        public Character Avatar { get; set; }

        public ShopState Shop { get; set; }

        public Agent Agent { get; set; }
        
    }

    public class Agent 
    {
        public string Address { get; set; }

        public string Gold { get; set; }

        public Character[] AvatarStates { get; set; }

        public async static Task<Agent> GetAgent(NodeInfo node, string address)
        {
            var wrap = await HttpUtils.PostAsync<StateQueryWrap>(node.GraphqlServer, "{\"query\":\"query{stateQuery{agent(address:\\\"" + address + "\\\"){address,gold,avatarStates{actionPoint,address,agentAddress,name,level,exp,dailyRewardReceivedIndex,updatedAt,stageMap{count,pairs}}}}}\"}");
            return wrap.StateQuery.Agent;
        }

    }


    public class ShopState {
    
    }





}
