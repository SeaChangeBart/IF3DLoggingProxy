using System;
using System.IO;

namespace Logging3DproxyApp
{
    internal class App
    {
        private readonly ProxyHandler m_Handler1;
        private readonly ProxyHandler m_Handler2;
        public App( Action<string> traceFn)
        {
            m_Handler1 = new ProxyHandler(Properties.Settings.Default.ListenPort, Properties.Settings.Default.Resource1,
                                          Properties.Settings.Default.EndPoint1,
                                          Path.Combine(Properties.Settings.Default.LogPath,
                                                       Properties.Settings.Default.Resource1), traceFn);
            m_Handler2 = new ProxyHandler(Properties.Settings.Default.ListenPort, Properties.Settings.Default.Resource2,
                                          Properties.Settings.Default.EndPoint2,
                                          Path.Combine(Properties.Settings.Default.LogPath,
                                                       Properties.Settings.Default.Resource2), traceFn);
        }
        public void Start()
        {
            m_Handler1.Start();
            m_Handler2.Start();
        }
        public void Stop()
        {
            m_Handler1.Stop();
            m_Handler2.Stop();
        }
    }
}