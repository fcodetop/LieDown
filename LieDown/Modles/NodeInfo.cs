using Libplanet.Blockchain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
namespace LieDown.Modles
{
    public class NodeInfo
    {
        public string Host { get; set; }

        public int GraphqlPort { get; set; }

        public int RpcPort { get; set; }

        public string GraphqlServer => $"http://{this.Host}:{this.GraphqlPort}/graphql";

        public string RpcUrl => $"{this.Host}:{this.RpcPort}";

        bool _preloadEnded;
        public bool PreloadEnded { get { return _preloadEnded; } internal set { _preloadEnded = value; PreloadEndedUpdateAt = DateTime.Now; } }

        public DateTime PreloadEndedUpdateAt { get; private set; }

        int _clientCount;
        public int ClientCount { get { return _clientCount; } internal set { _clientCount = value; ClientCountUpateAt = DateTime.Now; } }

        public DateTime ClientCountUpateAt { get; private set; }

        public long PingDelay { get; set; }

        public async Task<bool> GetPreloadEndedAsync()
        {
            try
            {
                var wrap = await HttpUtils.PostAsync<NodeStatusWrap>(GraphqlServer, "{\"query\":\"query{nodeStatus{bootstrapEnded,preloadEnded,isMining,tip{index},genesis{id,hash}}}\"}");
                PreloadEnded = wrap.NodeStatus.PreloadEnded;
                return PreloadEnded;
            }
            catch
            {
                PreloadEnded = false;
            }

            return false;
        }

        public async Task<long> GetBlockIndexAsync()
        {
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
                var wrap = await HttpUtils.PostAsync<NodeStatusWrap>(GraphqlServer, "{\"query\":\"query{nodeStatus{preloadEnded,tip{index}}}\"}");
                PreloadEnded = wrap.NodeStatus.PreloadEnded;
                if (PreloadEnded)
                {
                    return wrap.NodeStatus.Tip.Index;
                }
            }
            catch
            {
                PreloadEnded = false;
            }
            finally
            {
                sw.Stop();
                PingDelay = sw.ElapsedMilliseconds;
            }
            return -1;
        }

        public async Task<int> GetRpcClientCountAsync()
        {
            Stopwatch sw = new Stopwatch();
            try
            {

                sw.Start();
                var wrap = await HttpUtils.PostAsync<RpcInformationWrap>(GraphqlServer, "{\"query\":\"query{rpcInformation{totalCount}}\"}");
                ClientCount = wrap.RpcInformation.TotalCount;
                return ClientCount;
            }
            catch
            {
                ClientCount = 0;
            }
            finally
            {
                sw.Stop();
                PingDelay = sw.ElapsedMilliseconds;
            }

            return -1;

        }

        public static IEnumerable<NodeInfo> GetDefaultNodeList()
        {
            var list = new string[] {
       "aba105ce4868747839ab806175be8b37-13503424.us-east-2.elb.amazonaws.com,80,31238",
      "a0d01f897e0a2434798e8b4607ac32ea-994288696.us-east-2.elb.amazonaws.com,80,31238",
      "aaa453a1c166c4d7eb6dad7151ca373b-1343028350.us-east-2.elb.amazonaws.com,80,31238",
      "a5e8503f9fd024ca292f193c86de744a-754114509.us-east-2.elb.amazonaws.com,80,31238",
      "a00d334f09e1c42feb4c38f8c3010543-423825111.us-east-2.elb.amazonaws.com,80,31238",
      "aa7d058e4606a4cc7b2bc7c6c915670b-1136359886.us-east-2.elb.amazonaws.com,80,31238",
      "afff4ede31cef4543a2706d8b1f594e2-1812701003.us-east-2.elb.amazonaws.com,80,31238",
      "af1da83a0dbf14b1d976308c7b3efb5d-689966316.us-east-2.elb.amazonaws.com,80,31238",
      "a2bb53cb50b1f4e698396bdc9f93320e-1430351524.us-east-2.elb.amazonaws.com,80,31238",
      "a86a3b8c3140943ec9abe0115e8ab0b6-1765153438.us-east-2.elb.amazonaws.com,80,31238",
      "adb1932b4da92426abd7116c65875faa-1809698636.us-east-2.elb.amazonaws.com,80,31238",
      "ad6b3a37d7d09408593d012193bdea55-1167581570.us-east-2.elb.amazonaws.com,80,31238",
      "ac38a8718f27544c088bb73086ff305c-1852178024.us-east-2.elb.amazonaws.com,80,31238",
      "aa26cee904d0540c9ab30deb71260de6-963671627.us-east-2.elb.amazonaws.com,80,31238",
      "a68fae48aeecd4661bc653eb8bfb5815-777682477.us-east-2.elb.amazonaws.com,80,31238",
      "a9a7fa4e68584472eadaa859c1ccfa96-307491802.us-east-2.elb.amazonaws.com,80,31238",
      "a2e01f4a7f4ce47efa3097d52fdf56f8-108664089.us-east-2.elb.amazonaws.com,80,31238",
      "a6b8aa8271d6946ea998b110863cbfb9-1918498264.us-east-2.elb.amazonaws.com,80,31238",
      "ae4fa84e10d214209ad600a77371223a-1562070758.us-east-2.elb.amazonaws.com,80,31238",
      "af7640523846d4152b45b33076a5629d-1374793070.us-east-2.elb.amazonaws.com,80,31238",
            }.Select(x =>
            {
                var rawInfos = x.Split(',');
                return new NodeInfo()
                {
                    Host = rawInfos[0],
                    GraphqlPort = int.Parse(rawInfos[1]),
                    RpcPort = int.Parse(rawInfos[2])
                };
            }).ToList();
            return list;
        }

    }

    public class NodeStatus
    {

        public bool BootstrapEnded { get; set; }

        public bool PreloadEnded { get; set; }

        public bool IsMining { get; set; }

        public Genesis Genesis { get; set; }

        /// <summary>
        /// top most block
        /// </summary>
        public Block Tip { get; set; }

        public class Block
        {
            public long Index { get; set; }

        }


    }

    public class Genesis
    {
        public string ID { get; set; }
        public string Hash { get; set; }
    }
    public class NodeStatusWrap
    {
        public NodeStatus NodeStatus { get; set; }
    }

    public class RpcInformation
    {
        public int TotalCount { get; set; }
    }

    public class RpcInformationWrap
    {
        public RpcInformation RpcInformation { get; set; }
    }


}
