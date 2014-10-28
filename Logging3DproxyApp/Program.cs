using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using log4net.Config;

namespace Logging3DproxyApp
{
    internal class Program
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger
            (MethodBase.GetCurrentMethod().DeclaringType);

        private static void LoadLogConfiguration()
        {
            try
            {
                string configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "log4net.config");
                XmlConfigurator.ConfigureAndWatch(new FileInfo(configFilePath));
                Log.InfoFormat("Watching log4net config file {0}", configFilePath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to Configure and Watch log4net config log4net.config: {0}", e);
            }
        }

        private static void Main(string[] args)
        {
            LoadLogConfiguration();

            if (Environment.UserInteractive)
            {
                Log.InfoFormat("Starting User Interactive (Console) Mode");
                var app = new App();
                app.Start();
                while (!Console.KeyAvailable)
                    Thread.Sleep(500);
                app.Stop();
                Log.InfoFormat("Exiting User Interactive (Console) Mode");
            }
            else
            {
                Log.InfoFormat("Starting Service Mode");
                var servicesToRun = new ServiceBase[] { new ProxyService() };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}