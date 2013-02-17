using System;
using System.Threading;

namespace Logging3DproxyApp
{
    internal class Program
    {
        private static void Main()
        {
            var app = new App();
            app.Start();
            while (!Console.KeyAvailable)
                Thread.Sleep(500);
            app.Stop();
        }
    }
}