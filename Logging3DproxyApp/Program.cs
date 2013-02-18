using System;
using System.ServiceProcess;
using System.Threading;

namespace Logging3DproxyApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                var app = new App(Console.WriteLine);
                app.Start();
                while (!Console.KeyAvailable)
                    Thread.Sleep(500);
                app.Stop();
            }
            else
            {
                var servicesToRun = new ServiceBase[] { new ProxyService() };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}