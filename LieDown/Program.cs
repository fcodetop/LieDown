using Libplanet.Crypto;
using LieDown.Modles;

namespace LieDown
{
    internal static class Program
    {
        public static PrivateKey? PrivateKey;

        public static List<NodeInfo> Nodes=new List<NodeInfo>();

        public static Agent Agent;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            var preLoad = new Preload();
            if (preLoad.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            var login = new Login();
            if (login.ShowDialog() == DialogResult.OK)
            {
                var main = new Main();
                Application.Run(main);
            }
        }
    }
}
