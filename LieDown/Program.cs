using Libplanet.Crypto;
using LieDown.Modles;
using NLog;
using System.Diagnostics;

namespace LieDown
{
    internal static class Program
    {
        public static PrivateKey? PrivateKey;

        public static List<NodeInfo> Nodes = new List<NodeInfo>();

        public static Agent Agent;
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static System.Collections.Generic.HashSet<string> FightingList=new HashSet<string>();       

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string [] args)
        {
            ApplicationConfiguration.Initialize();
            var config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { 
                FileName = $"./logs/ld.log", ConcurrentWrites = true, ArchiveAboveSize = 1024 * 1024 * 5, 
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,               
                Layout = "${longdate} ${level:uppercase=true} ${message:withexception=true}"
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            // Apply config           
            NLog.LogManager.Configuration = config;

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
           
            if (args.Length > 2)
            {
                try
                {
                    PrivateKey = Libplanet.Crypto.PrivateKey.FromString(args[0]);
                    Agent = new Agent() { Address = args[1] };                    
                    FightingList=args[2].Split(',',StringSplitOptions.RemoveEmptyEntries).ToHashSet();
                }
                catch { }
            }

            var preLoad = new Preload();
            if (preLoad.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            
         

            var login = new Login();
            if (login.ShowDialog() == DialogResult.OK)
            {
                var main = new Main();
                //if (args.Length == 0) {
                //    throw new Exception("test");
                //}
                Application.Run(main);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                log.Error(ex, "CurrentDomain_UnhandledException");
            }
            else
            {
                log.Error("CurrentDomain_UnhandledException {0}", e.ExceptionObject.ToJson());
            }

            //auto restart
            Start(); 
        }

        public static void Start()
        {
            if ((Program.PrivateKey?.Equals(null)).HasValue)
            {
                log.Info("Auto Restart");

                var appName = Application.ExecutablePath;
                Process ps = new Process();
                ps.StartInfo.FileName = appName;
                ps.StartInfo.Arguments = Libplanet.ByteUtil.Hex(PrivateKey.ToByteArray()) + " " + Agent.Address + " " + string.Join(",", FightingList);
                ps.Start();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            log.Error(e.Exception, "Application_ThreadException");
        }
    }
}
